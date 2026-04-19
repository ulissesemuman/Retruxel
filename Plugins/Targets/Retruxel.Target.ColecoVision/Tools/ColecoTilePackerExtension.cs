using Retruxel.Core.Interfaces;

namespace Retruxel.Target.ColecoVision.Tools;

/// <summary>
/// ColecoVision-specific extension for tile_packer tool.
/// Disables all transformations (TMS9918 VDP has no flip/rotation support).
/// </summary>
public class ColecoTilePackerExtension : IToolExtension
{
    public string ToolId => "tile_packer";

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // TMS9918 VDP has no flip or rotation support
        input["enableFlipH"] = false;
        input["enableFlipV"] = false;
        input["enableRotation"] = false;

        // Generic tool already executed, we receive its output
        var tilemap = input["tilemap"] as List<object> ?? new List<object>();
        var formattedTilemap = new List<Dictionary<string, object>>();

        foreach (var entry in tilemap)
        {
            if (entry is not Dictionary<string, object> dict) continue;

            var tileIndex = Convert.ToInt32(dict["TileIndex"]);

            formattedTilemap.Add(new Dictionary<string, object>
            {
                ["tileIndex"] = tileIndex,
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
