using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

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
        };

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _settings = await SettingsService.LoadAsync();
        ApplySettingsToUi();
        _loading = false;
    }

    // ── Apply loaded settings to controls ────────────────────────────────────

    private void ApplySettingsToUi()
    {
        // General
        SelectComboByTag(CmbLanguage, _settings.General.Language);
        ChkShowWelcome.IsChecked = _settings.General.ShowWelcomeOnStartup;

        // Appearance
        TxtFontSize.Text = _settings.Appearance.FontSize.ToString();

        // SMS
        ChkSmsShowWarnings.IsChecked = _settings.Targets.Sms.ShowToolchainWarnings;
        ChkSmsLaunchEmulator.IsChecked = _settings.Targets.Sms.LaunchEmulatorAfterBuild;
        
        // Update SMS emulator path display
        TxtSmsEmulatorPath.Text = string.IsNullOrEmpty(_settings.Targets.Sms.EmulatorPath)
            ? "Not configured"
            : _settings.Targets.Sms.EmulatorPath;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void NavGeneral_Click(object sender, MouseButtonEventArgs e)    => ShowSection("general");
    private void NavAppearance_Click(object sender, MouseButtonEventArgs e) => ShowSection("appearance");
    private void NavToolchain_Click(object sender, MouseButtonEventArgs e)  => ShowSection("toolchain");
    private void NavSms_Click(object sender, MouseButtonEventArgs e)        => ShowSection("sms");

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
        TabGeneralInterface.Style = (Style)FindResource("TabButtonActive");
        TabGeneralBehavior.Style  = (Style)FindResource("TabButton");
    }

    private void TabGeneralBehavior_Click(object sender, RoutedEventArgs e)
    {
        TabContentGeneralInterface.Visibility = Visibility.Collapsed;
        TabContentGeneralBehavior.Visibility  = Visibility.Visible;
        TabGeneralInterface.Style = (Style)FindResource("TabButton");
        TabGeneralBehavior.Style  = (Style)FindResource("TabButtonActive");
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

    private void TxtFontSize_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        if (int.TryParse(TxtFontSize.Text, out var size) && size is >= 8 and <= 24)
            _settings.Appearance.FontSize = size;
        AutoSave();
    }

    private void ChkSmsShowWarnings_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Sms.ShowToolchainWarnings = ChkSmsShowWarnings.IsChecked == true;
        AutoSave();
    }

    private void BtnBrowseSmsEmulator_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select SMS Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            InitialDirectory = string.IsNullOrEmpty(_settings.Targets.Sms.EmulatorPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                : Path.GetDirectoryName(_settings.Targets.Sms.EmulatorPath)
        };

        if (dialog.ShowDialog() == true)
        {
            _settings.Targets.Sms.EmulatorPath = dialog.FileName;
            TxtSmsEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void ChkSmsLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Sms.LaunchEmulatorAfterBuild = ChkSmsLaunchEmulator.IsChecked == true;
        AutoSave();
    }

    // ── Auto-save ─────────────────────────────────────────────────────────────

    private void AutoSave()
    {
        SettingsService.Save(_settings);
        LblSaved.Text = "SAVED";

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

    // ── Window chrome ─────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
