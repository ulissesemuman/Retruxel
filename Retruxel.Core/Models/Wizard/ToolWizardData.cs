namespace Retruxel.Core.Models.Wizard;

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
