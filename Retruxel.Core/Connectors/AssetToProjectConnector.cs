using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Connector that adds imported asset to the current project.
/// Used when AssetImporter is called from within a project context.
/// </summary>
public class AssetToProjectConnector : IToolConnector
{
    public string ConnectorId => "asset_to_project";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        if (!toolOutput.ContainsKey("asset"))
        {
            context.AddError("Asset output missing 'asset' field");
            return;
        }

        if (context.CurrentProject == null)
        {
            context.AddError("No active project to add asset");
            return;
        }

        var asset = (AssetEntry)toolOutput["asset"];

        // Check for duplicate ID
        if (context.CurrentProject.Assets.Any(a => a.Id == asset.Id))
        {
            context.AddError($"Asset '{asset.Id}' already exists in project");
            return;
        }

        // Add to project
        context.CurrentProject.Assets.Add(asset);

        // Return asset ID
        context.ChainResult(new Dictionary<string, object>
        {
            ["assetId"] = asset.Id,
            ["assetPath"] = asset.RelativePath
        });
    }
}
