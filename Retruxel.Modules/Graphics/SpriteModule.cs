using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Sprite module — loads sprite tile graphics into VRAM and provides
/// functions for managing the SMS Sprite Attribute Table (SAT).
///
/// Supports both simple sprites (single 8×8 tile) and metasprites (multiple tiles
/// with X/Y offsets). A simple sprite is just a metasprite with one frame containing
/// one tile at offset 0,0.
///
/// The SMS supports up to 64 sprites on screen (8 per scanline limit).
/// Sprites are 8×8 or 8×16 pixels depending on VDP mode.
///
/// JSON format:
/// {
///   "module":       "sprite",
///   "tilesAssetId": "player_tiles",   // asset ID for sprite CHR data
///   "startTile":    256,              // first VRAM sprite tile slot (256–511 recommended)
///   "doubleHeight": false,            // enable 8×16 sprite mode
///   "paletteRef":   "palette_abc123", // elementId of PaletteModule instance
///   "frames": [                       // animation frames (optional, defaults to single tile)
///     [
///       { "tileIndex": 0, "offsetX": 0, "offsetY": 0 },
///       { "tileIndex": 1, "offsetX": 8, "offsetY": 0 }
///     ]
///   ]
/// }
/// </summary>
public class SpriteModule : IGraphicModule
{
    public string ModuleId => "sprite";
    public string DisplayName => "Sprite";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Graphics;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = [];
    public string? VisualToolId => "sprite_editor";
    public ModuleScope DefaultScope => ModuleScope.Project;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private SpriteState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "2.0.0",
        Type = ModuleType.Graphics,
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "tilesAssetId",
                DisplayName  = "Sprite Tiles Asset",
                Description  = "Asset ID for the sprite CHR graphics data.",
                Type         = ParameterType.String,
                DefaultValue = string.Empty
            },
            new ParameterDefinition
            {
                Name         = "startTile",
                DisplayName  = "Start Tile",
                Description  = "First VRAM tile slot for sprite graphics (256–511 recommended).",
                Type         = ParameterType.Int,
                DefaultValue = 256,
                MinValue     = 0,
                MaxValue     = 511
            },
            new ParameterDefinition
            {
                Name         = "doubleHeight",
                DisplayName  = "Double Height (8×16)",
                Description  = "Enable 8×16 sprite mode. Each sprite entry uses two stacked 8×8 tiles.",
                Type         = ParameterType.Bool,
                DefaultValue = false
            },
            new ParameterDefinition
            {
                Name         = "paletteRef",
                DisplayName  = "Palette",
                Description  = "Palette module to use for this sprite.",
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
    /// Generates sprite assets.
    /// </summary>
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<SpriteState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new SpriteState(), _jsonOptions);

    private class SpriteState
    {
        public string TilesAssetId { get; set; } = string.Empty;
        public int StartTile { get; set; } = 256;
        public bool DoubleHeight { get; set; } = false;

        /// <summary>Palette reference — elementId of a PaletteModule in the same scene.</summary>
        public string PaletteRef { get; set; } = string.Empty;

        /// <summary>
        /// Animation frames. Each frame is a list of tile entries with X/Y offsets.
        /// A single-tile sprite has one frame with one entry at offset 0,0.
        /// Frame index matches the AnimationModule frame index for the same character.
        /// </summary>
        public List<List<SpriteTile>> Frames { get; set; } =
        [
            [new SpriteTile { TileIndex = 0, OffsetX = 0, OffsetY = 0 }]
        ];
    }

    private class SpriteTile
    {
        /// <summary>Tile index relative to StartTile.</summary>
        public int TileIndex { get; set; }

        /// <summary>Horizontal offset in pixels from the sprite origin.</summary>
        public int OffsetX { get; set; }

        /// <summary>Vertical offset in pixels from the sprite origin.</summary>
        public int OffsetY { get; set; }
    }
}
