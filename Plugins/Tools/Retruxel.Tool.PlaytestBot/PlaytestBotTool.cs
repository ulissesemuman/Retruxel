

using Retruxel.Core.Interfaces;

namespace Retruxel.Tool.PlaytestBot;

/// <summary>
/// Automated playtesting bot for finding bugs and balance issues.
/// </summary>
public class PlaytestBotTool : ITool
{
    public string ToolId => "retruxel.tool.playtestbot";
    public string DisplayName => "Playtest Bot";
    public string Description => "Automated playtesting bot for finding bugs and balance issues";
    public object? Icon => null;
    public string Category => "AI";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => true;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // TODO: Implement playtest bot
        throw new NotImplementedException("Playtest Bot is not yet implemented.");
    }
}
