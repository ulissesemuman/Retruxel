using Retruxel.Core.Interfaces;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Retruxel.Views;

/// <summary>
/// Partial class handling visual rendering — builds Image controls from assets,
/// renders tilemaps by assembling tiles, and extracts asset references from modules.
/// </summary>
public partial class SceneEditorView
{
    /// <summary>
    /// Tries to build an Image visual from the asset referenced by the module.
    /// Returns null if the module has no asset reference or the asset file is missing —
    /// the caller falls back to the gray box label in that case.
    /// 
    /// For tilemap modules, renders the actual tile map instead of showing the raw asset.
    /// </summary>
    private UIElement? TryBuildAssetVisual(SceneElement element)
    {
        if (_project is null || element.Module is not IModule module) return null;

        // Special handling for tilemaps — render the actual map
        if (element.ModuleId.Contains("tilemap", StringComparison.OrdinalIgnoreCase))
            return TryBuildTilemapVisual(element);

        // Extract tilesAssetId from the module JSON
        var assetId = ExtractAssetId(module);
        if (string.IsNullOrEmpty(assetId)) return null;

        // Find asset entry in project
        var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
        if (asset is null) return null;

        // Resolve absolute path
        var absPath = Path.Combine(_project.ProjectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absPath)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(absPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            var img = new Image
            {
                Source = bitmap,
                Width = asset.SourceWidth,
                Height = asset.SourceHeight,
                Stretch = Stretch.None,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            return img;
        }
        catch
        {
            return null; // Any I/O or decode failure → fall back to gray box
        }
    }

    /// <summary>
    /// Renders a tilemap module by assembling tiles from the tileset.
    /// Returns null if tilemap data or asset is missing.
    /// </summary>
    private UIElement? TryBuildTilemapVisual(SceneElement element)
    {
        if (_project is null || _target is null || element.Module is not IModule module) return null;

        try
        {
            var json = module.Serialize();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract tilemap properties - try both naming conventions
            if (!root.TryGetProperty("tilesAssetId", out var assetIdProp)) return null;
            var assetId = assetIdProp.GetString();
            if (string.IsNullOrEmpty(assetId)) return null;
            
            int width, height;
            if (root.TryGetProperty("mapWidth", out var mapWidthProp))
                width = mapWidthProp.GetInt32();
            else if (root.TryGetProperty("width", out var widthProp))
                width = widthProp.GetInt32();
            else
                return null;

            if (root.TryGetProperty("mapHeight", out var mapHeightProp))
                height = mapHeightProp.GetInt32();
            else if (root.TryGetProperty("height", out var heightProp))
                height = heightProp.GetInt32();
            else
                return null;

            string? dataBase64 = null;
            int[]? mapDataArray = null;
            
            if (root.TryGetProperty("mapData", out var mapDataProp))
            {
                if (mapDataProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // int[] format (TilemapModule)
                    mapDataArray = mapDataProp.EnumerateArray().Select(e => e.GetInt32()).ToArray();
                }
                else if (mapDataProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    // Base64 string format (legacy)
                    dataBase64 = mapDataProp.GetString();
                }
            }
            else if (root.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                dataBase64 = dataProp.GetString();
            }
            
            byte[] tileData;
            if (mapDataArray != null)
            {
                // Convert int[] to byte[]
                tileData = new byte[mapDataArray.Length];
                for (int i = 0; i < mapDataArray.Length; i++)
                {
                    tileData[i] = mapDataArray[i] < 0 ? (byte)255 : (byte)Math.Min(254, mapDataArray[i]);
                }
            }
            else if (!string.IsNullOrEmpty(dataBase64))
            {
                // Decode Base64
                tileData = Convert.FromBase64String(dataBase64);
            }
            else
            {
                return null;
            }

            // Find asset
            var asset = _project.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset is null) return null;

            var absPath = Path.Combine(_project.ProjectPath, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absPath)) return null;

            // Load tileset
            var tileset = new BitmapImage();
            tileset.BeginInit();
            tileset.UriSource = new Uri(absPath);
            tileset.CacheOption = BitmapCacheOption.OnLoad;
            tileset.EndInit();
            tileset.Freeze();

            var tileSize = _target.Specs.TileWidth;
            var tilesetColumns = asset.SourceWidth / tileSize;

            // Create render target
            var renderWidth = width * tileSize;
            var renderHeight = height * tileSize;
            var renderTarget = new RenderTargetBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        if (index >= tileData.Length) continue;

                        int tileId = tileData[index];
                        // 255 is empty marker, skip it
                        if (tileId < 0 || tileId == 255) continue;

                        // Calculate source rect in tileset
                        int srcX = (tileId % tilesetColumns) * tileSize;
                        int srcY = (tileId / tilesetColumns) * tileSize;

                        var sourceRect = new Int32Rect(srcX, srcY, tileSize, tileSize);
                        var croppedTile = new CroppedBitmap(tileset, sourceRect);

                        // Draw tile at position
                        var destRect = new Rect(x * tileSize, y * tileSize, tileSize, tileSize);
                        dc.DrawImage(croppedTile, destRect);
                    }
                }
            }

            renderTarget.Render(drawingVisual);
            renderTarget.Freeze();

            var img = new Image
            {
                Source = renderTarget,
                Width = renderWidth,
                Height = renderHeight,
                Stretch = Stretch.None,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            return img;
        }
        catch
        {
            return null; // Any error → fall back to gray box
        }
    }

    /// <summary>
    /// Extracts the tilesAssetId from a module's serialized JSON.
    /// Works for TilemapModule and SpriteModule which both use "tilesAssetId".
    /// </summary>
    private static string? ExtractAssetId(IModule module)
    {
        try
        {
            var json = module.Serialize();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("tilesAssetId", out var prop))
                return prop.GetString();
        }
        catch { /* ignore */ }

        return null;
    }
}
