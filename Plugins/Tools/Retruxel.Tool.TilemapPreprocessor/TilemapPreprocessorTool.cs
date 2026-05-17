
using Retruxel.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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
        var mapDataObj = input.ContainsKey("mapData") ? input["mapData"] : null;

        // DEBUG: Log input
        System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] ===== EXECUTE START =====");
        System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] mapDataObj type: {mapDataObj?.GetType().Name ?? "null"}");

        // Convert JsonElement to object[] if needed
        if (mapDataObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var arrayLength = jsonElement.GetArrayLength();
            System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] mapDataObj is JsonElement array with {arrayLength} items");

            var objArray = new object[arrayLength];
            int idx = 0;
            foreach (var item in jsonElement.EnumerateArray())
            {
                objArray[idx++] = item;
            }
            mapDataObj = objArray;

            if (arrayLength > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] First item type: {objArray[0]?.GetType().Name}");
            }
        }
        else if (mapDataObj is object[] objArr)
        {
            System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] mapDataObj is object[] with {objArr.Length} items");
            if (objArr.Length > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] First item type: {objArr[0]?.GetType().Name}");
            }
        }

        // Convert mapData to TileEntry[] if it's an object array
        var mapData = ConvertToTileEntryArray(mapDataObj);

        // DEBUG: Log mapData info
        System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] mapData converted: Length={mapData.Length}");
        if (mapData.Length > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] First entry: TileIndex={mapData[0].TileIndex}, FlipH={mapData[0].FlipH}, FlipV={mapData[0].FlipV}");
            if (mapData.Length > 1)
                System.Diagnostics.Debug.WriteLine($"[TilemapPreprocessor] Second entry: TileIndex={mapData[1].TileIndex}, FlipH={mapData[1].FlipH}, FlipV={mapData[1].FlipV}");
        }

        var startTile = GetInt(input, "startTile", 0);
        var mapWidth = GetInt(input, "mapWidth", 32);
        var mapHeight = GetInt(input, "mapHeight", 24);
        var mapX = GetInt(input, "mapX", 0);
        var mapY = GetInt(input, "mapY", 0);
        var maxTileSlots = GetInt(input, "maxTileSlots", 448);
        var paletteSlot = GetInt(input, "paletteSlot", 0);

        // Only apply clipping if mapX < 0 or mapY < 0 (tilemap starts off-screen)
        bool needsClipping = mapX < 0 || mapY < 0;

        TileEntry[] processedMapData;
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

        // Process map data - convert to ProcessedTileEntry[] with VRAM slots
        var processedMap = ProcessMapData(processedMapData, startTile, finalWidth, finalHeight, maxTileSlots);

        var result = new Dictionary<string, object>
        {
            // Collision data
            ["collisionBytes"] = collisionBytes,
            ["collisionArray"] = collisionArray,
            ["collisionHex"] = collisionHex,
            ["solidTilesCount"] = solidTiles.Length,
            ["solidTilesList"] = string.Join(", ", solidTiles),

            // Map data (as ProcessedTileEntry[])
            ["processedMap"] = processedMap,
            ["mapEntryCount"] = processedMap.Length,

            // Clipping info
            ["clippedWidth"] = finalWidth,
            ["clippedHeight"] = finalHeight,
            ["drawX"] = drawX,
            ["drawY"] = drawY,
            ["originalWidth"] = mapWidth,
            ["originalHeight"] = mapHeight,
            ["wasClipped"] = needsClipping,

            // Pass paletteSlot for target extension
            ["paletteSlot"] = paletteSlot
        };

        return result;
    }

    /// <summary>
    /// Converts mapData object to TileEntry array.
    /// Supports: TileEntry[], JsonElement array, anonymous objects, int[] (backward compat).
    /// </summary>
    private TileEntry[] ConvertToTileEntryArray(object? mapDataObj)
    {
        if (mapDataObj == null) return Array.Empty<TileEntry>();

        // Already TileEntry[]
        if (mapDataObj is TileEntry[] entries)
            return entries;

        // JsonElement or anonymous objects from JSON
        if (mapDataObj is object[] objArray)
        {
            return objArray.Select(obj =>
            {
                if (obj is JsonElement jsonElem && jsonElem.ValueKind == JsonValueKind.Object)
                {
                    return new TileEntry
                    {
                        TileIndex = jsonElem.TryGetProperty("tileIndex", out var ti) && ti.TryGetInt32(out var tiVal) ? tiVal : -1,
                        FlipH = jsonElem.TryGetProperty("flipH", out var fh) && fh.ValueKind == JsonValueKind.True,
                        FlipV = jsonElem.TryGetProperty("flipV", out var fv) && fv.ValueKind == JsonValueKind.True,
                        Rotation = jsonElem.TryGetProperty("rotation", out var rot) && rot.TryGetInt32(out var rotVal) ? rotVal : 0
                    };
                }
                else
                {
                    var type = obj.GetType();
                    var tiProp = type.GetProperty("tileIndex");
                    var fhProp = type.GetProperty("flipH");
                    var fvProp = type.GetProperty("flipV");
                    var rotProp = type.GetProperty("rotation");

                    return new TileEntry
                    {
                        TileIndex = tiProp != null ? (int)tiProp.GetValue(obj)! : -1,
                        FlipH = fhProp != null && (bool)fhProp.GetValue(obj)!,
                        FlipV = fvProp != null && (bool)fvProp.GetValue(obj)!,
                        Rotation = rotProp != null ? (int)rotProp.GetValue(obj)! : 0
                    };
                }
            }).ToArray();
        }

        // Backward compat: int[] (plain tile indices)
        if (mapDataObj is int[] intArray)
        {
            return intArray.Select(i => new TileEntry { TileIndex = i }).ToArray();
        }

        return Array.Empty<TileEntry>();
    }

    /// <summary>
    /// Applies clipping to the tilemap when it starts off-screen (negative mapX or mapY).
    /// Returns only the visible portion by skipping the off-screen tiles.
    /// </summary>
    private (TileEntry[] clippedData, int width, int height, int drawX, int drawY) ApplyClipping(
        TileEntry[] mapData, int mapWidth, int mapHeight, int mapX, int mapY)
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
        var clippedData = new TileEntry[drawWidth * drawHeight];
        for (int y = 0; y < drawHeight; y++)
        {
            for (int x = 0; x < drawWidth; x++)
            {
                int sourceX = sourceOffsetX + x;
                int sourceY = sourceOffsetY + y;

                if (sourceX >= mapWidth || sourceY >= mapHeight)
                {
                    clippedData[y * drawWidth + x] = TileEntry.Empty;
                    continue;
                }

                int sourceIndex = sourceY * mapWidth + sourceX;
                if (sourceIndex < mapData.Length)
                    clippedData[y * drawWidth + x] = mapData[sourceIndex];
                else
                    clippedData[y * drawWidth + x] = TileEntry.Empty;
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
    /// Processes map data by adding startTile offset to create VRAM slots.
    /// Returns ProcessedTileEntry[] with transformation flags - no hardware-specific encoding.
    /// </summary>
    private ProcessedTileEntry[] ProcessMapData(TileEntry[] mapData, int startTile, int mapWidth, int mapHeight, int maxTileSlots)
    {
        var totalCells = mapWidth * mapHeight;
        var result = new ProcessedTileEntry[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            if (i >= mapData.Length)
            {
                result[i] = new ProcessedTileEntry { VramSlot = 0 };
                continue;
            }

            var entry = mapData[i];

            // Empty cell → VRAM slot 0
            if (entry.IsEmpty)
            {
                result[i] = new ProcessedTileEntry { VramSlot = 0 };
                continue;
            }

            // Add startTile offset
            int vramSlot = entry.TileIndex + startTile;
            if (vramSlot >= maxTileSlots) vramSlot = maxTileSlots - 1;

            result[i] = new ProcessedTileEntry
            {
                VramSlot = vramSlot,
                FlipH = entry.FlipH,
                FlipV = entry.FlipV,
                Rotation = entry.Rotation
            };
        }

        return result;
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
        {
            if (value is int[] intArray) return intArray;
            if (value is System.Collections.IEnumerable enumerable)
            {
                var list = new List<int>();
                foreach (var item in enumerable)
                {
                    if (item is int i) list.Add(i);
                    else if (item is long l) list.Add((int)l);
                    else if (item is double d) list.Add((int)d);
                }
                return list.ToArray();
            }
        }
        return Array.Empty<int>();
    }
}
