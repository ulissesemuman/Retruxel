using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Palette module — loads color palettes for background and sprites.
/// Target-specific implementations handle the actual palette format and size.
///
/// JSON format:
/// {
///   "module": "palette",
///   "bgColors":     [0,0,0,21,42,63, ...],   // Color values for BG
///   "spriteColors": [0,0,0,21,42,63, ...]    // Color values for sprites
/// }
/// </summary>
public class PaletteModule : IGraphicModule
{
    public string ModuleId => "palette";
    public string DisplayName => "Palette";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Logic;
    public bool IsSingleton => false;
    public string[] Compatibility { get; set; } = [];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private PaletteState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "bgColors",
                DisplayName  = "BG Palette",
                Description  = "Color values for background tiles. Format and size depend on target platform.",
                Type         = ParameterType.String,
                DefaultValue = "0,63,0,0,0,0,0,0,0,0,0,0,0,0,0,0"
            },
            new ParameterDefinition
            {
                Name         = "spriteColors",
                DisplayName  = "Sprite Palette",
                Description  = "Color values for sprites. Format and size depend on target platform.",
                Type         = ParameterType.String,
                DefaultValue = "0,63,0,0,0,0,0,0,0,0,0,0,0,0,0,0"
            }
        ]
    };

    /// <summary>
    /// Creates the ViewModel for the property editor.
    /// Returns null as this module uses auto-generated UI from manifest.
    /// </summary>
    public object CreateEditorViewModel() => null!;

    /// <summary>
    /// Generates palette data as assets.
    /// </summary>
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<PaletteState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new PaletteState(), _jsonOptions);

    private class PaletteState
    {
        public byte[] BgColors { get; set; } = [0x00, 0x3F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        public byte[] SpriteColors { get; set; } = [0x00, 0x3F, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    }
}
