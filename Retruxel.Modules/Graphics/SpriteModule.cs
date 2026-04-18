using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Sprite module — loads sprite tile graphics into VRAM and provides
/// functions for managing the SMS Sprite Attribute Table (SAT).
///
/// The SMS supports up to 64 sprites on screen (8 per scanline limit).
/// Sprites are 8×8 or 8×16 pixels depending on VDP mode.
/// Metasprites (larger characters) are built from multiple 8×8 sprites.
///
/// JSON format:
/// {
///   "module":       "sprite",
///   "tilesAssetId": "player_tiles",   // asset ID for sprite CHR data
///   "startTile":    256,              // first VRAM sprite tile slot (256–511 recommended)
///   "doubleHeight": false             // enable 8×16 sprite mode
/// }
/// </summary>
public class SpriteModule : IGraphicModule
{
    public string ModuleId => "sprite";
    public string DisplayName => "Sprite";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Logic;
    public bool IsSingleton => false;
    public string[] Compatibility { get; set; } = [];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private SpriteState _state = new();

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
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<SpriteState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new SpriteState(), _jsonOptions);

    private class SpriteState
    {
        public string TilesAssetId { get; set; } = string.Empty;
        public int StartTile { get; set; } = 256;
        public bool DoubleHeight { get; set; } = false;
    }
}
