namespace GameboyDotnet;

public static class Cycles
{
    public static bool CgbSpeedSwitch = false;
    private static byte SpeedRatio => CgbSpeedSwitch ? (byte)2 : (byte)1;
    
    public static int CyclesPerSecond => 4194304 * (SpeedRatio);
    public static int CyclesPerFrame => 70224 * SpeedRatio;
    public static int CyclesPerScanline => 456 * SpeedRatio;
    public static int OamScanMode2CyclesThreshold => 80 * SpeedRatio;
    public static int VramMode3CyclesThreshold => (80+172) * SpeedRatio;
    public static int HBlankMode0CyclesThreshold => (80+172+204) * SpeedRatio;
    public static int VBlankMode1CyclesThreshold => (80+172+204) * SpeedRatio;
    public static int DividerCycles => CyclesPerSecond/(16384 * SpeedRatio);
}