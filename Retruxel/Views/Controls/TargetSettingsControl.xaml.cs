using Microsoft.Win32;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Views.Controls;

public partial class TargetSettingsControl : UserControl
{
    private AppSettings? _settings;
    private string? _targetId;
    private bool _loading = true;

    public TargetSettingsControl()
    {
        InitializeComponent();
    }

    public void Initialize(AppSettings settings, string targetId)
    {
        _settings = settings;
        _targetId = targetId;
        _loading = true;

        var targetSettings = SettingsService.GetTargetSettings(settings, targetId);
        var loc = LocalizationService.Instance;

        TxtEmulatorPath.Text = string.IsNullOrEmpty(targetSettings.EmulatorPath)
            ? loc.Get("settings.emulator.path.not_configured")
            : targetSettings.EmulatorPath;
        TxtEmulatorArguments.Text = targetSettings.EmulatorArguments;
        ChkLaunchEmulator.IsChecked = targetSettings.LaunchEmulatorAfterBuild;

        _loading = false;
    }

    private void BtnBrowseEmulator_Click(object sender, RoutedEventArgs e)
    {
        if (_settings == null || _targetId == null) return;

        var targetSettings = SettingsService.GetTargetSettings(_settings, _targetId);
        var dialog = new OpenFileDialog
        {
            Title = $"Select {_targetId.ToUpper()} Emulator Executable",
            Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
            InitialDirectory = string.IsNullOrEmpty(targetSettings.EmulatorPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                : Path.GetDirectoryName(targetSettings.EmulatorPath)
        };

        if (dialog.ShowDialog() == true)
        {
            targetSettings.EmulatorPath = dialog.FileName;
            TxtEmulatorPath.Text = dialog.FileName;
            SettingsService.Save(_settings);
        }
    }

    private void TxtEmulatorArguments_Changed(object sender, TextChangedEventArgs e)
    {
        if (_loading || _settings == null || _targetId == null) return;
        SettingsService.GetTargetSettings(_settings, _targetId).EmulatorArguments = TxtEmulatorArguments.Text;
        SettingsService.Save(_settings);
    }

    private void ChkLaunchEmulator_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || _settings == null || _targetId == null) return;
        SettingsService.GetTargetSettings(_settings, _targetId).LaunchEmulatorAfterBuild = ChkLaunchEmulator.IsChecked == true;
        SettingsService.Save(_settings);
    }
}
