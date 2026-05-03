
using Retruxel.Core.Interfaces;
using Retruxel.Core.Helpers;

namespace Retruxel.Tool.TilemapPreprocessor;

/// <summary>
/// Generic tilemap preprocessor tool.
/// Processes collision bitfield and map data for any tilemap-based console.
/// Returns raw numeric arrays - the CodeGen template decides the format.
/// </summary>
public class TilemapPreprocessorTool : ITool
{
    public string ToolId => "tilemap_preprocessor";
    public string DisplayName => "Tilemap Preprocessor";
    public string Description => "Generates collision bitfield and processes map data for tilemap modules";
    public string Category => "Graphics";
    public string? TargetId => null;
    public string? ModuleId => "tilemap";
    public object? Icon => null;
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public bool IsSingleton => true;
    public bool RequiresProject => false;
    public string? TargetExtensionId => "tilemap_preprocessor";

    public IEnumerable<string> Validate(Dictionary<string, object> input)
    {
        var errors = new List<string>();

        var maxTileSlots = GetInt(input, "maxTileSlots", 448);

        if (input.TryGetValue("solidTiles", out var st) && st is int[] solidTiles)
        {
            foreach (var id in solidTiles)
            {
                if (id < 0 || id >= maxTileSlots)
                    errors.Add($"Tile ID {id} out of range (0-{maxTileSlots - 1})");
            }
        }

        return errors;
    }

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Extract parameters
        var solidTiles = GetIntArray(input, "solidTiles");
        var mapData = GetIntArray(input, "mapData");
        
        // DEBUG: Log mapData info
        System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] mapData received: Length={mapData.Length}");
        if (mapData.Length > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] First 10 values: {string.Join(", ", mapData.Take(10))}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] WARNING: mapData is EMPTY!");
            if (input.TryGetValue("mapData", out var rawMapData))
            {
                System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] Raw mapData type: {rawMapData?.GetType().Name}");
                if (rawMapData is object[] objArr)
                {
                    System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] object[] length: {objArr.Length}");
                    if (objArr.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] First element type: {objArr[0]?.GetType().Name}, value: {objArr[0]}");
                    }
                }
            }
        }
        
        var startTile = GetInt(input, "startTile", 0);
        var mapWidth = GetInt(input, "mapWidth", 32);
        var mapHeight = GetInt(input, "mapHeight", 24);
        var mapX = GetInt(input, "mapX", 0);
        var mapY = GetInt(input, "mapY", 0);
        var maxTileSlots = GetInt(input, "maxTileSlots", 448);

        // Only apply clipping if mapX < 0 or mapY < 0 (tilemap starts off-screen)
        bool needsClipping = mapX < 0 || mapY < 0;
        
        int[] processedMapData;
        int finalWidth;
        int finalHeight;
        int drawX;
        int drawY;

        if (needsClipping)
        {
            var clippingResult = ApplyClipping(mapData, mapWidth, mapHeight, mapX, mapY);
            processedMapData = clippingResult.clippedData;
            finalWidth = clippingResult.width;
            finalHeight = clippingResult.height;
            drawX = clippingResult.drawX;
            drawY = clippingResult.drawY;
        }
        else
        {
            // No clipping needed - use full tilemap
            processedMapData = mapData;
            finalWidth = mapWidth;
            finalHeight = mapHeight;
            drawX = mapX;
            drawY = mapY;
        }

        // Calculate collision bytes needed
        var collisionBytes = (maxTileSlots + 7) / 8;

        // Generate collision bitfield
        var collisionArray = GenerateCollisionBitfield(solidTiles, maxTileSlots, collisionBytes);
        var collisionHex = string.Join(", ", collisionArray.Select(b => $"0x{b:X2}"));

        // Process map data (add startTile to each tile ID)
        var processedMap = ProcessMapData(processedMapData, startTile, finalWidth, finalHeight, maxTileSlots);

        // Return multiple formats for flexibility
        var processedMapHex = FormatMapAsHex(processedMap, finalWidth, finalHeight);
        var processedMapFlat = string.Join(", ", processedMap.Select(v => $"0x{v:X4}"));

        var result = new Dictionary<string, object>
        {
            // Collision data
            ["collisionBytes"] = collisionBytes,
            ["collisionArray"] = collisionArray,
            ["collisionHex"] = collisionHex,
            ["solidTilesCount"] = solidTiles.Length,
            ["solidTilesList"] = string.Join(", ", solidTiles),

            // Map data (as processed integers)
            ["processedMap"] = processedMap,
            ["processedMapHex"] = processedMapHex,
            ["processedMapFlat"] = processedMapFlat,
            ["mapEntryCount"] = processedMap.Length,
            
            // Clipping info
            ["clippedWidth"] = finalWidth,
            ["clippedHeight"] = finalHeight,
            ["drawX"] = drawX,
            ["drawY"] = drawY,
            ["originalWidth"] = mapWidth,
            ["originalHeight"] = mapHeight,
            ["wasClipped"] = needsClipping
        };
        
        return result;
    }

    /// <summary>
    /// Applies clipping to the tilemap when it starts off-screen (negative mapX or mapY).
    /// Returns only the visible portion by skipping the off-screen tiles.
    /// </summary>
    private (int[] clippedData, int width, int height, int drawX, int drawY) ApplyClipping(
        int[] mapData, int mapWidth, int mapHeight, int mapX, int mapY)
    {
        int sourceOffsetX = 0;
        int sourceOffsetY = 0;
        int drawX = mapX;
        int drawY = mapY;
        int drawWidth = mapWidth;
        int drawHeight = mapHeight;

        // Handle negative X offset (tilemap starts off-screen to the left)
        if (mapX < 0)
        {
            sourceOffsetX = -mapX;
            drawX = 0;
            drawWidth = mapWidth - sourceOffsetX;
        }

        // Handle negative Y offset (tilemap starts off-screen at the top)
        if (mapY < 0)
        {
            sourceOffsetY = -mapY;
            drawY = 0;
            drawHeight = mapHeight - sourceOffsetY;
        }

        // Extract the visible portion
        var clippedData = new int[drawWidth * drawHeight];
        for (int y = 0; y < drawHeight; y++)
        {
            for (int x = 0; x < drawWidth; x++)
            {
                int sourceX = sourceOffsetX + x;
                int sourceY = sourceOffsetY + y;
                
                if (sourceX >= mapWidth || sourceY >= mapHeight)
                {
                    clippedData[y * drawWidth + x] = -1; // Empty tile
                    continue;
                }

                int sourceIndex = sourceY * mapWidth + sourceX;
                if (sourceIndex < mapData.Length)
                    clippedData[y * drawWidth + x] = mapData[sourceIndex];
                else
                    clippedData[y * drawWidth + x] = -1;
            }
        }

        return (clippedData, drawWidth, drawHeight, drawX, drawY);
    }

    /// <summary>
    /// Generates a bitfield where each bit represents whether a tile ID is solid.
    /// Bit N = 1 means tile ID N is solid.
    /// </summary>
    private byte[] GenerateCollisionBitfield(int[] solidTiles, int maxTileSlots, int collisionBytes)
    {
        var bits = new byte[collisionBytes];

        foreach (var id in solidTiles)
        {
            if (id < 0 || id >= maxTileSlots) continue;
            bits[id >> 3] |= (byte)(1 << (id & 7));
        }

        return bits;
    }

    /// <summary>
    /// Processes map data by adding startTile offset to each tile ID.
    /// Returns array of processed tile values (raw integers).
    /// </summary>
    private int[] ProcessMapData(int[] mapData, int startTile, int mapWidth, int mapHeight, int maxTileSlots)
    {
        var totalCells = mapWidth * mapHeight;
        var result = new int[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            if (i >= mapData.Length)
            {
                result[i] = 0; // Empty cell
                continue;
            }

            int rawId = mapData[i];

            // -1 = empty cell → transparent tile 0
            int vramSlot = rawId < 0 ? 0 : rawId + startTile;

            // Clamp to valid VRAM range
            if (vramSlot >= maxTileSlots) vramSlot = maxTileSlots - 1;

            result[i] = vramSlot;
        }

        return result;
    }

    /// <summary>
    /// Formats processed map data as multi-line hex string (one row per line).
    /// </summary>
    private string FormatMapAsHex(int[] processedMap, int mapWidth, int mapHeight)
    {
        var lines = new List<string>();

        for (int row = 0; row < mapHeight; row++)
        {
            var entries = new List<string>();

            for (int col = 0; col < mapWidth; col++)
            {
                int idx = row * mapWidth + col;
                if (idx >= processedMap.Length)
                {
                    entries.Add("0x0000");
                    continue;
                }

                entries.Add($"0x{processedMap[idx]:X4}");
            }

            bool isLast = row == mapHeight - 1;
            lines.Add($"    {string.Join(", ", entries)}{(isLast ? "" : ",")}");
        }

        return string.Join("\n", lines);
    }

    // Helper methods to safely extract values from input dictionary
    private int GetInt(Dictionary<string, object> input, string key, int defaultValue = 0)
    {
        if (input.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
        }
        return defaultValue;
    }

    private int[] GetIntArray(Dictionary<string, object> input, string key)
    {
        if (input.TryGetValue(key, out var value))
            return ArrayConversionHelper.ToIntArray(value);
        return Array.Empty<int>();
    }
}
