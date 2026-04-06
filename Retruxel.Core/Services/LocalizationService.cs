using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private List<LanguageInfo> _availableLanguages = new();

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public string CurrentLanguage => _currentLanguage;
    public IReadOnlyList<LanguageInfo> AvailableLanguages => _availableLanguages;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationService() { }

    /// <summary>
    /// Detects the best language to use based on system locale with fallback logic.
    /// 1. Try exact match (e.g., pt-BR)
    /// 2. Try language family match (e.g., pt-BR -> pt)
    /// 3. Fallback to English
    /// </summary>
    public string DetectSystemLanguage()
    {
        var systemCulture = CultureInfo.CurrentUICulture.Name; // e.g., "pt-BR", "en-US"
        
        // Try exact match
        if (_availableLanguages.Any(l => l.Code.Equals(systemCulture, StringComparison.OrdinalIgnoreCase)))
            return systemCulture;
        
        // Try language family match (pt-BR -> pt, pt-PT -> pt)
        var languageFamily = systemCulture.Split('-')[0]; // "pt-BR" -> "pt"
        var familyMatch = _availableLanguages.FirstOrDefault(l => 
            l.Code.Split('-')[0].Equals(languageFamily, StringComparison.OrdinalIgnoreCase));
        
        if (familyMatch != null)
            return familyMatch.Code;
        
        // Fallback to English
        return "en";
    }

    /// <summary>
    /// Scans the localization folder and discovers all available languages.
    /// </summary>
    public void DiscoverLanguages(string localizationFolderPath)
    {
        _availableLanguages.Clear();

        if (!Directory.Exists(localizationFolderPath))
            return;

        var jsonFiles = Directory.GetFiles(localizationFolderPath, "*.json");

        foreach (var filePath in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var doc = JsonDocument.Parse(json);

                string code;
                string displayName;
                string nativeName;

                if (doc.RootElement.TryGetProperty("_metadata", out var metadata))
                {
                    code = metadata.GetProperty("code").GetString() ?? "unknown";
                    
                    // Try to get native name from CultureInfo
                    try
                    {
                        var culture = CultureInfo.GetCultureInfo(code);
                        nativeName = culture.NativeName;
                        displayName = culture.DisplayName;
                    }
                    catch
                    {
                        // Fallback to metadata if culture not found
                        displayName = metadata.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? code : code;
                        nativeName = metadata.TryGetProperty("nativeName", out var nn) ? nn.GetString() ?? displayName : displayName;
                    }
                }
                else
                {
                    // Fallback: use filename as code
                    code = Path.GetFileNameWithoutExtension(filePath);
                    try
                    {
                        var culture = CultureInfo.GetCultureInfo(code);
                        nativeName = culture.NativeName;
                        displayName = culture.DisplayName;
                    }
                    catch
                    {
                        displayName = code;
                        nativeName = code;
                    }
                }

                _availableLanguages.Add(new LanguageInfo(code, displayName, nativeName));
            }
            catch { /* Skip invalid files */ }
        }
    }

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
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("strings", out var stringsElement))
            {
                _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(stringsElement.GetRawText()) ?? new();
            }
            else
            {
                // Fallback for old format without _metadata
                _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }

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

/// <summary>
/// Represents metadata about an available language.
/// </summary>
public record LanguageInfo(string Code, string DisplayName, string NativeName);
