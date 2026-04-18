# Tile Packer

Platform-agnostic tilemap optimizer that analyzes images and detects tile patterns.

## Architecture

Tile Packer is designed as a **platform-agnostic core** with **console-specific wrappers**:

- **TilePackerTool** (`retruxel.tool.tilepacker`) - Base tool that only analyzes patterns
- **TilePackerSMSTool** (`retruxel.tool.tilepacker.sms`) - SMS/Game Gear/SG-1000 wrapper
- **TilePackerNESTool** (`retruxel.tool.tilepacker.nes`) - NES wrapper
- **TilePackerColecoTool** (`retruxel.tool.tilepacker.coleco`) - ColecoVision wrapper

The base tool doesn't know about VDP formats, attribute bytes, or platform limitations. It simply:
1. Extracts tiles from an image
2. Detects duplicate patterns (with optional flip/rotation)
3. Returns raw tile data and transformation flags

Wrappers configure the base tool and format output for specific hardware.

## Features

- **Platform-Agnostic Core**: Analyzes patterns without platform knowledge
- **Duplicate Detection**: Identifies identical tiles to reduce memory usage
- **Configurable Transformations**: Enable/disable H flip, V flip, and rotation independently
- **Console-Specific Wrappers**: Format output for VDP/PPU requirements
- **Compression Metrics**: Reports original vs optimized tile count and compression ratio

## Usage

### Base Tool (Platform-Agnostic)

```json
{
  "from": "tool",
  "tool": "retruxel.tool.tilepacker",
  "toolInput": {
    "imagePath": "path/to/image.png",
    "tileWidth": 8,
    "tileHeight": 8,
    "enableFlipH": true,
    "enableFlipV": true,
    "enableRotation": false
  }
}
```

### SMS/Game Gear/SG-1000 Wrapper

```json
{
  "from": "tool",
  "tool": "retruxel.tool.tilepacker.sms",
  "toolInput": {
    "imagePath": "path/to/image.png",
    "tileWidth": 8,
    "tileHeight": 8
  }
}
```

Automatically enables H/V flip (VDP supports both) and formats tilemap with VDP attribute flags.

### NES Wrapper

```json
{
  "from": "tool",
  "tool": "retruxel.tool.tilepacker.nes",
  "toolInput": {
    "imagePath": "path/to/image.png",
    "tileWidth": 8,
    "tileHeight": 8
  }
}
```

Automatically enables H/V flip (PPU supports both) and formats tilemap with attribute bytes.

### ColecoVision Wrapper

```json
{
  "from": "tool",
  "tool": "retruxel.tool.tilepacker.coleco",
  "toolInput": {
    "imagePath": "path/to/image.png",
    "tileWidth": 8,
    "tileHeight": 8
  }
}
```

Disables all transformations (TMS9918 VDP has no flip/rotation support).

### Input Parameters (Base Tool)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `imagePath` | string | *required* | Path to the source image |
| `tileWidth` | int | 8 | Width of each tile in pixels |
| `tileHeight` | int | 8 | Height of each tile in pixels |
| `enableFlipH` | bool | false | Enable horizontal flip detection |
| `enableFlipV` | bool | false | Enable vertical flip detection |
| `enableRotation` | bool | false | Enable 90°/180°/270° rotation detection |

### Output (Base Tool)

| Field | Type | Description |
|-------|------|-------------|
| `uniqueTiles` | List<byte[]> | Array of unique tile data (RGBA) |
| `tilemap` | List<TilemapEntry> | Tilemap with tile indices and transformation flags |
| `tilemapWidth` | int | Width of tilemap in tiles |
| `tilemapHeight` | int | Height of tilemap in tiles |
| `originalTileCount` | int | Total number of tiles before optimization |
| `optimizedTileCount` | int | Number of unique tiles after optimization |
| `compressionRatio` | double | Ratio of unique tiles to original tiles (0.0 - 1.0) |

### TilemapEntry Structure (Base Tool)

```csharp
{
    TileIndex: int,    // Index into uniqueTiles array
    FlipH: bool,       // Horizontal flip flag
    FlipV: bool,       // Vertical flip flag
    Rotation: int,     // Rotation angle (0, 90, 180, 270)
    X: int,            // X position in tilemap
    Y: int             // Y position in tilemap
}
```

### Wrapper Output Formats

**SMS/Game Gear/SG-1000:**
```csharp
{
    tileIndex: int,    // Tile index
    flipH: bool,       // Horizontal flip
    flipV: bool,       // Vertical flip
    vdpValue: int,     // Combined VDP value (index | flip flags)
    x: int,
    y: int
}
```

**NES:**
```csharp
{
    tileIndex: int,       // Tile index
    flipH: bool,          // Horizontal flip
    flipV: bool,          // Vertical flip
    attributeByte: int,   // PPU attribute byte (flip flags)
    x: int,
    y: int
}
```

**ColecoVision:**
```csharp
{
    tileIndex: int,    // Tile index only (no transformations)
    x: int,
    y: int
}
```

## Hardware Capabilities

| Console | H Flip | V Flip | Rotation | Wrapper |
|---------|--------|--------|----------|----------|
| SMS/GG/SG-1000 | ✅ | ✅ | ❌ | `tilepacker.sms` |
| NES | ✅ | ✅ | ❌ | `tilepacker.nes` |
| ColecoVision | ❌ | ❌ | ❌ | `tilepacker.coleco` |

## Example: Retruxel Logo Optimization

The Retruxel logo was designed with repeating tiles. Using TilePacker:

**Before optimization:**
- 32x24 pixels = 96 tiles (8x8)
- Memory: 96 tiles × 32 bytes = 3,072 bytes

**After optimization (SMS with H/V flip):**
- ~30-40 unique tiles (estimated)
- Memory: ~1,280 bytes (58% reduction)

**After optimization (ColecoVision, no flip):**
- ~60-70 unique tiles (estimated)
- Memory: ~2,240 bytes (27% reduction)

## Future Enhancements

- [ ] Palette-aware optimization
- [ ] Pattern table bank management
- [ ] Visual tilemap editor integration
- [ ] Real-time preview
- [ ] Custom tile matching algorithms
- [ ] Support for 16x16 metatiles

## Technical Notes

- **Base tool is platform-agnostic** - doesn't know about VDP/PPU formats
- **Wrappers handle platform specifics** - attribute bytes, flip flags, limitations
- Uses SHA-256 hashing for fast tile comparison
- RGBA format internally (wrappers convert to target format)
- Rotation detection returns angle (0, 90, 180, 270)
- Designed to work with TilemapEditor and SplashScreen modules

## Adding New Console Wrappers

```csharp
public class TilePackerMyConsoleTool : ITool
{
    private readonly TilePackerTool _baseTool = new();

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Configure transformations based on hardware
        input["enableFlipH"] = true;  // Does hardware support H flip?
        input["enableFlipV"] = false; // Does hardware support V flip?
        input["enableRotation"] = false;

        var result = _baseTool.Execute(input);

        // Format tilemap for your console's video chip
        var tilemap = (List<object>)result["tilemap"];
        var formatted = FormatForMyConsole(tilemap);
        result["tilemap"] = formatted;

        return result;
    }
}
```
