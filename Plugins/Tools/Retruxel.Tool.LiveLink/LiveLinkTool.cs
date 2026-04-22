using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Live link connection to hardware or emulator for real-time debugging and asset capture.
/// </summary>
public class LiveLinkTool : ITool
{
    public string ToolId => "retruxel.tool.livelink";
    public string DisplayName => "Live Link";
    public string Description => "Capture tiles and nametables from emulator in real-time";
    public object? Icon => null;
    public string Category => "Debug";
    public string? Shortcut => "Ctrl+Shift+L";
    public bool IsStandalone => true;
    public string? TargetId => null;
    public bool RequiresProject => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var window = new LiveLinkWindow(input);
        var result = window.ShowDialog();
        
        if (result == true && window.ModuleData != null)
        {
            return window.ModuleData;
        }
        
        return new Dictionary<string, object>();
    }
}
