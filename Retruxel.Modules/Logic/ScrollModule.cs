using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Modules.Logic;

/// <summary>
/// Background scroll module.
/// Handles horizontal scrolling via the SMS VDP scroll register.
/// Code generation is delegated to SmsScrollCodeGen in Retruxel.Target.SMS.
/// </summary>
public class ScrollModule : ILogicModule
{
    public string     ModuleId     => "sms.scroll";
    public string     DisplayName  => "Background Scroll";
    public string     Category     => "Background";
    public ModuleType Type         => ModuleType.Logic;
    public bool       IsUniversal  => false;
    public string[]   Compatibility => ["sms"];

    private ScrollState _state = new();

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
                Name         = "speed",
                DisplayName  = "Speed",
                Description  = "Scroll speed in pixels per frame.",
                Type         = ParameterType.Int,
                DefaultValue = 1,
                MinValue     = 1,
                MaxValue     = 8
            },
            new ParameterDefinition
            {
                Name         = "direction",
                DisplayName  = "Direction",
                Description  = "Scroll direction.",
                Type         = ParameterType.Enum,
                DefaultValue = "Left",
                EnumOptions  = new() { { "Left", "Left" }, { "Right", "Right" } }
            },
            new ParameterDefinition
            {
                Name         = "loop",
                DisplayName  = "Loop",
                Description  = "Wraps the scroll position after a full screen width.",
                Type         = ParameterType.Bool,
                DefaultValue = true
            }
        ]
    };

    public IEnumerable<GeneratedFile> GenerateCode() => [];

    public string Serialize()              => JsonSerializer.Serialize(_state);
    public void   Deserialize(string json) => _state = JsonSerializer.Deserialize<ScrollState>(json) ?? new();
    public string GetValidationSample()    => JsonSerializer.Serialize(new ScrollState());

    private class ScrollState
    {
        public int    Speed     { get; set; } = 1;
        public string Direction { get; set; } = "Left";
        public bool   Loop      { get; set; } = true;
    }
}
