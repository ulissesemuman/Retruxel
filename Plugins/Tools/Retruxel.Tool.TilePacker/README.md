# Tile Packer

Optimizes tilemaps by detecting duplicate tiles with flip and rotation support.

## Features

- **Duplicate Detection**: Identifies identical tiles to reduce memory usage
- **Horizontal Flip**: Detects tiles that are horizontal mirrors of existing tiles
- **Vertical Flip**: Detects tiles that are vertical mirrors of existing tiles
- **Rotation Support**: Detects tiles rotated by 90°, 180°, or 270°
- **Compression Metrics**: Reports original vs optimized tile count and compression ratio

## Usage

### As a Tool (from code generators)

```json
{
  "from": "tool",
  "tool": "retruxel.tool.tilepacker",
  "toolInput": {
    "imagePath": "path/to/image.png",
    "tileWidth": 8,
    "tileHeight": 8,
    "enableFlip": true,
    "enableRotation": false
  }
}
```

### Input Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `imagePath` | string | *required* | Path to the source image |
| `tileWidth` | int | 8 | Width of each tile in pixels |
| `tileHeight` | int | 8 | Height of each tile in pixels |
| `enableFlip` | bool | false | Enable horizontal/vertical flip detection |
| `enableRotation` | bool | false | Enable 90°/180°/270° rotation detection |

### Output

| Field | Type | Description |
|-------|------|-------------|
| `uniqueTiles` | List<byte[]> | Array of unique tile data (RGBA) |
| `tilemap` | List<TilemapEntry> | Tilemap with tile indices and transformation flags |
| `tilemapWidth` | int | Width of tilemap in tiles |
| `tilemapHeight` | int | Height of tilemap in tiles |
| `originalTileCount` | int | Total number of tiles before optimization |
| `optimizedTileCount` | int | Number of unique tiles after optimization |
| `compressionRatio` | double | Ratio of unique tiles to original tiles (0.0 - 1.0) |
| `tileData` | string | C array formatted tile data |

### TilemapEntry Structure

```csharp
{
    TileIndex: int,    // Index into uniqueTiles array
    FlipH: bool,       // Horizontal flip flag
    FlipV: bool,       // Vertical flip flag
    Rotate: bool,      // Rotation flag (simplified)
    X: int,            // X position in tilemap
    Y: int             // Y position in tilemap
}
```

## Example: Retruxel Logo Optimization

The Retruxel logo was designed with repeating tiles. Using TilePacker:

**Before optimization:**
- 32x24 pixels = 96 tiles (8x8)
- Memory: 96 tiles × 32 bytes = 3,072 bytes

**After optimization (with flip detection):**
- ~30-40 unique tiles (estimated)
- Memory: ~1,280 bytes (58% reduction)

## Master System Integration

For SMS/Game Gear, the tool outputs:
- Tile data in planar format (2bpp)
- Tilemap with VDP attribute flags:
  - Bit 9: Horizontal flip
  - Bit 10: Vertical flip
  - Bit 11: Palette select
  - Bit 12: Priority

## Future Enhancements

- [ ] Palette-aware optimization
- [ ] Pattern table bank management
- [ ] Visual tilemap editor integration
- [ ] Real-time preview
- [ ] Custom tile matching algorithms
- [ ] Support for 16x16 metatiles

## Technical Notes

- Uses SHA-256 hashing for fast tile comparison
- RGBA format internally, converts to target format on export
- Rotation detection is simplified (marks as rotated, doesn't encode angle)
- Designed to work with TilemapEditor and SplashScreen modules
