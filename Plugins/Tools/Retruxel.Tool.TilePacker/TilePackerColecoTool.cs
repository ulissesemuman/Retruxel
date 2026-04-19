

namespace Retruxel.Tool.TilePacker;

/// <summary>
/// ColecoVision wrapper for TilePacker.
/// Disables all transformations (TMS9918 VDP has no flip/rotation support).
/// </summary>
public class TilePackerColecoTool : ITool
{
    public string ToolId => "retruxel.tool.tilepacker.coleco";
    public string DisplayName => "Tile Packer (ColecoVision)";
    public string Description => "Tilemap optimizer for ColecoVision (no flip support)";
    public object? Icon => null;
    public string Category => "Optimization";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => "coleco";
    public bool RequiresProject => false;

    private readonly TilePackerTool _baseTool = new();

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // TMS9918 VDP has no flip or rotation support
        input["enableFlipH"] = false;
        input["enableFlipV"] = false;
        input["enableRotation"] = false;

        var result = _baseTool.Execute(input);

        // Format tilemap for TMS9918 VDP (simple tile index only)
        var tilemap = (List<object>)result["tilemap"];
        var formattedTilemap = new List<Dictionary<string, object>>();

        foreach (var entry in tilemap)
        {
            var dict = (Dictionary<string, object>)entry;
            var tileIndex = Convert.ToInt32(dict["TileIndex"]);

            formattedTilemap.Add(new Dictionary<string, object>
            {
                ["tileIndex"] = tileIndex,
                ["x"] = dict["X"],
                ["y"] = dict["Y"]
            });
        }

        result["tilemap"] = formattedTilemap;
        return result;
    }
}
