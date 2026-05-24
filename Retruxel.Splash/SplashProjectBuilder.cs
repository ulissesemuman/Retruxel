using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Splash;

/// <summary>
/// Builds the "Made with Retruxel" splash RetruxelProject.
///
/// The splash is a generic module composition — it is not tied to any specific target.
/// The target provides the actual asset (image, palette) via GetSplashAsset() when
/// the declarative codegen runs.
///
/// Usage:
///   var splashProject = SplashProjectBuilder.Build(targetId);
///   // Merge into user project via CodeGenerator.InjectSplash() before GenerateAsync()
///
/// The returned project is a minimal in-memory representation:
///   - One project-level SplashModule with default timing parameters
///   - No scenes, no assets — the target codegen handles the image data
/// </summary>
public static class SplashProjectBuilder
{
    /// <summary>
    /// Builds a minimal RetruxelProject representing the "Made with Retruxel" splash screen.
    ///
    /// Contains a single SplashModule at project level with default timing:
    ///   - Fade in:  30 frames (~0.5s at 60fps)
    ///   - Hold:    120 frames (~2.0s at 60fps)
    ///   - Fade out: 30 frames (~0.5s at 60fps)
    ///
    /// The caller (CodeGenerator.InjectSplash) merges this into the user's project
    /// non-destructively — only in memory, never persisted to disk.
    /// </summary>
    /// <param name="targetId">Target console identifier (e.g. "sms"). Used to set TargetId on the returned project.</param>
    public static RetruxelProject Build(string targetId) => new()
    {
        Name     = "retruxel.splash",
        TargetId = targetId,
        Modules  =
        [
            new ProjectModuleData
            {
                ModuleId = "splash",
                Label    = "Made with Retruxel",
                Enabled  = true,
                State    = JsonSerializer.SerializeToElement(new
                {
                    fadeInDuration  = 30,
                    holdDuration    = 120,
                    fadeOutDuration = 30,
                    targetSlot      = 0
                })
            }
        ]
    };
}
