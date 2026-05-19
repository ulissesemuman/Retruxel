using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Internal module for palette fade-in effect.
/// Used by splash screen and potentially other transitions.
/// 
/// This module is hidden from the UI - it's only used internally
/// by the splash module or future transition effects.
/// 
/// Implementation is target-specific via codegens.
/// </summary>
public class FadeInModule : ILogicModule
{
    public string ModuleId => "fade.in";
    public string DisplayName => "Fade In (Internal)";
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

    private FadeInState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "duration",
                DisplayName = "Duration (frames)",
                Description = "How many frames for the fade effect (60 = 1 second)",
                Type = ParameterType.Int,
                DefaultValue = 30,
                MinValue = 1,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "targetSlot",
                DisplayName = "Target Slot",
                Description = "Which palette slot to fade (0 = Background, 1 = Sprite)",
                Type = ParameterType.Int,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 1
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<FadeInState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new FadeInState(), _jsonOptions);

    private class FadeInState
    {
        public int Duration { get; set; } = 30;
        public int TargetSlot { get; set; } = 0;
    }
}
