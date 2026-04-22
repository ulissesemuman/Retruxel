using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Metasprite module — defines a logical sprite built from multiple 8×8 hardware tiles.
///
/// ⚠️ OBSOLETE: Use SpriteModule v2.0.0 with Frames property instead.
/// This module is kept for backward compatibility only.
///
/// Required for characters larger than 8×8 pixels, like Kung Fu Master's player (16×24)
/// and enemies. Each animation frame lists the hardware tiles and their X/Y offsets
/// relative to the character's origin point.
///
/// The MetaspritePreprocessorTool flattens this into C arrays that the generated
/// code uses to call SMS_addSprite() once per tile entry every frame.
///
/// JSON format:
/// {
///   "module":      "metasprite",
///   "startTile":   256,
///   "frames": [
///     [
///       { "tileIndex": 0, "offsetX": 0,  "offsetY": 0  },
///       { "tileIndex": 1, "offsetX": 8,  "offsetY": 0  },
///       { "tileIndex": 2, "offsetX": 0,  "offsetY": 8  },
///       { "tileIndex": 3, "offsetX": 8,  "offsetY": 8  }
///     ],
///     ...
///   ]
/// }
/// </summary>
[Obsolete("Use SpriteModule v2.0.0 with Frames property instead. This module will be removed in v0.8.0.")]
public class MetaspriteModule : IGraphicModule
{
    public string ModuleId    => "metasprite";
    public string DisplayName => "Metasprite";
    public string Category    => "Graphics";
    public ModuleType Type    => ModuleType.Graphics;
    public bool IsSingleton   => false;
    public string[] Compatibility { get; set; } = [];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private MetaspriteState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version  = "1.0.0",
        Type     = ModuleType.Graphics,
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "startTile",
                DisplayName  = "Start Tile",
                Description  = "First VRAM tile slot for this metasprite's graphics (0–511).",
                Type         = ParameterType.Int,
                DefaultValue = 256,
                MinValue     = 0,
                MaxValue     = 511
            },
            new ParameterDefinition
            {
                Name         = "tilesAssetId",
                DisplayName  = "Tiles Asset",
                Description  = "Asset ID for the sprite tile graphics data.",
                Type         = ParameterType.String,
                DefaultValue = string.Empty
            }
        ]
    };

    public object CreateEditorViewModel() => null!;

    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()              => JsonSerializer.Serialize(_state, _jsonOptions);
    public void   Deserialize(string json) => _state = JsonSerializer.Deserialize<MetaspriteState>(json, _jsonOptions) ?? new();
    public string GetValidationSample()    => JsonSerializer.Serialize(new MetaspriteState(), _jsonOptions);

    private class MetaspriteState
    {
        public string TilesAssetId { get; set; } = string.Empty;

        /// <summary>First VRAM tile slot to load graphics into.</summary>
        public int StartTile { get; set; } = 256;

        /// <summary>
        /// Animation frames. Each frame is a list of tile entries with X/Y offsets.
        /// Frame index matches the AnimationModule frame index for the same character.
        /// </summary>
        public List<List<MetaspriteTile>> Frames { get; set; } =
        [
            // Default: single 8×8 tile at origin — replace with real frame data
            [new MetaspriteTile { TileIndex = 0, OffsetX = 0, OffsetY = 0 }]
        ];
    }

    private class MetaspriteTile
    {
        /// <summary>Tile index relative to StartTile.</summary>
        public int TileIndex { get; set; }

        /// <summary>Horizontal offset in pixels from the metasprite origin.</summary>
        public int OffsetX { get; set; }

        /// <summary>Vertical offset in pixels from the metasprite origin.</summary>
        public int OffsetY { get; set; }
    }
}
