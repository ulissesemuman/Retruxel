using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Tool.TilemapEditor;

/// <summary>
/// Tilemap Editor Tool - Visual editor for plane modules.
/// Implements IVisualTool to integrate with the module system.
/// </summary>
public class TilemapEditorTool : IVisualTool
{
    public string ToolId => "plane_editor";
    public string DisplayName => "Tilemap Editor";
    public string Description => "Visual editor for creating and editing planes";
    public object? Icon => null;
    public string Category => "Visual Editors";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => true;
    public bool HasUI => true;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Visual tools don't use Execute() - they use CreateWindow()
        // This is here for IToolExtension compatibility
        return new Dictionary<string, object>();
    }

    public object CreateWindow(Dictionary<string, object> input)
    {
        var target      = (ITarget)input["target"];
        var project     = (RetruxelProject)input["project"];
        var projectPath = (string)input["projectPath"];
        var toolRegistry        = input.ContainsKey("toolRegistry")        ? (Core.Services.ToolRegistry)input["toolRegistry"]                        : null;
        var saveProjectCallback = input.ContainsKey("saveProjectCallback") ? (Func<System.Threading.Tasks.Task>)input["saveProjectCallback"]           : null;
        var sceneEditor         = input.ContainsKey("sceneEditor")         ? input["sceneEditor"]                                                      : null;

        // Resolve planeId so the editor uses the correct PlaneSpecs.
        string? planeId = null;
        if (input.TryGetValue("planeData", out var planeDataObj) && planeDataObj is PlaneData pd)
            planeId = pd.PlaneId;

        var window = new TilemapEditorWindow(target, project, projectPath, toolRegistry, saveProjectCallback, sceneEditor, planeId);

        // Build moduleData from PlaneLayerData when opening an existing layer.
        // This is the primary path from VisualToolInvoker.OpenTilemapEditor.
        if (input.TryGetValue("layer", out var layerObj) && layerObj is PlaneLayerData layer)
        {
            var layerModuleData = new Dictionary<string, object>
            {
                ["mapWidth"]     = layer.Width,
                ["mapHeight"]    = layer.Height,
                ["tilesAssetId"] = layer.AssetId ?? string.Empty,
                ["mapData"]      = layer.Tiles.Select(t => new
                {
                    tileIndex = t.TileIndex,
                    flipH     = t.FlipH,
                    flipV     = t.FlipV,
                    rotation  = t.Rotation
                }).ToArray()
            };

            // Carry palette slot from PlaneData if available.
            if (planeDataObj is PlaneData planeData)
                layerModuleData["paletteSlot"] = planeData.PaletteSlot;

            window.LoadModuleData(layerModuleData);
        }
        else if (input.TryGetValue("moduleData", out var mdObj) && mdObj is Dictionary<string, object> moduleData)
        {
            // Legacy path: explicit moduleData dict (used by module-based invocations).
            window.LoadModuleData(moduleData);
        }

        return window;
    }
}
