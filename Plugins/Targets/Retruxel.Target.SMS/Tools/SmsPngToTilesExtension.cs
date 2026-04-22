using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for the png_to_tiles tool.
/// Provides SMS-specific parameters and converts palette to RGB222 format.
/// 
/// SMS specs:
/// - 4bpp planar format
/// - Interleave by line
/// - Palette: RGB222 (6 bits total: 2 bits per channel)
/// - Max 16 colors per palette
/// - Tiles: 8x8 pixels
/// </summary>
public class SmsPngToTilesExtension : IToolExtension
{
    public string ToolId => "png_to_tiles";

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["bpp"] = 4,
        ["maxColors"] = 16,
        ["tileFormat"] = "Planar",
        ["interleaveMode"] = "Line"
    };

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // input already contains the output from PngToTilesTool with generic palette
        if (!input.TryGetValue("palette", out var paletteObj) || paletteObj is not uint[] palette)
            return new();

        var smsPalette = ConvertToSmsRgb222(palette);

        return new Dictionary<string, object>
        {
            ["paletteHardware"] = smsPalette,
            ["paletteHex"] = string.Join(", ", smsPalette.Select(b => $"0x{b:X2}"))
        };
    }

    /// <summary>
    /// Converts RGB888 palette to SMS RGB222 format.
    /// SMS palette format: 0bBBGGRR (2 bits per channel, 6 bits total)
    /// </summary>
    private byte[] ConvertToSmsRgb222(uint[] rgbPalette)
    {
        var result = new byte[rgbPalette.Length];

        for (int i = 0; i < rgbPalette.Length; i++)
        {
            uint rgb = rgbPalette[i];
            byte r = (byte)((rgb >> 16) & 0xFF);
            byte g = (byte)((rgb >> 8) & 0xFF);
            byte b = (byte)(rgb & 0xFF);

            // Convert 8-bit to 2-bit per channel
            byte r2 = (byte)((r >> 6) & 0x03);
            byte g2 = (byte)((g >> 6) & 0x03);
            byte b2 = (byte)((b >> 6) & 0x03);

            // SMS format: 0bBBGGRR
            result[i] = (byte)(r2 | (g2 << 2) | (b2 << 4));
        }

        return result;
    }
}
