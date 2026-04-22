using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Core.Connectors;
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
    /// </summary>
    public static bool OpenVisualTool(
        IModule module,
        ITarget target,
        RetruxelProject project,
        string projectPath,
        SceneData scene,
        SceneElementData? existingElement = null)
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
            ["module"] = module
        };

        if (existingElement is not null)
            input["existingElement"] = existingElement;

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

        // Use SceneModuleConnector to save the result
        var connector = new SceneModuleConnector();
        var toolOutput = new Dictionary<string, object> { ["moduleData"] = moduleData };
        var context = new ToolExecutionContext(
            Core.Models.ExecutionContext.InProject,
            toolOutput,
            project,
            scene);

        connector.Connect(toolOutput, context);
        return true;
    }
}
