using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    /// <summary>
    /// Rebuilds the passive hardware usage panel.
    /// Combines VramAllocator dry-run data with ITarget.GetLiveDiagnostics().
    /// Called after any project mutation via StateChanged.
    /// </summary>
    internal void RefreshDiagnostics()
    {
        if (DiagnosticsPanel is null || _project is null || _target is null || _currentScene is null)
            return;

        DiagnosticsPanel.Children.Clear();

        // Scene label header
        DiagnosticsPanel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text       = $"SCENE: {_currentScene.SceneName.ToUpperInvariant()}",
            FontSize   = 9,
            FontFamily = new System.Windows.Media.FontFamily("Space Grotesk, Segoe UI, sans-serif"),
            Foreground = TryFindBrush("BrushPrimary") ?? System.Windows.Media.Brushes.Green,
            Margin     = new System.Windows.Thickness(0, 0, 0, 8)
        });

        try
        {
            // ── VRAM (VramAllocator dry-run) ──────────────────────────────────
            var vramReport = VramAllocator.Analyze(
                _currentScene, _target, _project.Assets,
                fontTileCount: 0, project: _project);

            int bytesPerTile = _target.Specs.Planes.FirstOrDefault()?.BytesPerTile ?? 32;
            int maxTiles     = _target.Specs.VramBytesForTiles / bytesPerTile;
            int usedTiles    = vramReport.TotalBytesUsed / bytesPerTile;

            AddCategoryHeader("VRAM");
            AddUsageBar("Tiles", usedTiles, maxTiles, "tiles");
            AddUsageBar("Bytes", vramReport.TotalBytesUsed, vramReport.TotalBytesAvailable, "bytes");

            foreach (var plane in vramReport.Planes.Where(p => p.BytesUsed > 0))
                AddUsageBar($"  {plane.PlaneLabel}", plane.BytesUsed / bytesPerTile, maxTiles, "tiles", isSubItem: true);

            // ── Live target metrics (palette, SAT, etc.) ──────────────────────
            var liveInput = new LiveDiagnosticInput
            {
                Scene    = _currentScene,
                Project  = _project,
                Specs    = _target.Specs,
                VramUsage = vramReport
            };

            var liveMetrics = _target.GetLiveDiagnostics(liveInput);
            if (liveMetrics.Count > 0)
            {
                string? lastCategory = null;
                foreach (var metric in liveMetrics)
                {
                    if (metric.Category != lastCategory)
                    {
                        AddCategoryHeader(metric.Category);
                        lastCategory = metric.Category;
                    }
                    AddUsageBarFromMetric(metric);
                }
            }
        }
        catch
        {
            // Silent — diagnostics are best-effort, never crash the editor
        }
    }

    // ── Rendering helpers ──────────────────────────────────────────────────────

    private void AddCategoryHeader(string text)
    {
        DiagnosticsPanel.Children.Add(new TextBlock
        {
            Text       = text.ToUpperInvariant(),
            FontSize   = 9,
            Foreground = TryFindBrush("BrushOnSurfaceVariant") ?? Brushes.Gray,
            Margin     = new Thickness(0, 8, 0, 4),
            FontFamily = new FontFamily("Inter, Segoe UI, sans-serif")
        });
    }

    private void AddUsageBarFromMetric(LiveDiagnosticMetric metric)
        => AddUsageBar(metric.Label, metric.Current, metric.Max, metric.Unit,
            warningThreshold: metric.WarningThreshold,
            errorThreshold: metric.ErrorThreshold);

    private void AddUsageBar(
        string label,
        int current,
        int max,
        string unit,
        bool isSubItem = false,
        double warningThreshold = 0.8,
        double errorThreshold = 1.0)
    {
        if (max <= 0) return;

        var ratio = Math.Min(1.0, (double)current / max);

        var severity = ratio switch
        {
            var r when r >= errorThreshold   => DiagnosticSeverity.Error,
            var r when r >= warningThreshold => DiagnosticSeverity.Warning,
            _                                => DiagnosticSeverity.Info
        };

        var fillColor = severity switch
        {
            DiagnosticSeverity.Error   => TryFindBrush("BrushError")   ?? Brushes.Red,
            DiagnosticSeverity.Warning => TryFindBrush("BrushWarning") ?? Brushes.Orange,
            _                          => TryFindBrush("BrushPrimary") ?? Brushes.Green
        };

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };

        // Label row
        var labelRow = new Grid();
        labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        labelRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var nameBlock = new TextBlock
        {
            Text       = label,
            FontSize   = isSubItem ? 9 : 10,
            Foreground = isSubItem
                ? TryFindBrush("BrushOnSurfaceVariant") ?? Brushes.Gray
                : TryFindBrush("BrushOnSurface") ?? Brushes.White,
            FontFamily = new FontFamily("Inter, Segoe UI, sans-serif")
        };
        Grid.SetColumn(nameBlock, 0);
        labelRow.Children.Add(nameBlock);

        var valueBlock = new TextBlock
        {
            Text       = $"{current}/{max} {unit}",
            FontSize   = 9,
            Foreground = severity == DiagnosticSeverity.Info
                ? TryFindBrush("BrushOnSurfaceVariant") ?? Brushes.Gray
                : fillColor,
            FontFamily = new FontFamily("Inter, Segoe UI, sans-serif")
        };
        Grid.SetColumn(valueBlock, 1);
        labelRow.Children.Add(valueBlock);

        panel.Children.Add(labelRow);

        // Progress bar
        var barContainer = new Grid { Height = isSubItem ? 3 : 5, Margin = new Thickness(0, 2, 0, 0) };

        barContainer.Children.Add(new Border
        {
            Background = TryFindBrush("BrushSurfaceContainerHighest")
                         ?? new SolidColorBrush(Color.FromRgb(40, 40, 40))
        });

        var fill = new Border { Background = fillColor, HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
        barContainer.SizeChanged += (_, e) => fill.Width = Math.Max(0, ratio * e.NewSize.Width);
        barContainer.Children.Add(fill);

        panel.Children.Add(barContainer);
        DiagnosticsPanel.Children.Add(panel);
    }

    private Brush? TryFindBrush(string key)
    {
        try { return FindResource(key) as Brush; }
        catch { return null; }
    }
}
