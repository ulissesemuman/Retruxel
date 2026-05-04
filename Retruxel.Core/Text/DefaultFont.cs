using SkiaSharp;

namespace Retruxel.Core.Text;

/// <summary>
/// Built-in 8x8 monochrome bitmap font for Retruxel.
///
/// Data derived from font8x8 by Daniel Hepper (Public Domain).
/// Original source: https://github.com/dhepper/font8x8
/// Original data from IBM VGA fonts by Marcel Sondaar (Public Domain).
///
/// Format: each glyph is 8 bytes, row-wise, LSB = pixel 0.
/// Example: 'A' = { 0x0C, 0x1E, 0x33, 0x33, 0x3F, 0x33, 0x33, 0x00 }
/// </summary>
public static class DefaultFont
{
    private static readonly Dictionary<char, byte[]> _glyphs = BuildGlyphs();

    public static byte[]? GetGlyph(char c)
        => _glyphs.TryGetValue(c, out var g) ? g : null;

    public static bool Supports(char c)
        => _glyphs.ContainsKey(c);

    public static IReadOnlyCollection<char> SupportedCharacters
        => _glyphs.Keys;

    /// <summary>
    /// Renders a string to an SKBitmap for preview.
    /// Returns null if text is empty.
    /// </summary>
    public static SKBitmap? RenderString(string text, SKColor foreground, SKColor background)
    {
        if (string.IsNullOrEmpty(text)) return null;

        int width = text.Length * 8;
        var bitmap = new SKBitmap(width, 8, SKColorType.Rgba8888, SKAlphaType.Premul);
        
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(background);

        for (int i = 0; i < text.Length; i++)
        {
            var glyph = GetGlyph(text[i]);
            if (glyph is null) continue;

            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                    if (((glyph[row] >> col) & 1) == 1)
                        bitmap.SetPixel(i * 8 + col, row, foreground);
        }

        return bitmap;
    }

    private static Dictionary<char, byte[]> BuildGlyphs()
    {
        var d = new Dictionary<char, byte[]>();

        // Basic Latin (U+0020-U+007F) - ASCII printable characters
        foreach (var (ch, data) in BasicLatin.Glyphs)
            d[ch] = data;

        // Extended Latin (U+00A0-U+00FF) - Accented characters for PT/ES/FR/DE
        foreach (var (ch, data) in ExtendedLatin.Glyphs)
            d[ch] = data;

        // Box Drawing (U+2500-U+257F) - Useful for HUD frames
        foreach (var (ch, data) in BoxDrawing.Glyphs)
            d[ch] = data;

        // Block Elements (U+2580-U+259F) - Shading and progress bars
        foreach (var (ch, data) in BlockElements.Glyphs)
            d[ch] = data;

        // Greek (U+0390-U+03C9) - Greek alphabet
        foreach (var (ch, data) in Greek.Glyphs)
            d[ch] = data;

        // Hiragana (U+3040-U+309F) - Japanese syllabary
        foreach (var (ch, data) in Hiragana.Glyphs)
            d[ch] = data;

        // Miscellaneous - Currency and special symbols
        foreach (var (ch, data) in Miscellaneous.Glyphs)
            d[ch] = data;

        // SGA (U+E541-U+E55A) - Stargate Alphabet (sci-fi)
        foreach (var (ch, data) in SGA.Glyphs)
            d[ch] = data;

        return d;
    }
}
