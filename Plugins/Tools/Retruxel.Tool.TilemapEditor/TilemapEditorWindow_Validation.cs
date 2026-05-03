using Retruxel.Core.Models;
using SkiaSharp;
using System.IO;
using System.Windows;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    /// <summary>
    /// Validates if the selected asset contains only colors from the target hardware palette.
    /// </summary>
    private bool ValidateAssetColors(out List<(byte R, byte G, byte B)> invalidColors)
    {
        invalidColors = new List<(byte R, byte G, byte B)>();

        if (CmbTilesetAsset.SelectedItem == null)
            return true;

        var assetId = CmbTilesetAsset.SelectedItem.ToString()!;
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
        
        if (asset == null)
            return true;

        var assetPath = Path.Combine(_projectPath, asset.RelativePath);
        
        if (!File.Exists(assetPath))
            return true;

        // Load image
        using var stream = File.OpenRead(assetPath);
        using var bitmap = SKBitmap.Decode(stream);
        
        if (bitmap == null)
            return true;

        // Get hardware palette
        var hardwarePalette = _target.GetHardwarePalette();
        
        if (hardwarePalette == null || hardwarePalette.Count == 0)
            return true; // No hardware restrictions

        // Convert to RGB tuples for comparison
        var hardwareRgb = hardwarePalette.Select(c => (c.R, c.G, c.B)).ToHashSet();

        // Extract unique colors from image
        var imageColors = new HashSet<(byte R, byte G, byte B)>();
        
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                
                // Skip transparent pixels
                if (pixel.Alpha == 0)
                    continue;

                var rgb = (pixel.Red, pixel.Green, pixel.Blue);
                
                if (!hardwareRgb.Contains(rgb))
                    invalidColors.Add(rgb);
                
                imageColors.Add(rgb);
            }
        }

        return invalidColors.Count == 0;
    }

    /// <summary>
    /// Shows a dialog when invalid colors are detected, offering options to fix or cancel.
    /// </summary>
    private bool HandleInvalidColors(List<(byte R, byte G, byte B)> invalidColors)
    {
        var uniqueInvalid = invalidColors.Distinct().Take(10).ToList();
        var colorList = string.Join("\n", uniqueInvalid.Select(c => $"  RGB({c.R}, {c.G}, {c.B})"));
        
        if (invalidColors.Count > 10)
            colorList += $"\n  ... and {invalidColors.Count - 10} more";

        var message = $"The selected asset contains colors not supported by {_target.DisplayName}.\n\n" +
                     $"Invalid colors found:\n{colorList}\n\n" +
                     $"What would you like to do?\n\n" +
                     $"• Optimize Colors - Open palette optimization tool to reduce colors\n" +
                     $"• Choose Another Asset - Return to editor to select a different asset\n" +
                     $"• Cancel - Close this dialog";

        var result = MessageBox.Show(
            message,
            "Invalid Colors Detected",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Yes);

        if (result == MessageBoxResult.Yes)
        {
            // Open diversity/optimization window
            return OpenColorOptimizationForAsset();
        }
        else if (result == MessageBoxResult.No)
        {
            // Return to editor (do nothing, keep dialog open)
            return false;
        }
        else
        {
            // Cancel - close dialog
            return false;
        }
    }

    /// <summary>
    /// Opens the palette optimization window for the selected asset.
    /// </summary>
    private bool OpenColorOptimizationForAsset()
    {
        try
        {
            var assetId = CmbTilesetAsset.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(assetId))
                return false;

            var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
                return false;

            var assetPath = Path.Combine(_projectPath, asset.RelativePath);
            if (!File.Exists(assetPath))
                return false;

            // Load image as BitmapSource for WPF
            using var stream = File.OpenRead(assetPath);
            using var skBitmap = SKBitmap.Decode(stream);
            
            if (skBitmap == null)
                return false;

            // Convert SKBitmap to WPF BitmapSource
            var bitmapSource = ConvertSkBitmapToBitmapSource(skBitmap);

            // Reduce to hardware palette first (like AssetImporter does)
            var hardwarePalette = _target.GetHardwarePalette();
            var rgbPalette = hardwarePalette.Select(c => (c.R, c.G, c.B)).ToList();
            
            using var reducedBitmap = ReduceColorsToHardware(skBitmap, rgbPalette);
            var reducedBitmapSource = ConvertSkBitmapToBitmapSource(reducedBitmap);

            // Calculate target color count: half of hardware palette, minimum 16
            int hardwareColorCount = hardwarePalette.Count;
            int targetColorCount = Math.Max(16, hardwareColorCount / 2);

            // Open palette optimization window
            var optimizationWindow = new Tool.LiveLink.Windows.PaletteOptimizationWindow(
                reducedBitmapSource,
                targetColorCount: targetColorCount,
                useLab: true,
                targetConsole: _target.TargetId)
            {
                Owner = this
            };

            if (optimizationWindow.ShowDialog() == true)
            {
                // Save optimized image back to asset
                var optimizedBitmap = optimizationWindow.OptimizedBitmap;
                SaveBitmapSourceToFile(optimizedBitmap, assetPath);

                // Reload tileset
                CmbTilesetAsset_SelectionChanged(null!, null!);

                MessageBox.Show(
                    $"Asset '{assetId}' has been optimized and saved.\n\n" +
                    $"Colors reduced to {optimizationWindow.OptimizedPalette.Count} using diversity {optimizationWindow.SelectedDiversity:F2}.",
                    "Optimization Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to optimize colors: {ex.Message}",
                "Optimization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private SKBitmap ReduceColorsToHardware(SKBitmap source, List<(byte R, byte G, byte B)> hardwarePalette)
    {
        var result = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);

                if (pixel.Alpha == 0)
                {
                    result.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                var nearest = Retruxel.Lib.ImageProcessing.ColorMatching.FindNearestRgb(
                    (pixel.Red, pixel.Green, pixel.Blue), hardwarePalette);
                result.SetPixel(x, y, new SKColor(nearest.R, nearest.G, nearest.B, pixel.Alpha));
            }
        }

        return result;
    }

    private System.Windows.Media.Imaging.BitmapSource ConvertSkBitmapToBitmapSource(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        var memoryStream = new MemoryStream();
        data.SaveTo(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = memoryStream;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    private void SaveBitmapSourceToFile(System.Windows.Media.Imaging.BitmapSource bitmap, string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        encoder.Save(fileStream);
    }
}
