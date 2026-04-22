using Retruxel.Core.Interfaces;
using Retruxel.Lib.ImageProcessing;

namespace Retruxel.Tool.PngToTiles;

/// <summary>
/// Generic PNG to tiles converter tool.
/// Orchestrates the ImageProcessing library to convert PNG images to tile data.
/// Target-specific parameters (format, interleave, palette conversion) are provided
/// by IToolExtension implementations in each target assembly.
/// 
/// Flow:
///   1. Load PNG via PngReader
///   2. Extract palette via PaletteExtractor
///   3. Ensure black at index 0
///   4. Slice into 8x8 tiles via TileSlicer
///   5. Convert to target format via TileConverter
///   6. Return tile data, count, dimensions, palette
/// </summary>
public class PngToTilesTool : ITool
{
    public string ToolId => "png_to_tiles";
    public string DisplayName => "PNG to Tiles";
    public string Description => "Converts PNG images to tile data for retro consoles";
    public object? Icon => null;
    public string Category => "Graphics";
    public string? Shortcut => null;
    public bool IsStandalone => false;
    public string? TargetId => null;
    public bool RequiresProject => false;
    public string? TargetExtensionId => "png_to_tiles";

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Extract parameters
        var imagePath = GetString(input, "imagePath") 
            ?? throw new ArgumentException("imagePath is required");
        
        var tileWidth = GetInt(input, "tileWidth", 8);
        var tileHeight = GetInt(input, "tileHeight", 8);
        var bpp = GetInt(input, "bpp", 4);
        var maxColors = GetInt(input, "maxColors", 16);
        var tileFormat = GetEnum<TileFormat>(input, "tileFormat", TileFormat.Planar);
        var interleaveMode = GetEnum<InterleaveMode>(input, "interleaveMode", InterleaveMode.Line);

        // 1. Load PNG
        var pixels = PngReader.Load(imagePath, out var width, out var height);

        // 2. Extract palette (generic RGB)
        var palette = PaletteExtractor.Extract(pixels, maxColors);

        // 3. Ensure black at index 0
        palette = PaletteExtractor.EnsureBlackAtZero(palette);

        // 4. Slice into tiles (with palette mapping)
        var tiles = TileSlicer.Slice(pixels, width, height, tileWidth, tileHeight, palette);

        // 5. Convert to target format
        var tileData = TileConverter.Convert(tiles, palette, tileFormat, interleaveMode, bpp);

        // 6. Calculate tile count
        var tilesX = width / tileWidth;
        var tilesY = height / tileHeight;
        var tileCount = tilesX * tilesY;

        // Return generic results
        // Target-specific extension will add/override keys like:
        // - "paletteHardware" (converted to target format)
        // - "tilesArrayFormatted" (with target-specific formatting)
        return new Dictionary<string, object>
        {
            ["tilesArray"] = tileData,
            ["tileCount"] = tileCount,
            ["tilesX"] = tilesX,
            ["tilesY"] = tilesY,
            ["width"] = width,
            ["height"] = height,
            ["palette"] = palette, // Generic RGB palette
            ["paletteCount"] = palette.Length,
            ["bpp"] = bpp,
            ["tileWidth"] = tileWidth,
            ["tileHeight"] = tileHeight
        };
    }

    // Helper methods
    private string? GetString(Dictionary<string, object> input, string key)
        => input.TryGetValue(key, out var value) ? value as string : null;

    private int GetInt(Dictionary<string, object> input, string key, int defaultValue)
    {
        if (input.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
        }
        return defaultValue;
    }

    private T GetEnum<T>(Dictionary<string, object> input, string key, T defaultValue) where T : struct, Enum
    {
        if (input.TryGetValue(key, out var value))
        {
            if (value is T enumValue) return enumValue;
            if (value is string str && Enum.TryParse<T>(str, true, out var parsed))
                return parsed;
        }
        return defaultValue;
    }
}
