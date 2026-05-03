using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Tool.SpriteEditor;

/// <summary>
/// Sprite Editor Tool - Visual editor for sprite/metasprite modules.
/// Implements IVisualTool to integrate with the module system.
/// </summary>
public class SpriteEditorTool : IVisualTool
{
    public string ToolId => "sprite_editor";
    public string DisplayName => "Sprite Editor";
    public string Description => "Visual editor for creating and editing sprites and metasprites";
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
        var target = (ITarget)input["target"];
        var project = (RetruxelProject)input["project"];
        var projectPath = (string)input["projectPath"];
        var toolRegistry = input.ContainsKey("toolRegistry") ? (Core.Services.ToolRegistry)input["toolRegistry"] : null;
        var saveProjectCallback = input.ContainsKey("saveProjectCallback") ? (Func<System.Threading.Tasks.Task>)input["saveProjectCallback"] : null;
        var sceneEditor = input.ContainsKey("sceneEditor") ? input["sceneEditor"] : null;

        // Optional: module instance data (when editing existing module)
        var moduleData = input.ContainsKey("moduleData")
            ? (Dictionary<string, object>)input["moduleData"]
            : null;

        var window = new SpriteEditorWindow(target, project, projectPath, toolRegistry, saveProjectCallback, sceneEditor);

        // If editing existing module, load its data
        if (moduleData != null)
        {
            window.LoadModuleData(moduleData);
        }

        return window;
    }
}
