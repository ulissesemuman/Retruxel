namespace Retruxel.Core.Models;

/// <summary>
/// Result of a tool execution with connectors applied
/// </summary>
public record ToolExecutionResult(
    bool Success,
    Dictionary<string, object> Output,
    List<Dictionary<string, object>> ChainedResults,
    List<string> Errors,
    string? ErrorMessage = null)
{
    public static ToolExecutionResult Cancelled => new(
        false,
        new Dictionary<string, object>(),
        new List<Dictionary<string, object>>(),
        new List<string>(),
        "Operation cancelled by user");

    public static ToolExecutionResult Error(string message) => new(
        false,
        new Dictionary<string, object>(),
        new List<Dictionary<string, object>>(),
        new List<string>(),
        message);

    public static ToolExecutionResult FromContext(ToolExecutionContext context) => new(
        context.Errors.Count == 0,
        context.ToolOutput,
        context.ChainedResults,
        context.Errors);
}
