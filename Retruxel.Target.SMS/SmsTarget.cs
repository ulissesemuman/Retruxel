using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Target.SMS.Modules.Enemy;
using Retruxel.Target.SMS.Modules.Entity;
using Retruxel.Target.SMS.Modules.Scroll;
using Retruxel.Modules.Graphics;
using Retruxel.Target.SMS.Modules.Graphics;

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
        Banks =
        [
            new RomBank("rom", "ROM", 524288)
        ],

        // CPU
        CPU        = "Zilog Z80",
        CpuClockHz = 3546893,

        // Manufacturer
        Manufacturer = "Sega",

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

    public IToolchain GetToolchain()
    {
        var builder = Retruxel.Toolchain.ToolchainOrchestrator.GetBuilder(TargetId);
        return new SmsToolchainAdapter(builder);
    }

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
            DefaultModules = ["sms.tilemap", "sms.sprite", "sms.physics", "sms.input"]
        },
        new ProjectTemplate
        {
            TemplateId = "sms.beatemup",
            DisplayName = "Beat Em Up",
            Description = "Pre-configured for side-scrolling beat em up games.",
            DefaultModules = ["sms.tilemap", "sms.sprite", "sms.physics", "sms.input", "sms.scroll"]
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
                {
                    var codeGen = new SmsTextDisplayCodeGen(module.Serialize());
                    return InjectWarnings(codeGen.Validate(), [codeGen.GenerateCode(), codeGen.GenerateHeader()]);
                }

            case "sms.entity":
                {
                    var codeGen = new SmsEntityCodeGen(module.Serialize());
                    return InjectWarnings(codeGen.Validate(), [codeGen.GenerateCode(), codeGen.GenerateHeader()]);
                }

            case "sms.scroll":
                {
                    var codeGen = new SmsScrollCodeGen(module.Serialize());
                    return [codeGen.GenerateCode(), codeGen.GenerateHeader()];
                }

            case "sms.enemy":
                {
                    var codeGen = new SmsEnemyCodeGen(module.Serialize());
                    return InjectWarnings(codeGen.Validate(), [codeGen.GenerateCode(), codeGen.GenerateHeader()]);
                }

            default:
                return [];
        }
    }

    public GeneratedFile GenerateMainFile(RetruxelProject project, IEnumerable<GeneratedFile> moduleFiles)
    {
        var fileList = moduleFiles.ToList();

        // Include all headers
        var headers = fileList
            .Where(f => f.FileType == GeneratedFileType.Header)
            .Select(f => $"#include \"{f.FileName}\"");

        // Group files by module to call init once per module type
        var moduleGroups = fileList
            .Where(f => f.FileType == GeneratedFileType.Source)
            .GroupBy(f => f.SourceModuleId)
            .ToList();

        // Modules that have init functions (not text.display)
        var modulesWithInit = new HashSet<string> { "sms.entity", "sms.enemy", "sms.scroll" };
        
        // Generate init calls - one per module type
        var initCalls = moduleGroups
            .Where(g => modulesWithInit.Contains(g.Key))
            .Select(g => $"    {g.Key.Replace(".", "_")}_init();");

        // Modules that have update functions
        var modulesWithUpdate = new HashSet<string> { "sms.entity", "sms.enemy", "sms.scroll" };
        
        var updateCalls = moduleGroups
            .Where(g => modulesWithUpdate.Contains(g.Key))
            .Select(g => $"        {g.Key.Replace(".", "_")}_update();");
        
        // text.display calls (one per instance) - use _init suffix
        var textDisplayCalls = fileList
            .Where(f => f.SourceModuleId == "text.display" && f.FileType == GeneratedFileType.Source)
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
            .. textDisplayCalls,
            "",
            "    while(1) {",
            "        SMS_waitForVBlank();",
            .. updateCalls,
            "    }",
            "}",
            "",
            "SMS_EMBED_SEGA_ROM_HEADER(9999, 0);",
            $"SMS_EMBED_SDSC_HEADER(1, 0, 2026, 1, 1, \"Retruxel\", \"{project.Name}\", \"\");",
            ""
        ]);

        return new GeneratedFile
        {
            FileName = "main.c",
            Content = content,
            FileType = GeneratedFileType.Source,
            SourceModuleId = "retruxel.core"
        };
    }
    
    public void ResetCodeGenerationState()
    {
        SmsTextDisplayCodeGen.ResetCounter();
    }

    // Helpers

    /// <summary>
    /// Prepends validation warnings as C comments to the first source file in the list.
    /// Returns the files unchanged if there are no errors.
    /// </summary>
    private static IEnumerable<GeneratedFile> InjectWarnings(
        IEnumerable<string> errors,
        List<GeneratedFile> files)
    {
        var errorList = errors.ToList();
        if (errorList.Count == 0 || files.Count == 0)
            return files;

        var warnings = string.Join("\n", errorList.Select(e => $"// WARNING: {e}"));
        var first = files[0];

        files[0] = new GeneratedFile
        {
            FileName = first.FileName,
            FileType = first.FileType,
            SourceModuleId = first.SourceModuleId,
            Content = warnings + "\n\n" + first.Content
        };

        return files;
    }
}