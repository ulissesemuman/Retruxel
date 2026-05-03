using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Text;

/// <summary>
/// Converts 8x8 1bpp bitmap glyphs to SMS 4bpp planar tile format.
/// Each SMS tile is 32 bytes (4 bitplanes × 8 rows).
/// </summary>
public class SmsFontConverter : IFontConverter
{
    public string TargetId => "sms";

    public byte[] ConvertGlyphs(IEnumerable<(char Character, byte[] Bitmap)> glyphs)
    {
        var result = new List<byte>();
        foreach (var (_, bitmap) in glyphs)
        {
            result.AddRange(ConvertTile(bitmap));
        }
        return result.ToArray();
    }

    private static byte[] ConvertTile(byte[] bitmap8x8)
    {
        var tile = new byte[32];
        for (int row = 0; row < 8; row++)
        {
            tile[row * 4 + 0] = bitmap8x8[row];
            tile[row * 4 + 1] = 0;
            tile[row * 4 + 2] = 0;
            tile[row * 4 + 3] = 0;
        }
        return tile;
    }
}
