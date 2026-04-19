

using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.PixelArtEditor;

/// <summary>
/// Integrated pixel art editor for creating and editing sprites and tiles.
/// </summary>
public class PixelArtEditorTool : ITool
{
    public string ToolId => "retruxel.tool.pixelarteditor";
    public string DisplayName => "Pixel Art Editor";
    public string Description => "Create and edit sprites and tiles with an integrated pixel art editor";
    public object? Icon => null;
    public string Category => "Graphics";
    public string? Shortcut => "Ctrl+Shift+P";
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => true;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // TODO: Implement pixel art editor
        throw new NotImplementedException("Pixel Art Editor is not yet implemented.");
    }
}
