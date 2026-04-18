namespace Retruxel.Tool.VisualScripting;

/// <summary>
/// Graph-based visual scripting system for complex game logic.
/// </summary>
public class VisualScriptingTool : ITool
{
    public string ToolId => "retruxel.tool.visualscripting";
    public string DisplayName => "Visual Scripting";
    public string Description => "Graph-based visual scripting for complex game logic";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.VisualScripting;component/icon.png";
    public string Category => "Logic";
    public string MenuPath => "Tools/Logic/Visual Scripting";
    public string? Shortcut => "Ctrl+Shift+V";
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement visual scripting
        throw new NotImplementedException("Visual Scripting is not yet implemented.");
    }
}
