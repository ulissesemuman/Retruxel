using Retruxel.Controls;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SettingsWindow
{
    #region UI Population

    private void PopulateLanguageCombo()
    {
        CmbLanguage.Items.Clear();

        foreach (var lang in LocalizationService.Instance.AvailableLanguages)
        {
            var item = new ComboBoxItem
            {
                Content = lang.NativeName,
                Tag = lang.Code
            };
            CmbLanguage.Items.Add(item);
        }

        // If no language is set (first run), detect system language
        var languageToSelect = _settings.General.Language;
        if (string.IsNullOrEmpty(languageToSelect))
        {
            languageToSelect = LocalizationService.Instance.DetectSystemLanguage();
            _settings.General.Language = languageToSelect;
        }

        // Select current language immediately after populating
        SelectComboByTag(CmbLanguage, languageToSelect);
    }

    private void PopulateEmulatorSettings()
    {
        var emulators = new List<(string Name, string[] SupportedSystems)>
        {
            ("Mesen 2", new[] { "NES", "SNES", "SMS", "Game Gear", "SG-1000" }),
            ("Emulicious", new[] { "SMS", "Game Gear", "SG-1000", "ColecoVision", "Game Boy", "MSX" }),
            ("mGBA", new[] { "Game Boy", "Game Boy Color", "Game Boy Advance" })
        };

        foreach (var emulator in emulators)
        {
            var targetSettings = GetOrCreateEmulatorSettings(emulator.Name);

            var border = new Border
            {
                Style = (Style)FindResource("SettingRow")
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Emulator name and supported systems
            var headerStack = new StackPanel();
            var emulatorLabel = new TextBlock
            {
                Text = emulator.Name,
                Style = (Style)FindResource("TextBody"),
                Foreground = (Brush)FindResource("BrushOnSurface"),
                FontWeight = FontWeights.Bold
            };
            headerStack.Children.Add(emulatorLabel);

            var systemsLabel = new TextBlock
            {
                Text = string.Join(", ", emulator.SupportedSystems),
                Style = (Style)FindResource("TextLabel"),
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant")
            };
            headerStack.Children.Add(systemsLabel);

            Grid.SetRow(headerStack, 0);
            grid.Children.Add(headerStack);

            // Emulator path label
            var pathLabel = new TextBlock
            {
                Text = "Emulator Path",
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(pathLabel, 2);
            grid.Children.Add(pathLabel);

            // Emulator path
            var pathGrid = new Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pathTextBox = new TextBox
            {
                Text = targetSettings.LiveLinkEmulatorPath,
                VerticalAlignment = VerticalAlignment.Center
            };
            pathTextBox.TextChanged += (s, e) =>
            {
                if (!_loading)
                {
                    targetSettings.LiveLinkEmulatorPath = pathTextBox.Text;
                    AutoSave();
                }
            };
            Grid.SetColumn(pathTextBox, 0);
            pathGrid.Children.Add(pathTextBox);

            var browseButton = new Button
            {
                Content = "BROWSE",
                Style = (Style)FindResource("ButtonSecondary"),
                Padding = new Thickness(16, 0, 16, 0),
                Height = 32
            };
            browseButton.Click += (s, e) =>
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                    Title = $"Select {emulator.Name} Emulator"
                };

                if (dialog.ShowDialog() == true)
                {
                    pathTextBox.Text = dialog.FileName;
                }
            };
            Grid.SetColumn(browseButton, 2);
            pathGrid.Children.Add(browseButton);

            Grid.SetRow(pathGrid, 4);
            grid.Children.Add(pathGrid);

            // Arguments label
            var argsLabel = new TextBlock
            {
                Text = "Arguments (use {ROM} for ROM path)",
                Style = (Style)FindResource("TextLabel"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            Grid.SetRow(argsLabel, 6);
            grid.Children.Add(argsLabel);

            // Arguments
            var argsTextBox = new TextBox
            {
                Text = targetSettings.LiveLinkEmulatorArguments,
                VerticalAlignment = VerticalAlignment.Center
            };
            argsTextBox.TextChanged += (s, e) =>
            {
                if (!_loading)
                {
                    targetSettings.LiveLinkEmulatorArguments = argsTextBox.Text;
                    AutoSave();
                }
            };
            Grid.SetRow(argsTextBox, 6);
            grid.Children.Add(argsTextBox);

            border.Child = grid;
            EmulatorsStack.Children.Add(border);
        }
    }

    private TargetSettings GetOrCreateEmulatorSettings(string emulatorName)
    {
        var key = emulatorName.ToLowerInvariant();

        if (!_settings.Targets.ContainsKey(key))
        {
            _settings.Targets[key] = new TargetSettings();
        }

        return _settings.Targets[key];
    }

    private void GenerateTargetSections()
    {
        var targets = TargetRegistry.GetAllTargets().OrderBy(t => t.DisplayName);
        var navStack = NavTargetsStack;
        var contentGrid = ContentGrid;

        foreach (var target in targets)
        {
            var targetId = target.TargetId;

            // Create nav item
            var navBorder = new Border
            {
                Style = (Style)FindResource("NavItem")
            };

            var navGrid = new Grid();
            var accent = new Border
            {
                Width = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = (Brush)FindResource("BrushPrimary"),
                Visibility = Visibility.Collapsed
            };
            var navLabel = new TextBlock
            {
                Text = target.DisplayName.ToUpper(),
                Style = (Style)FindResource("TextLabelCaps"),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            navGrid.Children.Add(accent);
            navGrid.Children.Add(navLabel);
            navBorder.Child = navGrid;
            navBorder.MouseLeftButtonDown += (s, e) => ShowSection(targetId);

            navStack.Children.Add(navBorder);

            // Create content panel
            var panel = new Grid { Visibility = Visibility.Collapsed };
            var control = new TargetSettingsControl();
            control.Initialize(_settings, targetId);
            panel.Children.Add(control);

            contentGrid.Children.Add(panel);

            _sections[targetId] = (panel, accent, navBorder);
            _targetControls[targetId] = control;
        }
    }

    private void ApplySettingsToUi()
    {
        ChkShowWelcome.IsChecked = _settings.General.ShowWelcomeOnStartup;
        ChkCheckUpdates.IsChecked = _settings.General.CheckUpdatesOnStartup;
        ChkShowMadeWithSplash.IsChecked = _settings.General.ShowMadeWithSplash;
        ChkAutoSave.IsChecked = _settings.General.AutoSaveEnabled;
        SliderUndoHistory.Value = _settings.General.UndoHistoryLimit;
        TxtUndoHistoryValue.Text = _settings.General.UndoHistoryLimit.ToString();

        var firstTarget = TargetRegistry.GetAllTargets().FirstOrDefault();
        if (firstTarget != null)
        {
            var firstSettings = SettingsService.GetTargetSettings(_settings, firstTarget.TargetId);
            ChkShowWarnings.IsChecked = firstSettings.ShowToolchainWarnings;
        }
    }

    #endregion

    #region Navigation

    private void NavGeneral_Click(object sender, MouseButtonEventArgs e) => ShowSection("general");
    private void NavAppearance_Click(object sender, MouseButtonEventArgs e) => ShowSection("appearance");
    private void NavToolchain_Click(object sender, MouseButtonEventArgs e) => ShowSection("toolchain");
    private void NavEmulators_Click(object sender, MouseButtonEventArgs e) => ShowSection("emulators");

    private void ShowSection(string key)
    {
        foreach (var (sectionKey, (panel, accent, nav)) in _sections)
        {
            var isActive = sectionKey == key;
            panel.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            accent.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;

            var label = FindTextBlockIn(nav);

            if (label is not null)
                label.Foreground = isActive
                    ? (Brush)FindResource("BrushOnSurface")
                    : (Brush)FindResource("BrushOnSurfaceVariant");
        }
    }

    #endregion

    #region Tab Switching

    private void TabGeneralInterface_Click(object sender, RoutedEventArgs e)
    {
        TabContentGeneralInterface.Visibility = Visibility.Visible;
        TabContentGeneralBehavior.Visibility = Visibility.Collapsed;
        TabGeneralInterface.Style = (Style)FindResource("ButtonTabActive");
        TabGeneralBehavior.Style = (Style)FindResource("ButtonTab");
    }

    private void TabGeneralBehavior_Click(object sender, RoutedEventArgs e)
    {
        TabContentGeneralInterface.Visibility = Visibility.Collapsed;
        TabContentGeneralBehavior.Visibility = Visibility.Visible;
        TabGeneralInterface.Style = (Style)FindResource("ButtonTab");
        TabGeneralBehavior.Style = (Style)FindResource("ButtonTabActive");
    }

    #endregion
}
