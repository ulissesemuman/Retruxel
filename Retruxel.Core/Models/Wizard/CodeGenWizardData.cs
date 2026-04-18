namespace Retruxel.Core.Models.Wizard;

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
