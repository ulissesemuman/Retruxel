using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Enables SMS VDP Shadow/Highlight mode.
/// Expands the effective palette from 64 to 192 colors by allowing sprites
/// to darken (0x3E) or lighten (0x3F) background tiles behind them.
/// </summary>
public class ShadowHighlightModule : IGraphicModule
{
    public string ModuleId    => "shadow_highlight";
    public string DisplayName => "Shadow/Highlight";
    public string Category    => "Effects";
    public ModuleType Type    => ModuleType.Graphic;
    public string[] Compatibility { get; set; } = ["sms", "gg"];
    public SingletonPolicy SingletonPolicy => SingletonPolicy.SingletonPerScene;
    public ModuleScope DefaultScope => ModuleScope.Scene;

    private ShadowHighlightState _state = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version  = "1.0.0",
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "enabled",
                DisplayName  = "Enable Shadow/Highlight",
                Description  = "Activates VDP Shadow/Highlight mode. Sprite color 0x3E = shadow, 0x3F = highlight.",
                Type         = ParameterType.Bool,
                DefaultValue = true
            }
        ]
    };

    public string Serialize()   => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) =>
        _state = JsonSerializer.Deserialize<ShadowHighlightState>(json, _jsonOptions) ?? new();

    public string GetValidationSample() => "{}";
    public IEnumerable<GeneratedFile> GenerateCode()   => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    private class ShadowHighlightState
    {
        public bool Enabled { get; set; } = true;
    }
}
