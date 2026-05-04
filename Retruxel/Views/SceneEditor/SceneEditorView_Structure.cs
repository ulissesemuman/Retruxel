using Retruxel.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    private void RefreshStructurePanel()
    {
        StructurePanel.Children.Clear();

        if (_project is null || _currentScene is null || _moduleRegistry is null) return;

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
            .Where(e => GetModuleScope(e.ModuleId) == ModuleScope.Project)
            .ToList();

        if (onStartModules.Count > 0)
        {
            foreach (var element in onStartModules)
            {
                var moduleName = element.UserId ?? element.ModuleId;
                var item = CreateStructureItemWithDelete(
                    moduleName,
                    (Brush)FindResource("BrushOnSurface"),
                    () => OpenOnStartModuleEditor(element),
                    () => RemoveElement(element));
                StructurePanel.Children.Add(item);
            }
        }
        AddStructureButton("+ Add", BtnAddOnStartModule_Click);

        // Variables
        AddStructureSubheader("Variables");
        var gameVars = _elements
            .Where(e => e.ModuleId == "gamevar")
            .ToList();

        if (gameVars.Count > 0)
        {
            foreach (var element in gameVars)
            {
                var varName = element.UserId ?? "var";
                var item = CreateStructureItem(
                    $"{varName}: int = 0",
                    (Brush)FindResource("BrushOnSurface"),
                    () => SelectElement(element));
                StructurePanel.Children.Add(item);
            }
        }
        AddStructureButton("+ Add", BtnAddVariable_Click);

        // SCENE section
        AddStructureHeader($"SCENE: {_currentScene.SceneName}");

        // Palette Slots
        AddStructureSubheader("Palette");
        if (_currentScene.PaletteSlots != null && _currentScene.PaletteSlots.Count > 0)
        {
            for (int i = 0; i < _currentScene.PaletteSlots.Count; i++)
            {
                var slot = _currentScene.PaletteSlots[i];
                var slotType = _target?.GetPaletteSlotType(i).ToString() ?? "Unknown";
                var label = string.IsNullOrEmpty(slot.Label) ? slotType : slot.Label;
                var item = CreatePaletteSlotItem(slot, label, i);
                StructurePanel.Children.Add(item);
            }
        }
        else
        {
            var noSlotsText = new TextBlock
            {
                Text = "No palette slots",
                Margin = new Thickness(28, 4, 12, 4),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
            };
            StructurePanel.Children.Add(noSlotsText);
        }

        // Tilemap Layers
        AddStructureSubheader("Tilemap Layers");
        var tilemapModules = _elements
            .Where(e => e.ModuleId == "tilemap")
            .ToList();

        if (tilemapModules.Count > 0)
        {
            foreach (var element in tilemapModules)
            {
                var layerName = element.UserId ?? "Layer 0";
                var item = CreateStructureItemWithDelete(
                    layerName,
                    (Brush)FindResource("BrushOnSurface"),
                    () => OpenVisualToolForElement(element),
                    () => RemoveElement(element));
                StructurePanel.Children.Add(item);
            }
        }
        else
        {
            var emptyText = new TextBlock
            {
                Text = "No tilemap layers",
                Margin = new Thickness(28, 4, 12, 4),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
            };
            StructurePanel.Children.Add(emptyText);
        }

        // Text (static)
        AddStructureSubheader("Text (static)");
        var textModules = _elements
            .Where(e => e.ModuleId == "text.display")
            .ToList();

        if (textModules.Count > 0)
        {
            foreach (var element in textModules)
            {
                var textLabel = element.UserId ?? "Text";
                var item = CreateStructureItemWithDelete(
                    textLabel,
                    (Brush)FindResource("BrushOnSurface"),
                    () => OpenVisualToolForElement(element),
                    () => RemoveElement(element));
                StructurePanel.Children.Add(item);
            }
        }
        else
        {
            var emptyText = new TextBlock
            {
                Text = "No text elements",
                Margin = new Thickness(28, 4, 12, 4),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
            };
            StructurePanel.Children.Add(emptyText);
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
            Background = (Brush)FindResource("BrushSurfaceContainerHigh"),
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

    private Border CreateStructureItemWithDelete(string text, Brush foreground, Action onClick, Action onDelete)
    {
        var item = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainerHigh"),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(28, 0, 12, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textBlock = new TextBlock
        {
            Text = text,
            Style = (Style)FindResource("TextBody"),
            Foreground = foreground,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand
        };
        textBlock.MouseLeftButtonDown += (s, e) =>
        {
            onClick?.Invoke();
            e.Handled = true;
        };
        Grid.SetColumn(textBlock, 0);

        var deleteBtn = new TextBlock
        {
            Text = "✕",
            FontSize = 12,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        deleteBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushError");
        deleteBtn.MouseLeftButtonDown += (s, e) =>
        {
            onDelete?.Invoke();
            e.Handled = true;
        };
        Grid.SetColumn(deleteBtn, 1);

        grid.Children.Add(textBlock);
        grid.Children.Add(deleteBtn);
        item.Child = grid;

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
            Padding = new Thickness(0, 4, 0, 4),
            FontSize = 9
        };

        if (handler is not null)
            button.Click += handler;

        StructurePanel.Children.Add(button);
    }

    private void BtnAddOnStartModule_Click(object sender, RoutedEventArgs e)
    {
        BtnTabModules.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
    }

    private void BtnAddVariable_Click(object sender, RoutedEventArgs e)
    {
        if (_project is null || _currentScene is null) return;

        var elementId = Guid.NewGuid().ToString();
        var element = new SceneElementData
        {
            ElementId = elementId,
            ModuleId = "gamevar",
            ModuleState = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                name = $"var{_elements.Count(e => e.ModuleId == "gamevar") + 1}",
                type = "int",
                initialValue = "0",
                showInHud = false
            })
        };

        _currentScene.Elements.Add(element);
        AddElementFromData(element);
        RefreshStructurePanel();
        SyncProjectModules();
    }

    private FrameworkElement CreatePaletteSlotItem(PaletteSlotData slot, string label, int slotIndex)
    {
        var border = new Border
        {
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(28, 0, 12, 4),
            Cursor = Cursors.Hand
        };
        border.SetResourceReference(Border.BackgroundProperty, "BrushSurfaceContainerHigh");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelBlock = new TextBlock { Text = label, FontSize = 10 };
        labelBlock.SetResourceReference(TextBlock.StyleProperty, "TextBody");
        Grid.SetColumn(labelBlock, 0);

        var colorStrip = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(8, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var hex in slot.Colors.Take(8))
        {
            colorStrip.Children.Add(new Border
            {
                Width = 6,
                Height = 6,
                Background = ParseHexBrush(hex),
                Margin = new Thickness(0, 0, 1, 0)
            });
        }
        Grid.SetColumn(colorStrip, 1);

        var editBtn = new TextBlock
        {
            Text = "EDIT",
            FontSize = 9,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center
        };
        editBtn.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");
        editBtn.MouseLeftButtonDown += (s, e) =>
        {
            e.Handled = true;
            OpenPaletteSlotEditor(slotIndex);
        };
        Grid.SetColumn(editBtn, 2);

        grid.Children.Add(labelBlock);
        grid.Children.Add(colorStrip);
        grid.Children.Add(editBtn);
        border.Child = grid;

        return border;
    }

    private Brush ParseHexBrush(string hex)
    {
        try
        {
            var converter = new BrushConverter();
            var result = converter.ConvertFromString(hex);
            return result as Brush ?? Brushes.Black;
        }
        catch
        {
            return Brushes.Black;
        }
    }

    private void OpenPaletteSlotEditor(int slotIndex)
    {
        if (_currentScene is null || _target is null) return;

        var slot = _currentScene.PaletteSlots[slotIndex];

        var window = new Retruxel.Tool.PaletteEditor.PaletteEditorWindow(_target, slot)
        {
            Owner = Window.GetWindow(this),
            Title = $"Palette — Slot {slotIndex} ({_target.GetPaletteSlotType(slotIndex)})"
        };

        if (window.ShowDialog() == true)
        {
            RefreshStructurePanel();
            SyncProjectModules();
        }
    }

    private void OpenOnStartModuleEditor(SceneElementData element)
    {
        if (element.ModuleId == "text.array")
        {
            OpenVisualToolForElement(element);
        }
        else
        {
            SelectElement(element);
        }
    }
}
