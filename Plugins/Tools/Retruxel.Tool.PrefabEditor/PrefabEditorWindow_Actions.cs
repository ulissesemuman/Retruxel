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
    private void PopulateActions()
    {
        ActionsPanel.Children.Clear();

        foreach (var instance in _prefab.Actions)
            ActionsPanel.Children.Add(BuildActionRow(instance));
    }

    private Border BuildActionRow(ActionInstance instance)
    {
        var def = _actionRegistry.GetById(instance.ActionId);
        var deps = _actionRegistry.GetAllDependencies(instance.ActionId);

        var row = new Border
        {
            Background   = new SolidColorBrush(Color.FromRgb(0x13, 0x13, 0x13)),
            Padding      = new Thickness(12, 8, 12, 8),
            Margin       = new Thickness(0, 0, 0, 4),
            BorderThickness = new Thickness(2, 0, 0, 0),
            BorderBrush  = (Brush)FindResource("BrushPrimary")
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Left: action id + parameters summary + dependencies
        var info = new StackPanel();

        var idLabel = new TextBlock
        {
            Text       = instance.ActionId.ToUpperInvariant(),
            FontFamily = new FontFamily("Consolas"),
            FontSize   = 11,
            Foreground = (Brush)FindResource("BrushOnSurface")
        };
        info.Children.Add(idLabel);

        if (instance.Parameters.Count > 0)
        {
            var paramsText = string.Join("   ",
                instance.Parameters.Select(p => $"{p.Key}: {p.Value}"));

            var paramsLabel = new TextBlock
            {
                Text       = paramsText,
                FontFamily = new FontFamily("Consolas"),
                FontSize   = 9,
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
                Margin     = new Thickness(0, 2, 0, 0)
            };
            info.Children.Add(paramsLabel);
        }

        // Dependencies section
        if (deps.Count > 0)
        {
            var depsHeader = new TextBlock
            {
                Text       = "DEPENDS ON:",
                FontFamily = new FontFamily("Consolas"),
                FontSize   = 8,
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
                Margin     = new Thickness(0, 6, 0, 2)
            };
            info.Children.Add(depsHeader);

            foreach (var depId in deps)
            {
                var depDef = _actionRegistry.GetById(depId);
                var depRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin      = new Thickness(8, 1, 0, 1)
                };

                var depLabel = new TextBlock
                {
                    Text       = depDef?.DisplayName ?? depId,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize   = 9,
                    Foreground = (Brush)FindResource("BrushPrimary"),
                    Cursor     = System.Windows.Input.Cursors.Hand
                };

                // Click on dependency name — open its ActionParameterDialog
                // (find or create an ActionInstance in the prefab for this dep)
                depLabel.MouseLeftButtonDown += (_, e) =>
                {
                    e.Handled = true;
                    if (depDef is null) return;
                    var depInstance = _prefab.Actions
                        .FirstOrDefault(a => a.ActionId.Equals(depId, StringComparison.OrdinalIgnoreCase));
                    if (depInstance is null)
                    {
                        // Dependency was auto-injected by codegen, not explicitly in prefab.
                        // Create a transient instance so user can preview/edit defaults.
                        depInstance = new Retruxel.Core.Models.ActionInstance
                        {
                            ActionId   = depDef.ActionId,
                            Parameters = depDef.Parameters.ToDictionary(p => p.Name, p => p.Default)
                        };
                        _prefab.Actions.Add(depInstance);
                        PopulateActions();
                    }
                    OpenActionParameterDialog(depInstance, depDef);
                };
                depRow.Children.Add(depLabel);

                if (depDef is not null)
                {
                    var depSub = new TextBlock
                    {
                        Text       = $"  ·  {depDef.Category}",
                        FontFamily = new FontFamily("Consolas"),
                        FontSize   = 9,
                        Foreground = (Brush)FindResource("BrushOnSurfaceVariant")
                    };
                    depRow.Children.Add(depSub);
                }

                info.Children.Add(depRow);
            }
        }

        Grid.SetColumn(info, 0);
        grid.Children.Add(info);

        // Right: ⚙ and ✕ buttons (visible on hover)
        var buttons = new StackPanel
        {
            Orientation       = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity           = 0
        };

        if (def is not null)
        {
            var settingsBtn = MakeLabelButton("⚙", "BrushOnSurfaceVariant", () =>
                OpenActionParameterDialog(instance, def));
            buttons.Children.Add(settingsBtn);
        }

        var removeBtn = MakeLabelButton("✕", "BrushError", () =>
        {
            _prefab.Actions.Remove(instance);
            // Remove any input mappings referencing this instance
            if (_prefab.InputMapping is not null)
            {
                foreach (var key in _prefab.InputMapping.ButtonMappings.Keys.ToList())
                    _prefab.InputMapping.ButtonMappings[key].Remove(instance.InstanceId);
            }
            PopulateActions();
            PopulateInputMapping();
        });
        buttons.Children.Add(removeBtn);

        Grid.SetColumn(buttons, 1);
        grid.Children.Add(buttons);

        row.Child = grid;

        // Show buttons on hover
        row.MouseEnter += (_, _) => buttons.Opacity = 1;
        row.MouseLeave += (_, _) => buttons.Opacity = 0;

        return row;
    }

    private void BtnAddAction_Click(object sender, RoutedEventArgs e)
    {
        var picker = new ActionPickerDialog(_actionRegistry, Owner ?? this);
        if (picker.ShowDialog() == true && picker.SelectedActionId is not null)
        {
            var def = _actionRegistry.GetById(picker.SelectedActionId)!;

            var instance = new ActionInstance
            {
                ActionId   = def.ActionId,
                Parameters = def.Parameters.ToDictionary(
                    p => p.Name,
                    p => p.Default)
            };

            _prefab.Actions.Add(instance);
            PopulateActions();
            PopulateInputMapping(); // Refresh chips — new action available for mapping
        }
    }

    private void OpenActionParameterDialog(ActionInstance instance, ActionDefinition def)
    {
        var dialog = new ActionParameterDialog(instance, def, Owner ?? this);
        if (dialog.ShowDialog() == true)
            PopulateActions(); // Refresh to show updated param values
    }

    private static TextBlock MakeLabelButton(string text, string colorKey, Action onClick)
    {
        var btn = new TextBlock
        {
            Text              = text,
            FontSize          = 12,
            Cursor            = System.Windows.Input.Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Center,
            Margin            = new Thickness(8, 0, 0, 0)
        };
        btn.SetResourceReference(TextBlock.ForegroundProperty, colorKey);
        btn.MouseLeftButtonDown += (_, e) => { e.Handled = true; onClick(); };
        return btn;
    }
}
