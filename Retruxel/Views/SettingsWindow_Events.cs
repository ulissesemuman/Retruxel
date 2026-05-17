using Retruxel.Core.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Views;

public partial class SettingsWindow
{
    #region Event Handlers

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

    #endregion

    #region Auto-Save

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

    #endregion
}
