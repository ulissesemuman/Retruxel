using System.Windows;

namespace Retruxel.Tool.LiveLink.Capture;

/// <summary>
/// Canvas Expansion - Expands VRAM capture by searching for maptable in ROM.
/// 
/// How it works:
/// 1. User specifies expansion size (e.g., +32 tiles right, +16 tiles down)
/// 2. System searches ROM for the current nametable pattern
/// 3. Once found, reads adjacent tiles from ROM
/// 4. Renders expanded map using current tileset and palette
/// 5. User visually confirms if it captured real map or garbage
/// 
/// This is useful when:
/// - Game stores full map in ROM (common in platformers)
/// - You don't know exact RAM addresses
/// - You want to see the full level layout
/// </summary>
public class CanvasExpansionCapture
{
    /// <summary>
    /// Expansion direction flags.
    /// </summary>
    [Flags]
    public enum ExpansionDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8,
        All = Left | Right | Up | Down
    }

    /// <summary>
    /// Result of canvas expansion operation.
    /// </summary>
    public class ExpansionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public byte[]? ExpandedNametable { get; set; }
        public int ExpandedWidth { get; set; }
        public int ExpandedHeight { get; set; }
        public int OriginalOffsetX { get; set; }
        public int OriginalOffsetY { get; set; }
        public int RomAddress { get; set; }
        public float Confidence { get; set; } // 0.0 - 1.0
    }

    /// <summary>
    /// Expands the current VRAM nametable by searching ROM or RAM.
    /// </summary>
    /// <param name="sourceConsole">Source console (nes, sms, gg, etc.) - determines input format</param>
    public static ExpansionResult ExpandCanvas(
        byte[] currentNametable,
        byte[] searchData,
        int expandLeft,
        int expandRight,
        int expandUp,
        int expandDown,
        string sourceConsole,
        bool isRamSearch = false)
    {
        // Determine bytes per tile based on source console hardware
        int bytesPerTile = sourceConsole switch
        {
            "nes" => 1,      // NES: 1 byte per tile in nametable
            "snes" => 2,     // SNES: 2 bytes per tile
            "sms" => 2,      // SMS: 2 bytes per tile
            "gg" => 2,       // Game Gear: 2 bytes per tile
            "sg1000" => 1,   // SG-1000: 1 byte per tile
            "gb" => 1,       // Game Boy: 1 byte per tile
            "gbc" => 1,      // Game Boy Color: 1 byte per tile
            _ => 2           // Default to 2 bytes
        };
        
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Source console: {sourceConsole.ToUpper()}, bytesPerTile={bytesPerTile}");
        
        // Input nametable is always in 2-byte format (ushort[] converted to byte[])
        // We need to extract actual tile indices based on source format
        int totalTiles = currentNametable.Length / 2;
        
        // Try common nametable sizes
        int vramWidth = 32;
        int vramHeight = 28;
        
        if (totalTiles == 32 * 30) // NES
        {
            vramWidth = 32;
            vramHeight = 30;
        }
        else if (totalTiles == 32 * 28) // SMS/GG
        {
            vramWidth = 32;
            vramHeight = 28;
        }
        else if (totalTiles == 32 * 24) // SG-1000
        {
            vramWidth = 32;
            vramHeight = 24;
        }
        else
        {
            return new ExpansionResult
            {
                Success = false,
                Message = $"Unsupported nametable size: {currentNametable.Length} bytes ({totalTiles} tiles).\n\n" +
                         $"Supported sizes:\n" +
                         $"• 32×30 tiles (1920 bytes) - NES\n" +
                         $"• 32×28 tiles (1792 bytes) - SMS/GG\n" +
                         $"• 32×24 tiles (1536 bytes) - SG-1000"
            };
        }

        if (expandLeft < 0 || expandRight < 0 || expandUp < 0 || expandDown < 0)
        {
            return new ExpansionResult
            {
                Success = false,
                Message = "Expansion values must be non-negative."
            };
        }

        if (expandLeft == 0 && expandRight == 0 && expandUp == 0 && expandDown == 0)
        {
            return new ExpansionResult
            {
                Success = false,
                Message = "No expansion specified. All values are zero."
            };
        }

        var searchResult = SearchNametableInData(currentNametable, searchData, vramWidth, vramHeight, bytesPerTile, isRamSearch);

        if (!searchResult.Found)
        {
            string source = isRamSearch ? "RAM" : "ROM";
            string compressionHint = "";
            
            // If we found something but confidence is too low, it's likely compressed
            if (searchResult.Confidence > 0.0f && searchResult.Confidence < 0.5f)
            {
                compressionHint = $"\n\nDetected pattern mismatch (confidence: {searchResult.Confidence:P1}).\n" +
                                 $"This suggests the map data is compressed or encoded.\n";
            }
            
            return new ExpansionResult
            {
                Success = false,
                Message = $"Could not find nametable pattern in {source}.{compressionHint}\n" +
                         $"Possible reasons:\n" +
                         $"• Map is compressed (RLE, LZ77, etc.)\n" +
                         $"• Map uses procedural generation\n" +
                         $"• Pattern is too uniform (all same tiles)\n\n" +
                         (isRamSearch ? 
                          $"Try:\n" +
                          $"• Scroll to a different part of the map\n" +
                          $"• Capture when more unique tiles are visible\n" :
                          $"Try these alternatives:\n" +
                          $"• Test with games that have static screens (title, menus)\n" +
                          $"• Test with single-screen puzzle games\n" +
                          $"• Use RAM search (scroll in-game first)\n\n" +
                          $"Examples of games that work:\n" +
                          $"• Donkey Kong (static screens)\n" +
                          $"• Pac-Man (static mazes)\n" +
                          $"• Balloon Fight (static levels)\n\n") +
                         $"Canvas Expansion works best for:\n" +
                         $"• Static screens (title, menus)\n" +
                         $"• Single-screen games\n" +
                         $"• Games with uncompressed maps\n\n" +
                         $"Does NOT work for:\n" +
                         $"• Scrolling platformers (Mega Man, Mario, Metroid)\n" +
                         $"• Games with compressed maps\n" +
                         $"• Procedurally generated levels",
                Confidence = searchResult.Confidence
            };
        }

        int newWidth = vramWidth + expandLeft + expandRight;
        int newHeight = vramHeight + expandUp + expandDown;

        var expandedNametable = ExtractExpandedNametable(
            searchData,
            searchResult.Address,
            searchResult.MapWidth,
            searchResult.MapHeight,
            vramWidth,
            vramHeight,
            expandLeft,
            expandRight,
            expandUp,
            expandDown,
            bytesPerTile);

        if (expandedNametable == null)
        {
            string failSource = isRamSearch ? "RAM" : "ROM";
            return new ExpansionResult
            {
                Success = false,
                Message = $"Failed to extract expanded nametable from {failSource}.",
                Confidence = searchResult.Confidence
            };
        }

        string successSource = isRamSearch ? "RAM" : "ROM";
        return new ExpansionResult
        {
            Success = true,
            Message = $"Successfully expanded canvas to {newWidth}×{newHeight} tiles.\n\n" +
                     $"Found at {successSource} address: 0x{searchResult.Address:X6}\n" +
                     $"Detected map size: {searchResult.MapWidth}×{searchResult.MapHeight}\n" +
                     $"Confidence: {searchResult.Confidence:P0}\n\n" +
                     $"Review the expanded map visually to confirm it's correct.",
            ExpandedNametable = expandedNametable,
            ExpandedWidth = newWidth,
            ExpandedHeight = newHeight,
            OriginalOffsetX = expandLeft,
            OriginalOffsetY = expandUp,
            RomAddress = searchResult.Address,
            Confidence = searchResult.Confidence
        };
    }

    private static SearchResult SearchNametableInData(byte[] nametable, byte[] searchData, int vramWidth, int vramHeight, int bytesPerTile, bool isRamSearch)
    {
        const int MIN_MATCH_THRESHOLD = 10; // Reduzido de 20 para aceitar padrões menores

        var tileIndices = new ushort[vramWidth * vramHeight];
        for (int i = 0; i < tileIndices.Length; i++)
        {
            if (bytesPerTile == 1)
            {
                // NES format: 1 byte per tile (skip every second byte which is 0x00)
                tileIndices[i] = nametable[i * 2];
            }
            else
            {
                // SMS format: 2 bytes per tile
                tileIndices[i] = (ushort)(nametable[i * 2] | (nametable[i * 2 + 1] << 8));
            }
        }

        // Extract pattern from center of nametable (more unique than edges)
        int patternStartX = Math.Min(8, vramWidth / 4);
        int patternStartY = Math.Min(8, vramHeight / 4);
        int patternWidth = Math.Min(16, vramWidth / 2);
        int patternHeight = Math.Min(12, vramHeight / 2);
        var pattern = new ushort[patternWidth * patternHeight];

        for (int y = 0; y < patternHeight; y++)
        {
            for (int x = 0; x < patternWidth; x++)
            {
                int srcIdx = (patternStartY + y) * vramWidth + (patternStartX + x);
                if (srcIdx < tileIndices.Length)
                    pattern[y * patternWidth + x] = tileIndices[srcIdx];
            }
        }

        // Debug: Check if pattern has variety (not all same tile)
        var uniqueTiles = pattern.Distinct().Count();
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Pattern size: {patternWidth}x{patternHeight} = {pattern.Length} tiles");
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Unique tiles in pattern: {uniqueTiles}/{pattern.Length}");
        
        // Log pattern tiles
        var patternPreview = string.Join(" ", pattern.Take(32).Select(t => $"{t:X2}"));
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Pattern preview: {patternPreview}");
        
        if (uniqueTiles < 20)
        {
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] WARNING: Pattern has low variety ({uniqueTiles} unique tiles) - trying full nametable search");
            
            // If pattern is too uniform, try searching for the entire nametable
            return SearchFullNametableInRom(tileIndices, searchData, vramWidth, vramHeight, bytesPerTile);
        }

        float bestConfidence = 0.0f;
        int bestAddress = -1;
        int bestMapWidth = vramWidth;
        int bestMapHeight = vramHeight;

        int[] commonWidths = { 32, 64, 128, 256, 40, 48, 56, 72, 80, 96, 112 };
        
        string source = isRamSearch ? "RAM" : "ROM";
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Searching {source} ({searchData.Length} bytes) for pattern...");

        foreach (int mapWidth in commonWidths)
        {
            int matchesFound = 0;
            
            for (int addr = 0; addr < searchData.Length - pattern.Length * 2; addr += 2)
            {
                int matches = 0;
                int total = 0;

                for (int i = 0; i < pattern.Length; i++)
                {
                    int x = i % patternWidth;
                    int y = i / patternWidth;
                    int romIdx = addr + (y * mapWidth + x) * bytesPerTile;

                    if (romIdx + bytesPerTile - 1 >= searchData.Length)
                        break;

                    ushort romTile = bytesPerTile == 1 ? searchData[romIdx] : (ushort)(searchData[romIdx] | (searchData[romIdx + 1] << 8));
                    if (romTile == pattern[i])
                        matches++;

                    total++;
                }

                if (total > 0)
                {
                    float confidence = (float)matches / total;

                    if (confidence > bestConfidence && matches >= MIN_MATCH_THRESHOLD)
                    {
                        bestConfidence = confidence;
                        bestAddress = addr - (patternStartY * mapWidth + patternStartX) * bytesPerTile;
                        bestMapWidth = mapWidth;
                        bestMapHeight = EstimateMapHeight(searchData, bestAddress, mapWidth, bytesPerTile);
                        matchesFound++;
                        
                        // Debug: Log when we find a better match
                        if (confidence > 0.05f)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Match at 0x{addr:X6} (width={mapWidth}): {matches}/{total} = {confidence:P1}");
                            
                            // Show what ROM has vs what we're looking for
                            var romSample = new List<string>();
                            for (int i = 0; i < Math.Min(16, pattern.Length); i++)
                            {
                                int x = i % patternWidth;
                                int y = i / patternWidth;
                                int romIdx = addr + (y * mapWidth + x) * bytesPerTile;
                                if (romIdx < searchData.Length)
                                {
                                    ushort romTile = bytesPerTile == 1 ? searchData[romIdx] : (ushort)(searchData[romIdx] | (searchData[romIdx + 1] << 8));
                                    romSample.Add($"{romTile:X2}");
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"  ROM:     {string.Join(" ", romSample)}");
                            System.Diagnostics.Debug.WriteLine($"  Pattern: {string.Join(" ", pattern.Take(16).Select(t => $"{t:X2}"))}");
                        }
                    }
                }
            }
            
            if (matchesFound > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Width {mapWidth}: {matchesFound} potential matches found");
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Best match: confidence={bestConfidence:P1}, address=0x{bestAddress:X6}, size={bestMapWidth}x{bestMapHeight}");

        return new SearchResult
        {
            Found = bestConfidence >= 0.5f, // Reduzido de 0.7 para 0.5 (50%)
            Address = bestAddress,
            MapWidth = bestMapWidth,
            MapHeight = bestMapHeight,
            Confidence = bestConfidence
        };
    }
    
    /// <summary>
    /// Fallback: Search for full nametable when pattern is too uniform.
    /// </summary>
    private static SearchResult SearchFullNametableInRom(ushort[] tileIndices, byte[] searchData, int vramWidth, int vramHeight, int bytesPerTile)
    {
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] === FULL NAMETABLE SEARCH ===");
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Searching for full nametable ({vramWidth}x{vramHeight} = {tileIndices.Length} tiles)...");
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Search data size: {searchData.Length} bytes");
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Bytes per tile: {bytesPerTile}");
        
        // Show first 32 tiles of nametable we're searching for
        var nametablePreview = string.Join(" ", tileIndices.Take(32).Select(t => $"{t:X2}"));
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Nametable preview (first 32 tiles): {nametablePreview}");
        
        float bestConfidence = 0.0f;
        int bestAddress = -1;
        int bestMapWidth = vramWidth;
        int totalAddressesChecked = 0;
        
        int[] commonWidths = { 32, 64, 128, 256, 40, 48, 56, 72, 80, 96, 112 };
        
        foreach (int mapWidth in commonWidths)
        {
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Trying width={mapWidth}...");
            int addressesForThisWidth = 0;
            int bestMatchForWidth = 0;
            
            // Search for exact nametable match with this width
            for (int addr = 0; addr < searchData.Length - tileIndices.Length * bytesPerTile; addr += bytesPerTile)
            {
                int matches = 0;
                int total = 0;
                
                for (int i = 0; i < tileIndices.Length; i++)
                {
                    int x = i % vramWidth;
                    int y = i / vramWidth;
                    int romIdx = addr + (y * mapWidth + x) * bytesPerTile;
                    
                    if (romIdx + bytesPerTile - 1 >= searchData.Length)
                        break;
                    
                    ushort romTile = bytesPerTile == 1 ? searchData[romIdx] : (ushort)(searchData[romIdx] | (searchData[romIdx + 1] << 8));
                    if (romTile == tileIndices[i])
                        matches++;
                    
                    total++;
                }
                
                if (total > 0)
                {
                    addressesForThisWidth++;
                    totalAddressesChecked++;
                    
                    if (matches > bestMatchForWidth)
                        bestMatchForWidth = matches;
                    
                    float confidence = (float)matches / total;
                    
                    if (confidence > bestConfidence)
                    {
                        bestConfidence = confidence;
                        bestAddress = addr;
                        bestMapWidth = mapWidth;
                        
                        if (confidence >= 0.1f) // Log anything above 10%
                        {
                            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Match at 0x{addr:X6} (width={mapWidth}): {matches}/{total} = {confidence:P1}");
                            
                            // Show what ROM has vs what we're looking for (first 16 tiles)
                            var romSample = new List<string>();
                            for (int i = 0; i < Math.Min(16, tileIndices.Length); i++)
                            {
                                int x = i % vramWidth;
                                int y = i / vramWidth;
                                int romIdx = addr + (y * mapWidth + x) * bytesPerTile;
                                if (romIdx + bytesPerTile - 1 < searchData.Length)
                                {
                                    ushort romTile = bytesPerTile == 1 ? searchData[romIdx] : (ushort)(searchData[romIdx] | (searchData[romIdx + 1] << 8));
                                    romSample.Add($"{romTile:X2}");
                                }
                            }
                            System.Diagnostics.Debug.WriteLine($"  ROM:       {string.Join(" ", romSample)}");
                            System.Diagnostics.Debug.WriteLine($"  Nametable: {string.Join(" ", tileIndices.Take(16).Select(t => $"{t:X2}"))}");
                        }
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Width {mapWidth}: checked {addressesForThisWidth} addresses, best match={bestMatchForWidth}/{tileIndices.Length} tiles");
        }
        
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] === FULL NAMETABLE SEARCH COMPLETE ===");
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Total addresses checked: {totalAddressesChecked}");
        System.Diagnostics.Debug.WriteLine($"[CanvasExpansion] Best confidence: {bestConfidence:P1} at 0x{bestAddress:X6}, width={bestMapWidth}");
        
        return new SearchResult
        {
            Found = bestConfidence >= 0.5f,
            Address = bestAddress,
            MapWidth = bestMapWidth,
            MapHeight = EstimateMapHeight(searchData, bestAddress, bestMapWidth, bytesPerTile),
            Confidence = bestConfidence
        };
    }

    private static int EstimateMapHeight(byte[] romData, int startAddress, int mapWidth, int bytesPerTile)
    {
        const int MAX_HEIGHT = 256;
        int height = 0;

        for (int y = 0; y < MAX_HEIGHT; y++)
        {
            int rowAddress = startAddress + y * mapWidth * bytesPerTile;
            if (rowAddress + mapWidth * bytesPerTile >= romData.Length)
                break;

            int validTiles = 0;
            for (int x = 0; x < mapWidth; x++)
            {
                int addr = rowAddress + x * bytesPerTile;
                ushort tile = bytesPerTile == 1 ? romData[addr] : (ushort)(romData[addr] | (romData[addr + 1] << 8));

                if (tile != 0xFFFF && tile != 0x0000 && tile < 0x01FF)
                    validTiles++;
            }

            if (validTiles > mapWidth / 2)
                height = y + 1;
            else
                break;
        }

        return Math.Max(height, 28);
    }

    private static byte[]? ExtractExpandedNametable(
        byte[] romData,
        int baseAddress,
        int mapWidth,
        int mapHeight,
        int vramWidth,
        int vramHeight,
        int expandLeft,
        int expandRight,
        int expandUp,
        int expandDown,
        int bytesPerTile)
    {
        int newWidth = vramWidth + expandLeft + expandRight;
        int newHeight = vramHeight + expandUp + expandDown;

        // Allow expansion beyond detected map size - user wants to explore
        // Just clamp to ROM size to avoid reading invalid memory
        int maxPossibleWidth = (romData.Length - baseAddress) / (bytesPerTile * Math.Max(1, newHeight));
        if (newWidth > maxPossibleWidth)
        {
            newWidth = maxPossibleWidth;
            expandRight = newWidth - vramWidth - expandLeft;
        }

        var expandedNametable = new byte[newWidth * newHeight * 2]; // Always output 2 bytes per tile

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                int srcX = x - expandLeft;
                int srcY = y - expandUp;

                // Calculate ROM address for this tile
                int romAddr = baseAddress + (srcY * mapWidth + srcX) * bytesPerTile;

                // Check if address is valid
                if (romAddr < 0 || romAddr + bytesPerTile - 1 >= romData.Length)
                {
                    // Fill with empty tile
                    expandedNametable[(y * newWidth + x) * 2] = 0x00;
                    expandedNametable[(y * newWidth + x) * 2 + 1] = 0x00;
                    continue;
                }

                if (bytesPerTile == 1)
                {
                    // NES format: 1 byte in ROM, output as 2 bytes (tile, 0x00)
                    expandedNametable[(y * newWidth + x) * 2] = romData[romAddr];
                    expandedNametable[(y * newWidth + x) * 2 + 1] = 0x00;
                }
                else
                {
                    // SMS format: 2 bytes in ROM, output as 2 bytes
                    expandedNametable[(y * newWidth + x) * 2] = romData[romAddr];
                    expandedNametable[(y * newWidth + x) * 2 + 1] = romData[romAddr + 1];
                }
            }
        }

        return expandedNametable;
    }

    private class SearchResult
    {
        public bool Found { get; set; }
        public int Address { get; set; }
        public int MapWidth { get; set; }
        public int MapHeight { get; set; }
        public float Confidence { get; set; }
    }
}
