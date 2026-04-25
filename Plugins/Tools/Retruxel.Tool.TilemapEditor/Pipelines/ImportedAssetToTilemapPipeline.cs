using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.IO;
using System.Windows.Media.Imaging;

namespace Retruxel.Tool.TilemapEditor.Pipelines;

/// <summary>
/// Converts ImportedAssetData to Tilemap Editor format.
/// Saves tiles as asset and prepares tilemap data for editor.
/// </summary>
public class ImportedAssetToTilemapPipeline : AssetPipelineBase<ImportedAssetData, Dictionary<string, object>>
{
    public override string PipelineId => "imported_to_tilemap_editor";
    public override string DisplayName => "Imported Asset → Tilemap Editor";
    public override string Description => "Converts imported asset data to tilemap editor format";

    public override Dictionary<string, object> ProcessTyped(ImportedAssetData input, Dictionary<string, object>? options = null)
    {
        if (!input.IsValid(out var error))
        {
            throw new InvalidOperationException($"Invalid imported asset data: {error}");
        }

        // Extract required options
        if (options == null || !options.ContainsKey("project") || !options.ContainsKey("projectPath"))
        {
            throw new ArgumentException("Missing required options: 'project' and 'projectPath'");
        }

        var project = (RetruxelProject)options["project"];
        var projectPath = (string)options["projectPath"];
        var targetId = options.ContainsKey("targetId") ? (string)options["targetId"] : input.SourceTargetId;

        // Generate unique asset ID
        var assetId = GenerateUniqueAssetId(project, "livelink_tileset");

        // Save tiles as PNG asset
        var assetPath = SaveTilesAsAsset(input, project, projectPath, assetId, targetId);

        // Create asset entry
        var asset = new AssetEntry
        {
            Id = assetId,
            FileName = Path.GetFileName(assetPath),
            RelativePath = Path.GetRelativePath(projectPath, assetPath).Replace('\\', '/'),
            VramRegionId = "background",
            SourceWidth = input.Tiles.Length > 0 ? CalculateTilesetWidth(input.Tiles.Length, input.TileWidth) : 0,
            SourceHeight = input.Tiles.Length > 0 ? CalculateTilesetHeight(input.Tiles.Length, input.TileWidth, input.TileHeight) : 0,
            TileCount = input.Tiles.Length
        };

        project.Assets.Add(asset);

        // Return data for tilemap editor
        return new Dictionary<string, object>
        {
            ["tilesAssetId"] = assetId,
            ["mapWidth"] = input.MapWidth,
            ["mapHeight"] = input.MapHeight,
            ["mapData"] = input.TilemapData,
            ["palette"] = input.Palette,
            ["asset"] = asset
        };
    }

    private string GenerateUniqueAssetId(RetruxelProject project, string baseName)
    {
        var existingIds = project.Assets.Select(a => a.Id).ToHashSet();
        var assetId = baseName;
        int suffix = 1;

        while (existingIds.Contains(assetId))
        {
            assetId = $"{baseName}_{suffix}";
            suffix++;
        }

        return assetId;
    }

    private string SaveTilesAsAsset(ImportedAssetData input, RetruxelProject project, string projectPath, string assetId, string targetId)
    {
        // Create assets directory if it doesn't exist
        var assetsDir = Path.Combine(projectPath, "assets", "graphics");
        Directory.CreateDirectory(assetsDir);

        var assetPath = Path.Combine(assetsDir, $"{assetId}.png");

        // Calculate tileset dimensions (arrange tiles in a grid)
        int tilesPerRow = CalculateTilesPerRow(input.Tiles.Length);
        int tilesetWidth = tilesPerRow * input.TileWidth;
        int tilesetHeight = ((input.Tiles.Length + tilesPerRow - 1) / tilesPerRow) * input.TileHeight;

        // Create bitmap
        var bitmap = new WriteableBitmap(tilesetWidth, tilesetHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

        // Draw tiles to bitmap
        for (int tileIndex = 0; tileIndex < input.Tiles.Length; tileIndex++)
        {
            int tileX = (tileIndex % tilesPerRow) * input.TileWidth;
            int tileY = (tileIndex / tilesPerRow) * input.TileHeight;

            DrawTileToBitmap(bitmap, input.Tiles[tileIndex], input.Palette, tileX, tileY, input.TileWidth, input.TileHeight);
        }

        // Save bitmap as PNG
        using var fileStream = new FileStream(assetPath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(fileStream);

        return assetPath;
    }

    private void DrawTileToBitmap(WriteableBitmap bitmap, byte[] tilePixels, uint[] palette, int offsetX, int offsetY, int tileWidth, int tileHeight)
    {
        bitmap.Lock();

        try
        {
            unsafe
            {
                var backBuffer = (byte*)bitmap.BackBuffer.ToPointer();
                int stride = bitmap.BackBufferStride;

                for (int y = 0; y < tileHeight; y++)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        int pixelIndex = y * tileWidth + x;
                        if (pixelIndex >= tilePixels.Length) continue;

                        byte colorIndex = tilePixels[pixelIndex];
                        
                        // Special handling for 1bpp (SG-1000): use white/black instead of palette
                        // TODO: Implement proper Color Table reading for SG-1000
                        uint color;
                        if (palette.Length == 16 && colorIndex <= 1)
                        {
                            // 1bpp: 0=black, 1=white (temporary fix until Color Table is implemented)
                            color = colorIndex == 0 ? 0xFF000000 : 0xFFFFFFFF;
                        }
                        else
                        {
                            if (colorIndex >= palette.Length) continue;
                            color = palette[colorIndex];
                        }
                        
                        byte a = (byte)((color >> 24) & 0xFF);
                        byte r = (byte)((color >> 16) & 0xFF);
                        byte g = (byte)((color >> 8) & 0xFF);
                        byte b = (byte)(color & 0xFF);

                        int bitmapX = offsetX + x;
                        int bitmapY = offsetY + y;
                        int offset = bitmapY * stride + bitmapX * 4;

                        backBuffer[offset] = b;
                        backBuffer[offset + 1] = g;
                        backBuffer[offset + 2] = r;
                        backBuffer[offset + 3] = a;
                    }
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(offsetX, offsetY, tileWidth, tileHeight));
        }
        finally
        {
            bitmap.Unlock();
        }
    }

    private int CalculateTilesPerRow(int tileCount)
    {
        // Arrange tiles in a square-ish grid (prefer 16 tiles per row for SMS)
        if (tileCount <= 16) return tileCount;
        if (tileCount <= 256) return 16;
        return 32;
    }

    private int CalculateTilesetWidth(int tileCount, int tileWidth)
    {
        return CalculateTilesPerRow(tileCount) * tileWidth;
    }

    private int CalculateTilesetHeight(int tileCount, int tileWidth, int tileHeight)
    {
        int tilesPerRow = CalculateTilesPerRow(tileCount);
        int rows = (tileCount + tilesPerRow - 1) / tilesPerRow;
        return rows * tileHeight;
    }
}
