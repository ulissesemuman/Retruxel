namespace Retruxel.Tool.TilemapEditor.Helpers;

/// <summary>
/// Calculates tileset layout dimensions for arranging tiles in a grid.
/// Internal to TilemapEditor — not shared with other tools.
/// </summary>
public static class TilesetLayoutCalculator
{
    /// <summary>
    /// Calculates optimal tiles per row for a given tile count.
    /// Prefers 16 tiles per row for SMS, scales up for larger tilesets.
    /// </summary>
    public static int CalculateTilesPerRow(int tileCount)
    {
        if (tileCount <= 16)  return tileCount;
        if (tileCount <= 256) return 16;
        return 32;
    }

    public static int CalculateTilesetWidth(int tileCount, int tileWidth)
        => CalculateTilesPerRow(tileCount) * tileWidth;

    public static int CalculateTilesetHeight(int tileCount, int tileWidth, int tileHeight)
    {
        int tilesPerRow = CalculateTilesPerRow(tileCount);
        int rows        = (tileCount + tilesPerRow - 1) / tilesPerRow;
        return rows * tileHeight;
    }
}
