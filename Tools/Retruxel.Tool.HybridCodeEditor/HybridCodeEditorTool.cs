namespace Retruxel.Tool.HybridCodeEditor;

/// <summary>
/// Hybrid code editor allowing manual C/assembly code injection alongside visual modules.
/// </summary>
public class HybridCodeEditorTool : ITool
{
    public string ToolId => "retruxel.tool.hybridcodeeditor";
    public string DisplayName => "Hybrid Code Editor";
    public string Description => "Edit generated C code or inject custom assembly alongside visual modules";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.HybridCodeEditor;component/icon.png";
    public string Category => "Advanced";
    public string MenuPath => "Tools/Advanced/Hybrid Code Editor";
    public string? Shortcut => "Ctrl+Shift+H";
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement hybrid code editor
        throw new NotImplementedException("Hybrid Code Editor is not yet implemented.");
    }
}
