using Retruxel.Core.Interfaces;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for tile_packer tool.
/// Enables H/V flip detection and formats tilemap for SMS VDP.
/// </summary>
public class SmsTilePackerExtension : IToolExtension
{
    public string ToolId => "tile_packer";

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["enableFlipH"] = true,
        ["enableFlipV"] = true,
        ["enableRotation"] = false
    };

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // input already contains the output from TilePackerTool with flipH/flipV correctly applied
        var tilemap = input["tilemap"] as List<object> ?? new List<object>();
        var formattedTilemap = new List<Dictionary<string, object>>();

        foreach (var entry in tilemap)
        {
            if (entry is not Dictionary<string, object> dict) continue;

            var tileIndex = Convert.ToInt32(dict["TileIndex"]);
            var flipH = Convert.ToBoolean(dict["FlipH"]);
            var flipV = Convert.ToBoolean(dict["FlipV"]);

            // SMS VDP nametable entry format:
            // bits 0-8:  tile index
            // bit 9:     flip H
            // bit 10:    flip V
            var vdpValue = tileIndex | (flipH ? 0x200 : 0) | (flipV ? 0x400 : 0);

            formattedTilemap.Add(new Dictionary<string, object>
            {
                ["tileIndex"] = tileIndex,
                ["flipH"] = flipH,
                ["flipV"] = flipV,
                ["vdpValue"] = vdpValue,
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
