using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Defines a global game variable that persists across scenes.
/// Used for score, lives, timer, flags, etc.
/// Each instance generates a global variable declaration.
/// </summary>
public class GameVarModule : ILogicModule
{
    public string ModuleId => "gamevar";
    public string DisplayName => "Game Variable";
    public string Description => "Global variable that persists across scenes (score, lives, flags)";
    public string Category => "Logic";
    public ModuleType Type => ModuleType.Logic;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = [];
    public ModuleScope DefaultScope => ModuleScope.Project;

    private GameVarState _state = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Parameters =
        [
            new ParameterDefinition
            {
                Name = "name",
                DisplayName = "Variable Name",
                Description = "C identifier for this variable (e.g., score, lives, timer)",
                Type = ParameterType.String,
                DefaultValue = "myVar",
                Required = true
            },
            new ParameterDefinition
            {
                Name = "type",
                DisplayName = "Type",
                Description = "Data type of the variable",
                Type = ParameterType.Enum,
                EnumOptions = new Dictionary<string, string>
                {
                    { "int", "int" },
                    { "byte", "byte" },
                    { "bool", "bool" }
                },
                DefaultValue = "int",
                Required = true
            },
            new ParameterDefinition
            {
                Name = "initialValue",
                DisplayName = "Initial Value",
                Description = "Starting value when game begins",
                Type = ParameterType.String,
                DefaultValue = "0",
                Required = true
            },
            new ParameterDefinition
            {
                Name = "showInHud",
                DisplayName = "Show in HUD",
                Description = "Display this variable in the HUD (future feature)",
                Type = ParameterType.Bool,
                DefaultValue = false
            }
        ]
    };

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);

    public void Deserialize(string json) =>
        _state = JsonSerializer.Deserialize<GameVarState>(json, _jsonOptions) ?? new();

    public string GetValidationSample() => "{}";

    public IEnumerable<GeneratedFile> GenerateCode() => [];
    public IEnumerable<GeneratedAsset> GenerateAssets() => [];

    private class GameVarState
    {
        public string Name { get; set; } = "myVar";
        public string Type { get; set; } = "int";
        public string InitialValue { get; set; } = "0";
        public bool ShowInHud { get; set; } = false;
    }
}
