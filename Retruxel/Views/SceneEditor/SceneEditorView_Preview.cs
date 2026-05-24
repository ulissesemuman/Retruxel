using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

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
            _currentScene.Entities.Any(e => !string.IsNullOrEmpty(e.SpriteAssetId));

        PreviewEmptyState.Visibility = hasRenderableContent ? Visibility.Collapsed : Visibility.Visible;

        foreach (var plane in _currentScene.Planes)
        {
            foreach (var layer in plane.Layers)
            {
                if (!layer.Visible) continue;
                var visual = TryRenderPlaneLayer(plane, layer);
                if (visual is not null)
                    SceneCanvas.Children.Add(visual);
            }
        }

        foreach (var entity in _currentScene.Entities)
        {
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
    }

    // ── Plane layer rendering ───────────────────────────────────────────────

    private UIElement? TryRenderPlaneLayer(PlaneData plane, PlaneLayerData layer)
    {
        if (_project is null || _target is null) return null;
        if (string.IsNullOrEmpty(layer.AssetId) || layer.Tiles.Count == 0) return null;

        // Resolve PlaneSpecs for this specific plane
        var planeSpecs = _target.Specs.Planes.FirstOrDefault(p => p.Id == plane.PlaneId);
        int mapWidth   = layer.Width  > 0 ? layer.Width  : planeSpecs?.DefaultWidth  ?? 32;
        int mapHeight  = layer.Height > 0 ? layer.Height : planeSpecs?.DefaultHeight ?? 28;

        try
        {
            var asset = _project.Assets.FirstOrDefault(a => a.Id == layer.AssetId);
            if (asset is null) return null;

            var absPath = IOPath.Combine(_project.ProjectPath,
                asset.RelativePath.Replace('/', IOPath.DirectorySeparatorChar));
            if (!IOFile.Exists(absPath)) return null;

            var tileset = new BitmapImage();
            tileset.BeginInit();
            tileset.UriSource   = new Uri(absPath);
            tileset.CacheOption = BitmapCacheOption.OnLoad;
            tileset.EndInit();
            tileset.Freeze();

            int tileSize       = _target.Specs.TileWidth;
            int tilesetColumns = Math.Max(1, tileset.PixelWidth / tileSize);

            var rtb = new RenderTargetBitmap(
                mapWidth * tileSize, mapHeight * tileSize, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();

            using (var dc = dv.RenderOpen())
            {
                for (int i = 0; i < layer.Tiles.Count; i++)
                {
                    int tileId = layer.Tiles[i].TileIndex;
                    if (tileId < 0) continue;

                    int x    = i % mapWidth;
                    int y    = i / mapWidth;
                    int srcX = (tileId % tilesetColumns) * tileSize;
                    int srcY = (tileId / tilesetColumns) * tileSize;

                    if (srcX + tileSize > tileset.PixelWidth ||
                        srcY + tileSize > tileset.PixelHeight) continue;

                    var cropped = new CroppedBitmap(tileset,
                        new Int32Rect(srcX, srcY, tileSize, tileSize));
                    dc.DrawImage(cropped, new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
                }
            }

            rtb.Render(dv);
            rtb.Freeze();

            var img = new Image
            {
                Source              = rtb,
                Width               = mapWidth * tileSize,
                Height              = mapHeight * tileSize,
                Stretch             = Stretch.None,
                SnapsToDevicePixels = true,
                IsHitTestVisible    = false
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            Canvas.SetLeft(img, 0);
            Canvas.SetTop (img, 0);
            return img;
        }
        catch { return null; }
    }

    // ── Entity sprite rendering ───────────────────────────────────────────────

    private UIElement? TryRenderEntitySprite(EntityData entity)
    {
        if (_project is null || string.IsNullOrEmpty(entity.SpriteAssetId)) return null;

        try
        {
            var asset = _project.Assets.FirstOrDefault(a => a.Id == entity.SpriteAssetId);
            if (asset is null) return null;

            var absPath = IOPath.Combine(_project.ProjectPath,
                asset.RelativePath.Replace('/', IOPath.DirectorySeparatorChar));
            if (!IOFile.Exists(absPath)) return null;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource   = new Uri(absPath);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            var img = new Image
            {
                Source              = bmp,
                Width               = asset.SourceWidth,
                Height              = asset.SourceHeight,
                Stretch             = Stretch.None,
                SnapsToDevicePixels = true,
                IsHitTestVisible    = false
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            return img;
        }
        catch { return null; }
    }
}
