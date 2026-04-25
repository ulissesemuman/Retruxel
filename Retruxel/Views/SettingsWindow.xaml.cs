using Retruxel.Controls;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.IO;
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

    // ── Populate language combo dynamically ──────────────────────────────────

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
        // Use emulator name as key (lowercase for consistency)
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

    // ── Apply loaded settings to controls ────────────────────────────────────

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

    // ── Navigation ────────────────────────────────────────────────────────────

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

            // Active nav item — white text; inactive — variant
            var label = FindTextBlockIn(nav);

            if (label is not null)
                label.Foreground = isActive
                    ? (Brush)FindResource("BrushOnSurface")
                    : (Brush)FindResource("BrushOnSurfaceVariant");
        }
    }

    private static TextBlock? FindTextBlockIn(Border border)
    {
        if (border.Child is Grid grid)
            foreach (var child in grid.Children)
                if (child is TextBlock tb) return tb;
        return null;
    }

    // ── Tab switching — General ───────────────────────────────────────────────

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

    // ── Change handlers ───────────────────────────────────────────────────────

    private void CmbLanguage_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        if (CmbLanguage.SelectedItem is ComboBoxItem item)
        {
            var selectedLanguage = (string)item.Tag;
            _settings.General.Language = selectedLanguage;

            // Reload localization in runtime
            var localizationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Localization");
            LocalizationService.Instance.Load(selectedLanguage, localizationPath);
        }
        AutoSave();
    }

    private void ChkShowWelcome_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.General.ShowWelcomeOnStartup = ChkShowWelcome.IsChecked == true;
        AutoSave();
    }

    private void ChkCheckUpdates_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.General.CheckUpdatesOnStartup = ChkCheckUpdates.IsChecked == true;
        AutoSave();
    }

    private void ChkShowMadeWithSplash_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;

        var isEnabling = ChkShowMadeWithSplash.IsChecked == true;

        if (!isEnabling)
        {
            var result = MessageBox.Show(
                "Retruxel is completely free and built with a lot of dedication " +
                "and care for the retro dev community.\n\n" +
                "The \"MADE WITH RETRUXEL\" splash screen is the only way this " +
                "tool gets noticed by other developers.\n\n" +
                "Are you sure you want to disable it?",
                "Retruxel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                // User changed their mind — restore the toggle visually
                _loading = true;
                ChkShowMadeWithSplash.IsChecked = true;
                _loading = false;
                return;
            }
        }

        _settings.General.ShowMadeWithSplash = isEnabling;
        AutoSave();
    }

    private void SliderUndoHistory_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        var value = (int)SliderUndoHistory.Value;
        _settings.General.UndoHistoryLimit = value;
        TxtUndoHistoryValue.Text = value.ToString();
        AutoSave();
    }

    private void ChkShowWarnings_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var isChecked = ChkShowWarnings.IsChecked == true;
        foreach (var target in TargetRegistry.GetAllTargets())
        {
            SettingsService.GetTargetSettings(_settings, target.TargetId).ShowToolchainWarnings = isChecked;
        }
        AutoSave();
    }

    private void ChkAutoSave_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.General.AutoSaveEnabled = ChkAutoSave.IsChecked == true;
        AutoSave();
    }

    private void AutoSave()
    {
        SettingsService.Save(_settings);
        LblSaved.Text = LocalizationService.Instance.Get("settings.saved");

        // Clear the label after 2 seconds
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (_, _) =>
        {
            LblSaved.Text = "";
            timer.Stop();
        };
        timer.Start();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    // ── Window chrome ─────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
