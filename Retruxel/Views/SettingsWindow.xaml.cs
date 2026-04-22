using Microsoft.Win32;
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
    private bool _loading = true;   // suppresses change handlers during initial load

    // ── Nav panels & accents ──────────────────────────────────────────────────

    private readonly Dictionary<string, (Grid Panel, Border Accent, Border Nav)> _sections;

    public SettingsWindow()
    {
        InitializeComponent();

        _sections = new()
        {
            ["general"]    = (PanelGeneral,    AccentGeneral,    NavGeneral),
            ["appearance"] = (PanelAppearance, AccentAppearance, NavAppearance),
            ["toolchain"]  = (PanelToolchain,  AccentToolchain,  NavToolchain),
            ["sms"]        = (PanelSms,        AccentSms,        NavSms),
            ["gg"]         = (PanelGg,         AccentGg,         NavGg),
            ["sg1000"]     = (PanelSg1000,     AccentSg1000,     NavSg1000),
            ["coleco"]     = (PanelColeco,     AccentColeco,     NavColeco),
            ["nes"]        = (PanelNes,        AccentNes,        NavNes),
        };

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = await SettingsService.LoadAsync();
        PopulateLanguageCombo();
        ApplySettingsToUi();
        _loading = false;
    }

    // ── Populate language combo dynamically ──────────────────────────────────

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
        var loc = LocalizationService.Instance;
        
        // General - language already selected in PopulateLanguageCombo
        ChkShowWelcome.IsChecked = _settings.General.ShowWelcomeOnStartup;
        ChkCheckUpdates.IsChecked = _settings.General.CheckUpdatesOnStartup;
        ChkShowMadeWithSplash.IsChecked = _settings.General.ShowMadeWithSplash;
        SliderUndoHistory.Value = _settings.General.UndoHistoryLimit;
        TxtUndoHistoryValue.Text = _settings.General.UndoHistoryLimit.ToString();

        // Toolchain
        var smsSettings = SettingsService.GetTargetSettings(_settings, "sms");
        ChkShowWarnings.IsChecked = smsSettings.ShowToolchainWarnings;

        // SMS
        TxtSmsEmulatorArguments.Text = smsSettings.EmulatorArguments;
        ChkSmsLaunchEmulator.IsChecked = smsSettings.LaunchEmulatorAfterBuild;
        TxtSmsEmulatorPath.Text = string.IsNullOrEmpty(smsSettings.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : smsSettings.EmulatorPath;

        // Game Gear
        var ggSettings = SettingsService.GetTargetSettings(_settings, "gg");
        TxtGgEmulatorArguments.Text = ggSettings.EmulatorArguments;
        ChkGgLaunchEmulator.IsChecked = ggSettings.LaunchEmulatorAfterBuild;
        TxtGgEmulatorPath.Text = string.IsNullOrEmpty(ggSettings.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : ggSettings.EmulatorPath;

        // SG-1000
        var sg1000Settings = SettingsService.GetTargetSettings(_settings, "sg1000");
        TxtSg1000EmulatorArguments.Text = sg1000Settings.EmulatorArguments;
        ChkSg1000LaunchEmulator.IsChecked = sg1000Settings.LaunchEmulatorAfterBuild;
        TxtSg1000EmulatorPath.Text = string.IsNullOrEmpty(sg1000Settings.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : sg1000Settings.EmulatorPath;

        // ColecoVision
        var colecoSettings = SettingsService.GetTargetSettings(_settings, "coleco");
        TxtColecoEmulatorArguments.Text = colecoSettings.EmulatorArguments;
        ChkColecoLaunchEmulator.IsChecked = colecoSettings.LaunchEmulatorAfterBuild;
        TxtColecoEmulatorPath.Text = string.IsNullOrEmpty(colecoSettings.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : colecoSettings.EmulatorPath;

        // NES
        var nesSettings = SettingsService.GetTargetSettings(_settings, "nes");
        TxtNesEmulatorArguments.Text = nesSettings.EmulatorArguments;
        ChkNesLaunchEmulator.IsChecked = nesSettings.LaunchEmulatorAfterBuild;
        TxtNesEmulatorPath.Text = string.IsNullOrEmpty(nesSettings.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : nesSettings.EmulatorPath;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void NavGeneral_Click(object sender, MouseButtonEventArgs e)    => ShowSection("general");
    private void NavAppearance_Click(object sender, MouseButtonEventArgs e) => ShowSection("appearance");
    private void NavToolchain_Click(object sender, MouseButtonEventArgs e)  => ShowSection("toolchain");
    private void NavSms_Click(object sender, MouseButtonEventArgs e)        => ShowSection("sms");
    private void NavGg_Click(object sender, MouseButtonEventArgs e)         => ShowSection("gg");
    private void NavSg1000_Click(object sender, MouseButtonEventArgs e)     => ShowSection("sg1000");
    private void NavColeco_Click(object sender, MouseButtonEventArgs e)     => ShowSection("coleco");
    private void NavNes_Click(object sender, MouseButtonEventArgs e)        => ShowSection("nes");

    private void ShowSection(string key)
    {
        foreach (var (sectionKey, (panel, accent, nav)) in _sections)
        {
            var isActive = sectionKey == key;
            panel.Visibility  = isActive ? Visibility.Visible : Visibility.Collapsed;
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
        TabContentGeneralBehavior.Visibility  = Visibility.Collapsed;
        TabGeneralInterface.Style = (Style)FindResource("ButtonTabActive");
        TabGeneralBehavior.Style  = (Style)FindResource("ButtonTab");
    }

    private void TabGeneralBehavior_Click(object sender, RoutedEventArgs e)
    {
        TabContentGeneralInterface.Visibility = Visibility.Collapsed;
        TabContentGeneralBehavior.Visibility  = Visibility.Visible;
        TabGeneralInterface.Style = (Style)FindResource("ButtonTab");
        TabGeneralBehavior.Style  = (Style)FindResource("ButtonTabActive");
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
        SettingsService.GetTargetSettings(_settings, "sms").ShowToolchainWarnings = ChkShowWarnings.IsChecked == true;
        AutoSave();
    }

    private void TxtSmsEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "sms").EmulatorArguments = TxtSmsEmulatorArguments.Text;
        AutoSave();
    }

    private void BtnBrowseSmsEmulator_Click(object sender, RoutedEventArgs e)
    {
        var smsSettings = SettingsService.GetTargetSettings(_settings, "sms");
        var dialog = new OpenFileDialog
        {
            Title = "Select SMS Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            InitialDirectory = string.IsNullOrEmpty(smsSettings.EmulatorPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                : Path.GetDirectoryName(smsSettings.EmulatorPath)
        };

        if (dialog.ShowDialog() == true)
        {
            smsSettings.EmulatorPath = dialog.FileName;
            TxtSmsEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void ChkSmsLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "sms").LaunchEmulatorAfterBuild = ChkSmsLaunchEmulator.IsChecked == true;
        AutoSave();
    }

    // ── Game Gear handlers ────────────────────────────────────────────────────

    private void BtnBrowseGgEmulator_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Game Gear Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            SettingsService.GetTargetSettings(_settings, "gg").EmulatorPath = dialog.FileName;
            TxtGgEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtGgEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "gg").EmulatorArguments = TxtGgEmulatorArguments.Text;
        AutoSave();
    }

    private void ChkGgLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "gg").LaunchEmulatorAfterBuild = ChkGgLaunchEmulator.IsChecked == true;
        AutoSave();
    }

    // ── SG-1000 handlers ──────────────────────────────────────────────────────

    private void BtnBrowseSg1000Emulator_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select SG-1000 Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            SettingsService.GetTargetSettings(_settings, "sg1000").EmulatorPath = dialog.FileName;
            TxtSg1000EmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtSg1000EmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "sg1000").EmulatorArguments = TxtSg1000EmulatorArguments.Text;
        AutoSave();
    }

    private void ChkSg1000LaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "sg1000").LaunchEmulatorAfterBuild = ChkSg1000LaunchEmulator.IsChecked == true;
        AutoSave();
    }

    // ── ColecoVision handlers ─────────────────────────────────────────────────

    private void BtnBrowseColecoEmulator_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select ColecoVision Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            SettingsService.GetTargetSettings(_settings, "coleco").EmulatorPath = dialog.FileName;
            TxtColecoEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtColecoEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "coleco").EmulatorArguments = TxtColecoEmulatorArguments.Text;
        AutoSave();
    }

    private void ChkColecoLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "coleco").LaunchEmulatorAfterBuild = ChkColecoLaunchEmulator.IsChecked == true;
        AutoSave();
    }

    // ── NES handlers ──────────────────────────────────────────────────────────

    private void BtnBrowseNesEmulator_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select NES Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            SettingsService.GetTargetSettings(_settings, "nes").EmulatorPath = dialog.FileName;
            TxtNesEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtNesEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "nes").EmulatorArguments = TxtNesEmulatorArguments.Text;
        AutoSave();
    }

    private void ChkNesLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        SettingsService.GetTargetSettings(_settings, "nes").LaunchEmulatorAfterBuild = ChkNesLaunchEmulator.IsChecked == true;
        AutoSave();
    }

    // ── Auto-save ─────────────────────────────────────────────────────────────

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
