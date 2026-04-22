using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Manages loading and saving of application settings.
/// Settings are persisted to %AppData%\Retruxel\settings.json.
///
/// Usage:
///   var settings = await SettingsService.LoadAsync();
///   var smsSettings = SettingsService.GetTargetSettings(settings, "sms");
///   smsSettings.ShowToolchainWarnings = false;
///   await SettingsService.SaveAsync(settings);
/// </summary>
public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Retruxel", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Loads settings from disk.
    /// Returns defaults if the file does not exist or cannot be parsed.
    /// </summary>
    public static async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = await File.ReadAllTextAsync(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return settings;
        }
        catch
        {
            // Corrupted or unreadable settings — return defaults silently
            return new AppSettings();
        }
    }

    /// <summary>
    /// Synchronous load for use in constructors and non-async contexts.
    /// Prefer LoadAsync when possible.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            return settings;
        }
        catch
        {
            // Corrupted or unreadable settings — return defaults silently
            return new AppSettings();
        }
    }

    /// <summary>
    /// Gets settings for a specific target by ID.
    /// Creates default settings if the target doesn't have an entry yet.
    /// </summary>
    public static TargetSettings GetTargetSettings(AppSettings settings, string targetId)
    {
        if (!settings.Targets.TryGetValue(targetId, out var targetSettings))
        {
            targetSettings = new TargetSettings();
            settings.Targets[targetId] = targetSettings;
        }
        return targetSettings;
    }

    /// <summary>
    /// Saves settings to disk.
    /// Creates the Retruxel AppData folder if it does not exist.
    /// </summary>
    public static async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(SettingsPath, json);
    }

    /// <summary>
    /// Synchronous save for use in window closing handlers.
    /// Prefer SaveAsync when possible.
    /// </summary>
    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
