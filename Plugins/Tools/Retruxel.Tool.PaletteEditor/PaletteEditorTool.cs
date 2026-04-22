using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Tool.PaletteEditor;

/// <summary>
/// Palette Editor Tool - Visual editor for palette modules.
/// Implements IVisualTool to integrate with the module system.
/// </summary>
public class PaletteEditorTool : IVisualTool
{
    public string ToolId => "palette_editor";
    public string DisplayName => "Palette Editor";
    public string Description => "Visual editor for creating and editing color palettes";
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
        return new Dictionary<string, object>();
    }

    public object CreateWindow(Dictionary<string, object> input)
    {
        var target = (ITarget)input["target"];
        var project = (RetruxelProject)input["project"];
        var projectPath = (string)input["projectPath"];

        if (!input.ContainsKey("paletteProvider"))
        {
            throw new InvalidOperationException(
                $"Target '{target.TargetId}' must provide an IToolExtension that returns 'paletteProvider' key. " +
                "See IPaletteProvider interface in Retruxel.Tool.PaletteEditor.");
        }

        var paletteProvider = (IPaletteProvider)input["paletteProvider"];
        var window = new PaletteEditorWindow(paletteProvider);

        return window;
    }
}
