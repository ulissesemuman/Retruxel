using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Retruxel.Tool.TilePacker;

/// <summary>
/// Platform-agnostic tile packer that deduplicates tiles based on palette indices.
///
/// Operates on byte[] index maps produced by IndexedBitmapRenderer.Encodes() —
/// never touches raw PNG pixels. This ensures that two tiles that look identical
/// under the active palette are correctly recognized as duplicates, regardless
/// of their original RGB values.
///
/// Input contract (Dictionary keys):
///   "indexMap"       byte[]  — palette indices, one byte per pixel, row-major
///   "imageWidth"     int     — image width in pixels
///   "imageHeight"    int     — image height in pixels
///   "tileWidth"      int     — tile width in pixels  (default: 8)
///   "tileHeight"     int     — tile height in pixels (default: 8)
///   "enableFlipH"    bool    — detect horizontally flipped duplicates
///   "enableFlipV"    bool    — detect vertically flipped duplicates
///   "enableRotation" bool    — detect 90/180/270° rotated duplicates
///
/// Output contract (Dictionary keys):
///   "result"  TilePackResult  — typed result (List&lt;TileEntry&gt; + unique tile data)
///
/// PaletteSlot in each TileEntry defaults to 0 — the caller is responsible for
/// assigning per-tile palette slots after packing (e.g. via K-Means palette split).
/// VRAM offsets (startTile) are also resolved by the caller, not here.
/// </summary>
public class TilePackerTool : ITool
{
    public string ToolId => "retruxel.tool.tilepacker";
    public string DisplayName => "Tile Packer";
    public string Description => "Platform-agnostic tilemap optimizer — deduplicates tiles by palette index";
    public object? Icon => null;
    public string Category => "Optimization";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => false;
    public string? TargetExtensionId => "tile_packer";

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var indexMap = input["indexMap"] as byte[]
                          ?? throw new ArgumentException("indexMap (byte[]) is required");
        var imageWidth = Convert.ToInt32(input["imageWidth"]);
        var imageHeight = Convert.ToInt32(input["imageHeight"]);
        var tileWidth = input.TryGetValue("tileWidth", out var tw) ? Convert.ToInt32(tw) : 8;
        var tileHeight = input.TryGetValue("tileHeight", out var th) ? Convert.ToInt32(th) : 8;
        var enableFlipH = input.TryGetValue("enableFlipH", out var fh) && Convert.ToBoolean(fh);
        var enableFlipV = input.TryGetValue("enableFlipV", out var fv) && Convert.ToBoolean(fv);
        var enableRotation = input.TryGetValue("enableRotation", out var ro) && Convert.ToBoolean(ro);

        if (imageWidth <= 0 || imageHeight <= 0)
            throw new ArgumentException("imageWidth and imageHeight must be positive");

        if (indexMap.Length != imageWidth * imageHeight)
            throw new ArgumentException(
                $"indexMap length ({indexMap.Length}) does not match " +
                $"{imageWidth}×{imageHeight} = {imageWidth * imageHeight}");

        var result = PackTiles(
            indexMap, imageWidth, imageHeight,
            tileWidth, tileHeight,
            enableFlipH, enableFlipV, enableRotation);

        return new Dictionary<string, object>
        {
            ["result"] = result
        };
    }


    private static TilePackResult PackTiles(
        byte[] indexMap,
        int imageWidth, int imageHeight,
        int tileWidth, int tileHeight,
        bool enableFlipH, bool enableFlipV, bool enableRotation)
    {
        int tilesX = imageWidth / tileWidth;
        int tilesY = imageHeight / tileHeight;
        int totalTiles = tilesX * tilesY;

        var uniqueTiles = new List<byte[]>(totalTiles);
        var tilemap = new List<TileEntry>(totalTiles);
        var tileHashes = new Dictionary<string, int>(totalTiles);

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                var tile = ExtractTile(indexMap, imageWidth, tx, ty, tileWidth, tileHeight);

                var (tileIndex, flipH, flipV, rotation) =
                    FindOrAddTile(tile, tileWidth, tileHeight,
                                  uniqueTiles, tileHashes,
                                  enableFlipH, enableFlipV, enableRotation);

                tilemap.Add(new TileEntry
                {
                    TileIndex = tileIndex,
                    X = tx,
                    Y = ty,
                    FlipH = flipH,
                    FlipV = flipV,
                    Rotation = rotation,
                    PaletteSlot = 0   // assigned by caller after packing
                });
            }
        }

        return new TilePackResult
        {
            UniqueTiles = uniqueTiles,
            Tilemap = tilemap,
            TilemapWidth = tilesX,
            TilemapHeight = tilesY,
            OriginalTileCount = totalTiles,
            OptimizedTileCount = uniqueTiles.Count,
            CompressionRatio = totalTiles > 0 ? (double)uniqueTiles.Count / totalTiles : 0
        };
    }


    /// <summary>
    /// Extracts a tileWidth×tileHeight block of palette indices from the flat index map.
    /// </summary>
    private static byte[] ExtractTile(
        byte[] indexMap, int imageWidth,
        int tileCol, int tileRow,
        int tileWidth, int tileHeight)
    {
        var tile = new byte[tileWidth * tileHeight];
        int dstIdx = 0;
        int originX = tileCol * tileWidth;
        int originY = tileRow * tileHeight;

        for (int py = 0; py < tileHeight; py++)
        {
            int srcOffset = (originY + py) * imageWidth + originX;
            Array.Copy(indexMap, srcOffset, tile, dstIdx, tileWidth);
            dstIdx += tileWidth;
        }

        return tile;
    }


    private static (int tileIndex, bool flipH, bool flipV, int rotation) FindOrAddTile(
        byte[] tile,
        int tileWidth, int tileHeight,
        List<byte[]> uniqueTiles,
        Dictionary<string, int> tileHashes,
        bool enableFlipH, bool enableFlipV, bool enableRotation)
    {
        // Original
        var hash = ComputeHash(tile);
        if (tileHashes.TryGetValue(hash, out var idx))
            return (idx, false, false, 0);

        // FlipH
        if (enableFlipH)
        {
            hash = ComputeHash(FlipH(tile, tileWidth, tileHeight));
            if (tileHashes.TryGetValue(hash, out idx))
                return (idx, true, false, 0);
        }

        // FlipV
        if (enableFlipV)
        {
            hash = ComputeHash(FlipV(tile, tileWidth, tileHeight));
            if (tileHashes.TryGetValue(hash, out idx))
                return (idx, false, true, 0);
        }

        // FlipH + FlipV
        if (enableFlipH && enableFlipV)
        {
            hash = ComputeHash(FlipV(FlipH(tile, tileWidth, tileHeight), tileWidth, tileHeight));
            if (tileHashes.TryGetValue(hash, out idx))
                return (idx, true, true, 0);
        }

        // Rotations (90, 180, 270 clockwise)
        if (enableRotation)
        {
            var rot90 = Rotate90(tile, tileWidth, tileHeight);
            var rot180 = Rotate90(rot90, tileHeight, tileWidth);  // dimensions swap after 90°
            var rot270 = Rotate90(rot180, tileWidth, tileHeight);

            hash = ComputeHash(rot90);
            if (tileHashes.TryGetValue(hash, out idx)) return (idx, false, false, 90);

            hash = ComputeHash(rot180);
            if (tileHashes.TryGetValue(hash, out idx)) return (idx, false, false, 180);

            hash = ComputeHash(rot270);
            if (tileHashes.TryGetValue(hash, out idx)) return (idx, false, false, 270);
        }

        // New unique tile — store original orientation
        var newIndex = uniqueTiles.Count;
        uniqueTiles.Add(tile);
        tileHashes[ComputeHash(tile)] = newIndex;
        return (newIndex, false, false, 0);
    }


    private static byte[] FlipH(byte[] tile, int width, int height)
    {
        var result = new byte[tile.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[y * width + (width - 1 - x)] = tile[y * width + x];
        return result;
    }

    private static byte[] FlipV(byte[] tile, int width, int height)
    {
        var result = new byte[tile.Length];
        for (int y = 0; y < height; y++)
            Array.Copy(tile, y * width, result, (height - 1 - y) * width, width);
        return result;
    }

    /// <summary>
    /// Rotates 90° clockwise. Output dimensions are swapped (width↔height).
    /// For square tiles (8×8) dimensions remain the same.
    /// </summary>
    private static byte[] Rotate90(byte[] tile, int width, int height)
    {
        var result = new byte[tile.Length];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                result[x * height + (height - 1 - y)] = tile[y * width + x];
        return result;
    }


    private static string ComputeHash(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
    }
}
