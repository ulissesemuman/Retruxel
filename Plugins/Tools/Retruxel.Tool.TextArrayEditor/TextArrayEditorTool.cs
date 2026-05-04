using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Modules.Graphics;

namespace Retruxel.Tool.TextArrayEditor;

/// <summary>
/// Text Array Editor Tool - Visual editor for text array modules.
/// Implements IVisualTool to integrate with the module system.
/// </summary>
public class TextArrayEditorTool : IVisualTool
{
    public string ToolId => "TextArrayEditor";
    public string DisplayName => "Text Array Editor";
    public string Description => "Visual editor for creating and editing multilingual text arrays";
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
        var module = (TextArrayModule)input["module"];
        var projectPath = (string)input["projectPath"];

        var window = new TextArrayEditorWindow(module, projectPath);

        return window;
    }
}
