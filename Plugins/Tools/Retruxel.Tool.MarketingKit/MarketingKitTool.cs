namespace Retruxel.Tool.MarketingKit;

/// <summary>
/// Export and marketing kit generator for promotional materials.
/// </summary>
public class MarketingKitTool : ITool
{
    public string ToolId => "retruxel.tool.marketingkit";
    public string DisplayName => "Marketing Kit";
    public string Description => "Generate promotional materials (screenshots, GIFs, press kit)";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.MarketingKit;component/icon.png";
    public string Category => "Export";
    public string MenuPath => "Tools/Export/Marketing Kit";
    public string? Shortcut => null;
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement marketing kit
        throw new NotImplementedException("Marketing Kit is not yet implemented.");
    }
}
