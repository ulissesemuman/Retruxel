using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// CRUD window for project-level game variables.
/// Opened from the VARIABLES section gear icon in the project tree.
/// </summary>
public partial class VariableManagerWindow : Window
{
    private readonly RetruxelProject _project;
    private readonly Action          _saveCallback;

    private GameVariableData? _selected;

    private static readonly string[] CTypeOptions =
        ["uint8_t", "int8_t", "uint16_t", "int16_t", "uint32_t", "int32_t"];

    // ──────────────────────────────────────────────────────────────────────────

    public VariableManagerWindow(RetruxelProject project, Action saveCallback)
    {
        _project      = project;
        _saveCallback = saveCallback;

        InitializeComponent();
        BuildList();
        ClearForm();
        UpdateFormVisibility(false);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    private void BuildList()
    {
        VarListPanel.Children.Clear();

        if (_project.Variables.Count == 0)
        {
            var empty = new TextBlock
            {
                Text    = "No variables yet — click + to add one.",
                FontSize = 11,
                Margin  = new Thickness(8, 12, 8, 0)
            };
            empty.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            VarListPanel.Children.Add(empty);
            return;
        }

        var grouped = _project.Variables
            .GroupBy(v => string.IsNullOrEmpty(v.Group) ? "General" : v.Group)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var groupLabel = new TextBlock
            {
                Text     = group.Key.ToUpperInvariant(),
                FontSize = 9,
                Margin   = new Thickness(4, 10, 4, 2)
            };
            groupLabel.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");
            VarListPanel.Children.Add(groupLabel);

            foreach (var v in group)
            {
                var item     = v;
                var isActive = _selected?.VariableId == v.VariableId;

                var row = new Border
                {
                    Padding       = new Thickness(8, 6, 8, 6),
                    Cursor        = Cursors.Hand,
                    CornerRadius  = new CornerRadius(4),
                    Margin        = new Thickness(0, 1, 0, 1)
                };
                row.SetResourceReference(Border.BackgroundProperty,
                    isActive ? "BrushSurfaceVariant" : "BrushSurface");

                var rowStack = new StackPanel { Orientation = Orientation.Horizontal };

                var nameLbl = new TextBlock
                {
                    Text       = v.Label,
                    FontSize   = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth   = 120
                };
                nameLbl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurface");

                var idLbl = new TextBlock
                {
                    Text    = $"  {v.VariableId}",
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center
                };
                idLbl.SetResourceReference(TextBlock.ForegroundProperty, "BrushOnSurfaceVariant");

                var typeLbl = new TextBlock
                {
                    Text    = $"  [{v.CType.Replace("_t", "")}]",
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center
                };
                typeLbl.SetResourceReference(TextBlock.ForegroundProperty, "BrushPrimary");

                if (v.Persistent)
                {
                    var saveLbl = new TextBlock
                    {
                        Text    = "  💾",
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        ToolTip = "Saved to SRAM"
                    };
                    rowStack.Children.Add(saveLbl);
                }

                rowStack.Children.Add(nameLbl);
                rowStack.Children.Add(idLbl);
                rowStack.Children.Add(typeLbl);
                row.Child = rowStack;

                row.MouseLeftButtonDown += (_, _) => SelectVariable(item);
                VarListPanel.Children.Add(row);
            }
        }
    }

    // ── Selection ─────────────────────────────────────────────────────────────

    private void SelectVariable(GameVariableData v)
    {
        _selected = v;
        PopulateForm(v);
        UpdateFormVisibility(true);
        BuildList();
    }

    private void ClearSelection()
    {
        _selected = null;
        ClearForm();
        UpdateFormVisibility(false);
        BuildList();
    }

    // ── Form ──────────────────────────────────────────────────────────────────

    private void PopulateForm(GameVariableData v)
    {
        TxtLabel.Text        = v.Label;
        TxtVariableId.Text   = v.VariableId;
        TxtDefault.Text      = v.DefaultValue;
        TxtMin.Text          = v.MinValue.ToString();
        TxtMax.Text          = v.MaxValue.ToString();
        TxtGroup.Text        = v.Group;
        ChkPersistent.IsChecked = v.Persistent;

        CmbType.SelectedItem = CTypeOptions.Contains(v.CType) ? v.CType : CTypeOptions[0];
    }

    private void ClearForm()
    {
        TxtLabel.Text        = string.Empty;
        TxtVariableId.Text   = string.Empty;
        TxtDefault.Text      = "0";
        TxtMin.Text          = "0";
        TxtMax.Text          = "255";
        TxtGroup.Text        = string.Empty;
        ChkPersistent.IsChecked = false;
        CmbType.SelectedIndex   = 0;
    }

    private void UpdateFormVisibility(bool visible)
    {
        FormPanel.Visibility        = visible ? Visibility.Visible : Visibility.Collapsed;
        FormPlaceholder.Visibility  = visible ? Visibility.Collapsed : Visibility.Visible;
        BtnDelete.Visibility        = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Auto-generate variable ID from label ──────────────────────────────────

    private void TxtLabel_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Only auto-fill if the ID field is empty or was previously auto-filled
        if (_selected is not null) return; // editing — don't overwrite
        var autoId = Regex.Replace(TxtLabel.Text.ToLower().Trim(), @"[^a-z0-9]+", "_").Trim('_');
        TxtVariableId.Text = autoId;
    }

    // ── CRUD ──────────────────────────────────────────────────────────────────

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        ClearSelection();
        TxtLabel.Focus();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm(out var error))
        {
            MessageBox.Show(error, "Retruxel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var id    = TxtVariableId.Text.Trim();
        var label = TxtLabel.Text.Trim();

        if (_selected is null)
        {
            // New variable
            if (_project.Variables.Any(v => v.VariableId == id))
            {
                MessageBox.Show($"A variable with id '{id}' already exists.", "Retruxel",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var v = new GameVariableData
            {
                VariableId   = id,
                Label        = label,
                CType        = CmbType.SelectedItem?.ToString() ?? "uint8_t",
                DefaultValue = TxtDefault.Text.Trim(),
                MinValue     = int.TryParse(TxtMin.Text, out var mn) ? mn : 0,
                MaxValue     = int.TryParse(TxtMax.Text, out var mx) ? mx : 255,
                Group        = TxtGroup.Text.Trim(),
                Persistent   = ChkPersistent.IsChecked == true
            };
            _project.Variables.Add(v);
            _selected = v;
        }
        else
        {
            // Update existing
            _selected.Label        = label;
            _selected.CType        = CmbType.SelectedItem?.ToString() ?? "uint8_t";
            _selected.DefaultValue = TxtDefault.Text.Trim();
            _selected.MinValue     = int.TryParse(TxtMin.Text, out var mn) ? mn : 0;
            _selected.MaxValue     = int.TryParse(TxtMax.Text, out var mx) ? mx : 255;
            _selected.Group        = TxtGroup.Text.Trim();
            _selected.Persistent   = ChkPersistent.IsChecked == true;
            // VariableId is immutable after creation — it's the C field name
        }

        _saveCallback();
        BuildList();
        UpdateFormVisibility(true);
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;

        if (MessageBox.Show(
                $"Delete variable '{_selected.Label}' ({_selected.VariableId})?\n\nThis cannot be undone.",
                "Delete Variable", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        _project.Variables.Remove(_selected);
        _saveCallback();
        ClearSelection();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateForm(out string error)
    {
        var id    = TxtVariableId.Text.Trim();
        var label = TxtLabel.Text.Trim();

        if (string.IsNullOrEmpty(label))      { error = "Label is required."; return false; }
        if (string.IsNullOrEmpty(id))         { error = "Variable ID is required."; return false; }
        if (!Regex.IsMatch(id, @"^[a-z_][a-z0-9_]*$"))
        {
            error = "Variable ID must be a valid C identifier (lowercase letters, digits, underscores).";
            return false;
        }
        if (!int.TryParse(TxtMin.Text, out var mn)) { error = "Min value must be an integer."; return false; }
        if (!int.TryParse(TxtMax.Text, out var mx)) { error = "Max value must be an integer."; return false; }
        if (mn > mx) { error = "Min value must be ≤ Max value."; return false; }
        if (!IsValidDefault(TxtDefault.Text.Trim(), CmbType.SelectedItem?.ToString()))
        {
            error = "Default value is not valid for the selected type."; return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsValidDefault(string value, string? cType)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return cType switch
        {
            "uint8_t"  => byte.TryParse(value, out _),
            "int8_t"   => sbyte.TryParse(value, out _),
            "uint16_t" => ushort.TryParse(value, out _),
            "int16_t"  => short.TryParse(value, out _),
            "uint32_t" => uint.TryParse(value, out _),
            "int32_t"  => int.TryParse(value, out _),
            _          => int.TryParse(value, out _)
        };
    }
}
