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

    /// <summary>Whether to check for updates on application startup.</summary>
    [JsonPropertyName("checkUpdatesOnStartup")]
    public bool CheckUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to show the "MADE WITH RETRUXEL" splash screen at the start
    /// of every compiled ROM. Enabled by default — helps spread the word about
    /// the tool. Can be disabled in Settings, with a confirmation prompt.
    /// </summary>
    [JsonPropertyName("showMadeWithSplash")]
    public bool ShowMadeWithSplash { get; set; } = true;

    /// <summary>
    /// Maximum number of undoable actions retained in history.
    /// Configurable between 20 and 100. Default: 32.
    /// </summary>
    [JsonPropertyName("undoHistoryLimit")]
    public int UndoHistoryLimit { get; set; } = 32;

    /// <summary>
    /// Whether to enable auto-save every 30 seconds.
    /// Default: true.
    /// </summary>
    [JsonPropertyName("autoSaveEnabled")]
    public bool AutoSaveEnabled { get; set; } = true;

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
/// Targets are discovered dynamically via TargetRegistry.
/// </summary>
public class TargetSettingsMap : Dictionary<string, TargetSettings>
{
    public TargetSettingsMap() : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}

/// <summary>
/// Settings common to all targets.
/// Each target gets its own instance with independent values.
/// </summary>
public class TargetSettings
{
    [JsonPropertyName("showToolchainWarnings")]
    public bool ShowToolchainWarnings { get; set; } = true;

    [JsonPropertyName("emulatorPath")]
    public string EmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("emulatorArguments")]
    public string EmulatorArguments { get; set; } = string.Empty;

    [JsonPropertyName("launchEmulatorAfterBuild")]
    public bool LaunchEmulatorAfterBuild { get; set; } = false;

    [JsonPropertyName("liveLinkEmulatorPath")]
    public string LiveLinkEmulatorPath { get; set; } = string.Empty;

    [JsonPropertyName("liveLinkEmulatorArguments")]
    public string LiveLinkEmulatorArguments { get; set; } = string.Empty;
}
