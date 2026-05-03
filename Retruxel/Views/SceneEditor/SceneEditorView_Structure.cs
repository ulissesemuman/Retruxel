using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    private void RefreshStructurePanel()
    {
        StructurePanel.Children.Clear();

        if (_project is null || _currentScene is null) return;

        // PROJECT section
        AddStructureHeader("PROJECT");

        // Scenes
        AddStructureSubheader("Scenes");
        foreach (var scene in _project.Scenes)
        {
            var isInitial = scene.SceneId == _project.InitialSceneId;
            var sceneItem = CreateStructureItem(
                isInitial ? $"\u25CF {scene.SceneName} (initial)" : scene.SceneName,
                isInitial ? (Brush)FindResource("BrushPrimary") : (Brush)FindResource("BrushOnSurface"),
                () => { /* TODO: Switch scene */ });
            StructurePanel.Children.Add(sceneItem);
        }
        AddStructureButton("+ Add Scene", BtnNewScene_Click);

        // OnStart Modules
        AddStructureSubheader("OnStart Modules");
        var onStartModules = _elements
            .Where(e => GetModuleDestination(e.ModuleId) == "PROJECT")
            .ToList();
        
        foreach (var element in onStartModules)
        {
            var item = CreateStructureItem(
                $"[{element.UserId ?? element.ModuleId}]",
                (Brush)FindResource("BrushOnSurface"),
                () => SelectElement(element));
            StructurePanel.Children.Add(item);
        }
        AddStructureButton("+ Add", null);

        // Variables
        AddStructureSubheader("Variables");
        var gameVars = _elements
            .Where(e => e.ModuleId == "gamevar")
            .ToList();
        
        foreach (var element in gameVars)
        {
            var varName = element.UserId ?? "var";
            var item = CreateStructureItem(
                $"{varName}: int = 0",
                (Brush)FindResource("BrushOnSurface"),
                () => SelectElement(element));
            StructurePanel.Children.Add(item);
        }
        AddStructureButton("+ Add", null);

        // SCENE section
        AddStructureHeader($"SCENE: {_currentScene.SceneName}");

        // Palette
        AddStructureSubheader("Palette");
        var paletteModules = _elements
            .Where(e => e.ModuleId == "palette")
            .ToList();
        
        foreach (var element in paletteModules)
        {
            var item = CreateStructureItem(
                $"[{element.UserId ?? element.ModuleId}]",
                (Brush)FindResource("BrushOnSurface"),
                () => SelectElement(element));
            StructurePanel.Children.Add(item);
        }

        // Tilemap Layers
        AddStructureSubheader("Tilemap Layers");
        var tilemapModules = _elements
            .Where(e => e.ModuleId == "tilemap")
            .ToList();
        
        foreach (var element in tilemapModules)
        {
            var item = CreateStructureItem(
                $"[{element.UserId ?? element.ModuleId}]",
                (Brush)FindResource("BrushOnSurface"),
                () => SelectElement(element));
            StructurePanel.Children.Add(item);
        }

        // Text (static)
        AddStructureSubheader("Text (static)");
        var textModules = _elements
            .Where(e => e.ModuleId == "text.display")
            .ToList();
        
        foreach (var element in textModules)
        {
            var item = CreateStructureItem(
                $"[{element.UserId ?? element.ModuleId}]",
                (Brush)FindResource("BrushOnSurface"),
                () => SelectElement(element));
            StructurePanel.Children.Add(item);
        }
    }

    private void AddStructureHeader(string text)
    {
        var header = new TextBlock
        {
            Text = text,
            Style = (Style)FindResource("TextLabel"),
            Foreground = (Brush)FindResource("BrushPrimary"),
            Margin = new Thickness(12, 8, 12, 4),
            FontSize = 11
        };
        StructurePanel.Children.Add(header);
    }

    private void AddStructureSubheader(string text)
    {
        var subheader = new TextBlock
        {
            Text = text,
            Style = (Style)FindResource("TextLabel"),
            Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
            Margin = new Thickness(20, 8, 12, 4),
            FontSize = 9
        };
        StructurePanel.Children.Add(subheader);
    }

    private Border CreateStructureItem(string text, Brush foreground, Action onClick)
    {
        var item = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainer"),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(28, 0, 12, 4),
            Cursor = Cursors.Hand
        };

        var textBlock = new TextBlock
        {
            Text = text,
            Style = (Style)FindResource("TextBody"),
            Foreground = foreground,
            FontSize = 10
        };

        item.Child = textBlock;

        item.MouseLeftButtonDown += (s, e) =>
        {
            onClick?.Invoke();
            e.Handled = true;
        };

        return item;
    }

    private void AddStructureButton(string text, RoutedEventHandler? handler)
    {
        var button = new Button
        {
            Content = text,
            Style = (Style)FindResource("ButtonSecondary"),
            Margin = new Thickness(28, 4, 12, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(0, 4),
            FontSize = 9
        };

        if (handler is not null)
            button.Click += handler;

        StructurePanel.Children.Add(button);
    }

    private string GetModuleDestination(string moduleId)
    {
        return moduleId switch
        {
            "palette" => "SCENE",
            "tilemap" => "SCENE",
            "text.display" => "SCENE",
            "input" => "PROJECT",
            "physics" => "PROJECT",
            "sprite" => "PROJECT",
            "animation" => "PROJECT",
            "gamevar" => "PROJECT",
            "entity" or "enemy" or "scroll" => "CANVAS",
            _ => "CANVAS"
        };
    }
}
