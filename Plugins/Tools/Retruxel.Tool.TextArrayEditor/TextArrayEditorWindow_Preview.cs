using Retruxel.Core.Text;
using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TextArrayEditor;

public partial class TextArrayEditorWindow
{
    #region Preview

    private void RenderPreview(string text)
    {
        PreviewCanvas.Children.Clear();

        if (string.IsNullOrEmpty(text))
        {
            TxtPreviewWarning.Visibility = Visibility.Visible;
            TxtPreviewWarning.Text = "Type a string to see preview";
            return;
        }

        // Check for unsupported characters
        var unsupportedChars = text.Where(c => !DefaultFont.Supports(c)).Distinct().ToList();
        if (unsupportedChars.Count > 0)
        {
            TxtPreviewWarning.Visibility = Visibility.Visible;
            TxtPreviewWarning.Text = $"Unsupported characters: {string.Join(", ", unsupportedChars.Select(c => $"'{c}'"))}";
        }
        else
        {
            TxtPreviewWarning.Visibility = Visibility.Collapsed;
        }

        // Render using DefaultFont with SkiaSharp
        var skBitmap = DefaultFont.RenderString(text, SKColors.White, SKColors.Transparent);
        if (skBitmap is null) return;

        // Scale 2x for preview
        var scaledBitmap = new SKBitmap(skBitmap.Width * 2, skBitmap.Height * 2);
        using (var canvas = new SKCanvas(scaledBitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(skBitmap, SKRect.Create(0, 0, scaledBitmap.Width, scaledBitmap.Height),
                new SKPaint { FilterQuality = SKFilterQuality.None });
        }

        var img = new System.Windows.Controls.Image
        {
            // Convert SKBitmap to WPF BitmapSource
            Source = ImageProcessing.ConvertSkBitmapToBitmapSource(scaledBitmap),
            Stretch = Stretch.None
        };

        PreviewCanvas.Children.Add(img);

        skBitmap.Dispose();
        scaledBitmap.Dispose();
    }
    #endregion
}
