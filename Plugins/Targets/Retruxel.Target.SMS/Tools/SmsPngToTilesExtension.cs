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
        var result = new Dictionary<string, object>();

        // 1. Process Palette
        if (input.TryGetValue("palette", out var paletteObj) && paletteObj is uint[] palette)
        {
            var smsPalette = SmsColorUtils.ConvertPaletteToSmsRgb222(palette);
            result["paletteHardware"] = smsPalette;
            result["paletteHex"] = string.Join(", ", smsPalette.Select(b => $"0x{b:X2}"));
        }

        // 2. Process Tiles
        if (input.TryGetValue("tilesArray", out var tilesObj) && tilesObj is byte[] tiles)
        {
            result["totalBytes"] = tiles.Length;
            
            // Format tiles as hex string, 16 bytes per line for readability
            var hexLines = new List<string>();
            for (int i = 0; i < tiles.Length; i += 16)
            {
                var chunk = tiles.Skip(i).Take(16);
                hexLines.Add("    " + string.Join(", ", chunk.Select(b => $"0x{b:X2}")));
            }
            result["tilesHex"] = string.Join(",\n", hexLines);
            
            if (input.TryGetValue("tileCount", out var count))
                result["tileCount"] = count;
        }

        return result;
    }
}
