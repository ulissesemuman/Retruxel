namespace Retruxel.Core.Models;

/// <summary>
/// Severity level for a build diagnostic metric.
/// </summary>
public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Represents a single hardware usage metric measured during a build.
/// Severity and UsageRatio are calculated by the Core — targets cannot set them directly.
/// </summary>
public class BuildDiagnosticMetric
{
    /// <summary>Unique identifier for this metric. Ex: "vram.tiles", "ram.usage", "sprites.scanline"</summary>
    public string MetricId { get; }

    /// <summary>Label displayed in the UI. Ex: "VRAM Tiles", "RAM Usage", "Sprites / Scanline"</summary>
    public string DisplayName { get; }

    /// <summary>Category for grouping. Ex: "VRAM", "RAM", "Sprites", "ROM"</summary>
    public string Category { get; }

    /// <summary>Value measured in the current build</summary>
    public int Current { get; }

    /// <summary>Maximum limit declared by the target (hard limit)</summary>
    public int Max { get; }

    /// <summary>
    /// Threshold percentage at which severity becomes Warning (0.0–1.0).
    /// Declared by target — default suggested: 0.8
    /// </summary>
    public double WarningThreshold { get; }

    /// <summary>
    /// Threshold percentage at which severity becomes Error (0.0–1.0).
    /// Declared by target — default suggested: 1.0
    /// </summary>
    public double ErrorThreshold { get; }

    /// <summary>
    /// Calculated by Core — never by target.
    /// Info if Current/Max &lt; WarningThreshold
    /// Warning if &gt;= WarningThreshold and &lt; ErrorThreshold
    /// Error if &gt;= ErrorThreshold
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>Calculated by Core: (double)Current / Max</summary>
    public double UsageRatio { get; }

    /// <summary>Optional additional message from target. Ex: "Exceeded scanline limit on row 14"</summary>
    public string? Detail { get; }

    // Private constructor — use Builder to create instances
    private BuildDiagnosticMetric(
        string metricId,
        string displayName,
        string category,
        int current,
        int max,
        double warningThreshold,
        double errorThreshold,
        string? detail)
    {
        MetricId = metricId;
        DisplayName = displayName;
        Category = category;
        Current = current;
        Max = max;
        WarningThreshold = warningThreshold;
        ErrorThreshold = errorThreshold;
        Detail = detail;

        // Calculate UsageRatio
        UsageRatio = max > 0 ? (double)current / max : 0.0;

        // Calculate Severity
        if (UsageRatio >= errorThreshold)
            Severity = DiagnosticSeverity.Error;
        else if (UsageRatio >= warningThreshold)
            Severity = DiagnosticSeverity.Warning;
        else
            Severity = DiagnosticSeverity.Info;
    }

    /// <summary>
    /// Builder for creating BuildDiagnosticMetric instances.
    /// Ensures Severity and UsageRatio are calculated by Core, not set by target.
    /// </summary>
    public class Builder
    {
        private string _metricId = string.Empty;
        private string _displayName = string.Empty;
        private string _category = string.Empty;
        private int _current;
        private int _max;
        private double _warningThreshold = 0.8;
        private double _errorThreshold = 1.0;
        private string? _detail;

        public Builder WithMetricId(string metricId) { _metricId = metricId; return this; }
        public Builder WithDisplayName(string displayName) { _displayName = displayName; return this; }
        public Builder WithCategory(string category) { _category = category; return this; }
        public Builder WithCurrent(int current) { _current = current; return this; }
        public Builder WithMax(int max) { _max = max; return this; }
        public Builder WithWarningThreshold(double threshold) { _warningThreshold = threshold; return this; }
        public Builder WithErrorThreshold(double threshold) { _errorThreshold = threshold; return this; }
        public Builder WithDetail(string? detail) { _detail = detail; return this; }

        public BuildDiagnosticMetric Build()
        {
            return new BuildDiagnosticMetric(
                _metricId,
                _displayName,
                _category,
                _current,
                _max,
                _warningThreshold,
                _errorThreshold,
                _detail);
        }
    }
}

/// <summary>
/// Contains all diagnostic metrics produced by a target for a build.
/// </summary>
public class BuildDiagnosticsReport
{
    /// <summary>All metrics produced by the target for this build</summary>
    public IReadOnlyList<BuildDiagnosticMetric> Metrics { get; }

    /// <summary>Highest severity among all metrics</summary>
    public DiagnosticSeverity OverallSeverity { get; }

    /// <summary>Metrics grouped by category</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<BuildDiagnosticMetric>> ByCategory { get; }

    public BuildDiagnosticsReport(IEnumerable<BuildDiagnosticMetric> metrics)
    {
        Metrics = metrics.ToList();

        // Calculate overall severity
        OverallSeverity = Metrics.Any()
            ? Metrics.Max(m => m.Severity)
            : DiagnosticSeverity.Info;

        // Group by category
        ByCategory = Metrics
            .GroupBy(m => m.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BuildDiagnosticMetric>)g.ToList());
    }
}
