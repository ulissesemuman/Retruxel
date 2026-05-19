using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;

namespace Retruxel.Core.Services;

/// <summary>
/// Splash injection - adds splash screen modules to project if enabled.
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// Injects splash screen modules into project if enabled.
    /// Called at the start of GenerateAsync() before any other processing.
    /// Does NOT change InitialSceneId - splash is only injected into OnStart.
    /// </summary>
    private void InjectSplash(RetruxelProject project)
    {
        // Load settings (global, not project-specific)
        var settings = SettingsService.Load();
        
        // Check if splash is enabled
        if (!settings.General.ShowMadeWithSplash)
            return;

        // Add splash module to first scene's OnStart modules (or create scene if none exists)
        if (project.Scenes.Count == 0)
        {
            project.Scenes.Add(new SceneData { SceneId = "main", SceneName = "main" });
        }

        var firstScene = project.Scenes[0];
        
        // Add splash module to scene elements (minimal state - codegen will handle the rest)
        firstScene.Elements.Add(new SceneElementData
        {
            ElementId = Guid.NewGuid().ToString(),
            ModuleId = "splash",
            ModuleState = System.Text.Json.JsonSerializer.SerializeToElement(new { })
        });
    }
}
