namespace Retruxel.Core.Models;

/// <summary>
/// Context passed to connectors during tool execution
/// </summary>
public class ToolExecutionContext
{
    public ExecutionContext Context { get; }
    public Dictionary<string, object> ToolOutput { get; }
    public List<Dictionary<string, object>> ChainedResults { get; } = new();
    public List<string> Errors { get; } = new();
    public RetruxelProject? CurrentProject { get; }
    public SceneData? CurrentScene { get; }

    public ToolExecutionContext(
        ExecutionContext context,
        Dictionary<string, object> toolOutput,
        RetruxelProject? currentProject = null,
        SceneData? currentScene = null)
    {
        Context = context;
        ToolOutput = toolOutput;
        CurrentProject = currentProject;
        CurrentScene = currentScene;
    }

    /// <summary>
    /// Adds a result from a chained tool execution
    /// </summary>
    public void ChainResult(Dictionary<string, object> result)
    {
        ChainedResults.Add(result);
    }

    /// <summary>
    /// Adds an error message from a connector
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}
