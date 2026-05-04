using Retruxel.Core.Interfaces;
using Retruxel.Lib.ImageProcessing;

namespace Retruxel.Target.SMS.Tools;

/// <summary>
/// SMS-specific extension for the png_to_tiles tool.
/// Provides SMS-specific parameters and converts palette to RGB222 format.
/// 
/// SMS specs:
/// - 4bpp planar format
/// - Interleave by line
/// - Palette: RGB222 (6 bits total: 2 bits per channel)
/// - Max 16 colors per palette
/// - Tiles: 8x8 pixels
/// </summary>
public class SmsPngToTilesExtension : IToolExtension
{
    public string ToolId => "png_to_tiles";

    public Dictionary<string, object> GetDefaultParameters() => new()
    {
        ["bpp"] = 4,
        ["maxColors"] = 16,
        ["tileFormat"] = "Planar",
        ["interleaveMode"] = "Line"
    };

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        System.Diagnostics.Debug.WriteLine("=== SmsPngToTilesExtension.Execute START ===");
        System.Diagnostics.Debug.WriteLine($"Input keys: {string.Join(", ", input.Keys)}");

        var result = new Dictionary<string, object>();

        // Check if we have an indexed PNG path
        if (input.TryGetValue("imagePath", out var imagePathObj) && 
            imagePathObj is string imagePath && 
            !string.IsNullOrWhiteSpace(imagePath) &&
            File.Exists(imagePath))
        {
            System.Diagnostics.Debug.WriteLine($"Processing indexed PNG: {imagePath}");
            
            try
            {
                var indexedPngService = new IndexedPngService();
                var indexedData = indexedPngService.Read(imagePath);
                
                if (indexedData == null)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR: Failed to load indexed PNG from {imagePath}");
                    return result;
                }
                
                System.Diagnostics.Debug.WriteLine($"Loaded indexed PNG: {indexedData.Width}x{indexedData.Height}, {indexedData.Colors.Count} colors");
                
                // Convert indexed PNG to SMS 4bpp planar tiles
                var tiles = ConvertIndexedToSmsTiles(indexedData);
                result["tilesArray"] = tiles;
                result["totalBytes"] = tiles.Length;
                result["tileCount"] = (indexedData.Width / 8) * (indexedData.Height / 8);
                
                // Format tiles as hex string
                var hexLines = new List<string>();
                for (int i = 0; i < tiles.Length; i += 16)
                {
                    var chunk = tiles.Skip(i).Take(16);
                    hexLines.Add("    " + string.Join(", ", chunk.Select(b => $"0x{b:X2}")));
                }
                result["tilesHex"] = string.Join(",\n", hexLines);
                
                System.Diagnostics.Debug.WriteLine($"Generated {result["tileCount"]} tiles, {tiles.Length} bytes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR: Failed to process indexed PNG: {ex.Message}");
                throw;
            }
        }
        else
        {
            // Fallback: Legacy path with palette processing
            System.Diagnostics.Debug.WriteLine("Using legacy palette processing");
            
            // 1. Process Palette
            if (input.TryGetValue("palette", out var paletteObj) && paletteObj is uint[] palette)
            {
                System.Diagnostics.Debug.WriteLine($"Processing palette: {palette.Length} colors");
                var smsPalette = SmsColorUtils.ConvertPaletteToSmsRgb222(palette);
                result["paletteHardware"] = smsPalette;
                result["paletteHex"] = string.Join(", ", smsPalette.Select(b => $"0x{b:X2}"));
                System.Diagnostics.Debug.WriteLine($"Generated paletteHex: {result["paletteHex"]}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WARNING: No palette found in input!");
            }

            // 2. Process Tiles
            if (input.TryGetValue("tilesArray", out var tilesObj) && tilesObj is byte[] tiles)
            {
                System.Diagnostics.Debug.WriteLine($"Processing tiles: {tiles.Length} bytes");
                result["totalBytes"] = tiles.Length;

                // Format tiles as hex string, 16 bytes per line for readability
                var hexLines = new List<string>();
                for (int i = 0; i < tiles.Length; i += 16)
                {
                    var chunk = tiles.Skip(i).Take(16);
                    hexLines.Add("    " + string.Join(", ", chunk.Select(b => $"0x{b:X2}")));
                }
                result["tilesHex"] = string.Join(",\n", hexLines);
                var tilesHexStr = result["tilesHex"].ToString();
                if (tilesHexStr != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Generated tilesHex: {tilesHexStr.Substring(0, Math.Min(100, tilesHexStr.Length))}...");
                }

                if (input.TryGetValue("tileCount", out var count))
                {
                    result["tileCount"] = count;
                    System.Diagnostics.Debug.WriteLine($"Tile count: {count}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WARNING: No tilesArray found in input!");
            }
        }

        System.Diagnostics.Debug.WriteLine($"=== SmsPngToTilesExtension.Execute END (returned {result.Count} keys) ===");
        return result;
    }

    /// <summary>
    /// Converts indexed PNG data to SMS 4bpp planar tile format.
    /// Each tile is 8x8 pixels = 32 bytes (4 bitplanes × 8 rows).
    /// </summary>
    private static byte[] ConvertIndexedToSmsTiles(IndexedPngData data)
    {
        var tilesW = data.Width / 8;
        var tilesH = data.Height / 8;
        var tiles = new List<byte>();

        for (int tileY = 0; tileY < tilesH; tileY++)
        {
            for (int tileX = 0; tileX < tilesW; tileX++)
            {
                tiles.AddRange(ConvertTile(data, tileX, tileY));
            }
        }

        return tiles.ToArray();
    }

    /// <summary>
    /// Converts a single 8x8 tile from indexed data to SMS 4bpp planar format.
    /// SMS tile: 32 bytes, 4 bitplanes × 8 rows.
    /// </summary>
    private static byte[] ConvertTile(IndexedPngData data, int tileX, int tileY)
    {
        var tile = new byte[32];

        for (int row = 0; row < 8; row++)
        {
            byte bp0 = 0, bp1 = 0, bp2 = 0, bp3 = 0;

            for (int col = 0; col < 8; col++)
            {
                var px = tileX * 8 + col;
                var py = tileY * 8 + row;
                var idx = data.Indices[py * data.Width + px];

                var bit = 7 - col;
                if ((idx & 1) != 0) bp0 |= (byte)(1 << bit);
                if ((idx & 2) != 0) bp1 |= (byte)(1 << bit);
                if ((idx & 4) != 0) bp2 |= (byte)(1 << bit);
                if ((idx & 8) != 0) bp3 |= (byte)(1 << bit);
            }

            tile[row * 4 + 0] = bp0;
            tile[row * 4 + 1] = bp1;
            tile[row * 4 + 2] = bp2;
            tile[row * 4 + 3] = bp3;
        }

        return tile;
    }
}
