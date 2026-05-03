

using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Target.SMS.Modules.Splash;

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
        ScreenWidth = 256,
        ScreenHeight = 192,

        // Tiles
        TileWidth = 8,
        TileHeight = 8,
        VramRegions =
        [
            new VramRegion("bg",     "Background", 0,   255),
            new VramRegion("sprite", "Sprites",    256, 447)
        ],

        // Colors & Palettes
        // SMS VDP: 2 bits per channel (R, G, B) → 4 levels per channel → 64 total colors
        TotalColors = 64,
        ColorDepthBitsPerChannel = 2,
        ColorsPerTile = 16,
        ColorsPerPalette = 16,
        SimultaneousPalettes = 2,
        BgPalettes = 2,   // both palettes available for BG tiles
        SpritePalettes = 2,   // both palettes available for sprites

        // Tilemap
        Tilemap = new TilemapSpecs
        {
            MaxLayers = 1,
            SupportsHorizontalFlip = true,
            SupportsVerticalFlip = true,
            SupportsRotation = false,
            PaletteMode = PaletteMode.PerTile,
            PaletteBitsPerTile = 1,  // 2 palettes (0 or 1)
            DefaultWidth = 32,
            DefaultHeight = 28,
            MaxWidth = 64,
            MaxHeight = 64
        },

        // Sprites
        SpritesPerScanline = 8,
        MaxSpritesOnScreen = 64,
        SpriteWidth = 8,
        SpriteHeight = 8,
        SupportsDoubleHeightSprites = true,   // 8×16 mode via VDP register

        // Memory
        RamBytes = 8192,
        Banks =
        [
            new RomBank("rom", "ROM", 524288)
        ],

        // CPU
        CPU = "Zilog Z80",
        CpuClockHz = 3546893,

        // Manufacturer
        Manufacturer = "Sega",

        // Sound
        SoundChip = "SN76489",
        SoundToneChannels = 3,
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
        var builder = Retruxel.Toolchain.ToolchainOrchestrator.GetBuilder(TargetId, ((ITarget)this).GetCustomToolchainBuilder());
        return new Retruxel.Toolchain.ToolchainAdapter(builder);
    }

    public IEnumerable<string> GetRequiredToolchainBinaries() =>
    [
        Path.Combine("compilers", "sdcc", "bin", "sdcc.exe"),
        Path.Combine("utils", "sega", "bin", "ihx2sms.exe")
    ];

    public IEnumerable<IModule> GetBuiltinModules()
    {
        return
        [
            new Retruxel.Modules.Logic.EntityModule(),
            new Retruxel.Modules.Logic.EnemyModule(),
            new Retruxel.Modules.Logic.PhysicsModule(),
            new Retruxel.Modules.Logic.InputModule(),
            new Retruxel.Modules.Logic.AnimationModule(),
            new Retruxel.Modules.Logic.ScrollModule(),
            new Retruxel.Modules.Graphics.PaletteModule(),
            new Retruxel.Modules.Graphics.TilemapModule(),
            new Retruxel.Modules.Graphics.SpriteModule(),
            new Retruxel.Modules.Graphics.TextDisplayModule()
        ];
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
            DefaultModules = []
        },
        new ProjectTemplate
        {
            TemplateId = "sms.beatemup",
            DisplayName = "Beat Em Up",
            Description = "Pre-configured for side-scrolling beat em up games.",
            DefaultModules = []
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

    public IEnumerable<ModuleOverride> GetModuleOverrides() =>
    [
        new ModuleOverride
        {
            ModuleId = "palette",
            MaxInstances = 2  // BG palette + Sprite palette
        },
        new ModuleOverride
        {
            ModuleId = "entity",
            IsSingleton = true
        },
        new ModuleOverride
        {
            ModuleId = "input",
            IsSingleton = true
        },
        new ModuleOverride
        {
            ModuleId = "physics",
            IsSingleton = true
        },
        new ModuleOverride
        {
            ModuleId = "scroll",
            IsSingleton = true
        }
    ];

    // Code generation

    private static Dictionary<string, Type>? _codeGenCache;

    /// <summary>
    /// Translates universal module data into SMS-specific C code using reflection.
    /// Discovers code generators dynamically by scanning the assembly.
    /// Returns empty if no translator exists for the given module.
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module)
    {
        // Build cache on first call
        _codeGenCache ??= ReflectionCodeGenHelper.BuildCodeGenCache(typeof(SmsTarget).Assembly, "Sms");

        // Generate code using reflection
        return ReflectionCodeGenHelper.GenerateCodeForModule(module, _codeGenCache, InjectWarnings);
    }

    public IEnumerable<GeneratedFile> GenerateSystemFiles()
    {
        var settings = SettingsService.Load();
        if (!settings.General.ShowMadeWithSplash)
            return [];

        var splashGen = new SmsSplashCodeGen();
        return
        [
            splashGen.GenerateHeader(),
            splashGen.GenerateCode()
        ];
    }

    public IEnumerable<GeneratedFile> GenerateEngineRuntime()
    {
        var backend = new Rendering.SmsRenderBackend();
        return
        [
            backend.GenerateEngineHeader(),
            backend.GenerateEngineSource()
        ];
    }

    public BuildDiagnosticsReport? GetBuildDiagnostics(BuildDiagnosticInput input)
    {
        return new SmsDiagnosticsProvider().Analyze(input);
    }

    public GeneratedFile GenerateMainFile(RetruxelProject project, IEnumerable<GeneratedFile> moduleFiles)
    {
        var fileList = moduleFiles.ToList();

        // Include all headers
        IEnumerable<string> headers = fileList
            .Where(f => f.FileType == GeneratedFileType.Header)
            .Select(f => $"#include \"{f.FileName}\"");

        // Group files by module to call init once per module type
        var moduleGroups = fileList
            .Where(f => f.FileType == GeneratedFileType.Source)
            .GroupBy(f => f.SourceModuleId)
            .ToList();

        // Modules that have init functions (not text.display)
        var modulesWithInit = new HashSet<string> { "entity", "enemy", "scroll", "palette", "tilemap", "sprite", "input", "physics", "animation" };

        // Generate init calls - one per module type
        var initCalls = moduleGroups
            .Where(g => modulesWithInit.Contains(g.Key))
            .Select(g => $"    {g.Key.Replace(".", "_")}_init();");

        // Modules that have update functions
        var modulesWithUpdate = new HashSet<string> { "entity", "enemy", "scroll", "input", "physics", "animation" };

        var updateCalls = moduleGroups
            .Where(g => modulesWithUpdate.Contains(g.Key))
            .Select(g => $"        {g.Key.Replace(".", "_")}_update();");

        // text.display calls (one per instance) - use _init suffix
        var textDisplayCalls = fileList
            .Where(f => f.SourceModuleId == "text.display" && f.FileType == GeneratedFileType.Source)
            .Select(f => $"    {Path.GetFileNameWithoutExtension(f.FileName)}_init();");

        // Load settings to check splash preference
        var settings = SettingsService.Load();
        var splashEnabled = settings.General.ShowMadeWithSplash;

        var content = string.Join("\n", [
            "// Generated by Retruxel — do not edit manually.",
            $"// Project: {project.Name} | Target: {project.TargetId}",
            $"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "#include \"SMSlib.h\"",
            .. headers,
            "",
            "void main(void) {",
            .. (splashEnabled ? new[] { "    splash_show();" } : Array.Empty<string>()),
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
