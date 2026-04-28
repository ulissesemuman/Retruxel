using System.Windows;

namespace Retruxel.Views.SceneEditor.Helpers;

/// <summary>
/// Manages canvas transformation state (pan and zoom).
/// </summary>
public class SceneCanvasTransform
{
    public double PanOffsetX { get; set; }
    public double PanOffsetY { get; set; }
    public double ZoomLevel { get; set; } = 1.0;
    
    public const double MinZoom = 0.25;
    public const double MaxZoom = 4.0;
    public const double ZoomStep = 0.1;

    /// <summary>
    /// Applies zoom centered on a specific point.
    /// </summary>
    public void ZoomAt(Point center, double delta)
    {
        var oldZoom = ZoomLevel;
        ZoomLevel = Math.Clamp(ZoomLevel + delta, MinZoom, MaxZoom);
        
        // Adjust pan to keep zoom centered on cursor
        var zoomRatio = ZoomLevel / oldZoom;
        PanOffsetX = center.X - (center.X - PanOffsetX) * zoomRatio;
        PanOffsetY = center.Y - (center.Y - PanOffsetY) * zoomRatio;
    }

    /// <summary>
    /// Converts screen coordinates to canvas coordinates.
    /// </summary>
    public Point ScreenToCanvas(Point screenPoint)
    {
        return new Point(
            (screenPoint.X - PanOffsetX) / ZoomLevel,
            (screenPoint.Y - PanOffsetY) / ZoomLevel
        );
    }

    /// <summary>
    /// Converts canvas coordinates to screen coordinates.
    /// </summary>
    public Point CanvasToScreen(Point canvasPoint)
    {
        return new Point(
            canvasPoint.X * ZoomLevel + PanOffsetX,
            canvasPoint.Y * ZoomLevel + PanOffsetY
        );
    }

    /// <summary>
    /// Resets transform to default state.
    /// </summary>
    public void Reset()
    {
        PanOffsetX = 0;
        PanOffsetY = 0;
        ZoomLevel = 1.0;
    }
}
