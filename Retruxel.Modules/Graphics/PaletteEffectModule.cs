using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Palette Effect module for runtime palette manipulation.
/// 
/// Provides effects like:
/// - Flash: Temporarily brighten/darken palette (damage, powerup)
/// - Fade: Gradual transition between palettes (scene transitions)
/// - Cycle: Rotate palette entries (water animation, lava)
/// - Pulse: Rhythmic color changes (warning indicators)
/// 
/// Unlike the obsolete PaletteModule, this is a logic module that modifies
/// existing scene palette slots at runtime, not a configuration module.
/// 
/// This module is arrastável (draggable) because it represents game logic,
/// not hardware configuration.
/// </summary>
public class PaletteEffectModule : ILogicModule
{
    public string ModuleId => "palette.effect";
    public string DisplayName => "Palette Effect";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Logic;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = ["all"];
    public ModuleScope DefaultScope => ModuleScope.Project;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private PaletteEffectState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "effectType",
                DisplayName = "Effect Type",
                Description = "Type of palette effect to apply",
                Type = ParameterType.Enum,
                DefaultValue = "flash",
                EnumOptions = new Dictionary<string, string>
                {
                    ["Flash"] = "flash",
                    ["Fade"] = "fade",
                    ["Cycle"] = "cycle",
                    ["Pulse"] = "pulse"
                }
            },
            new ParameterDefinition
            {
                Name = "targetSlot",
                DisplayName = "Target Slot",
                Description = "Which palette slot to affect (0 = Background, 1 = Sprite)",
                Type = ParameterType.Int,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 1
            },
            new ParameterDefinition
            {
                Name = "duration",
                DisplayName = "Duration (frames)",
                Description = "How long the effect lasts (60 frames = 1 second)",
                Type = ParameterType.Int,
                DefaultValue = 30,
                MinValue = 1,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "intensity",
                DisplayName = "Intensity",
                Description = "Effect strength (0-255)",
                Type = ParameterType.Int,
                DefaultValue = 128,
                MinValue = 0,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "triggerVar",
                DisplayName = "Trigger Variable",
                Description = "GameVar that triggers this effect when non-zero",
                Type = ParameterType.String,
                Required = false
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<PaletteEffectState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new PaletteEffectState(), _jsonOptions);

    private class PaletteEffectState
    {
        public string EffectType { get; set; } = "flash";
        public int TargetSlot { get; set; } = 0;
        public int Duration { get; set; } = 30;
        public int Intensity { get; set; } = 128;
        public string? TriggerVar { get; set; }
    }
}
