using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Enemy entity module.
/// Handles positioning, movement pattern (Static/Patrol) and
/// AABB collision detection against the player entity.
/// Code generation is delegated to SmsEnemyCodeGen in Retruxel.Target.SMS.
/// </summary>
public class EnemyModule : ILogicModule
{
    public string     ModuleId     => "sms.enemy";
    public string     DisplayName  => "Enemy Entity";
    public string     Category     => "Entities";
    public ModuleType Type         => ModuleType.Logic;
    public string[]   Compatibility => ["sms"];

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private EnemyState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId      = ModuleId,
        Version       = "1.0.0",
        Type          = ModuleType.Logic,
        Compatibility = ["sms"],
        Parameters    =
        [
            new ParameterDefinition
            {
                Name         = "spriteId",
                DisplayName  = "Sprite",
                Description  = "Tile index of the enemy sprite in VRAM.",
                Type         = ParameterType.TileRef,
                DefaultValue = 1
            },
            new ParameterDefinition
            {
                Name         = "x",
                DisplayName  = "Initial X",
                Description  = "Initial horizontal position in pixels.",
                Type         = ParameterType.Int,
                DefaultValue = 120,
                MinValue     = 0,
                MaxValue     = 255
            },
            new ParameterDefinition
            {
                Name         = "y",
                DisplayName  = "Initial Y",
                Description  = "Initial vertical position in pixels.",
                Type         = ParameterType.Int,
                DefaultValue = 144,
                MinValue     = 0,
                MaxValue     = 191
            },
            new ParameterDefinition
            {
                Name         = "speed",
                DisplayName  = "Speed",
                Description  = "Movement speed in pixels per frame.",
                Type         = ParameterType.Int,
                DefaultValue = 1,
                MinValue     = 1,
                MaxValue     = 8
            },
            new ParameterDefinition
            {
                Name         = "health",
                DisplayName  = "Health",
                Description  = "Initial hit points.",
                Type         = ParameterType.Int,
                DefaultValue = 1,
                MinValue     = 1,
                MaxValue     = 16
            },
            new ParameterDefinition
            {
                Name         = "pattern",
                DisplayName  = "Pattern",
                Description  = "Movement pattern.",
                Type         = ParameterType.Enum,
                DefaultValue = "Patrol",
                EnumOptions  = new() { { "Static", "Static" }, { "Patrol", "Patrol" } }
            },
            new ParameterDefinition
            {
                Name         = "patrolRange",
                DisplayName  = "Patrol Range",
                Description  = "Horizontal distance in pixels the enemy patrols.",
                Type         = ParameterType.Int,
                DefaultValue = 64,
                MinValue     = 8,
                MaxValue     = 255
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()              => JsonSerializer.Serialize(_state, _jsonOptions);
    public void   Deserialize(string json) => _state = JsonSerializer.Deserialize<EnemyState>(json, _jsonOptions) ?? new();
    public string GetValidationSample()    => JsonSerializer.Serialize(new EnemyState(), _jsonOptions);

    private class EnemyState
    {
        public int    SpriteId    { get; set; } = 1;
        public int    X           { get; set; } = 120;
        public int    Y           { get; set; } = 144;
        public int    Speed       { get; set; } = 1;
        public int    Health      { get; set; } = 1;
        public string Pattern     { get; set; } = "Patrol";
        public int    PatrolRange { get; set; } = 64;
    }
}
