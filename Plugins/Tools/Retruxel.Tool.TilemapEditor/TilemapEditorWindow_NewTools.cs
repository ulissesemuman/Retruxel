using System;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    private void ChkAutoTiling_CheckedChanged(object sender, RoutedEventArgs e)
    {
        _isAutoTilingEnabled = ChkAutoTiling.IsChecked == true;
    }

    private void TxtAutoTileBase_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (TxtAutoTileBase != null && int.TryParse(TxtAutoTileBase.Text, out int val))
        {
            _autoTileBaseIndex = val;
        }
    }

    private void CmbToolMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbToolMode == null) return;

        switch (CmbToolMode.SelectedIndex)
        {
            case 0:
                _currentToolMode = ToolMode.Paint;
                SetSelectorVisibility(Visibility.Collapsed);
                break;
            case 1:
                _currentToolMode = ToolMode.MetatilePaint;
                SetSelectorVisibility(Visibility.Visible);
                UpdateBlockSelectorList();
                break;
            case 2:
                _currentToolMode = ToolMode.BrushPaint;
                SetSelectorVisibility(Visibility.Visible);
                UpdateBlockSelectorList();
                break;
        }
    }

    private void SetSelectorVisibility(Visibility visibility)
    {
        if (LblPaletteTools != null) LPaletteTools_SafeSet(visibility);
        if (CmbBlockSelector != null) CmbBlockSelector.Visibility = visibility;
    }

    private void LPaletteTools_SafeSet(Visibility visibility)
    {
        LblPaletteTools.Visibility = visibility;
    }

    private void UpdateBlockSelectorList()
    {
        if (CmbBlockSelector == null) return;
        CmbBlockSelector.Items.Clear();

        if (_currentToolMode == ToolMode.MetatilePaint)
        {
            if (LblPaletteTools != null) LblPaletteTools.Text = "Select Metatile:";
            foreach (var metatile in _metatiles)
            {
                CmbBlockSelector.Items.Add(metatile.Name);
            }
            if (_metatiles.Count > 0)
            {
                CmbBlockSelector.SelectedIndex = 0;
                _selectedMetatileIndex = 0;
            }
        }
        else if (_currentToolMode == ToolMode.BrushPaint)
        {
            if (LblPaletteTools != null) LblPaletteTools.Text = "Select Brush:";
            foreach (var brush in _brushes)
            {
                CmbBlockSelector.Items.Add(brush.Name);
            }
            if (_brushes.Count > 0)
            {
                CmbBlockSelector.SelectedIndex = 0;
                _selectedBrushIndex = 0;
            }
        }
    }

    private void CmbBlockSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbBlockSelector == null || CmbBlockSelector.SelectedIndex < 0) return;

        if (_currentToolMode == ToolMode.MetatilePaint)
        {
            _selectedMetatileIndex = CmbBlockSelector.SelectedIndex;
        }
        else if (_currentToolMode == ToolMode.BrushPaint)
        {
            _selectedBrushIndex = CmbBlockSelector.SelectedIndex;
        }
    }

    private void BtnCreateMetatile_Click(object sender, RoutedEventArgs e)
    {
        var metatile = CreateMetatileFromSelection();
        if (metatile != null)
        {
            MessageBox.Show($"Metatile '{metatile.Name}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            if (_currentToolMode == ToolMode.MetatilePaint)
            {
                UpdateBlockSelectorList();
                CmbBlockSelector.SelectedIndex = _metatiles.Count - 1;
            }
        }
        else
        {
            MessageBox.Show("Please select a block of tiles on the tileset first.", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCreateBrush_Click(object sender, RoutedEventArgs e)
    {
        var brush = CreateBrushFromSelection();
        if (brush != null)
        {
            MessageBox.Show($"Brush '{brush.Name}' created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            if (_currentToolMode == ToolMode.BrushPaint)
            {
                UpdateBlockSelectorList();
                CmbBlockSelector.SelectedIndex = _brushes.Count - 1;
            }
        }
        else
        {
            MessageBox.Show("Please select a block of tiles on the tileset first.", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
