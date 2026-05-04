using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Text;

/// <summary>
/// Converts font8x8 1bpp glyphs to SMS 4bpp planar tile format.
/// </summary>
public class SmsFontConverter : IFontConverter
{
    public string TargetId => "sms";

    public byte[] ConvertGlyphs(IEnumerable<(char Character, byte[] Bitmap)> glyphs)
    {
        var result = new List<byte>();
        foreach (var (_, bitmap) in glyphs)
            result.AddRange(ConvertOneTile(bitmap));
        return [.. result];
    }

    /// <summary>
    /// Converts one 8x8 1bpp glyph (font8x8 format) to SMS 4bpp planar tile (32 bytes).
    ///
    /// font8x8 format: LSB of byte N = leftmost pixel of row N
    /// SMS tile format: 4 bytes per row, 1 byte per bitplane
    ///   byte 0 = bitplane 0 (color bit 0), MSB = leftmost pixel
    ///   byte 1 = bitplane 1, byte 2 = bitplane 2, byte 3 = bitplane 3
    ///
    /// We use bitplane 0 only → color index 1 (fg) vs 0 (transparent/bg)
    /// NOTE: SMS bitplane byte has MSB as leftmost pixel — opposite of font8x8 LSB convention.
    /// </summary>
    private static byte[] ConvertOneTile(byte[] bitmap)
    {
        var tile = new byte[32];
        for (int row = 0; row < 8; row++)
        {
            // font8x8: LSB = pixel 0 (leftmost)
            // SMS bitplane: MSB = pixel 0 (leftmost)
            // → need to reverse bit order
            byte sourceByte = bitmap[row];
            byte reversed   = ReverseBits(sourceByte);

            tile[row * 4 + 0] = reversed; // bitplane 0 — color index bit 0
            tile[row * 4 + 1] = 0;        // bitplane 1
            tile[row * 4 + 2] = 0;        // bitplane 2
            tile[row * 4 + 3] = 0;        // bitplane 3
        }
        return tile;
    }

    private static byte ReverseBits(byte b)
    {
        byte r = 0;
        for (int i = 0; i < 8; i++)
        {
            r = (byte)((r << 1) | (b & 1));
            b >>= 1;
        }
        return r;
    }
}
