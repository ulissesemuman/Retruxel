namespace Retruxel.Core.Models.Wizard;

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
