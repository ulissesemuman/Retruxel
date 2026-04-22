namespace Retruxel.Core.Interfaces;

/// <summary>
/// Provides localization services for plugins and tools.
/// Implemented by the main application and exposed through ServiceLocator.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Translates a localization key to the current language.
    /// </summary>
    /// <param name="key">Localization key (e.g., "app.title")</param>
    /// <returns>Translated string, or the key itself if translation not found</returns>
    string Translate(string key);

    /// <summary>
    /// Gets the current language code (e.g., "en", "pt-BR")
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Event raised when the language changes at runtime
    /// </summary>
    event Action? LanguageChanged;
}
