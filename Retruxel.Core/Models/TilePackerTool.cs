using System.Collections.Generic;

namespace Retruxel.Core.Models
{
    public class TilePackResult
    {
        public List<byte[]> UniqueTiles { get; set; } = new();
        public List<TileEntry> Tilemap { get; set; } = new();
        public int TilemapWidth { get; set; }
        public int TilemapHeight { get; set; }
        public int OriginalTileCount { get; set; }
        public int OptimizedTileCount { get; set; }
        public double CompressionRatio { get; set; }
    }
}