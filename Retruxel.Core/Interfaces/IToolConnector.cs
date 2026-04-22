using Retruxel.Core.Models;

namespace Retruxel.Core.Interfaces;

/// <summary>
/// Interprets tool output and decides what to do with it (file, scene, another tool, etc.)
/// </summary>
public interface IToolConnector
{
    /// <summary>
    /// Unique identifier for this connector type
    /// </summary>
    string ConnectorId { get; }

    /// <summary>
    /// Connects tool output to its destination
    /// </summary>
    /// <param name="toolOutput">Output dictionary from tool execution</param>
    /// <param name="context">Execution context with project/scene info</param>
    void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context);
}
