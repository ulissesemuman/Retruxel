using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Services;

/// <summary>
/// Generates initialization and update calls for modules based on their assigned triggers.
/// Handles OnStart, OnVBlank, and OnInput event generation.
/// </summary>
internal static class EventCallGenerator
{
    public static List<string> GenerateUpdateCalls(List<GeneratedFile> files)
    {
        var modulesWithUpdate = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "animation", 
            "entity", 
            "enemy", 
            "scroll"
        };

        var updateCalls = new List<string>();
        var processedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file.FileType != GeneratedFileType.Header)
                continue;

            var moduleId = file.SourceModuleId;
            if (string.IsNullOrEmpty(moduleId) || processedModules.Contains(moduleId))
                continue;

            if (modulesWithUpdate.Contains(moduleId))
            {
                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                updateCalls.Add($"        {baseName}_update();");
                processedModules.Add(moduleId);
            }
        }

        return updateCalls;
    }

    public static List<string> GenerateInputCalls(List<GeneratedFile> files)
    {
        var inputCalls = new List<string>();
        var processedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file.FileType != GeneratedFileType.Header)
                continue;

            var moduleId = file.SourceModuleId;
            if (string.IsNullOrEmpty(moduleId) || processedModules.Contains(moduleId))
                continue;

            if (moduleId.Equals("input", StringComparison.OrdinalIgnoreCase))
            {
                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                inputCalls.Add($"        {baseName}_update();");
                processedModules.Add(moduleId);
            }
        }

        return inputCalls;
    }
    
    public static List<object> GenerateEventCalls(
        List<GeneratedFile> files,
        Dictionary<IModule, string> triggersByElement,
        string targetTrigger,
        ModuleRegistry? moduleRegistry)
    {
        var calls = new List<object>();
        var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        var graphicModuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tilemap", "sprite", "text.display", "palette", "background", "animation"
        };

        var allCalls = new List<(string fileName, string moduleId, string baseName, bool isGraphic, bool isTextDisplay, bool isPalette)>();

        foreach (var file in files)
        {
            if (file.FileType != GeneratedFileType.Header)
                continue;

            var moduleId = file.SourceModuleId;
            if (string.IsNullOrEmpty(moduleId))
                continue;
                
            if (moduleId.Equals("retruxel.core", StringComparison.OrdinalIgnoreCase) ||
                moduleId.Equals("retruxel.splash", StringComparison.OrdinalIgnoreCase) ||
                moduleId.Equals("retruxel.engine", StringComparison.OrdinalIgnoreCase))
                continue;

            var hasTargetTrigger = triggersByElement.Any(kvp => 
                kvp.Key.ModuleId == moduleId && 
                kvp.Value.Equals(targetTrigger, StringComparison.OrdinalIgnoreCase));

            if (!hasTargetTrigger)
                continue;

            if (processedFiles.Contains(file.FileName))
                continue;

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var isGraphic = graphicModuleIds.Contains(moduleId);
            var isTextDisplay = moduleId.Equals("text.display", StringComparison.OrdinalIgnoreCase);
            var isPalette = moduleId.Equals("palette", StringComparison.OrdinalIgnoreCase);
            
            allCalls.Add((file.FileName, moduleId, baseName, isGraphic, isTextDisplay, isPalette));
            processedFiles.Add(file.FileName);
        }

        var sortedCalls = allCalls
            .OrderByDescending(c => c.isPalette)
            .ThenBy(c => c.isTextDisplay)
            .ToList();

        foreach (var (fileName, moduleId, baseName, isGraphic, isTextDisplay, isPalette) in sortedCalls)
        {
            calls.Add(new Dictionary<string, object>
            {
                ["call"] = $"    {baseName}_init();",
                ["isGraphicModule"] = isGraphic,
                ["isTextDisplay"] = isTextDisplay
            });
        }

        return calls;
    }
}
