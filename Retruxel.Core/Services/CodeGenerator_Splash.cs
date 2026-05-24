using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Services;

/// <summary>
/// Splash injection - adds splash screen module to project if enabled.
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// Injects the splash screen as a project-level module if enabled in settings.
    /// Called at the start of GenerateAsync() before any other processing.
    /// Non-destructive: modifies only the in-memory copy, never persisted to disk.
    /// The splash module runs before any scene by being inserted at index 0 of project.Modules.
    /// </summary>
    private void InjectSplash(RetruxelProject project)
    {
        var settings = SettingsService.Load();

        if (!settings.General.ShowMadeWithSplash)
            return;

        // Inject splash as a project-level module (runs before any scene).
        // Non-destructive: only adds to in-memory copy, never persisted.
        var splashModule = new ProjectModuleData
        {
            ModuleId = "splash",
            Label    = "Made with Retruxel",
            Enabled  = true,
            State    = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                fadeInDuration  = 30,
                holdDuration    = 120,
                fadeOutDuration = 30,
                targetSlot      = 0
            })
        };

        // Insert at beginning so it runs first
        project.Modules.Insert(0, splashModule);
    }
}
