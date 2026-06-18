using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.TilemapEditor;

public partial class TilemapEditorWindow
{
    // Set of tile IDs (indices into the tileset) that are marked as solid/collision.
    // SMS convention: collision is per tile ID, not per map position.
    private readonly HashSet<int> _solidTileIds = new();

    private bool _isCollisionMode => ChkShowCollision?.IsChecked == true;

    // ── Overlay rendering ──────────────────────────────────────────────────────

    /// <summary>
    /// Draws semi-transparent overlays on the canvas for every map cell
    /// whose tile ID is in _solidTileIds. Called at the end of RenderCanvas().
    /// </summary>
    internal void DrawCollisionOverlay()
    {
        if (!_isCollisionMode || _solidTileIds.Count == 0) return;

        int tileSize = _target.Specs.TileWidth;
        double scaledTileSize = tileSize * _canvasZoom;

        if (_planeData.LayerCount <= _currentLayerIndex) return;

        foreach (var (x, y, entry) in _planeData.GetLayerSparse(_currentLayerIndex))
        {
            if (entry.IsEmpty || !_solidTileIds.Contains(entry.TileIndex)) continue;

            var overlay = new Rectangle
            {
                Width            = scaledTileSize,
                Height           = scaledTileSize,
                Fill             = new SolidColorBrush(Color.FromArgb(120, 220, 50, 50)),
                Stroke           = new SolidColorBrush(Color.FromRgb(220, 50, 50)),
                StrokeThickness  = 1,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(overlay, x * scaledTileSize);
            Canvas.SetTop(overlay,  y * scaledTileSize);
            PlaneCanvas.Children.Add(overlay);
        }

        // Label in top-left corner when collision mode is active
        var label = new TextBlock
        {
            Text             = $"COLLISION MODE — {_solidTileIds.Count} solid tile(s)",
            Foreground       = new SolidColorBrush(Color.FromRgb(220, 50, 50)),
            Background       = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            FontSize         = 10,
            Padding          = new Thickness(6, 3, 6, 3),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(label, 4);
        Canvas.SetTop(label,  4);
        PlaneCanvas.Children.Add(label);
    }

    // ── Tileset interaction in collision mode ──────────────────────────────────

    /// <summary>
    /// Called from TilesetCanvas_MouseLeftButtonDown when collision mode is active.
    /// Toggles the clicked tile ID as solid/not-solid.
    /// </summary>
    internal bool TryHandleCollisionTilesetClick(int tileId)
    {
        if (!_isCollisionMode) return false;

        if (_solidTileIds.Contains(tileId))
            _solidTileIds.Remove(tileId);
        else
            _solidTileIds.Add(tileId);

        RefreshTilesetCollisionOverlay();
        RenderCanvas();
        return true;
    }

    /// <summary>
    /// Redraws red markers on the tileset panel for all solid tile IDs.
    /// </summary>
    internal void RefreshTilesetCollisionOverlay()
    {
        if (TilesetCanvas == null) return;

        // Remove previous collision markers (tagged rectangles)
        var toRemove = TilesetCanvas.Children
            .OfType<Rectangle>()
            .Where(r => r.Tag as string == "collision")
            .ToList();
        foreach (var r in toRemove)
            TilesetCanvas.Children.Remove(r);

        if (!_isCollisionMode || _solidTileIds.Count == 0) return;

        int tileSize  = _target.Specs.TileWidth;
        double scaled = tileSize * _tileZoomLevel;
        int columns   = GetTilesetColumns();
        if (columns <= 0) return;

        foreach (var tileId in _solidTileIds)
        {
            int col = tileId % columns;
            int row = tileId / columns;

            var marker = new Rectangle
            {
                Width            = scaled,
                Height           = scaled,
                Fill             = new SolidColorBrush(Color.FromArgb(100, 220, 50, 50)),
                Stroke           = new SolidColorBrush(Color.FromRgb(220, 50, 50)),
                StrokeThickness  = 2,
                IsHitTestVisible = false,
                Tag              = "collision"
            };
            Canvas.SetLeft(marker, col * scaled);
            Canvas.SetTop(marker,  row * scaled);
            TilesetCanvas.Children.Add(marker);
        }
    }

    // ── Persistence ────────────────────────────────────────────────────────────

    internal int[] GetSolidTileIds() => _solidTileIds.OrderBy(x => x).ToArray();

    internal void LoadSolidTileIds(IEnumerable<int> ids)
    {
        _solidTileIds.Clear();
        foreach (var id in ids)
            _solidTileIds.Add(id);
    }
}
