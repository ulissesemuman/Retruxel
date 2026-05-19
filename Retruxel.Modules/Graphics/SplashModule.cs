using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Splash screen module - displays a logo with fade in/out effect.
/// 
/// This module orchestrates:
/// 1. fade_in - gradually reveal the logo
/// 2. pause - hold the logo visible
/// 3. fade_out - gradually hide the logo
/// 
/// The splash uses a tilemap with an asset provided by the target
/// (via ITarget.GetSplashAsset()). The target can embed the asset
/// in its assembly for a "Made with Retruxel" logo.
/// 
/// Implementation is target-specific via codegens.
/// </summary>
public class SplashModule : ILogicModule
{
    public string ModuleId => "splash";
    public string DisplayName => "Splash Screen";
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

    private SplashState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "fadeInDuration",
                DisplayName = "Fade In Duration",
                Description = "Frames for fade-in effect (60 = 1 second)",
                Type = ParameterType.Int,
                DefaultValue = 30,
                MinValue = 1,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "holdDuration",
                DisplayName = "Hold Duration",
                Description = "Frames to hold logo visible (60 = 1 second)",
                Type = ParameterType.Int,
                DefaultValue = 120,
                MinValue = 1,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "fadeOutDuration",
                DisplayName = "Fade Out Duration",
                Description = "Frames for fade-out effect (60 = 1 second)",
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
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<SplashState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new SplashState(), _jsonOptions);

    private class SplashState
    {
        public int FadeInDuration { get; set; } = 30;
        public int HoldDuration { get; set; } = 120;
        public int FadeOutDuration { get; set; } = 30;
        public int TargetSlot { get; set; } = 0;
    }
}
