using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Connector that saves imported asset as a standalone .asset JSON file.
/// Used when AssetImporter is called in standalone mode (no project context).
/// </summary>
public class AssetToFileConnector : IToolConnector
{
    public string ConnectorId => "asset_to_file";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        if (!toolOutput.ContainsKey("asset"))
        {
            context.AddError("Asset output missing 'asset' field");
            return;
        }

        var asset = (AssetEntry)toolOutput["asset"];

        // Determine output path
        string outputPath;
        if (toolOutput.ContainsKey("outputPath"))
        {
            outputPath = toolOutput["outputPath"].ToString()!;
        }
        else
        {
            // Default: save next to source image
            var sourceDir = Path.GetDirectoryName(asset.RelativePath) ?? ".";
            outputPath = Path.Combine(sourceDir, $"{asset.Id}.asset");
        }

        try
        {
            // Serialize asset to JSON
            var json = JsonSerializer.Serialize(asset, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(outputPath, json);

            context.ChainResult(new Dictionary<string, object>
            {
                ["assetFilePath"] = outputPath,
                ["assetId"] = asset.Id
            });
        }
        catch (Exception ex)
        {
            context.AddError($"Failed to save asset file: {ex.Message}");
        }
    }
}
