using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Exports tool output to a file
/// </summary>
public class FileExportConnector : IToolConnector
{
    public string ConnectorId => "file_export";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        if (toolOutput.TryGetValue("filePath", out var filePathObj) && filePathObj is string filePath)
        {
            ExportToFile(filePath, toolOutput);
        }
        else
        {
            context.AddError("FileExportConnector: No 'filePath' specified in tool output");
        }
    }

    private void ExportToFile(string filePath, Dictionary<string, object> data)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }
}
