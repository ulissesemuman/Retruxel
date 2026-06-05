using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Connector that creates or updates a palette module override in the current scene.
/// Used for standalone palette creation (not called from TilemapEditor).
/// </summary>
public class PaletteToModuleConnector : IToolConnector
{
    public string ConnectorId => "palette_to_module";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
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

        // Generate a unique palette module ID from existing scene ModuleOverrides
        var existingIds = context.CurrentProject.Scenes
            .SelectMany(s => s.ModuleOverrides)
            .Where(m => m.ModuleId.StartsWith("palette"))
            .Select(m => m.ModuleId)
            .ToHashSet();

        int paletteIndex = 0;
        string paletteId;
        do
        {
            paletteId = $"palette_{paletteIndex}";
            paletteIndex++;
        } while (existingIds.Contains(paletteId));

        var moduleData = new Dictionary<string, object>
        {
            ["name"]   = toolOutput["name"],
            ["colors"] = toolOutput["colors"]
        };

        var state = JsonDocument.Parse(
            JsonSerializer.Serialize(moduleData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })).RootElement.Clone();

        context.CurrentScene.ModuleOverrides.Add(new ProjectModuleData
        {
            ModuleId = paletteId,
            Label    = toolOutput["name"].ToString()!,
            Enabled  = true,
            State    = state
        });

        context.ChainResult(new Dictionary<string, object>
        {
            ["paletteId"]   = paletteId,
            ["paletteName"] = toolOutput["name"]
        });
    }
}
