using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        // Built-in modules from ModuleRegistry
        foreach (var (moduleId, module) in _moduleRegistry.LogicModules)
        {
            // Skip obsolete palette module
            if (moduleId == "palette") continue;

            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var policy = _moduleRegistry.GetModulePolicy(moduleId);
            var isDisabled = policy != SingletonPolicy.Multiple && currentCount >= 1;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        foreach (var (moduleId, module) in _moduleRegistry.GraphicModules)
        {
            // Skip obsolete palette module
            if (moduleId == "palette") continue;

            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var policy = _moduleRegistry.GetModulePolicy(moduleId);
            var isDisabled = policy != SingletonPolicy.Multiple && currentCount >= 1;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        foreach (var (moduleId, module) in _moduleRegistry.AudioModules)
        {
            var m = (IModule)module;
            var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
            var policy = _moduleRegistry.GetModulePolicy(moduleId);
            var isDisabled = policy != SingletonPolicy.Multiple && currentCount >= 1;
            modules.Add((m.Category, moduleId, m.DisplayName, isDisabled));
        }

        // User modules from CodeGens
        if (_moduleRenderer is not null)
        {
            var userModules = _moduleRenderer.GetUserModules()
                .Where(um => um.TargetId == _project?.TargetId);

            foreach (var (moduleId, displayName, category, _) in userModules)
            {
                var currentCount = instanceCounts.GetValueOrDefault(moduleId, 0);
                // User modules are always Multiple by default
                modules.Add(("User Modules", moduleId, displayName, false));
            }
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
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(8, 0, 8, 4),
            Cursor = isDisabled ? Cursors.Arrow : Cursors.Hand,
            Tag = moduleId,
            Opacity = isDisabled ? 0.5 : 1.0
        };
        item.SetResourceReference(Border.BackgroundProperty, "BrushSurfaceContainerHigh");

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        var indicator = new TextBlock
        {
            Text = "⬛ ",
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        indicator.SetResourceReference(TextBlock.ForegroundProperty,
            isDisabled ? "BrushError" : "BrushPrimary");
        panel.Children.Add(indicator);

        var label = new TextBlock
        {
            Text = displayName,
            Style = (Style)FindResource("TextBody"),
            VerticalAlignment = VerticalAlignment.Center
        };
        label.SetResourceReference(TextBlock.ForegroundProperty,
            isDisabled ? "BrushOnSurfaceVariant" : "BrushOnSurface");
        panel.Children.Add(label);

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
