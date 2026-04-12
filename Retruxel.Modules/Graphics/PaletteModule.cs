using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Palette module — loads BG and sprite color palettes into the VDP.
/// The SMS VDP supports 2 palettes of 16 colors each.
/// Each color is stored as a 6-bit SMS register value (2 bits per R/G/B channel).
///
/// SMS color format byte: 0b00BBGGRR
///   RR = red   (0-3 → 0, 85, 170, 255)
///   GG = green (0-3 → 0, 85, 170, 255)
///   BB = blue  (0-3 → 0, 85, 170, 255)
///
/// JSON format:
/// {
///   "module": "sms.palette",
///   "bgColors":     [0,0,0,21,42,63, ...],   // 16 SMS color bytes for BG
///   "spriteColors": [0,0,0,21,42,63, ...]    // 16 SMS color bytes for sprites
/// }
/// </summary>
public class PaletteModule : IGraphicModule
{
    public string     ModuleId     => "sms.palette";
    public string     DisplayName  => "Palette";
    public string     Category     => "Graphics";
    public ModuleType Type         => ModuleType.Logic;
    public bool     IsSingleton  => false;
    public string[]   Compatibility => ["sms", "gg"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private PaletteState _state = new();

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
                Name         = "bgColors",
                DisplayName  = "BG Palette",
                Description  = "16 SMS color register values (0x00–0x3F) for background tiles.",
                Type         = ParameterType.String,
                DefaultValue = "0,63,0,0,0,0,0,0,0,0,0,0,0,0,0,0"
            },
            new ParameterDefinition
            {
                Name         = "spriteColors",
                DisplayName  = "Sprite Palette",
                Description  = "16 SMS color register values (0x00–0x3F) for sprites.",
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
    /// Generates font tiles as assets.
    /// Each character in the text becomes a tile in the asset.
    /// </summary>
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()              => JsonSerializer.Serialize(_state, _jsonOptions);
    public void   Deserialize(string json) => _state = JsonSerializer.Deserialize<PaletteState>(json, _jsonOptions) ?? new();
    public string GetValidationSample()    => JsonSerializer.Serialize(new PaletteState(), _jsonOptions);

    private class PaletteState
    {
        // Default: black background (0x00), white foreground (0x3F), remaining black
        public byte[] BgColors     { get; set; } = [0x00, 0x3F, 0,0,0,0,0,0,0,0,0,0,0,0,0,0];
        public byte[] SpriteColors { get; set; } = [0x00, 0x3F, 0,0,0,0,0,0,0,0,0,0,0,0,0,0];
    }
}
