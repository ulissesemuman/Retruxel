namespace Retruxel.Tool.AudioEditor;

/// <summary>
/// Audio editor for creating music and sound effects for retro sound chips.
/// </summary>
public class AudioEditorTool : ITool
{
    public string ToolId => "retruxel.tool.audioeditor";
    public string DisplayName => "Audio Editor";
    public string Description => "Create music and sound effects for retro sound chips (PSG, APU)";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.AudioEditor;component/icon.png";
    public string Category => "Audio";
    public string MenuPath => "Tools/Audio/Audio Editor";
    public string? Shortcut => "Ctrl+Shift+M";
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement audio editor
        throw new NotImplementedException("Audio Editor is not yet implemented.");
    }
}
