namespace Retruxel.Core.Services;

/// <summary>
/// Represents a loaded CodeGen manifest with resolved template path.
/// </summary>
internal class CodeGenManifest
{
    public string ModuleId { get; init; } = "";
    public string TargetId { get; init; } = "";
    public string Version { get; init; } = "1.0.0";
    public string TemplatePath { get; init; } = "";
    public bool IsSystemModule { get; init; } = false;
    public Dictionary<string, VariableDefinition> Variables { get; init; } = new();
}

/// <summary>
/// Defines how a variable should be resolved from different sources.
/// </summary>
internal class VariableDefinition
{
    public string Name { get; set; } = "";
    public string From { get; set; } = "module";
    public string? Path { get; set; }
    public string? ToolId { get; set; }
    public bool ParseBool { get; set; }
    public object? Default { get; set; }
    public Dictionary<string, string>? ToolInput { get; set; }
    public string? GroupBy { get; set; }
    public string? Transform { get; set; }
}
