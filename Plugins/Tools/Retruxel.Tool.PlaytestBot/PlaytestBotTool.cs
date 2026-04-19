

namespace Retruxel.Tool.PlaytestBot;

/// <summary>
/// Automated playtesting bot for finding bugs and balance issues.
/// </summary>
public class PlaytestBotTool : ITool
{
    public string ToolId => "retruxel.tool.playtestbot";
    public string DisplayName => "Playtest Bot";
    public string Description => "Automated playtesting bot for finding bugs and balance issues";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.PlaytestBot;component/icon.png";
    public string Category => "AI";
    public string MenuPath => "Tools/AI/Playtest Bot";
    public string? Shortcut => null;
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement playtest bot
        throw new NotImplementedException("Playtest Bot is not yet implemented.");
    }
}
