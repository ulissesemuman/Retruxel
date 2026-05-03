using System.Text.Json;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Modules.Graphics;

/// <summary>
/// Text Array module for storing multilingual string arrays.
/// All languages must have the same number of strings.
/// </summary>
public class TextArrayModule : IGraphicModule
{
    public string ModuleId => "text.array";
    public string DisplayName => "Text Array";
    public string Category => "Graphics";
    public ModuleType Type => ModuleType.Graphic;
    public bool IsSingleton => false;

    public ModuleScope DefaultScope => ModuleScope.Project;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;

    public string[] Compatibility { get; set; } = ["all"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private TextArrayState _state = new();

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<TextArrayState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new TextArrayState(), _jsonOptions);

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "name",
                DisplayName = "Array Name",
                Description = "C identifier for this text array (e.g., 'dialog', 'menu')",
                Type = ParameterType.String,
                DefaultValue = "strings",
                Required = true
            },
            new ParameterDefinition
            {
                Name = "fontAssetId",
                DisplayName = "Font Asset",
                Description = "Custom font asset (optional, uses default if empty)",
                Type = ParameterType.AssetReference,
                Required = false
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    private class TextArrayState
    {
        public string Name { get; set; } = "strings";
        public List<TextLanguage> Languages { get; set; } = new()
        {
            new TextLanguage { Code = "default", Strings = [""] }
        };
        public string? FontAssetId { get; set; } = null;
    }

    private class TextLanguage
    {
        public string Code { get; set; } = "default";
        public List<string> Strings { get; set; } = [];
    }
}
