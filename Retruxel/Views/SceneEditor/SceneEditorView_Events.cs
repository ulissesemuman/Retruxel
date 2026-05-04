using System.Windows.Controls;

namespace Retruxel.Views;

/// <summary>
/// Partial class handling events panel — manages OnStart, OnVBlank, OnInput triggers
/// and displays module actions for each event.
/// </summary>
public partial class SceneEditorView
{
    /// <summary>
    /// Initializes the events panel with all available triggers.
    /// Called on scene load and when switching scenes.
    /// </summary>
    private void LoadEvents()
    {
        // Events panel removed - functionality moved to structure panel
    }

    /// <summary>
    /// Creates an event block for a specific trigger.
    /// Each block shows "WHEN [trigger]" and a list of module actions.
    /// </summary>
    private void AddEventBlock(string trigger)
    {
        // Events panel removed - functionality moved to structure panel
    }

    /// <summary>
    /// Shows module picker dialog for adding actions to an event.
    /// Currently adds text.display directly — will become a picker dialog.
    /// </summary>
    private void ShowModulePicker(string trigger)
    {
        // For now, directly add text.display — will become a picker dialog
        AddModuleToEvent(trigger, "text.display");
    }

    /// <summary>
    /// Adds a module action to an event block.
    /// Creates a new element if not provided, or uses existing element during load.
    /// </summary>
    private void AddModuleToEvent(string trigger, string moduleId,
        SceneElement? existingElement = null)
    {
        // Events panel removed - functionality moved to structure panel
    }

    /// <summary>
    /// Builds a single event action row for the events panel.
    /// Shows category icon, module label, and remove button.
    /// </summary>
    private Border BuildEventAction(SceneElement element)
    {
        // Events panel removed - functionality moved to structure panel
        return new Border();
    }




}
