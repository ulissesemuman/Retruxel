using Retruxel.Core.Interfaces;
using System.Text.Json;

namespace Retruxel.Core.Models;

/// <summary>
/// Dynamic module created from ICodeGenPlugin metadata when ModuleId is null.
/// Represents target-specific modules that don't have a universal parent module.
/// </summary>
public class TargetModule : ILogicModule
{
    public string ModuleId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public ModuleType Type => ModuleType.Logic;
    public bool IsSingleton { get; init; } = false;
    public string[] Compatibility { get; init; } = Array.Empty<string>();

    private Dictionary<string, object> _state = new();

    public string Serialize() => JsonSerializer.Serialize(_state);

    public void Deserialize(string json)
        => _state = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

    public string GetValidationSample() => "{}";

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Compatibility = Compatibility,
        Dependencies = Array.Empty<string>(),
        Parameters = Array.Empty<ParameterDefinition>(),
        ExposedFunctions = Array.Empty<string>()
    };

    public IEnumerable<GeneratedFile> GenerateCode() => Array.Empty<GeneratedFile>();
    public IEnumerable<GeneratedAsset> GenerateAssets() => Array.Empty<GeneratedAsset>();

}
