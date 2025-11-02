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
        
        _cyclesCounter += cpuCycles;
        
        while (true)
        {
            var previousLy = _ly;
            
            // Process current scanline based on current state
            if (_ly >= Lcd.ScreenHeight)
            {
                // VBlank mode
                if (_cyclesCounter >= Cycles.VBlankMode1CyclesThreshold)
                {
                    _ly++;
                    _cyclesCounter -= Cycles.VBlankMode1CyclesThreshold;
                    if (_ly == Lcd.ScreenHeight + 10)
                    {
                        _ly = 0;
                        _windowLineCounter = 0;
                        _windowYConditionTriggered = false;
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                // Visible scanline modes
                if (_cyclesCounter < Cycles.OamScanMode2CyclesThreshold)
                {
                    // Still in OAM scan
                    if (_ly == Lcd.Wy && Lcd.WindowDisplay == WindowDisplay.Enabled)
                    {
                        _windowYConditionTriggered = true;
                    }
                    break;
                }
                else if (_cyclesCounter < Cycles.VramMode3CyclesThreshold)
                {
                    // Still in VRAM access
                    break;
                }
                else if (_cyclesCounter < Cycles.HBlankMode0CyclesThreshold)
                {
                    // Still in HBlank
                    if (_cyclesCounter >= Cycles.VramMode3CyclesThreshold && _cyclesCounter < Cycles.HBlankMode0CyclesThreshold)
                    {
                        var previousPpuMode = (PpuMode)(Lcd.Stat & 0b11);
                        if (previousPpuMode == PpuMode.VramAccessMode3)
                        {
                            PushScanlineToBuffer();
                        }
                        
                        if (memoryController.CgbState.HdmaActive && memoryController.CgbState.HdmaIsHBlankMode)
                        {
                            memoryController.PerformHBlankDmaBlock();
                        }
                    }
                    break;
                }
                else
                {
                    // End of scanline, move to next line
                    if (_cyclesCounter >= Cycles.VramMode3CyclesThreshold)
                    {
                        var previousPpuMode = (PpuMode)(Lcd.Stat & 0b11);
                        if (previousPpuMode == PpuMode.VramAccessMode3)
                        {
                            PushScanlineToBuffer();
                        }
                    }
                    
                    if (memoryController.CgbState.HdmaActive && memoryController.CgbState.HdmaIsHBlankMode)
                    {
                        memoryController.PerformHBlankDmaBlock();
                    }
                    
                    _ly++;
                    _cyclesCounter -= Cycles.HBlankMode0CyclesThreshold;
                }
            }
            
            if (previousLy != _ly)
            {
                Lcd.UpdateLy(_ly);
            }
        }
        
        // Update PPU mode based on final state
        var currentPpuMode = CalculateCurrentPpuMode();
        var previousMode = (PpuMode)(Lcd.Stat & 0b11);
        if (previousMode != currentPpuMode)
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
        
        bool isCgbMode = memoryController.CgbState.IsCgbEnabled;
        bool useDmgPriority = isCgbMode && memoryController.CgbState.DmgStyleObjectPriority;
        
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
        
        // Sort sprites by X coordinate (leftmost/smallest X has priority) in DMG mode or when DMG priority is set
        // In CGB mode with CGB priority, OAM order takes precedence
        if (!isCgbMode || useDmgPriority)
        {
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
        }
        
        // Render sprites in priority order (but draw in reverse to respect priority)
        for (var spriteIdx = visibleSpritesCount - 1; spriteIdx >= 0; spriteIdx--)
        {
            var (x, oamOffset) = visibleSprites[spriteIdx];
            var y = oamMemoryView[oamOffset] - 16;
            var tileNumber = oamMemoryView[oamOffset.Add(2)];
            var objAttributes = oamMemoryView[oamOffset.Add(3)];
            var attributes = ExtractObjectAttributes(ref objAttributes, isCgbMode);
            var objSize = (byte)Lcd.ObjSize;
            const byte spriteWidth = 8;

            if (objSize == 16)
            {
                tileNumber &= 0xFE;
            }

            // In CGB mode, check VRAM bank bit
            if (isCgbMode && attributes.cgbVramBank)
            {
                var originalBank = memoryController.CgbState.CurrentVramBank;
                memoryController.CgbState.CurrentVramBank = 1;
                
                var tileAddress = (ushort)(0x8000 + tileNumber * 16);
                var tileLine = attributes.yFlipped ? objSize - 1 - (_ly - y) : _ly - y;
                var tileLineAddress = tileAddress.Add((ushort)(tileLine * 2));
                var tileLineData = memoryController.ReadWord(tileLineAddress);
                
                memoryController.CgbState.CurrentVramBank = originalBank;

                for (var pixel = 0; pixel < spriteWidth; pixel++)
                {
                    var pixelPos = attributes.xFlipped ? pixel : spriteWidth - 1 - pixel;
                    var pixelColor = (ushort)(tileLineData.GetBit(pixelPos + 8) << 1 |
                                              tileLineData.GetBit(pixelPos));

                    if (x + pixel is < 0 or >= Lcd.ScreenWidth || pixelColor == 0)
                        continue;

                    var screenX = x + pixel;
                    
                    bool shouldDrawSprite = ShouldDrawSprite(screenX, attributes.objToBackgroundPriority, isCgbMode);
                    
                    if (shouldDrawSprite)
                        SetCgbPixel(screenX, _ly, pixelColor, attributes.cgbPaletteNumber, isBackground: false);
                }
            }
            else
            {
                var tileAddress = (ushort)(0x8000 + tileNumber * 16);
                var tileLine = attributes.yFlipped ? objSize - 1 - (_ly - y) : _ly - y;
                var tileLineAddress = tileAddress.Add((ushort)(tileLine * 2));
                var tileLineData = memoryController.ReadWord(tileLineAddress);

                for (var pixel = 0; pixel < spriteWidth; pixel++)
                {
                    var pixelPos = attributes.xFlipped ? pixel : spriteWidth - 1 - pixel;
                    var pixelColor = (ushort)(tileLineData.GetBit(pixelPos + 8) << 1 |
                                              tileLineData.GetBit(pixelPos));

                    if (x + pixel is < 0 or >= Lcd.ScreenWidth || pixelColor == 0)
                        continue;

                    var screenX = x + pixel;
                    
                    bool shouldDrawSprite = ShouldDrawSprite(screenX, attributes.objToBackgroundPriority, isCgbMode);
                    
                    if (shouldDrawSprite)
                    {
                        if (isCgbMode)
                        {
                            SetCgbPixel(screenX, _ly, pixelColor, attributes.cgbPaletteNumber, isBackground: false);
                        }
                        else
                        {
                            var paletteColor = GetPaletteColorByPixelColor(ref attributes.palette, ref pixelColor);
                            Lcd.Buffer[screenX, _ly] = paletteColor;
                        }
                    }
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldDrawSprite(int screenX, bool objToBackgroundPriority, bool isCgbMode)
    {
        if (!isCgbMode)
        {
            // DMG mode
            if (Lcd.BgWindowDisplayPriority == BgWindowDisplayPriority.Low)
            {
                return true;
            }
            else
            {
                var bgRawColor = _bgColorLine[screenX];
                return !objToBackgroundPriority || bgRawColor == 0;
            }
        }
        else
        {
            // CGB mode
            var bgRawColor = _bgColorLine[screenX];
            
            // If LCDC.0 is off, sprites are always drawn
            if (Lcd.BgWindowDisplayPriority == BgWindowDisplayPriority.Low)
                return true;
            
            // If BG color is 0, sprite is drawn
            if (bgRawColor == 0)
                return true;
            
            // Otherwise, check OBJ-to-BG priority bit
            return !objToBackgroundPriority;
        }
    }
    
    private void RenderBackgroundOrWindow()
    {
        var wx = Lcd.Wx.Subtract(7);
        var wy = Lcd.Wy;
        var scx = Lcd.Scx;
        var scy = Lcd.Scy;
        var palette = Lcd.Bgp;
        
        bool isCgbMode = memoryController.CgbState.IsCgbEnabled;

        // In DMG mode, when LCDC bit 0 is clear, both BG and Window are disabled
        // In CGB mode, LCDC bit 0 changes priority behavior but doesn't disable BG/Window
        bool isBgWindowEnabled = Lcd.BgWindowDisplayPriority == BgWindowDisplayPriority.High;
        
        // Window is visible if:
        // 1. BG/Window is enabled (LCDC bit 0) in DMG mode, or always in CGB mode
        // 2. Window is enabled (LCDC bit 5)
        // 3. WY condition was triggered (WY=LY happened at some Mode 2 this frame)
        bool isWindowActive = (isCgbMode || isBgWindowEnabled) && 
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
            var tileMapAddress = (ushort)(baseTileMapAddress + tileRowIndex + tileColumnIndex);
            
            var tileIndex = memoryController.ReadByte(tileMapAddress);
            
            // In CGB mode, read tile attributes from VRAM bank 1
            byte tileAttributes = 0;
            byte cgbPaletteNumber = 0;
            bool xFlip = false;
            bool yFlip = false;
            bool vramBank = false;
            bool bgToOamPriority = false;
            
            if (isCgbMode)
            {
                // Switch to VRAM bank 1 to read attributes
                var originalBank = memoryController.CgbState.CurrentVramBank;
                memoryController.CgbState.CurrentVramBank = 1;
                tileAttributes = memoryController.ReadByte(tileMapAddress);
                memoryController.CgbState.CurrentVramBank = originalBank;
                
                // Parse CGB tile attributes
                cgbPaletteNumber = (byte)(tileAttributes & 0x07);
                vramBank = (tileAttributes & 0x08) != 0;
                xFlip = (tileAttributes & 0x20) != 0;
                yFlip = (tileAttributes & 0x40) != 0;
                bgToOamPriority = (tileAttributes & 0x80) != 0;
            }

            // Get tile data from appropriate VRAM bank
            if (isCgbMode && vramBank)
            {
                var originalBank = memoryController.CgbState.CurrentVramBank;
                memoryController.CgbState.CurrentVramBank = 1;
                
                var tileNumber = Lcd.TileDataSelect switch
                {
                    BgWindowTileDataArea.Unsigned8000
                        => (ushort)(0x8000 + tileIndex * 16),
                    BgWindowTileDataArea.Signed8800
                        => (ushort)(0x8800 + ((sbyte)tileIndex + 128) * 16),
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                var tileLine = yFlip ? (7 - (yPos & 0b111)) : (yPos & 0b111);
                var tileData = memoryController.ReadWord((ushort)(tileNumber + tileLine * 2));
                memoryController.CgbState.CurrentVramBank = originalBank;
                
                var bitIndex = xFlip ? (xPos & 0b111) : (7 - (xPos & 0b111));
                var pixelColor = (ushort)((tileData.GetBit(bitIndex + 8) << 1) |
                                          tileData.GetBit(bitIndex));
                
                _bgColorLine[pixel] = (byte)pixelColor;
                
                if (isCgbMode || isBgWindowEnabled)
                {
                    SetCgbPixel(pixel, _ly, pixelColor, cgbPaletteNumber, isBackground: true);
                }
                else
                {
                    Lcd.Buffer[pixel, _ly] = 0;
                }
            }
            else
            {
                var tileNumber = Lcd.TileDataSelect switch
                {
                    BgWindowTileDataArea.Unsigned8000
                        => (ushort)(0x8000 + tileIndex * 16),
                    BgWindowTileDataArea.Signed8800
                        => (ushort)(0x8800 + ((sbyte)tileIndex + 128) * 16),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var tileLine = yFlip ? (7 - (yPos & 0b111)) : (yPos & 0b111);
                var tileData = memoryController.ReadWord((ushort)(tileNumber + tileLine * 2));

                var bitIndex = xFlip ? (xPos & 0b111) : (7 - (xPos & 0b111));
                var pixelColor = (ushort)((tileData.GetBit(bitIndex + 8) << 1) |
                                          tileData.GetBit(bitIndex));

                // Store raw pixel color for sprite priority checks
                _bgColorLine[pixel] = (byte)pixelColor;

                if (isCgbMode)
                {
                    SetCgbPixel(pixel, _ly, pixelColor, cgbPaletteNumber, isBackground: true);
                }
                else
                {
                    //Get color from BGP palette
                    var paletteColor = GetPaletteColorByPixelColor(ref palette, ref pixelColor);

                    // DMG mode behavior: When LCDC bit 0 is clear, both BG and Window become white
                    if (!isBgWindowEnabled)
                    {
                        Lcd.Buffer[pixel, _ly] = 0; // Force white (DMG mode)
                    }
                    else
                    {
                        Lcd.Buffer[pixel, _ly] = paletteColor;
                    }
                }
            }
        }
        
        // Increment window line counter if window was rendered this scanline
        if (windowRenderedThisLine)
            _windowLineCounter++;
    }
    
    /// <summary>
    /// Sets a pixel in CGB mode using the color palette system
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetCgbPixel(int x, int y, ushort colorIndex, byte paletteNumber, bool isBackground)
    {
        // Get the palette memory (background or object)
        var paletteMemory = isBackground 
            ? memoryController.CgbState.BackgroundPaletteMemory 
            : memoryController.CgbState.ObjectPaletteMemory;
        
        // Calculate palette address: paletteNumber * 8 + colorIndex * 2
        int paletteAddr = (paletteNumber * 8) + (colorIndex * 2);
        
        // Read RGB555 color (little-endian)
        ushort rgb555 = (ushort)(paletteMemory[paletteAddr] | (paletteMemory[paletteAddr + 1] << 8));
        
        // Convert to RGB888
        var (r, g, b) = CgbState.ConvertRgb555ToRgb888(rgb555);
        
        // Store in color buffer
        Lcd.ColorBuffer[x, y, 0] = r;
        Lcd.ColorBuffer[x, y, 1] = g;
        Lcd.ColorBuffer[x, y, 2] = b;
        
        // Also update the DMG buffer for compatibility (convert to grayscale)
        byte gray = (byte)((r + g + b) / 3);
        Lcd.Buffer[x, y] = (byte)(gray >> 6); // Convert 0-255 to 0-3
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
    
    private (bool objToBackgroundPriority, bool yFlipped, bool xFlipped, byte palette, byte cgbPaletteNumber, bool cgbVramBank) 
        ExtractObjectAttributes(ref byte objectAttributes, bool isCgbMode)
    {
        bool objToBackgroundPriority = objectAttributes.IsBitSet(7);
        bool yFlipped = objectAttributes.IsBitSet(6);
        bool xFlipped = objectAttributes.IsBitSet(5);
        
        if (isCgbMode)
        {
            // In CGB mode:
            // Bit 4 is still used for DMG palette selection for backwards compatibility
            // Bit 3 selects VRAM bank (0 or 1)
            // Bits 0-2 select CGB palette (0-7)
            byte dmgPalette = objectAttributes.IsBitSet(4) ? Lcd.Obp1 : Lcd.Obp0;
            bool vramBank = objectAttributes.IsBitSet(3);
            byte cgbPalette = (byte)(objectAttributes & 0x07);
            
            return (objToBackgroundPriority, yFlipped, xFlipped, dmgPalette, cgbPalette, vramBank);
        }
        else
        {
            // DMG mode
            byte palette = objectAttributes.IsBitSet(4) ? Lcd.Obp1 : Lcd.Obp0;
            return (objToBackgroundPriority, yFlipped, xFlipped, palette, 0, false);
        }
    }
}