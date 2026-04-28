using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        EventsPanel.Children.Clear();

        var triggers = new[] { "OnStart", "OnVBlank", "OnInput_Button1", "OnInput_Button2" };

        foreach (var trigger in triggers)
            AddEventBlock(trigger);
    }

    /// <summary>
    /// Creates an event block for a specific trigger.
    /// Each block shows "WHEN [trigger]" and a list of module actions.
    /// </summary>
    private void AddEventBlock(string trigger)
    {
        var loc = LocalizationService.Instance;
        var block = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 4),
            Tag = trigger
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var whenLabel = new TextBlock
        {
            Text = loc.Get("scene.event.when"),
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
            FontFamily = new FontFamily("Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var triggerLabel = new TextBlock
        {
            Text = trigger,
            FontSize = 10,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var actionsPanel = new StackPanel
        {
            Tag = $"actions_{trigger}"
        };

        var addAction = new TextBlock
        {
            Text = loc.Get("scene.event.add_action"),
            Style = (Style)FindResource("TextLabel"),
            Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 4, 0, 0)
        };
        addAction.MouseLeftButtonDown += (s, e) => ShowModulePicker(trigger);

        Grid.SetColumn(whenLabel, 0);
        Grid.SetColumn(triggerLabel, 1);

        grid.Children.Add(whenLabel);
        grid.Children.Add(triggerLabel);

        var outerPanel = new StackPanel();
        outerPanel.Children.Add(grid);
        outerPanel.Children.Add(actionsPanel);
        outerPanel.Children.Add(addAction);

        block.Child = outerPanel;

        // Allow drop from module palette
        block.AllowDrop = true;
        block.Drop += (s, e) =>
        {
            if (e.Data.GetData(typeof(string)) is string moduleId)
                AddModuleToEvent(trigger, moduleId);
        };

        EventsPanel.Children.Add(block);
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
        var element = existingElement ?? CreateSceneElement(moduleId, 0, 0);
        element.Trigger = trigger;

        if (existingElement is null)
            _elements.Add(element);

        // Find the actions panel for this trigger
        foreach (Border block in EventsPanel.Children)
        {
            if (block.Tag?.ToString() != trigger) continue;
            if (block.Child is not StackPanel outer) continue;

            var actionsPanel = outer.Children.OfType<StackPanel>()
                .FirstOrDefault(p => p.Tag?.ToString() == $"actions_{trigger}");

            if (actionsPanel is null) continue;

            var action = BuildEventAction(element);
            actionsPanel.Children.Add(action);
            break;
        }

        SelectElement(element);

        // Only mark dirty if this is a new element, not during load
        if (existingElement is null)
            SyncProjectModules();
    }

    /// <summary>
    /// Builds a single event action row for the events panel.
    /// Shows category icon, module label, and remove button.
    /// </summary>
    private Border BuildEventAction(SceneElement element)
    {
        var action = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 2, 0, 2),
            Cursor = Cursors.Hand,
            Tag = element
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Category icon
        var icon = new TextBlock
        {
            Text = "⬛",
            FontSize = 10,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(GetCategoryColor(element))
        };

        var label = new TextBlock
        {
            Text = GetModuleDisplayText(element),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var remove = new TextBlock
        {
            Text = "✕",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAD, 0xAA, 0xAA)),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        remove.MouseLeftButtonDown += (s, e) =>
        {
            RemoveElement(element);
            e.Handled = true;
        };

        Grid.SetColumn(icon, 0);
        Grid.SetColumn(label, 1);
        Grid.SetColumn(remove, 2);
        grid.Children.Add(icon);
        grid.Children.Add(label);
        grid.Children.Add(remove);
        action.Child = grid;

        action.MouseLeftButtonDown += (s, e) =>
        {
            SelectElement(element);
            e.Handled = true;
        };

        element.EventVisual = action;
        return action;
    }




}
