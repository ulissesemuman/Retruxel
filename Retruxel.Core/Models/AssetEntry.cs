using System;
using System.Collections.Generic;

namespace Retruxel.Core.Models;

/// <summary>
/// Represents an imported asset in a Retruxel project.
/// Assets are identified by their filename without extension (the asset ID).
/// Ex: a file named "bg_tiles.png" has Id = "bg_tiles".
/// </summary>
public class AssetEntry
{
    /// <summary>
    /// Unique asset identifier — filename without extension.
    /// Used by modules to reference this asset (tilesAssetId, spriteAssetId).
    /// Ex: "bg_tiles", "player_sprites"
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Original filename including extension. Ex: "bg_tiles.png"
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Path relative to the project root. Ex: "assets/tiles/bg_tiles.png"
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the original source image (before processing).
    /// Ex: "Assets/Source/player_original.png"
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// Original image width in pixels. Stored for display in the Asset panel.
    /// </summary>
    public int SourceWidth { get; set; }

    /// <summary>
    /// Original image height in pixels. Stored for display in the Asset panel.
    /// </summary>
    public int SourceHeight { get; set; }

    /// <summary>
    /// Timestamp when this asset was imported.
    /// </summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// Parameters used to generate the processed image from source.
    /// Null for legacy assets imported before this system.
    /// </summary>
    public AssetGenerationParams? GenerationParams { get; set; }
}

/// <summary>
/// Parameters for generating processed assets from source images.
/// </summary>
public class AssetGenerationParams
{
    /// <summary>
    /// Color space for distance calculation: "RGB" or "LAB".
    /// </summary>
    public string ColorSpace { get; set; } = "LAB";

    /// <summary>
    /// Diversity weight for color selection (0.0 - 1.0).
    /// Higher = more diverse colors, lower = more accurate to source.
    /// </summary>
    public double DiversityWeight { get; set; } = 1.25;

    /// <summary>
    /// Target palette slot index (0 = Background, 1 = Sprite FOR SMS).
    /// </summary>
    /// 
    public int TargetPalette { get; set; }

    /// <summary>
    /// Number of colors to reduce to (typically 16 for SMS).
    /// </summary>
    public int ColorCount { get; set; } = 16;

    /// <summary>
    /// List of colors in the asset's palette, as hex strings.
    /// </summary>
    public List<string> Palette { get; set; } = new();

    /// <summary>
    /// The processed image data as a byte array of palette indices (mapIndex).
    /// </summary>
    public byte[] MapIndex { get; set; }

    /// <summary>
    /// Optimized image width in pixels. Stored for display in the Asset panel.
    /// </summary>
    public int OptimizedWidth { get; set; }

    /// <summary>
    /// Optimized image height in pixels. Stored for display in the Asset panel.
    /// </summary>
    public int OptimizedHeight { get; set; }

    /// <summary>
    /// Number of 8×8 tiles in this asset.
    /// Calculated at import time from image dimensions.
    /// Ex: a 64×8 image = 8 tiles.
    /// </summary>
    public int TileCount { get; set; }

    /// <summary>
    /// Enable detection of duplicate tiles that are flipped horizontally°.
    /// </summary>
    public bool EnableFlipH { get; set; }

    /// <summary>
    /// Enable detection of duplicate tiles that are flipped vertically.
    /// </summary>
    public bool EnableFlipV { get; set; }

    /// <summary>
    /// Enable detection of duplicate tiles that are rotated by 90/180/270°.
    /// </summary>
    public bool EnableRotation { get; set; }
}


