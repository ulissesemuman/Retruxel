namespace Retruxel.Tool.PlanePreprocessor;

/// <summary>
/// Represents a processed tile entry ready for code generation.
/// Contains VRAM slot and transformation flags - no hardware-specific encoding.
/// </summary>
public class ProcessedTileEntry
{
    /// <summary>VRAM slot (tileIndex + startTile offset).</summary>
    public int VramSlot { get; set; }

    /// <summary>Flip the tile horizontally.</summary>
    public bool FlipH { get; set; }

    /// <summary>Flip the tile vertically.</summary>
    public bool FlipV { get; set; }

    /// <summary>Clockwise rotation in degrees (0, 90, 180, 270).</summary>
    public int Rotation { get; set; }
}
