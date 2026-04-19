



using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.TilePacker;

/// <summary>
/// NES wrapper for TilePacker.
/// Enables horizontal and vertical flip detection (PPU supports both).
/// </summary>
public class TilePackerNESTool : ITool
{
    public string ToolId => "retruxel.tool.tilepacker.nes";
    public string DisplayName => "Tile Packer (NES)";
    public string Description => "Tilemap optimizer for NES with H/V flip support";
    public object? Icon => null;
    public string Category => "Optimization";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => "nes";
    public bool RequiresProject => false;

    private readonly TilePackerTool _baseTool = new();

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // NES PPU supports horizontal and vertical flip
        input["enableFlipH"] = true;
        input["enableFlipV"] = true;
        input["enableRotation"] = false;

        var result = _baseTool.Execute(input);

        // Format tilemap for NES PPU
        var tilemap = (List<object>)result["tilemap"];
        var formattedTilemap = new List<Dictionary<string, object>>();

        foreach (var entry in tilemap)
        {
            var dict = (Dictionary<string, object>)entry;
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

        result["tilemap"] = formattedTilemap;
        return result;
    }
}
