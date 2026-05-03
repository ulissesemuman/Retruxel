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
    public string ModuleId => "enemy";
    public string DisplayName => "Enemy Entity";
    public string Category => "Entities";
    public ModuleType Type => ModuleType.Logic;
    public SingletonPolicy SingletonPolicy => SingletonPolicy.Multiple;
    public string[] Compatibility { get; set; } = [];
    public ModuleScope DefaultScope => ModuleScope.Scene;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private EnemyState _state = new();

    public ModuleManifest GetManifest() => new()
    {
        ModuleId = ModuleId,
        Version = "1.0.0",
        Type = ModuleType.Logic,
        Parameters =
        [
            new ParameterDefinition
            {
                Name         = "spriteId",
                DisplayName  = "Sprite (Legacy)",
                Description  = "[Obsolete] Tile index of the enemy sprite in VRAM. Use spriteRef instead.",
                Type         = ParameterType.TileRef,
                DefaultValue = 1
            },
            new ParameterDefinition
            {
                Name         = "spriteRef",
                DisplayName  = "Sprite",
                Description  = "Sprite module to render for this enemy.",
                Type         = ParameterType.ModuleReference,
                ModuleFilter = "sprite",
                Required     = true
            },
            new ParameterDefinition
            {
                Name         = "animationRef",
                DisplayName  = "Animation",
                Description  = "Animation module to drive this enemy's frames (optional).",
                Type         = ParameterType.ModuleReference,
                ModuleFilter = "animation",
                Required     = false
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

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize() => JsonSerializer.Serialize(_state, _jsonOptions);
    public void Deserialize(string json) => _state = JsonSerializer.Deserialize<EnemyState>(json, _jsonOptions) ?? new();
    public string GetValidationSample() => JsonSerializer.Serialize(new EnemyState(), _jsonOptions);

    private class EnemyState
    {
        [Obsolete("Use SpriteRef instead. Kept for backward compatibility.")]
        public int SpriteId { get; set; } = 1;

        /// <summary>Reference to a SpriteModule instance — replaces the raw SpriteId int.</summary>
        public string SpriteRef { get; set; } = string.Empty;

        /// <summary>Reference to an AnimationModule instance (optional).</summary>
        public string AnimationRef { get; set; } = string.Empty;

        public int X { get; set; } = 120;
        public int Y { get; set; } = 144;
        public int Speed { get; set; } = 1;
        public int Health { get; set; } = 1;
        public string Pattern { get; set; } = "Patrol";
        public int PatrolRange { get; set; } = 64;
    }
}
