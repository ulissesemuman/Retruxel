



namespace Retruxel.Target.GG;

/// <summary>
/// Sega Game Gear target definition.
/// Registers the GG toolchain, built-in modules and project templates.
/// </summary>
public class GgTarget : ITarget
{
    public string TargetId => "gg";
    public string DisplayName => "Sega Game Gear";
    public string Description => "Z80 @ 3.58MHz — 8KB RAM — 4096 colors (12-bit)";

    public TargetSpecs Specs => new()
    {
        // Screen — GG displays 160×144 from the VDP's 256×192 output
        ScreenWidth = 160,
        ScreenHeight = 144,

        // Tiles
        TileWidth = 8,
        TileHeight = 8,
        MaxTilesInVram = 448,

        // Colors & Palettes
        // GG VDP: 4 bits per channel (R, G, B) ? 16 levels per channel ? 4096 total colors
        TotalColors = 4096,
        ColorDepthBitsPerChannel = 4,
        ColorsPerTile = 4,
        ColorsPerPalette = 16,
        SimultaneousPalettes = 2,
        BgPalettes = 2,
        SpritePalettes = 2,

        // Sprites
        SpritesPerScanline = 8,
        MaxSpritesOnScreen = 64,
        SpriteWidth = 8,
        SpriteHeight = 8,
        SupportsDoubleHeightSprites = true,

        // Memory
        RamBytes = 8192,
        Banks =
        [
            new RomBank("rom", "ROM", 524288)
        ],

        // CPU
        CPU = "Zilog Z80",
        CpuClockHz = 3579545,

        // Manufacturer
        Manufacturer = "Sega",

        // Sound
        SoundChip = "SN76489",
        SoundToneChannels = 3,
        SoundNoiseChannels = 1
    };

    // Hardware palette

    /// <summary>
    /// The GG VDP produces colors from 4-bit RGB values.
    /// Each channel has 16 possible levels (0x0–0xF mapped to 0–255).
    /// All 16×16×16 = 4096 combinations are valid hardware colors.
    /// </summary>
    public IReadOnlyList<HardwareColor> GetHardwarePalette()
    {
        var palette = new List<HardwareColor>(4096);

        for (int r = 0; r < 16; r++)
            for (int g = 0; g < 16; g++)
                for (int b = 0; b < 16; b++)
                    palette.Add(new HardwareColor(
                        (byte)(r * 17),
                        (byte)(g * 17),
                        (byte)(b * 17)));

        return palette;
    }

    // Toolchain & modules

    public IToolchain GetToolchain()
    {
        var builder = Retruxel.Toolchain.ToolchainOrchestrator.GetBuilder(TargetId);
        return new Retruxel.Toolchain.ToolchainAdapter(builder);
    }

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
            TemplateId  = "gg.blank",
            DisplayName = "Blank Project",
            Description = "Empty Game Gear project with no pre-configured modules.",
            DefaultModules = []
        },
        new ProjectTemplate
        {
            TemplateId = "gg.platformer",
            DisplayName = "Platformer",
            Description = "Pre-configured with tiles, sprites, physics and input modules.",
            DefaultModules = ["tiles", "sprites", "physics", "input"]
        }
    ];

    // Settings

    public IEnumerable<ParameterDefinition> GetSettingsDefinitions() =>
    [
        new ParameterDefinition
        {
            Name         = "romSize",
            DisplayName  = "ROM Size",
            Description  = "Maximum ROM size in KB.",
            Type         = ParameterType.Enum,
            DefaultValue = "32",
            EnumOptions  = new() { { "32KB", "32" }, { "128KB", "128" }, { "256KB", "256" }, { "512KB", "512" } }
        }
    ];

    // Code generation

    private static Dictionary<string, Type>? _codeGenCache;

    /// <summary>
    /// Translates universal module data into GG-specific C code.
    /// Uses reflection to discover and invoke codegen classes.
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module)
    {
        _codeGenCache ??= ReflectionCodeGenHelper.BuildCodeGenCache(GetType().Assembly, "GG");
        return ReflectionCodeGenHelper.GenerateCodeForModule(module, _codeGenCache);
    }

    public IEnumerable<GeneratedFile> GenerateSystemFiles() => [];

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
