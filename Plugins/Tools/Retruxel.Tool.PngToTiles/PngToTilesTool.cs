using Retruxel.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Retruxel.Tool.PngToTiles;

/// <summary>
/// PNG to tiles converter tool.
/// Reads the asset's MapIndex (pre-computed color indices from AssetImporter)
/// and passes raw index data to the target extension for hardware-specific encoding.
///
/// Flow:
///   1. Resolve assetId → load project → find asset entry
///   2. Read MapIndex (base64 byte[] of color indices) + optimized dimensions
///   3. Pass indices + dimensions to target extension (e.g. SmsPngToTilesExtension)
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

        var assetEntry = ResolveAsset(assetId, input);
        if (assetEntry == null)
            throw new InvalidOperationException($"Asset '{assetId}' not found in project");

        if (!assetEntry.Value.TryGetProperty("GenerationParams", out var genParams))
            throw new InvalidOperationException($"Asset '{assetId}' has no GenerationParams");

        if (!genParams.TryGetProperty("MapIndex", out var mapIndexProp))
            throw new InvalidOperationException($"Asset '{assetId}' has no MapIndex");

        var mapIndexBase64 = mapIndexProp.GetString() ?? "";
        var indices = Convert.FromBase64String(mapIndexBase64);

        var width = genParams.TryGetProperty("OptimizedWidth", out var wProp) ? wProp.GetInt32() : 0;
        var height = genParams.TryGetProperty("OptimizedHeight", out var hProp) ? hProp.GetInt32() : 0;
        var tileCount = genParams.TryGetProperty("TileCount", out var tcProp) ? tcProp.GetInt32() : (width / 8) * (height / 8);

        System.Diagnostics.Debug.WriteLine($"Asset '{assetId}': {width}x{height}, {tileCount} tiles, {indices.Length} index bytes");
        System.Diagnostics.Debug.WriteLine("=== PngToTilesTool.Execute END ===");

        return new Dictionary<string, object>
        {
            ["indices"] = indices,
            ["width"] = width,
            ["height"] = height,
            ["tileCount"] = tileCount,
            ["tilesX"] = width / 8,
            ["tilesY"] = height / 8,
            ["tileWidth"] = 8,
            ["tileHeight"] = 8
        };
    }

    private JsonElement? ResolveAsset(string assetId, Dictionary<string, object> input)
    {
        var projectPath = GetString(input, "projectPath");
        if (string.IsNullOrEmpty(projectPath)) return null;

        var projectFile = Path.Combine(projectPath, Path.GetFileName(projectPath) + ".rtrxproject");
        if (!File.Exists(projectFile)) return null;

        try
        {
            var json = File.ReadAllText(projectFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("Id", out var id) && id.GetString() == assetId)
                        return asset.Clone();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ResolveAsset error: {ex.Message}");
        }

        return null;
    }

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

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["assetId"] = "",
        ["tileWidth"] = 8,
        ["tileHeight"] = 8
    };
}
