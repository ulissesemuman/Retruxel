using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.TilePacker;

/// <summary>
/// SMS/Game Gear/SG-1000 wrapper for TilePacker.
/// Enables horizontal and vertical flip detection (VDP supports both).
/// </summary>
public class TilePackerSMSTool : ITool
{
    public string ToolId => "retruxel.tool.tilepacker.sms";
    public string DisplayName => "Tile Packer (SMS/GG/SG-1000)";
    public string Description => "Tilemap optimizer for SMS/Game Gear/SG-1000 with H/V flip support";
    public string Category => "Optimization";
    public bool IsStandalone => false;
    public bool RequiresProject => false;

    private readonly TilePackerTool _baseTool = new();

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // SMS VDP supports horizontal and vertical flip
        input["enableFlipH"] = true;
        input["enableFlipV"] = true;
        input["enableRotation"] = false;

        var result = _baseTool.Execute(input);

        // Format tilemap for SMS VDP
        var tilemap = (List<object>)result["tilemap"];
        var formattedTilemap = new List<Dictionary<string, object>>();

        foreach (var entry in tilemap)
        {
            var dict = (Dictionary<string, object>)entry;
            var tileIndex = Convert.ToInt32(dict["TileIndex"]);
            var flipH = Convert.ToBoolean(dict["FlipH"]);
            var flipV = Convert.ToBoolean(dict["FlipV"]);

            // SMS VDP format: bits 9-10 for flip flags
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

        result["tilemap"] = formattedTilemap;
        return result;
    }
}
