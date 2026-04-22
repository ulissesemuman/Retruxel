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
    /// VRAM region ID from target.Specs.VramRegions.
    /// Ex: "background", "sprites", "plane_a", "plane_b"
    /// </summary>
    public string VramRegionId { get; set; } = string.Empty;

    /// <summary>
    /// Number of 8×8 tiles in this asset.
    /// Calculated at import time from image dimensions.
    /// Ex: a 64×8 image = 8 tiles.
    /// </summary>
    public int TileCount { get; set; }

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
}


