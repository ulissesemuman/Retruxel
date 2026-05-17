using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = [];
    public string? VisualToolId => "tilemap_editor";
    public ModuleScope DefaultScope => ModuleScope.Scene;

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
            },
            new ParameterDefinition
            {
                Name         = "paletteSlot",
                DisplayName  = "Palette Slot",
                Description  = "Scene palette slot to use (0 = Background, 1 = Sprite).",
                Type         = ParameterType.Int,
                DefaultValue = 0,
                MinValue     = 0,
                MaxValue     = 1
            },
            new ParameterDefinition
            {
                Name         = "paletteRef",
                DisplayName  = "Palette",
                Description  = "Palette module to use for this tilemap.",
                Type         = ParameterType.ModuleReference,
                ModuleFilter = "palette",
                Required     = true
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

    public string Serialize()
    {
        var json = JsonSerializer.Serialize(_state, _jsonOptions);

        // DEBUG: Log serialized JSON
        System.Diagnostics.Debug.WriteLine($"[TilemapModule.Serialize] MapData.Length = {_state.MapData.Length}");
        if (_state.MapData.Length > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[TilemapModule.Serialize] First item type: {_state.MapData[0]?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[TilemapModule.Serialize] JSON length: {json.Length}");
            System.Diagnostics.Debug.WriteLine($"[TilemapModule.Serialize] JSON preview: {json.Substring(0, Math.Min(500, json.Length))}");
        }

        return json;
    }

    public void Deserialize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        _state = new TilemapState
        {
            TilesAssetId = root.TryGetProperty("tilesAssetId", out var ta) ? ta.GetString() ?? "" : "",
            MapAssetId = root.TryGetProperty("mapAssetId", out var ma) ? ma.GetString() ?? "" : "",
            StartTile = root.TryGetProperty("startTile", out var st) ? st.GetInt32() : 0,
            MapX = root.TryGetProperty("mapX", out var mx) ? mx.GetInt32() : 0,
            MapY = root.TryGetProperty("mapY", out var my) ? my.GetInt32() : 0,
            MapWidth = root.TryGetProperty("mapWidth", out var mw) ? mw.GetInt32() : 32,
            MapHeight = root.TryGetProperty("mapHeight", out var mh) ? mh.GetInt32() : 24,
            PaletteSlot = root.TryGetProperty("paletteSlot", out var ps) ? ps.GetInt32() : 0,
            PaletteRef = root.TryGetProperty("paletteRef", out var pr) ? pr.GetString() ?? "" : "",
            SolidTiles = root.TryGetProperty("solidTiles", out var solid) && solid.ValueKind == JsonValueKind.Array
                ? solid.EnumerateArray().Select(e => e.GetInt32()).ToArray()
                : Array.Empty<int>(),
            MapData = ParseMapData(root)
        };
    }

    private static object[] ParseMapData(JsonElement root)
    {
        if (!root.TryGetProperty("mapData", out var mapDataProp))
            return Array.Empty<object>();

        if (mapDataProp.ValueKind != JsonValueKind.Array)
            return Array.Empty<object>();

        var result = new List<object>();

        foreach (var item in mapDataProp.EnumerateArray())
        {
            // New format: object with tileIndex, flipH, flipV, rotation
            if (item.ValueKind == JsonValueKind.Object)
            {
                var entry = new Dictionary<string, object>();

                if (item.TryGetProperty("tileIndex", out var ti))
                    entry["tileIndex"] = ti.GetInt32();
                else
                    entry["tileIndex"] = -1;

                if (item.TryGetProperty("flipH", out var fh))
                    entry["flipH"] = fh.GetBoolean();
                else
                    entry["flipH"] = false;

                if (item.TryGetProperty("flipV", out var fv))
                    entry["flipV"] = fv.GetBoolean();
                else
                    entry["flipV"] = false;

                if (item.TryGetProperty("rotation", out var rot))
                    entry["rotation"] = rot.GetInt32();
                else
                    entry["rotation"] = 0;

                result.Add(entry);
            }
            // Old format: plain integer (convert to object)
            else if (item.ValueKind == JsonValueKind.Number)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["tileIndex"] = item.GetInt32(),
                    ["flipH"] = false,
                    ["flipV"] = false,
                    ["rotation"] = 0
                });
            }
        }

        return result.ToArray();
    }
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
        public object[] MapData { get; set; } = []; // Array of objects with tileIndex, flipH, flipV, rotation
        public int[] SolidTiles { get; set; } = [];
        public int PaletteSlot { get; set; } = 0;

        /// <summary>Palette reference — elementId of a PaletteModule in the same scene.</summary>
        public string PaletteRef { get; set; } = string.Empty;
    }
}
