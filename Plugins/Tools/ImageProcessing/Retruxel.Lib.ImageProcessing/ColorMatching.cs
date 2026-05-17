using Retruxel.Core.Models;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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

    public struct LabColor
    {
        public double L, A, B;
    }

    public struct RgbColor
    {
        public byte R, G, B;
    }

    public struct FastColor
    {
        public RgbColor rgbColor;
        public LabColor labColor;
    }

    /// <summary>
    /// Converts a list of ideal colors to the nearest available colors in a hardware palette, using the 
    /// specified distance mode.
    /// </summary>
    /// <remarks>The order of the returned colors corresponds to the order of the colors provided in 'idealColors'. 
    /// Performance may vary depending on the palette size and the selected distance mode.</remarks>
    /// <param name="idealColors">The list of ideal colors to be quantized to the hardware palette.</param>
    /// <param name="hardwarePalette">The available hardware palette, represented as a read-only list of colors.</param>
    /// <param name="distanceMode">The color distance calculation mode to be used for finding the nearest color. 
    /// The default value is DistanceMode.RGB.</param>
    /// <returns>A list of colors from the hardware palette, where each color corresponds to the nearest match 
    /// for the respective provided ideal color.</returns>
    public static List<HardwareColor> QuantizePalette(
        List<HardwareColor> idealColors,
        IReadOnlyList<HardwareColor> hardwarePalette,
        DistanceMode distanceMode = DistanceMode.RGB)
    {
        var fastPalette = PrepareFastPalette(hardwarePalette, distanceMode);

        return idealColors.Select(c =>
        {
            var index = FindNearestColorIndex((c.R, c.G, c.B), fastPalette, distanceMode);
            return hardwarePalette[index];
        }).ToList();
    }

    public static byte FindNearestColorIndex(
        (byte R, byte G, byte B) color,
        FastColor[] fastPalette,
        DistanceMode distanceMode = DistanceMode.RGB)
    {
        double tL = 0, tA = 0, tB = 0;
        if (distanceMode == DistanceMode.LAB) (tL, tA, tB) = RgbToLab(color.R, color.G, color.B);

        byte bestIndex = 0;
        double bestDistance = double.MaxValue;

        byte targetR = color.R;
        byte targetG = color.G;
        byte targetB = color.B;

        for (byte i = 0; i < fastPalette.Length; i++)
        {
            var p = fastPalette[i];

            double distance;

            if (distanceMode == DistanceMode.LAB)
            {
                double dL = tL - fastPalette[i].labColor.L;
                double dA = tA - fastPalette[i].labColor.A;
                double dB = tB - fastPalette[i].labColor.B;
                distance = dL * dL + dA * dA + dB * dB;
            }
            else
            {
                distance = ColorDistance(targetR, targetG, targetB, fastPalette[i].rgbColor.R, fastPalette[i].rgbColor.G, fastPalette[i].rgbColor.B, distanceMode);
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = (byte)i;

                if (distance == 0) break;
            }
        }
        return bestIndex;
    }

    public static FastColor[] PrepareFastPalette(IReadOnlyList<HardwareColor> palette, DistanceMode distanceMode = DistanceMode.RGB)
    {
        int count = palette.Count;
        var fastPalette = new FastColor[count];

        for (int i = 0; i < count; i++)
        {
            var p = palette[i];

            var lab = (distanceMode == DistanceMode.LAB)
                      ? RgbToLab(p.R, p.G, p.B)
                      : (L: 0.0, A: 0.0, B: 0.0);

            fastPalette[i] = new FastColor
            {
                rgbColor = new RgbColor
                {
                    R = p.R,
                    G = p.G,
                    B = p.B
                },
                labColor = new LabColor
                {
                    L = lab.L,
                    A = lab.A,
                    B = lab.B
                }
            };
        }

        return fastPalette;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ColorDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2, DistanceMode mode = DistanceMode.RGB)
    {
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;

        return mode switch
        {
            DistanceMode.Perceptual => (dr * dr * 0.299) + (dg * dg * 0.587) + (db * db * 0.114),
            _ => (dr * dr + dg * dg + db * db) // RGB
        };
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

    /// <summary>
    /// Public method for palette optimization with custom diversity.
    /// Used by PaletteOptimizationWindow for real-time preview.
    /// </summary>
    public static List<HardwareColor> OptimizePalette(
        List<(byte R, byte G, byte B)> pixels,
        int targetColorCount,
        double diversity = 1.25)
    {
        var colorSet = new HashSet<uint>();
        foreach (var (r, g, b) in pixels)
        {
            uint color = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
            colorSet.Add(color);
        }

        if (colorSet.Count <= targetColorCount)
        {
            return colorSet.Select(c => new HardwareColor(
                (byte)((c >> 16) & 0xFF),
                (byte)((c >> 8) & 0xFF),
                (byte)(c & 0xFF)
            )).ToList();
        }

        var optimized = HierarchicalClustering(colorSet, targetColorCount, 20, diversity);

        return optimized.Select(c => new HardwareColor(
            (byte)((c >> 16) & 0xFF),
            (byte)((c >> 8) & 0xFF),
            (byte)(c & 0xFF)
        )).ToList();
    }

    private static uint[] HierarchicalClustering(HashSet<uint> colors, int targetSlots, int maxIterations, double diversity)
    {
        var colorList = colors.ToList();
        var centroids = DiversityWeightedInit(colorList, targetSlots, diversity);
        int[] pixelAssignment = new int[colorList.Count];

        // Arrays para acumular R, G, B e contagem de cada cluster sem alocar novas listas
        long[] sumR = new long[targetSlots];
        long[] sumG = new long[targetSlots];
        long[] sumB = new long[targetSlots];
        int[] clusterCounts = new int[targetSlots];

        for (int iter = 0; iter < maxIterations; iter++)
        {
            Array.Clear(sumR, 0, targetSlots);
            Array.Clear(sumG, 0, targetSlots);
            Array.Clear(sumB, 0, targetSlots);
            Array.Clear(clusterCounts, 0, targetSlots);

            bool changed = false;

            // Fase de Atribui��o
            for (int i = 0; i < colorList.Count; i++)
            {
                uint color = colorList[i];
                int nearest = FindNearestCentroid(color, centroids);

                if (pixelAssignment[i] != nearest)
                {
                    pixelAssignment[i] = nearest;
                    changed = true;
                }

                // Acumula para o novo centroide
                sumR[nearest] += (color >> 16) & 0xFF;
                sumG[nearest] += (color >> 8) & 0xFF;
                sumB[nearest] += color & 0xFF;
                clusterCounts[nearest]++;
            }

            if (!changed && iter > 0) break;

            // Fase de Atualiza��o
            for (int i = 0; i < targetSlots; i++)
            {
                if (clusterCounts[i] > 0)
                {
                    byte r = (byte)(sumR[i] / clusterCounts[i]);
                    byte g = (byte)(sumG[i] / clusterCounts[i]);
                    byte b = (byte)(sumB[i] / clusterCounts[i]);
                    centroids[i] = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
                }
            }
        }
        return centroids;
    }

    private static uint[] DiversityWeightedInit(List<uint> colors, int k, double diversity)
    {
        var colorCountsDict = new Dictionary<uint, int>();
        foreach (var color in colors)
        {
            colorCountsDict.TryGetValue(color, out int count);
            colorCountsDict[color] = count + 1;
        }

        uint[] uniqueColors = colorCountsDict.Keys.ToArray();
        int[] counts = colorCountsDict.Values.ToArray();
        int uniqueCount = uniqueColors.Length;

        int seed = (int)(diversity * 10000);
        var random = new Random(seed);
        var centroids = new List<uint>();

        double bias = diversity * 0.5 + 0.5;
        double decay = diversity * 0.25 + 0.65;

        var colorCounts = new Dictionary<uint, int>();
        foreach (var color in colors)
        {
            if (!colorCounts.ContainsKey(color))
                colorCounts[color] = 0;
            colorCounts[color]++;
        }

        var firstColor = uniqueColors.OrderByDescending(c => colorCounts[c] * bias).First();
        centroids.Add(firstColor);

        for (int i = 1; i < k; i++)
        {
            var weights = new double[uniqueCount];
            double totalWeight = 0;

            for (int j = 0; j < uniqueCount; j++)
            {
                uint color = uniqueColors[j];

                double minDist = double.MaxValue;
                foreach (var centroid in centroids)
                {
                    byte r1 = (byte)((color >> 16) & 0xFF);
                    byte g1 = (byte)((color >> 8) & 0xFF);
                    byte b1 = (byte)(color & 0xFF);
                    byte r2 = (byte)((centroid >> 16) & 0xFF);
                    byte g2 = (byte)((centroid >> 8) & 0xFF);
                    byte b2 = (byte)(centroid & 0xFF);
                    double dist = ColorMatching.ColorDistance(r1, g1, b1, r2, g2, b2);
                    if (dist < minDist)
                        minDist = dist;
                }

                if (minDist == 0) // J� � um centroide
                {
                    weights[j] = 0;
                    continue;
                }

                double freqWeight = colorCounts[color] * bias;
                double divWeight = minDist * minDist * decay;
                weights[j] = divWeight + freqWeight * 0.1;
                totalWeight += weights[j];
            }

            double threshold = random.NextDouble() * totalWeight;
            double sum = 0;

            for (int j = 0; j < uniqueColors.Length; j++)
            {
                sum += weights[j];
                if (sum >= threshold)
                {
                    centroids.Add(uniqueColors[j]);
                    break;
                }
            }

            if (centroids.Count == i)
            {
                int maxIdx = 0;
                double maxWeight = 0;
                for (int j = 0; j < uniqueColors.Length; j++)
                {
                    if (weights[j] > maxWeight)
                    {
                        maxWeight = weights[j];
                        maxIdx = j;
                    }
                }
                centroids.Add(uniqueColors[maxIdx]);
            }
        }

        return centroids.ToArray();
    }

    private static int FindNearestCentroid(uint color, uint[] centroids)
    {
        int nearest = 0;
        byte r = (byte)((color >> 16) & 0xFF);
        byte g = (byte)((color >> 8) & 0xFF);
        byte b = (byte)(color & 0xFF);
        byte r0 = (byte)((centroids[0] >> 16) & 0xFF);
        byte g0 = (byte)((centroids[0] >> 8) & 0xFF);
        byte b0 = (byte)(centroids[0] & 0xFF);
        double minDist = ColorMatching.ColorDistance(r, g, b, r0, g0, b0);

        for (int i = 1; i < centroids.Length; i++)
        {
            byte ri = (byte)((centroids[i] >> 16) & 0xFF);
            byte gi = (byte)((centroids[i] >> 8) & 0xFF);
            byte bi = (byte)(centroids[i] & 0xFF);
            double dist = ColorMatching.ColorDistance(r, g, b, ri, gi, bi);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

}
