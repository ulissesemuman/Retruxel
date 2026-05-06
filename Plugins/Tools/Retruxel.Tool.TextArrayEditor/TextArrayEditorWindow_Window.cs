using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Tool.TextArrayEditor;

public partial class TextArrayEditorWindow
{
    #region Window Management

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else
            DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveAndClose();
    }

    private void SaveAndClose()
    {
        // Validate array name
        if (!IsValidIdentifier(_state.Name))
        {
            MessageBox.Show("Array name must be a valid C identifier (letters, numbers, underscore only).",
                "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate that all languages have the same string count
        if (_state.Languages.Count > 0)
        {
            var expectedCount = _state.Languages[0].Strings.Count;
            if (_state.Languages.Any(lang => lang.Strings.Count != expectedCount))
            {
                MessageBox.Show("All languages must have the same number of strings.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // Check if using repository font and show copyright warning
        if (CmbFontCategory.SelectedItem is FontCategoryItem selectedFont && selectedFont.IsRepositoryFont)
        {
            var copyrightInfo = string.IsNullOrEmpty(selectedFont.Copyright) 
                ? "Copyright information not available" 
                : selectedFont.Copyright;

            var result = MessageBox.Show(
                $"You are using a font from the u8g2 repository.\n\n" +
                $"FONT: {selectedFont.Name}\n" +
                $"COPYRIGHT: {copyrightInfo}\n\n" +
                "IMPORTANT NOTICE:\n" +
                "• This font may be subject to copyright restrictions\n" +
                "• You must respect the font's license terms\n" +
                "• Credit the original author when required\n" +
                "• Visit the u8g2 repository for full copyright information:\n" +
                "  https://github.com/olikraus/u8g2\n\n" +
                "By clicking YES, you acknowledge that you are responsible for:\n" +
                "• Verifying the font's license\n" +
                "• Complying with copyright requirements\n" +
                "• Providing proper attribution if needed\n\n" +
                "Do you want to continue?",
                "Repository Font - Copyright Notice",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;
        }

        // Serialize to ModuleData for VisualToolInvoker
        ModuleData = new System.Collections.Generic.Dictionary<string, object>
        {
            ["name"] = _state.Name,
            ["languages"] = _state.Languages.Select(lang => new System.Collections.Generic.Dictionary<string, object>
            {
                ["code"] = lang.Code,
                ["strings"] = lang.Strings
            }).ToList(),
            ["fontAssetId"] = _state.FontAssetId ?? ""
        };

        DialogResult = true;
        Close();
    }

    #endregion

    #region Tab Management

    private void BtnTabStrings_Click(object sender, RoutedEventArgs e)
    {
        ActivateTab(TabStrings, BtnTabStrings);
    }

    private void BtnTabFont_Click(object sender, RoutedEventArgs e)
    {
        ActivateTab(TabFont, BtnTabFont);
    }

    private void ActivateTab(UIElement tabContent, Button tabButton)
    {
        // Hide all tabs
        TabStrings.Visibility = Visibility.Collapsed;
        TabFont.Visibility = Visibility.Collapsed;

        // Reset all tab buttons
        BtnTabStrings.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");
        BtnTabFont.Foreground = (Brush)FindResource("BrushOnSurfaceVariant");

        // Show selected tab
        tabContent.Visibility = Visibility.Visible;
        tabButton.Foreground = (Brush)FindResource("BrushPrimary");
    }

    #endregion
}
