
using Retruxel.Lib.ImageProcessing;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace Retruxel.Tool.PngToTiles.Sms;

public class PngToTilesSmsTool : ITool
{
    public string ToolId => "retruxel.tool.pngtotiles.sms";
    public string DisplayName => "PNG to Tiles (SMS)";
    public string Description => "Converts PNG images to SMS 4bpp planar tile format";
    public object? Icon => null;
    public string Category => "ImageProcessing";
    public string? Shortcut => null;
    public bool IsStandalone => true;
    public string? TargetId => "sms";
    public bool RequiresProject => false;

    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        var assetPath = input["assetPath"].ToString()!;

        // SMS specific parameters (encapsulated)
        const int bpp = 4;
        const int tileWidth = 8;
        const int tileHeight = 8;
        const int maxColors = 16;
        const TileFormat format = TileFormat.Planar;
        const InterleaveMode interleave = InterleaveMode.Line;  // SMS uses line interleave!

        // Load image
        var reader = new PngReader();
        var image = reader.Load(assetPath);

        // Extract palette
        var paletteExtractor = new PaletteExtractor();
        var originalPalette = paletteExtractor.Extract(image, maxColors);

        // Ensure black is at index 0 for background fill
        var palette = new SKColor[originalPalette.Length];
        int blackIndex = -1;
        
        // Find black in original palette
        for (int i = 0; i < originalPalette.Length; i++)
        {
            if (originalPalette[i].Red == 0 && originalPalette[i].Green == 0 && originalPalette[i].Blue == 0)
            {
                blackIndex = i;
                break;
            }
        }

        // Build new palette with black at index 0
        if (blackIndex >= 0)
        {
            palette[0] = originalPalette[blackIndex];  // Black at 0
            int destIndex = 1;
            for (int i = 0; i < originalPalette.Length; i++)
            {
                if (i != blackIndex)
                {
                    palette[destIndex++] = originalPalette[i];
                }
            }
        }
        else
        {
            // No black found, use original palette
            palette = originalPalette;
        }

        // Slice into tiles (TileSlicer will remap pixels to new palette automatically)
        var slicer = new TileSlicer();
        var tiles = slicer.SliceIntoTiles(image, palette, tileWidth, tileHeight);

        // Convert each tile to SMS format
        var converter = new TileConverter();
        var allTilesData = new List<byte>();

        foreach (var tile in tiles)
        {
            var tileData = converter.Convert(tile, bpp, tileWidth, tileHeight, format, interleave);
            allTilesData.AddRange(tileData);
        }

        // Convert palette to SMS format (RGB222)
        var smspalette = palette.Select(c => 
        {
            int r = (c.Red * 3 + 127) / 255;
            int g = (c.Green * 3 + 127) / 255;
            int b = (c.Blue * 3 + 127) / 255;
            return r | (g << 2) | (b << 4);
        }).ToArray();

        return new Dictionary<string, object>
        {
            ["tilesArray"] = allTilesData.ToArray(),
            ["palette"] = smspalette,
            ["width"] = image.Width / tileWidth,
            ["height"] = image.Height / tileHeight,
            ["tileCount"] = tiles.Length
        };
    }
}
