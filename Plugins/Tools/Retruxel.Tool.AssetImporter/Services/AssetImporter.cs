

using SkiaSharp;
using System.IO;

namespace Retruxel.Tool.AssetImporter.Services;

/// <summary>
/// Imports PNG images as tile/sprite assets for a Retruxel project.
///
/// Import pipeline:
///   1. Load source PNG via SkiaSharp
///   2. Reduce colors to the hardware palette via nearest-color matching
///   3. Save reduced PNG to assets/tiles/ or assets/sprites/ (source of truth)
///   4. Return AssetEntry ready to be added to RetruxelProject.Assets
///
/// The binary 4bpp planar output is generated at build time by the CodeGen,
/// not stored on disk — the PNG is the canonical source.
/// </summary>
public static class AssetImporter
{
    private const int TileSize = 8;

    /// <summary>
    /// Imports a PNG file as a tile or sprite asset.
    /// Reduces colors to the hardware palette and saves to the project assets folder.
    /// </summary>
    /// <param name="sourcePngPath">Absolute path to the source PNG file.</param>
    /// <param name="projectPath">Absolute path to the project root folder.</param>
    /// <param name="assetType">Tiles or Sprites — determines target subfolder.</param>
    /// <param name="target">Active target — provides the hardware palette for color reduction.</param>
    /// <returns>AssetEntry ready to add to RetruxelProject.Assets.</returns>
    /// <exception cref="AssetImportException">Thrown when the import fails for a known reason.</exception>
    public static AssetEntry Import(
        string sourcePngPath,
        string projectPath,
        AssetType assetType,
        ITarget target)
    {
        // 1. Validate source file
        if (!File.Exists(sourcePngPath))
            throw new AssetImportException($"Source file not found: {sourcePngPath}");

        // 2. Load source image
        using var sourceStream = File.OpenRead(sourcePngPath);
        using var sourceBitmap = SKBitmap.Decode(sourceStream)
            ?? throw new AssetImportException($"Failed to decode image: {sourcePngPath}");

        ValidateDimensions(sourceBitmap, sourcePngPath);

        // 3. Get hardware palette
        var hardwarePalette = target.GetHardwarePalette();
        if (hardwarePalette.Count == 0)
            throw new AssetImportException($"Target '{target.TargetId}' returned an empty hardware palette.");

        // 4. Reduce colors to hardware palette
        using var reducedBitmap = ReduceColors(sourceBitmap, hardwarePalette);

        // 5. Determine output path
        var subfolder = assetType == AssetType.Tiles ? "tiles" : "sprites";
        var assetId = Path.GetFileNameWithoutExtension(sourcePngPath);
        var assetFileName = assetId + ".png";
        var assetFolder = Path.Combine(projectPath, "assets", subfolder);
        var assetFullPath = Path.Combine(assetFolder, assetFileName);
        var relativePath = Path.Combine("assets", subfolder, assetFileName)
                                .Replace('\\', '/');

        // 6. Ensure folder exists
        Directory.CreateDirectory(assetFolder);

        // 7. Save reduced PNG
        SavePng(reducedBitmap, assetFullPath);

        // 8. Calculate tile count
        var tileCount = (reducedBitmap.Width / TileSize) * (reducedBitmap.Height / TileSize);

        return new AssetEntry
        {
            Id = assetId,
            FileName = assetFileName,
            RelativePath = relativePath,
            Type = assetType,
            TileCount = tileCount,
            SourceWidth = reducedBitmap.Width,
            SourceHeight = reducedBitmap.Height,
            ImportedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Generates a preview of the color-reduced image without saving to disk.
    /// Used by the AssetImporterWindow to show the before/after comparison.
    /// </summary>
    public static SKBitmap PreviewReduction(string sourcePngPath, ITarget target)
    {
        using var stream = File.OpenRead(sourcePngPath);
        using var source = SKBitmap.Decode(stream)
            ?? throw new AssetImportException($"Failed to decode image: {sourcePngPath}");

        var palette = target.GetHardwarePalette();
        return ReduceColors(source, palette);
    }

    // ── Color Reduction ───────────────────────────────────────────────────────

    /// <summary>
    /// Reduces every pixel in the bitmap to the nearest color in the hardware palette.
    /// Uses Euclidean distance in RGB space for nearest-color matching.
    /// </summary>
    private static SKBitmap ReduceColors(
        SKBitmap source,
        IReadOnlyList<HardwareColor> palette)
    {
        var result = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);

                // Treat fully transparent pixels as transparent in output
                if (pixel.Alpha == 0)
                {
                    result.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                var nearest = FindNearestColor(pixel, palette);
                result.SetPixel(x, y, new SKColor(nearest.R, nearest.G, nearest.B, pixel.Alpha));
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the nearest hardware color to the given pixel using Euclidean distance in RGB space.
    /// </summary>
    private static HardwareColor FindNearestColor(SKColor pixel, IReadOnlyList<HardwareColor> palette)
    {
        var bestColor = palette[0];
        var bestDistance = double.MaxValue;

        foreach (var color in palette)
        {
            var dr = (double)(pixel.Red - color.R);
            var dg = (double)(pixel.Green - color.G);
            var db = (double)(pixel.Blue - color.B);

            var distance = dr * dr + dg * dg + db * db;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestColor = color;

                if (distance == 0) break; // Exact match — no need to continue
            }
        }

        return bestColor;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates that the image dimensions are multiples of 8 (tile size).
    /// </summary>
    private static void ValidateDimensions(SKBitmap bitmap, string path)
    {
        if (bitmap.Width % TileSize != 0)
            throw new AssetImportException(
                $"Image width ({bitmap.Width}px) must be a multiple of {TileSize}. File: {Path.GetFileName(path)}");

        if (bitmap.Height % TileSize != 0)
            throw new AssetImportException(
                $"Image height ({bitmap.Height}px) must be a multiple of {TileSize}. File: {Path.GetFileName(path)}");
    }

    // ── I/O ───────────────────────────────────────────────────────────────────

    private static void SavePng(SKBitmap bitmap, string outputPath)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }
}

/// <summary>
/// Exception thrown when an asset import fails for a known, user-facing reason.
/// </summary>
public class AssetImportException(string message) : Exception(message);
