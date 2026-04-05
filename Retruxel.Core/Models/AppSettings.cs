using System.Text.Json.Serialization;

namespace Retruxel.Core.Models;

/// <summary>
/// Root settings object for the Retruxel application.
/// Serialized to %AppData%\Retruxel\settings.json.
/// Each section maps to a category in the Settings window.
/// </summary>
public class AppSettings
{
    [JsonPropertyName("general")]
    public GeneralSettings General { get; set; } = new();

    [JsonPropertyName("appearance")]
    public AppearanceSettings Appearance { get; set; } = new();

    [JsonPropertyName("targets")]
    public TargetSettingsMap Targets { get; set; } = new();
}

// ── General ───────────────────────────────────────────────────────────────────

public class GeneralSettings
{
    /// <summary>UI language code. Ex: "en", "pt-BR"</summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    /// <summary>Whether to show the Welcome screen on application startup.</summary>
    [JsonPropertyName("showWelcomeOnStartup")]
    public bool ShowWelcomeOnStartup { get; set; } = true;

    /// <summary>Last folder used in the New Project dialog.</summary>
    [JsonPropertyName("lastProjectLocation")]
    public string LastProjectLocation { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Retruxel");

    /// <summary>Whether to show all modules regardless of target compatibility.</summary>
    [JsonPropertyName("showAllModules")]
    public bool ShowAllModules { get; set; } = false;
}

// ── Appearance ────────────────────────────────────────────────────────────────

public class AppearanceSettings
{
    /// <summary>Base font size for the UI in points.</summary>
    [JsonPropertyName("fontSize")]
    public int FontSize { get; set; } = 13;
}

// ── Targets ───────────────────────────────────────────────────────────────────

/// <summary>
/// Map of target-specific settings, keyed by TargetId.
/// Ex: settings.Targets["sms"].ShowToolchainWarnings
/// New targets are added here as they are implemented.
/// </summary>
public class TargetSettingsMap
{
    [JsonPropertyName("sms")]
    public SmsSettings Sms { get; set; } = new();
}

/// <summary>
/// Settings specific to the Sega Master System target.
/// </summary>
public class SmsSettings
{
    /// <summary>
    /// Whether to display toolchain warnings in the Build Console.
    /// When false, WARNING-level entries from the SMS toolchain are suppressed.
    /// </summary>
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;
}
