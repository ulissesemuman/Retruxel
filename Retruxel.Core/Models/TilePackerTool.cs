using System.Collections.Generic;

namespace Retruxel.Core.Models
{
    public class TilePackResult
    {
        public List<byte[]> UniqueTiles { get; set; } = new();
        public List<TileEntry> Plane { get; set; } = new();
        public int PlaneWidth { get; set; }
        public int PlaneHeight { get; set; }
        public int OriginalTileCount { get; set; }
        public int OptimizedTileCount { get; set; }
        public double CompressionRatio { get; set; }
    }
}