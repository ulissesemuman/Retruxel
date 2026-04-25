using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Feeds tool output as input to another tool (chaining)
/// </summary>
public class ToolFeedConnector : IToolConnector
{
    private readonly ITool _targetTool;
    private readonly Dictionary<string, string>? _mapping;

    public string ConnectorId => "tool_feed";

    /// <summary>
    /// Creates a connector that feeds output to another tool
    /// </summary>
    /// <param name="targetTool">Tool to receive the output</param>
    /// <param name="mapping">Optional mapping of output keys to input keys. If null, passes all keys directly</param>
    public ToolFeedConnector(ITool targetTool, Dictionary<string, string>? mapping = null)
    {
        _targetTool = targetTool;
        _mapping = mapping;
    }

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        try
        {
            var mappedInput = MapOutput(toolOutput);
            var result = _targetTool.Execute(mappedInput);
            context.ChainResult(result);
        }
        catch (Exception ex)
        {
            context.AddError($"ToolFeedConnector: Failed to execute target tool '{_targetTool.ToolId}': {ex.Message}");
        }
    }

    private Dictionary<string, object> MapOutput(Dictionary<string, object> output)
    {
        if (_mapping == null || _mapping.Count == 0)
        {
            return new Dictionary<string, object>(output);
        }

        var mapped = new Dictionary<string, object>();

        foreach (var (outputKey, inputKey) in _mapping)
        {
            if (output.TryGetValue(outputKey, out var value))
            {
                mapped[inputKey] = value;
            }
        }

        return mapped;
    }
}
