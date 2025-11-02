namespace GameboyDotnet;

/// <summary>
/// Represents the Game Boy Color compatibility mode based on cartridge header byte 0x143
/// </summary>
public enum CgbMode
{
    /// <summary>
    /// DMG (original Game Boy) mode - no CGB features
    /// </summary>
    DmgOnly = 0x00,
    
    /// <summary>
    /// CGB enhanced mode - supports both DMG and CGB ($80)
    /// </summary>
    CgbCompatible = 0x80,
    
    /// <summary>
    /// CGB only mode - only works on CGB hardware ($C0)
    /// </summary>
    CgbOnly = 0xC0
}

/// <summary>
/// Represents the detected hardware type
/// </summary>
public enum HardwareType
{
    /// <summary>
    /// Original Game Boy (DMG) hardware
    /// </summary>
    Dmg = 0x01,
    
    /// <summary>
    /// Game Boy Color (CGB) or Game Boy Advance (GBA) hardware
    /// </summary>
    Cgb = 0x11
}
