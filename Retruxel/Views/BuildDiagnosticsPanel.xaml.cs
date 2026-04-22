using Retruxel.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

/// <summary>
/// Displays build diagnostics metrics with progress bars colored by severity.
/// UI is generated entirely in code-behind following Neo-Technical Archive design system.
/// </summary>
public partial class BuildDiagnosticsPanel : UserControl
{
    private BuildDiagnosticsReport? _report;

    public BuildDiagnosticsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the diagnostics report and rebuilds the UI.
    /// Pass null to hide the panel.
    /// </summary>
    public BuildDiagnosticsReport? Report
    {
        set
        {
            _report = value;
            RebuildUI();
        }
    }

    private void RebuildUI()
    {
        Content = null;

        if (_report is null)
        {
            Visibility = Visibility.Collapsed;
            return;
        }

        Visibility = Visibility.Visible;

        var root = new StackPanel
        {
            Margin = new Thickness(16, 8, 16, 16)
        };

        // Title
        var title = new TextBlock
        {
            Text = "BUILD DIAGNOSTICS",
            Style = (Style)FindResource("TextLabelCaps"),
            Foreground = (Brush)FindResource("BrushPrimary"),
            Margin = new Thickness(0, 0, 0, 16)
        };
        root.Children.Add(title);

        // Render each category
        foreach (var (category, metrics) in _report.ByCategory)
        {
            // Category label
            var categoryLabel = new TextBlock
            {
                Text = category.ToUpperInvariant(),
                Style = (Style)FindResource("TextLabelCaps"),
                Foreground = (Brush)FindResource("BrushPrimary"),
                Margin = new Thickness(0, 8, 0, 8)
            };
            root.Children.Add(categoryLabel);

            // Metrics in this category
            foreach (var metric in metrics)
            {
                var metricPanel = CreateMetricPanel(metric);
                root.Children.Add(metricPanel);
            }
        }

        Content = root;
    }

    private StackPanel CreateMetricPanel(BuildDiagnosticMetric metric)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        // Metric name
        var nameLabel = new TextBlock
        {
            Text = metric.DisplayName,
            Style = (Style)FindResource("TextLabel"),
            Foreground = (Brush)FindResource("BrushOnSurface"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        panel.Children.Add(nameLabel);

        // Progress bar container
        var progressContainer = new Grid
        {
            Height = 24,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // Background bar
        var background = new Border
        {
            Background = (Brush)FindResource("BrushSurfaceContainerHighest"),
            CornerRadius = new CornerRadius(0)
        };
        progressContainer.Children.Add(background);

        // Fill bar (colored by severity)
        var fillBrush = metric.Severity switch
        {
            DiagnosticSeverity.Error => (Brush)FindResource("BrushError"),
            DiagnosticSeverity.Warning => (Brush)FindResource("BrushWarning"),
            _ => (Brush)FindResource("BrushPrimary")
        };

        var fill = new Border
        {
            Background = fillBrush,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = Math.Max(0, Math.Min(1.0, metric.UsageRatio)) * progressContainer.ActualWidth,
            CornerRadius = new CornerRadius(0)
        };

        // Update fill width when container size changes
        progressContainer.SizeChanged += (s, e) =>
        {
            fill.Width = Math.Max(0, Math.Min(1.0, metric.UsageRatio)) * e.NewSize.Width;
        };

        progressContainer.Children.Add(fill);

        panel.Children.Add(progressContainer);

        // Value label: "Current / Max"
        var valueLabel = new TextBlock
        {
            Text = $"{metric.Current} / {metric.Max}",
            Style = (Style)FindResource("TextLabel"),
            Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        panel.Children.Add(valueLabel);

        // Detail (if present)
        if (!string.IsNullOrEmpty(metric.Detail))
        {
            var detailLabel = new TextBlock
            {
                Text = metric.Detail,
                Style = (Style)FindResource("TextLabel"),
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(detailLabel);
        }

        return panel;
    }
}
