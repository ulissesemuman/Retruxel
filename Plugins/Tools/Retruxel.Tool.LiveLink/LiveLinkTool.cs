using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Live link connection to hardware or emulator for real-time debugging and asset capture.
/// </summary>
public class LiveLinkTool : IVisualTool
{
    public string ToolId => "livelink";
    public string DisplayName => "Live Link";
    public string Description => "Capture tiles and nametables from emulator in real-time";
    public object? Icon => null;
    public string Category => "Debug";
    public string? Shortcut => "Ctrl+Shift+L";
    public bool IsStandalone => true;
    public string? TargetId => null;
    public bool RequiresProject => false;
    public bool HasUI => true;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Visual tools use CreateWindow() instead
        return new Dictionary<string, object>();
    }

    public object CreateWindow(Dictionary<string, object> input)
    {
        return new LiveLinkWindow(input);
    }
}
