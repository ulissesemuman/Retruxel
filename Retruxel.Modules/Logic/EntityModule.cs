using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Player entity module.
/// Handles initial positioning, sprite assignment and per-frame movement.
/// Code generation is delegated to SmsEntityCodeGen in Retruxel.Target.SMS.
/// </summary>
public class EntityModule : ILogicModule
{
    public string     ModuleId     => "sms.entity";
    public string     DisplayName  => "Player Entity";
    public string     Category     => "Entities";
    public ModuleType Type         => ModuleType.Logic;
    public bool       IsUniversal  => false;
    public string[]   Compatibility => ["sms"];

    private EntityState _state = new();

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
                Description  = "Tile index of the player sprite in VRAM.",
                Type         = ParameterType.TileRef,
                DefaultValue = 0
            },
            new ParameterDefinition
            {
                Name         = "x",
                DisplayName  = "Initial X",
                Description  = "Initial horizontal position in pixels.",
                Type         = ParameterType.Int,
                DefaultValue = 32,
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
                DefaultValue = 2,
                MinValue     = 1,
                MaxValue     = 8
            },
            new ParameterDefinition
            {
                Name         = "health",
                DisplayName  = "Health",
                Description  = "Initial hit points.",
                Type         = ParameterType.Int,
                DefaultValue = 3,
                MinValue     = 1,
                MaxValue     = 16
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()            => JsonSerializer.Serialize(_state);
    public void   Deserialize(string json) => _state = JsonSerializer.Deserialize<EntityState>(json) ?? new();
    public string GetValidationSample()  => JsonSerializer.Serialize(new EntityState());

    private class EntityState
    {
        public int SpriteId { get; set; } = 0;
        public int X        { get; set; } = 32;
        public int Y        { get; set; } = 144;
        public int Speed    { get; set; } = 2;
        public int Health   { get; set; } = 3;
    }
}
