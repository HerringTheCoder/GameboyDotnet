namespace GameboyDotnet.Graphics.Registers.LcdControl;

/// <summary>
/// LCDC Bit 4 - BG & Window Tile Data Select
/// </summary>
public enum BgWindowTileDataArea : ushort
{
    Unsigned8000 = 0x8000,
    Signed8800 = 0x8800,
}