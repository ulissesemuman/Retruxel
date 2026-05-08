using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using static Retruxel.Lib.ImageProcessing.ColorMatching;

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
        var palette = ColorMatching.OptimizePalette(pixels, genParams.ColorCount, genParams.DiversityWeight);

        if (genParams.ColorOrder != null && genParams.ColorOrder.Length > 0)
        {
            palette = ReorderPalette(palette, genParams.ColorOrder);
        }

        DistanceMode distanceMode;

        switch (genParams.ColorSpace)
        {
            case "RGB":
                distanceMode = DistanceMode.RGB;
                break;
            case "Perceptual":
                distanceMode = DistanceMode.Perceptual;
                break;
            case "LAB":
                distanceMode = DistanceMode.LAB;
                break;
            default:
                distanceMode = DistanceMode.RGB;
                break;
        }

        var indices = MapPixelsToIndices(sourceBitmap, palette, distanceMode);
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

    private List<HardwareColor> ReorderPalette(
        List<HardwareColor> palette,
        int[] order)
    {
        var reordered = new List<HardwareColor>();
        foreach (var index in order)
        {
            if (index >= 0 && index < palette.Count)
                reordered.Add(palette[index]);
        }
        return reordered;
    }

    private byte[] MapPixelsToIndices(SKBitmap bitmap, List<HardwareColor> palette, DistanceMode distanceMode = DistanceMode.RGB)
    {
        var indices = new byte[bitmap.Width * bitmap.Height];
        int idx = 0;

        var fastPalette = ColorMatching.PrepareFastPalette(palette, distanceMode);

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
                    indices[idx++] = ColorMatching.FindNearestColorIndex(color, fastPalette, distanceMode);
                }
            }
        }

        return indices;
    }
}
