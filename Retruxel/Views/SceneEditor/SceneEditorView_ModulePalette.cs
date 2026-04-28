using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Retruxel.Core.Interfaces;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    private void LoadModulePalette(ITarget target)
    {
        RefreshModulePalette();
    }

    private void RefreshModulePalette()
    {
        ModulePalettePanel.Children.Clear();

        if (_moduleRegistry is null) return;

        var instanceCounts = _elements
            .GroupBy(e => e.ModuleId)
            .ToDictionary(g => g.Key, g => g.Count());

        var modules = new List<(string Category, string ModuleId, string DisplayName, bool IsDisabled)>();

        foreach (var (moduleId, module) in _moduleRegistry.LogicModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var maxInstances = _moduleRegistry.GetMaxInstances(moduleId);
            var isDisabled = maxInstances.HasValue && currentCount >= maxInstances.Value;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        foreach (var (moduleId, module) in _moduleRegistry.GraphicModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var maxInstances = _moduleRegistry.GetMaxInstances(moduleId);
            var isDisabled = maxInstances.HasValue && currentCount >= maxInstances.Value;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        foreach (var (moduleId, module) in _moduleRegistry.AudioModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var maxInstances = _moduleRegistry.GetMaxInstances(moduleId);
            var isDisabled = maxInstances.HasValue && currentCount >= maxInstances.Value;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        var grouped = modules.GroupBy(m => m.Category);

        foreach (var group in grouped)
        {
            var header = new TextBlock
            {
                Text = group.Key,
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(12, 8, 12, 4)
            };
            ModulePalettePanel.Children.Add(header);

            foreach (var module in group)
            {
                var item = BuildModulePaletteItem(module.ModuleId, module.DisplayName, module.IsDisabled);
                ModulePalettePanel.Children.Add(item);
            }
        }
    }

    private Border BuildModulePaletteItem(string moduleId, string displayName, bool isDisabled)
    {
        var item = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(8, 0, 8, 4),
            Cursor = isDisabled ? Cursors.Arrow : Cursors.Hand,
            Tag = moduleId,
            Opacity = isDisabled ? 0.5 : 1.0
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        panel.Children.Add(new TextBlock
        {
            Text = "⬛ ",
            Foreground = new SolidColorBrush(isDisabled 
                ? Color.FromRgb(0xFF, 0x52, 0x52)
                : Color.FromRgb(0x8E, 0xFF, 0x71)),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = displayName,
            Style = (Style)FindResource("TextBody"),
            Foreground = isDisabled 
                ? new SolidColorBrush(Color.FromRgb(0x76, 0x75, 0x75))
                : Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        });

        item.Child = panel;

        if (!isDisabled)
        {
            item.MouseMove += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    DragDrop.DoDragDrop(item, moduleId, DragDropEffects.Copy);
                }
            };
        }

        return item;
    }
}
