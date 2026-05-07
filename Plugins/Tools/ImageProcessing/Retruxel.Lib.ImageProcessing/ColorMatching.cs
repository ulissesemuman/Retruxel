using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Lib.ImageProcessing;

/// <summary>
/// Provides color matching algorithms for finding nearest colors in palettes.
/// Supports both RGB (mathematical) and LAB (perceptual) color spaces.
/// </summary>
public static class ColorMatching
{
    public enum DistanceMode
    {
        RGB,
        Perceptual,
        LAB
    }

    private struct LabColor
    {
        public double L, A, B;
    }

    /// <summary>
    /// Finds the nearest color in a palette using Euclidean distance in RGB space.
    /// Uses squared distance for performance (avoids Math.Sqrt).
    /// </summary>
    /// <param name="color">Source color to match.</param>
    /// <param name="palette">Target palette to search.</param>
    /// <returns>Closest color from the palette.</returns>
    public static (byte R, byte G, byte B) FindNearestRgb1(
        (byte R, byte G, byte B) color,
        IReadOnlyList<(byte R, byte G, byte B)> palette)
    {
        var bestColor = palette[0];
        var bestDistance = double.MaxValue;

        foreach (var p in palette)
        {
            var dr = (double)(color.R - p.R);
            var dg = (double)(color.G - p.G);
            var db = (double)(color.B - p.B);
            var distance = dr * dr + dg * dg + db * db;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestColor = p;
                if (distance == 0) break;
            }
        }
        return bestColor;
    }

    public static byte FindNearestColorIndex(
        (byte R, byte G, byte B) color,
        IReadOnlyList<(byte R, byte G, byte B)> palette,
        DistanceMode distanceMode = DistanceMode.RGB)
    {
        var fastPalette = palette.Select(p => new {
                                            R = (int)p.R,
                                            G = (int)p.G,
                                            B = (int)p.B
                                        }).ToArray();

        if (distanceMode == DistanceMode.LAB)
        {
            return (byte)FindNearestLab(color, palette).R; // Assuming palette is indexed and R component holds the index
        }

        byte bestIndex = 0;
        double bestDistance = double.MaxValue;

        int targetR = color.R;
        int targetG = color.G;
        int targetB = color.B;

        for (byte i = 0; i < fastPalette.Length; i++)
        {
            var p = fastPalette[i];

            int dr = targetR - p.R;
            int dg = targetG - p.G;
            int db = targetB - p.B;

            double distance;

            if (distanceMode == DistanceMode.Perceptual)
            {
                distance = (double)((dr * dr * 0.299) + (dg * dg * 0.587) + (db * db * 0.114));
            }
            else
            {
                distance = (double)(dr * dr + dg * dg + db * db);
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;

                if (distance == 0) break;
            }
        }
        return bestIndex;
    }

    public static byte FindNearestColorIndexLab(
        (byte R, byte G, byte B) color,
        IReadOnlyList<(byte R, byte G, byte B)> palette)
    {
        var fastPalette = palette.Select(p => {
            var lab = RgbToLab(p.R, p.G, p.B);
            return new LabColor {L = lab.L, A = lab.A, B = lab.B};
        }).ToArray();

        byte bestIndex = 0;
        double bestDistance = double.MaxValue;

        var targetLab = RgbToLab(color.R, color.G, color.B);

        for (byte i = 0; i < fastPalette.Length; i++)
        {
            var p = fastPalette[i];

            var dl = targetLab.L - p.L;
            var da = targetLab.A - p.A;
            var db = targetLab.B - p.B;

            double distance = (double)(dl * dl + da * da + db * db);
 
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;

                if (distance == 0) break;
            }
        }
        return bestIndex;
    }

    /// <summary>
    /// Finds the nearest color in a palette using perceptual distance in LAB color space.
    /// LAB provides better visual matching than RGB but is slower due to conversion overhead.
    /// </summary>
    /// <param name="color">Source color to match.</param>
    /// <param name="palette">Target palette to search.</param>
    /// <returns>Closest color from the palette.</returns>
    public static (byte R, byte G, byte B) FindNearestLab(
        (byte R, byte G, byte B) color,
        IReadOnlyList<(byte R, byte G, byte B)> palette)
    {
        var lab = RgbToLab(color.R, color.G, color.B);
        var bestColor = palette[0];
        var bestDistance = double.MaxValue;

        foreach (var p in palette)
        {
            var pLab = RgbToLab(p.R, p.G, p.B);
            var dL = lab.L - pLab.L;
            var dA = lab.A - pLab.A;
            var dB = lab.B - pLab.B;
            var distance = dL * dL + dA * dA + dB * dB;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestColor = p;
            }
        }
        return bestColor;
    }

    /// <summary>
    /// Converts RGB color to LAB color space.
    /// LAB is a perceptually uniform color space where Euclidean distance correlates with human perception.
    /// </summary>
    private static (double L, double A, double B) RgbToLab(byte r, byte g, byte b)
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

    private static double RgbToLinear(double c)
    {
        return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static double LabF(double t)
    {
        const double delta = 6.0 / 29.0;
        return t > delta * delta * delta ? Math.Pow(t, 1.0 / 3.0) : t / (3.0 * delta * delta) + 4.0 / 29.0;
    }
}
