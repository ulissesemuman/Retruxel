using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Tool.Wizard.Views;

namespace Retruxel.Tool.Wizard;

/// <summary>
/// Wizard tool for creating Targets, CodeGens, and Tools with guided step-by-step interface.
/// Generates boilerplate code, JSON manifests, and .rtrx templates.
/// </summary>
public class WizardTool : ITool
{
    public string ToolId => "retruxel.wizard";
    public string DisplayName => "Creation Wizard";
    public string Description => "Step-by-step wizard to create new Targets, CodeGens, and Tools";
    public string Category => "Utility";
    public string? TargetId => null; // Universal
    public string? ModuleId => null; // Standalone
    public object? Icon => null; // TODO: Add wizard icon
    public string? Shortcut => "Ctrl+Shift+W";
    public bool IsStandalone => true;
    public bool IsSingleton => true;
    public bool RequiresProject => false;

    public IEnumerable<string> Validate(Dictionary<string, object> input)
    {
        // Wizard doesn't need input validation - it's a standalone UI tool
        return Enumerable.Empty<string>();
    }

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Open the wizard window
        var window = new WizardMainWindow();
        window.ShowDialog();

        // Wizard doesn't return output - it generates files directly
        return new Dictionary<string, object>();
    }
}
