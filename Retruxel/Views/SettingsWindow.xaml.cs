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
        ApplyLocalization();
        _loading = false;
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Instance;
        
        // Title
        Title = loc.Get("settings.title");
        TxtTitleSettings.Text = loc.Get("settings.title");
        
        // Navigation
        FindTextBlockIn(NavGeneral)!.Text = loc.Get("settings.nav.general");
        FindTextBlockIn(NavAppearance)!.Text = loc.Get("settings.nav.appearance");
        FindTextBlockIn(NavToolchain)!.Text = loc.Get("settings.nav.toolchain");
        TxtNavTargets.Text = loc.Get("settings.nav.targets");
        FindTextBlockIn(NavSms)!.Text = loc.Get("settings.nav.sms");
        FindTextBlockIn(NavGg)!.Text = "GAME GEAR";
        FindTextBlockIn(NavSg1000)!.Text = "SG-1000";
        FindTextBlockIn(NavColeco)!.Text = "COLECOVISION";
        FindTextBlockIn(NavNes)!.Text = loc.Get("settings.nav.nes");
        
        // Tabs
        TabGeneralInterface.Content = loc.Get("settings.tab.interface");
        TabGeneralBehavior.Content = loc.Get("settings.tab.behavior");
        TabAppearanceEditor.Content = loc.Get("settings.tab.editor");
        TabToolchainGeneral.Content = loc.Get("settings.tab.general");
        TabSmsBuild.Content = loc.Get("settings.tab.build");
        TabGgBuild.Content = loc.Get("settings.tab.build");
        TabSg1000Build.Content = loc.Get("settings.tab.build");
        TabColecoBuild.Content = loc.Get("settings.tab.build");
        TabNesBuild.Content = loc.Get("settings.tab.build");
        
        // General - Interface
        TxtSectionLanguage.Text = loc.Get("settings.section.language");
        TxtLanguageLabel.Text = loc.Get("settings.general.language");
        TxtLanguageDesc.Text = loc.Get("settings.general.language.desc");
        
        // General - Behavior
        TxtSectionStartup.Text = loc.Get("settings.section.startup");
        TxtWelcomeLabel.Text = loc.Get("settings.general.welcome");
        TxtWelcomeDesc.Text = loc.Get("settings.general.welcome.desc");
        TxtCheckUpdatesLabel.Text = loc.Get("settings.general.check_updates");
        TxtCheckUpdatesDesc.Text = loc.Get("settings.general.check_updates.desc");
        TxtSectionEditor.Text = loc.Get("settings.section.editor");
        TxtUndoLimitLabel.Text = loc.Get("settings.editor.undo_limit");
        TxtUndoLimitDesc.Text = loc.Get("settings.editor.undo_limit.desc");
        
        // Appearance
        TxtSectionTheme.Text = loc.Get("settings.section.theme");
        TxtThemeLabel.Text = loc.Get("settings.theme.current");
        TxtThemeDesc.Text = loc.Get("settings.theme.desc");
        
        // Toolchain
        TxtSectionBuildConsole.Text = loc.Get("settings.section.build_console");
        TxtToolchainWarningsLabel.Text = loc.Get("settings.toolchain.warnings");
        TxtToolchainWarningsDesc.Text = loc.Get("settings.toolchain.warnings.desc");
        
        // SMS
        TxtSectionEmulatorSms.Text = loc.Get("settings.section.emulator");
        TxtEmulatorPathLabelSms.Text = loc.Get("settings.emulator.path");
        TxtEmulatorArgsLabelSms.Text = loc.Get("settings.emulator.arguments");
        TxtEmulatorArgsDescSms.Text = loc.Get("settings.emulator.arguments.desc");
        TxtLaunchEmulatorLabelSms.Text = loc.Get("settings.emulator.launch");
        TxtLaunchEmulatorDescSms.Text = loc.Get("settings.emulator.launch.desc");
        BtnBrowseSmsEmulator.Content = loc.Get("settings.browse");
        
        // Game Gear
        TxtSectionEmulatorGg.Text = loc.Get("settings.section.emulator");
        TxtEmulatorPathLabelGg.Text = loc.Get("settings.emulator.path");
        TxtEmulatorArgsLabelGg.Text = loc.Get("settings.emulator.arguments");
        TxtEmulatorArgsDescGg.Text = loc.Get("settings.emulator.arguments.desc");
        TxtLaunchEmulatorLabelGg.Text = loc.Get("settings.emulator.launch");
        TxtLaunchEmulatorDescGg.Text = loc.Get("settings.emulator.launch.desc");
        BtnBrowseGgEmulator.Content = loc.Get("settings.browse");
        
        // SG-1000
        TxtSectionEmulatorSg1000.Text = loc.Get("settings.section.emulator");
        TxtEmulatorPathLabelSg1000.Text = loc.Get("settings.emulator.path");
        TxtEmulatorArgsLabelSg1000.Text = loc.Get("settings.emulator.arguments");
        TxtEmulatorArgsDescSg1000.Text = loc.Get("settings.emulator.arguments.desc");
        TxtLaunchEmulatorLabelSg1000.Text = loc.Get("settings.emulator.launch");
        TxtLaunchEmulatorDescSg1000.Text = loc.Get("settings.emulator.launch.desc");
        BtnBrowseSg1000Emulator.Content = loc.Get("settings.browse");
        
        // ColecoVision
        TxtSectionEmulatorColeco.Text = loc.Get("settings.section.emulator");
        TxtEmulatorPathLabelColeco.Text = loc.Get("settings.emulator.path");
        TxtEmulatorArgsLabelColeco.Text = loc.Get("settings.emulator.arguments");
        TxtEmulatorArgsDescColeco.Text = loc.Get("settings.emulator.arguments.desc");
        TxtLaunchEmulatorLabelColeco.Text = loc.Get("settings.emulator.launch");
        TxtLaunchEmulatorDescColeco.Text = loc.Get("settings.emulator.launch.desc");
        BtnBrowseColecoEmulator.Content = loc.Get("settings.browse");
        
        // NES
        TxtSectionEmulatorNes.Text = loc.Get("settings.section.emulator");
        TxtEmulatorPathLabelNes.Text = loc.Get("settings.emulator.path");
        TxtEmulatorArgsLabelNes.Text = loc.Get("settings.emulator.arguments");
        TxtEmulatorArgsDescNes.Text = loc.Get("settings.emulator.arguments.desc");
        TxtLaunchEmulatorLabelNes.Text = loc.Get("settings.emulator.launch");
        TxtLaunchEmulatorDescNes.Text = loc.Get("settings.emulator.launch.desc");
        BtnBrowseNesEmulator.Content = loc.Get("settings.browse");
        
        // Close button
        BtnClose.Content = loc.Get("settings.close");
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
        SliderUndoHistory.Value = _settings.General.UndoHistoryLimit;
        TxtUndoHistoryValue.Text = _settings.General.UndoHistoryLimit.ToString();

        // Toolchain
        ChkShowWarnings.IsChecked = _settings.Targets.Sms.ShowToolchainWarnings;

        // SMS
        TxtSmsEmulatorArguments.Text = _settings.Targets.Sms.EmulatorArguments;
        ChkSmsLaunchEmulator.IsChecked = _settings.Targets.Sms.LaunchEmulatorAfterBuild;
        TxtSmsEmulatorPath.Text = string.IsNullOrEmpty(_settings.Targets.Sms.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : _settings.Targets.Sms.EmulatorPath;

        // Game Gear
        TxtGgEmulatorArguments.Text = _settings.Targets.Gg.EmulatorArguments;
        ChkGgLaunchEmulator.IsChecked = _settings.Targets.Gg.LaunchEmulatorAfterBuild;
        TxtGgEmulatorPath.Text = string.IsNullOrEmpty(_settings.Targets.Gg.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : _settings.Targets.Gg.EmulatorPath;

        // SG-1000
        TxtSg1000EmulatorArguments.Text = _settings.Targets.Sg1000.EmulatorArguments;
        ChkSg1000LaunchEmulator.IsChecked = _settings.Targets.Sg1000.LaunchEmulatorAfterBuild;
        TxtSg1000EmulatorPath.Text = string.IsNullOrEmpty(_settings.Targets.Sg1000.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : _settings.Targets.Sg1000.EmulatorPath;

        // ColecoVision
        TxtColecoEmulatorArguments.Text = _settings.Targets.Coleco.EmulatorArguments;
        ChkColecoLaunchEmulator.IsChecked = _settings.Targets.Coleco.LaunchEmulatorAfterBuild;
        TxtColecoEmulatorPath.Text = string.IsNullOrEmpty(_settings.Targets.Coleco.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : _settings.Targets.Coleco.EmulatorPath;

        // NES
        TxtNesEmulatorArguments.Text = _settings.Targets.Nes.EmulatorArguments;
        ChkNesLaunchEmulator.IsChecked = _settings.Targets.Nes.LaunchEmulatorAfterBuild;
        TxtNesEmulatorPath.Text = string.IsNullOrEmpty(_settings.Targets.Nes.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured") : _settings.Targets.Nes.EmulatorPath;
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
            
            // Refresh this window
            ApplyLocalization();
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
        _settings.Targets.Sms.ShowToolchainWarnings = ChkShowWarnings.IsChecked == true;
        AutoSave();
    }

    private void TxtSmsEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Sms.EmulatorArguments = TxtSmsEmulatorArguments.Text;
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
            _settings.Targets.Gg.EmulatorPath = dialog.FileName;
            TxtGgEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtGgEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Gg.EmulatorArguments = TxtGgEmulatorArguments.Text;
        AutoSave();
    }

    private void ChkGgLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Gg.LaunchEmulatorAfterBuild = ChkGgLaunchEmulator.IsChecked == true;
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
            _settings.Targets.Sg1000.EmulatorPath = dialog.FileName;
            TxtSg1000EmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtSg1000EmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Sg1000.EmulatorArguments = TxtSg1000EmulatorArguments.Text;
        AutoSave();
    }

    private void ChkSg1000LaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Sg1000.LaunchEmulatorAfterBuild = ChkSg1000LaunchEmulator.IsChecked == true;
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
            _settings.Targets.Coleco.EmulatorPath = dialog.FileName;
            TxtColecoEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtColecoEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Coleco.EmulatorArguments = TxtColecoEmulatorArguments.Text;
        AutoSave();
    }

    private void ChkColecoLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Coleco.LaunchEmulatorAfterBuild = ChkColecoLaunchEmulator.IsChecked == true;
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
            _settings.Targets.Nes.EmulatorPath = dialog.FileName;
            TxtNesEmulatorPath.Text = dialog.FileName;
            AutoSave();
        }
    }

    private void TxtNesEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Nes.EmulatorArguments = TxtNesEmulatorArguments.Text;
        AutoSave();
    }

    private void ChkNesLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.Targets.Nes.LaunchEmulatorAfterBuild = ChkNesLaunchEmulator.IsChecked == true;
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
