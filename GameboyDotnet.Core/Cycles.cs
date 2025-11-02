namespace GameboyDotnet;

public static class Cycles
{
    /// <summary>
    /// Indicates whether CGB double speed mode is active
    /// In double speed mode, CPU runs at 2x speed but PPU operates at normal speed
    /// </summary>
    public static bool CgbDoubleSpeedMode = false;
    
    private static byte SpeedRatio => CgbDoubleSpeedMode ? (byte)2 : (byte)1;
    
    public static byte DivFallingEdgeDetectorBitIndex => (byte)(CgbDoubleSpeedMode ? 5 : 4);
    public static int CyclesPerSecond => 4194304 * SpeedRatio;
    
    // PPU timing does NOT scale with double speed mode
    // In double speed mode, the CPU runs twice as fast, but the PPU stays the same speed
    // This means we need twice as many CPU cycles to match the same PPU frame time
    public static int CyclesPerFrame => 70224 * SpeedRatio;
    public static int CyclesPerScanline => 456 * SpeedRatio;
    public static int OamScanMode2CyclesThreshold => 80 * SpeedRatio;
    public static int VramMode3CyclesThreshold => (80+172) * SpeedRatio;
    public static int HBlankMode0CyclesThreshold => (80+172+204) * SpeedRatio;
    public static int VBlankMode1CyclesThreshold => (80+172+204) * SpeedRatio;
    public static int DividerCycles => 256; //Total Cycle divided by 16384Hz or 32768Hz (scales with speed)
}