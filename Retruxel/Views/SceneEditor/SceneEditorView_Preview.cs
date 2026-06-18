using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Retruxel.Lib.ImageProcessing;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    // ── Zoom button ───────────────────────────────────────────────────────────

    private void BtnPreviewZoom_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = BtnPreviewZoom, Placement = PlacementMode.Bottom };

        void Item(string label, Action action)
        {
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }

        Item("25%",  () => SetPreviewZoom(0.25));
        Item("50%",  () => SetPreviewZoom(0.50));
        Item("100%", () => SetPreviewZoom(1.00));
        Item("200%", () => SetPreviewZoom(2.00));
        Item("400%", () => SetPreviewZoom(4.00));
        menu.Items.Add(new Separator());
        Item("Zoom In",       () => StepPreviewZoom(+1));
        Item("Zoom Out",      () => StepPreviewZoom(-1));
        Item("Reset (100%)",  () => SetPreviewZoom(1.0));
        Item("Fit to Window", FitPreviewToWindow);

        menu.IsOpen = true;
    }

    // ── Zoom helpers ──────────────────────────────────────────────────────────

    private void SetPreviewZoom(double zoom)
    {
        _previewZoom = Math.Clamp(zoom, PreviewZoomMin, PreviewZoomMax);
        UpdateZoomLabel();
        ApplyPreviewTransform();
    }

    private void StepPreviewZoom(int direction)
    {
        var next = direction > 0 ? _previewZoom * PreviewZoomStep : _previewZoom / PreviewZoomStep;
        SetPreviewZoom(next);
    }

    private void FitPreviewToWindow()
    {
        if (_target is null) return;

        double availW = PreviewHost.ActualWidth;
        double availH = PreviewHost.ActualHeight;
        if (availW <= 0 || availH <= 0) return;

        double fitZoom  = Math.Min(availW / _target.Specs.ScreenWidth,
                                   availH / _target.Specs.ScreenHeight);
        _previewZoom    = Math.Clamp(fitZoom * 0.9, PreviewZoomMin, PreviewZoomMax);
        _previewOffsetX = (availW - _target.Specs.ScreenWidth  * _previewZoom) / 2;
        _previewOffsetY = (availH - _target.Specs.ScreenHeight * _previewZoom) / 2;

        UpdateZoomLabel();
        ApplyPreviewTransform();
    }

    private void UpdateZoomLabel()
    {
        BtnPreviewZoom.Content = $"{_previewZoom * 100:F0}%";
    }

    // ── Transform ─────────────────────────────────────────────────────────────

    private void ApplyPreviewTransform()
    {
        var transform = new TransformGroup();
        transform.Children.Add(new ScaleTransform(_previewZoom, _previewZoom));
        transform.Children.Add(new TranslateTransform(_previewOffsetX, _previewOffsetY));
        PreviewCanvas.RenderTransform = transform;

        if (_target is not null)
        {
            PreviewCanvas.Width  = _target.Specs.ScreenWidth;
            PreviewCanvas.Height = _target.Specs.ScreenHeight;
            SceneCanvas.Width    = _target.Specs.ScreenWidth;
            SceneCanvas.Height   = _target.Specs.ScreenHeight;
        }
    }

    // ── Mouse: wheel ──────────────────────────────────────────────────────────

    private void PreviewHost_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            var mouseInHost = e.GetPosition(PreviewHost);
            double newZoom  = e.Delta > 0
                ? _previewZoom * PreviewZoomStep
                : _previewZoom / PreviewZoomStep;
            newZoom = Math.Clamp(newZoom, PreviewZoomMin, PreviewZoomMax);

            double ratio     = newZoom / _previewZoom;
            _previewOffsetX  = mouseInHost.X - (mouseInHost.X - _previewOffsetX) * ratio;
            _previewOffsetY  = mouseInHost.Y - (mouseInHost.Y - _previewOffsetY) * ratio;
            _previewZoom     = newZoom;

            UpdateZoomLabel();
            ApplyPreviewTransform();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            _previewOffsetX += e.Delta * 0.5;
            ApplyPreviewTransform();
            e.Handled = true;
        }
        else
        {
            _previewOffsetY += e.Delta * 0.5;
            ApplyPreviewTransform();
            e.Handled = true;
        }
    }

    // ── Mouse: middle button pan ──────────────────────────────────────────────

    private void PreviewHost_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            _isPanning       = true;
            _panStartMouse   = e.GetPosition(PreviewHost);
            _panStartOffsetX = _previewOffsetX;
            _panStartOffsetY = _previewOffsetY;
            PreviewHost.CaptureMouse();
            PreviewHost.Cursor = Cursors.SizeAll;
            e.Handled = true;
        }
    }

    private void PreviewHost_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle && _isPanning)
        {
            _isPanning = false;
            PreviewHost.ReleaseMouseCapture();
            PreviewHost.Cursor = Cursors.Arrow;
            e.Handled = true;
        }
    }

    private void PreviewHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { }
    private void PreviewHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }

    private void PreviewHost_MouseMove(object sender, MouseEventArgs e)
    {
        TxtCoordinates.Text = $"X: {(int)e.GetPosition(SceneCanvas).X} | Y: {(int)e.GetPosition(SceneCanvas).Y}";

        if (_isPanning)
        {
            var cur         = e.GetPosition(PreviewHost);
            _previewOffsetX = _panStartOffsetX + (cur.X - _panStartMouse.X);
            _previewOffsetY = _panStartOffsetY + (cur.Y - _panStartMouse.Y);
            ApplyPreviewTransform();
        }
    }

    // ── Keyboard shortcuts ────────────────────────────────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.OemPlus  || e.Key == Key.Add)      { StepPreviewZoom(+1); e.Handled = true; }
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)  { StepPreviewZoom(-1); e.Handled = true; }
            if (e.Key == Key.D0       || e.Key == Key.NumPad0)   { SetPreviewZoom(1.0); e.Handled = true; }
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            if (e.Key == Key.D0 || e.Key == Key.NumPad0) { FitPreviewToWindow(); e.Handled = true; }
        }
    }

    // ── Scene preview refresh ─────────────────────────────────────────────────

    internal void RefreshPreview()
    {
        SceneCanvas.Children.Clear();

        if (_project is null || _target is null || _currentScene is null)
        {
            PreviewEmptyState.Visibility = Visibility.Visible;
            return;
        }

        // Determine whether there is anything renderable in the scene.
        // A layer counts only if it has an asset assigned; an entity counts
        // only if it has a sprite asset assigned.
        bool hasRenderableContent =
            _currentScene.Planes.Any(p => p.Layers.Any(l => l.Visible && !string.IsNullOrEmpty(l.AssetId))) ||
            _currentScene.Entities.Any(e =>
            {
                var prefab = _project.Prefabs.FirstOrDefault(p => p.PrefabId == e.PrefabId);
                var sprite = prefab?.SpriteAssetId ?? e.SpriteAssetId ?? string.Empty;
                return !string.IsNullOrEmpty(sprite);
            });

        PreviewEmptyState.Visibility = hasRenderableContent ? Visibility.Collapsed : Visibility.Visible;

        foreach (var plane in _currentScene.Planes)
        {
            var visibleLayers = plane.Layers.Where(l => l.Visible).ToList();
            if (visibleLayers.Count == 0) continue;
            var visual = TryRenderPlane(plane, visibleLayers);
            if (visual is not null)
                SceneCanvas.Children.Add(visual);
        }

        foreach (var entity in _currentScene.Entities)
        {
            if (!entity.Visible) continue;
            var visual = TryRenderEntitySprite(entity);
            if (visual is not null)
            {
                Canvas.SetLeft(visual, entity.StartTileX * _target.Specs.TileWidth);
                Canvas.SetTop (visual, entity.StartTileY * _target.Specs.TileHeight);
                SceneCanvas.Children.Add(visual);
            }
        }

        var boundary = new Rectangle
        {
            Width            = _target.Specs.ScreenWidth,
            Height           = _target.Specs.ScreenHeight,
            Stroke           = new SolidColorBrush(Color.FromArgb(80, 0xFF, 0x4D, 0x4D)),
            StrokeThickness  = 1,
            Fill             = Brushes.Transparent,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(boundary, 0);
        Canvas.SetTop (boundary, 0);
        SceneCanvas.Children.Add(boundary);

        DrawConstraintOverlays();
    }

    // ── Plane layer rendering ───────────────────────────────────────────────

    // ── Plane rendering (merged layers) ────────────────────────────────────────────

    /// <summary>
    /// Merges all visible layers of a hardware plane into a single bitmap.
    /// Upper layers (higher index) take priority — empty tiles (tileIndex &lt; 0)
    /// are transparent, showing the layer below.
    /// </summary>
    private UIElement? TryRenderPlane(PlaneData plane, List<PlaneLayerData> visibleLayers)
    {
        if (_project is null || _target is null || _currentScene is null) return null;

        var paletteSlot   = _currentScene.PaletteSlots.Count > plane.PaletteSlot
            ? _currentScene.PaletteSlots[plane.PaletteSlot]
            : _currentScene.PaletteSlots.FirstOrDefault();
        var paletteColors = paletteSlot?.Colors ?? [];

        var planeSpecs = _target.Specs.Planes.FirstOrDefault(p => p.Id == plane.PlaneId);
        int tileSize   = _target.Specs.TileWidth;

        // Use dimensions from the first layer with content
        var baseLayer = visibleLayers.FirstOrDefault(l => l.Width > 0) ?? visibleLayers[0];
        int mapWidth  = baseLayer.Width  > 0 ? baseLayer.Width  : planeSpecs?.DefaultWidth  ?? 32;
        int mapHeight = baseLayer.Height > 0 ? baseLayer.Height : planeSpecs?.DefaultHeight ?? 28;
        int outW      = mapWidth  * tileSize;
        int outH      = mapHeight * tileSize;

        try
        {
            using var output = new SkiaSharp.SKBitmap(outW, outH,
                SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
            using var canvas = new SkiaSharp.SKCanvas(output);
            canvas.Clear(SkiaSharp.SKColors.Transparent);

            // Render bottom layer first, then upper layers on top
            foreach (var layer in visibleLayers)
            {
                if (string.IsNullOrEmpty(layer.AssetId) || layer.Tiles.Count == 0) continue;

                var asset = _project.Assets.FirstOrDefault(a => a.Id == layer.AssetId);
                if (asset?.GenerationParams?.MapIndex is not { Length: > 0 } mapIndex) continue;

                int assetW = asset.GenerationParams.OptimizedWidth;
                int assetH = asset.GenerationParams.OptimizedHeight;
                if (assetW <= 0 || assetH <= 0) continue;

                int tilesetColumns = Math.Max(1, assetW / tileSize);

                using var tilesetBitmap = RenderMapIndexToBitmap(mapIndex, paletteColors, assetW, assetH);

                for (int i = 0; i < layer.Tiles.Count && i < mapWidth * mapHeight; i++)
                {
                    var entry = layer.Tiles[i];
                    if (entry.TileIndex < 0) continue;  // transparent — show layer below

                    int destX = (i % mapWidth) * tileSize;
                    int destY = (i / mapWidth) * tileSize;
                    int srcX  = (entry.TileIndex % tilesetColumns) * tileSize;
                    int srcY  = (entry.TileIndex / tilesetColumns) * tileSize;

                    if (srcX + tileSize > assetW || srcY + tileSize > assetH) continue;

                    var src  = new SkiaSharp.SKRect(srcX, srcY, srcX + tileSize, srcY + tileSize);
                    var dest = new SkiaSharp.SKRect(destX, destY, destX + tileSize, destY + tileSize);

                    if (!entry.FlipH && !entry.FlipV && entry.Rotation == 0)
                    {
                        canvas.DrawBitmap(tilesetBitmap, src, dest);
                    }
                    else
                    {
                        using var tile = new SkiaSharp.SKBitmap(tileSize, tileSize,
                            SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
                        using (var tc = new SkiaSharp.SKCanvas(tile))
                            tc.DrawBitmap(tilesetBitmap, src, new SkiaSharp.SKRect(0, 0, tileSize, tileSize));
                        using var transformed = TileTransformation.ApplyTileTransform(
                            tile, entry.FlipH, entry.FlipV, entry.Rotation, tileSize);
                        canvas.DrawBitmap(transformed, new SkiaSharp.SKRect(0, 0, tileSize, tileSize), dest);
                    }
                }
            }

            var wb = new WriteableBitmap(outW, outH, 96, 96, PixelFormats.Bgra32, null);
            wb.Lock();
            unsafe
            {
                var src = (byte*)output.GetPixels().ToPointer();
                var dst = (byte*)wb.BackBuffer.ToPointer();
                int srcStride = output.RowBytes;
                int dstStride = wb.BackBufferStride;
                if (srcStride == dstStride)
                    Buffer.MemoryCopy(src, dst, dstStride * outH, srcStride * outH);
                else
                    for (int row = 0; row < outH; row++)
                        Buffer.MemoryCopy(src + row * srcStride, dst + row * dstStride, dstStride, outW * 4);
            }
            wb.AddDirtyRect(new Int32Rect(0, 0, outW, outH));
            wb.Unlock();
            wb.Freeze();

            var img = new Image
            {
                Source              = wb,
                Width               = outW,
                Height              = outH,
                Stretch             = Stretch.None,
                SnapsToDevicePixels = true,
                IsHitTestVisible    = false
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            Canvas.SetLeft(img, 0);
            Canvas.SetTop(img, 0);
            return img;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Preview] Exception rendering plane '{plane.PlaneId}': {ex}");
            return null;
        }
    }

    private UIElement? TryRenderPlaneLayer(PlaneData plane, PlaneLayerData layer)
    {
        if (_project is null || _target is null) return null;
        if (string.IsNullOrEmpty(layer.AssetId) || layer.Tiles.Count == 0) return null;

        var asset = _project.Assets.FirstOrDefault(a => a.Id == layer.AssetId);
        if (asset?.GenerationParams is null) return null;

        var mapIndex = asset.GenerationParams.MapIndex;
        if (mapIndex is null || mapIndex.Length == 0) return null;

        // Resolve palette: use the plane's palette slot from the current scene.
        var paletteSlot = _currentScene!.PaletteSlots.Count > plane.PaletteSlot
            ? _currentScene.PaletteSlots[plane.PaletteSlot]
            : _currentScene.PaletteSlots.FirstOrDefault();

        var paletteColors = paletteSlot?.Colors ?? [];

        // Resolve PlaneSpecs for this specific plane
        var planeSpecs = _target.Specs.Planes.FirstOrDefault(p => p.Id == plane.PlaneId);
        int mapWidth   = layer.Width  > 0 ? layer.Width  : planeSpecs?.DefaultWidth  ?? 32;
        int mapHeight  = layer.Height > 0 ? layer.Height : planeSpecs?.DefaultHeight ?? 28;
        int tileSize   = _target.Specs.TileWidth;

        int assetW = asset.GenerationParams.OptimizedWidth;
        int assetH = asset.GenerationParams.OptimizedHeight;
        if (assetW <= 0 || assetH <= 0) return null;

        int tilesetColumns = Math.Max(1, assetW / tileSize);

        System.Diagnostics.Debug.WriteLine(
            $"[Preview] Rendering '{layer.LayerName}' — {mapWidth}×{mapHeight} tiles, " +
            $"asset {assetW}×{assetH}px, {tilesetColumns} cols, palette {paletteColors.Count} colors");

        try
        {
            // Build the full tileset SKBitmap from MapIndex + palette (same as TilemapEditor).
            using var tilesetBitmap = RenderMapIndexToBitmap(mapIndex, paletteColors, assetW, assetH);

            // Compose the scene layer by drawing each tile onto an output bitmap.
            int outW = mapWidth  * tileSize;
            int outH = mapHeight * tileSize;

            using var output = new SkiaSharp.SKBitmap(outW, outH,
                SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
            using var canvas = new SkiaSharp.SKCanvas(output);
            canvas.Clear(SkiaSharp.SKColors.Transparent);

            for (int i = 0; i < layer.Tiles.Count && i < mapWidth * mapHeight; i++)
            {
                var entry = layer.Tiles[i];
                if (entry.TileIndex < 0) continue;

                int tileId = entry.TileIndex;
                int destX  = (i % mapWidth) * tileSize;
                int destY  = (i / mapWidth) * tileSize;
                int srcX   = (tileId % tilesetColumns) * tileSize;
                int srcY   = (tileId / tilesetColumns) * tileSize;

                if (srcX + tileSize > assetW || srcY + tileSize > assetH) continue;

                var src  = new SkiaSharp.SKRect(srcX, srcY, srcX + tileSize, srcY + tileSize);
                var dest = new SkiaSharp.SKRect(destX, destY, destX + tileSize, destY + tileSize);

                if (!entry.FlipH && !entry.FlipV && entry.Rotation == 0)
                {
                    canvas.DrawBitmap(tilesetBitmap, src, dest);
                }
                else
                {
                    // Extract tile, apply transform, draw at destination
                    using var tile = new SkiaSharp.SKBitmap(tileSize, tileSize,
                        SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
                    using (var tc = new SkiaSharp.SKCanvas(tile))
                        tc.DrawBitmap(tilesetBitmap, src, new SkiaSharp.SKRect(0, 0, tileSize, tileSize));

                    using var transformed = TileTransformation.ApplyTileTransform(tile, entry.FlipH, entry.FlipV, entry.Rotation, tileSize);
                    canvas.DrawBitmap(transformed, new SkiaSharp.SKRect(0, 0, tileSize, tileSize), dest);
                }
            }

            // Convert SKBitmap → WPF WriteableBitmap (inline, no plugin dependency).
            var wb = new WriteableBitmap(outW, outH, 96, 96, PixelFormats.Bgra32, null);
            wb.Lock();
            unsafe
            {
                var src = (byte*)output.GetPixels().ToPointer();
                var dst = (byte*)wb.BackBuffer.ToPointer();
                int srcStride = output.RowBytes;
                int dstStride = wb.BackBufferStride;
                if (srcStride == dstStride)
                    Buffer.MemoryCopy(src, dst, dstStride * outH, srcStride * outH);
                else
                    for (int row = 0; row < outH; row++)
                        Buffer.MemoryCopy(src + row * srcStride, dst + row * dstStride, dstStride, outW * 4);
            }
            wb.AddDirtyRect(new Int32Rect(0, 0, outW, outH));
            wb.Unlock();
            wb.Freeze();

            var img = new Image
            {
                Source              = wb,
                Width               = outW,
                Height              = outH,
                Stretch             = Stretch.None,
                SnapsToDevicePixels = true,
                IsHitTestVisible    = false
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            Canvas.SetLeft(img, 0);
            Canvas.SetTop (img, 0);
            return img;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Preview] Exception rendering layer '{layer.LayerName}': {ex}");
            return null;
        }
    }

    /// <summary>
    /// Renders a MapIndex byte array to an SKBitmap using the given palette hex colors.
    /// Replicates IndexedBitmapRenderer.Render without depending on the plugin assembly.
    /// </summary>
    private static SkiaSharp.SKBitmap RenderMapIndexToBitmap(
        byte[] mapIndex, IReadOnlyList<string> paletteHex, int width, int height)
    {
        // Parse hex palette → ARGB uint array for fast lookup.
        var palette = new uint[Math.Max(1, paletteHex.Count)];
        for (int i = 0; i < paletteHex.Count; i++)
        {
            var hex = paletteHex[i].TrimStart('#');
            if (hex.Length >= 6)
            {
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                // BGRA layout for SKColorType.Bgra8888
                palette[i] = (uint)((255u << 24) | ((uint)r << 16) | ((uint)g << 8) | b);
            }
        }

        var bitmap = new SkiaSharp.SKBitmap(width, height,
            SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);

        int paletteMax = palette.Length - 1;
        unsafe
        {
            var ptr = (uint*)bitmap.GetPixels().ToPointer();
            int total = width * height;
            for (int i = 0; i < total; i++)
            {
                int idx = Math.Min(mapIndex[i], paletteMax);
                ptr[i] = palette[idx];
            }
        }

        return bitmap;
    }

    // ── Entity sprite rendering ───────────────────────────────────────────────

    /// <summary>
    /// Renders an entity sprite using the MapIndex pipeline.
    /// Draws exactly widthTiles × heightTiles tiles from the asset, row-major,
    /// matching the same layout the entity codegen uses at runtime.
    /// </summary>
    private UIElement? TryRenderEntitySprite(EntityData entity)
    {
        if (_project is null || _target is null || _currentScene is null) return null;

        // Resolve sprite properties from Prefab, falling back to legacy nullable fields
        var prefab = _project.Prefabs.FirstOrDefault(p => p.PrefabId == entity.PrefabId);
        var spriteAssetId = prefab?.SpriteAssetId ?? entity.SpriteAssetId ?? string.Empty;
        var paletteSlotIndex = prefab?.PaletteSlot ?? entity.PaletteSlot ?? 1;
        var widthTilesVal  = prefab?.WidthTiles  ?? entity.WidthTiles  ?? 2;
        var heightTilesVal = prefab?.HeightTiles ?? entity.HeightTiles ?? 2;

        if (string.IsNullOrEmpty(spriteAssetId)) return null;

        var asset = _project.Assets.FirstOrDefault(a => a.Id == spriteAssetId);
        if (asset?.GenerationParams?.MapIndex is not { Length: > 0 } mapIndex) return null;

        int assetW = asset.GenerationParams.OptimizedWidth;
        int assetH = asset.GenerationParams.OptimizedHeight;
        if (assetW <= 0 || assetH <= 0) return null;

        var paletteSlot = _currentScene.PaletteSlots.Count > paletteSlotIndex
            ? _currentScene.PaletteSlots[paletteSlotIndex]
            : _currentScene.PaletteSlots.FirstOrDefault();
        var paletteColors = paletteSlot?.Colors ?? [];

        int tileSize    = _target.Specs.TileWidth;
        int assetCols   = Math.Max(1, assetW / tileSize);
        int assetRows   = Math.Max(1, assetH / tileSize);
        int widthTiles  = widthTilesVal  > 0 ? widthTilesVal  : assetCols;
        int heightTiles = heightTilesVal > 0 ? heightTilesVal : assetRows;

        // When widthTiles doesn't match assetCols the row-major tile mapping breaks —
        // tile indices land on the wrong source pixels and the sprite renders as garbage.
        // If there's a mismatch, fall back to the actual asset grid dimensions so the
        // preview always shows the correct sprite (even if the prefab config is wrong).
        if (widthTiles != assetCols)
            widthTiles = assetCols;
        if (heightTiles != assetRows)
            heightTiles = assetRows;
        int outW        = widthTiles  * tileSize;
        int outH        = heightTiles * tileSize;

        try
        {
            using var tilesetBitmap = RenderMapIndexToBitmap(mapIndex, paletteColors, assetW, assetH);
            using var output = new SkiaSharp.SKBitmap(outW, outH,
                SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Premul);
            using var skCanvas = new SkiaSharp.SKCanvas(output);
            skCanvas.Clear(SkiaSharp.SKColors.Transparent);

            // Draw widthTiles × heightTiles tiles, row-major, same as the codegen loop:
            //   tile_idx = row * widthTiles + col  →  VRAM sequential
            //   source position in asset: (tile_idx % assetCols, tile_idx / assetCols)
            for (int row = 0; row < heightTiles; row++)
            {
                for (int col = 0; col < widthTiles; col++)
                {
                    int tileIdx = row * widthTiles + col;
                    int srcX    = (tileIdx % assetCols) * tileSize;
                    int srcY    = (tileIdx / assetCols) * tileSize;
                    if (srcX + tileSize > assetW || srcY + tileSize > assetH) continue;

                    var src  = new SkiaSharp.SKRect(srcX, srcY, srcX + tileSize, srcY + tileSize);
                    var dest = new SkiaSharp.SKRect(
                        col * tileSize, row * tileSize,
                        (col + 1) * tileSize, (row + 1) * tileSize);
                    skCanvas.DrawBitmap(tilesetBitmap, src, dest);
                }
            }

            var wb = new WriteableBitmap(outW, outH, 96, 96, PixelFormats.Bgra32, null);
            wb.Lock();
            unsafe
            {
                var src = (byte*)output.GetPixels().ToPointer();
                var dst = (byte*)wb.BackBuffer.ToPointer();
                int srcStride = output.RowBytes;
                int dstStride = wb.BackBufferStride;
                if (srcStride == dstStride)
                    Buffer.MemoryCopy(src, dst, dstStride * outH, srcStride * outH);
                else
                    for (int r = 0; r < outH; r++)
                        Buffer.MemoryCopy(src + r * srcStride, dst + r * dstStride, dstStride, outW * 4);
            }
            wb.AddDirtyRect(new Int32Rect(0, 0, outW, outH));
            wb.Unlock();
            wb.Freeze();

            var img = new Image
            {
                Source              = wb,
                Width               = outW,
                Height              = outH,
                Stretch             = Stretch.None,
                SnapsToDevicePixels = true,
                IsHitTestVisible    = false
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            return img;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Preview] Entity '{entity.Label}': {ex.Message}");
            return null;
        }
    }
}
