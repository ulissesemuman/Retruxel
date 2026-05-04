using Retruxel.Core.Interfaces;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.LiveLink.Services;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.LiveLink.Windows;

public partial class PaletteOptimizationWindow : Window
{
    // ── State ─────────────────────────────────────────────────────────────────

    private readonly BitmapSource _originalBitmap;
    private readonly int _targetColorCount;
    private readonly ITarget _target;

    private bool _useLab;
    private double _currentDiversity = 1.25;

    // Cached on construction to avoid re-extracting per preview update
    private readonly List<(byte R, byte G, byte B)> _originalPixels;

    // ── Results ───────────────────────────────────────────────────────────────

    public double SelectedDiversity => _currentDiversity;
    public List<(byte R, byte G, byte B)> OptimizedPalette { get; private set; } = [];
    public BitmapSource OptimizedBitmap { get; private set; } = null!;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the palette optimization preview for the given bitmap and target.
    /// </summary>
    /// <param name="originalBitmap">Source image captured from the emulator or imported.</param>
    /// <param name="targetColorCount">Maximum number of colors in the optimized palette.</param>
    /// <param name="useLab">True to use LAB color space for nearest-color matching.</param>
    /// <param name="target">Active target — used to obtain the hardware palette via ITarget.</param>
    public PaletteOptimizationWindow(
        BitmapSource originalBitmap,
        int targetColorCount,
        bool useLab,
        ITarget target)
    {
        _originalBitmap = originalBitmap;
        _targetColorCount = targetColorCount;
        _useLab = useLab;
        _target = target;

        _originalPixels = ExtractPixels(_originalBitmap);

        InitializeComponent();

        ImgOriginal.Source = _originalBitmap;
        UpdateOptimizedPreview();
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void SliderDiversity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _currentDiversity = SliderDiversity.Value / 100.0;
        TxtDiversityValue.Text = _currentDiversity.ToString("F2");
        UpdateOptimizedPreview();
    }

    private void ColorSpace_Changed(object sender, RoutedEventArgs e)
    {
        _useLab = RbLab.IsChecked == true;
        UpdateOptimizedPreview();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // ── Preview pipeline ──────────────────────────────────────────────────────

    private void UpdateOptimizedPreview()
    {
        if (_originalPixels.Count == 0 || ImgOptimized == null) return;

        var rgbPalette = PaletteOptimizer.OptimizePalette(
            _originalPixels, _targetColorCount, _currentDiversity);

        OptimizedPalette = MapToHardwarePalette(rgbPalette);

        var bitmap = ApplyPalette(_originalBitmap, OptimizedPalette);
        ImgOptimized.Source = bitmap;
        OptimizedBitmap = bitmap;

        RefreshPaletteSwatches();
    }

    // ── Color mapping ─────────────────────────────────────────────────────────

    /// <summary>
    /// Maps an RGB palette to the nearest colors in the target's hardware palette.
    /// Uses ITarget.GetHardwarePalette() — no hardcoded target IDs.
    /// </summary>
    private List<(byte R, byte G, byte B)> MapToHardwarePalette(
        List<(byte R, byte G, byte B)> rgbPalette)
    {
        var hardwareColors = _target.GetHardwarePalette();
        if (hardwareColors.Count == 0) return rgbPalette;

        var hardwareRgb = hardwareColors
            .Select(c => (c.R, c.G, c.B))
            .ToList();

        return rgbPalette
            .Select(c => FindClosestColor(c, hardwareRgb))
            .ToList();
    }

    private (byte R, byte G, byte B) FindClosestColor(
        (byte R, byte G, byte B) color,
        List<(byte R, byte G, byte B)> palette)
    {
        return _useLab
            ? ColorMatching.FindNearestLab(color, palette)
            : ColorMatching.FindNearestRgb(color, palette);
    }

    // ── Bitmap helpers ────────────────────────────────────────────────────────

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

    private BitmapSource ApplyPalette(
        BitmapSource original,
        List<(byte R, byte G, byte B)> palette)
    {
        if (palette.Count == 0) return original;

        var converted = EnsureBgra32(original);
        int width  = converted.PixelWidth;
        int height = converted.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[height * stride];
        converted.CopyPixels(pixels, stride, 0);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            var closest = FindClosestColor(
                (pixels[i + 2], pixels[i + 1], pixels[i]), palette);

            pixels[i + 2] = closest.R;
            pixels[i + 1] = closest.G;
            pixels[i]     = closest.B;
        }

        return BitmapSource.Create(
            width, height, 96, 96,
            PixelFormats.Bgra32, null, pixels, stride);
    }

    private static BitmapSource EnsureBgra32(BitmapSource bitmap)
    {
        if (bitmap.Format == PixelFormats.Bgra32) return bitmap;
        return new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
    }

    // ── Palette swatch strip ──────────────────────────────────────────────────

    private void RefreshPaletteSwatches()
    {
        if (PalettePreview == null) return;

        var brushes = OptimizedPalette
            .Distinct()
            .OrderBy(c  => RgbToHue(c.R, c.G, c.B))
            .ThenBy(c   => RgbToLightness(c.R, c.G, c.B))
            .ThenBy(c   => RgbToSaturation(c.R, c.G, c.B))
            .Select(c   => new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)))
            .ToList();

        PalettePreview.ItemsSource = brushes;
    }

    // ── Color space math ──────────────────────────────────────────────────────

    private static double RgbToHue(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        double max   = Math.Max(rd, Math.Max(gd, bd));
        double min   = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        if (delta == 0) return 0;

        double hue;
        if      (max == rd) hue = ((gd - bd) / delta) % 6;
        else if (max == gd) hue = (bd - rd) / delta + 2;
        else                hue = (rd - gd) / delta + 4;

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
        double max   = Math.Max(rd, Math.Max(gd, bd));
        double min   = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        if (delta == 0) return 0;

        double lightness = (max + min) / 2.0;
        return delta / (1 - Math.Abs(2 * lightness - 1));
    }
}
