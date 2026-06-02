using Retruxel.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for the png_to_tiles tool.
/// Receives raw color indices (byte[]) from PngToTilesTool and converts
/// them to SMS 4bpp planar tile format.
///
/// SMS tile format: 32 bytes per 8x8 tile (4 bitplanes × 8 rows).
/// Each row: bp0, bp1, bp2, bp3 — one bit per pixel per plane.
/// </summary>
public class SmsPngToTilesExtension : IToolExtension
{
    public string ToolId => "png_to_tiles";

    public Dictionary<string, object> GetDefaultParameters() => new();

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        System.Diagnostics.Debug.WriteLine("=== SmsPngToTilesExtension.Execute START ===");

        if (!input.TryGetValue("indices", out var indicesObj) || indicesObj is not byte[] indices)
        {
            System.Diagnostics.Debug.WriteLine("ERROR: 'indices' not found or wrong type");
            return new();
        }

        var width = GetInt(input, "width", 0);
        var height = GetInt(input, "height", 0);
        var tileCount = GetInt(input, "tileCount", (width / 8) * (height / 8));

        System.Diagnostics.Debug.WriteLine($"Converting {tileCount} tiles ({width}x{height}), {indices.Length} index bytes");

        var tiles = ConvertToSmsTiles(indices, width, height, tileCount);

        var hexLines = new List<string>();
        for (int i = 0; i < tiles.Length; i += 16)
        {
            var chunk = tiles.Skip(i).Take(16);
            hexLines.Add("    " + string.Join(", ", chunk.Select(b => $"0x{b:X2}")));
        }

        var result = new Dictionary<string, object>
        {
            ["tilesHex"] = string.Join(",\n", hexLines),
            ["tileCount"] = tileCount,
            ["totalBytes"] = tiles.Length
        };

        System.Diagnostics.Debug.WriteLine($"Generated {tileCount} tiles, {tiles.Length} bytes");
        System.Diagnostics.Debug.WriteLine("=== SmsPngToTilesExtension.Execute END ===");

        return result;
    }

    /// <summary>
    /// Converts flat color-index array to SMS 4bpp planar tile data.
    /// Each tile is 32 bytes: 8 rows × 4 bitplanes.
    /// </summary>
    private static byte[] ConvertToSmsTiles(byte[] indices, int width, int height, int tileCount)
    {
        var tilesW = width / 8;
        var tilesH = height / 8;
        // tileCount may be less than tilesW*tilesH when the source has trailing padding.
        // Stop at tileCount to avoid emitting blank padding tiles.
        int limit  = tileCount < tilesW * tilesH ? tileCount : tilesW * tilesH;
        var result = new List<byte>(limit * 32);

        int generated = 0;
        for (int tileY = 0; tileY < tilesH && generated < limit; tileY++)
        {
            for (int tileX = 0; tileX < tilesW && generated < limit; tileX++)
            {
                for (int row = 0; row < 8; row++)
                {
                    byte bp0 = 0, bp1 = 0, bp2 = 0, bp3 = 0;

                    for (int col = 0; col < 8; col++)
                    {
                        var px = tileX * 8 + col;
                        var py = tileY * 8 + row;
                        var idx = indices[py * width + px];
                        var bit = 7 - col;

                        if ((idx & 1) != 0) bp0 |= (byte)(1 << bit);
                        if ((idx & 2) != 0) bp1 |= (byte)(1 << bit);
                        if ((idx & 4) != 0) bp2 |= (byte)(1 << bit);
                        if ((idx & 8) != 0) bp3 |= (byte)(1 << bit);
                    }

                    result.Add(bp0);
                    result.Add(bp1);
                    result.Add(bp2);
                    result.Add(bp3);
                }
                generated++;
            }
        }

        return result.ToArray();
    }

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
}
