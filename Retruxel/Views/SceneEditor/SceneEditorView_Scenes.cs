using Retruxel.Core.Models;
using Retruxel.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Partial class handling scene tabs — manages scene creation, deletion, renaming,
/// and switching between scenes.
/// </summary>
public partial class SceneEditorView
{
    /// <summary>
    /// Creates a new scene with auto-generated name.
    /// </summary>
    private void BtnNewScene_Click(object sender, RoutedEventArgs e)
    {
        if (_project is null) return;

        var index = _project.Scenes.Count + 1;
        var newName = $"Scene {index}";
        while (_project.Scenes.Any(s => s.SceneName == newName))
        {
            index++;
            newName = $"Scene {index}";
        }

        var scene = new SceneData { SceneId = Guid.NewGuid().ToString(), SceneName = newName };

        // Create state change for new scene (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Create scene '{newName}'",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _project.Scenes.Add(scene);
                RebuildSceneTabs();
                ActivateScene(scene);
            },
            IsUndoable = false // Scene creation not undoable (would need to track all elements)
        };

        _stateManager?.ApplyChange(change);
    }

    /// <summary>
    /// Rebuilds the scene tab strip from the current project's scene list.
    /// Called on project load and after adding/removing/renaming scenes.
    /// </summary>
    private void RebuildSceneTabs()
    {
        if (_project is null) return;

        SceneTabsPanel.Children.Clear();

        foreach (var scene in _project.Scenes)
            SceneTabsPanel.Children.Add(BuildSceneTab(scene));
    }

    /// <summary>
    /// Builds a single scene tab. Double-click or right-click → Rename/Delete.
    /// </summary>
    private FrameworkElement BuildSceneTab(SceneData scene)
    {
        var isActive = _currentScene?.SceneId == scene.SceneId;

        var border = new Border
        {
            Padding = new Thickness(16, 0, 16, 0),
            Cursor = Cursors.Hand,
            Tag = scene.SceneId,
            BorderThickness = new Thickness(0, 0, 0, isActive ? 2 : 0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        border.SetResourceReference(Border.BorderBrushProperty, "BrushPrimary");
        border.SetResourceReference(Border.BackgroundProperty,
            isActive ? "BrushSurfaceContainerHigh" : "BrushSurfaceContainerLow");

        border.Child = BuildLabelForTab(scene);

        // Single click → activate, double click → rename
        border.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
                StartInlineRename(border, scene);
            else
                ActivateScene(scene);
        };

        // Right-click context menu: Rename / Delete
        var menu = new ContextMenu();
        var menuRename = new MenuItem { Header = "Rename" };
        var menuDelete = new MenuItem { Header = "Delete" };

        menuRename.Click += (_, _) => StartInlineRename(border, scene);
        menuDelete.Click += (_, _) => DeleteScene(scene);

        // Disable Delete when only one scene remains
        if (_project!.Scenes.Count <= 1)
            menuDelete.IsEnabled = false;

        menu.Items.Add(menuRename);
        menu.Items.Add(menuDelete);
        border.ContextMenu = menu;

        return border;
    }

    /// <summary>
    /// Builds the label TextBlock for a scene tab.
    /// Active scene is highlighted with primary color.
    /// </summary>
    private TextBlock BuildLabelForTab(SceneData scene)
    {
        var isActive = _currentScene?.SceneId == scene.SceneId;
        var label = new TextBlock
        {
            Text = scene.SceneName,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.SetResourceReference(TextBlock.StyleProperty, "TextLabel");
        if (isActive)
            label.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        return label;
    }

    /// <summary>
    /// Activates a scene — clears the canvas and reloads elements for the selected scene.
    /// </summary>
    private void ActivateScene(SceneData scene)
    {
        // Save current scene state before switching
        SyncProjectModules();

        // Switch active scene
        _currentScene = scene;

        // Clear canvas and element list
        _elements.Clear();
        SceneCanvas.Children.Clear();
        SelectElement(null);

        // Clear events panel
        LoadEvents();

        // Clear undo stack — history is per-session, not per-scene
        _undoRedo.Clear();

        // Reload elements for the new scene
        LoadFromProject();

        // Refresh palette — singletons already on canvas must be hidden
        RefreshModulePalette();

        // Rebuild tabs to reflect active state
        RebuildSceneTabs();
    }

    /// <summary>
    /// Starts inline rename — replaces tab label with a TextBox.
    /// Confirms on Enter or focus loss, cancels on Esc.
    /// </summary>
    private void StartInlineRename(Border tab, SceneData scene)
    {
        var textBox = new TextBox
        {
            Text = scene.SceneName,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 60,
            MaxWidth = 160
        };
        textBox.SetResourceReference(TextBox.StyleProperty, "RetruxelTextBox");
        textBox.SelectAll();

        tab.Child = textBox;
        textBox.Focus();

        void Confirm()
        {
            var newName = textBox.Text.Trim();
            if (string.IsNullOrEmpty(newName)
                || _project!.Scenes.Any(s => s.SceneId != scene.SceneId && s.SceneName == newName))
            {
                tab.Child = BuildLabelForTab(scene); // revert
                return;
            }

            var oldName = scene.SceneName;

            // Create state change for renaming scene (Small change — marks dirty only)
            var change = new StateChange
            {
                Description = $"Rename scene '{oldName}' to '{newName}'",
                Type = ChangeType.Small,
                Execute = () =>
                {
                    scene.SceneName = newName;
                    RebuildSceneTabs();
                },
                IsUndoable = false // Scene rename not undoable (low priority)
            };

            _stateManager?.ApplyChange(change);
        }

        textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Return) { Confirm(); e.Handled = true; }
            if (e.Key == Key.Escape) { tab.Child = BuildLabelForTab(scene); e.Handled = true; }
        };
        textBox.LostFocus += (_, _) => Confirm();
    }

    /// <summary>
    /// Deletes a scene. No-op when only one scene remains.
    /// </summary>
    private void DeleteScene(SceneData scene)
    {
        if (_project is null || _project.Scenes.Count <= 1) return;

        // Create state change for deleting scene (Large change — auto-saves)
        var change = new StateChange
        {
            Description = $"Delete scene '{scene.SceneName}'",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _project.Scenes.Remove(scene);

                if (_currentScene?.SceneId == scene.SceneId)
                    ActivateScene(_project.Scenes[0]);
                else
                    RebuildSceneTabs();
            },
            IsUndoable = false // Scene deletion not undoable
        };

        _stateManager?.ApplyChange(change);
    }
}
