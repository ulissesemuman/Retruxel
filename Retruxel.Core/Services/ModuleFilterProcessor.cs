using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Retruxel.Core.Services;

/// <summary>
/// Processes module collections with filtering and transformation logic.
/// Handles projectModules and sceneModules variable sources.
/// </summary>
internal static class ModuleFilterProcessor
{
    public static List<string> ProcessProjectModules(RetruxelProject project, VariableDefinition varDef)
    {
        // Collect module IDs from project-level modules and all scene-level overrides.
        // Legacy Elements are intentionally excluded — they are only processed by
        // CodeGenerator's backward-compat pass (step 2e) and should not appear here.
        var allModuleIds = project.Modules
            .Select(m => m.ModuleId)
            .Concat(project.Scenes.SelectMany(s => s.ModuleOverrides).Select(m => m.ModuleId))
            .ToList();

        if (!string.IsNullOrEmpty(varDef.Path))
        {
            allModuleIds = allModuleIds.Where(moduleId => EvaluateModuleIdFilter(moduleId, varDef.Path)).ToList();
        }

        if (varDef.Transform == "moduleId")
        {
            return allModuleIds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return allModuleIds;
    }

    public static object ProcessSceneModules(List<IModule> modules, VariableDefinition varDef)
    {
        var filtered = modules.AsEnumerable();
        if (!string.IsNullOrEmpty(varDef.Path))
        {
            filtered = filtered.Where(m => EvaluateModuleFilter(m, varDef.Path));
        }

        if (varDef.Transform == "initCall")
        {
            return filtered.Select(m => new Dictionary<string, object>
            {
                ["header"] = $"{m.ModuleId.Replace(".", "_")}.h",
                ["call"] = $"{m.ModuleId.Replace(".", "_")}_init"
            }).ToList();
        }
        else if (varDef.Transform == "hasAny")
        {
            return filtered.Any();
        }

        return filtered.Select(m => m.ModuleId).ToList();
    }

    private static bool EvaluateModuleIdFilter(string moduleId, string filter)
    {
        var parts = filter.Split(new[] { "||" }, StringSplitOptions.TrimEntries);
        return parts.Any(part =>
        {
            if (part.Contains("=="))
            {
                var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                if (tokens.Length == 2)
                {
                    var left = tokens[0].Trim();
                    var right = tokens[1].Trim().Trim('\'', '"');

                    if (left == "moduleId")
                        return moduleId.Equals(right, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        });
    }

    private static bool EvaluateModuleFilter(IModule module, string filter)
    {
        var parts = filter.Split(new[] { "||" }, StringSplitOptions.TrimEntries);
        return parts.Any(part =>
        {
            if (part.Contains("=="))
            {
                var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                if (tokens.Length == 2)
                {
                    var left = tokens[0].Trim();
                    var right = tokens[1].Trim().Trim('\'', '"');

                    if (left == "moduleId")
                        return module.ModuleId.Equals(right, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        });
    }
}
