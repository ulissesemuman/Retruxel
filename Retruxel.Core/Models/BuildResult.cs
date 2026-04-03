namespace Retruxel.Core.Models;

/// <summary>
/// Represents the outcome of a toolchain build operation.
/// Returned by IToolchain.BuildAsync and displayed in the Build Console.
/// </summary>
public class BuildResult
{
    /// <summary>Whether the build completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Full path to the generated ROM file. Null if the build failed.</summary>
    public string? RomPath { get; set; }

    /// <summary>ROM size in bytes. Zero if the build failed.</summary>
    public int RomSizeBytes { get; set; }

    /// <summary>
    /// Full build log output as displayed in the Build Console.
    /// Includes compiler output, warnings and errors.
    /// </summary>
    public List<BuildLogEntry> Log { get; set; } = [];

    /// <summary>Build start timestamp.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Build end timestamp.</summary>
    public DateTime FinishedAt { get; set; }

    /// <summary>Total build duration.</summary>
    public TimeSpan Duration => FinishedAt - StartedAt;

    /// <summary>
    /// MD5 checksum of the generated ROM.
    /// Used for verification in the Build Console and ToolchainValidator.
    /// </summary>
    public string? RomMd5 { get; set; }

    /// <summary>
    /// SHA-256 checksum of the generated ROM.
    /// Used for verification in the Build Console and ToolchainValidator.
    /// </summary>
    public string? RomSha256 { get; set; }
}

/// <summary>
/// A single entry in the build log.
/// </summary>
public class BuildLogEntry
{
    /// <summary>Log entry severity level.</summary>
    public BuildLogLevel Level { get; set; }

    /// <summary>Log message text.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Timestamp of this log entry.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Severity level of a build log entry.
/// Maps to colored prefixes in the Build Console UI.
/// Ex: INFO → white, WARN → yellow, ERROR → red, SUCCESS → green.
/// </summary>
public enum BuildLogLevel
{
    /// <summary>General information — white in the Build Console</summary>
    Info,

    /// <summary>Non-critical warning — yellow in the Build Console</summary>
    Warning,

    /// <summary>Build error — red in the Build Console</summary>
    Error,

    /// <summary>Successful step — green in the Build Console</summary>
    Success
}