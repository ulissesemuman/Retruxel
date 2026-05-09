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

        using var sourceBitmap = LoadNormalizedBitmap(sourcePath);
        if (sourceBitmap == null)
            return null;

        // Apply generation parameters
        return ApplyGenerationParams(sourceBitmap, asset.GenerationParams);
    }

    public static SKBitmap LoadNormalizedBitmap(string path)
    {
        using var codec = SKCodec.Create(path);
        if (codec == null) throw new Exception("Falha ao abrir imagem.");

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Premul);

        var bitmap = new SKBitmap(info);

        codec.GetPixels(bitmap.Info, bitmap.GetPixels());

        return bitmap;
    }

    private IndexedPngData ApplyGenerationParams(SKBitmap sourceBitmap, AssetGenerationParams genParams)
    {
        var palette = new List<HardwareColor>();
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

        var a = ColorMatching.ReduceColors(sourceBitmap, palette, distanceMode);

        if (genParams.ColorOrder != null && genParams.ColorOrder.Length > 0)
        {
            palette = ReorderPalette(palette, genParams.ColorOrder);
        }

        var indices = ColorMatching.ReduceColors(sourceBitmap, palette, distanceMode);
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
}
