using System.ComponentModel;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Singleton service for managing UI localization.
/// Loads translation files and provides string lookup with automatic UI refresh on language change.
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private Dictionary<string, string> _strings = new();
    private string _currentLanguage = "en";

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public string CurrentLanguage => _currentLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService() { }

    /// <summary>
    /// Loads a language file from the specified folder.
    /// </summary>
    /// <param name="language">Language code (e.g., "en", "pt-BR")</param>
    /// <param name="localizationFolderPath">Path to the folder containing .json files</param>
    public void Load(string language, string localizationFolderPath)
    {
        var filePath = Path.Combine(localizationFolderPath, $"{language}.json");

        if (!File.Exists(filePath))
        {
            // Fallback to English if requested language doesn't exist
            filePath = Path.Combine(localizationFolderPath, "en.json");
            if (!File.Exists(filePath))
            {
                _strings.Clear();
                return;
            }
        }

        try
        {
            var json = File.ReadAllText(filePath);
            _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            _currentLanguage = language;

            // Notify all bindings to refresh by passing empty string
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
        catch
        {
            _strings.Clear();
        }
    }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">Translation key (e.g., "welcome.title")</param>
    /// <returns>Translated string or [key] if not found</returns>
    public string Get(string key)
    {
        return _strings.TryGetValue(key, out var value) ? value : $"[{key}]";
    }

    /// <summary>
    /// Indexer for XAML binding support.
    /// </summary>
    public string this[string key] => Get(key);
}
