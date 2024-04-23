using GameboyDotnet.Components;
using GameboyDotnet.Extensions;
using GameboyDotnet.Graphics.Registers;
using GameboyDotnet.Graphics.Registers.LcdControl;
using GameboyDotnet.Memory;
using GameboyDotnet.PPU;

namespace GameboyDotnet.Graphics;

public class Lcd(MemoryController memoryController)
{
    public const int ScreenHeight = 144;
    public const int ScreenWidth = 160;
    public byte[,] Buffer = new byte[160, 144];
    public byte Lcdc => memoryController.ReadByte(Constants.LCDControlRegister);
    private byte Lyc => memoryController.ReadByte(Constants.LYCompareRegister);

    public byte Ly
    {
        get => memoryController.ReadByte(Constants.LYRegister);
        private set => memoryController.WriteByte(Constants.LYRegister, value);
    }

    public byte Stat
    {
        get => memoryController.ReadByte(Constants.LcdStatusRegister);
        private set => memoryController.WriteByte(Constants.LcdStatusRegister, value);
    }

    public byte Scx => memoryController.ReadByte(Constants.SCXRegister);
    public byte Scy => memoryController.ReadByte(Constants.SCYRegister);
    public byte Wy => memoryController.ReadByte(Constants.WYRegister);
    public byte Wx => memoryController.ReadByte(Constants.WXRegister);
    public byte Obp1 => memoryController.ReadByte(Constants.OBP1Register);
    public byte Obp0 => memoryController.ReadByte(Constants.OBP0Register);
    public byte Bgp => memoryController.ReadByte(Constants.BGPRegister);

    public BgTileMapArea BgTileMapArea => Lcdc.IsBitSet(3)
        ? BgTileMapArea.Tilemap9C00
        : BgTileMapArea.Tilemap9800;

    public BgWindowTileDataArea TileDataSelect => Lcdc.IsBitSet(4)
        ? BgWindowTileDataArea.Unsigned8000
        : BgWindowTileDataArea.Signed8800;

    public WindowDisplay WindowDisplay => Lcdc.IsBitSet(5)
        ? WindowDisplay.Enabled
        : WindowDisplay.Disabled;

    public WindowTileMapArea WindowTileMapArea => Lcdc.IsBitSet(6)
        ? WindowTileMapArea.Tilemap9C00
        : WindowTileMapArea.Tilemap9800;

    public ObjDisplay ObjDisplay => Lcdc.IsBitSet(1)
        ? ObjDisplay.Enabled
        : ObjDisplay.Disabled;

    public BgWindowDisplayPriority BgWindowDisplayPriority => Lcdc.IsBitSet(0)
        ? BgWindowDisplayPriority.High
        : BgWindowDisplayPriority.Low;

    public ObjSize ObjSize => Lcdc.IsBitSet(2)
        ? ObjSize.Size8X16
        : ObjSize.Size8X8;
    
    
    public void UpdatePpuMode(PpuMode currentPpuMode)
    {
        var stat = (byte)((Stat & 0b11111100) | (byte)currentPpuMode);

        if (currentPpuMode == PpuMode.VBlankMode1)
        {
            RequestVBlankInterrupt();
        }
        
        switch (currentPpuMode)
        {
            case PpuMode.HBlankMode0 when stat.IsBitSet(3):
                RequestLcdInterrupt();
                break;
            case PpuMode.VBlankMode1 when stat.IsBitSet(4):
                RequestVBlankInterrupt();
                break;
            case PpuMode.OamScanMode2 when stat.IsBitSet(5):
                RequestLcdInterrupt();
                break;
        }

        Stat = stat;
    }

    public void UpdateLy(byte ly)
    {
        var stat = Stat;
        bool isLyEqualToLyc = ly == Lyc;

        if (isLyEqualToLyc && stat.IsBitSet(6)) //LYC=LY stat interrupt enabled
        {
            RequestLcdInterrupt();
        }

        Ly = ly;
        Stat = isLyEqualToLyc ? stat.SetBit(2) : stat.ClearBit(2);
    }

    private void RequestVBlankInterrupt()
    {
        memoryController.WriteByte(
            Constants.IFRegister,
            memoryController.ReadByte(Constants.IFRegister).SetBit(0)
        );
    }

    private void RequestLcdInterrupt()
    {
        memoryController.WriteByte(
            Constants.IFRegister,
            memoryController.ReadByte(Constants.IFRegister).SetBit(1)
        );
    }
}