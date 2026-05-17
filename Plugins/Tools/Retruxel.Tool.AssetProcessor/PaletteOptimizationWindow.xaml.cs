using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Lib.WPFImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Retruxel.Lib.ImageProcessing.ColorMatching;

namespace Retruxel.Tool.AssetProcessor;

public partial class PaletteOptimizationWindow : Window
{
    private readonly SKBitmap _originalBitmap;
    private readonly int _targetColorCount;
    private readonly ITarget _target;

    private SKBitmap _previewBitmap;
    private List<HardwareColor> _hardwarePalette;

    private DistanceMode _distanceMode;
    private double _currentDiversity = 1.25;

    // Cached on construction to avoid re-extracting per preview update
    private readonly List<(byte R, byte G, byte B)> _originalPixels;

    public double SelectedDiversity => _currentDiversity;
    public DistanceMode ColorSpace => _distanceMode;
    public List<(byte R, byte G, byte B)> OptimizedPalette { get; private set; } = [];
    public SKBitmap OptimizedBitmap { get; private set; } = null!;

    public byte[] MapIndex;

    /// <summary>
    /// Opens the palette optimization preview for the given bitmap and target.
    /// </summary>
    /// <param name="originalBitmap">Source image captured from the emulator or imported.</param>
    /// <param name="targetColorCount">Maximum number of colors in the optimized palette.</param>
    /// <param name="distanceMode">Distance mode for color matching (RGB, LAB, Perceptual).</param>
    /// <param name="target">Active target — used to obtain the hardware palette via ITarget.</param>
    public PaletteOptimizationWindow(
        SKBitmap originalBitmap,
        int targetColorCount,
        DistanceMode distanceMode,
        ITarget target)
    {
        _originalBitmap = originalBitmap;
        _targetColorCount = targetColorCount;
        _distanceMode = distanceMode;
        _target = target;

        _originalPixels = IndexedBitmapRenderer.ExtractPixels(_originalBitmap);
        MapIndex = new byte[_originalPixels.Count];

        InitializeComponent();

        ImgOriginal.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(_originalBitmap);
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
        MapIndex = IndexedBitmapRenderer.Encode(_previewBitmap, _hardwarePalette, DistanceMode.RGB);

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateOptimizedPreview()
    {
        if (_originalPixels.Count == 0 || ImgOptimized == null) return;

        var reducedPalette = ColorMatching.OptimizePalette(
            _originalPixels, _targetColorCount, _currentDiversity);

        _hardwarePalette = ColorMatching.QuantizePalette(reducedPalette, _target.GetHardwarePalette(), _distanceMode);

        _hardwarePalette = SortPalette(_hardwarePalette);

        OptimizedPalette = _hardwarePalette.Select(c => (c.R, c.G, c.B)).ToList();

        var bitmap = ApplyPalette(_hardwarePalette);
        ImgOptimized.Source = ImageProcessing.ConvertSkBitmapToBitmapSource(bitmap);
        OptimizedBitmap = bitmap;

        RefreshPaletteSwatches();
    }

    private List<HardwareColor> SortPalette(List<HardwareColor> palette)
    {
        return palette
                    .OrderBy(c => RgbToHue(c.R, c.G, c.B))
                    .ThenBy(c => RgbToLightness(c.R, c.G, c.B))
                    .ThenBy(c => RgbToSaturation(c.R, c.G, c.B))
                    .ToList();
    }

    private SKBitmap ApplyPalette(IReadOnlyList<HardwareColor> palette)
    {
        if (palette.Count == 0) return _originalBitmap;

        var fastPalette = ColorMatching.PrepareFastPalette(palette, _distanceMode);

        int width = _originalBitmap.Width;
        int height = _originalBitmap.Height;
        int stride = width * 4;
        byte[] outputPixels = new byte[height * stride];

        Parallel.For(0, height, y =>
        {
            int rowStart = y * width;
            int byteStart = rowStart * 4;

            for (int x = 0; x < width; x++)
            {
                var color = _originalBitmap.GetPixel(x, y);
                byte index = ColorMatching.FindNearestColorIndex((color.Red, color.Green, color.Blue), fastPalette, _distanceMode);
                var finalColor = palette[index];

                int offset = byteStart + x * 4;
                outputPixels[offset] = finalColor.B;
                outputPixels[offset + 1] = finalColor.G;
                outputPixels[offset + 2] = finalColor.R;
                outputPixels[offset + 3] = 255;
            }
        });

        if (_previewBitmap == null)
            _previewBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        unsafe
        {
            var ptr = _previewBitmap.GetPixels();
            System.Runtime.InteropServices.Marshal.Copy(outputPixels, 0, ptr, outputPixels.Length);
        }

        return _previewBitmap;
    }

    private void ConfirmPalette(IReadOnlyList<HardwareColor> palette)
    {
        var fastPalette = ColorMatching.PrepareFastPalette(palette, _distanceMode);

        int width = _originalBitmap.Width;
        int height = _originalBitmap.Height;

        Parallel.For(0, height, y =>
        {
            int rowStart = y * width;
            for (int x = 0; x < width; x++)
            {
                var color = _originalBitmap.GetPixel(x, y);
                byte index = ColorMatching.FindNearestColorIndex((color.Red, color.Green, color.Blue), fastPalette, _distanceMode);
            }
        });
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
