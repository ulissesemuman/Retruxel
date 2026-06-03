using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Retruxel.Services;

/// <summary>
/// Helper service to invoke visual tools for modules.
/// Handles opening the tool window, passing context, and processing results via connectors.
/// </summary>
public static class VisualToolInvoker
{
    private static ToolRegistry? _toolRegistry;

    /// <summary>
    /// Initializes the invoker with a tool registry.
    /// Must be called before OpenVisualTool.
    /// </summary>
    public static void Initialize(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    /// <summary>
    /// Opens a visual tool for a module.
    /// If the module has no visual tool, returns false.
    /// Updates the module instance in memory directly.
    /// </summary>
    public static bool OpenVisualTool(
        IModule module,
        ITarget target,
        RetruxelProject project,
        string projectPath,
        SceneData scene,
        SceneElementData? existingElement = null,
        Func<Task>? saveProjectCallback = null,
        object? sceneEditor = null)
    {
        // Check if module has a visual tool
        if (string.IsNullOrEmpty(module.VisualToolId))
            return false;

        if (_toolRegistry is null)
        {
            MessageBox.Show(
                "ToolRegistry not initialized. Call VisualToolInvoker.Initialize() first.",
                "Retruxel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }

        // Get the visual tool
        var visualTool = _toolRegistry.GetVisualTool(module.VisualToolId);
        if (visualTool is null)
        {
            MessageBox.Show(
                $"Visual tool '{module.VisualToolId}' not found. Make sure the tool plugin is installed.",
                "Retruxel",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        // Prepare input for the tool
        var input = new Dictionary<string, object>
        {
            ["target"] = target,
            ["project"] = project,
            ["projectPath"] = projectPath,
            ["scene"] = scene,
            ["module"] = module,
            ["toolRegistry"] = _toolRegistry
        };

        if (existingElement is not null)
        {
            input["existingElement"] = existingElement;
            input["elementId"] = existingElement.ElementId;

            // Extract module data from existing element
            if (existingElement.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                existingElement.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                var rawJson = existingElement.ModuleState.GetRawText();
                System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Loading existing element '{existingElement.ElementId}'");
                System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Raw JSON: {rawJson}");

                var existingModuleData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(
                    rawJson);
                if (existingModuleData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Deserialized keys: {string.Join(", ", existingModuleData.Keys)}");
                    input["moduleData"] = existingModuleData;
                }
            }
        }

        if (saveProjectCallback is not null)
            input["saveProjectCallback"] = saveProjectCallback;

        if (sceneEditor is not null)
            input["sceneEditor"] = sceneEditor;

        // Create and show the window
        var window = visualTool.CreateWindow(input);
        if (window is not Window wpfWindow)
        {
            MessageBox.Show(
                $"Visual tool '{module.VisualToolId}' did not return a valid WPF Window.",
                "Retruxel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }

        wpfWindow.Owner = Application.Current.MainWindow;
        var result = wpfWindow.ShowDialog();

        // If user cancelled, do nothing
        if (result != true)
            return false;

        // Extract ModuleData from window via reflection
        var moduleDataProp = window.GetType().GetProperty("ModuleData");
        if (moduleDataProp is null)
        {
            MessageBox.Show(
                $"Visual tool window does not expose a ModuleData property.",
                "Retruxel",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var moduleData = moduleDataProp.GetValue(window) as Dictionary<string, object>;
        if (moduleData is null || moduleData.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] ModuleData is null or empty!");
            return false;
        }

        System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Received module data, keys: {string.Join(", ", moduleData.Keys)}");

        // Log tiles size if present
        if (moduleData.ContainsKey("tiles") && moduleData["tiles"] is int[] tiles)
        {
            System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] tiles array length: {tiles.Length}");
            int nonEmpty = tiles.Count(t => t >= 0);
            System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Non-empty tiles: {nonEmpty}");
        }

        // Update the module instance in memory directly
        var moduleJson = System.Text.Json.JsonSerializer.Serialize(moduleData);
        module.Deserialize(moduleJson);
        System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Updated module instance in memory");

        // Update the SceneElementData.ModuleState with the new module state
        if (existingElement is not null)
        {
            var updatedModuleJson = module.Serialize();
            existingElement.ModuleState = System.Text.Json.JsonDocument.Parse(updatedModuleJson).RootElement.Clone();
            System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Updated SceneElementData.ModuleState");
        }

        // Trigger save callback to persist memory state to disk
        if (saveProjectCallback != null)
        {
            System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Calling saveProjectCallback");
            _ = saveProjectCallback.Invoke();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] No saveProjectCallback provided");
        }

        return true;
    }

    /// <summary>
    /// Opens the tilemap editor for a typed PlaneLayerData.
    /// Persists changes back to the layer's AssetId and tile data.
    /// </summary>
    public static bool OpenTilemapEditor(
        PlaneLayerData layer,
        ITarget target,
        RetruxelProject project,
        string projectPath,
        SceneData scene,
        Func<Task>? saveProjectCallback = null,
        object? sceneEditor = null)
    {
        if (_toolRegistry is null) return false;

        var visualTool = _toolRegistry.GetVisualTool("plane_editor");
        if (visualTool is null) return false;

        // Find the PlaneData that owns this layer so the editor knows which PlaneSpecs to use
        var planeData = scene.Planes.FirstOrDefault(p => p.Layers.Contains(layer));

        var input = new Dictionary<string, object>
        {
            ["target"]      = target,
            ["project"]     = project,
            ["projectPath"] = projectPath,
            ["scene"]       = scene,
            ["layer"]       = layer,
            ["toolRegistry"]= _toolRegistry
        };

        if (planeData is not null)           input["planeData"]           = planeData;
        if (saveProjectCallback is not null) input["saveProjectCallback"] = saveProjectCallback;
        if (sceneEditor is not null)         input["sceneEditor"]         = sceneEditor;

        var window = visualTool.CreateWindow(input);
        if (window is not Window wpfWindow) return false;

        wpfWindow.Owner = Application.Current.MainWindow;
        var result = wpfWindow.ShowDialog();
        if (result != true) return false;

        // Persist layer data from window
        var moduleDataProp = window.GetType().GetProperty("ModuleData");
        if (moduleDataProp?.GetValue(window) is Dictionary<string, object> moduleData)
        {
            if (moduleData.TryGetValue("tilesAssetId", out var assetIdObj))
                layer.AssetId = assetIdObj?.ToString() ?? layer.AssetId;

            // Update layer dimensions so the preview renders at the correct size.
            if (moduleData.TryGetValue("mapWidth", out var wObj))
                layer.Width = Convert.ToInt32(wObj);
            if (moduleData.TryGetValue("mapHeight", out var hObj))
                layer.Height = Convert.ToInt32(hObj);

            // PaletteSlot belongs to PlaneData (the hardware plane), not to the layer.
            if (planeData is not null && moduleData.TryGetValue("paletteSlot", out var slotObj))
                planeData.PaletteSlot = Convert.ToInt32(slotObj);

            // Rebuild Tiles from tiles if present.
            // tiles is an object[] of anonymous objects {tileIndex, flipH, flipV, rotation}.
            // Serialize to JSON first so TileEntry's JsonPropertyName attributes are respected.
            if (moduleData.TryGetValue("tiles", out var tilesObj))
            {
                var opts    = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var json    = System.Text.Json.JsonSerializer.Serialize(tilesObj);
                var entries = System.Text.Json.JsonSerializer.Deserialize<List<TileEntry>>(json, opts);
                if (entries is not null)
                    layer.Tiles = entries;
            }
        }

        saveProjectCallback?.Invoke();
        return true;
    }

    /// <summary>
    /// Opens the sprite editor for a PrefabData.
    /// Persists the selected asset back to prefab.SpriteAssetId.
    /// </summary>
    public static bool OpenSpriteEditorForPrefab(
        PrefabData prefab,
        ITarget target,
        RetruxelProject project,
        string projectPath,
        SceneData? scene,
        Func<Task>? saveProjectCallback = null)
    {
        if (_toolRegistry is null) return false;

        var visualTool = _toolRegistry.GetVisualTool("sprite_editor");
        if (visualTool is null) return false;

        // Use first scene or an empty one for context
        var contextScene = scene ?? project.Scenes.FirstOrDefault() ?? new SceneData();

        var input = new Dictionary<string, object>
        {
            ["target"]       = target,
            ["project"]      = project,
            ["projectPath"]  = projectPath,
            ["scene"]        = contextScene,
            ["toolRegistry"] = _toolRegistry
        };

        if (saveProjectCallback is not null) input["saveProjectCallback"] = saveProjectCallback;

        var window = visualTool.CreateWindow(input);
        if (window is not Window wpfWindow) return false;

        wpfWindow.Owner = Application.Current.MainWindow;
        var result = wpfWindow.ShowDialog();
        if (result != true) return false;

        var moduleDataProp = window.GetType().GetProperty("ModuleData");
        if (moduleDataProp?.GetValue(window) is Dictionary<string, object> moduleData)
        {
            if (moduleData.TryGetValue("tilesetAssetId", out var assetIdObj) &&
                assetIdObj is string assetId &&
                !string.IsNullOrEmpty(assetId))
            {
                prefab.SpriteAssetId = assetId;
            }
        }

        saveProjectCallback?.Invoke();
        return true;
    }

    /// <summary>
    /// Opens the sprite editor for a typed EntityData.
    /// Persists changes back to the entity's SpriteAssetId.
    /// </summary>
    public static bool OpenSpriteEditor(
        EntityData entity,
        ITarget target,
        RetruxelProject project,
        string projectPath,
        SceneData scene,
        Func<Task>? saveProjectCallback = null,
        object? sceneEditor = null)
    {
        if (_toolRegistry is null) return false;

        var visualTool = _toolRegistry.GetVisualTool("sprite_editor");
        if (visualTool is null) return false;

        var input = new Dictionary<string, object>
        {
            ["target"]      = target,
            ["project"]     = project,
            ["projectPath"] = projectPath,
            ["scene"]       = scene,
            ["entity"]      = entity,
            ["toolRegistry"]= _toolRegistry
        };

        if (saveProjectCallback is not null) input["saveProjectCallback"] = saveProjectCallback;
        if (sceneEditor is not null)         input["sceneEditor"]         = sceneEditor;

        var window = visualTool.CreateWindow(input);
        if (window is not Window wpfWindow) return false;

        wpfWindow.Owner = Application.Current.MainWindow;
        var result = wpfWindow.ShowDialog();
        if (result != true) return false;

        // Persist entity data from window.
        // SpriteEditorWindow exposes ModuleData (not EntityData).
        // Extract tilesetAssetId from ModuleData and write it back to entity.SpriteAssetId.
        var moduleDataProp = window.GetType().GetProperty("ModuleData");
        if (moduleDataProp?.GetValue(window) is Dictionary<string, object> moduleData)
        {
            if (moduleData.TryGetValue("tilesetAssetId", out var assetIdObj) &&
                assetIdObj is string assetId &&
                !string.IsNullOrEmpty(assetId))
            {
                entity.SpriteAssetId = assetId;
            }
        }

        saveProjectCallback?.Invoke();
        return true;
    }
}
