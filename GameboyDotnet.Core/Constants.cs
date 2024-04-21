namespace GameboyDotnet;

//https://gbdev.io/pandocs/Timer_and_Divider_Registers.html
public static class Constants
{
    public const ushort IFRegister = 0xFF0F;
    public const ushort IERegister = 0xFFFF;
    public const int R8_HL_Index = 6;
    public const ushort DIVRegister = 0xFF04;
    public const ushort TIMARegister = 0xFF05;
    public const ushort TMARegister = 0xFF06;
    public const ushort TACRegister = 0xFF07;


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
    public static ushort WYRegister = 0xFF4A;
    public static ushort WXRegister = 0xFF4B;
}