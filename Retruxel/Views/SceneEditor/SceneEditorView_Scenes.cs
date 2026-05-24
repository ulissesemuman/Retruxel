using Retruxel.Core.Models;
using Retruxel.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Retruxel.Views;

/// <summary>
/// Scene tab strip management — create, rename, delete, switch, set-initial.
/// Tabs are kept in the top bar; the tree left panel shows the active scene expanded.
/// </summary>
public partial class SceneEditorView
{
    private void BtnNewScene_Click(object? sender = null, RoutedEventArgs? e = null)
    {
        if (_project is null || _target is null) return;

        var index   = _project.Scenes.Count + 1;
        var newName = $"Scene {index}";
        while (_project.Scenes.Any(s => s.SceneName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            index++;
            newName = $"Scene {index}";
        }

        var scene = new SceneData { SceneId = Guid.NewGuid().ToString(), SceneName = newName };
        InitializePaletteSlots(scene, _target);

        var change = new StateChange
        {
            Description = $"Create scene '{newName}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                _project.Scenes.Add(scene);
                RebuildSceneTabs();
                ActivateScene(scene);
            },
            IsUndoable = false
        };

        _stateManager?.ApplyChange(change);
    }

    private void RebuildSceneTabs()
    {
        if (_project is null) return;

        SceneTabsPanel.Children.Clear();

        foreach (var scene in _project.Scenes)
            SceneTabsPanel.Children.Add(BuildSceneTab(scene));
    }

    private FrameworkElement BuildSceneTab(SceneData scene)
    {
        var isActive = _currentScene?.SceneId == scene.SceneId;

        var border = new Border
        {
            Padding           = new Thickness(16, 0, 16, 0),
            Cursor            = Cursors.Hand,
            Tag               = scene.SceneId,
            BorderThickness   = new Thickness(0, 0, 0, isActive ? 2 : 0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        border.SetResourceReference(Border.BorderBrushProperty, "BrushPrimary");
        border.SetResourceReference(Border.BackgroundProperty,
            isActive ? "BrushSurfaceContainerHigh" : "BrushSurfaceContainerLow");

        border.Child = BuildLabelForTab(scene);

        border.MouseLeftButtonDown += (_, ev) =>
        {
            if (ev.ClickCount == 2) StartInlineRename(border, scene);
            else                    ActivateScene(scene);
        };

        var menu         = new ContextMenu();
        var menuRename   = new MenuItem { Header = "Rename" };
        var menuInitial  = new MenuItem { Header = "Set as Initial Scene",
            IsEnabled = _project!.InitialSceneId != scene.SceneId };
        var menuDelete   = new MenuItem { Header = "Delete",
            IsEnabled = _project!.Scenes.Count > 1 };

        menuRename.Click  += (_, _) => StartInlineRename(border, scene);
        menuInitial.Click += (_, _) => SetInitialScene(scene);
        menuDelete.Click  += (_, _) => DeleteScene(scene);

        menu.Items.Add(menuRename);
        menu.Items.Add(menuInitial);
        menu.Items.Add(menuDelete);
        border.ContextMenu = menu;

        return border;
    }

    private TextBlock BuildLabelForTab(SceneData scene)
    {
        var isActive = _currentScene?.SceneId == scene.SceneId;
        var label = new TextBlock
        {
            Text              = scene.SceneName,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.SetResourceReference(TextBlock.StyleProperty, "TextLabel");
        if (isActive)
            label.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        return label;
    }

    internal void ActivateScene(SceneData scene)
    {
        _currentScene = scene;
        SceneCanvas.Children.Clear();
        SelectItem(null);
        _undoRedo.Clear();
        MigrateScene(scene, _target!);

        RebuildProjectTree();
        RebuildSceneTabs();
        RefreshPreview();
    }

    private void StartInlineRename(Border tab, SceneData scene)
    {
        var textBox = new TextBox
        {
            Text              = scene.SceneName,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth          = 60,
            MaxWidth          = 160
        };
        textBox.SetResourceReference(TextBox.StyleProperty, "RetruxelTextBox");
        textBox.SelectAll();

        tab.Child = textBox;
        textBox.Focus();

        void Confirm()
        {
            var newName = textBox.Text.Trim();
            if (string.IsNullOrEmpty(newName) ||
                _project!.Scenes.Any(s => s.SceneId != scene.SceneId &&
                    s.SceneName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                tab.Child = BuildLabelForTab(scene);
                return;
            }

            var change = new StateChange
            {
                Description = $"Rename scene to '{newName}'",
                Type        = ChangeType.Small,
                Execute     = () => { scene.SceneName = newName; RebuildSceneTabs(); RebuildProjectTree(); },
                IsUndoable  = false
            };
            _stateManager?.ApplyChange(change);
        }

        textBox.KeyDown  += (_, ev) =>
        {
            if (ev.Key == Key.Return) { Confirm(); ev.Handled = true; }
            if (ev.Key == Key.Escape) { tab.Child = BuildLabelForTab(scene); ev.Handled = true; }
        };
        textBox.LostFocus += (_, _) => Confirm();
    }

    private void SetInitialScene(SceneData scene)
    {
        if (_project is null || _project.InitialSceneId == scene.SceneId) return;

        var change = new StateChange
        {
            Description = $"Set '{scene.SceneName}' as initial scene",
            Type        = ChangeType.Small,
            Execute     = () => { _project.InitialSceneId = scene.SceneId; RebuildSceneTabs(); RebuildProjectTree(); },
            IsUndoable  = false
        };
        _stateManager?.ApplyChange(change);
    }

    internal void DeleteScene(SceneData scene)
    {
        if (_project is null || _project.Scenes.Count <= 1) return;

        var change = new StateChange
        {
            Description = $"Delete scene '{scene.SceneName}'",
            Type        = ChangeType.Large,
            Execute     = () =>
            {
                _project.Scenes.Remove(scene);
                if (_currentScene?.SceneId == scene.SceneId)
                    ActivateScene(_project.Scenes[0]);
                else
                    { RebuildSceneTabs(); RebuildProjectTree(); }
            },
            IsUndoable = false
        };
        _stateManager?.ApplyChange(change);
    }
}
