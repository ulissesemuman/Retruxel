namespace Retruxel.Tool.PixelArtEditor;

/// <summary>
/// Integrated pixel art editor for creating and editing sprites and tiles.
/// </summary>
public class PixelArtEditorTool : ITool
{
    public string ToolId => "retruxel.tool.pixelarteditor";
    public string DisplayName => "Pixel Art Editor";
    public string Description => "Create and edit sprites and tiles with an integrated pixel art editor";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.PixelArtEditor;component/icon.png";
    public string Category => "Graphics";
    public string MenuPath => "Tools/Graphics/Pixel Art Editor";
    public string? Shortcut => "Ctrl+Shift+P";
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement pixel art editor
        throw new NotImplementedException("Pixel Art Editor is not yet implemented.");
    }
}
