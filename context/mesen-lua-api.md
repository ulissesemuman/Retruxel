Mesen Lua API reference
=======================

**Important:** This API is similar but not completely compatible with the old Mesen 0.9.x (or Mesen-S) Lua APIs.

Generated on Jul 6 2025, 06:45:57 for Mesen 2.1.1.

## Key Functions for LiveLink

### emu.getPaletteColor(index)
Returns the ARGB color value for the specified palette index.
- **index**: Palette color index (0-31 for SMS, 0-255 for SNES)
- **Returns**: Integer in ARGB format (alpha inverted: 0=opaque, 255=transparent)

### emu.read(address, memoryType, signed)
Reads an 8-bit value from the specified address and memory type.
- **address**: Address to read from
- **memoryType**: Memory type enum (e.g., `emu.memType.smsVideoRam`, `emu.memType.nesPpuMemory`)
- **signed**: When true, returns signed 8-bit value
- **Returns**: 8-bit value

### emu.getMemorySize(memoryType)
Returns the size (in bytes) of the specified memory type.
- **memoryType**: Memory type enum
- **Returns**: Size in bytes

### emu.getState()
Returns a table containing the console's current state, including:
- **consoleType**: String ("Nes", "Snes", "Sms", "GameGear", "Sg1000", etc.)
- **cpu**: CPU state (varies by console)
- Other console-specific state data

## Memory Types (memType enum)

### NES
- `emu.memType.nesPpuMemory` - PPU memory (includes CHR, nametables, palette)
- `emu.memType.nesMemory` - CPU memory
- `emu.memType.nesPaletteRam` - Palette RAM only

### SNES
- `emu.memType.snesVideoRam` - Video RAM (VRAM)
- `emu.memType.snesCgRam` - Palette RAM (CGRAM)
- `emu.memType.snesMemory` - CPU memory

### SMS/Game Gear/SG-1000
- `emu.memType.smsVideoRam` - Video RAM
- `emu.memType.smsPaletteRam` - Palette RAM (CRAM)
- `emu.memType.smsMemory` - CPU memory

## Event Types

### emu.eventType.startFrame
Triggered when a frame starts (after vertical blank ends)

### emu.eventType.endFrame
Triggered when a frame ends (when vertical blank starts)

### emu.eventType.inputPolled
Triggered after the emulator updates input devices (once per frame)

### emu.eventType.cpuExec
Triggered during CPU execution (high frequency)

## Socket Library (LuaSocket)

Mesen includes LuaSocket for network communication:

```lua
local socket = require("socket")
local server = socket.bind("*", 8888)
server:settimeout(0) -- Non-blocking mode

local client = server:accept()
if client then
    client:settimeout(0)
    local line, err = client:receive() -- Receive line (until \n)
    if not err then
        client:send("response\n")
    end
end
```

## Important Notes

1. **Network Access**: Must be enabled in Mesen settings:
   - Debug → Script Window → Settings → Restrictions
   - Check "Allow access to I/O and OS functions"
   - Check "Allow network access"

2. **getPaletteColor() returns ARGB**: Alpha is inverted (0=opaque, 255=transparent)
   - White: 0xFFFFFF
   - Black: 0x000000
   - Red: 0xFF0000

3. **Console Type Detection**: Use `emu.getState().consoleType` to detect console
   - Returns: "Nes", "Snes", "Sms", "GameGear", "Sg1000", etc.

4. **Memory Access**: Use appropriate memType for each console
   - SMS VRAM: `emu.memType.smsVideoRam`
   - NES PPU: `emu.memType.nesPpuMemory`
   - SNES VRAM: `emu.memType.snesVideoRam`
