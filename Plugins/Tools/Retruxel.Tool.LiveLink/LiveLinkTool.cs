

using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Live link connection to hardware or emulator for real-time debugging and hot-reload.
/// </summary>
public class LiveLinkTool : ITool
{
    public string ToolId => "retruxel.tool.livelink";
    public string DisplayName => "Live Link";
    public string Description => "Real-time debugging and hot-reload with hardware or emulator";
    public object? Icon => null;
    public string Category => "Debug";
    public string? Shortcut => "Ctrl+Shift+L";
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => true;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // TODO: Implement live link
        throw new NotImplementedException("Live Link is not yet implemented.");
    }
}
