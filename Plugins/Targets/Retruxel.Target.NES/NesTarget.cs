





namespace Retruxel.Target.NES;

/// <summary>
/// Target implementation for Nintendo Entertainment System (NES).
/// </summary>
public class NesTarget : ITarget
{
    public string TargetId => "nes";
    public string DisplayName => "Nintendo Entertainment System";
    public string Description => "8-bit home video game console by Nintendo (1983)";

    public TargetSpecs Specs => new()
    {
        // ── Screen ───────────────────────────────────────────────────────────
        ScreenWidth = 256,
        ScreenHeight = 240,

        // ── Tiles ────────────────────────────────────────────────────────────
        TileWidth = 8,
        TileHeight = 8,
        MaxTilesInVram = 256,

        // ── Colors & Palettes ─────────────────────────────────────────────────
        // NES PPU: fixed 54-color palette, not mathematically calculable
        TotalColors = 54,
        ColorDepthBitsPerChannel = 0,   // N/A — NES palette is fixed hardware output
        ColorsPerTile = 4,
        ColorsPerPalette = 4,   // 4 colors per palette slot (including transparent)
        SimultaneousPalettes = 8,   // 4 BG + 4 sprite palette slots
        BgPalettes = 4,
        SpritePalettes = 4,

        // ── Sprites ──────────────────────────────────────────────────────────
        SpritesPerScanline = 8,
        MaxSpritesOnScreen = 64,
        SpriteWidth = 8,
        SpriteHeight = 8,
        SupportsDoubleHeightSprites = true,   // 8×16 sprite mode via PPUCTRL bit

        // ── Memory ───────────────────────────────────────────────────────────
        RamBytes = 2048,
        Banks =
        [
            new RomBank("prg", "PRG-ROM", 524288),
            new RomBank("chr", "CHR-ROM",   8192)
        ],

        // ── CPU ──────────────────────────────────────────────────────────────
        CPU = "MOS 6502",
        CpuClockHz = 1789773,

        // Manufacturer
        Manufacturer = "Nintendo",

        // ── Sound ────────────────────────────────────────────────────────────
        SoundChip = "2A03",
        SoundToneChannels = 4,
        SoundNoiseChannels = 1
    };

    // ── Hardware palette ──────────────────────────────────────────────────────

    /// <summary>
    /// NES PPU fixed palette — 54 colors hardcoded from hardware output.
    /// Unlike the SMS, these cannot be calculated mathematically.
    /// Values sourced from the widely-accepted Nestopia NTSC palette.
    /// </summary>
    public IReadOnlyList<HardwareColor> GetHardwarePalette()
    {
        var nesColors = new (byte r, byte g, byte b)[]
        {
            // Row 0
            (0x7C, 0x7C, 0x7C), (0x00, 0x00, 0xFC), (0x00, 0x00, 0xBC), (0x44, 0x28, 0xBC),
            (0x94, 0x00, 0x84), (0xA8, 0x00, 0x20), (0xA8, 0x10, 0x00), (0x88, 0x14, 0x00),
            (0x50, 0x30, 0x00), (0x00, 0x78, 0x00), (0x00, 0x68, 0x00), (0x00, 0x58, 0x00),
            (0x00, 0x40, 0x58),
            // Row 1
            (0xBC, 0xBC, 0xBC), (0x00, 0x78, 0xF8), (0x00, 0x58, 0xF8), (0x68, 0x44, 0xFC),
            (0xD8, 0x00, 0xCC), (0xE4, 0x00, 0x58), (0xF8, 0x38, 0x00), (0xE4, 0x5C, 0x10),
            (0xAC, 0x7C, 0x00), (0x00, 0xB8, 0x00), (0x00, 0xA8, 0x00), (0x00, 0xA8, 0x44),
            (0x00, 0x88, 0x88),
            // Row 2
            (0xF8, 0xF8, 0xF8), (0x3C, 0xBC, 0xFC), (0x68, 0x88, 0xFC), (0x98, 0x78, 0xF8),
            (0xF8, 0x78, 0xF8), (0xF8, 0x58, 0x98), (0xF8, 0x78, 0x58), (0xFC, 0xA0, 0x44),
            (0xF8, 0xB8, 0x00), (0xB8, 0xF8, 0x18), (0x58, 0xD8, 0x54), (0x58, 0xF8, 0x98),
            (0x00, 0xE8, 0xD8),
            // Row 3
            (0xF8, 0xF8, 0xF8), (0xA4, 0xE4, 0xFC), (0xB8, 0xB8, 0xF8), (0xD8, 0xB8, 0xF8),
            (0xF8, 0xB8, 0xF8), (0xF8, 0xA4, 0xC0), (0xF0, 0xD0, 0xB0), (0xFC, 0xE0, 0xA8),
            (0xF8, 0xD8, 0x78), (0xD8, 0xF8, 0x78), (0xB8, 0xF8, 0xB8), (0xB8, 0xF8, 0xD8),
            (0x00, 0xFC, 0xFC)
        };

        return nesColors
            .Select(c => new HardwareColor(c.r, c.g, c.b))
            .ToList();
    }

    // ── Toolchain & modules ───────────────────────────────────────────────────

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

    // ── Templates ─────────────────────────────────────────────────────────────

    public IEnumerable<ProjectTemplate> GetTemplates() =>
    [
        new ProjectTemplate
        {
            TemplateId  = "nes.blank",
            DisplayName = "Blank Project",
            Description = "Empty NES project with basic initialization.",
            DefaultModules    = [],
            DefaultParameters = new Dictionary<string, object>
            {
                { "region", "NTSC" },
                { "mapper", "NROM" }
            }
        }
    ];

    // ── Settings ──────────────────────────────────────────────────────────────

    public IEnumerable<ParameterDefinition> GetSettingsDefinitions() =>
    [
        new ParameterDefinition
        {
            Name         = "region",
            DisplayName  = "Region",
            Description  = "Video standard — affects timing and resolution.",
            Type         = ParameterType.Enum,
            DefaultValue = "NTSC",
            EnumOptions  = new() { { "NTSC", "NTSC" }, { "PAL", "PAL" } }
        },
        new ParameterDefinition
        {
            Name         = "mapper",
            DisplayName  = "Mapper",
            Description  = "Memory mapper chip used by the cartridge.",
            Type         = ParameterType.Enum,
            DefaultValue = "NROM",
            EnumOptions  = new()
            {
                { "NROM",  "NROM"  },
                { "MMC1",  "MMC1"  },
                { "MMC3",  "MMC3"  },
                { "UNROM", "UNROM" }
            }
        }
    ];

    // ── Code generation ───────────────────────────────────────────────────────

    private static Dictionary<string, Type>? _codeGenCache;

    /// <summary>
    /// Translates module JSON into NES C code (cc65 + neslib).
    /// Uses reflection to discover and invoke codegen classes.
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module)
    {
        _codeGenCache ??= ReflectionCodeGenHelper.BuildCodeGenCache(GetType().Assembly, "NES");
        return ReflectionCodeGenHelper.GenerateCodeForModule(module, _codeGenCache);
    }

    /// <summary>
    /// Generates the NES main.c entry point using cc65 + neslib.
    /// Includes neslib.h, all module headers, calls init functions
    /// and runs update loop via ppu_wait_nmi().
    /// </summary>
    public IEnumerable<GeneratedFile> GenerateSystemFiles() => [];

    public GeneratedFile GenerateMainFile(
        RetruxelProject project,
        IEnumerable<GeneratedFile> moduleFiles)
    {
        var fileList = moduleFiles.ToList();

        var headers = fileList
            .Where(f => f.FileType == GeneratedFileType.Header)
            .Select(f => $"#include \"{f.FileName}\"");

        // text.display calls (one per instance)
        var textDisplayCalls = fileList
            .Where(f => f.SourceModuleId == "text.display" && f.FileType == GeneratedFileType.Source)
            .Select(f => $"    {Path.GetFileNameWithoutExtension(f.FileName)}_init();");

        var updateModules = new HashSet<string> { "entity", "enemy", "scroll" };
        var updateCalls = fileList
            .Where(f => f.FileType == GeneratedFileType.Source
                     && updateModules.Contains(f.SourceModuleId))
            .Select(f => $"        {Path.GetFileNameWithoutExtension(f.FileName)}_update();");

        var content = string.Join("\n", [
            "// Generated by Retruxel — do not edit manually.",
            $"// Project: {project.Name} | Target: {project.TargetId}",
            $"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "// NES cartridge configuration",
            "#define NES_MAPPER 0        // NROM mapper",
            "#define NES_PRG_BANKS 2     // 2 × 16KB PRG-ROM banks",
            "#define NES_CHR_BANKS 1     // 1 × 8KB CHR-ROM bank",
            "#define NES_MIRRORING 1     // 0=horizontal, 1=vertical",
            "",
            "#include \"neslib.h\"",
            "#include \"nes.h\"",
            .. headers,
            "",
            "// NES palette - white text on black background",
            "const unsigned char palette[16] = {",
            "    0x0F, 0x30, 0x30, 0x30,  // BG0: preto (fundo), branco (texto)",
            "    0x0F, 0x30, 0x30, 0x30,  // BG1",
            "    0x0F, 0x30, 0x30, 0x30,  // BG2",
            "    0x0F, 0x30, 0x30, 0x30   // BG3",
            "};\n",
            "// OAM sprite buffer offset",
            "#pragma bss-name(push, \"ZEROPAGE\")",
            "unsigned char oam_off;",
            "#pragma bss-name(pop)",
            "",
            "void main(void) {",
            "    ppu_off();",
            "    oam_off = 0;",
            "    ",
            "    // Load background palette",
            "    pal_bg(palette);",
            "    ",
            .. textDisplayCalls,
            "    ",
            "    ppu_on_all();",
            "",
            "    while(1) {",
            "        ppu_wait_nmi();",
            .. updateCalls,
            "    }",
            "}",
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
