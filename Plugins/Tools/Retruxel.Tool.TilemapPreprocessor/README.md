# Tilemap Preprocessor Tool

Generic tool for processing tilemap collision and map data.

## Purpose

This tool processes raw tilemap data and returns numeric arrays that CodeGen templates can format as needed. It's completely **target-agnostic** and reusable across multiple consoles.

## What It Does

1. **Generates collision bitfield** - Converts an array of solid tile IDs into a compact bitfield where each bit represents whether that tile ID is solid
2. **Processes map data** - Adds startTile offset to each tile ID and clamps to valid VRAM range

## Input Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `solidTiles` | int[] | [] | Array of tile IDs that are solid (for collision) |
| `mapData` | int[] | [] | Raw map data (tile IDs before startTile offset) |
| `startTile` | int | 0 | VRAM slot where tiles start |
| `mapWidth` | int | 32 | Map width in tiles |
| `mapHeight` | int | 24 | Map height in tiles |
| `maxTileSlots` | int | 448 | Maximum VRAM tile slots (console-specific) |

## Output

Returns a dictionary with processed data:

| Key | Type | Description |
|---|---|---|
| `collisionBytes` | int | Number of bytes needed for collision bitfield |
| `collisionArray` | byte[] | Raw collision bitfield bytes |
| `collisionHex` | string | Collision bytes formatted as hex (e.g., "0x22, 0x00, 0x04") |
| `solidTilesCount` | int | Number of solid tiles |
| `solidTilesList` | string | Comma-separated list of solid tile IDs |
| `processedMap` | int[] | Map data with startTile offset applied |
| `processedMapHex` | string | Map formatted as multi-line hex (one row per line) |
| `processedMapFlat` | string | Map formatted as single-line hex |
| `mapEntryCount` | int | Total number of map entries |

## Usage in CodeGen

### codegen.json

```json
{
  "variables": {
    "solidTiles": { "from": "module", "path": "solidTiles", "default": [] },
    "mapData": { "from": "module", "path": "mapData", "default": [] },
    "maxTileSlots": { "from": "module", "path": "maxTileSlots", "default": 448 },
    "preprocessed": {
      "from": "tool",
      "tool": "tilemap_preprocessor",
      "toolInput": {
        "solidTiles": "solidTiles",
        "mapData": "mapData",
        "startTile": "startTile",
        "mapWidth": "mapWidth",
        "mapHeight": "mapHeight",
        "maxTileSlots": "maxTileSlots"
      }
    }
  }
}
```

### Template (SMS - 2 bytes per tile)

```c
// Collision bitfield
const unsigned char collision[{{preprocessed.collisionBytes}}] = {
    {{preprocessed.collisionHex}}
};

// Map data as unsigned int (2 bytes)
const unsigned int bg_map[] = {
{{preprocessed.processedMapHex}}
};
```

### Template (NES - 1 byte per tile)

```c
// Collision bitfield (same)
const unsigned char collision[{{preprocessed.collisionBytes}}] = {
    {{preprocessed.collisionHex}}
};

// Map data as unsigned char (1 byte)
const unsigned char bg_map[] = {
    {{preprocessed.processedMapFlat}}  // Use flat format, template converts to 0xXX
};
```

## Supported Targets

This tool is used by:
- ✅ Sega Master System (448 tiles)
- ✅ Game Gear (448 tiles)
- ✅ SG-1000 (448 tiles)
- ✅ ColecoVision (448 tiles)
- 🔮 Future: NES, SNES, Genesis (different maxTileSlots)

## Design Philosophy

The tool is **pure data processing** - it doesn't know or care about target console formats. The CodeGen template decides how to format the output (unsigned int vs unsigned char, hex format, etc).

This separation makes the tool:
- ✅ Reusable across multiple targets
- ✅ Easy to test (pure functions)
- ✅ Simple to maintain
- ✅ Flexible for future consoles
