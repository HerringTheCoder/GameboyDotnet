namespace GameboyDotnet;

/// <summary>
/// Manages Game Boy Color specific state and features
/// </summary>
public class CgbState
{
    /// <summary>
    /// The CGB mode detected from cartridge header byte 0x143
    /// </summary>
    public CgbMode CartridgeCgbMode { get; set; } = CgbMode.DmgOnly;
    
    /// <summary>
    /// The hardware type we're emulating (DMG or CGB)
    /// </summary>
    public HardwareType EmulatedHardware { get; set; } = HardwareType.Cgb;
    
    /// <summary>
    /// Whether CGB features are enabled (based on cartridge and hardware)
    /// </summary>
    public bool IsCgbEnabled => EmulatedHardware == HardwareType.Cgb && 
                                (CartridgeCgbMode == CgbMode.CgbCompatible || CartridgeCgbMode == CgbMode.CgbOnly);
    
    /// <summary>
    /// Whether running in DMG compatibility mode (CGB hardware, DMG game)
    /// </summary>
    public bool IsDmgCompatibilityMode => EmulatedHardware == HardwareType.Cgb && CartridgeCgbMode == CgbMode.DmgOnly;
    
    /// <summary>
    /// Speed switch armed flag (bit 0 of KEY1)
    /// </summary>
    public bool SpeedSwitchArmed { get; set; }
    
    /// <summary>
    /// Current VRAM bank (0 or 1)
    /// </summary>
    public byte CurrentVramBank { get; set; } = 0;
    
    /// <summary>
    /// Current WRAM bank (1-7, bank 0 is always mapped to C000-CFFF)
    /// </summary>
    public byte CurrentWramBank { get; set; } = 1;
    
    /// <summary>
    /// Object priority mode: false = CGB-style, true = DMG-style
    /// </summary>
    public bool DmgStyleObjectPriority { get; set; } = false;
    
    /// <summary>
    /// HDMA source address (high byte and low byte combined)
    /// </summary>
    public ushort HdmaSource { get; set; }
    
    /// <summary>
    /// HDMA destination address (in VRAM 8000-9FF0)
    /// </summary>
    public ushort HdmaDestination { get; set; }
    
    /// <summary>
    /// Remaining HDMA transfer length in blocks (0-127, each block is 16 bytes)
    /// </summary>
    public byte HdmaLength { get; set; } = 0xFF;
    
    /// <summary>
    /// Whether HBlank DMA is active
    /// </summary>
    public bool HdmaActive { get; set; }
    
    /// <summary>
    /// Whether this is HBlank DMA (true) or General Purpose DMA (false)
    /// </summary>
    public bool HdmaIsHBlankMode { get; set; }
    
    /// <summary>
    /// Background color palette specification (BCPS/BGPI - FF68)
    /// Bit 7: Auto-increment, Bits 5-0: Index
    /// </summary>
    public byte BackgroundPaletteIndex { get; set; }
    
    /// <summary>
    /// Object color palette specification (OCPS/OBPI - FF6A)
    /// Bit 7: Auto-increment, Bits 5-0: Index
    /// </summary>
    public byte ObjectPaletteIndex { get; set; }
    
    /// <summary>
    /// 8 background palettes, 4 colors each, 2 bytes per color (RGB555)
    /// Total: 64 bytes (8 palettes × 4 colors × 2 bytes)
    /// </summary>
    public byte[] BackgroundPaletteMemory { get; } = new byte[64];
    
    /// <summary>
    /// 8 object palettes, 4 colors each, 2 bytes per color (RGB555)
    /// Total: 64 bytes (8 palettes × 4 colors × 2 bytes)
    /// </summary>
    public byte[] ObjectPaletteMemory { get; } = new byte[64];
    
    /// <summary>
    /// Converts RGB555 palette data to RGB888 for rendering
    /// </summary>
    public static (byte r, byte g, byte b) ConvertRgb555ToRgb888(ushort rgb555)
    {
        // RGB555 format: 0bbbbbgggggrrrrr
        byte r5 = (byte)(rgb555 & 0x1F);
        byte g5 = (byte)((rgb555 >> 5) & 0x1F);
        byte b5 = (byte)((rgb555 >> 10) & 0x1F);
        
        // Convert 5-bit to 8-bit by scaling: value * 255 / 31
        // Using bit shifting for efficiency: (value << 3) | (value >> 2)
        byte r8 = (byte)((r5 << 3) | (r5 >> 2));
        byte g8 = (byte)((g5 << 3) | (g5 >> 2));
        byte b8 = (byte)((b5 << 3) | (b5 >> 2));
        
        return (r8, g8, b8);
    }
}
