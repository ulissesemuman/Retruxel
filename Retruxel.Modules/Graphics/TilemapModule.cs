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
///   "module":       "tilemap",
///   "tilesAssetId": "bg_tiles",       // asset ID for CHR tile data
///   "mapAssetId":   "bg_map",         // asset ID for nametable layout
///   "startTile":    0,                // first VRAM tile slot to load into (0–447)
///   "mapX":         0,                // nametable destination X (tile units)
///   "mapY":         0,                // nametable destination Y (tile units)
///   "mapWidth":     32,               // width of the map in tiles
///   "mapHeight":    24                // height of the map in tiles
/// }
/// </summary>
public class TilemapModule : IGraphicModule
{
    public string ModuleId => "tilemap";
    public string DisplayName => "Tilemap";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Logic;
    public bool IsSingleton => false;
    public string[] Compatibility { get; set; } = [];
    public string? VisualToolId => "tilemap_editor";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private TilemapState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "tilesAssetId",
                DisplayName  = "Tiles Asset",
                Description  = "Asset ID for the tile CHR graphics data.",
                Type         = ParameterType.String,
                DefaultValue = string.Empty
            },
            new ParameterDefinition
            {
                Name         = "mapAssetId",
                DisplayName  = "Map Asset",
                Description  = "Asset ID for the nametable layout data.",
                Type         = ParameterType.String,
                DefaultValue = string.Empty
            },
            new ParameterDefinition
            {
                Name         = "startTile",
                DisplayName  = "Start Tile",
                Description  = "First VRAM tile slot to load graphics into (0–447).",
                Type         = ParameterType.Int,
                DefaultValue = 0,
                MinValue     = 0,
                MaxValue     = 447
            },
            new ParameterDefinition
            {
                Name         = "mapX",
                DisplayName  = "Map X",
                Description  = "Nametable destination X in tile units.",
                Type         = ParameterType.Int,
                DefaultValue = 0,
                MinValue     = 0,
                MaxValue     = 31
            },
            new ParameterDefinition
            {
                Name         = "mapY",
                DisplayName  = "Map Y",
                Description  = "Nametable destination Y in tile units.",
                Type         = ParameterType.Int,
                DefaultValue = 0,
                MinValue     = 0,
                MaxValue     = 23
            },
            new ParameterDefinition
            {
                Name         = "mapWidth",
                DisplayName  = "Map Width",
                Description  = "Width of the map in tiles (max 32).",
                Type         = ParameterType.Int,
                DefaultValue = 32,
                MinValue     = 1,
                MaxValue     = 32
            },
            new ParameterDefinition
            {
                Name         = "mapHeight",
                DisplayName  = "Map Height",
                Description  = "Height of the map in tiles (max 24 visible).",
                Type         = ParameterType.Int,
                DefaultValue = 24,
                MinValue     = 1,
                MaxValue     = 28
            },
            new ParameterDefinition
            {
                Name         = "mapData",
                DisplayName  = "Map Data",
                Description  = "Tile indices for the map layout.",
                Type         = ParameterType.IntArray,
                DefaultValue = Array.Empty<int>()
            },
            new ParameterDefinition
            {
                Name         = "solidTiles",
                DisplayName  = "Solid Tiles",
                Description  = "List of tile indices that are solid for collision.",
                Type         = ParameterType.IntArray,
                DefaultValue = Array.Empty<int>()
            }
        ]
    };

    /// <summary>
    /// Creates the ViewModel for the property editor.
    /// Returns null as this module uses auto-generated UI from manifest.
    /// </summary>
    public object CreateEditorViewModel() => null!;

    /// <summary>
    /// Generates font tiles as assets.
    /// Each character in the text becomes a tile in the asset.
    /// </summary>
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<TilemapState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new TilemapState(), _jsonOptions);

    private class TilemapState
    {
        public string TilesAssetId { get; set; } = string.Empty;
        public string MapAssetId { get; set; } = string.Empty;
        public int StartTile { get; set; } = 0;
        public int MapX { get; set; } = 0;
        public int MapY { get; set; } = 0;
        public int MapWidth { get; set; } = 32;
        public int MapHeight { get; set; } = 24;
        public int[] MapData { get; set; } = [];
        public int[] SolidTiles { get; set; } = [];
    }
}
