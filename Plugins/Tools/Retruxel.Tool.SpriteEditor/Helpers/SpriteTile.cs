namespace Retruxel.Tool.SpriteEditor.Helpers;

/// <summary>
/// Represents a single tile in a metasprite frame.
/// Stores tile index and offset position relative to sprite origin.
/// </summary>
public class SpriteTile
{
    public int TileIndex { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public SpriteTile()
    {
    }

    public SpriteTile(int tileIndex, int offsetX, int offsetY)
    {
        TileIndex = tileIndex;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    public SpriteTile Clone()
    {
        return new SpriteTile(TileIndex, OffsetX, OffsetY);
    }
}
