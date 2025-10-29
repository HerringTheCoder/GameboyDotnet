using System.Runtime.CompilerServices;
using GameboyDotnet.Extensions;
using GameboyDotnet.Graphics.Registers.LcdControl;
using GameboyDotnet.Memory;
using GameboyDotnet.PPU;

namespace GameboyDotnet.Graphics;

public class Ppu(MemoryController memoryController)
{
    public FrameBuffer FrameBuffer { get; } = new();
    private int _cyclesCounter;
    private byte _ly;
    private byte _windowLineCounter;
    private bool _windowYConditionTriggered; // WY=LY was met this frame
    private readonly byte[] _bgColorLine = new byte[160]; // Track raw BG colors (0-3) for sprite priority

    private PpuMode CalculateCurrentPpuMode()
    {
        if (_ly >= Lcd.ScreenHeight)
            return PpuMode.VBlankMode1;

        return _cyclesCounter switch
        {
            _ when _cyclesCounter < Cycles.OamScanMode2CyclesThreshold
                => PpuMode.OamScanMode2,
            _ when _cyclesCounter >= Cycles.OamScanMode2CyclesThreshold &&
                   _cyclesCounter < Cycles.VramMode3CyclesThreshold
                => PpuMode.VramAccessMode3,
            _ when _cyclesCounter >= Cycles.VramMode3CyclesThreshold &&
                   _cyclesCounter < Cycles.HBlankMode0CyclesThreshold
                => PpuMode.HBlankMode0,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public Lcd Lcd { get; } = new(memoryController);

    public void PushPpuCycles(byte cpuCycles)
    {
        if(!Lcd.Lcdc.IsBitSet(7))
            return;
        
        var previousPpuMode = (PpuMode)(Lcd.Stat & 0b11);
        var previousLy = Lcd.Ly;
        _cyclesCounter += cpuCycles;

        switch (previousPpuMode)
        {
            case PpuMode.OamScanMode2:
                // Check WY=LY condition at the start of Mode 2 (OAM scan)
                if (_ly == Lcd.Wy && Lcd.WindowDisplay == WindowDisplay.Enabled)
                {
                    _windowYConditionTriggered = true;
                }
                //Pixel FIFO?
                break;
            case PpuMode.VramAccessMode3:
                if (_cyclesCounter >= Cycles.VramMode3CyclesThreshold)
                {
                    PushScanlineToBuffer();
                }
                break;
            case PpuMode.HBlankMode0:
                if (_cyclesCounter >= Cycles.HBlankMode0CyclesThreshold)
                {
                    _ly++;
                    _cyclesCounter -= Cycles.HBlankMode0CyclesThreshold;
                }
                break;
            case PpuMode.VBlankMode1:
                if (_cyclesCounter >= Cycles.VBlankMode1CyclesThreshold)
                {
                    _ly++;
                    _cyclesCounter -= Cycles.VBlankMode1CyclesThreshold;
                    if (_ly == Lcd.ScreenHeight + 10)
                    {
                        _ly = 0;
                        _windowLineCounter = 0; // Reset window line counter at the start of a new frame
                        _windowYConditionTriggered = false; // Reset WY condition for new frame
                    }
                }
                break;
        }
        
        if (previousLy != _ly)
        {
            Lcd.UpdateLy(_ly);
        }

        var currentPpuMode = CalculateCurrentPpuMode();
        if (previousPpuMode != currentPpuMode)
        {
            Lcd.UpdatePpuMode(currentPpuMode);
        }
    }

    private void PushScanlineToBuffer()
    {   
        RenderBackgroundOrWindow();
        
        if (Lcd.ObjDisplay == ObjDisplay.Enabled)
            RenderObjects();
    }

    private void RenderObjects()
    {
        var oamMemoryView = memoryController.Oam.MemorySpaceView;
        Span<(int x, ushort oamOffset)> visibleSprites = stackalloc (int x, ushort oamOffset)[10];
        var visibleSpritesCount = 0;
        
        // Collect visible sprites
        for (ushort oamOffset = 0;
             oamOffset < 160 && visibleSpritesCount < 10;
             oamOffset += 4)
        {
            var y = oamMemoryView[oamOffset] - 16;
            var objSize = (byte)Lcd.ObjSize;
            
            if (_ly < y || _ly >= y + objSize)
                continue;
            
            var x = oamMemoryView[oamOffset.Add(1)] - 8;
            visibleSprites[visibleSpritesCount++] = (x, oamOffset);
        }
        
        // Sort sprites by X coordinate (leftmost/smallest X has priority)
        for (var i = 0; i < visibleSpritesCount - 1; i++)
        {
            for (var j = i + 1; j < visibleSpritesCount; j++)
            {
                if (visibleSprites[j].x < visibleSprites[i].x)
                {
                    (visibleSprites[i], visibleSprites[j]) = (visibleSprites[j], visibleSprites[i]);
                }
            }
        }
        
        // Render sprites in priority order (but draw in reverse to respect priority)
        for (var spriteIdx = visibleSpritesCount - 1; spriteIdx >= 0; spriteIdx--)
        {
            var (x, oamOffset) = visibleSprites[spriteIdx];
            var y = oamMemoryView[oamOffset] - 16;
            var tileNumber = oamMemoryView[oamOffset.Add(2)];
            var objAttributes = oamMemoryView[oamOffset.Add(3)];
            var attributes = ExtractObjectAttributes(ref objAttributes);
            var objSize = (byte)Lcd.ObjSize;
            const byte spriteWidth = 8;

            if (objSize == 16)
            {
                tileNumber &= 0xFE;
            }

            var tileAddress = (ushort)(0x8000 + tileNumber * 16);
            //Calculate tile line to render
            var tileLine = attributes.yFlipped ? objSize - 1 - (_ly - y) : _ly - y;
            var tileLineAddress = tileAddress.Add((ushort)(tileLine * 2));
            var tileLineData = memoryController.ReadWord(tileLineAddress);

            for (var pixel = 0; pixel < spriteWidth; pixel++)
            {
                //Calculate pixel position
                var pixelPos = attributes.xFlipped ? pixel : spriteWidth - 1 - pixel;

                //Combine 2 pixel data bytes (2BPP) to get the color number at corresponding pixel
                //i.e. Combine word bits at the same bytes position, 15+7, 14+6 and so on...
                var pixelColor = (ushort)(tileLineData.GetBit(pixelPos + 8) << 1 |
                                          tileLineData.GetBit(pixelPos));

                //Skip if pixel is outside of screen or transparent
                if (x + pixel is < 0 or >= Lcd.ScreenWidth || pixelColor == 0)
                    continue;

                var screenX = x + pixel;
                
                //Get color from palette
                var paletteColor = GetPaletteColorByPixelColor(ref attributes.palette, ref pixelColor);
                
                bool shouldDrawSprite;
                if (Lcd.BgWindowDisplayPriority == BgWindowDisplayPriority.Low)
                {
                    shouldDrawSprite = true;
                }
                else
                {
                    var bgRawColor = _bgColorLine[screenX];
                    shouldDrawSprite = !attributes.objToBackgroundPriority || bgRawColor == 0;
                }
                
                if (shouldDrawSprite)
                    Lcd.Buffer[screenX, _ly] = paletteColor;
            }
        }
    }
    
    private void RenderBackgroundOrWindow()
    {
        var wx = Lcd.Wx.Subtract(7);
        var wy = Lcd.Wy;
        var scx = Lcd.Scx;
        var scy = Lcd.Scy;
        var palette = Lcd.Bgp;

        // In DMG mode, when LCDC bit 0 is clear, both BG and Window are disabled
        // TODO: In CGB mode, LCDC bit 0 changes priority behavior but doesn't disable BG/Window
        bool isBgWindowEnabled = Lcd.BgWindowDisplayPriority == BgWindowDisplayPriority.High;
        
        // Window is visible if:
        // 1. BG/Window is enabled (LCDC bit 0)
        // 2. Window is enabled (LCDC bit 5)
        // 3. WY condition was triggered (WY=LY happened at some Mode 2 this frame)
        bool isWindowActive = isBgWindowEnabled && 
                             Lcd.WindowDisplay is WindowDisplay.Enabled && 
                             _windowYConditionTriggered;
        bool windowRenderedThisLine = false;

        for (byte pixel = 0; pixel < Lcd.ScreenWidth; pixel++)
        {
            bool isWindow = isWindowActive && pixel >= wx;
            
            if (isWindow)
                windowRenderedThisLine = true;

            var baseTileMapAddress = isWindow
                ? (ushort)Lcd.WindowTileMapArea
                : (ushort)Lcd.BgTileMapArea;

            // Use window line counter for window, otherwise use scrolled position
            var yPos = isWindow ? _windowLineCounter : _ly.Add(scy);
            var xPos = isWindow ? pixel.Subtract(wx) : pixel.Add(scx);
            
            var tileLineIndex = (byte)((yPos & 0b111) * 2);
            var tileRowIndex = (ushort)(yPos / 8 * 32);
            var tileColumnIndex = (ushort)(xPos / 8);
            var tileAddress = (ushort)(baseTileMapAddress + tileRowIndex + tileColumnIndex);

            var tileNumber = Lcd.TileDataSelect switch
            {
                BgWindowTileDataArea.Unsigned8000
                    => (ushort)(0x8000 + memoryController.ReadByte(tileAddress) * 16),
                BgWindowTileDataArea.Signed8800
                    => (ushort)(0x8800 + ((sbyte)memoryController.ReadByte(tileAddress) + 128) * 16),
                _ => throw new ArgumentOutOfRangeException()
            };

            var tileData = memoryController.ReadWord((ushort)(tileNumber + tileLineIndex));

            var bitIndex = 7 - (xPos & 0b111);
            var pixelColor = (ushort)((tileData.GetBit(bitIndex + 8) << 1) |
                                      tileData.GetBit(bitIndex));

            // Store raw pixel color for sprite priority checks
            _bgColorLine[pixel] = (byte)pixelColor;

            //Get color from BGP palette
            var paletteColor = GetPaletteColorByPixelColor(ref palette, ref pixelColor);

            // DMG mode behavior: When LCDC bit 0 is clear, both BG and Window become white
            // CGB mode behavior: LCDC bit 0 affects priority but BG/Window still render
            if (!isBgWindowEnabled)
            {
                Lcd.Buffer[pixel, _ly] = 0; // Force white (DMG mode)
            }
            else
            {
                Lcd.Buffer[pixel, _ly] = paletteColor;
            }
        }
        
        // Increment window line counter if window was rendered this scanline
        if (windowRenderedThisLine)
            _windowLineCounter++;
    }
    
    private static byte GetPaletteColorByPixelColor(ref byte palette, ref ushort pixelColor)
    {
        return pixelColor switch
        {
            0 => (byte)(palette & 0b11),
            1 => (byte)((palette & 0b11_00) >> 2),
            2 => (byte)((palette & 0b11_00_00) >> 4),
            3 => (byte)((palette & 0b11_00_00_00) >> 6),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private (bool objToBackgroundPriority, bool yFlipped, bool xFlipped, byte palette) ExtractObjectAttributes(
        ref byte objectAttributes)
    {
        return (
            objectAttributes.IsBitSet(7),
            objectAttributes.IsBitSet(6),
            objectAttributes.IsBitSet(5),
            objectAttributes.IsBitSet(4) ? Lcd.Obp1 : Lcd.Obp0 //TODO: GBC mode palette
        );
    }
}