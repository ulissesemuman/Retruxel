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
        private readonly bool _useLab;
        private readonly string _targetConsole;
        private double _currentDiversity = 1.25;
        
        // Store original pixels to avoid re-extracting
        private readonly List<(byte R, byte G, byte B)> _originalPixels;
        
        public double SelectedDiversity => _currentDiversity;
        public List<(byte R, byte G, byte B)> OptimizedPalette { get; private set; }
        public BitmapSource OptimizedBitmap { get; private set; }

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
        
        private List<(byte R, byte G, byte B)> GetTargetHardwarePalette(string targetConsole)
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
            
            var brushes = OptimizedPalette.Select(c => 
                new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B))).ToList();
            
            PalettePreview.ItemsSource = brushes;
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

        private BitmapSource ApplyPalette(BitmapSource original, List<(byte R, byte G, byte B)> palette, bool useLab)
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
            if (useLab)
            {
                var lab = RgbToLab(color.R, color.G, color.B);
                double minDistance = double.MaxValue;
                var closest = palette[0];

                foreach (var p in palette)
                {
                    var pLab = RgbToLab(p.R, p.G, p.B);
                    double distance = Math.Sqrt(
                        Math.Pow(lab.L - pLab.L, 2) +
                        Math.Pow(lab.A - pLab.A, 2) +
                        Math.Pow(lab.B - pLab.B, 2));

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest = p;
                    }
                }

                return closest;
            }
            else
            {
                double minDistance = double.MaxValue;
                var closest = palette[0];

                foreach (var p in palette)
                {
                    double distance = Math.Sqrt(
                        Math.Pow(color.R - p.R, 2) +
                        Math.Pow(color.G - p.G, 2) +
                        Math.Pow(color.B - p.B, 2));

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closest = p;
                    }
                }

                return closest;
            }
        }

        private (double L, double A, double B) RgbToLab(byte r, byte g, byte b)
        {
            double rLinear = RgbToLinear(r / 255.0);
            double gLinear = RgbToLinear(g / 255.0);
            double bLinear = RgbToLinear(b / 255.0);

            double x = rLinear * 0.4124564 + gLinear * 0.3575761 + bLinear * 0.1804375;
            double y = rLinear * 0.2126729 + gLinear * 0.7151522 + bLinear * 0.0721750;
            double z = rLinear * 0.0193339 + gLinear * 0.1191920 + bLinear * 0.9503041;

            x /= 0.95047;
            y /= 1.00000;
            z /= 1.08883;

            double fx = LabF(x);
            double fy = LabF(y);
            double fz = LabF(z);

            double L = 116.0 * fy - 16.0;
            double A = 500.0 * (fx - fy);
            double B = 200.0 * (fy - fz);

            return (L, A, B);
        }

        private double RgbToLinear(double c)
        {
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        private double LabF(double t)
        {
            const double delta = 6.0 / 29.0;
            return t > delta * delta * delta ? Math.Pow(t, 1.0 / 3.0) : t / (3.0 * delta * delta) + 4.0 / 29.0;
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
