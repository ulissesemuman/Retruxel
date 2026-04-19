using Retruxel.Core.Interfaces;

namespace Retruxel.Target.NES.Tools;

/// <summary>
/// NES-specific extension for tile_packer tool.
/// Enables H/V flip detection and formats tilemap for NES PPU.
/// </summary>
public class NesTilePackerExtension : IToolExtension
{
    public string ToolId => "tile_packer";

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // NES PPU supports horizontal and vertical flip
        input["enableFlipH"] = true;
        input["enableFlipV"] = true;
        input["enableRotation"] = false;

        // Generic tool already executed, we receive its output
        var tilemap = input["tilemap"] as List<object> ?? new List<object>();
        var formattedTilemap = new List<Dictionary<string, object>>();

        foreach (var entry in tilemap)
        {
            if (entry is not Dictionary<string, object> dict) continue;

            var tileIndex = Convert.ToInt32(dict["TileIndex"]);
            var flipH = Convert.ToBoolean(dict["FlipH"]);
            var flipV = Convert.ToBoolean(dict["FlipV"]);

            // NES attribute byte format: bits 6-7 for flip flags
            var attributeByte = (flipH ? 0x40 : 0) | (flipV ? 0x80 : 0);

            formattedTilemap.Add(new Dictionary<string, object>
            {
                ["tileIndex"] = tileIndex,
                ["flipH"] = flipH,
                ["flipV"] = flipV,
                ["attributeByte"] = attributeByte,
                ["x"] = dict["X"],
                ["y"] = dict["Y"]
            });
        }

        return new Dictionary<string, object>
        {
            ["tilemap"] = formattedTilemap
        };
    }
}
