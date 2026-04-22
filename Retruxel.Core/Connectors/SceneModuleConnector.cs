using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Creates or updates a module in the current scene with tool output.
/// Used when visual tools (like TilemapEditor) return module data.
/// </summary>
public class SceneModuleConnector : IToolConnector
{
    public string ConnectorId => "scene_module";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        if (context.CurrentScene == null)
        {
            context.AddError("SceneModuleConnector: No active scene in context");
            return;
        }

        if (!toolOutput.TryGetValue("moduleId", out var moduleIdObj) || moduleIdObj is not string moduleId)
        {
            context.AddError("SceneModuleConnector: No 'moduleId' specified in tool output");
            return;
        }

        // Serialize module data to JSON
        string moduleJson = JsonSerializer.Serialize(toolOutput, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var moduleState = JsonDocument.Parse(moduleJson).RootElement.Clone();

        // Check if updating existing module or creating new one
        if (toolOutput.TryGetValue("instanceId", out var instanceIdObj) && instanceIdObj is string instanceId)
        {
            // Update existing module
            var existingElement = context.CurrentScene.Elements
                .FirstOrDefault(e => e.ElementId == instanceId);

            if (existingElement != null)
            {
                existingElement.ModuleState = moduleState;
            }
            else
            {
                context.AddError($"SceneModuleConnector: Element '{instanceId}' not found in scene");
            }
        }
        else
        {
            // Create new element instance
            string newElementId = $"{moduleId}_{context.CurrentScene.Elements.Count(e => e.ModuleId == moduleId)}";
            
            var element = new SceneElementData
            {
                ElementId = newElementId,
                ModuleId = moduleId,
                ModuleState = moduleState,
                TileX = 0,
                TileY = 0,
                Trigger = "OnStart"
            };

            context.CurrentScene.Elements.Add(element);
        }
    }
}
