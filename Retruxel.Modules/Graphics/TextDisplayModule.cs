using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Universal text display module.
/// Displays a string at a given tile position on screen.
/// Compatible with all targets — code generation is delegated
/// to each target's specific code generator.
///
/// JSON format:
/// {
///   "module": "text.display",
///   "x": 5,
///   "y": 10,
///   "text": "HELLO WORLD"
/// }
/// </summary>
public class TextDisplayModule : IGraphicModule
{
    public string ModuleId => "text.display";
    public string DisplayName => "Text Display";
    public string Category => "Output";
    public ModuleType Type => ModuleType.Graphics;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = [];
    public ModuleScope DefaultScope => ModuleScope.Scene;

    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public string Text { get; set; } = "";

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Graphics,
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "x",
                DisplayName = "X Position",
                Description = "Horizontal tile position. Target validator enforces hardware limits.",
                Type = ParameterType.Int,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "y",
                DisplayName = "Y Position",
                Description = "Vertical tile position. Target validator enforces hardware limits.",
                Type = ParameterType.Int,
                DefaultValue = 0,
                MinValue = 0,
                MaxValue = 255
            },
            new ParameterDefinition
            {
                Name = "text",
                DisplayName = "Text",
                Description = "Text string to display on screen.",
                Type = ParameterType.String,
                DefaultValue = ""
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

    /// <summary>
    /// Code generation is delegated to the target-specific code generator.
    /// This module only defines the data — the target translates it to C.
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()
    {
        var data = new { module = ModuleId, x = X, y = Y, text = Text };
        return JsonSerializer.Serialize(data);
    }

    public void Deserialize(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("x", out var x)) X = x.GetInt32();
        if (root.TryGetProperty("y", out var y)) Y = y.GetInt32();
        if (root.TryGetProperty("text", out var t)) Text = t.GetString() ?? "";
    }

    public string GetValidationSample() => Serialize();
}