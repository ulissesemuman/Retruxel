using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Retruxel.Tool.AssetProcessor;

/// <summary>
/// Tool for processing assets with generation parameters.
/// Loads source images and applies color reduction, palette optimization, etc.
/// </summary>
public class AssetProcessorTool : ITool
{
    public string ToolId => "asset_processor";
    public string DisplayName => "Asset Processor";
    public string Description => "Processes assets with generation parameters";
    public string Category => "Asset Processing";
    public object? Icon => null;
    public string? Shortcut => null;
    public string? TargetId => null;
    public bool IsStandalone => false;
    public bool RequiresProject => true;

    private readonly IndexedPngService _indexedPngService = new();

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        if (!input.ContainsKey("asset") || !input.ContainsKey("projectPath"))
            throw new ArgumentException("Missing required parameters: asset, projectPath");

        var asset = (AssetEntry)input["asset"];
        var projectPath = (string)input["projectPath"];

        var processedData = ProcessAsset(asset, projectPath);
        
        return new Dictionary<string, object>
        {
            ["indexedData"] = processedData!,
            ["width"] = processedData!.Width,
            ["height"] = processedData.Height,
            ["colorCount"] = processedData.Colors.Count
        };
    }

    /// <summary>
    /// Loads and processes an asset with its generation parameters.
    /// Returns the processed IndexedPngData ready for use.
    /// </summary>
    public IndexedPngData? ProcessAsset(AssetEntry asset, string projectPath)
    {
        if (asset.GenerationParams == null)
        {
            // Legacy asset without generation params - load directly
            var legacyPath = Path.Combine(projectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(legacyPath))
                return null;

            return _indexedPngService.Read(legacyPath);
        }

        // Load source image
        var sourcePath = Path.Combine(projectPath, asset.SourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
            return null;

        using var sourceBitmap = SKBitmap.Decode(sourcePath);
        if (sourceBitmap == null)
            return null;

        // Apply generation parameters
        return ApplyGenerationParams(sourceBitmap, asset.GenerationParams);
    }

    private IndexedPngData ApplyGenerationParams(SKBitmap sourceBitmap, AssetGenerationParams genParams)
    {
        var pixels = ExtractPixelsFromBitmap(sourceBitmap);
        var palette = OptimizePalette(pixels, genParams.ColorCount, genParams.DiversityWeight);

        if (genParams.ColorOrder != null && genParams.ColorOrder.Length > 0)
        {
            palette = ReorderPalette(palette, genParams.ColorOrder);
        }

        var useLab = genParams.ColorSpace == "LAB";
        var indices = MapPixelsToIndices(sourceBitmap, palette, useLab);
        var hexColors = palette.Select(c => $"#{c.R:X2}{c.G:X2}{c.B:X2}").ToList();

        return new IndexedPngData
        {
            Width = sourceBitmap.Width,
            Height = sourceBitmap.Height,
            Indices = indices,
            Colors = hexColors
        };
    }

    private List<(byte R, byte G, byte B)> ExtractPixelsFromBitmap(SKBitmap bitmap)
    {
        var pixels = new List<(byte R, byte G, byte B)>();
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > 0)
                {
                    pixels.Add((pixel.Red, pixel.Green, pixel.Blue));
                }
            }
        }
        return pixels;
    }

    private List<(byte R, byte G, byte B)> OptimizePalette(
        List<(byte R, byte G, byte B)> pixels,
        int targetColorCount,
        double diversity)
    {
        var colorSet = new HashSet<uint>();
        foreach (var (r, g, b) in pixels)
        {
            uint color = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
            colorSet.Add(color);
        }

        if (colorSet.Count <= targetColorCount)
        {
            return colorSet.Select(c => (
                R: (byte)((c >> 16) & 0xFF),
                G: (byte)((c >> 8) & 0xFF),
                B: (byte)(c & 0xFF)
            )).ToList();
        }

        var optimized = HierarchicalClustering(colorSet, targetColorCount, 20, diversity);

        return optimized.Select(c => (
            R: (byte)((c >> 16) & 0xFF),
            G: (byte)((c >> 8) & 0xFF),
            B: (byte)(c & 0xFF)
        )).ToList();
    }

    private uint[] HierarchicalClustering(HashSet<uint> colors, int targetSlots, int maxIterations, double diversity)
    {
        var colorList = colors.ToList();
        var centroids = DiversityWeightedInit(colorList, targetSlots, diversity);

        for (int iter = 0; iter < maxIterations; iter++)
        {
            var clusters = new List<uint>[targetSlots];
            for (int i = 0; i < targetSlots; i++)
                clusters[i] = new List<uint>();

            foreach (var color in colorList)
            {
                int nearest = FindNearestCentroid(color, centroids);
                clusters[nearest].Add(color);
            }

            bool changed = false;
            for (int i = 0; i < targetSlots; i++)
            {
                if (clusters[i].Count > 0)
                {
                    var newCentroid = CalculateCentroid(clusters[i]);
                    if (newCentroid != centroids[i])
                    {
                        centroids[i] = newCentroid;
                        changed = true;
                    }
                }
            }

            if (!changed)
                break;
        }

        return centroids;
    }

    private uint[] DiversityWeightedInit(List<uint> colors, int k, double diversity)
    {
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

        var uniqueColors = colorCounts.Keys.ToList();
        var firstColor = uniqueColors.OrderByDescending(c => colorCounts[c] * bias).First();
        centroids.Add(firstColor);

        for (int i = 1; i < k; i++)
        {
            var weights = new double[uniqueColors.Count];
            double totalWeight = 0;

            for (int j = 0; j < uniqueColors.Count; j++)
            {
                var color = uniqueColors[j];

                if (centroids.Contains(color))
                {
                    weights[j] = 0;
                    continue;
                }

                double minDist = double.MaxValue;
                foreach (var centroid in centroids)
                {
                    double dist = ColorDistance(color, centroid);
                    if (dist < minDist)
                        minDist = dist;
                }

                double freqWeight = colorCounts[color] * bias;
                double divWeight = minDist * minDist * decay;
                weights[j] = divWeight + freqWeight * 0.1;
                totalWeight += weights[j];
            }

            double threshold = random.NextDouble() * totalWeight;
            double sum = 0;

            for (int j = 0; j < uniqueColors.Count; j++)
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
                for (int j = 0; j < uniqueColors.Count; j++)
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

    private int FindNearestCentroid(uint color, uint[] centroids)
    {
        int nearest = 0;
        double minDist = ColorDistance(color, centroids[0]);

        for (int i = 1; i < centroids.Length; i++)
        {
            double dist = ColorDistance(color, centroids[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

    private double ColorDistance(uint c1, uint c2)
    {
        int r1 = (int)((c1 >> 16) & 0xFF);
        int g1 = (int)((c1 >> 8) & 0xFF);
        int b1 = (int)(c1 & 0xFF);

        int r2 = (int)((c2 >> 16) & 0xFF);
        int g2 = (int)((c2 >> 8) & 0xFF);
        int b2 = (int)(c2 & 0xFF);

        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;

        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private uint CalculateCentroid(List<uint> colors)
    {
        long r = 0, g = 0, b = 0;

        foreach (var color in colors)
        {
            r += (color >> 16) & 0xFF;
            g += (color >> 8) & 0xFF;
            b += color & 0xFF;
        }

        int count = colors.Count;
        byte avgR = (byte)(r / count);
        byte avgG = (byte)(g / count);
        byte avgB = (byte)(b / count);

        return 0xFF000000u | ((uint)avgR << 16) | ((uint)avgG << 8) | avgB;
    }

    private List<(byte R, byte G, byte B)> ReorderPalette(
        List<(byte R, byte G, byte B)> palette,
        int[] order)
    {
        var reordered = new List<(byte R, byte G, byte B)>();
        foreach (var index in order)
        {
            if (index >= 0 && index < palette.Count)
                reordered.Add(palette[index]);
        }
        return reordered;
    }

    private byte[] MapPixelsToIndices(SKBitmap bitmap, List<(byte R, byte G, byte B)> palette, bool useLab)
    {
        var indices = new byte[bitmap.Width * bitmap.Height];
        int idx = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha == 0)
                {
                    indices[idx++] = 0;
                }
                else
                {
                    var color = (pixel.Red, pixel.Green, pixel.Blue);
                    indices[idx++] = (byte)FindNearestColorIndex(color, palette, useLab);
                }
            }
        }

        return indices;
    }

    private int FindNearestColorIndex((byte R, byte G, byte B) color, List<(byte R, byte G, byte B)> palette, bool useLab)
    {
        if (useLab)
        {
            var lab = RgbToLab(color.R, color.G, color.B);
            int closestIndex = 0;
            double minDistance = double.MaxValue;

            for (int i = 0; i < palette.Count; i++)
            {
                var p = palette[i];
                var pLab = RgbToLab(p.R, p.G, p.B);
                var dL = lab.L - pLab.L;
                var dA = lab.A - pLab.A;
                var dB = lab.B - pLab.B;
                var distance = dL * dL + dA * dA + dB * dB;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }
        else
        {
            int closestIndex = 0;
            double minDistance = double.MaxValue;

            uint colorUint = 0xFF000000u | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;

            for (int i = 0; i < palette.Count; i++)
            {
                var p = palette[i];
                uint paletteUint = 0xFF000000u | ((uint)p.R << 16) | ((uint)p.G << 8) | p.B;
                var distance = ColorDistance(colorUint, paletteUint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }

            return closestIndex;
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
}
