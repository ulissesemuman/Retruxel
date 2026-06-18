using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    /// <summary>
    /// Rebuilds the passive hardware usage panel from a live VramAllocator dry-run.
    /// Called after any project mutation (tree rebuild, property change, asset import).
    /// </summary>
    internal void RefreshDiagnostics()
    {
        if (DiagnosticsPanel is null || _project is null || _target is null || _currentScene is null)
            return;

        DiagnosticsPanel.Children.Clear();

        try
        {
            var report = VramAllocator.Analyze(
                _currentScene,
                _target,
                _project.Assets,
                fontTileCount: 0,
                project: _project);

            AddUsageBar(
                "VRAM Tiles",
                report.TotalBytesUsed / (_target.Specs.Planes.FirstOrDefault()?.BytesPerTile ?? 32),
                report.TotalBytesAvailable / (_target.Specs.Planes.FirstOrDefault()?.BytesPerTile ?? 32),
                "tiles");

            AddUsageBar(
                "VRAM Bytes",
                report.TotalBytesUsed,
                report.TotalBytesAvailable,
                "bytes");

            // Per-plane breakdown
            foreach (var plane in report.Planes)
            {
                if (plane.BytesUsed == 0) continue;
                var bytesPerTile = plane.BytesPerTile > 0 ? plane.BytesPerTile : 32;
                AddUsageBar(
                    $"  {plane.PlaneLabel}",
                    plane.BytesUsed / bytesPerTile,
                    report.TotalBytesAvailable / bytesPerTile,
                    "tiles",
                    isSubItem: true);
            }

            // Sprite count
            var spriteCount = _currentScene.Entities.Count;
            var maxSprites  = _target.Specs.MaxSpritesOnScreen;
            if (maxSprites > 0)
                AddUsageBar("Sprites", spriteCount, maxSprites, "sprites");
        }
        catch
        {
            // Silent — diagnostics are best-effort, never crash the editor
        }
    }

    private void AddUsageBar(
        string label,
        int current,
        int max,
        string unit,
        bool isSubItem = false)
    {
        if (max <= 0) return;

        var ratio = Math.Min(1.0, (double)current / max);

        var severity = ratio switch
        {
            >= 1.0  => DiagnosticSeverity.Error,
            >= 0.8  => DiagnosticSeverity.Warning,
            _       => DiagnosticSeverity.Info
        };

        var fillColor = severity switch
        {
            DiagnosticSeverity.Error   => TryFindBrush("BrushError")   ?? Brushes.Red,
            DiagnosticSeverity.Warning => TryFindBrush("BrushWarning") ?? Brushes.Orange,
            _                          => TryFindBrush("BrushPrimary") ?? Brushes.Green
        };

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        // Label row: name + value
        var labelRow = new Grid();
        labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text       = label,
            Style      = TryFindStyle("TextLabel"),
            Foreground = isSubItem
                ? TryFindBrush("BrushOnSurfaceVariant") ?? Brushes.Gray
                : TryFindBrush("BrushOnSurface")        ?? Brushes.White,
            FontSize   = isSubItem ? 10 : 11
        };
        Grid.SetColumn(nameBlock, 0);
        labelRow.Children.Add(nameBlock);

        var valueBlock = new TextBlock
        {
            Text       = $"{current} / {max} {unit}",
            Style      = TryFindStyle("TextLabel"),
            Foreground = severity == DiagnosticSeverity.Info
                ? TryFindBrush("BrushOnSurfaceVariant") ?? Brushes.Gray
                : fillColor,
            FontSize   = 10
        };
        Grid.SetColumn(valueBlock, 1);
        labelRow.Children.Add(valueBlock);

        panel.Children.Add(labelRow);

        // Progress bar
        var barContainer = new Grid { Height = isSubItem ? 4 : 6, Margin = new Thickness(0, 2, 0, 0) };

        var bg = new Border
        {
            Background = TryFindBrush("BrushSurfaceContainerHighest") ?? new SolidColorBrush(Color.FromRgb(40, 40, 40))
        };
        barContainer.Children.Add(bg);

        var fill = new Border
        {
            Background          = fillColor,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width               = 0
        };
        barContainer.SizeChanged += (_, e) =>
            fill.Width = Math.Max(0, ratio * e.NewSize.Width);

        barContainer.Children.Add(fill);
        panel.Children.Add(barContainer);

        DiagnosticsPanel.Children.Add(panel);
    }

    private Brush? TryFindBrush(string key)
    {
        try { return FindResource(key) as Brush; }
        catch { return null; }
    }

    private Style? TryFindStyle(string key)
    {
        try { return FindResource(key) as Style; }
        catch { return null; }
    }
}
