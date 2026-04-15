using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;

namespace Retruxel.Target.ColecoVision;

/// <summary>
/// ColecoVision target definition.
/// Registers the ColecoVision toolchain, built-in modules and project templates.
/// </summary>
public class ColecoVisionTarget : ITarget
{
    public string TargetId => "coleco";
    public string DisplayName => "ColecoVision";
    public string Description => "Z80 @ 3.58MHz — 1KB RAM — TMS9918A VDP";

    public TargetSpecs Specs => new()
    {
        Manufacturer = "Coleco",

        // Screen
        ScreenWidth = 256,
        ScreenHeight = 192,

        // Tiles
        TileWidth = 8,
        TileHeight = 8,
        MaxTilesInVram = 256,

        // Colors & Palettes
        // TMS9918A: fixed 15-color palette (+ transparent)
        TotalColors = 15,
        ColorDepthBitsPerChannel = 0,
        ColorsPerTile = 2,
        ColorsPerPalette = 15,
        SimultaneousPalettes = 1,
        BgPalettes = 1,
        SpritePalettes = 1,

        // Sprites
        SpritesPerScanline = 4,
        MaxSpritesOnScreen = 32,
        SpriteWidth = 8,
        SpriteHeight = 8,
        SupportsDoubleHeightSprites = false,

        // Memory
        RamBytes = 1024,
        Banks =
        [
            new RomBank("rom", "ROM", 32768)
        ],

        // CPU
        CPU = "Zilog Z80",
        CpuClockHz = 3579545,

        // Sound
        SoundChip = "SN76489",
        SoundToneChannels = 3,
        SoundNoiseChannels = 1
    };

    // Hardware palette

    /// <summary>
    /// The TMS9918A has a fixed 15-color palette (index 0 is transparent).
    /// Identical to the SG-1000 — both use the same VDP chip.
    /// </summary>
    public IReadOnlyList<HardwareColor> GetHardwarePalette() =>
    [
        new HardwareColor(  0,   0,   0),   //  0 — Transparent
        new HardwareColor(  0,   0,   0),   //  1 — Black
        new HardwareColor( 33, 200,  66),   //  2 — Medium Green
        new HardwareColor( 94, 220, 120),   //  3 — Light Green
        new HardwareColor( 84,  85, 237),   //  4 — Dark Blue
        new HardwareColor(125, 118, 252),   //  5 — Light Blue
        new HardwareColor(212,  82,  77),   //  6 — Dark Red
        new HardwareColor( 66, 235, 245),   //  7 — Cyan
        new HardwareColor(252,  85,  84),   //  8 — Medium Red
        new HardwareColor(255, 121, 120),   //  9 — Light Red
        new HardwareColor(212, 193,  84),   // 10 — Dark Yellow
        new HardwareColor(230, 206, 128),   // 11 — Light Yellow
        new HardwareColor( 33, 176,  59),   // 12 — Dark Green
        new HardwareColor(201,  91, 186),   // 13 — Magenta
        new HardwareColor(204, 204, 204),   // 14 — Gray
        new HardwareColor(255, 255, 255),   // 15 — White
    ];

    // Toolchain & modules

    public IToolchain GetToolchain() => new ColecoToolchainAdapter();

    public IEnumerable<IModule> GetBuiltinModules() => [];

    // Templates

    public IEnumerable<ProjectTemplate> GetTemplates() =>
    [
        new ProjectTemplate
        {
            TemplateId     = "coleco.blank",
            DisplayName    = "Blank Project",
            Description    = "Empty ColecoVision project with no pre-configured modules.",
            DefaultModules = []
        },
        new ProjectTemplate
        {
            TemplateId     = "coleco.platformer",
            DisplayName    = "Platformer",
            Description    = "Pre-configured with tiles, sprites, physics and input modules.",
            DefaultModules = ["tiles", "sprites", "physics", "input"]
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
        }
    ];

    // Code generation

    private static Dictionary<string, Type>? _codeGenCache;

    /// <summary>
    /// Translates universal module data into ColecoVision-specific C code.
    /// Uses reflection to discover and invoke codegen classes.
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module)
    {
        _codeGenCache ??= ReflectionCodeGenHelper.BuildCodeGenCache(GetType().Assembly, "ColecoVision");
        return ReflectionCodeGenHelper.GenerateCodeForModule(module, _codeGenCache);
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
        "#include \"SGlib.h\"",
        .. headers,
        "",
        "void main(void) {",
        .. initCalls,
        "",
        "    while(1) {",
        "        SG_waitForVBlank();",
        "    }",
        "}",
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
}