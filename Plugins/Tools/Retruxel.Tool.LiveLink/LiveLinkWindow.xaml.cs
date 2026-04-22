using System.Windows;
using System.Windows.Controls;
using Retruxel.Core.Interfaces;
using Retruxel.Tool.LiveLink.Emulators;
using Retruxel.Tool.LiveLink.Services;

namespace Retruxel.Tool.LiveLink;

public partial class LiveLinkWindow : Window
{
    private IEmulatorConnection? _connection;
    private CaptureResult? _lastCapture;
    private readonly List<IEmulatorConnection> _availableEmulators = new();

    public LiveLinkWindow()
    {
        InitializeComponent();
        DiscoverEmulators();
    }

    private void DiscoverEmulators()
    {
        _availableEmulators.Add(new MesenConnection());
        _availableEmulators.Add(new EmuliciousConnection());
        _availableEmulators.Add(new BgbConnection());
        _availableEmulators.Add(new MesenSConnection());
        _availableEmulators.Add(new MgbaConnection());
        
        foreach (var emu in _availableEmulators)
        {
            CmbEmulator.Items.Add($"{emu.DisplayName} ({string.Join(", ", emu.SupportedTargets.Select(t => t.ToUpper()))})");
        }
        
        CmbEmulator.SelectedIndex = 0;
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        if (_connection?.IsConnected == true)
        {
            await _connection.DisconnectAsync();
            _connection = null;
            BtnConnect.Content = "CONNECT";
            BtnCapture.IsEnabled = false;
            TxtStatus.Text = "Disconnected";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Gray;
            return;
        }

        try
        {
            TxtStatus.Text = "Connecting...";
            
            _connection = _availableEmulators[CmbEmulator.SelectedIndex];
            bool connected = await _connection.ConnectAsync();
            
            if (connected)
            {
                BtnConnect.Content = "DISCONNECT";
                BtnCapture.IsEnabled = true;
                TxtStatus.Text = $"Connected to {_connection.DisplayName}";
                TxtStatus.Foreground = System.Windows.Media.Brushes.LimeGreen;
            }
            else
            {
                _connection = null;
                TxtStatus.Text = "Connection failed. Make sure emulator is running with debug API enabled.";
                TxtStatus.Foreground = System.Windows.Media.Brushes.OrangeRed;
            }
        }
        catch (Exception ex)
        {
            _connection = null;
            MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Connection error";
            TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
        }
    }

    private async void BtnCapture_Click(object sender, RoutedEventArgs e)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            MessageBox.Show("Not connected to emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            TxtPreview.Text = "Capturing...";
            
            var capture = new CaptureResult();
            
            if (ChkCaptureTiles.IsChecked == true)
            {
                byte[] vramData = await _connection.ReadVramAsync(0x0000, 0x4000);
                capture.Tiles = VramDecoder.DecodePlanarTiles(vramData, 512, 4, 8, 8, false);
            }
            
            if (ChkCaptureNametable.IsChecked == true)
            {
                byte[] nametableData = await _connection.ReadVramAsync(0x3800, 32 * 28 * 2);
                capture.Nametable = VramDecoder.DecodeNametable(nametableData, 32, 28);
                capture.NametableWidth = 32;
                capture.NametableHeight = 28;
            }
            
            if (ChkCapturePalette.IsChecked == true)
            {
                byte[] paletteData = await _connection.ReadMemoryAsync(0xC000, 32);
                capture.Palette = DecodePalette(paletteData);
            }
            
            _lastCapture = capture;
            BtnExport.IsEnabled = true;
            
            TxtPreview.Text = $"Capture complete!\n\n" +
                             $"Tiles: {capture.Tiles.Length}\n" +
                             $"Nametable: {capture.Nametable.Length} entries ({capture.NametableWidth}x{capture.NametableHeight})\n" +
                             $"Palette: {capture.Palette.Length} colors";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Capture error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtPreview.Text = "Capture failed.";
        }
    }
    
    private uint[] DecodePalette(byte[] paletteData)
    {
        var palette = new uint[paletteData.Length];
        for (int i = 0; i < paletteData.Length; i++)
        {
            byte smsColor = paletteData[i];
            byte r = (byte)(((smsColor >> 0) & 0x03) * 85);
            byte g = (byte)(((smsColor >> 2) & 0x03) * 85);
            byte b = (byte)(((smsColor >> 4) & 0x03) * 85);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCapture == null)
        {
            MessageBox.Show("No capture data to export.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png|JSON Data|*.json",
            FileName = "capture"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // TODO: Export capture to PNG or JSON
                MessageBox.Show($"Export to {dialog.FileName} not yet implemented.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
