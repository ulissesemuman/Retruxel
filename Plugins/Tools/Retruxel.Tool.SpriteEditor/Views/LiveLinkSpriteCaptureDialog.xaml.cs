using System.Windows;
using System.Windows.Controls;

namespace Retruxel.Tool.SpriteEditor.Views;

public partial class LiveLinkSpriteCaptureDialog : Window
{
    public string AssetName { get; private set; } = "sprite";
    public int StartTile { get; private set; } = 256;
    public int TileCount { get; private set; } = 32;

    public LiveLinkSpriteCaptureDialog()
    {
        InitializeComponent();
        UpdateVramSize();
    }

    private void TxtTileCount_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateVramSize();
    }

    private void UpdateVramSize()
    {
        if (int.TryParse(TxtTileCount.Text, out var count))
        {
            var bytes = count * 32; // SMS: 32 bytes per tile (4bpp planar)
            TxtVramSize.Text = $"({bytes} bytes)";
        }
    }

    private void BtnCapture_Click(object sender, RoutedEventArgs e)
    {
        AssetName = TxtAssetName.Text;

        if (!int.TryParse(TxtStartTile.Text, out var start) || start < 0)
        {
            MessageBox.Show("Invalid start tile.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtTileCount.Text, out var count) || count <= 0)
        {
            MessageBox.Show("Invalid tile count.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StartTile = start;
        TileCount = count;

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
