using Retruxel.Core.Text;
using SkiaSharp;
using System;
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

        // Convert SKBitmap to WPF BitmapSource
        var bitmapSource = ConvertSkBitmapToBitmapSource(scaledBitmap);

        var img = new System.Windows.Controls.Image
        {
            Source = bitmapSource,
            Stretch = Stretch.None
        };

        PreviewCanvas.Children.Add(img);

        skBitmap.Dispose();
        scaledBitmap.Dispose();
    }

    private static BitmapSource ConvertSkBitmapToBitmapSource(SKBitmap skBitmap)
    {
        var info = skBitmap.Info;
        var pixels = skBitmap.GetPixels();

        var bitmap = new WriteableBitmap(info.Width, info.Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.Lock();

        unsafe
        {
            var src = (byte*)pixels.ToPointer();
            var dst = (byte*)bitmap.BackBuffer.ToPointer();
            var stride = bitmap.BackBufferStride;

            for (int y = 0; y < info.Height; y++)
            {
                for (int x = 0; x < info.Width; x++)
                {
                    var srcOffset = (y * info.Width + x) * 4;
                    var dstOffset = y * stride + x * 4;

                    // RGBA → BGRA
                    dst[dstOffset + 0] = src[srcOffset + 2]; // B
                    dst[dstOffset + 1] = src[srcOffset + 1]; // G
                    dst[dstOffset + 2] = src[srcOffset + 0]; // R
                    dst[dstOffset + 3] = src[srcOffset + 3]; // A
                }
            }
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, info.Width, info.Height));
        bitmap.Unlock();

        return bitmap;
    }

    #endregion
}
