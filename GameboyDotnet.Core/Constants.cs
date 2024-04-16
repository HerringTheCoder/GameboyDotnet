namespace GameboyDotnet;

//https://gbdev.io/pandocs/Timer_and_Divider_Registers.html
public static class Constants
{
    public const int DmgCyclesPerSecond = 4194304;
    public const int GbcCyclesPerSecond = 4194304 * 2;
    public const int DmgCyclesPerFrame = DmgCyclesPerSecond/60;
    public const int GbcCyclesPerFrame = GbcCyclesPerSecond/60;
    //Divider has 16384Mhz on DMG and 32768Mhz on GBC, so the ratio is the same
    public const int DividerCycles = DmgCyclesPerSecond/16384;
    
    public const ushort IFRegister = 0xFF0F;
    public const ushort IERegister = 0xFFFF;
    public const int R8_HL_Index = 6;
    public const ushort DIVRegister = 0xFF04;
    public const ushort TIMARegister = 0xFF05;
    public const ushort TMARegister = 0xFF06;
    public const ushort TACRegister = 0xFF07;
    
    
    public const ushort LYRegister = 0xFF44;
    public const ushort LYCompareRegister = 0xFF45;
    public const ushort LcdStatusRegister = 0xFF41;
    public const ushort LCDControlRegister = 0xFF40;
    public const ushort DMARegister = 0xFF46;
}