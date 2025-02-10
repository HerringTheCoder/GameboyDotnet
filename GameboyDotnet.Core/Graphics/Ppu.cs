using System.Runtime.CompilerServices;
using GameboyDotnet.Extensions;
using GameboyDotnet.Graphics.Registers.LcdControl;
using GameboyDotnet.Memory;
using GameboyDotnet.PPU;

namespace GameboyDotnet.Graphics;

public class Ppu(MemoryController memoryController)
{
    private int _cyclesCounter;
    private byte _ly;

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
        if (Lcd.BgWindowDisplayPriority == BgWindowDisplayPriority.High)
        {
            RenderBackgroundOrWindow();
        }
           

        if (Lcd.ObjDisplay == ObjDisplay.Enabled)
            RenderObjects();
    }

    private void RenderObjects()
    {
        var oamMemoryView = memoryController.Oam.MemorySpaceView;
        var objectsCount = 0;
        for (ushort oamOffset = 0;
             oamOffset < 160;
             oamOffset += 4)
        {
            var y = oamMemoryView[oamOffset] - 16;
            var x = oamMemoryView[oamOffset.Add(1)] - 8;
            var tileNumber = oamMemoryView[oamOffset.Add(2)];
            var objAttributes = oamMemoryView[oamOffset.Add(3)];
            var attributes = ExtractObjectAttributes(ref objAttributes);
            var objSize = (byte)Lcd.ObjSize;
            const byte spriteWidth = 8;

            if (objSize == 16)
            {
                tileNumber &= 0xFE;
            }

            if (_ly < y || _ly >= y + objSize)
                continue;

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

                //Get color from palette
                var paletteColor = GetPaletteColorByPixelColor(ref attributes.palette, ref pixelColor);

                //Check if object should be rendered over background
                if (!attributes.objToBackgroundPriority || Lcd.Buffer[x + pixel, _ly] == 0)
                    Lcd.Buffer[x + pixel, _ly] = paletteColor;
            }

            objectsCount++;

            if (objectsCount == 10)
                break;
        }
    }
    
    private void RenderBackgroundOrWindow()
    {
        var wx = Lcd.Wx.Subtract(7);
        var wy = Lcd.Wy;
        var scx = Lcd.Scx;
        var scy = Lcd.Scy;
        var palette = Lcd.Bgp;

        bool isWindow = Lcd.WindowDisplay is WindowDisplay.Enabled && wy <= _ly;

        var baseTileMapAddress = isWindow
            ? (ushort)Lcd.WindowTileMapArea
            : (ushort)Lcd.BgTileMapArea;

        var yPos = isWindow ? _ly.Subtract(wy) : _ly.Add(scy);
        var tileLineIndex = (byte)((yPos & 7) * 2);
        var tileRowIndex = (ushort)(yPos / 8 * 32);
        ushort tileData = 0;

        for (byte pixel = 0; pixel < Lcd.ScreenWidth; pixel++)
        {
            var xPos = isWindow && pixel >= wx ? pixel.Subtract(wx) : pixel.Add(scx);

            if ((pixel & 0x7) == 0 || ((pixel + scx) & 7) == 0)
            {
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

                tileData = memoryController.ReadWord((ushort)(tileNumber + tileLineIndex));
            }

            var bitIndex = 7 - (xPos & 0b111);
            var pixelColor = (ushort)((tileData.GetBit(bitIndex + 8) << 1) |
                                      tileData.GetBit(bitIndex));


            //Get color from BGP palette
            var paletteColor = GetPaletteColorByPixelColor(ref palette, ref pixelColor);

            Lcd.Buffer[pixel, _ly] = paletteColor;
        }
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