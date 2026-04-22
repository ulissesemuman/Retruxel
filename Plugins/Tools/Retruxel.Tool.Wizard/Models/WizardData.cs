namespace Retruxel.Tool.Wizard.Models;

/// <summary>
/// Data collected from Target Wizard.
/// </summary>
public class TargetWizardData
{
    // Step 1: Basic Information
    public string TargetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReleaseYear { get; set; } = 1980;
    public string Status { get; set; } = "Scaffolding"; // Active, Scaffolding, Planned

    // Step 2: Technical Specifications
    public string CPU { get; set; } = string.Empty;
    public long CpuClockHz { get; set; }
    public int ScreenWidth { get; set; } = 256;
    public int ScreenHeight { get; set; } = 192;
    public int TileWidth { get; set; } = 8;
    public int TileHeight { get; set; } = 8;
    public int RamBytes { get; set; } = 8192;
    public int VramBytes { get; set; } = 16384;
    public int TotalColors { get; set; } = 64;
    public int ColorDepthBitsPerChannel { get; set; } = 2;
    public int ColorsPerPalette { get; set; } = 16;
    public int MaxSpritesOnScreen { get; set; } = 64;
    public int SpritesPerScanline { get; set; } = 8;
    public string SoundChip { get; set; } = string.Empty;
    public int SoundToneChannels { get; set; } = 3;
    public int SoundNoiseChannels { get; set; } = 1;

    // Step 3: Toolchain
    public string Compiler { get; set; } = "sdcc"; // sdcc, cc65, custom
    public string RomExtension { get; set; } = ".rom";

    // Step 4: Main file
    public string MainFileSource { get; set; } = "template"; // template, type, attach
    public string MainFileContent { get; set; } = string.Empty;

    // Step 5: Variable mappings
    public Dictionary<string, string> VariableMappings { get; set; } = new();

    // Output
    public string OutputPath { get; set; } = string.Empty;
}

/// <summary>
/// Data collected from CodeGen Wizard.
/// </summary>
public class CodeGenWizardData
{
    // Step 1: Basic Information
    public string ModuleId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Step 2: Code
    public string CodeSource { get; set; } = "type"; // type, attach
    public string CodeContent { get; set; } = string.Empty;

    // Step 3: Variable mappings
    public Dictionary<string, VariableMapping> VariableMappings { get; set; } = new();

    // Step 4: Dependencies
    public List<string> RequiredTools { get; set; } = new();

    // Output
    public string OutputPath { get; set; } = string.Empty;
}

/// <summary>
/// Variable mapping configuration for CodeGen.
/// </summary>
public class VariableMapping
{
    public string VariableName { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, int, bool, array
    public string JsonPath { get; set; } = string.Empty; // e.g., "parameters.text"
    public string? Transformation { get; set; } // e.g., "*Length"
}

/// <summary>
/// Data collected from Tool Wizard.
/// </summary>
public class ToolWizardData
{
    // Step 1: Basic Information
    public string ToolId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Utility"; // Graphic, Logic, Audio, Utility

    // Step 2: Configuration
    public string? TargetId { get; set; } // null = Universal
    public string? ModuleId { get; set; } // null = Standalone
    public bool IsSingleton { get; set; }
    public bool RequiresProject { get; set; }

    // Step 3: Parameters
    public List<ToolParameter> Parameters { get; set; } = new();

    // Output
    public string OutputPath { get; set; } = string.Empty;
}

/// <summary>
/// Tool parameter definition.
/// </summary>
public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, int, bool, file, folder
    public string Description { get; set; } = string.Empty;
}
