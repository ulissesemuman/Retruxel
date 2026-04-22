namespace Retruxel.SDK.LiveLink;

public class CaptureResult
{
    public byte[][] Tiles { get; set; } = Array.Empty<byte[]>();
    public uint[] Palette { get; set; } = Array.Empty<uint>();
    public ushort[] Nametable { get; set; } = Array.Empty<ushort>();
    public int TileWidth { get; set; } = 8;
    public int TileHeight { get; set; } = 8;
    public int NametableWidth { get; set; }
    public int NametableHeight { get; set; }
    public string TargetId { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
