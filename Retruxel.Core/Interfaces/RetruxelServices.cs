namespace Retruxel.Core.Interfaces;

/// <summary>
/// Static service locator for Retruxel services.
/// Provides access to core services for plugins and tools without requiring direct Core references.
/// Services are registered by the main application at startup.
/// </summary>
public static class RetruxelServices
{
    /// <summary>
    /// Localization service for translating UI strings.
    /// Must be set by the main application before any plugin uses it.
    /// </summary>
    public static ILocalizationService Localization { get; set; } = null!;

    // Future services can be added here:
    // public static IProjectManager ProjectManager { get; set; } = null!;
    // public static IAssetManager AssetManager { get; set; } = null!;
    // public static IThemeService Theme { get; set; } = null!;
}
