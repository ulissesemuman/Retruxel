namespace Retruxel.Core.Text;

/// <summary>
/// Miscellaneous glyphs (currency symbols and special characters).
/// Data from font8x8_misc.h by Daniel Hepper (Public Domain).
/// Includes currency symbols and mathematical operators.
/// </summary>
internal static class Miscellaneous
{
    internal static readonly (char, byte[])[] Glyphs =
    [
        ('\u20A7', [0x1F, 0x33, 0x33, 0x5F, 0x63, 0xF3, 0x63, 0xE3]), // Spanish Pesetas/Pt
        ('\u0192', [0x70, 0xD8, 0x18, 0x3C, 0x18, 0x18, 0x1B, 0x0E]), // dutch florijn
        ('\u2310', [0x00, 0x00, 0x00, 0x3F, 0x03, 0x03, 0x00, 0x00])  // gun pointing right
    ];
}
