using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Retruxel.Core.Connectors;

/// <summary>
/// Creates or updates a module override in the current scene with tool output.
/// Used when visual tools (like TilemapEditor) return module data.
/// </summary>
public class SceneModuleConnector : IToolConnector
{
    public string ConnectorId => "scene_module";

    public void Connect(Dictionary<string, object> toolOutput, ToolExecutionContext context)
    {
        System.Diagnostics.Debug.WriteLine($"[SceneModuleConnector] Connect called with keys: {string.Join(", ", toolOutput.Keys)}");
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

        System.Diagnostics.Debug.WriteLine($"[SceneModuleConnector] ModuleId: {moduleId}");

        var stateJson = JsonSerializer.Serialize(toolOutput, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var state = JsonDocument.Parse(stateJson).RootElement.Clone();

        System.Diagnostics.Debug.WriteLine($"[SceneModuleConnector] Serialized state: {stateJson}");

        // Update existing override or create a new one
        var existing = context.CurrentScene.ModuleOverrides
            .FirstOrDefault(m => m.ModuleId == moduleId);

        if (existing != null)
        {
            System.Diagnostics.Debug.WriteLine($"[SceneModuleConnector] Updating existing override: {moduleId}");
            existing.State = state;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[SceneModuleConnector] Creating new module override: {moduleId}");
            context.CurrentScene.ModuleOverrides.Add(new ProjectModuleData
            {
                ModuleId = moduleId,
                Label    = moduleId,
                Enabled  = true,
                State    = state
            });
        }
    }
}
