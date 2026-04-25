# LiveLink Lua Scripts

These Lua scripts enable the LiveLink debug API for each emulator.

## Emulicious (SMS/GG/SG-1000/ColecoVision)

**No script needed** - Emulicious has a built-in debugger that automatically enables the TCP API on port 58870 when you open Tools → Debugger.

## Mesen (NES)

**Script:** `retruxel_mesen.lua`  
**Port:** 8888

**Usage:**
1. Open Mesen
2. Load your ROM
3. Go to Tools → Script Window
4. Load `retruxel_mesen.lua`
5. The script will run automatically and enable the LiveLink API

**Commands:**
- `GET_NAMETABLE` - Returns 1KB nametable data from $2000
- `GET_PALETTE` - Returns 32 bytes palette data from $3F00
- `GET_CHR` - Returns 8KB CHR data from $0000

## Mesen-S (SNES)

**Script:** `retruxel_mesen_s.lua`  
**Port:** 8888

**Usage:**
1. Open Mesen-S
2. Load your ROM
3. Go to Tools → Script Window
4. Load `retruxel_mesen_s.lua`
5. The script will run automatically and enable the LiveLink API

**Commands:**
- `GET_TILEMAP` - Returns 2KB tilemap data from VRAM $0000
- `GET_PALETTE` - Returns 512 bytes palette data from CGRAM
- `GET_CHR` - Returns 64KB CHR data from VRAM

## BGB (Game Boy / Game Boy Color)

**Script:** `retruxel_bgb.lua`  
**Port:** 8765

**Usage:**
1. Open BGB
2. Load your ROM
3. Right-click → Options → System → Enable "Debug console"
4. Load the Lua script via the debug console
5. The script will run automatically and enable the LiveLink API

**Commands:**
- `GET_TILEMAP` - Returns 1KB tilemap data from $9800
- `GET_PALETTE` - Returns 5 bytes palette data from $FF47
- `GET_TILES` - Returns 6KB tile data from $8000

## mGBA (Game Boy Advance)

**Script:** `retruxel_mgba.lua`  
**Port:** 8888

**Usage:**
1. Open mGBA
2. Load your ROM
3. Go to Tools → Scripting
4. Load `retruxel_mgba.lua`
5. The script will run automatically and enable the LiveLink API

**Commands:**
- `GET_TILEMAP` - Returns 2KB tilemap data from VRAM $06000000
- `GET_PALETTE` - Returns 512 bytes palette data from Palette RAM
- `GET_TILES` - Returns 64KB tile data from VRAM

## Protocol

All scripts use a simple TCP request-response protocol:

1. Client connects to emulator on specified port
2. Client sends command string (e.g., "GET_NAMETABLE\n")
3. Emulator responds with raw binary data
4. Client closes connection

The scripts run on every frame and check for incoming connections without blocking emulation.
