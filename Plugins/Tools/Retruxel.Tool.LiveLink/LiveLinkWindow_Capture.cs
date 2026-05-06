using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.LiveLink.Emulators;
using Retruxel.Tool.LiveLink.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// VRAM and screen capture with console-specific logic.
/// </summary>
public partial class LiveLinkWindow
{
    private async void BtnCaptureVRAM_Click(object sender, RoutedEventArgs e)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            LogError("Not connected to emulator");
            MessageBox.Show("Not connected to emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnCaptureVRAM.IsEnabled = false;
        BtnCaptureScreen.IsEnabled = false;
        BtnExpandCanvas.IsEnabled = false;

        try
        {
            LogInfo("Starting capture...");

            var capture = new CaptureResult();

            if (ChkCaptureTiles.IsChecked == true)
            {
                if (string.IsNullOrEmpty(_sourceConsole))
                {
                    LogError("Source console not detected");
                    return;
                }

                // Use hardware specs based on console (independent of Retruxel targets)
                var specs = GetConsoleSpecs(_sourceConsole);
                if (specs == null)
                {
                    LogError($"No specs available for console: {_sourceConsole}");
                    return;
                }
                int bpp = (int)Math.Log2(specs.ColorsPerTile);
                int bytesPerTile = (specs.TileWidth * specs.TileHeight * bpp) / 8;

                // NES: Allow selecting pattern table
                if (_sourceConsole == "nes")
                {
                    await CaptureNesTiles(capture, specs, bpp, bytesPerTile);
                }
                else if (_sourceConsole == "snes")
                {
                    await CaptureSnesTiles(capture, specs, bpp, bytesPerTile);
                }
                else if (_sourceConsole == "gb" || _sourceConsole == "gbc")
                {
                    await CaptureGameBoyTiles(capture, specs, bpp, bytesPerTile);
                }
                else if (_sourceConsole == "gba" || _sourceConsole == "ws" || _sourceConsole == "wsc" || _sourceConsole == "pce")
                {
                    LogWarning($"{_sourceConsole.ToUpper()} tile capture not yet fully implemented");
                }
                else if (_sourceConsole == "sms" || _sourceConsole == "gg")
                {
                    await CaptureSmsTiles(capture, specs, bpp, bytesPerTile);
                }
                else if (_sourceConsole == "sg1000")
                {
                    await CaptureSg1000Tiles(capture, specs, bytesPerTile);
                }
                else
                {
                    LogWarning($"{_sourceConsole.ToUpper()} tile capture not yet implemented");
                }
            }

            if (ChkCaptureNametable.IsChecked == true)
            {
                if (_sourceConsole == "nes")
                {
                    await CaptureNesNametable(capture);
                }
                else if (_sourceConsole == "snes")
                {
                    LogWarning("SNES nametable capture not yet implemented");
                }
                else if (_sourceConsole == "sms" || _sourceConsole == "gg")
                {
                    await CaptureSmsNametable(capture);
                }
                else if (_sourceConsole == "sg1000")
                {
                    await CaptureSg1000Nametable(capture);
                }
            }

            if (ChkCapturePalette.IsChecked == true)
            {
                if (_sourceConsole == "nes")
                {
                    await CaptureNesPalette(capture);
                }
                else if (_sourceConsole == "snes")
                {
                    await CaptureSnesPalette(capture);
                }
                else if (_sourceConsole == "sms")
                {
                    await CaptureSmsPalette(capture);
                }
                else if (_sourceConsole == "sg1000")
                {
                    await CaptureSg1000Palette(capture);
                }
                else if (_sourceConsole == "gg")
                {
                    await CaptureGameGearPalette(capture);
                }
                else if (_sourceConsole == "gb" || _sourceConsole == "gbc")
                {
                    await CaptureGameBoyPalette(capture);
                }
                else if (_sourceConsole == "gba" || _sourceConsole == "ws" || _sourceConsole == "wsc" || _sourceConsole == "pce")
                {
                    LogWarning($"{_sourceConsole.ToUpper()} palette capture not yet implemented");
                }
            }

            _lastCapture = capture;
            BtnImport.IsEnabled = true;

            // Enable Canvas Expansion if nametable was captured
            bool hasNametable = capture.Nametable != null &&
                               capture.NametableWidth > 0 &&
                               capture.NametableHeight > 0;
            BtnExpandCanvas.IsEnabled = hasNametable;

            if (hasNametable)
            {
                LogInfo($"Canvas Expansion available for {capture.NametableWidth}×{capture.NametableHeight} nametable");
            }

            // Render preview
            RenderPreview(capture);

            LogSuccess("✓ Capture complete!");
        }
        catch (Exception ex)
        {
            LogError($"Capture failed: {ex.Message}");
        }
        finally
        {
            BtnCaptureVRAM.IsEnabled = true;
            BtnCaptureScreen.IsEnabled = true;
        }
    }

    private async Task CaptureNesTiles(CaptureResult capture, Retruxel.Core.Models.TargetSpecs specs, int bpp, int bytesPerTile)
    {
        int patternTableIndex = CmbPatternTable.SelectedIndex;
        uint startAddr = 0x0000;
        int totalTiles = 256;

        if (patternTableIndex == 0) // 0x0000 (BG)
        {
            startAddr = 0x0000;
            totalTiles = 256;
            LogInfo("Pattern Table: 0x0000 (BG, 256 tiles)");
        }
        else if (patternTableIndex == 1) // 0x1000 (Sprites)
        {
            startAddr = 0x1000;
            totalTiles = 256;
            LogInfo("Pattern Table: 0x1000 (Sprites, 256 tiles)");
        }
        else // Both
        {
            startAddr = 0x0000;
            totalTiles = 512;
            LogInfo("Pattern Table: Both (512 tiles)");
        }

        int vramSize = totalTiles * bytesPerTile;
        LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
        LogInfo($"Requesting tiles from VRAM (0x{startAddr:X4})...");

        byte[] vramData = await _connection.ReadVramAsync(startAddr, vramSize);
        LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

        if (vramData.Length < vramSize)
        {
            LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
            totalTiles = vramData.Length / bytesPerTile;
        }

        LogInfo($"Decoding {totalTiles} tiles ({bpp}bpp)...");
        capture.Tiles = TileConverter.Decode(
            vramData,
            totalTiles,
            bpp,
            specs.TileWidth,
            specs.TileHeight,
            TileFormat.Planar,
            InterleaveMode.Tile);
        LogSuccess($"Decoded {capture.Tiles.Length} tiles");
    }

    private async Task CaptureSnesTiles(CaptureResult capture, Retruxel.Core.Models.TargetSpecs specs, int bpp, int bytesPerTile)
    {
        // SNES: Try to detect BG mode, but default to 4bpp if detection fails
        int detectedBpp = 4; // Default to 4bpp (most common)

        try
        {
            LogInfo("Attempting to read SNES BG mode from PPU registers...");
            LogWarning("SNES BG mode auto-detection not yet implemented - using 4bpp default");
        }
        catch
        {
            LogWarning("Could not detect SNES BG mode - using 4bpp default");
        }

        // Recalculate based on detected bpp
        int detectedBytesPerTile = (specs.TileWidth * specs.TileHeight * detectedBpp) / 8;

        // SNES: Start with smaller capture to test (256 tiles = 8KB)
        int totalTiles = 256;
        int vramSize = totalTiles * detectedBytesPerTile;

        LogInfo($"Using {detectedBpp}bpp mode: {totalTiles} tiles × {detectedBytesPerTile} bytes/tile = {vramSize} bytes");
        LogInfo($"Requesting tiles from VRAM (0x0000)...");

        byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
        LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

        // Debug: Log first 64 bytes to check if data looks valid
        var preview = string.Join(" ", vramData.Take(64).Select(b => b.ToString("X2")));
        LogInfo($"First 64 bytes: {preview}");

        // Check if data looks like valid tile data (not all zeros or all 0xFF)
        int zeroCount = vramData.Take(256).Count(b => b == 0x00);
        int ffCount = vramData.Take(256).Count(b => b == 0xFF);
        if (zeroCount > 240 || ffCount > 240)
        {
            LogWarning($"⚠ VRAM data looks suspicious: {zeroCount} zeros, {ffCount} 0xFF in first 256 bytes");
            LogWarning($"⚠ This may indicate VRAM is not initialized or wrong memory type");
        }

        if (vramData.Length < vramSize)
        {
            LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / detectedBytesPerTile} tiles");
            totalTiles = vramData.Length / detectedBytesPerTile;
        }

        LogInfo($"Decoding {totalTiles} tiles ({detectedBpp}bpp, line-interleaved)...");
        capture.Tiles = TileConverter.Decode(
            vramData,
            totalTiles,
            detectedBpp,
            specs.TileWidth,
            specs.TileHeight,
            TileFormat.Planar,
            InterleaveMode.Line); // SNES uses line-interleaved (bitplane pairs)
        LogSuccess($"Decoded {capture.Tiles.Length} tiles");
    }

    private async Task CaptureGameBoyTiles(CaptureResult capture, Retruxel.Core.Models.TargetSpecs specs, int bpp, int bytesPerTile)
    {
        // GB/GBC: 8KB VRAM (384 tiles), 2bpp
        int totalTiles = specs.MaxTilesInVram;
        int vramSize = totalTiles * bytesPerTile;

        LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
        LogInfo($"Requesting tiles from VRAM...");

        byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
        LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

        if (vramData.Length < vramSize)
        {
            LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
            totalTiles = vramData.Length / bytesPerTile;
        }

        LogInfo($"Decoding {totalTiles} tiles ({bpp}bpp, InterleaveMode.Line)...");
        capture.Tiles = TileConverter.Decode(
            vramData,
            totalTiles,
            bpp,
            specs.TileWidth,
            specs.TileHeight,
            TileFormat.Planar,
            InterleaveMode.Line); // GB uses line-interleaved like SMS
        LogSuccess($"Decoded {capture.Tiles.Length} tiles");
    }

    private async Task CaptureSmsTiles(CaptureResult capture, Retruxel.Core.Models.TargetSpecs specs, int bpp, int bytesPerTile)
    {
        // SMS/GG: 4bpp, line-interleaved
        int totalTiles = specs.MaxTilesInVram;
        int vramSize = totalTiles * bytesPerTile;

        LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
        LogInfo($"Requesting tiles from VRAM...");

        byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
        LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

        if (vramData.Length < vramSize)
        {
            LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
            totalTiles = vramData.Length / bytesPerTile;
        }

        LogInfo($"Decoding {totalTiles} tiles (4bpp, InterleaveMode.Line)...");
        capture.Tiles = TileConverter.Decode(
            vramData,
            totalTiles,
            bpp,
            specs.TileWidth,
            specs.TileHeight,
            TileFormat.Planar,
            InterleaveMode.Line);
        LogSuccess($"Decoded {capture.Tiles.Length} tiles");
    }

    private async Task CaptureSg1000Tiles(CaptureResult capture, Retruxel.Core.Models.TargetSpecs specs, int bytesPerTile)
    {
        // SG-1000: 1bpp (TMS9918), tile-interleaved
        int totalTiles = specs.MaxTilesInVram;
        int vramSize = totalTiles * bytesPerTile;

        LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
        LogInfo($"Requesting tiles from VRAM...");

        byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
        LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

        if (vramData.Length < vramSize)
        {
            LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
            totalTiles = vramData.Length / bytesPerTile;
        }

        LogInfo($"Decoding {totalTiles} tiles (1bpp, InterleaveMode.Tile)...");
        capture.Tiles = TileConverter.Decode(
            vramData,
            totalTiles,
            1, // 1bpp for SG-1000
            specs.TileWidth,
            specs.TileHeight,
            TileFormat.Planar,
            InterleaveMode.Tile);
        LogSuccess($"Decoded {capture.Tiles.Length} tiles");
    }

    // ==================== NAMETABLE CAPTURE ====================

    private async Task CaptureNesNametable(CaptureResult capture)
    {
        // NES: Allow selecting which nametable to capture
        int nametableIndex = CmbNametable.SelectedIndex;
        uint[] nametableAddresses = { 0x2000, 0x2400, 0x2800, 0x2C00 };
        uint nametableAddr = nametableAddresses[nametableIndex];

        LogInfo($"Requesting NES nametable from VRAM (0x{nametableAddr:X4})...");
        byte[] nametableData = await _connection.ReadVramAsync(nametableAddr, 32 * 30);
        LogSuccess($"✓ Nametable received: {nametableData.Length} bytes");

        // Read attribute table (64 bytes after nametable)
        uint attributeAddr = nametableAddr + 0x3C0; // 960 bytes after nametable start
        LogInfo($"Requesting NES attribute table from VRAM (0x{attributeAddr:X4})...");
        byte[] attributeData = await _connection.ReadVramAsync(attributeAddr, 64);
        LogSuccess($"✓ Attribute table received: {attributeData.Length} bytes");

        LogInfo("Decoding nametable...");
        capture.Nametable = NametableDecoder.Decode(nametableData, 32, 30, 1);
        capture.NametableWidth = 32;
        capture.NametableHeight = 30;

        // Store attribute table in metadata
        capture.Metadata["attributeTable"] = attributeData;

        LogSuccess($"Decoded nametable: {capture.NametableWidth}x{capture.NametableHeight}");
    }

    private async Task CaptureSmsNametable(CaptureResult capture)
    {
        // SMS/GG nametable at 0x3800 (32x28, 2 bytes per entry)
        LogInfo("Requesting SMS/GG nametable from VRAM (0x3800)...");
        byte[] nametableData = await _connection.ReadVramAsync(0x3800, 32 * 28 * 2);
        LogSuccess($"✓ Nametable received: {nametableData.Length} bytes");

        LogInfo("Decoding nametable...");
        capture.Nametable = NametableDecoder.Decode(nametableData, 32, 28, 2);
        capture.NametableWidth = 32;
        capture.NametableHeight = 28;
        LogSuccess($"Decoded nametable: {capture.NametableWidth}x{capture.NametableHeight}");
    }

    private async Task CaptureSg1000Nametable(CaptureResult capture)
    {
        // SG-1000 (TMS9918) nametable
        // TMS9918 Name Table is typically at 0x3800 (not 0x1800)
        // Address is configurable via VDP registers, but 0x3800 is most common
        LogInfo("Requesting SG-1000 nametable from VRAM (0x3800)...");
        byte[] nametableData = await _connection.ReadVramAsync(0x3800, 32 * 24);
        LogSuccess($"✓ Nametable received: {nametableData.Length} bytes");

        // Debug: Log first 32 bytes to verify data
        var preview = string.Join(" ", nametableData.Take(32).Select(b => b.ToString("X2")));
        LogInfo($"First 32 bytes: {preview}");

        LogInfo("Decoding nametable...");
        capture.Nametable = NametableDecoder.Decode(nametableData, 32, 24, 1);
        capture.NametableWidth = 32;
        capture.NametableHeight = 24;

        // Debug: Log first 32 decoded entries
        var decodedPreview = string.Join(" ", capture.Nametable.Take(32).Select(n => n.ToString("X2")));
        LogInfo($"First 32 decoded: {decodedPreview}");

        LogSuccess($"Decoded nametable: {capture.NametableWidth}x{capture.NametableHeight}");
    }

    // ==================== PALETTE CAPTURE ====================

    private async Task CaptureNesPalette(CaptureResult capture)
    {
        LogInfo("Requesting NES palette from PPU (0x3F00)...");
        byte[] paletteData = await _connection.ReadVramAsync(0x3F00, 32);
        LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

        LogInfo("Decoding NES palette...");
        capture.Palette = DecodeNesPalette(paletteData);
        LogSuccess($"Decoded {capture.Palette.Length} colors");
    }

    private async Task CaptureSnesPalette(CaptureResult capture)
    {
        LogInfo("Requesting SNES palette from CGRAM...");
        // SNES CGRAM: 512 bytes (256 colors × 2 bytes)
        // Mesen 2 exposes this via ReadCramAsync or palette memory type
        byte[] paletteData;
        if (_connection is MesenConnection mesenConn)
        {
            // Try reading CGRAM via Mesen's palette interface
            paletteData = await mesenConn.ReadCramAsync(256); // 256 colors
        }
        else
        {
            paletteData = await _connection.ReadMemoryAsync(0x0000, 512);
        }
        LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

        LogInfo("Decoding SNES palette...");
        capture.Palette = DecodeSnesPalette(paletteData);
        LogSuccess($"Decoded {capture.Palette.Length} colors");
    }

    private async Task CaptureSmsPalette(CaptureResult capture)
    {
        LogInfo("Requesting SMS palette from CRAM...");

        byte[] paletteData;
        if (_connection is MesenConnection mesenConn)
        {
            paletteData = await mesenConn.ReadCramAsync(32);
        }
        else
        {
            paletteData = await _connection.ReadMemoryAsync(0xC000, 32);
        }

        LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

        LogInfo("Decoding SMS palette (6-bit RGB)...");
        capture.Palette = DecodeSmsPalette(paletteData);
        LogSuccess($"Decoded {capture.Palette.Length} colors");
    }

    private async Task CaptureSg1000Palette(CaptureResult capture)
    {
        LogInfo("Using SG-1000 fixed TMS9918 palette...");

        // TMS9918 has a fixed 16-color palette (no CRAM)
        uint[] tms9918Palette = new uint[16]
        {
            0x00000000, // 0: Transparent
            0xFF000000, // 1: Black
            0xFF21C842, // 2: Medium Green
            0xFF5EDC78, // 3: Light Green
            0xFF5455ED, // 4: Dark Blue
            0xFF7D76FC, // 5: Light Blue
            0xFFD4524D, // 6: Dark Red
            0xFF42EBF5, // 7: Cyan
            0xFFFC5554, // 8: Medium Red
            0xFFFF7978, // 9: Light Red
            0xFFD4C154, // 10: Dark Yellow
            0xFFE6CE80, // 11: Light Yellow
            0xFF21B03B, // 12: Dark Green
            0xFFC95BBA, // 13: Magenta
            0xFFCCCCCC, // 14: Gray
            0xFFFFFFFF  // 15: White
        };

        capture.Palette = tms9918Palette;
        LogSuccess($"Loaded TMS9918 fixed palette: {capture.Palette.Length} colors");

        // Read Color Table
        LogInfo("Requesting SG-1000 Color Table from VRAM (0x2000)...");
        byte[] colorTableData = await _connection.ReadVramAsync(0x2000, 2048); // Read first 2KB to check
        LogSuccess($"✓ Color Table received: {colorTableData.Length} bytes");

        // Debug: Log first 32 bytes
        var ctPreview = string.Join(" ", colorTableData.Take(32).Select(b => b.ToString("X2")));
        LogInfo($"First 32 bytes: {ctPreview}");

        // Debug: Log bytes 32-64 to check if Mode II
        var ctPreview2 = string.Join(" ", colorTableData.Skip(32).Take(32).Select(b => b.ToString("X2")));
        LogInfo($"Bytes 32-64: {ctPreview2}");

        // Check if Graphics Mode II (bytes should vary after first 32)
        bool isGraphicsModeII = !colorTableData.Skip(32).Take(32).All(b => b == colorTableData[0]);
        LogInfo($"Graphics Mode: {(isGraphicsModeII ? "Mode II (per-line colors)" : "Mode I (per-8-tiles colors)")}");

        // Store Color Table in metadata for later processing
        capture.Metadata["colorTable"] = colorTableData;
        capture.Metadata["graphicsMode"] = isGraphicsModeII ? "mode2" : "mode1";
    }

    private async Task CaptureGameGearPalette(CaptureResult capture)
    {
        LogInfo("Requesting Game Gear palette from CRAM...");

        byte[] paletteData;
        if (_connection is MesenConnection mesenConn)
        {
            // Game Gear: 32 colors × 2 bytes = 64 bytes
            paletteData = await mesenConn.ReadCramAsync(32);
            LogInfo($"Game Gear palette: requested 32 colors, received {paletteData.Length} bytes");
        }
        else
        {
            paletteData = await _connection.ReadMemoryAsync(0xC000, 64);
        }

        LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

        LogInfo("Decoding Game Gear palette (12-bit RGB)...");
        capture.Palette = DecodeGameGearPalette(paletteData);
        LogSuccess($"Decoded {capture.Palette.Length} colors");
    }

    private async Task CaptureGameBoyPalette(CaptureResult capture)
    {
        LogInfo("Requesting GB/GBC palette from CRAM...");

        byte[] paletteData;
        if (_connection is MesenConnection mesenConn)
        {
            paletteData = await mesenConn.ReadCramAsync(32);
        }
        else
        {
            paletteData = await _connection.ReadMemoryAsync(0xFF47, 3);
        }

        LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

        LogInfo("Decoding GB/GBC palette (15-bit RGB)...");
        capture.Palette = DecodeGameBoyPalette(paletteData);
        LogSuccess($"Decoded {capture.Palette.Length} colors");
    }

    // ==================== SCREEN CAPTURE ====================

    private async void BtnCaptureScreen_Click(object sender, RoutedEventArgs e)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            LogError("Not connected to emulator");
            MessageBox.Show("Not connected to emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_connection is not MesenConnection mesenConn)
        {
            LogError("Screen capture only supported for Mesen 2");
            MessageBox.Show("Screen capture is currently only supported for Mesen 2.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        BtnCaptureVRAM.IsEnabled = false;
        BtnCaptureScreen.IsEnabled = false;
        BtnExpandCanvas.IsEnabled = false;

        try
        {
            LogInfo("Starting screen capture...");

            LogInfo("Requesting screen buffer...");
            byte[] screenBuffer = await mesenConn.GetScreenBufferAsync();
            LogSuccess($"✓ Received screen buffer: {screenBuffer.Length} bytes");

            // Get console specs for screen dimensions
            var specs = GetConsoleSpecs(_sourceConsole!);
            if (specs == null)
            {
                LogError($"No specs available for console: {_sourceConsole}");
                return;
            }

            int width = specs.ScreenWidth;
            int height = specs.ScreenHeight;
            int expectedSize = width * height * 4; // RGBA

            LogInfo($"Screen dimensions: {width}x{height} (expected {expectedSize} bytes)");

            if (screenBuffer.Length != expectedSize)
            {
                LogWarning($"⚠ Buffer size mismatch: got {screenBuffer.Length}, expected {expectedSize}");
            }

            // Convert screen buffer to bitmap for preview
            var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                width, height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null);

            bitmap.Lock();

            unsafe
            {
                byte* ptr = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = (y * width + x) * 4;
                        int dstIdx = y * stride + x * 4;

                        if (srcIdx + 3 < screenBuffer.Length)
                        {
                            ptr[dstIdx + 0] = screenBuffer[srcIdx + 2]; // B
                            ptr[dstIdx + 1] = screenBuffer[srcIdx + 1]; // G
                            ptr[dstIdx + 2] = screenBuffer[srcIdx + 0]; // R
                            ptr[dstIdx + 3] = screenBuffer[srcIdx + 3]; // A
                        }
                    }
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
            bitmap.Freeze();

            ImgPreview.Source = bitmap;
            LogSuccess("✓ Screen capture complete!");

            // Convert screen to tiles + palette + nametable
            LogInfo("Converting screen to tiles...");

            // Get destination target from project (not source console)
            string? destinationTarget = null;
            if (_input?.TryGetValue("targetId", out var targetObj) == true)
            {
                destinationTarget = targetObj?.ToString();
            }

            var conversion = ScreenToTilesConverter.Convert(
                screenBuffer,
                width,
                height,
                destinationTarget ?? _sourceConsole!);

            LogSuccess($"✓ Converted to {conversion.Tiles.Length} tiles, {conversion.Palette.Length} colors");
            LogInfo($"Nametable: {conversion.NametableWidth}×{conversion.NametableHeight}");

            // Log color distribution
            var colorGroups = conversion.Palette.GroupBy(c => c).OrderByDescending(g => g.Count());
            LogInfo($"Color distribution: {string.Join(", ", colorGroups.Take(5).Select(g => $"#{g.Key:X6} ({g.Count()}x)"))}");

            // Check tile limit for DESTINATION target (not source console)
            if (!string.IsNullOrEmpty(destinationTarget))
            {
                var targetSpecs = GetConsoleSpecs(destinationTarget);
                if (targetSpecs != null)
                {
                    int maxTiles = targetSpecs.MaxTilesInVram;
                    if (conversion.Tiles.Length > maxTiles)
                    {
                        LogWarning($"⚠ Generated {conversion.Tiles.Length} tiles, but target {destinationTarget.ToUpper()} supports max {maxTiles} tiles");
                        LogWarning($"⚠ Use Tile Optimizer tool to deduplicate and reduce tile count");
                    }
                    else
                    {
                        LogInfo($"✓ Tile count ({conversion.Tiles.Length}) is within {destinationTarget.ToUpper()} limit ({maxTiles})");
                    }
                }
            }
            else
            {
                LogInfo($"No destination target specified - skipping tile limit check");
            }

            // Store in capture result
            _lastCapture = new CaptureResult
            {
                Tiles = conversion.Tiles,
                Palette = conversion.Palette,
                Nametable = conversion.Nametable,
                NametableWidth = conversion.NametableWidth,
                NametableHeight = conversion.NametableHeight,
                TileWidth = 8,
                TileHeight = 8,
                TargetId = _sourceConsole!,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "screen_capture",
                    ["tilePaletteAssignments"] = conversion.TilePaletteAssignments
                }
            };

            BtnImport.IsEnabled = true;
            BtnExpandCanvas.IsEnabled = true;
            LogSuccess("✓ Ready to import!");
        }
        catch (Exception ex)
        {
            LogError($"Screen capture failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LiveLink] Screen capture error: {ex}");
        }
        finally
        {
            BtnCaptureVRAM.IsEnabled = true;
            BtnCaptureScreen.IsEnabled = true;
        }
    }
}
