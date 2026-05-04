using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// OBSOLETE: Palette module is deprecated in favor of scene-level palette slots.
/// 
/// In the new architecture:
/// - Palettes are scene-level properties (SceneData.PaletteSlots)
/// - Each scene has fixed slots defined by target (e.g., SMS: 2 slots of 16 colors)
/// - Palette data is generated directly in scene_X.c by scene/sms/codegen.json
/// - No separate palette_0.c/h files are generated
/// 
/// This module is kept for backward compatibility with old projects only.
/// New projects should not use this module.
/// 
/// For runtime palette effects (flash, fade, cycle), use PaletteEffectModule instead.
/// </summary>
[Obsolete("Use scene-level palette slots instead. This module is kept for backward compatibility only.")]
public class PaletteModule : IGraphicModule
{
    public string ModuleId => "palette";
    public string DisplayName => "Palette (Obsolete)";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Logic;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = [];
    public string? VisualToolId => null; // Disabled - use scene palette slots
    public ModuleScope DefaultScope => ModuleScope.Scene;

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
        Parameters = []
    };

    public object? CreateEditorViewModel() => null;
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
