using Retruxel.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    /// <summary>
    /// Draws hardware constraint violation overlays on SceneCanvas after RefreshPreview().
    /// Currently detects sprites-per-scanline violations (SMS: max 8 sprites per scanline).
    /// Each violating entity gets a semi-transparent orange border.
    /// </summary>
    internal void DrawConstraintOverlays()
    {
        if (_project is null || _target is null || _currentScene is null) return;

        var violations = _target.GetLiveDiagnostics(new LiveDiagnosticInput
        {
            Scene   = _currentScene,
            Project = _project,
            Specs   = _target.Specs
        });

        // Only draw overlays when there are sprite-related violations
        var spriteViolations = violations
            .Where(v => v.Severity == DiagnosticSeverity.Error &&
                        v.Category == "Sprites")
            .ToList();

        if (spriteViolations.Count == 0) return;

        // For scanline overflow: highlight all entities with a warning border
        // since we can't know at edit-time which specific scanlines overflow
        DrawSpriteOverflowOverlays();
    }

    private void DrawSpriteOverflowOverlays()
    {
        if (_project is null || _target is null || _currentScene is null) return;

        int tileSize    = _target.Specs.TileWidth;
        int maxPerLine  = _target.Specs.SpritesPerScanline;
        if (maxPerLine <= 0) return;

        // Build scanline occupancy: scanlineY (pixel) → list of entity indices
        var scanlineOccupancy = new Dictionary<int, List<int>>();

        var entityList = _currentScene.Entities
            .Where(e => e.Visible)
            .ToList();

        for (int ei = 0; ei < entityList.Count; ei++)
        {
            var entity  = entityList[ei];
            var prefab  = _project.Prefabs.FirstOrDefault(p => p.PrefabId == entity.PrefabId);
            int h       = (prefab?.HeightTiles ?? entity.HeightTiles ?? 2) * tileSize;
            int startY  = entity.StartTileY * tileSize;

            for (int py = startY; py < startY + h; py++)
            {
                if (!scanlineOccupancy.ContainsKey(py))
                    scanlineOccupancy[py] = [];
                scanlineOccupancy[py].Add(ei);
            }
        }

        // Find which entity indices appear on overloaded scanlines
        var overloadedEntities = new HashSet<int>();
        foreach (var (_, indices) in scanlineOccupancy)
        {
            if (indices.Count > maxPerLine)
                foreach (var idx in indices)
                    overloadedEntities.Add(idx);
        }

        if (overloadedEntities.Count == 0) return;

        // Draw orange warning border over each overloaded entity
        foreach (var idx in overloadedEntities)
        {
            var entity  = entityList[idx];
            var prefab  = _project.Prefabs.FirstOrDefault(p => p.PrefabId == entity.PrefabId);
            int w       = (prefab?.WidthTiles  ?? entity.WidthTiles  ?? 2) * tileSize;
            int h       = (prefab?.HeightTiles ?? entity.HeightTiles ?? 2) * tileSize;
            int x       = entity.StartTileX * tileSize;
            int y       = entity.StartTileY * tileSize;

            var overlay = new Rectangle
            {
                Width            = w,
                Height           = h,
                Fill             = new SolidColorBrush(Color.FromArgb(60, 255, 165, 0)),
                Stroke           = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                StrokeThickness  = 2,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(overlay, x);
            Canvas.SetTop(overlay,  y);
            SceneCanvas.Children.Add(overlay);

            // Label
            var label = new TextBlock
            {
                Text             = $"FLICKER",
                Foreground       = new SolidColorBrush(Color.FromRgb(255, 165, 0)),
                Background       = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                FontSize         = 8,
                Padding          = new Thickness(2, 1, 2, 1),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(label, x + 2);
            Canvas.SetTop(label,  y + 2);
            SceneCanvas.Children.Add(label);
        }
    }
}
