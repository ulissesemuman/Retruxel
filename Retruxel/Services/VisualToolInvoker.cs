using Retruxel.Core.Connectors;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
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
            return false;

        System.Diagnostics.Debug.WriteLine($"[VisualToolInvoker] Received module data, keys: {string.Join(", ", moduleData.Keys)}");
        
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
}
