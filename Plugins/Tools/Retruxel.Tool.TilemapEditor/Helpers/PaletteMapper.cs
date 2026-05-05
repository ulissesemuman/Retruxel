using Retruxel.Core.Interfaces;
using System;
using System.Linq;

namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Maps RGB palettes to target hardware colors using perceptual color matching.
/// </summary>
public static class PaletteMapper
{
    /// <summary>
    /// Maps RGB palette to target hardware colors.
    /// Finds the closest hardware color for each RGB color.
    /// </summary>
    public static uint[] MapToTargetHardware(uint[] rgbPalette, ITarget target, bool useLab = false)
    {
        var hardwareColors = target.GetHardwarePalette();

        if (hardwareColors == null || hardwareColors.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[PaletteMapper] {target.TargetId} has no hardware palette, using RGB directly");
            return rgbPalette;
        }

        var hardwarePalette = hardwareColors
            .Select(c => (uint)((0xFF << 24) | (c.R << 16) | (c.G << 8) | c.B))
            .ToArray();

        var mappedPalette = new uint[rgbPalette.Length];

        for (int i = 0; i < rgbPalette.Length; i++)
        {
            mappedPalette[i] = FindClosestHardwareColor(rgbPalette[i], hardwarePalette, useLab);
        }

        return mappedPalette;
    }

    /// <summary>
    /// Finds the closest hardware color to a given RGB color.
    /// Uses LAB color space for perceptually accurate matching, or RGB Euclidean distance.
    /// </summary>
    private static uint FindClosestHardwareColor(uint rgbColor, uint[] hardwarePalette, bool useLab)
    {
        byte r1 = (byte)((rgbColor >> 16) & 0xFF);
        byte g1 = (byte)((rgbColor >> 8) & 0xFF);
        byte b1 = (byte)(rgbColor & 0xFF);

        int closestIndex = 0;
        double minDistance = double.MaxValue;

        if (useLab)
        {
            var lab1 = RgbToLab(r1, g1, b1);

            for (int i = 0; i < hardwarePalette.Length; i++)
            {
                uint hwColor = hardwarePalette[i];
                byte r2 = (byte)((hwColor >> 16) & 0xFF);
                byte g2 = (byte)((hwColor >> 8) & 0xFF);
                byte b2 = (byte)(hwColor & 0xFF);

                var lab2 = RgbToLab(r2, g2, b2);

                // Delta E (CIE76) - perceptual distance in LAB space
                double dL = lab1.L - lab2.L;
                double dA = lab1.A - lab2.A;
                double dB = lab1.B - lab2.B;
                double distance = Math.Sqrt(dL * dL + dA * dA + dB * dB);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
        }
        else
        {
            // RGB Euclidean distance
            for (int i = 0; i < hardwarePalette.Length; i++)
            {
                uint hwColor = hardwarePalette[i];
                byte r2 = (byte)((hwColor >> 16) & 0xFF);
                byte g2 = (byte)((hwColor >> 8) & 0xFF);
                byte b2 = (byte)(hwColor & 0xFF);

                int dr = r1 - r2;
                int dg = g1 - g2;
                int db = b1 - b2;
                double distance = Math.Sqrt(dr * dr + dg * dg + db * db);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
        }

        return hardwarePalette[closestIndex];
    }

    /// <summary>
    /// Converts RGB to LAB color space for perceptually accurate color matching.
    /// </summary>
    private static (double L, double A, double B) RgbToLab(byte r, byte g, byte b)
    {
        // Step 1: RGB to XYZ
        double rLinear = RgbToLinear(r / 255.0);
        double gLinear = RgbToLinear(g / 255.0);
        double bLinear = RgbToLinear(b / 255.0);

        // D65 illuminant matrix
        double x = rLinear * 0.4124564 + gLinear * 0.3575761 + bLinear * 0.1804375;
        double y = rLinear * 0.2126729 + gLinear * 0.7151522 + bLinear * 0.0721750;
        double z = rLinear * 0.0193339 + gLinear * 0.1191920 + bLinear * 0.9503041;

        // Step 2: XYZ to LAB (D65 reference white)
        const double xn = 0.95047;
        const double yn = 1.00000;
        const double zn = 1.08883;

        double fx = LabF(x / xn);
        double fy = LabF(y / yn);
        double fz = LabF(z / zn);

        double L = 116.0 * fy - 16.0;
        double A = 500.0 * (fx - fy);
        double B = 200.0 * (fy - fz);

        return (L, A, B);
    }

    /// <summary>
    /// Converts sRGB gamma-corrected value to linear RGB.
    /// </summary>
    private static double RgbToLinear(double c)
    {
        if (c <= 0.04045)
            return c / 12.92;
        else
            return Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    /// <summary>
    /// LAB color space conversion function.
    /// </summary>
    private static double LabF(double t)
    {
        const double delta = 6.0 / 29.0;
        const double delta3 = delta * delta * delta;

        if (t > delta3)
            return Math.Pow(t, 1.0 / 3.0);
        else
            return t / (3.0 * delta * delta) + 4.0 / 29.0;
    }
}
