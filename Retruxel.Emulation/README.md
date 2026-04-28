# Retruxel.Emulation - Libretro Integration (Proof of Concept)

## Overview

This is a proof of concept for integrating libretro cores directly into Retruxel, eliminating the need for external emulators and Lua scripting.

## Architecture

```
LibRetroCore (P/Invoke wrapper)
    ↓
Libretro Core DLL (genesis_plus_gx_libretro.dll, mesen_libretro.dll, etc.)
    ↓
EmulatorWindow (WPF UI with WriteableBitmap rendering)
```

## Features Implemented

✅ Load libretro cores dynamically (.dll)
✅ Load ROM files
✅ Video rendering (XRGB8888 format)
✅ Run/Pause/Reset controls
✅ Direct VRAM access via `retro_get_memory_data()`
✅ Frame-accurate emulation loop

## Testing the PoC

### 1. Download a Libretro Core

Download Genesis Plus GX (SMS/GG/SG-1000 support):
- https://buildbot.libretro.com/nightly/windows/x86_64/latest/

Look for: `genesis_plus_gx_libretro.dll`

Or download Mesen core (NES support):
- Look for: `mesen_libretro.dll`

### 2. Build and Run

```bash
dotnet build Retruxel.Emulation
```

Then open `EmulatorWindow` from Retruxel main app or create a test launcher.

### 3. Usage

1. Click **LOAD CORE** → select `genesis_plus_gx_libretro.dll`
2. Click **LOAD ROM** → select a `.sms`, `.gg`, or `.sg` ROM
3. Click **RUN** to start emulation
4. Click **DUMP VRAM** to extract video memory

## Next Steps

### Phase 1: Core Features (1 week)
- [ ] Audio output (NAudio integration)
- [ ] Input handling (keyboard → libretro joypad)
- [ ] Savestate support
- [ ] Frame advance (step-by-step debugging)

### Phase 2: Memory Inspection (3-5 days)
- [ ] Memory viewer panel (VRAM, CRAM, System RAM)
- [ ] Live tile viewer (decode tiles from VRAM)
- [ ] Palette viewer (decode colors from CRAM)
- [ ] Nametable viewer

### Phase 3: Asset Extraction (1 week)
- [ ] Extract tiles while running
- [ ] Extract sprites with animation detection
- [ ] Extract palettes with clustering
- [ ] One-click "Capture Scene" → create modules

### Phase 4: Integration (1 week)
- [ ] Embed EmulatorWindow as dockable panel in Retruxel
- [ ] Live preview: edit module → see changes in emulator
- [ ] Hot reload: modify assets → reload in emulator
- [ ] Automated testing: run ROM → verify behavior

## Advantages over LiveLink

| Feature | LiveLink (Lua) | Libretro Integration |
|---------|----------------|----------------------|
| Setup | External emulator + config | Built-in, zero setup |
| VRAM Access | TCP socket + base64 | Direct memory pointer |
| Control | Limited API | Full control (pause, step, rewind) |
| Rendering | Separate window | Integrated panel |
| Debugging | Log messages | Breakpoints, memory watch |
| Layer Control | Not available | Can be implemented |
| Performance | Network overhead | Native speed |

## Technical Notes

### Memory Access

Libretro provides direct access to:
- `RETRO_MEMORY_SAVE_RAM` - Save RAM (battery backup)
- `RETRO_MEMORY_RTC` - Real-time clock
- `RETRO_MEMORY_SYSTEM_RAM` - Main system RAM
- `RETRO_MEMORY_VIDEO_RAM` - Video RAM (tiles, nametable)

For SMS/GG, VRAM contains:
- 0x0000-0x3FFF: Tile data (448 tiles × 32 bytes)
- 0x3800-0x3FFF: Nametable (32×28 × 2 bytes)

CRAM (palette) is typically accessed via system RAM or core-specific extensions.

### Pixel Format

Currently supports `RETRO_PIXEL_FORMAT_XRGB8888` (32-bit BGRA).
Can be extended to support RGB565 for better performance.

### Threading

Emulation runs on background thread, video refresh dispatches to UI thread.
For better performance, consider using DirectX/OpenGL rendering.

## Known Limitations

- Audio not yet implemented
- Input hardcoded to return 0 (no controller support)
- No savestate UI
- CRAM access depends on core implementation

## Cores Tested

- [ ] Genesis Plus GX (SMS/GG/SG-1000/Genesis)
- [ ] Mesen (NES)
- [ ] Snes9x (SNES)
- [ ] Gambatte (GB/GBC)

## Resources

- Libretro API: https://github.com/libretro/libretro-common
- Core Downloads: https://buildbot.libretro.com/nightly/windows/x86_64/latest/
- Documentation: https://docs.libretro.com/
