

using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Lib.ImageProcessing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Retruxel.Lib.ImageProcessing.ColorMatching;

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
    /// <param name="vramRegionId">VRAM region ID from target.Specs.VramRegions.</param>
    /// <param name="target">Active target — provides the hardware palette for color reduction.</param>
    /// <param name="reducedPalette">Reduced color palette from optimization window.</param>
    /// <returns>AssetEntry ready to add to RetruxelProject.Assets.</returns>
    /// <exception cref="AssetImportException">Thrown when the import fails for a known reason.</exception>
    public static AssetEntry Import(
        string sourcePngPath,
        string projectPath,
        string vramRegionId,
        ITarget target,
        List<SKColor>? reducedPalette = null,
        string? colorSpace = null,
        double? diversityWeight = null)
    {
        // 1. Validate source file
        if (!File.Exists(sourcePngPath))
            throw new AssetImportException($"Source file not found: {sourcePngPath}");

        // 2. Load source image
        using var sourceStream = File.OpenRead(sourcePngPath);
        using var sourceBitmap = SKBitmap.Decode(sourceStream)
            ?? throw new AssetImportException($"Failed to decode image: {sourcePngPath}");

        ValidateDimensions(sourceBitmap, sourcePngPath);

        // 3. Determine output paths
        var region = target.Specs.VramRegions.FirstOrDefault(r => r.Id == vramRegionId)
            ?? throw new AssetImportException($"VRAM region '{vramRegionId}' not found in target specs.");

        var assetId = Path.GetFileNameWithoutExtension(sourcePngPath);
        var assetFileName = assetId + ".png";
        
        // Source folder: Assets/Source/
        var sourceFolder = Path.Combine(projectPath, "Assets", "Source");
        var sourceFullPath = Path.Combine(sourceFolder, assetFileName);
        var sourceRelativePath = Path.Combine("Assets", "Source", assetFileName).Replace('\\', '/');

        // 4. Ensure folder exists
        Directory.CreateDirectory(sourceFolder);

        // 5. Copy original to Source folder (preserve original)
        File.Copy(sourcePngPath, sourceFullPath, overwrite: true);

        // 6. Get hardware palette
        var hardwarePalette = target.GetHardwarePalette();
        if (hardwarePalette.Count == 0)
            throw new AssetImportException($"Target '{target.TargetId}' returned an empty hardware palette.");

        // 7. Calculate generation params from reduced palette
        var palette = reducedPalette ?? hardwarePalette.Select(c => new SKColor(c.R, c.G, c.B)).ToList();
        var colorCount = palette.Count;

        // 8. Calculate tile count from source dimensions
        var tileCount = (sourceBitmap.Width / TileSize) * (sourceBitmap.Height / TileSize);

        // 9. Extract suggested colors for palette slot population
        var suggestedColors = palette.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}").ToList();

        // 10. Create generation params
        var generationParams = new AssetGenerationParams
        {
            TargetPalette = 0, // Default to slot 0
            ColorCount = colorCount,
            ColorSpace = colorSpace ?? "LAB",
            DiversityWeight = diversityWeight ?? 0.7,
            ColorOrder = null // No reordering by default
        };

        return new AssetEntry
        {
            Id = assetId,
            FileName = assetFileName,
            RelativePath = sourceRelativePath, // Points to source
            SourcePath = sourceRelativePath,
            GeneratedPath = string.Empty, // No generated file needed
            GenerationParams = generationParams,
            VramRegionId = vramRegionId,
            TileCount = tileCount,
            SourceWidth = sourceBitmap.Width,
            SourceHeight = sourceBitmap.Height,
            ImportedAt = DateTime.Now,
            IsIndexed = true,
            ColorCount = colorCount,
            SuggestedColors = suggestedColors
        };
    }

    /// <summary>
    /// Generates a preview of the color-reduced image without saving to disk.
    /// Used by the AssetImporterWindow to show the before/after comparison.
    /// </summary>
    public static SKBitmap ReduceColorsToHardware(string sourcePngPath, ITarget target)
    {
        using var stream = File.OpenRead(sourcePngPath);
        using var source = SKBitmap.Decode(stream)
            ?? throw new AssetImportException($"Failed to decode image: {sourcePngPath}");

        var palette = target.GetHardwarePalette();
        var reduced = ColorMatching.ReduceColors(source, palette);

        return ColorMatching.BitmapFromByteArray(reduced, source.Width, source.Height, palette);
    }

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
