using Retruxel.Core.Interfaces;
using SkiaSharp;

namespace Retruxel.Tool.TilePacker;

/// <summary>
/// Platform-agnostic tile packer that analyzes images and detects tile patterns.
/// Returns raw tile data and transformation flags without platform-specific formatting.
/// </summary>
public class TilePackerTool : ITool
{
    public string ToolId => "retruxel.tool.tilepacker";
    public string DisplayName => "Tile Packer";
    public string Description => "Platform-agnostic tilemap optimizer with pattern detection";
    public object? Icon => null;
    public string Category => "Optimization";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var imagePath = input["imagePath"] as string ?? throw new ArgumentException("imagePath is required");
        var tileWidth = input.ContainsKey("tileWidth") ? Convert.ToInt32(input["tileWidth"]) : 8;
        var tileHeight = input.ContainsKey("tileHeight") ? Convert.ToInt32(input["tileHeight"]) : 8;
        var enableFlipH = input.ContainsKey("enableFlipH") && Convert.ToBoolean(input["enableFlipH"]);
        var enableFlipV = input.ContainsKey("enableFlipV") && Convert.ToBoolean(input["enableFlipV"]);
        var enableRotation = input.ContainsKey("enableRotation") && Convert.ToBoolean(input["enableRotation"]);

        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap == null)
            throw new InvalidOperationException($"Failed to load image: {imagePath}");

        var result = PackTiles(bitmap, tileWidth, tileHeight, enableFlipH, enableFlipV, enableRotation);

        return new Dictionary<string, object>
        {
            ["uniqueTiles"] = result.UniqueTiles,
            ["tilemap"] = result.Tilemap,
            ["tilemapWidth"] = result.TilemapWidth,
            ["tilemapHeight"] = result.TilemapHeight,
            ["originalTileCount"] = result.OriginalTileCount,
            ["optimizedTileCount"] = result.OptimizedTileCount,
            ["compressionRatio"] = result.CompressionRatio
        };
    }

    private PackResult PackTiles(SKBitmap bitmap, int tileWidth, int tileHeight, bool enableFlipH, bool enableFlipV, bool enableRotation)
    {
        var tilesX = bitmap.Width / tileWidth;
        var tilesY = bitmap.Height / tileHeight;
        var totalTiles = tilesX * tilesY;

        var uniqueTiles = new List<byte[]>();
        var tilemap = new List<TilemapEntry>();
        var tileHashes = new Dictionary<string, int>();

        for (int ty = 0; ty < tilesY; ty++)
        {
            for (int tx = 0; tx < tilesX; tx++)
            {
                var tile = ExtractTile(bitmap, tx * tileWidth, ty * tileHeight, tileWidth, tileHeight);
                var (tileIndex, flipH, flipV, rotate) = FindOrAddTile(tile, uniqueTiles, tileHashes, enableFlipH, enableFlipV, enableRotation);

                tilemap.Add(new TilemapEntry
                {
                    TileIndex = tileIndex,
                    FlipH = flipH,
                    FlipV = flipV,
                    Rotate = rotate,
                    X = tx,
                    Y = ty
                });
            }
        }

        var compressionRatio = totalTiles > 0 ? (double)uniqueTiles.Count / totalTiles : 0;

        return new PackResult
        {
            UniqueTiles = uniqueTiles,
            Tilemap = tilemap,
            TilemapWidth = tilesX,
            TilemapHeight = tilesY,
            OriginalTileCount = totalTiles,
            OptimizedTileCount = uniqueTiles.Count,
            CompressionRatio = compressionRatio
        };
    }

    private byte[] ExtractTile(SKBitmap bitmap, int x, int y, int width, int height)
    {
        var tile = new byte[width * height * 4]; // RGBA
        int index = 0;

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                var color = bitmap.GetPixel(x + px, y + py);
                tile[index++] = color.Red;
                tile[index++] = color.Green;
                tile[index++] = color.Blue;
                tile[index++] = color.Alpha;
            }
        }

        return tile;
    }

    private (int tileIndex, bool flipH, bool flipV, int rotation) FindOrAddTile(
        byte[] tile,
        List<byte[]> uniqueTiles,
        Dictionary<string, int> tileHashes,
        bool enableFlipH,
        bool enableFlipV,
        bool enableRotation)
    {
        // Try original
        var hash = ComputeHash(tile);
        if (tileHashes.TryGetValue(hash, out var index))
            return (index, false, false, 0);

        // Try horizontal flip
        if (enableFlipH)
        {
            var flippedH = FlipHorizontal(tile);
            hash = ComputeHash(flippedH);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, true, false, 0);
        }

        // Try vertical flip
        if (enableFlipV)
        {
            var flippedV = FlipVertical(tile);
            hash = ComputeHash(flippedV);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, true, 0);
        }

        // Try both flips
        if (enableFlipH && enableFlipV)
        {
            var flippedH = FlipHorizontal(tile);
            var flippedHV = FlipVertical(flippedH);
            hash = ComputeHash(flippedHV);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, true, true, 0);
        }

        // Try rotations (90, 180, 270 degrees)
        if (enableRotation)
        {
            var rotated90 = Rotate90(tile);
            hash = ComputeHash(rotated90);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, false, 90);

            var rotated180 = Rotate90(rotated90);
            hash = ComputeHash(rotated180);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, false, 180);

            var rotated270 = Rotate90(rotated180);
            hash = ComputeHash(rotated270);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, false, 270);
        }

        // Add as new unique tile
        var newIndex = uniqueTiles.Count;
        uniqueTiles.Add(tile);
        tileHashes[ComputeHash(tile)] = newIndex;
        return (newIndex, false, false, 0);
    }

    private byte[] FlipHorizontal(byte[] tile)
    {
        var width = (int)Math.Sqrt(tile.Length / 4);
        var height = width;
        var flipped = new byte[tile.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * 4;
                var dstIndex = (y * width + (width - 1 - x)) * 4;
                Array.Copy(tile, srcIndex, flipped, dstIndex, 4);
            }
        }

        return flipped;
    }

    private byte[] FlipVertical(byte[] tile)
    {
        var width = (int)Math.Sqrt(tile.Length / 4);
        var height = width;
        var flipped = new byte[tile.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * 4;
                var dstIndex = ((height - 1 - y) * width + x) * 4;
                Array.Copy(tile, srcIndex, flipped, dstIndex, 4);
            }
        }

        return flipped;
    }

    private byte[] Rotate90(byte[] tile)
    {
        var width = (int)Math.Sqrt(tile.Length / 4);
        var height = width;
        var rotated = new byte[tile.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * 4;
                var dstIndex = (x * width + (height - 1 - y)) * 4;
                Array.Copy(tile, srcIndex, rotated, dstIndex, 4);
            }
        }

        return rotated;
    }

    private string ComputeHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Convert.ToBase64String(hash);
    }



    private class PackResult
    {
        public List<byte[]> UniqueTiles { get; set; } = new();
        public List<TilemapEntry> Tilemap { get; set; } = new();
        public int TilemapWidth { get; set; }
        public int TilemapHeight { get; set; }
        public int OriginalTileCount { get; set; }
        public int OptimizedTileCount { get; set; }
        public double CompressionRatio { get; set; }
    }

    private class TilemapEntry
    {
        public int TileIndex { get; set; }
        public bool FlipH { get; set; }
        public bool FlipV { get; set; }
        public int Rotation { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
