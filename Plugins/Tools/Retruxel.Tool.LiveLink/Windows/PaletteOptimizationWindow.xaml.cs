using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Retruxel.Tool.LiveLink.Services;

namespace Retruxel.Tool.LiveLink.Windows
{
    public partial class PaletteOptimizationWindow : Window
    {
        private readonly BitmapSource _originalBitmap;
        private readonly int _targetColorCount;
        private bool _useLab;
        private readonly string _targetConsole;
        private double _currentDiversity = 1.25;
        
        // Store original pixels to avoid re-extracting
        private readonly List<(byte R, byte G, byte B)> _originalPixels;
        
        public double SelectedDiversity => _currentDiversity;
        public List<(byte R, byte G, byte B)> OptimizedPalette { get; private set; } = new();
        public BitmapSource OptimizedBitmap { get; private set; } = null!;

        public PaletteOptimizationWindow(BitmapSource originalBitmap, int targetColorCount, bool useLab, string targetConsole)
        {
            InitializeComponent();
            
            _originalBitmap = originalBitmap;
            _targetColorCount = targetColorCount;
            _useLab = useLab;
            _targetConsole = targetConsole;
            
            System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] Target console: {targetConsole}");
            System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] Target color count: {targetColorCount}");
            System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] Use LAB: {useLab}");
            
            // Show original image (with source console colors)
            ImgOriginal.Source = _originalBitmap;
            
            // Extract pixels once
            _originalPixels = ExtractPixels(_originalBitmap);
            System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] Extracted {_originalPixels.Count} pixels");
            
            // Count unique colors in original
            var uniqueColors = _originalPixels.Distinct().Count();
            System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] Unique colors in original: {uniqueColors}");
            
            // Generate initial optimized preview
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
            _useLab = RbLab.IsChecked == true;
            UpdateOptimizedPreview();
        }

        private void UpdateOptimizedPreview()
        {
            if (_originalPixels == null || _originalPixels.Count == 0)
                return;
            
            System.Diagnostics.Debug.WriteLine($"[UpdateOptimizedPreview] Starting optimization...");
            System.Diagnostics.Debug.WriteLine($"[UpdateOptimizedPreview] Original pixels: {_originalPixels.Count}");
            System.Diagnostics.Debug.WriteLine($"[UpdateOptimizedPreview] Target color count: {_targetColorCount}");
            System.Diagnostics.Debug.WriteLine($"[UpdateOptimizedPreview] Current diversity: {_currentDiversity}");
            
            // Optimize palette with current diversity
            var rgbPalette = PaletteOptimizer.OptimizePalette(_originalPixels, _targetColorCount, _currentDiversity);
            
            System.Diagnostics.Debug.WriteLine($"[UpdateOptimizedPreview] RGB palette after optimization: {rgbPalette.Count} colors");
            
            // Map to target hardware palette (SMS, NES, etc)
            OptimizedPalette = MapToTargetHardware(rgbPalette, _targetConsole, _useLab);
            
            System.Diagnostics.Debug.WriteLine($"[UpdateOptimizedPreview] Optimized to {OptimizedPalette.Count} colors for {_targetConsole}");
            
            // Apply optimized palette to ORIGINAL image (not to already-optimized)
            var optimizedBitmap = ApplyPalette(_originalBitmap, OptimizedPalette, _useLab);
            ImgOptimized.Source = optimizedBitmap;
            OptimizedBitmap = optimizedBitmap; // Store for export
            
            UpdatePalettePreview();
        }
        
        private List<(byte R, byte G, byte B)> MapToTargetHardware(List<(byte R, byte G, byte B)> rgbPalette, string targetConsole, bool useLab)
        {
            // Get target hardware palette
            var hardwarePalette = GetTargetHardwarePalette(targetConsole);
            if (hardwarePalette == null || hardwarePalette.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] No hardware palette for {targetConsole}, using RGB as-is");
                return rgbPalette;
            }
            
            System.Diagnostics.Debug.WriteLine($"[PaletteOptimizationWindow] Mapping {rgbPalette.Count} colors to {targetConsole} hardware palette ({hardwarePalette.Count} colors)");
            
            // Map each RGB color to closest hardware color
            var mappedPalette = new List<(byte R, byte G, byte B)>();
            foreach (var color in rgbPalette)
            {
                var closest = FindClosestColor(color, hardwarePalette, useLab);
                mappedPalette.Add(closest);
            }
            
            return mappedPalette;
        }
        
        private List<(byte R, byte G, byte B)>? GetTargetHardwarePalette(string targetConsole)
        {
            return targetConsole switch
            {
                "sms" => GenerateSmsMasterPalette(),
                "gg" => GenerateGameGearMasterPalette(),
                _ => null
            };
        }
        
        private List<(byte R, byte G, byte B)> GenerateSmsMasterPalette()
        {
            // SMS: 6-bit RGB (2 bits per channel) = 64 colors
            var palette = new List<(byte R, byte G, byte B)>();
            for (int r = 0; r < 4; r++)
            {
                for (int g = 0; g < 4; g++)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        palette.Add(((byte)(r * 85), (byte)(g * 85), (byte)(b * 85)));
                    }
                }
            }
            return palette;
        }
        
        private List<(byte R, byte G, byte B)> GenerateGameGearMasterPalette()
        {
            // Game Gear: 12-bit RGB (4 bits per channel) = 4096 colors
            var palette = new List<(byte R, byte G, byte B)>();
            for (int r = 0; r < 16; r++)
            {
                for (int g = 0; g < 16; g++)
                {
                    for (int b = 0; b < 16; b++)
                    {
                        palette.Add(((byte)(r * 17), (byte)(g * 17), (byte)(b * 17)));
                    }
                }
            }
            return palette;
        }

        private void UpdatePalettePreview()
        {
            if (OptimizedPalette == null)
                return;
            
            var brushes = OptimizedPalette
                .Distinct()
                .OrderBy(c => RgbToHue(c.R, c.G, c.B))
                .ThenBy(c => RgbToLightness(c.R, c.G, c.B))
                .ThenBy(c => RgbToSaturation(c.R, c.G, c.B))
                .Select(c => new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)))
                .ToList();
            
            PalettePreview.ItemsSource = brushes;
        }
        
        private static double RgbToHue(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;
            
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;
            
            if (delta == 0) return 0;
            
            double hue = 0;
            if (max == rd)
                hue = ((gd - bd) / delta) % 6;
            else if (max == gd)
                hue = (bd - rd) / delta + 2;
            else
                hue = (rd - gd) / delta + 4;
            
            hue *= 60;
            if (hue < 0) hue += 360;
            
            return hue;
        }
        
        private static double RgbToLightness(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;
            
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            
            return (max + min) / 2.0;
        }
        
        private static double RgbToSaturation(byte r, byte g, byte b)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;
            
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;
            
            if (delta == 0) return 0;
            
            double lightness = (max + min) / 2.0;
            return delta / (1 - Math.Abs(2 * lightness - 1));
        }

        private List<(byte R, byte G, byte B)> ExtractPixels(BitmapSource bitmap)
        {
            if (bitmap == null)
                return new List<(byte R, byte G, byte B)>();
            
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            
            // Convert to Bgra32 format if needed
            BitmapSource convertedBitmap = bitmap;
            if (bitmap.Format != PixelFormats.Bgra32)
            {
                convertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            }
            
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            
            convertedBitmap.CopyPixels(pixels, stride, 0);
            
            var result = new List<(byte R, byte G, byte B)>();
            for (int i = 0; i < pixels.Length; i += 4)
            {
                result.Add((pixels[i + 2], pixels[i + 1], pixels[i]));
            }
            
            return result;
        }

        private BitmapSource? ApplyPalette(BitmapSource? original, List<(byte R, byte G, byte B)> palette, bool useLab)
        {
            if (original == null || palette == null || palette.Count == 0)
                return original;
            
            int width = original.PixelWidth;
            int height = original.PixelHeight;
            
            // Convert to Bgra32 format if needed
            BitmapSource convertedBitmap = original;
            if (original.Format != PixelFormats.Bgra32)
            {
                convertedBitmap = new FormatConvertedBitmap(original, PixelFormats.Bgra32, null, 0);
            }
            
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            
            convertedBitmap.CopyPixels(pixels, stride, 0);
            
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte r = pixels[i + 2];
                byte g = pixels[i + 1];
                byte b = pixels[i];
                
                var closest = FindClosestColor((r, g, b), palette, useLab);
                
                pixels[i + 2] = closest.R;
                pixels[i + 1] = closest.G;
                pixels[i] = closest.B;
            }
            
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        }

        private (byte R, byte G, byte B) FindClosestColor((byte R, byte G, byte B) color, 
            List<(byte R, byte G, byte B)> palette, bool useLab)
        {
            return useLab 
                ? Retruxel.Lib.ImageProcessing.ColorMatching.FindNearestLab(color, palette)
                : Retruxel.Lib.ImageProcessing.ColorMatching.FindNearestRgb(color, palette);
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
    }
}
