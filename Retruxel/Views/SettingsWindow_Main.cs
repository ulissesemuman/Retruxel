using Retruxel.Controls;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SettingsWindow : Window
{
    private AppSettings _settings = new();
    private bool _loading = true;
    private readonly Dictionary<string, (Grid Panel, Border Accent, Border Nav)> _sections = new();
    private readonly Dictionary<string, TargetSettingsControl> _targetControls = new();

    public SettingsWindow()
    {
        InitializeComponent();

        _sections["general"] = (PanelGeneral, AccentGeneral, NavGeneral);
        _sections["appearance"] = (PanelAppearance, AccentAppearance, NavAppearance);
        _sections["toolchain"] = (PanelToolchain, AccentToolchain, NavToolchain);
        _sections["emulators"] = (PanelEmulators, AccentEmulators, NavEmulators);

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = await SettingsService.LoadAsync();
        PopulateLanguageCombo();
        GenerateTargetSections();
        PopulateEmulatorSettings();
        ApplySettingsToUi();
        _loading = false;
    }

    #region Window Chrome

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    #endregion

    #region Helpers

    private static void SelectComboByTag(ComboBox combo, string tag)
    {
        foreach (ComboBoxItem item in combo.Items)
            if ((string)item.Tag == tag)
            {
                combo.SelectedItem = item;
                return;
            }
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindVisualChild<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private static TextBlock? FindTextBlockIn(Border border)
    {
        if (border.Child is Grid grid)
            foreach (var child in grid.Children)
                if (child is TextBlock tb) return tb;
        return null;
    }

    #endregion
}
