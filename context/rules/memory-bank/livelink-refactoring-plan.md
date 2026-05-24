# LiveLinkWindow Refactoring - Implementation Plan

## Status: COMPLETED ✅ (8/8 files created)

## Problem
LiveLinkWindow.xaml.cs has **2164 lines** with multiple responsibilities:
- Connection management (emulator discovery, connect/disconnect, keep-alive)
- VRAM/Screen capture (tiles, nametable, palette)
- Palette decoding (NES, SMS, SNES, GB, GG, SG-1000)
- Preview rendering (tilemap, tileset grid)
- Import/export (bitmap conversion, optimization)
- UI management (logging, button handlers)
- Console specs (hardware specifications)

## Solution: 8 Partial Classes

### ✅ 1. LiveLinkWindow_Connection.cs (COMPLETED) - ~400 lines
**Responsibility**: Connection management

**Methods**:
- `DiscoverEmulators()` - Discovers available emulators
- `BtnConnect_Click()` - Connect/disconnect button handler
- `GetDebugApiInstructions()` - Returns emulator-specific instructions
- `StartKeepAlive()` - Starts keep-alive timer
- `KeepAliveCheck()` - Checks connection and reconnects if needed
- `DetectConsoleFromRom()` - Detects console from ROM extension
- `DetectEmulatorByRom()` - Detects compatible emulator

### ✅ 2. LiveLinkWindow_UI.cs (COMPLETED) - ~100 lines
**Responsibility**: UI management

**Methods**:
- `AppendLog()` - Appends log entry with color
- `LogInfo()`, `LogSuccess()`, `LogWarning()`, `LogError()` - Logging helpers
- `BtnCopyLog_Click()` - Copies log to clipboard
- `BtnClearLog_Click()` - Clears log
- `BtnClose_Click()` - Closes window
- `TitleBar_MouseLeftButtonDown()` - Drag window
- `SelectRomFile()` - Opens ROM file dialog

### ✅ 3. LiveLinkWindow_Capture.cs (COMPLETED) - ~600 lines
**Responsibility**: VRAM and screen capture

**Methods**:
- `BtnCaptureVRAM_Click()` - Captures VRAM (tiles, nametable, palette)
  - NES: Pattern table selection, attribute table
  - SNES: BG mode detection, line-interleaved
  - SMS/GG: 4bpp line-interleaved
  - SG-1000: 1bpp tile-interleaved
  - GB/GBC: 2bpp line-interleaved
- `BtnCaptureScreen_Click()` - Captures screen buffer
- Console-specific capture logic

### ✅ 4. LiveLinkWindow_Palette.cs (COMPLETED) - ~300 lines
**Responsibility**: Palette decoding and validation

**Methods**:
- `DecodeSmsPalette()` - 6-bit RGB (00BBGGRR)
- `DecodeGameGearPalette()` - 12-bit RGB (0000BBBBGGGGRRRR)
- `DecodeGameBoyPalette()` - 15-bit RGB (5-5-5)
- `DecodeSnesPalette()` - 15-bit RGB (5-5-5)
- `DecodeNesPalette()` - 6-bit index to master palette
- `BtnValidateSmsColors_Click()` - Validates SMS color compliance

### ✅ 5. LiveLinkWindow_Preview.cs (COMPLETED) - ~400 lines
**Responsibility**: Preview rendering

**Methods**:
- `RenderPreview()` - Main preview dispatcher
- `CreatePreviewBitmap()` - Renders tilemap with nametable
  - Handles flip/palette attributes
  - NES attribute table support
  - SMS/GG nametable decoding
- `RenderTilesInGrid()` - Renders tiles in grid (fallback)

### ✅ 6. LiveLinkWindow_Import.cs (COMPLETED) - ~250 lines
**Responsibility**: Import and conversion

**Methods**:
- `BtnImport_Click()` - Opens palette optimization and imports
- `ConvertBitmapToCapture()` - Converts optimized bitmap to CaptureResult
- `ExtractPixelsFromBitmap()` - Extracts RGB pixels from bitmap
- `ApplyOptimizedPaletteToCapture()` - Remaps tiles to new palette

### ⏳ 7. LiveLinkWindow_Specs.cs (PENDING) - ~150 lines
**Responsibility**: Console hardware specifications

**Methods**:
- `GetConsoleSpecs()` - Returns TargetSpecs for console
  - NES: 256×240, 8×8 tiles, 4 colors (2bpp)
  - SNES: 256×224, 8×8 tiles, 16 colors (4bpp)
  - SMS: 256×192, 8×8 tiles, 16 colors (4bpp)
  - SG-1000: 256×192, 8×8 tiles, 2 colors (1bpp)
  - GG: 160×144, 8×8 tiles, 16 colors (4bpp)
  - GB/GBC: 160×144, 8×8 tiles, 4 colors (2bpp)
  - GBA: 240×160, 8×8 tiles, 256 colors (8bpp)
  - WS/WSC: 224×144, 8×8 tiles, 16 colors (4bpp)
  - PCE: 256×224, 8×8 tiles, 16 colors (4bpp)

### ✅ 7. LiveLinkWindow_Specs.cs (COMPLETED) - ~150 lines
**Responsibility**: Main class, initialization, fields

**Contents**:
- Private fields (connection, capture, settings, etc.)
- Constructor
- `OnWindowClosing()` - Cleanup
- Public properties (`ModuleData`)

### ✅ 8. LiveLinkWindow_Main.cs (COMPLETED) - ~100 lines
**Responsibility**: Main class, initialization, fields

**Contents**:
- Private fields (connection, capture, settings, etc.)
- Constructor
- `OnWindowClosing()` - Cleanup
- Public properties (`ModuleData`)

## Implementation Steps

1. ✅ Create LiveLinkWindow_Connection.cs
2. ✅ Create LiveLinkWindow_UI.cs
3. ✅ Create LiveLinkWindow_Capture.cs
4. ✅ Create LiveLinkWindow_Palette.cs
5. ✅ Create LiveLinkWindow_Preview.cs
6. ✅ Create LiveLinkWindow_Import.cs
7. ✅ Create LiveLinkWindow_Specs.cs
8. ✅ Create LiveLinkWindow_Main.cs (new main class)
9. ⏳ Rename/backup original LiveLinkWindow.xaml.cs
10. ⏳ Test compilation
11. ⏳ Test functionality (connect, capture, import)

## Benefits

- **Maintainability**: Each file has single responsibility
- **Readability**: ~150-600 lines per file vs 2164 lines
- **Testability**: Easier to test individual components
- **Collaboration**: Multiple developers can work on different aspects
- **Navigation**: Easier to find specific functionality

## File Size Comparison

| File | Lines | Responsibility |
|------|-------|----------------|
| **Before** | 2164 | Everything |
| **After (total)** | ~2300 | Split across 8 files |
| LiveLinkWindow.xaml.cs | ~150 | Main class |
| LiveLinkWindow_Connection.cs | ~400 | Connection |
| LiveLinkWindow_Capture.cs | ~600 | Capture |
| LiveLinkWindow_Palette.cs | ~300 | Palette |
| LiveLinkWindow_Preview.cs | ~400 | Preview |
| LiveLinkWindow_Import.cs | ~250 | Import |
| LiveLinkWindow_UI.cs | ~100 | UI |
| LiveLinkWindow_Specs.cs | ~150 | Specs |

## Next Steps

1. ⏳ Backup original LiveLinkWindow.xaml.cs → LiveLinkWindow.xaml.cs.bak
2. ⏳ Rename LiveLinkWindow_Main.cs → LiveLinkWindow.xaml.cs
3. ⏳ Test compilation
4. ⏳ Test all functionality:
   - Connection to emulators
   - VRAM capture (tiles, nametable, palette)
   - Screen capture
   - Preview rendering
   - Import with palette optimization
   - Keep-alive and reconnection
5. ⏳ If successful, delete backup file

## Notes

- All partial classes use `public partial class LiveLinkWindow`
- No breaking changes to public API
- All methods remain private (internal implementation)
- XAML file unchanged
- Zero impact on external code
