using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Tilemap module — loads tile graphics and a nametable layout into the SMS VDP.
///
/// The SMS background is a 32×28 grid of 8×8 tiles (only 32×24 visible in Mode 4).
/// This module references asset IDs that the Asset Manager will resolve to binary data.
///
/// JSON format:
/// {
///   "module":       "sms.tilemap",
///   "tilesAssetId": "bg_tiles",       // asset ID for CHR tile data
///   "mapAssetId":   "bg_map",         // asset ID for nametable layout
///   "startTile":    0,                // first VRAM tile slot to load into (0–447)
///   "mapX":         0,                // nametable destination X (tile units)
///   "mapY":         0,                // nametable destination Y (tile units)
///   "mapWidth":     32,               // width of the map in tiles
///   "mapHeight":    24                // height of the map in tiles
/// }
/// </summary>
public class TilemapModule : ILogicModule
{
    public string ModuleId    => "sms.tilemap";
    public string DisplayName => "Tilemap";
    public string Category    => "Graphics";
    public ModuleType Type    => ModuleType.Logic;
    public string[] Compatibility => ["sms", "gg"];

    /// <summary>Asset ID for the tile CHR data (8×8 pixel tiles, 4bpp planar).</summary>
    public string TilesAssetId { get; set; } = string.Empty;

    /// <summary>Asset ID for the nametable map (array of tile indices).</summary>
    public string MapAssetId { get; set; } = string.Empty;

    /// <summary>First VRAM tile slot to load tile graphics into (0–447 for SMS).</summary>
    public int StartTile { get; set; } = 0;

    /// <summary>Nametable destination X in tile units.</summary>
    public int MapX { get; set; } = 0;

    /// <summary>Nametable destination Y in tile units.</summary>
    public int MapY { get; set; } = 0;

    /// <summary>Width of the map area in tiles (max 32).</summary>
    public int MapWidth { get; set; } = 32;

    /// <summary>Height of the map area in tiles (max 24 visible on SMS).</summary>
    public int MapHeight { get; set; } = 24;

    public ModuleManifest GetManifest() => new()
    {
        ModuleId      = ModuleId,
        Version       = "1.0.0",
        Type          = ModuleType.Logic,
        Compatibility = Compatibility,
        Parameters    =
        [
            new ParameterDefinition
            {
                Name        = "tilesAssetId",
                DisplayName = "Tiles Asset",
                Description = "Asset ID for the tile CHR graphics data.",
                Type        = ParameterType.String,
                DefaultValue = string.Empty
            },
            new ParameterDefinition
            {
                Name        = "mapAssetId",
                DisplayName = "Map Asset",
                Description = "Asset ID for the nametable layout data.",
                Type        = ParameterType.String,
                DefaultValue = string.Empty
            },
            new ParameterDefinition
            {
                Name        = "startTile",
                DisplayName = "Start Tile",
                Description = "First VRAM tile slot to load graphics into (0–447).",
                Type        = ParameterType.Int,
                DefaultValue = 0,
                MinValue    = 0,
                MaxValue    = 447
            },
            new ParameterDefinition
            {
                Name        = "mapX",
                DisplayName = "Map X",
                Description = "Nametable destination X in tile units.",
                Type        = ParameterType.Int,
                DefaultValue = 0,
                MinValue    = 0,
                MaxValue    = 31
            },
            new ParameterDefinition
            {
                Name        = "mapY",
                DisplayName = "Map Y",
                Description = "Nametable destination Y in tile units.",
                Type        = ParameterType.Int,
                DefaultValue = 0,
                MinValue    = 0,
                MaxValue    = 23
            },
            new ParameterDefinition
            {
                Name        = "mapWidth",
                DisplayName = "Map Width",
                Description = "Width of the map in tiles (max 32).",
                Type        = ParameterType.Int,
                DefaultValue = 32,
                MinValue    = 1,
                MaxValue    = 32
            },
            new ParameterDefinition
            {
                Name        = "mapHeight",
                DisplayName = "Map Height",
                Description = "Height of the map in tiles (max 24 visible).",
                Type        = ParameterType.Int,
                DefaultValue = 24,
                MinValue    = 1,
                MaxValue    = 28
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()
    {
        var data = new
        {
            module       = ModuleId,
            tilesAssetId = TilesAssetId,
            mapAssetId   = MapAssetId,
            startTile    = StartTile,
            mapX         = MapX,
            mapY         = MapY,
            mapWidth     = MapWidth,
            mapHeight    = MapHeight
        };
        return JsonSerializer.Serialize(data);
    }

    public void Deserialize(string json)
    {
        var doc  = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("tilesAssetId", out var ta)) TilesAssetId = ta.GetString() ?? string.Empty;
        if (root.TryGetProperty("mapAssetId",   out var ma)) MapAssetId   = ma.GetString() ?? string.Empty;
        if (root.TryGetProperty("startTile",    out var st)) StartTile    = st.GetInt32();
        if (root.TryGetProperty("mapX",         out var mx)) MapX         = mx.GetInt32();
        if (root.TryGetProperty("mapY",         out var my)) MapY         = my.GetInt32();
        if (root.TryGetProperty("mapWidth",     out var mw)) MapWidth     = mw.GetInt32();
        if (root.TryGetProperty("mapHeight",    out var mh)) MapHeight    = mh.GetInt32();
    }

    public string GetValidationSample() => Serialize();
}
