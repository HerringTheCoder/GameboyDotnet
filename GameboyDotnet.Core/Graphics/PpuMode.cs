namespace GameboyDotnet.PPU;

/// <summary>
/// Order is chronological on purpose
/// </summary>
public enum PpuMode
{
    HBlankMode0 = 0,
    VBlankMode1 = 1,
    OamScanMode2 = 2,
    VramAccessMode3 = 3
}