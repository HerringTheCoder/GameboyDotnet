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

    public byte Lcdc => memoryController.IoRegisters.MemorySpace[0x40];
    public byte Lyc => memoryController.IoRegisters.MemorySpace[0x45];

    public byte Ly
    {
        get => memoryController.IoRegisters.MemorySpace[0x44];
        private set => memoryController.IoRegisters.MemorySpace[0x44] = value;
    }
    
    public byte Stat
    {
        // 0xFF41 & ~(0xFF00) = 0xFF41 & 0x00FF = 0x0041
        get => memoryController.IoRegisters.MemorySpace[Constants.LcdStatusRegister & ~BankAddress.IoRegistersStart];
        private set => memoryController.IoRegisters.MemorySpace[0x41] = value;
    }

    public byte Scx => memoryController.IoRegisters.MemorySpace[0x43];
    public byte Scy => memoryController.IoRegisters.MemorySpace[0x42];
    public byte Wy => memoryController.IoRegisters.MemorySpace[0x4A];
    public byte Wx => memoryController.IoRegisters.MemorySpace[0x4B];
    public byte Obp1 => memoryController.IoRegisters.MemorySpace[0x49];
    public byte Obp0 => memoryController.IoRegisters.MemorySpace[0x48];
    public byte Bgp => memoryController.IoRegisters.MemorySpace[0x47];

    public BgWindowDisplayPriority BgWindowDisplayPriority => Lcdc.IsBitSet(0)
        ? BgWindowDisplayPriority.High
        : BgWindowDisplayPriority.Low;
    public ObjDisplay ObjDisplay => Lcdc.IsBitSet(1)
        ? ObjDisplay.Enabled
        : ObjDisplay.Disabled;

    public ObjSize ObjSize => Lcdc.IsBitSet(2)
        ? ObjSize.Size8X16
        : ObjSize.Size8X8;
    
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
        bool isLyEqualToLyc = ly == Lyc;

        if (isLyEqualToLyc && Stat.IsBitSet(6)) //LYC=LY stat interrupt enabled
        {
            RequestLcdInterrupt();
        }

        Ly = ly;
        
        Stat = isLyEqualToLyc
            ? Stat.SetBit(2)
            : Stat.ClearBit(2);
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