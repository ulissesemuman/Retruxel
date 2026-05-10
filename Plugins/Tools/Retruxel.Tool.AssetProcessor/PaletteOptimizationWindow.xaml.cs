using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Retruxel.Lib.ImageProcessing.ColorMatching;

namespace Retruxel.Tool.AssetProcessor;

public partial class PaletteOptimizationWindow : Window
{
    private readonly BitmapSource _originalBitmap;
    private readonly int _targetColorCount;
    private readonly ITarget _target;

    private WriteableBitmap? _previewBitmap;
    private List<HardwareColor> _hardwarePalette;

    private DistanceMode _distanceMode;
    private double _currentDiversity = 1.25;

    // Cached on construction to avoid re-extracting per preview update
    private readonly List<(byte R, byte G, byte B)> _originalPixels;

    public double SelectedDiversity => _currentDiversity;
    public List<(byte R, byte G, byte B)> OptimizedPalette { get; private set; } = [];
    public BitmapSource OptimizedBitmap { get; private set; } = null!;

    public byte[] MapIndex;

    /// <summary>
    /// Opens the palette optimization preview for the given bitmap and target.
    /// </summary>
    /// <param name="originalBitmap">Source image captured from the emulator or imported.</param>
    /// <param name="targetColorCount">Maximum number of colors in the optimized palette.</param>
    /// <param name="distanceMode">Distance mode for color matching (RGB, LAB, Perceptual).</param>
    /// <param name="target">Active target — used to obtain the hardware palette via ITarget.</param>
    public PaletteOptimizationWindow(
        BitmapSource originalBitmap,
        int targetColorCount,
        DistanceMode distanceMode,
        ITarget target)
    {
        _originalBitmap = originalBitmap;
        _targetColorCount = targetColorCount;
        _distanceMode = distanceMode;
        _target = target;

        _originalPixels = ExtractPixels(_originalBitmap);
        MapIndex = new byte[_originalPixels.Count];

        InitializeComponent();

        ImgOriginal.Source = _originalBitmap;
        UpdateOptimizedPreview();
    }

    private void SliderDiversity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _currentDiversity = SliderDiversity.Value / 100.0;
        TxtDiversityValue.Text = _currentDiversity.ToString("F2");
        UpdateOptimizedPreview();
    }

    private void ColorSpace_Changed(object sender, RoutedEventArgs e)
    {
        if (RbPerceptual.IsChecked == true)
            _distanceMode = DistanceMode.Perceptual;
        else if (RbLab.IsChecked == true)
            _distanceMode = DistanceMode.LAB;   
        else
            _distanceMode = DistanceMode.RGB;

        UpdateOptimizedPreview();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        ConfirmPalette(_hardwarePalette);
        
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static List<(byte R, byte G, byte B)> ExtractPixels(BitmapSource bitmap)
    {
        var converted = EnsureBgra32(bitmap);
        int stride = converted.PixelWidth * 4;
        byte[] pixels = new byte[converted.PixelHeight * stride];
        converted.CopyPixels(pixels, stride, 0);

        var result = new List<(byte R, byte G, byte B)>(pixels.Length / 4);
        for (int i = 0; i < pixels.Length; i += 4)
            result.Add((pixels[i + 2], pixels[i + 1], pixels[i]));

        return result;
    }

    private void UpdateOptimizedPreview()
    {
        if (_originalPixels.Count == 0 || ImgOptimized == null) return;

        var reducedPalette = ColorMatching.OptimizePalette(
            _originalPixels, _targetColorCount, _currentDiversity);

        _hardwarePalette = ColorMatching.QuantizePalette(reducedPalette, _target.GetHardwarePalette(), _distanceMode);

        OptimizedPalette = _hardwarePalette.Select(c => (c.R, c.G, c.B)).ToList();

        var bitmap = ApplyPalette(_hardwarePalette);
        ImgOptimized.Source = bitmap;
        OptimizedBitmap = bitmap;

        RefreshPaletteSwatches();
    }

    private BitmapSource ApplyPalette(IReadOnlyList<HardwareColor> palette)
    {
        if (palette.Count == 0) return _originalBitmap;

        var fastPalette = ColorMatching.PrepareFastPalette(palette, _distanceMode);

        int width = _originalBitmap.PixelWidth;
        int height = _originalBitmap.PixelHeight;
        int stride = width * 4;
        byte[] outputPixels = new byte[height * stride];

        Parallel.For(0, height, y =>
        {
            int rowStart = y * width;
            int byteStart = rowStart * 4;

            for (int x = 0; x < width; x++)
            {
                var color = _originalPixels[rowStart + x];
                byte index = ColorMatching.FindNearestColorIndex(color, fastPalette, _distanceMode);
                var finalColor = palette[index];

                int offset = byteStart + x * 4;
                outputPixels[offset] = finalColor.B;
                outputPixels[offset + 1] = finalColor.G;
                outputPixels[offset + 2] = finalColor.R;
                outputPixels[offset + 3] = 255;
            }
        });

        //return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, outputPixels, stride);

        if (_previewBitmap == null)
            _previewBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

        _previewBitmap.WritePixels(
            new System.Windows.Int32Rect(0, 0, width, height),
            outputPixels, stride, 0);

        return _previewBitmap;
    }

    private void ConfirmPalette(IReadOnlyList<HardwareColor> palette)
    {
        var fastPalette = ColorMatching.PrepareFastPalette(palette, _distanceMode);

        int width = _originalBitmap.PixelWidth;
        int height = _originalBitmap.PixelHeight;

        Parallel.For(0, height, y =>
        {
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                var color = _originalPixels[rowStart + x];
                MapIndex[rowStart + x] = ColorMatching.FindNearestColorIndex(color, fastPalette, _distanceMode);
            }
        });
    }

    private BitmapSource ApplyPalette2(IReadOnlyList<HardwareColor> palette)
    {
        if (palette.Count == 0) return _originalBitmap;

        var fastPalette = ColorMatching.PrepareFastPalette(palette, _distanceMode);

        int width = _originalBitmap.PixelWidth;
        int height = _originalBitmap.PixelHeight;
        int stride = width * 4;





        var info = new SKImageInfo(width, height, SKColorType.Gray8, SKAlphaType.Opaque);

        var bitmap = new SKBitmap(info);

        Span<byte> skSpan = bitmap.GetPixelSpan();

        MapIndex.CopyTo(skSpan);

        byte[] tableR = new byte[256];
        byte[] tableG = new byte[256];
        byte[] tableB = new byte[256];

        for (int i = 0; i < palette.Count; i++)
        {
            var color = palette[i];

            byte index = ColorMatching.FindNearestColorIndex((color.R, color.G, color.B), fastPalette, _distanceMode);

            tableR[i] = color.R;
            tableG[i] = color.G;
            tableB[i] = color.B;
        }

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var paint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateTable(null, tableR, tableG, tableB)
        };

        surface.Canvas.DrawBitmap(bitmap, 0, 0, paint);

        var bitmapSource = new WriteableBitmap(
            width, height,
            96, 96,
            PixelFormats.Pbgra32,
            null);

        using (var finalImage = surface.Snapshot())
        using (var pixmap = finalImage.PeekPixels())
        {
            var dstInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            bitmapSource.Lock();

            pixmap.ReadPixels(dstInfo, bitmapSource.BackBuffer, bitmapSource.BackBufferStride);

            bitmapSource.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmapSource.Unlock();
        }

        return bitmapSource;







        byte[] outputPixels = new byte[height * stride];

        for (int i = 0; i < _originalPixels.Count; i++)
        {
            var color = _originalPixels[i];
            byte index = ColorMatching.FindNearestColorIndex(color, fastPalette, _distanceMode);
            var finalColor = palette[index];
            MapIndex[i] = index;

            int pixelOffset = i * 4;
            outputPixels[pixelOffset] = finalColor.B;     // Blue
            outputPixels[pixelOffset + 1] = finalColor.G; // Green
            outputPixels[pixelOffset + 2] = finalColor.R; // Red
            outputPixels[pixelOffset + 3] = 255;          // Alpha
        }

        return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, outputPixels, stride);
    }

    private static BitmapSource EnsureBgra32(BitmapSource bitmap)
    {
        if (bitmap.Format == PixelFormats.Bgra32) return bitmap;
        return new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
    }

    private void RefreshPaletteSwatches()
    {
        if (PalettePreview == null) return;

        var brushes = OptimizedPalette
            .Distinct()
            .OrderBy(c => RgbToHue(c.R, c.G, c.B))
            .ThenBy(c => RgbToLightness(c.R, c.G, c.B))
            .ThenBy(c => RgbToSaturation(c.R, c.G, c.B))
            .Select(c => new SolidColorBrush(System.Windows.Media.Color.FromRgb(c.R, c.G, c.B)))
            .ToList();

        PalettePreview.ItemsSource = brushes;
    }

    private static double RgbToHue(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        if (delta == 0) return 0;

        double hue;
        if (max == rd) hue = ((gd - bd) / delta) % 6;
        else if (max == gd) hue = (bd - rd) / delta + 2;
        else hue = (rd - gd) / delta + 4;

        hue *= 60;
        return hue < 0 ? hue + 360 : hue;
    }

    private static double RgbToLightness(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        return (max + min) / 2.0;
    }

    private static double RgbToSaturation(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        if (delta == 0) return 0;

        double lightness = (max + min) / 2.0;
        return delta / (1 - Math.Abs(2 * lightness - 1));
    }
}
