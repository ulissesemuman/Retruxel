using Retruxel.Tool.LiveLink.Capture;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.LiveLink.Windows;

public partial class CanvasExpansionWindow : Window
{
    public CanvasExpansionCapture.ExpansionResult? Result { get; private set; }

    private readonly byte[] _currentNametable;
    private readonly byte[]? _romData;
    private readonly byte[]? _ramData;
    private readonly string _sourceConsole;

    public CanvasExpansionWindow(byte[] currentNametable, byte[]? romData, byte[]? ramData, string sourceConsole)
    {
        InitializeComponent();

        _currentNametable = currentNametable;
        _romData = romData;
        _ramData = ramData;
        _sourceConsole = sourceConsole;

        TxtExpandLeft.TextChanged += UpdatePreview;
        TxtExpandRight.TextChanged += UpdatePreview;
        TxtExpandUp.TextChanged += UpdatePreview;
        TxtExpandDown.TextChanged += UpdatePreview;

        UpdatePreview(null, null);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Close();

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
        => Close();

    private void UpdatePreview(object? sender, EventArgs? e)
    {
        if (!int.TryParse(TxtExpandLeft.Text, out int left)) left = 0;
        if (!int.TryParse(TxtExpandRight.Text, out int right)) right = 0;
        if (!int.TryParse(TxtExpandUp.Text, out int up)) up = 0;
        if (!int.TryParse(TxtExpandDown.Text, out int down)) down = 0;

        int newWidth = 32 + left + right;
        int newHeight = 28 + up + down;

        var parts = new List<string>();
        if (left > 0) parts.Add($"{left} left");
        if (right > 0) parts.Add($"{right} right");
        if (up > 0) parts.Add($"{up} up");
        if (down > 0) parts.Add($"{down} down");

        string expansion = parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";

        TxtPreview.Text = $"New size: {newWidth}×{newHeight} tiles{expansion}";
    }

    private void BtnExpand_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtExpandLeft.Text, out int left) || left < 0)
        {
            MessageBox.Show("Invalid 'Left' value. Must be a non-negative integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtExpandRight.Text, out int right) || right < 0)
        {
            MessageBox.Show("Invalid 'Right' value. Must be a non-negative integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtExpandUp.Text, out int up) || up < 0)
        {
            MessageBox.Show("Invalid 'Up' value. Must be a non-negative integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtExpandDown.Text, out int down) || down < 0)
        {
            MessageBox.Show("Invalid 'Down' value. Must be a non-negative integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnExpand.IsEnabled = false;
        BtnExpand.Content = "SEARCHING...";

        try
        {
            // Debug: Log nametable info
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Nametable size: {_currentNametable.Length} bytes");
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] ROM data: {(_romData != null ? $"{_romData.Length} bytes" : "null")}");
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] RAM data: {(_ramData != null ? $"{_ramData.Length} bytes" : "null")}");
            
            // Log first 64 bytes of nametable
            var nametablePreview = string.Join(" ", _currentNametable.Take(64).Select(b => $"{b:X2}"));
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Nametable preview: {nametablePreview}");
            
            // Try ROM first, then RAM
            Result = null;
            
            if (_romData != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Trying ROM search...");
                Result = CanvasExpansionCapture.ExpandCanvas(
                    _currentNametable,
                    _romData,
                    left,
                    right,
                    up,
                    down,
                    _sourceConsole,
                    false);
                System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] ROM search result: {(Result.Success ? "SUCCESS" : "FAILED")} (confidence: {Result.Confidence:P1})");
            }
            
            if ((Result == null || !Result.Success) && _ramData != null)
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Trying RAM search...");
                Result = CanvasExpansionCapture.ExpandCanvas(
                    _currentNametable,
                    _ramData,
                    left,
                    right,
                    up,
                    down,
                    _sourceConsole,
                    true);
                System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] RAM search result: {(Result.Success ? "SUCCESS" : "FAILED")} (confidence: {Result.Confidence:P1})");
            }

            if (Result.Success)
            {
                MessageBox.Show(Result.Message, "Expansion Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                // Show detailed error with confidence score
                string detailedMessage = Result.Message;
                if (Result.Confidence > 0)
                {
                    detailedMessage += $"\n\nBest match confidence: {Result.Confidence:P1}\n" +
                                      $"(Threshold: 50%)";
                }
                MessageBox.Show(detailedMessage, "Expansion Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Exception: {ex}");
            MessageBox.Show($"Expansion error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnExpand.IsEnabled = true;
            BtnExpand.Content = "EXPAND CANVAS";
        }
    }
}
