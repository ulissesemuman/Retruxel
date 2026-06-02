using Retruxel.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Retruxel.Tool.PngToTiles;

/// <summary>
/// PNG to tiles converter tool.
/// Reads the asset's MapIndex (pre-computed color indices from AssetImporter)
/// and passes raw index data to the target extension for hardware-specific encoding.
///
/// The asset data is always provided in-memory by the VariableResolver — this tool
/// never reads files from disk. The RetruxelProject in memory is the single source
/// of truth; the .rtrxproject file on disk is persistence-only.
///
/// Flow:
///   1. VariableResolver injects inMemoryMapIndex/Width/Height/TileCount from project.Assets
///   2. This tool packages the data into a standard result dict
///   3. The target extension (e.g. SmsPngToTilesExtension) converts indices → tilesHex
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
        System.Diagnostics.Debug.WriteLine("=== PngToTilesTool.Execute START ===");
        System.Diagnostics.Debug.WriteLine($"Input keys: {string.Join(", ", input.Keys)}");

        var assetId = GetString(input, "assetId");
        System.Diagnostics.Debug.WriteLine($"assetId: '{assetId}'");

        if (string.IsNullOrEmpty(assetId))
            throw new ArgumentException("assetId is required");

        // All assets are injected in-memory by VariableResolver before this tool is called.
        // Keys: "inMemoryMapIndex" (byte[]), "inMemoryWidth" (int), "inMemoryHeight" (int),
        //       "inMemoryTileCount" (int).
        if (!input.TryGetValue("inMemoryMapIndex", out var rawMapIndex) || rawMapIndex is not byte[] indices)
            throw new InvalidOperationException(
                $"Asset '{assetId}' was not found in the in-memory asset registry. " +
                $"Ensure CodeGenerator.GenerateAsync populates inMemoryAssets with all project.Assets before rendering.");

        var width     = GetInt(input, "inMemoryWidth",     0);
        var height    = GetInt(input, "inMemoryHeight",    0);
        var tileCount = GetInt(input, "inMemoryTileCount", (width / 8) * (height / 8));

        System.Diagnostics.Debug.WriteLine($"Asset '{assetId}': {width}x{height}, {tileCount} tiles, {indices.Length} index bytes");
        System.Diagnostics.Debug.WriteLine("=== PngToTilesTool.Execute END ===");

        return new Dictionary<string, object>
        {
            ["indices"]    = indices,
            ["width"]      = width,
            ["height"]     = height,
            ["tileCount"]  = tileCount,
            ["tilesX"]     = width  / 8,
            ["tilesY"]     = height / 8,
            ["tileWidth"]  = 8,
            ["tileHeight"] = 8
        };
    }

    private static string? GetString(Dictionary<string, object> input, string key)
        => input.TryGetValue(key, out var value) ? value as string : null;

    private static int GetInt(Dictionary<string, object> input, string key, int defaultValue)
    {
        if (input.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
        }
        return defaultValue;
    }

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["assetId"]    = "",
        ["tileWidth"]  = 8,
        ["tileHeight"] = 8
    };
}
