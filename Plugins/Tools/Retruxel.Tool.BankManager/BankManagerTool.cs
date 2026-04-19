

namespace Retruxel.Tool.BankManager;

/// <summary>
/// Manages ROM banking and memory mapping for large projects.
/// </summary>
public class BankManagerTool : ITool
{
    public string ToolId => "retruxel.tool.bankmanager";
    public string DisplayName => "Bank Manager";
    public string Description => "Manage ROM banking and memory mapping for large projects";
    public string IconPath => "pack://application:,,,/Retruxel.Tool.BankManager;component/icon.png";
    public string Category => "Advanced";
    public string MenuPath => "Tools/Advanced/Bank Manager";
    public string? Shortcut => null;
    public bool RequiresProject => true;
    public string? TargetId => null;

    public bool Execute(IToolContext context)
    {
        // TODO: Implement bank manager
        throw new NotImplementedException("Bank Manager is not yet implemented.");
    }
}
