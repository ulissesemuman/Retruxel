using Retruxel.Core.Interfaces;
using System.Collections.Generic;

namespace Retruxel.Tool.PrefabEditor;

/// <summary>
/// ITool stub for PrefabEditor.
/// The window is opened programmatically by SceneEditorView — not via tool discovery.
/// This stub is required so the post-build step copies the DLL to Plugins/Tools/.
/// </summary>
public class PrefabEditorTool : ITool
{
    public string ToolId        => "prefab_editor";
    public string DisplayName   => "Prefab Editor";
    public string Description   => "Visual editor for configuring Prefabs — sprite, actions and input mapping.";
    public object? Icon         => null;
    public string Category      => "Visual Editors";
    public string? Shortcut     => null;
    public bool IsStandalone    => false;
    public string? TargetId     => null;
    public bool RequiresProject => true;

    public Dictionary<string, object> GetDefaultParameters() => new();
    public Dictionary<string, object> Execute(Dictionary<string, object> input) => new();
}
