using Retruxel.Core.Interfaces;
using SkiaSharp;

namespace Retruxel.Tool.TilePacker;

/// <summary>
/// Optimizes tilemaps by detecting duplicate tiles with flip/rotation support.
/// Analyzes 8x8 tiles and creates an optimized tilemap with transformation flags.
/// </summary>
public class TilePackerTool : ITool
{
    public string ToolId => "retruxel.tool.tilepacker";
    public string DisplayName => "Tile Packer";
    public string Description => "Optimize tilemaps by detecting duplicate tiles with flip/rotation support";
    public string Category => "Optimization";
    public bool IsStandalone => false;
    public bool RequiresProject => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var imagePath = input["imagePath"] as string ?? throw new ArgumentException("imagePath is required");
        var tileWidth = input.ContainsKey("tileWidth") ? Convert.ToInt32(input["tileWidth"]) : 8;
        var tileHeight = input.ContainsKey("tileHeight") ? Convert.ToInt32(input["tileHeight"]) : 8;
        var enableFlip = input.ContainsKey("enableFlip") && Convert.ToBoolean(input["enableFlip"]);
        var enableRotation = input.ContainsKey("enableRotation") && Convert.ToBoolean(input["enableRotation"]);

        using var bitmap = SKBitmap.Decode(imagePath);
        if (bitmap == null)
            throw new InvalidOperationException($"Failed to load image: {imagePath}");

        var result = PackTiles(bitmap, tileWidth, tileHeight, enableFlip, enableRotation);

        return new Dictionary<string, object>
        {
            ["uniqueTiles"] = result.UniqueTiles,
            ["tilemap"] = result.Tilemap,
            ["tilemapWidth"] = result.TilemapWidth,
            ["tilemapHeight"] = result.TilemapHeight,
            ["originalTileCount"] = result.OriginalTileCount,
            ["optimizedTileCount"] = result.OptimizedTileCount,
            ["compressionRatio"] = result.CompressionRatio,
            ["tileData"] = result.TileData
        };
    }

    private PackResult PackTiles(SKBitmap bitmap, int tileWidth, int tileHeight, bool enableFlip, bool enableRotation)
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
                var (tileIndex, flipH, flipV, rotate) = FindOrAddTile(tile, uniqueTiles, tileHashes, enableFlip, enableRotation);

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
            CompressionRatio = compressionRatio,
            TileData = SerializeTileData(uniqueTiles)
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

    private (int tileIndex, bool flipH, bool flipV, bool rotate) FindOrAddTile(
        byte[] tile,
        List<byte[]> uniqueTiles,
        Dictionary<string, int> tileHashes,
        bool enableFlip,
        bool enableRotation)
    {
        // Try original
        var hash = ComputeHash(tile);
        if (tileHashes.TryGetValue(hash, out var index))
            return (index, false, false, false);

        // Try horizontal flip
        if (enableFlip)
        {
            var flippedH = FlipHorizontal(tile);
            hash = ComputeHash(flippedH);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, true, false, false);

            // Try vertical flip
            var flippedV = FlipVertical(tile);
            hash = ComputeHash(flippedV);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, true, false);

            // Try both flips
            var flippedHV = FlipVertical(flippedH);
            hash = ComputeHash(flippedHV);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, true, true, false);
        }

        // Try rotations (90, 180, 270 degrees)
        if (enableRotation)
        {
            var rotated90 = Rotate90(tile);
            hash = ComputeHash(rotated90);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, false, true); // Simplified: just mark as rotated

            var rotated180 = Rotate90(rotated90);
            hash = ComputeHash(rotated180);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, false, true);

            var rotated270 = Rotate90(rotated180);
            hash = ComputeHash(rotated270);
            if (tileHashes.TryGetValue(hash, out index))
                return (index, false, false, true);
        }

        // Add as new unique tile
        var newIndex = uniqueTiles.Count;
        uniqueTiles.Add(tile);
        tileHashes[ComputeHash(tile)] = newIndex;
        return (newIndex, false, false, false);
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

    private string SerializeTileData(List<byte[]> tiles)
    {
        var lines = new List<string>();
        
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            var hexValues = new List<string>();
            
            // Convert RGBA to indexed color or grayscale for SMS
            for (int j = 0; j < tile.Length; j += 4)
            {
                var r = tile[j];
                var g = tile[j + 1];
                var b = tile[j + 2];
                var a = tile[j + 3];
                
                // Simple grayscale conversion for now
                var gray = (byte)((r + g + b) / 3);
                hexValues.Add($"0x{gray:X2}");
            }
            
            lines.Add($"    // Tile {i}");
            lines.Add($"    {string.Join(", ", hexValues)}");
        }
        
        return string.Join(",\n", lines);
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
        public string TileData { get; set; } = string.Empty;
    }

    private class TilemapEntry
    {
        public int TileIndex { get; set; }
        public bool FlipH { get; set; }
        public bool FlipV { get; set; }
        public bool Rotate { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
