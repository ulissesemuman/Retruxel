using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Services;

/// <summary>
/// Orchestrates tool execution with connectors
/// </summary>
public class ToolExecutor
{
    private readonly ConnectorRegistry _connectorRegistry;

    public ToolExecutor(ConnectorRegistry connectorRegistry)
    {
        _connectorRegistry = connectorRegistry;
    }

    /// <summary>
    /// Executes a tool and applies connectors to its output
    /// </summary>
    public ToolExecutionResult Execute(
        ITool tool,
        Dictionary<string, object> input,
        Models.ExecutionContext context = Models.ExecutionContext.InProject,
        IEnumerable<IToolConnector>? connectors = null,
        RetruxelProject? currentProject = null,
        SceneData? currentScene = null)
    {
        try
        {
            // 1. Execute tool
            var output = tool.Execute(input);

            // 2. Resolve connectors (use override or defaults)
            var activeConnectors = connectors ?? _connectorRegistry.GetDefaults(context);

            // 3. Apply connectors
            var execContext = new ToolExecutionContext(context, output, currentProject, currentScene);

            foreach (var connector in activeConnectors)
            {
                try
                {
                    connector.Connect(output, execContext);
                }
                catch (Exception ex)
                {
                    execContext.AddError($"Connector '{connector.ConnectorId}' failed: {ex.Message}");
                }
            }

            return ToolExecutionResult.FromContext(execContext);
        }
        catch (Exception ex)
        {
            return ToolExecutionResult.Error($"Tool execution failed: {ex.Message}");
        }
    }
}
