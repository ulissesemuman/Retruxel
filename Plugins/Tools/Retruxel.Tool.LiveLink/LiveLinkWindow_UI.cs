using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// UI management: logging, button handlers, window events.
/// </summary>
public partial class LiveLinkWindow
{
    private void AppendLog(string message, Brush foreground)
    {
        var entry = new TextBlock
        {
            Text = $"> {message}",
            Foreground = foreground,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 2)
        };

        LogPanel.Children.Add(entry);
        LogScrollViewer.ScrollToBottom();
    }

    private void LogInfo(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)));
    private void LogSuccess(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)));
    private void LogWarning(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)));
    private void LogError(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)));

    private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
    {
        var lines = LogPanel.Children
            .OfType<TextBlock>()
            .Select(tb => tb.Text);

        var logText = string.Join(Environment.NewLine, lines);

        if (string.IsNullOrEmpty(logText))
        {
            MessageBox.Show("No log to copy.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Clipboard.SetText(logText);
            LogInfo("Log copied to clipboard");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogPanel.Children.Clear();
        LogInfo("Log cleared");
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private string? SelectRomFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select ROM File",
            Filter = "All Supported ROMs|*.nes;*.sfc;*.smc;*.sms;*.gg;*.sg;*.col;*.gb;*.gbc;*.gba;*.ws;*.wsc;*.pce|" +
                     "NES ROMs (*.nes)|*.nes|" +
                     "SNES ROMs (*.sfc, *.smc)|*.sfc;*.smc|" +
                     "SMS/GG/SG-1000 ROMs (*.sms, *.gg, *.sg)|*.sms;*.gg;*.sg|" +
                     "ColecoVision ROMs (*.col)|*.col|" +
                     "Game Boy ROMs (*.gb, *.gbc)|*.gb;*.gbc|" +
                     "GBA ROMs (*.gba)|*.gba|" +
                     "WonderSwan ROMs (*.ws, *.wsc)|*.ws;*.wsc|" +
                     "PC Engine ROMs (*.pce)|*.pce|" +
                     "All Files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
