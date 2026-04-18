namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Live link connection to hardware or emulator for real-time debugging and hot-reload.
/// </summary>
public class LiveLinkTool : ITool
{
    public string ToolId => "retruxel.tool.livelink";
    public string DisplayName => "Live Link";
    public string Description => "Real-time debugging and hot-reload with hardware or emulator";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.LiveLink;component/icon.png";
    public string Category => "Debug";
    public string MenuPath => "Tools/Debug/Live Link";
    public string? Shortcut => "Ctrl+Shift+L";
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement live link
        throw new NotImplementedException("Live Link is not yet implemented.");
    }
}
