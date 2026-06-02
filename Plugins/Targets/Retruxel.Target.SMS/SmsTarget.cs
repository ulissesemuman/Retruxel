

using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Target.SMS.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Retruxel.Target.SMS;

/// <summary>
/// Sega Master System target definition.
/// Registers the SMS toolchain, built-in modules and project templates.
/// </summary>
public class SmsTarget : ITarget, IPaletteConverter
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

        // Colors & Palettes
        // SMS VDP: 2 bits per channel (R, G, B) → 4 levels per channel → 64 total colors
        TotalColors = 64,
        ColorDepthBitsPerChannel = 2,
        ColorsPerTile = 16,
        ColorsPerPalette = 16,
        SimultaneousPalettes = 2,
        BgPalettes = 2,   // both palettes available for BG tiles
        SpritePalettes = 2,   // both palettes available for sprites

        // BG Planes
        // SMS has a single scrollable BG plane (Name Table).
        // Sprites are implicit — controlled by MaxSpritesOnScreen/SpritesPerScanline.
        Planes =
        [
            new PlaneSpecs
            {
                Id                   = "bg",
                Label                = "Background",
                SupportsHorizontalFlip = true,
                SupportsVerticalFlip   = true,
                SupportsRotation       = false,
                PaletteMode            = PaletteMode.PerTile,
                BitsPerPixel           = 4,   // 16-color → 4bpp → 32 bytes per tile
                PaletteBitsPerTile     = 1,   // 1 bit → 2 palettes (0 or 1)
                DefaultWidth           = 32,
                DefaultHeight          = 28,
                MaxWidth               = 64,
                MaxHeight              = 64,
            }
        ],

        // VRAM
        // SMS VRAM: 16 KB total
        //   Name Table:  32×28 × 2 bytes = 1792 bytes
        //   SAT:         256 bytes (64 sprites × 4 bytes)
        //   Tiles:       remaining ≈ 14336 bytes → 448 tiles × 32 bytes
        VramBytesForTiles = 14336,

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
        SoundNoiseChannels = 1,

        // Input
        // SMS has two DE-9 controller ports (Port A and Port B).
        // Each port exposes a D-pad and 2 fire buttons.
        InputPorts =
        [
            new InputPort("port1", "Controller 11", InputPortType.Controller,
                Buttons:
                [
                    new InputButton("up",    "Up",       "PORT_A_KEY_UP"),
                    new InputButton("down",  "Down",     "PORT_A_KEY_DOWN"),
                    new InputButton("left",  "Left",     "PORT_A_KEY_LEFT"),
                    new InputButton("right", "Right",    "PORT_A_KEY_RIGHT"),
                    new InputButton("btn1",  "Button 1", "PORT_A_KEY_1"),
                    new InputButton("btn2",  "Button 2", "PORT_A_KEY_2"),
                ]),

            new InputPort("port2", "Controller 2", InputPortType.Controller,
                Buttons:
                [
                    new InputButton("up",    "Up",       "PORT_B_KEY_UP"),
                    new InputButton("down",  "Down",     "PORT_B_KEY_DOWN"),
                    new InputButton("left",  "Left",     "PORT_B_KEY_LEFT"),
                    new InputButton("right", "Right",    "PORT_B_KEY_RIGHT"),
                    new InputButton("btn1",  "Button 1", "PORT_B_KEY_1"),
                    new InputButton("btn2",  "Button 2", "PORT_B_KEY_2"),
                ]),
        ]
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
            new Retruxel.Modules.Graphics.PlaneModule(),
            new Retruxel.Modules.Graphics.SpriteModule(),
            new Retruxel.Modules.Graphics.TextDisplayModule(),
            new Retruxel.Modules.Graphics.FadeInModule(),
            new Retruxel.Modules.Graphics.FadeOutModule(),
            new Retruxel.Modules.Graphics.SplashModule()
        ];
    }

    // Templates

    public IEnumerable<ProjectTemplate> GetTemplates() =>
    [
        new ProjectTemplate
        {
            TemplateId = "sms.blank",
            DisplayName = "Blank Project",
            Description = "Empty SMS project with no pre-configured modules."
        },
        new ProjectTemplate
        {
            TemplateId = "sms.platformer",
            DisplayName = "Platformer",
            Description = "Pre-configured with tiles, sprites, physics and input modules."
        },
        new ProjectTemplate
        {
            TemplateId = "sms.beatemup",
            DisplayName = "Beat Em Up",
            Description = "Pre-configured for side-scrolling beat em up games."
        }
    ];

    // Settings

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
        // Splash is now handled via codegen (see Plugins/CodeGens/splash/)
        // This method no longer generates splash code
        return [];
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

    public string GenerateSceneTransitionPreamble() =>
        "    SMS_displayOff();\n" +
        "    SMS_VRAMmemsetW(0, 0x0000, 16384);";

    public string GenerateSceneTransitionPostamble() =>
        "    SMS_displayOn();";

    public bool SupportsWindowPlane => true;

    public HudStrategy GetHudStrategy() => HudStrategy.WindowPlane;

    public int GetMaxPalettesPerTilemap() => 2;

    public int GetPaletteSlotCount() => 2;

    public int GetColorsPerSlot() => 16;

    public PaletteSlotType GetPaletteSlotType(int slotIndex) => slotIndex switch
    {
        0 => PaletteSlotType.Background,
        1 => PaletteSlotType.Sprite,
        _ => PaletteSlotType.Shared
    };

    public AssetEntry GetSplashAsset()
    {
        // Embedded splash asset - MapIndex, Tiles and palette from optimized version
        // Generated using Retruxel with tile deduplication and flip optimization
        // Tiles format: ushort[] with tile index + flip flags (bit 9 = H flip, bit 10 = V flip)
        return new AssetEntry
        {
            Id = "splash",
            FileName = "splash.png",
            RelativePath = "Assets/Source/splash.png",
            SourceWidth = 144,
            SourceHeight = 112,
            ImportedAt = new DateTime(2026, 5, 18, 12, 2, 57, 466, DateTimeKind.Local),
            GenerationParams = new AssetGenerationParams
            {
                ColorSpace = "LAB",
                DiversityWeight = 1.25,
                TargetPalette = 0,
                ColorCount = 4,
                Palette = new List<string>
                {
                    "#000000",
                    "#555555",
                    "#AAFF55",
                    "#55AA55"
                },
                MapIndex = Convert.FromBase64String("AgAAAAIAAAACAAAAAgICAAACAgIAAAACAAAAAAACAAIAAgICAgIAAgAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAgAAAAAAAwICAgICAgICAgICAgICAgICAgAAAAAAAAAAAAAAAAMCAgICAgACAgAAAgACAAACAAACAAIAAAAAAAACAAMAAgAAAgAAAAIAAAACAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgICAAAAAAECAgICAgICAgICAgICAgICAgICAAAAAAAAAAAAAAAAAwICAgIAAgACAAACAAIAAAIAAAIAAgIAAAAAAAIAAgACAAACAAAAAgAAAAICAgIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAICAgIAAAAAAwICAgICAgICAgICAgICAgICAgIAAAAAAAAAAAAAAAADAgICAgAAAAIAAwICAgMAAgAAAgACAAAAAAAAAAIAAgAAAAIAAAACAAAAAgAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAgAAAAADAgICAgICAgICAgICAgICAgICAgAAAAAAAAAAAAAAAAMCAgICAAAAAgACAAAAAgACAgIAAAICAgAAAAAAAgACAAAAAgAAAAIAAAACAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQMDAwMDAwMDAwMCAgICAAAAAAMCAgICAgICAAAAAAAAAAAAAAAAAAAAAAACAgICAgICAgICAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAICAgIAAAAAAAAAAAMCAgICAgICAgICAgICAgIAAAAAAwICAgICAgIAAAAAAAAAAAAAAAAAAAAAAAICAgICAgICAgICAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgICAgAAAAAAAAACAgICAgICAgICAgICAgICAgAAAAADAgICAgICAgAAAAAAAAAAAAAAAAAAAAAAAgICAgICAgICAgIAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACAgICAAAAAAAAAgICAgICAgICAgICAgICAgICAAAAAAMCAgICAgICAAAAAAAAAAAAAAAAAAAAAAACAgICAgICAgICAgICAgIAAAAAAAAAAAAAAAACAgICAgICAgAAAAACAgICAgICAwEAAAAAAAICAgICAgAAAAMCAgICAgICAgICAgICAgICAgIDAAICAgMBAAAAAAACAgICAAACAgICAAMCAgIDAAAAAAAAAAAAAAMCAgIDAAICAgICAgAAAgICAgAAAgICAgAAAAAAAAAAAAAAAAICAgICAgICAAAAAAICAgICAgICAgMBAAAAAgICAgICAAAAAwICAgICAgICAgICAgICAgICAgMAAgICAgIDAQAAAAICAgIAAAICAgIAAwICAgIBAAAAAAAAAAABAgICAgMAAgICAgICAAACAgICAAACAgICAAAAAAAAAAAAAAAAAgICAgICAgIAAAAAAgICAgICAgICAgIBAAACAgICAgIAAAADAgICAgICAgICAgICAgICAgICAwACAgICAgICAQAAAgICAgAAAgICAgAAAwICAgMBAAAAAAAAAQMCAgIDAAACAgICAgIAAAICAgIAAAICAgIAAAAAAAAAAAAAAAACAgICAgICAgAAAAACAgICAgICAgICAgMAAAICAgICAgAAAAMCAgICAgICAgICAgICAgICAgIDAAICAgICAgIDAAACAgICAAACAgICAAABAgICAgMAAAAAAAADAgICAgEAAAICAgICAgAAAgICAgAAAgICAgAAAAAAAAAAAAAAAAICAgICAgICAAAAAAAAAAAAAAICAgICAgEAAgICAgAAAAAAAAAAAAAAAAACAgICAAAAAAAAAAAAAAMCAgICAgIBAAICAgIAAAICAgIAAAADAgICAgEAAAAAAQICAgIDAAAAAgICAgAAAAACAgICAAACAgICAAAAAAICAgICAgICAgICAgICAgIAAAAAAAAAAAAAAQMCAgICAwACAgICAAAAAAAAAAAAAAAAAAICAgIAAAAAAAAAAAAAAAEDAgICAgMAAgICAgAAAgICAgAAAAECAgICAwAAAAADAgICAgEAAAACAgICAAAAAAICAgIAAAICAgIAAAAAAgICAgICAgICAgICAgICAgAAAAAAAAAAAAAAAAMCAgICAAICAgIAAAAAAAAAAAAAAAAAAgICAgAAAAAAAAAAAAAAAAEDAgICAgACAgICAAACAgICAAAAAAMCAgICAQAAAQICAgIDAAAAAAICAgIAAAAAAgICAgAAAgICAgAAAAACAgICAgICAgICAgICAgICAAAAAAAAAAAAAAAAAQICAgIAAgICAgAAAAAAAAAAAAAAAAACAgICAAAAAAAAAAAAAAAAAAECAgICAAICAgIAAAICAgIAAAAAAQICAgIDAAADAgICAgEAAAAAAgICAgAAAAACAgICAAAAAAAAAAAAAAAAAAADAgICAgACAgICAAAAAAAAAAAAAAAAAAICAgIAAAAAAQMCAgICAAICAgIAAAABAgICAgMDAgICAgEAAAAAAAICAgIAAAICAgAAAAAAAwACAgICAAADAAICAgIAAAAAAAAAAAAAAgICAgAAAAAAAAICAgIAAAAAAAAAAAAAAAAAAwICAgIDAAICAgIAAAAAAAAAAAAAAAAAAgICAgAAAAEDAgICAgMAAgICAgAAAAADAgICAgICAgIDAAAAAAAAAgICAgAAAgICAAAAAAADAAICAgIAAAMAAwICAgMAAAAAAAAAAAMCAgIDAAAAAAAAAgICAgAAAAAAAAAAAAAAAAMCAgICAwEAAgICAgICAgICAgICAgAAAAACAgICAAABAwICAgIDAQACAgICAAAAAAECAgICAgICAgEAAAAAAAACAgICAgICAgIAAAAAAAMAAgICAgAAAwABAgICAgEAAAAAAAABAgICAgEAAAABAAACAgICAAACAgICAgICAgICAgICAgMBAAACAgICAgICAgICAgICAAAAAAICAgIAAgICAgICAwEAAAICAgIAAAAAAAMCAgICAgIDAAAAAAAAAAICAgICAgICAgAAAAAAAwACAgICAAADAAADAgICAgEAAAAAAQICAgIDAAAAAAMAAAICAgIAAAICAgICAgICAgICAgICAwAAAAICAgICAgICAgICAgIAAAAAAgICAgACAgICAgIDAAAAAgICAgAAAAAAAwICAgICAgMAAAAAAAAAAgICAgICAgICAAAAAAADAAICAgICAgMAAAECAgICAgMBAQMCAgICAgEAAAABAgAAAgICAgICAgICAgICAgICAgICAgICAwAAAgICAgICAgICAgICAgAAAAACAgICAAICAgICAgIDAAACAgICAAAAAAECAgICAgICAgEAAAAAAAACAgICAgICAgIAAAAAAAMAAgICAgICAwAAAAMCAgICAgICAgICAgIDAAAAAAMCAAACAgICAgICAgICAgICAgICAgICAgICAQACAgICAAAAAAAAAAAAAAAAAAICAgIAAgICAgICAgIBAAICAgIAAAAAAwICAgICAgICAwAAAAAAAAICAgIAAAICAgAAAAAAAwACAgICAgIDAAAAAAMCAgICAgICAgICAwAAAAADAgIAAAICAgICAgICAgIAAAAAAAAAAAICAgIDAAICAgIAAAAAAAAAAAAAAAAAAgICAgAAAAAAAgICAgMAAgICAgAAAAECAgICAwMCAgICAQAAAAAAAgICAgAAAgICAAAAAAADAAICAgICAgMAAAAAAAEDAgICAgICAwEAAAAAAAMCAgAAAgICAgIC"),
                //Tiles = new ushort[]
                //{
                //    // Row 0
                //    2, 0, 2, 0, 2, 0, 2, 2, 2, 0, 2, 2, 2, 0, 2, 0, 0, 2, 0, 2, 2, 2, 2, 2, 2, 0, 2, 0, 2, 0, 0, 0,
                //    // Row 1
                //    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                //    // Row 2
                //    2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 3, 2, 2, 2, 2, 2, 2, 2, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0,
                //    // Row 3
                //    2, 0, 0, 0, 2, 0, 3, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                //    // Row 4
                //    0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                //    // Row 5
                //    0, 0, 0, 0, 0, 0, 3, 2, 2, 2, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 2, 0, 0, 2, 0, 2, 0, 2, 0,
                //    // Row 6
                //    2, 0, 2, 0, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                //    // Row 7
                //    2, 2, 2, 2, 0, 0, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 3, 2,
                //    // Row 8
                //    2, 2, 2, 0, 2, 0, 3, 2, 2, 2, 3, 0, 2, 0, 2, 0, 2, 0, 0, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0,
                //    // Row 9
                //    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 3, 2,
                //    // Row 10
                //    2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 3, 2, 2, 2, 2, 0, 2, 0, 2, 0,
                //    // Row 11
                //    2, 0, 2, 2, 2, 0, 2, 2, 2, 0, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                //    // Row 12
                //    0, 0, 0, 0, 0, 0, 0, 0, 1, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 0, 0, 3, 2, 2, 2, 2, 2,
                //    // Row 13
                //    2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0,
                //    // Row 14
                //    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 0, 0,
                //    // Row 15
                //    3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 3, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0,
                //    // Row 16
                //    0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                //    // Row 17
                //    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                //    // Row 18
                //    2, 2, 2, 2, 2, 2, 2, 0, 0, 3, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2,
                //    // Row 19
                //    2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                //    // Row 20
                //    2, 2, 2, 2, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 3, 2, 2, 2, 2, 2,
                //    // Row 21
                //    2, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0,
                //    // Row 22
                //    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2,
                //    // Row 23
                //    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 0, 0
                //},
                OptimizedWidth = 128,
                OptimizedHeight = 24,
                TileCount = 48,
                EnableFlipH = true,
                EnableFlipV = true,
                EnableRotation = false
            }
        };
    }

    // IPaletteConverter implementation
    byte[] IPaletteConverter.ConvertColors(IEnumerable<string> hexColors)
    {
        var converter = new Palette.SmsPaletteConverter();
        return converter.ConvertColors(hexColors);
    }

    // IFontConverter implementation
    public IFontConverter GetFontConverter() => new SmsFontConverter();

    // Input ports
    // Delegates to Specs.InputPorts — single source of truth.
    public InputPort[] GetInputPorts() => Specs.InputPorts;

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
        var modulesWithInit = new HashSet<string> { "entity", "enemy", "scroll", "palette", "plane", "sprite", "input", "physics", "animation" };
        // Modules that have update functions
        var modulesWithUpdate = new HashSet<string> { "entity", "enemy", "scroll", "input", "physics", "animation" };

        // Generate init calls - one per module type
        var initCalls = moduleGroups
            .Where(g => modulesWithInit.Contains(g.Key))
            .Select(g => $"    {g.Key.Replace(".", "_")}_init();");

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
