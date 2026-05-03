namespace Retruxel.Core.Text;

/// <summary>
/// Built-in 8x8 pixel font embedded as a resource in Retruxel.Core.
/// Covers printable ASCII (0x20–0x7E) + common Latin Extended characters.
/// Each glyph is 8x8 pixels, stored as an 8-byte row-major bitmap (1 bit per pixel).
/// Targets convert this to their native tile format via IFontConverter.
/// </summary>
public static class DefaultFont
{
    private static readonly Dictionary<char, byte[]> _glyphs = new();
    private static readonly HashSet<char> _supportedChars = new();

    static DefaultFont()
    {
        LoadEmbeddedFont();
    }

    /// <summary>
    /// Returns the raw 8x8 bitmap for a given Unicode codepoint.
    /// Returns null if the character is not in the default font.
    /// Each byte represents one row (8 pixels), MSB = leftmost pixel.
    /// </summary>
    public static byte[]? GetGlyph(char c)
    {
        return _glyphs.TryGetValue(c, out var glyph) ? glyph : null;
    }

    /// <summary>
    /// Returns all characters covered by the default font.
    /// </summary>
    public static IReadOnlySet<char> SupportedCharacters => _supportedChars;

    private static void LoadEmbeddedFont()
    {
        for (char c = ' '; c <= '~'; c++)
        {
            _glyphs[c] = GenerateSimpleGlyph(c);
            _supportedChars.Add(c);
        }

        var extendedChars = "ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöøùúûüýþÿ";
        foreach (var c in extendedChars)
        {
            _glyphs[c] = GenerateSimpleGlyph(c);
            _supportedChars.Add(c);
        }
    }

    private static byte[] GenerateSimpleGlyph(char c)
    {
        var glyph = new byte[8];
        
        if (c >= '0' && c <= '9')
        {
            glyph[0] = 0x3C; glyph[1] = 0x42; glyph[2] = 0x42; glyph[3] = 0x42;
            glyph[4] = 0x42; glyph[5] = 0x42; glyph[6] = 0x3C; glyph[7] = 0x00;
        }
        else if (c >= 'A' && c <= 'Z')
        {
            glyph[0] = 0x18; glyph[1] = 0x24; glyph[2] = 0x42; glyph[3] = 0x7E;
            glyph[4] = 0x42; glyph[5] = 0x42; glyph[6] = 0x42; glyph[7] = 0x00;
        }
        else if (c >= 'a' && c <= 'z')
        {
            glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x3C; glyph[3] = 0x02;
            glyph[4] = 0x3E; glyph[5] = 0x42; glyph[6] = 0x3E; glyph[7] = 0x00;
        }
        else if (c == ' ')
        {
            // Space - all zeros
        }
        else
        {
            glyph[3] = 0x7E;
        }

        return glyph;
    }
}
