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
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] ProcessTyped called");
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Input tiles: {input.Tiles.Length}");
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Input TilemapData.Length: {input.TilemapData.Length}");
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Map dimensions: {input.MapWidth}×{input.MapHeight} = {input.MapWidth * input.MapHeight}");
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Metadata keys: {string.Join(", ", input.Metadata.Keys)}");
        
        if (!input.IsValid(out var error))
        {
            throw new InvalidOperationException($"Invalid imported asset data: {error}");
        }

        // Extract required options
        if (options == null || !options.ContainsKey("project") || !options.ContainsKey("projectPath") || !options.ContainsKey("target"))
        {
            throw new ArgumentException("Missing required options: 'project', 'projectPath', and 'target'");
        }

        var project = (RetruxelProject)options["project"];
        var projectPath = (string)options["projectPath"];
        var target = (ITarget)options["target"];
        var targetId = target.TargetId;

        // Generate unique asset ID
        var assetId = GenerateUniqueAssetId(project, "livelink_tileset");

        // Save tiles as PNG asset
        var assetPath = SaveTilesAsAsset(input, project, projectPath, assetId, targetId, options);

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
        
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Asset dimensions: {asset.SourceWidth}×{asset.SourceHeight}");
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Asset tile count: {asset.TileCount}");

        project.Assets.Add(asset);

        // Map RGB palette to target hardware colors
        bool useLab = options.ContainsKey("useLab") && (bool)options["useLab"];
        uint[] mappedPalette = MapPaletteToTargetHardware(input.Palette, target, useLab);
        System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Palette mapped to {targetId.ToUpper()} hardware using {(useLab ? "LAB" : "RGB")} color space");

        // Return data for tilemap editor (keep as int[] in memory)
        return new Dictionary<string, object>
        {
            ["tilesAssetId"] = assetId,
            ["mapWidth"] = input.MapWidth,
            ["mapHeight"] = input.MapHeight,
            ["mapData"] = input.TilemapData.Select(x => (int)x).ToArray(),
            ["palette"] = mappedPalette,
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

    private string SaveTilesAsAsset(ImportedAssetData input, RetruxelProject project, string projectPath, string assetId, string targetId, Dictionary<string, object>? options = null)
    {
        // Check if we received an optimized bitmap to save directly (tileset-only mode)
        // First check options, then check metadata (passed from LiveLink)
        bool hasBitmap = (options?.ContainsKey("optimizedBitmap") == true) || 
                        (input.Metadata.ContainsKey("optimizedBitmap"));
        
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Checking for optimized bitmap...");
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] options is null: {options == null}");
        if (options != null)
        {
            System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] options.Keys: {string.Join(", ", options.Keys)}");
            System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Contains 'optimizedBitmap': {options.ContainsKey("optimizedBitmap")}");
        }
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] metadata.Keys: {string.Join(", ", input.Metadata.Keys)}");
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] hasBitmap: {hasBitmap}");
        
        if (hasBitmap)
        {
            // Get bitmap from options or metadata
            var optimizedBitmap = options?.ContainsKey("optimizedBitmap") == true
                ? (System.Windows.Media.Imaging.BitmapSource)options["optimizedBitmap"]
                : (System.Windows.Media.Imaging.BitmapSource)input.Metadata["optimizedBitmap"];
            
            var originalPalette = options?.ContainsKey("originalPalette") == true
                ? (uint[])options["originalPalette"]
                : input.Metadata.ContainsKey("originalPalette") ? (uint[])input.Metadata["originalPalette"] : null;
            
            System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Using optimized bitmap directly (tileset-only mode)");
            System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Bitmap size: {optimizedBitmap.PixelWidth}×{optimizedBitmap.PixelHeight}");
            
            return SaveBitmapDirectly(optimizedBitmap, originalPalette, project, projectPath, assetId, targetId);
        }
        
        // Original reconstruction logic for tilemap mode
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Using tile reconstruction (tilemap mode)");
        return SaveTilesReconstructed(input, project, projectPath, assetId, targetId, options);
    }
    
    /// <summary>
    /// Saves optimized bitmap directly as PNG (tileset-only mode).
    /// Creates 2 assets: original (RGB from emulator) + optimized (target hardware colors).
    /// </summary>
    private string SaveBitmapDirectly(System.Windows.Media.Imaging.BitmapSource optimizedBitmap, uint[]? originalPalette, RetruxelProject project, string projectPath, string assetId, string targetId)
    {
        var assetsDir = Path.Combine(projectPath, "assets", "graphics");
        Directory.CreateDirectory(assetsDir);
        
        // Save optimized version (target hardware colors)
        var optimizedPath = Path.Combine(assetsDir, $"{assetId}.png");
        using (var fileStream = new FileStream(optimizedPath, FileMode.Create))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(optimizedBitmap));
            encoder.Save(fileStream);
        }
        System.Diagnostics.Debug.WriteLine($"[SaveBitmapDirectly] Saved optimized PNG: {optimizedPath}");
        
        // TODO: Save original version with RGB palette from emulator
        // This would require reconstructing the bitmap with original palette
        // For now, we only save the optimized version
        
        return optimizedPath;
    }
    
    /// <summary>
    /// Reconstructs tiles from ImportedAssetData and saves as PNG (tilemap mode).
    /// </summary>
    private string SaveTilesReconstructed(ImportedAssetData input, RetruxelProject project, string projectPath, string assetId, string targetId, Dictionary<string, object>? options = null)
    {
        // Create assets directory if it doesn't exist
        var assetsDir = Path.Combine(projectPath, "assets", "graphics");
        Directory.CreateDirectory(assetsDir);

        var assetPath = Path.Combine(assetsDir, $"{assetId}.png");

        // Calculate tileset dimensions (arrange tiles in a grid)
        int tilesPerRow = CalculateTilesPerRow(input.Tiles.Length);
        int tilesetWidth = tilesPerRow * input.TileWidth;
        int tilesetHeight = ((input.Tiles.Length + tilesPerRow - 1) / tilesPerRow) * input.TileHeight;

        // Map RGB palette to target hardware colors
        bool useLab = options?.ContainsKey("useLab") == true && (bool)options["useLab"];
        var target = options?.ContainsKey("target") == true ? (ITarget)options["target"] : null;
        if (target == null)
        {
            throw new ArgumentException("Missing required option: 'target'");
        }
        uint[] finalPalette = MapPaletteToTargetHardware(input.Palette, target, useLab);
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Mapped {input.Palette.Length} RGB colors to {targetId.ToUpper()} hardware palette using {(useLab ? "LAB" : "RGB")} color space");

        // Create bitmap
        var bitmap = new WriteableBitmap(tilesetWidth, tilesetHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);

        // Extract Color Table from metadata if available (SG-1000)
        byte[]? colorTable = null;
        if (input.Metadata.ContainsKey("colorTable") && input.Metadata["colorTable"] is byte[] ct)
        {
            colorTable = ct;
            System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] Color Table found: {ct.Length} bytes");
            System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] First 16 bytes: {string.Join(" ", ct.Take(16).Select(b => b.ToString("X2")))}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ImportedAssetToTilemapPipeline] No Color Table in metadata");
        }

        // Draw tiles to bitmap
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Drawing {input.Tiles.Length} tiles to {tilesetWidth}×{tilesetHeight} bitmap");
        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] TilesPerRow: {tilesPerRow}");
        
        // LOCK BITMAP ONCE FOR ALL TILES
        bitmap.Lock();
        try
        {
            unsafe
            {
                var backBuffer = (byte*)bitmap.BackBuffer.ToPointer();
                int stride = bitmap.BackBufferStride;
                
                for (int tileIndex = 0; tileIndex < input.Tiles.Length; tileIndex++)
                {
                    int tileX = (tileIndex % tilesPerRow) * input.TileWidth;
                    int tileY = (tileIndex / tilesPerRow) * input.TileHeight;
                    
                    if (tileIndex >= 224 && tileIndex <= 287)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SaveTilesAsAsset] Tile {tileIndex}: drawing at ({tileX}, {tileY})");
                    }

                    DrawTileUnsafe(backBuffer, stride, input.Tiles[tileIndex], finalPalette, tileX, tileY, input.TileWidth, input.TileHeight, tileIndex, colorTable);
                }
            }
            
            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, tilesetWidth, tilesetHeight));
        }
        finally
        {
            bitmap.Unlock();
        }

        // Save bitmap as PNG
        using var fileStream = new FileStream(assetPath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(fileStream);

        return assetPath;
    }

    private unsafe void DrawTileUnsafe(byte* backBuffer, int stride, byte[] tilePixels, uint[] palette, int offsetX, int offsetY, int tileWidth, int tileHeight, int tileIndex, byte[]? colorTable)
    {
        // Debug first tile
        if (tileIndex == 0)
        {
            System.Diagnostics.Debug.WriteLine($"[DrawTileUnsafe] Tile 0:");
            System.Diagnostics.Debug.WriteLine($"  tilePixels.Length: {tilePixels.Length}");
            System.Diagnostics.Debug.WriteLine($"  palette.Length: {palette.Length}");
            System.Diagnostics.Debug.WriteLine($"  First 10 color indices: {string.Join(", ", tilePixels.Take(10))}");
            System.Diagnostics.Debug.WriteLine($"  Palette colors:");
            for (int i = 0; i < Math.Min(palette.Length, 10); i++)
            {
                uint c = palette[i];
                byte r = (byte)((c >> 16) & 0xFF);
                byte g = (byte)((c >> 8) & 0xFF);
                byte b = (byte)(c & 0xFF);
                System.Diagnostics.Debug.WriteLine($"    [{i}] = R={r}, G={g}, B={b}");
            }
        }
        
        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                int pixelIndex = y * tileWidth + x;
                if (pixelIndex >= tilePixels.Length) continue;

                byte colorIndex = tilePixels[pixelIndex];
                
                uint color;
                
                if (colorTable != null && tileIndex >= 0 && palette.Length == 16)
                {
                    int colorTableIndex = tileIndex / 8;
                    if (colorTableIndex < colorTable.Length)
                    {
                        byte colorByte = colorTable[colorTableIndex];
                        byte fgColor = (byte)((colorByte >> 4) & 0x0F);
                        byte bgColor = (byte)(colorByte & 0x0F);
                        byte paletteIndex = colorIndex == 0 ? bgColor : fgColor;
                        color = palette[paletteIndex];
                    }
                    else
                    {
                        color = colorIndex == 0 ? 0xFF000000 : 0xFFFFFFFF;
                    }
                }
                else if (palette.Length == 16 && colorIndex <= 1)
                {
                    color = colorIndex == 0 ? 0xFF000000 : 0xFFFFFFFF;
                }
                else
                {
                    if (colorIndex >= palette.Length)
                    {
                        // Out of range - use magenta for debugging
                        color = 0xFFFF00FF;
                    }
                    else
                    {
                        color = palette[colorIndex];
                    }
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

    private void DrawTileToBitmap(WriteableBitmap bitmap, byte[] tilePixels, uint[] palette, int offsetX, int offsetY, int tileWidth, int tileHeight, int tileIndex = -1, byte[]? colorTable = null)
    {
        bitmap.Lock();

        try
        {
            unsafe
            {
                var backBuffer = (byte*)bitmap.BackBuffer.ToPointer();
                int stride = bitmap.BackBufferStride;
                
                // Debug: Log stride and buffer info
                if (tileIndex >= 224 && tileIndex <= 287)
                {
                    System.Diagnostics.Debug.WriteLine($"[DrawTileToBitmap] Tile {tileIndex}: stride={stride}, bitmapSize={bitmap.PixelWidth}x{bitmap.PixelHeight}");
                }

                for (int y = 0; y < tileHeight; y++)
                {
                    for (int x = 0; x < tileWidth; x++)
                    {
                        int pixelIndex = y * tileWidth + x;
                        if (pixelIndex >= tilePixels.Length) continue;

                        byte colorIndex = tilePixels[pixelIndex];
                        
                        uint color;
                        
                        // SG-1000 with Color Table: Apply FG/BG colors from Color Table
                        if (colorTable != null && tileIndex >= 0 && palette.Length == 16)
                        {
                            // Color Table: Each byte controls 8 tiles
                            int colorTableIndex = tileIndex / 8;
                            if (colorTableIndex < colorTable.Length)
                            {
                                byte colorByte = colorTable[colorTableIndex];
                                byte fgColor = (byte)((colorByte >> 4) & 0x0F); // High 4 bits
                                byte bgColor = (byte)(colorByte & 0x0F);        // Low 4 bits
                                
                                if (x == 0 && y == 0) // Log once per tile
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Tile {tileIndex}] ColorTable[{colorTableIndex}]=0x{colorByte:X2} FG={fgColor} BG={bgColor}");
                                }
                                
                                // 1bpp: 0=background, 1=foreground
                                byte paletteIndex = colorIndex == 0 ? bgColor : fgColor;
                                color = palette[paletteIndex];
                            }
                            else
                            {
                                // Fallback: use black/white
                                color = colorIndex == 0 ? 0xFF000000 : 0xFFFFFFFF;
                            }
                        }
                        // Special handling for 1bpp without Color Table (temporary fix)
                        else if (palette.Length == 16 && colorIndex <= 1)
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
                        
                        // Debug: Log first pixel of tiles around line 8
                        if (tileIndex >= 224 && tileIndex <= 287 && x == 0 && y == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[DrawTileToBitmap] Tile {tileIndex}: first pixel at bitmapX={bitmapX}, bitmapY={bitmapY}, offset={offset}, color=#{r:X2}{g:X2}{b:X2}");
                        }

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

    /// <summary>
    /// Maps RGB palette to target hardware colors.
    /// Finds the closest hardware color for each RGB color.
    /// </summary>
    private uint[] MapPaletteToTargetHardware(uint[] rgbPalette, ITarget target, bool useLab = false)
    {
        // Get hardware palette from target
        var hardwareColors = target.GetHardwarePalette();
        
        if (hardwareColors == null || hardwareColors.Count == 0)
        {
            // Target doesn't have hardware palette restriction, return original
            System.Diagnostics.Debug.WriteLine($"[MapPaletteToTargetHardware] {target.TargetId} has no hardware palette, using RGB directly");
            return rgbPalette;
        }
        
        // Convert HardwareColor list to uint[] palette
        var hardwarePalette = hardwareColors.Select(c => (uint)((0xFF << 24) | (c.R << 16) | (c.G << 8) | c.B)).ToArray();
        
        var mappedPalette = new uint[rgbPalette.Length];
        
        for (int i = 0; i < rgbPalette.Length; i++)
        {
            uint rgbColor = rgbPalette[i];
            mappedPalette[i] = FindClosestHardwareColor(rgbColor, hardwarePalette, useLab);
        }
        
        return mappedPalette;
    }
    

    
    /// <summary>
    /// Finds the closest hardware color to a given RGB color.
    /// Uses LAB color space for perceptually accurate matching, or RGB Euclidean distance.
    /// </summary>
    private uint FindClosestHardwareColor(uint rgbColor, uint[] hardwarePalette, bool useLab = false)
    {
        byte r1 = (byte)((rgbColor >> 16) & 0xFF);
        byte g1 = (byte)((rgbColor >> 8) & 0xFF);
        byte b1 = (byte)(rgbColor & 0xFF);
        
        int closestIndex = 0;
        double minDistance = double.MaxValue;
        
        if (useLab)
        {
            // Convert source color to LAB
            var lab1 = RgbToLab(r1, g1, b1);
            
            for (int i = 0; i < hardwarePalette.Length; i++)
            {
                uint hwColor = hardwarePalette[i];
                byte r2 = (byte)((hwColor >> 16) & 0xFF);
                byte g2 = (byte)((hwColor >> 8) & 0xFF);
                byte b2 = (byte)(hwColor & 0xFF);
                
                // Convert hardware color to LAB
                var lab2 = RgbToLab(r2, g2, b2);
                
                // Delta E (CIE76) - perceptual distance in LAB space
                double dL = lab1.L - lab2.L;
                double dA = lab1.A - lab2.A;
                double dB = lab1.B - lab2.B;
                double distance = Math.Sqrt(dL * dL + dA * dA + dB * dB);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
        }
        else
        {
            // RGB Euclidean distance
            for (int i = 0; i < hardwarePalette.Length; i++)
            {
                uint hwColor = hardwarePalette[i];
                byte r2 = (byte)((hwColor >> 16) & 0xFF);
                byte g2 = (byte)((hwColor >> 8) & 0xFF);
                byte b2 = (byte)(hwColor & 0xFF);
                
                int dr = r1 - r2;
                int dg = g1 - g2;
                int db = b1 - b2;
                double distance = Math.Sqrt(dr * dr + dg * dg + db * db);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }
            }
        }
        
        return hardwarePalette[closestIndex];
    }
    
    /// <summary>
    /// Converts RGB to LAB color space for perceptually accurate color matching.
    /// </summary>
    private (double L, double A, double B) RgbToLab(byte r, byte g, byte b)
    {
        // Step 1: RGB to XYZ
        double rLinear = RgbToLinear(r / 255.0);
        double gLinear = RgbToLinear(g / 255.0);
        double bLinear = RgbToLinear(b / 255.0);
        
        // D65 illuminant matrix
        double x = rLinear * 0.4124564 + gLinear * 0.3575761 + bLinear * 0.1804375;
        double y = rLinear * 0.2126729 + gLinear * 0.7151522 + bLinear * 0.0721750;
        double z = rLinear * 0.0193339 + gLinear * 0.1191920 + bLinear * 0.9503041;
        
        // Step 2: XYZ to LAB (D65 reference white)
        const double xn = 0.95047;
        const double yn = 1.00000;
        const double zn = 1.08883;
        
        double fx = LabF(x / xn);
        double fy = LabF(y / yn);
        double fz = LabF(z / zn);
        
        double L = 116.0 * fy - 16.0;
        double A = 500.0 * (fx - fy);
        double B = 200.0 * (fy - fz);
        
        return (L, A, B);
    }
    
    /// <summary>
    /// Converts sRGB gamma-corrected value to linear RGB.
    /// </summary>
    private double RgbToLinear(double c)
    {
        if (c <= 0.04045)
            return c / 12.92;
        else
            return Math.Pow((c + 0.055) / 1.055, 2.4);
    }
    
    /// <summary>
    /// LAB color space conversion function.
    /// </summary>
    private double LabF(double t)
    {
        const double delta = 6.0 / 29.0;
        const double delta3 = delta * delta * delta;
        
        if (t > delta3)
            return Math.Pow(t, 1.0 / 3.0);
        else
            return t / (3.0 * delta * delta) + 4.0 / 29.0;
    }
}
