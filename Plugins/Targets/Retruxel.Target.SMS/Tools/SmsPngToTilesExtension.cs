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
        if (!input.TryGetValue("palette", out var paletteObj) || paletteObj is not uint[] palette)
            return new();

        var smsPalette = SmsColorUtils.ConvertPaletteToSmsRgb222(palette);

        return new Dictionary<string, object>
        {
            ["paletteHardware"] = smsPalette,
            ["paletteHex"] = string.Join(", ", smsPalette.Select(b => $"0x{b:X2}"))
        };
    }
}
