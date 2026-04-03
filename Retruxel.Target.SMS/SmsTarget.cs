using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Target.SMS.Toolchain;

namespace Retruxel.Target.SMS;

/// <summary>
/// Sega Master System target definition.
/// Registers the SMS toolchain, built-in modules and project templates.
/// </summary>
public class SmsTarget : ITarget
{
    public string TargetId => "sms";
    public string DisplayName => "Sega Master System";
    public string Description => "Z80 @ 3.58MHz — 8KB RAM — SN76489 sound chip";

    public TargetSpecs Specs => new()
    {
        ScreenWidth = 256,
        ScreenHeight = 192,
        TotalColors = 64,
        ColorsPerTile = 4,
        SpritesPerScanline = 8,
        MaxSpritesOnScreen = 64,
        TileWidth = 8,
        TileHeight = 8,
        RamBytes = 8192,
        RomMaxBytes = 524288,
        CPU = "Zilog Z80",
        CpuClockHz = 3546893,
        SoundChip = "SN76489",
        SoundToneChannels = 3,
        SoundNoiseChannels = 1
    };

    public IToolchain GetToolchain() => new SmsToolchain();

    public IEnumerable<IModule> GetBuiltinModules()
    {
        // Built-in SMS modules will be registered here as they are implemented
        return [];
    }

    public IEnumerable<ProjectTemplate> GetTemplates() =>
    [
        new ProjectTemplate
        {
            TemplateId = "sms.blank",
            DisplayName = "Blank Project",
            Description = "Empty SMS project with no pre-configured modules.",
            DefaultModules = []
        },
        new ProjectTemplate
        {
            TemplateId = "sms.platformer",
            DisplayName = "Platformer",
            Description = "Pre-configured with tiles, sprites, physics and input modules.",
            DefaultModules = ["sms.tiles", "sms.sprites", "sms.physics", "sms.input"]
        },
        new ProjectTemplate
        {
            TemplateId = "sms.beatemup",
            DisplayName = "Beat Em Up",
            Description = "Pre-configured for side-scrolling beat em up games.",
            DefaultModules = ["sms.tiles", "sms.sprites", "sms.physics", "sms.input", "sms.scroll"]
        }
    ];

    public IEnumerable<ParameterDefinition> GetSettingsDefinitions() =>
    [
        new ParameterDefinition
        {
            Name = "region",
            DisplayName = "Region",
            Description = "Target region. Affects VBlank timing.",
            Type = ParameterType.Enum,
            DefaultValue = "NTSC",
            EnumOptions = new() { { "NTSC", "NTSC" }, { "PAL", "PAL" } }
        },
        new ParameterDefinition
        {
            Name = "romSize",
            DisplayName = "ROM Size",
            Description = "Maximum ROM size in KB.",
            Type = ParameterType.Enum,
            DefaultValue = "32",
            EnumOptions = new() { { "32KB", "32" }, { "128KB", "128" }, { "256KB", "256" }, { "512KB", "512" } }
        },
        new ParameterDefinition
        {
            Name = "fmSound",
            DisplayName = "FM Sound Unit",
            Description = "Enable FM sound support (Japan only).",
            Type = ParameterType.Bool,
            DefaultValue = false
        }
    ];
}