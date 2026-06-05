using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.PrefabEditor;

public partial class PrefabEditorWindow
{
    private void PopulateInputMapping()
    {
        var hasMapping = _prefab.InputMapping is not null;

        BtnToggleInput.Content = hasMapping ? "✕ DISABLE INPUT" : "+ ENABLE INPUT";
        InputMappingPanel.Visibility = hasMapping ? Visibility.Visible : Visibility.Collapsed;

        if (!hasMapping) return;

        // Suspend port change handling while we populate — otherwise SelectionChanged
        // fires during SelectedIndex assignment and clears the existing ButtonMappings.
        _suppressPortSelectionChanged = true;
        try
        {
            // Port selector
            CmbPort.Items.Clear();
            foreach (var port in _project.InputPorts)
            {
                CmbPort.Items.Add(new ComboBoxItem
                {
                    Content = port.Label,
                    Tag     = port.Id
                });
            }

            var portIndex = _project.InputPorts
                .FindIndex(p => p.Id == _prefab.InputMapping!.PortId);
            CmbPort.SelectedIndex = Math.Max(0, portIndex);
        }
        finally
        {
            _suppressPortSelectionChanged = false;
        }

        RebuildButtonMappingRows();
    }

    private void CmbPort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Suppressed during PopulateInputMapping to avoid clearing existing ButtonMappings
        // when SelectedIndex is set programmatically while populating the combo.
        if (_suppressPortSelectionChanged) return;

        if (_prefab.InputMapping is null) return;
        if (CmbPort.SelectedItem is ComboBoxItem item && item.Tag is string portId)
        {
            _prefab.InputMapping.PortId = portId;
            _prefab.InputMapping.ButtonMappings.Clear();
            RebuildButtonMappingRows();
        }
    }

    private void BtnToggleInput_Click(object sender, RoutedEventArgs e)
    {
        if (_prefab.InputMapping is not null)
        {
            _prefab.InputMapping = null;
        }
        else
        {
            var defaultPortId = _project.InputPorts.FirstOrDefault()?.Id ?? "port1";
            _prefab.InputMapping = new PrefabInputMapping { PortId = defaultPortId };
        }
        PopulateInputMapping();
    }

    private void RebuildButtonMappingRows()
    {
        ButtonMappingPanel.Children.Clear();
        if (_prefab.InputMapping is null) return;

        var port = _project.InputPorts
            .FirstOrDefault(p => p.Id == _prefab.InputMapping.PortId);
        if (port is null) return;

        foreach (var button in port.Buttons)
        {
            var row = BuildButtonMappingRow(button.Id, button.Label);
            ButtonMappingPanel.Children.Add(row);
        }
    }

    private UIElement BuildButtonMappingRow(string buttonId, string buttonLabel)
    {
        var mapping = _prefab.InputMapping!;
        if (!mapping.ButtonMappings.TryGetValue(buttonId, out var instanceIds))
            instanceIds = [];

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        // Row: label + chips + [+ add]
        var row = new WrapPanel { Orientation = Orientation.Horizontal };

        // Button label
        var lbl = new TextBlock
        {
            Text              = buttonLabel,
            FontFamily        = new FontFamily("Consolas"),
            FontSize          = 11,
            Foreground        = (Brush)FindResource("BrushOnSurfaceVariant"),
            VerticalAlignment = VerticalAlignment.Center,
            Width             = 60,
            Margin            = new Thickness(0, 0, 8, 0)
        };
        row.Children.Add(lbl);

        // Connector line visual
        var line = new System.Windows.Shapes.Rectangle
        {
            Height            = 1,
            Width             = 16,
            Fill              = (Brush)FindResource("BrushSurfaceContainerHighest"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(0, 0, 8, 0)
        };
        row.Children.Add(line);

        // Existing action chips
        foreach (var instanceId in instanceIds.ToList())
        {
            var instance = _prefab.Actions.FirstOrDefault(a => a.InstanceId == instanceId);
            if (instance is null) continue;

            var chip = BuildActionChip(instance, buttonId, () =>
            {
                mapping.ButtonMappings[buttonId].Remove(instanceId);
                if (mapping.ButtonMappings[buttonId].Count == 0)
                    mapping.ButtonMappings.Remove(buttonId);
                RebuildButtonMappingRows();
            });
            row.Children.Add(chip);
        }

        // [+ add] button
        var addBtn = new Button
        {
            Content           = "+ add",
            Style             = (Style)FindResource("ButtonGhost"),
            FontSize          = 10,
            Height            = 24,
            Padding           = new Thickness(6, 0, 6, 0),
            Margin            = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        addBtn.Click += (_, _) =>
        {
            // Inline popup: list available actions in this prefab
            var menu = new ContextMenu { PlacementTarget = addBtn };

            var available = _prefab.Actions
                .Where(a => !(mapping.ButtonMappings.TryGetValue(buttonId, out var ids)
                              && ids.Contains(a.InstanceId)))
                .ToList();

            if (available.Count == 0)
            {
                menu.Items.Add(new MenuItem
                {
                    Header    = "No actions available",
                    IsEnabled = false
                });
            }
            else
            {
                foreach (var action in available)
                {
                    var a = action; // capture
                    var item = new MenuItem { Header = a.ActionId };
                    item.Click += (_, _) =>
                    {
                        if (!mapping.ButtonMappings.ContainsKey(buttonId))
                            mapping.ButtonMappings[buttonId] = [];
                        mapping.ButtonMappings[buttonId].Add(a.InstanceId);
                        RebuildButtonMappingRows();
                    };
                    menu.Items.Add(item);
                }
            }

            menu.IsOpen = true;
        };

        row.Children.Add(addBtn);
        panel.Children.Add(row);
        return panel;
    }

    private Border BuildActionChip(ActionInstance instance, string buttonId, Action onRemove)
    {
        var chip = new Border
        {
            Background      = new SolidColorBrush(Color.FromRgb(0x26, 0x26, 0x26)),
            Padding         = new Thickness(6, 2, 4, 2),
            Margin          = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var inner = new StackPanel { Orientation = Orientation.Horizontal };

        inner.Children.Add(new TextBlock
        {
            Text              = instance.ActionId,
            FontFamily        = new FontFamily("Consolas"),
            FontSize          = 10,
            Foreground        = (Brush)FindResource("BrushOnSurface"),
            VerticalAlignment = VerticalAlignment.Center
        });

        var removeBtn = new TextBlock
        {
            Text              = " ×",
            FontSize          = 10,
            Cursor            = System.Windows.Input.Cursors.Hand,
            Foreground        = (Brush)FindResource("BrushOnSurfaceVariant"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(4, 0, 0, 0)
        };
        removeBtn.MouseLeftButtonDown += (_, e) => { e.Handled = true; onRemove(); };
        inner.Children.Add(removeBtn);

        chip.Child = inner;
        return chip;
    }
}
