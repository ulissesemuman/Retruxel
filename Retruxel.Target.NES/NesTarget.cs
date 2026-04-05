using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Target.NES.Toolchain;

namespace Retruxel.Target.NES;

/// <summary>
/// Target implementation for Nintendo Entertainment System (NES).
/// </summary>
public class NesTarget : ITarget
{
    public string TargetId => "nes";
    public string DisplayName => "Nintendo Entertainment System";
    public string ShortName => "NES";
    public string Description => "8-bit home video game console by Nintendo (1983)";

    public TargetSpecs Specs => new()
    {
        ScreenWidth = 256,
        ScreenHeight = 240,
        TileWidth = 8,
        TileHeight = 8,
        RamBytes = 2048,
        CPU = "MOS 6502",
        CpuClockHz = 1789773,
        SoundChip = "2A03",
        SoundToneChannels = 4,
        SoundNoiseChannels = 1,
        TotalColors = 54,
        ColorsPerTile = 4,
        MaxTilesInVram = 256
    };

    public IReadOnlyList<HardwareColor> GetHardwarePalette()
    {
        // NES PPU palette - 54 colors (4 rows of 13, last column unused)
        var palette = new List<HardwareColor>();
        
        // Hardcoded NES palette RGB values
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

        for (int i = 0; i < nesColors.Length; i++)
        {
            palette.Add(new HardwareColor(
                nesColors[i].r,
                nesColors[i].g,
                nesColors[i].b
            ));
        }

        return palette;
    }

    public IEnumerable<ProjectTemplate> GetTemplates() => new[]
    {
        new ProjectTemplate
        {
            TemplateId = "nes.blank",
            DisplayName = "Blank Project",
            Description = "Empty NES project with basic initialization",
            DefaultModules = [],
            DefaultParameters = new Dictionary<string, object>
            {
                { "region", "NTSC" },
                { "mapper", "NROM" }
            }
        }
    };

    public IEnumerable<ParameterDefinition> GetSettingsDefinitions() => new[]
    {
        new ParameterDefinition
        {
            Name = "region",
            DisplayName = "Region",
            Type = ParameterType.Enum,
            DefaultValue = "NTSC",
            EnumOptions = new Dictionary<string, string> { { "NTSC", "NTSC" }, { "PAL", "PAL" } },
            Description = "Video standard - affects timing and resolution"
        },
        new ParameterDefinition
        {
            Name = "mapper",
            DisplayName = "Mapper",
            Type = ParameterType.Enum,
            DefaultValue = "NROM",
            EnumOptions = new Dictionary<string, string> { { "NROM", "NROM" }, { "MMC1", "MMC1" }, { "MMC3", "MMC3" }, { "UNROM", "UNROM" } },
            Description = "Memory mapper chip used by the cartridge"
        }
    };

    public IToolchain GetToolchain() => new NesToolchain();

    public IEnumerable<IModule> GetBuiltinModules() => [];

    public IEnumerable<GeneratedFile> GenerateCodeForModule(IModule module) => [];

    public GeneratedFile GenerateMainFile(RetruxelProject project, IEnumerable<GeneratedFile> moduleFiles)
    {
        return new GeneratedFile
        {
            FileName = "main.c",
            Content = "// NES main file generation not yet implemented",
            FileType = GeneratedFileType.Source,
            SourceModuleId = "retruxel.core"
        };
    }
}
