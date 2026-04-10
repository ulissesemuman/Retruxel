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

    [JsonPropertyName("window")]
    public WindowSettings Window { get; set; } = new();

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

    /// <summary>Recent project file paths (max 5).</summary>
    [JsonPropertyName("recentProjects")]
    public List<string> RecentProjects { get; set; } = [];

    /// <summary>Favorite target IDs.</summary>
    [JsonPropertyName("favoriteTargets")]
    public List<string> FavoriteTargets { get; set; } = [];
}

// ── Window ────────────────────────────────────────────────────────────────────

public class WindowSettings
{
    [JsonPropertyName("width")]
    public double Width { get; set; } = 1280;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 800;

    [JsonPropertyName("left")]
    public double Left { get; set; } = 100;

    [JsonPropertyName("top")]
    public double Top { get; set; } = 100;

    [JsonPropertyName("isMaximized")]
    public bool IsMaximized { get; set; } = false;

    [JsonPropertyName("isFirstRun")]
    public bool IsFirstRun { get; set; } = true;
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
    
    [JsonPropertyName("gg")]
    public GgSettings Gg { get; set; } = new();
    
    [JsonPropertyName("sg1000")]
    public Sg1000Settings Sg1000 { get; set; } = new();
    
    [JsonPropertyName("coleco")]
    public ColecoSettings Coleco { get; set; } = new();
    
    [JsonPropertyName("nes")]
    public NesSettings Nes { get; set; } = new();
}

/// <summary>
/// Settings specific to the Sega Master System target.
/// </summary>
public class SmsSettings
{
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;

    [JsonPropertyName("emulatorPath")]
    public string EmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("emulatorArguments")]
    public string EmulatorArguments { get; set; } = string.Empty;

    [JsonPropertyName("launchEmulatorAfterBuild")]
    public bool LaunchEmulatorAfterBuild { get; set; } = false;
}

/// <summary>
/// Settings specific to the Sega Game Gear target.
/// </summary>
public class GgSettings
{
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;

    [JsonPropertyName("emulatorPath")]
    public string EmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("emulatorArguments")]
    public string EmulatorArguments { get; set; } = string.Empty;

    [JsonPropertyName("launchEmulatorAfterBuild")]
    public bool LaunchEmulatorAfterBuild { get; set; } = false;
}

/// <summary>
/// Settings specific to the Sega SG-1000 target.
/// </summary>
public class Sg1000Settings
{
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;

    [JsonPropertyName("emulatorPath")]
    public string EmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("emulatorArguments")]
    public string EmulatorArguments { get; set; } = string.Empty;

    [JsonPropertyName("launchEmulatorAfterBuild")]
    public bool LaunchEmulatorAfterBuild { get; set; } = false;
}

/// <summary>
/// Settings specific to the ColecoVision target.
/// </summary>
public class ColecoSettings
{
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;

    [JsonPropertyName("emulatorPath")]
    public string EmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("emulatorArguments")]
    public string EmulatorArguments { get; set; } = string.Empty;

    [JsonPropertyName("launchEmulatorAfterBuild")]
    public bool LaunchEmulatorAfterBuild { get; set; } = false;
}

/// <summary>
/// Settings specific to the Nintendo Entertainment System target.
/// </summary>
public class NesSettings
{
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;

    [JsonPropertyName("emulatorPath")]
    public string EmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("emulatorArguments")]
    public string EmulatorArguments { get; set; } = string.Empty;

    [JsonPropertyName("launchEmulatorAfterBuild")]
    public bool LaunchEmulatorAfterBuild { get; set; } = false;
}
