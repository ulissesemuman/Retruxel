# LiveLink Tool

Real-time emulator connection for capturing tiles, nametables, and palettes directly from running games.

## Purpose

LiveLink allows you to extract graphics from existing ROMs by connecting to emulator debug APIs. This is particularly useful for porting games like Kung Fu Master from NES to SMS — you can capture the exact tiles and tilemaps from the original game.

## Supported Emulators

| Emulator | Targets | Protocol | Default Port |
|---|---|---|---|
| **Mesen** | SMS, GG, SG-1000, NES | TCP/JSON | 8888 |
| **Emulicious** | SMS, GG, SG-1000, ColecoVision | TCP/Text | 58870 |

## How to Use

1. **Enable Debug API in Emulator**
   - **Mesen**: Tools → Preferences → Advanced → Enable Debug API
   - **Emulicious**: Tools → Remote Debugging → Enable

2. **Load ROM in Emulator**
   - Open the game you want to capture from
   - Navigate to the screen/level you want to extract

3. **Connect from Retruxel**
   - Open LiveLink tool (Tools → Live Link or Ctrl+Shift+L)
   - Select emulator from dropdown
   - Click CONNECT

4. **Capture Assets**
   - Check what you want to capture (Tiles, Nametable, Palette)
   - Click CAPTURE
   - Review captured data in preview

5. **Export**
   - Click EXPORT to save as PNG or JSON
   - Import into your Retruxel project

## Architecture

### SDK Interfaces

**IEmulatorConnection** — Base interface for emulator debug connections
- `ConnectAsync()` — Establish connection
- `ReadVramAsync()` — Read video RAM
- `ReadMemoryAsync()` — Read CPU memory
- `GetStateAsync()` — Get emulator state

**VramDecoder** — Converts raw VRAM back to tiles (inverse of TileConverter)
- `DecodePlanarTiles()` — Decode 4bpp planar tiles
- `DecodeNametable()` — Decode nametable/tilemap

**CaptureResult** — Captured data container
- `Tiles` — Decoded tile data
- `Palette` — Color palette
- `Nametable` — Tilemap data

### Implementations

**MesenConnection** — Mesen emulator via TCP/JSON protocol
**EmuliciousConnection** — Emulicious emulator via TCP/text protocol

## Memory Map Reference

### SMS/Game Gear/SG-1000

| Region | Address | Size | Description |
|---|---|---|---|
| VRAM Tiles | 0x0000 | 16KB | Tile pattern data (4bpp planar) |
| VRAM Nametable | 0x3800 | 1792 bytes | 32×28 tilemap (2 bytes per entry) |
| CRAM Palette | 0xC000 | 32 bytes | 16 colors × 2 palettes (RGB222) |

### NES

| Region | Address | Size | Description |
|---|---|---|---|
| CHR ROM | 0x0000 | 8KB | Tile pattern data (2bpp planar) |
| Nametable | 0x2000 | 1KB | 32×30 tilemap |
| Palette | 0x3F00 | 32 bytes | Background + sprite palettes |

## Workflow Example: Kung Fu Master Port

```
1. Load Kung Fu Master (NES) in Mesen
2. Navigate to first level
3. Connect LiveLink to Mesen
4. Capture tiles + nametable + palette
5. Export as PNG
6. Import PNG into Retruxel SMS project
7. Use TilemapReducer to optimize tileset
8. Generate SMS code with TilemapModule
```

## Future Enhancements

- [ ] Hot-reload: push changes back to emulator
- [ ] Sprite capture with animation detection
- [ ] Breakpoint integration
- [ ] Memory watch for dynamic data
- [ ] Multi-frame capture for animations
- [ ] Direct PNG export with palette mapping
- [ ] Automatic tileset reduction
- [ ] CHR ROM export for NES

## Technical Notes

- SMS uses 4bpp planar format (non-interleaved)
- NES uses 2bpp planar format (interleaved per line)
- Nametable entries are 16-bit on SMS (tile index + attributes)
- Palette is RGB222 on SMS (6-bit color), needs conversion to RGB888
- VRAM addresses are emulator-specific — check memory viewer
