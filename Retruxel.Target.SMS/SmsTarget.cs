using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Modules.Text;
using Retruxel.Target.SMS.Modules.Text;
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
        // Screen
        ScreenWidth  = 256,
        ScreenHeight = 192,

        // Tiles
        TileWidth      = 8,
        TileHeight     = 8,
        MaxTilesInVram = 448,

        // Colors & Palettes
        // SMS VDP: 2 bits per channel (R, G, B) → 4 levels per channel → 64 total colors
        TotalColors                = 64,
        ColorDepthBitsPerChannel   = 2,
        ColorsPerTile              = 4,
        ColorsPerPalette           = 16,
        SimultaneousPalettes       = 2,
        BgPalettes                 = 2,   // both palettes available for BG tiles
        SpritePalettes             = 2,   // both palettes available for sprites

        // Sprites
        SpritesPerScanline          = 8,
        MaxSpritesOnScreen          = 64,
        SpriteWidth                 = 8,
        SpriteHeight                = 8,
        SupportsDoubleHeightSprites = true,   // 8×16 mode via VDP register

        // Memory
        RamBytes    = 8192,
        RomMaxBytes = 524288,

        // CPU
        CPU        = "Zilog Z80",
        CpuClockHz = 3546893,

        // Sound
        SoundChip          = "SN76489",
        SoundToneChannels  = 3,
        SoundNoiseChannels = 1
    };

    // Hardware palette 

    /// <summary>
    /// The SMS VDP produces colors from 2-bit RGB values.
    /// Each channel has 4 possible levels: 0, 85, 170, 255 (0x00, 0x55, 0xAA, 0xFF).
    /// All 4×4×4 = 64 combinations are valid hardware colors.
    /// </summary>
    public IReadOnlyList<HardwareColor> GetHardwarePalette()
    {
        byte[] levels = [0, 85, 170, 255];

        var palette = new List<HardwareColor>(64);

        foreach (var r in levels)
            foreach (var g in levels)
                foreach (var b in levels)
                    palette.Add(new HardwareColor(r, g, b));

        return palette;
    }

    // Toolchain & modules

    public IToolchain GetToolchain() => new SmsToolchain();

    public IEnumerable<IModule> GetBuiltinModules()
    {
        // Built-in SMS modules will be registered here as they are implemented
        return [];
    }

    // Templates

    public IEnumerable<ProjectTemplate> GetTemplates() =>
    [
        new ProjectTemplate
        {
            TemplateId  = "sms.blank",
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

    // Settings

    public IEnumerable<ParameterDefinition> GetSettingsDefinitions() =>
    [
        new ParameterDefinition
        {
            Name         = "region",
            DisplayName  = "Region",
            Description  = "Target region. Affects VBlank timing.",
            Type         = ParameterType.Enum,
            DefaultValue = "NTSC",
            EnumOptions  = new() { { "NTSC", "NTSC" }, { "PAL", "PAL" } }
        },
        new ParameterDefinition
        {
            Name         = "romSize",
            DisplayName  = "ROM Size",
            Description  = "Maximum ROM size in KB.",
            Type         = ParameterType.Enum,
            DefaultValue = "32",
            EnumOptions  = new() { { "32KB", "32" }, { "128KB", "128" }, { "256KB", "256" }, { "512KB", "512" } }
        },
        new ParameterDefinition
        {
            Name         = "fmSound",
            DisplayName  = "FM Sound Unit",
            Description  = "Enable FM sound support (Japan only).",
            Type         = ParameterType.Bool,
            DefaultValue = false
        }
    ];

    // Code generation

    /// <summary>
    /// Translates universal module data into SMS-specific C code.
    /// Each module has a corresponding SMS code generator.
    /// Returns empty if no translator exists for the given module.
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module)
    {
        switch (module.ModuleId)
        {
            case "text.display":
                var textModule = (TextDisplayModule)module;
                var codeGen = new SmsTextDisplayCodeGen(textModule);

                // Validate hardware limits — warnings returned as comments in generated code
                var errors = codeGen.Validate().ToList();
                var files = new List<GeneratedFile>
                {
                    codeGen.GenerateCode(),
                    codeGen.GenerateHeader()
                };

                // Inject validation warnings as comments at top of source file
                if (errors.Count > 0)
                {
                    var warnings = string.Join("\n", errors.Select(e => $"// WARNING: {e}"));
                    files[0] = new GeneratedFile
                    {
                        FileName = files[0].FileName,
                        Content = warnings + "\n\n" + files[0].Content,
                        FileType = files[0].FileType,
                        SourceModuleId = files[0].SourceModuleId
                    };
                }

                return files;

            default:
                return [];
        }
    }

    public GeneratedFile GenerateMainFile(RetruxelProject project, IEnumerable<GeneratedFile> moduleFiles)
    {
        var headers = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Header)
            .Select(f => $"#include \"{f.FileName}\"");

        var initCalls = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Source)
            .Select(f => $"    {Path.GetFileNameWithoutExtension(f.FileName)}_init();");

        var content = string.Join("\n", [
            "// Generated by Retruxel — do not edit manually.",
        $"// Project: {project.Name} | Target: {project.TargetId}",
        $"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
        "",
        "#include \"SMSlib.h\"",
        .. headers,
        "",
        "void main(void) {",
        .. initCalls,
        "",
        "    while(1) {",
        "        SMS_waitForVBlank();",
        "    }",
        "}",
        "",
        "SMS_EMBED_SEGA_ROM_HEADER(9999, 0);",
        $"SMS_EMBED_SDSC_HEADER(1, 0, 2026, 1, 1, \"Retruxel\", \"{project.Name}\", \"\");",
    ]);

        return new GeneratedFile
        {
            FileName = "main.c",
            Content = content,
            FileType = GeneratedFileType.Source,
            SourceModuleId = "retruxel.core"
        };
    }
}