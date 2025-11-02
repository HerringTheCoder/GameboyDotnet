namespace GameboyDotnet;

//https://gbdev.io/pandocs/Timer_and_Divider_Registers.html
public static class Constants
{
    public const ushort JoypadRegister = 0xFF00;
    public const ushort IFRegister = 0xFF0F;
    public const ushort IERegister = 0xFFFF;
    public const int R8_HL_Index = 6;
    public const ushort DIVRegister = 0xFF04;
    public const ushort TIMARegister = 0xFF05;
    public const ushort TMARegister = 0xFF06;
    public const ushort TACRegister = 0xFF07;

    //LCD, PPU
    public const ushort LCDControlRegister = 0xFF40;
    public const ushort LcdStatusRegister = 0xFF41;
    public const ushort SCYRegister = 0xFF42;
    public const ushort SCXRegister = 0xFF43;
    public const ushort LYRegister = 0xFF44;
    public const ushort LYCompareRegister = 0xFF45;
    public const ushort DMARegister = 0xFF46;
    public const ushort BGPRegister = 0xFF47;
    public const ushort OBP0Register = 0xFF48;
    public const ushort OBP1Register = 0xFF49;
    public const ushort WYRegister = 0xFF4A;
    public const ushort WXRegister = 0xFF4B;
    
    //CGB Registers
    public const ushort KEY0Register = 0xFF4C; // CPU mode select (CGB only)
    public const ushort KEY1Register = 0xFF4D; // Prepare speed switch (CGB only)
    public const ushort VBKRegister = 0xFF4F; // VRAM bank (CGB only)
    public const ushort HDMA1Register = 0xFF51; // VRAM DMA source high (CGB only)
    public const ushort HDMA2Register = 0xFF52; // VRAM DMA source low (CGB only)
    public const ushort HDMA3Register = 0xFF53; // VRAM DMA destination high (CGB only)
    public const ushort HDMA4Register = 0xFF54; // VRAM DMA destination low (CGB only)
    public const ushort HDMA5Register = 0xFF55; // VRAM DMA length/mode/start (CGB only)
    public const ushort RPRegister = 0xFF56; // Infrared communications port (CGB only)
    public const ushort BCPSRegister = 0xFF68; // Background color palette specification (CGB only)
    public const ushort BCPDRegister = 0xFF69; // Background color palette data (CGB only)
    public const ushort OCPSRegister = 0xFF6A; // Object color palette specification (CGB only)
    public const ushort OCPDRegister = 0xFF6B; // Object color palette data (CGB only)
    public const ushort OPRIRegister = 0xFF6C; // Object priority mode (CGB only)
    public const ushort SVBKRegister = 0xFF70; // WRAM bank (CGB only)
}