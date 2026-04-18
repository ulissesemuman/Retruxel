namespace Retruxel.Core.Models;

/// <summary>
/// Represents metadata about a downloadable target package.
/// Used by the TargetPackageManager to list available targets
/// and track installation status.
/// </summary>
public class TargetPackageInfo
{
    /// <summary>Target identifier (e.g., "sms", "nes")</summary>
    public string TargetId { get; set; } = "";

    /// <summary>Human-readable name (e.g., "Sega Master System")</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>Manufacturer name (e.g., "Sega", "Nintendo")</summary>
    public string Manufacturer { get; set; } = "";

    /// <summary>Package version (e.g., "0.7.1")</summary>
    public string Version { get; set; } = "";

    /// <summary>Package size in bytes</summary>
    public long SizeBytes { get; set; }

    /// <summary>Direct download URL (GitHub Releases or CDN)</summary>
    public string DownloadUrl { get; set; } = "";

    /// <summary>SHA-256 checksum for integrity verification</summary>
    public string Checksum { get; set; } = "";

    /// <summary>Whether this target is currently installed</summary>
    public bool IsInstalled { get; set; }

    /// <summary>Short description of the target platform</summary>
    public string Description { get; set; } = "";

    /// <summary>Minimum Retruxel version required</summary>
    public string MinRetruxelVersion { get; set; } = "0.7.0";
}
