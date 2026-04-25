using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Connector that creates a Palette module in the project.
/// Used for standalone palette creation (not called from TilemapEditor).
/// </summary>
public class PaletteToModuleConnector : IToolConnector
{
    public string ConnectorId => "palette_to_module";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        // Validate output
        if (!toolOutput.ContainsKey("name") || !toolOutput.ContainsKey("colors"))
        {
            context.AddError("Palette output missing required fields: name, colors");
            return;
        }

        if (context.CurrentProject == null || context.CurrentScene == null)
        {
            context.AddError("No active project or scene to save palette module");
            return;
        }

        // Generate unique palette ID
        var existingPalettes = context.CurrentProject.Scenes
            .SelectMany(s => s.Elements)
            .Where(e => e.ModuleId == "palette")
            .Select(e => e.ElementId)
            .ToHashSet();

        int paletteIndex = 0;
        string paletteId;
        do
        {
            paletteId = $"palette_{paletteIndex}";
            paletteIndex++;
        } while (existingPalettes.Contains(paletteId));

        // Create palette module
        var moduleData = new Dictionary<string, object>
        {
            ["name"] = toolOutput["name"],
            ["colors"] = toolOutput["colors"]
        };

        var paletteElement = new SceneElementData
        {
            ElementId = paletteId,
            UserId = toolOutput["name"].ToString()!,
            ModuleId = "palette",
            ModuleState = JsonDocument.Parse(JsonSerializer.Serialize(moduleData)).RootElement.Clone(),
            TileX = 0,
            TileY = 0,
            Trigger = "OnStart"
        };

        context.CurrentScene.Elements.Add(paletteElement);

        // Return created palette ID
        context.ChainResult(new Dictionary<string, object>
        {
            ["paletteId"] = paletteId,
            ["paletteName"] = toolOutput["name"]
        });
    }
}
