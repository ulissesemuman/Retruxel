using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace Retruxel.Core.Services;

/// <summary>
/// Batch module processing: GameVars, TextArray, and other batch-generated files.
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// Generates gamevars.h and gamevars.c files for all GameVar module instances.
    /// Uses batch processing to generate a single file containing all variables.
    /// </summary>
    private List<GeneratedFile> GenerateGameVarsFile(
        RetruxelProject project,
        List<IModule> gameVarInstances,
        IProgress<string>? progress)
    {
        var files = new List<GeneratedFile>();

        // Collect vars from both the new GameVars list and legacy module instances
        var vars = new List<Dictionary<string, object>>();

        foreach (var gv in project.GameVars)
        {
            vars.Add(new Dictionary<string, object>
            {
                ["name"]         = gv.VariableId,
                ["type"]         = gv.Type,
                ["initialValue"] = gv.InitialValue,
                ["showInHud"]    = gv.ShowInHud
            });
        }

        // Legacy: module instances (GameVarModule still supported)
        foreach (var m in gameVarInstances)
        {
            var node = JsonNode.Parse(m.Serialize()) as JsonObject;
            var name = node?["name"]?.GetValue<string>() ?? "myVar";
            if (project.GameVars.Any(g => g.VariableId == name)) continue; // skip duplicates
            vars.Add(new Dictionary<string, object>
            {
                ["name"]         = name,
                ["type"]         = node?["type"]?.GetValue<string>() ?? "int",
                ["initialValue"] = node?["initialValue"]?.GetValue<string>() ?? "0",
                ["showInHud"]    = node?["showInHud"]?.GetValue<bool>() ?? false
            });
        }

        if (vars.Count == 0) return files;

        var hasIntOrByte = vars.Any(v => v["type"].ToString() is "int" or "byte");

        // Build a lookup: variableId → GameVarDefinition (for Reset operation initial value)
        var varLookup = project.GameVars.ToDictionary(
            g => g.VariableId,
            g => g,
            StringComparer.OrdinalIgnoreCase);

        // Build event bus data from EventBindings
        var bindings = project.EventBindings;
        var hasEventBindings = bindings.Count > 0;

        // Collect unique event IDs and assign numeric defines
        var uniqueEventIds = bindings
            .Select(b => b.EventId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e)
            .Select((eventId, idx) => new Dictionary<string, object>
            {
                ["define"] = $"EV_{eventId.ToUpperInvariant()}",
                ["index"]  = idx
            })
            .ToList<object>();

        // Group bindings by eventId for the switch statement
        var bindingGroups = bindings
            .GroupBy(b => b.EventId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Dictionary<string, object>
            {
                ["eventDefine"] = $"EV_{g.Key.ToUpperInvariant()}",
                ["bindings"] = g.Select(b =>
                {
                    varLookup.TryGetValue(b.VariableId, out var varDef);
                    return (object)new Dictionary<string, object>
                    {
                        ["variableName"] = b.VariableId,
                        ["operation"]    = b.Operation.ToString(),
                        ["value"]        = b.Value,
                        ["initialValue"] = varDef?.InitialValue ?? "0",
                        ["condition"]    = b.Condition ?? string.Empty
                    };
                }).ToList<object>()
            })
            .ToList<object>();

        var variables = new Dictionary<string, object>
        {
            ["vars"]             = vars.Cast<object>().ToList(),
            ["hasIntOrByte"]     = hasIntOrByte,
            ["hasEventBindings"] = hasEventBindings,
            ["eventIds"]         = uniqueEventIds,
            ["bindingGroups"]    = bindingGroups
        };

        var templatePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
            "Plugins", "CodeGens", "gamevar", project.TargetId, "gamevars.c.rtrx");

        if (!File.Exists(templatePath)) return files;

        var template = File.ReadAllText(templatePath);
        var content  = TemplateEngine.Render(template, variables);

        files.Add(new GeneratedFile
        {
            FileName       = "gamevars.c",
            Content        = content,
            FileType       = GeneratedFileType.Source,
            SourceModuleId = "gamevar"
        });

        progress?.Report($"INFO: gamevars.c generated — {vars.Count} variable(s), {bindings.Count} event binding(s).");

        return files;
    }

    /// <summary>
    /// Generates retruxel_text.h and retruxel_text.c files for all TextArray module instances.
    /// Uses batch processing to generate a single file containing all text arrays.
    /// </summary>
    private List<GeneratedFile> GenerateTextArrayFile(
        RetruxelProject project,
        List<IModule> textArrayInstances,
        IProgress<string>? progress)
    {
        var files = new List<GeneratedFile>();

        // Check if there are any strings defined
        var hasStrings = textArrayInstances.Any(m =>
        {
            var json = m.Serialize();
            var node = JsonNode.Parse(json) as JsonObject;
            var languagesNode = node?["languages"] as JsonArray;
            return languagesNode?.Any(langNode =>
            {
                var langObj = langNode as JsonObject;
                var stringsNode = langObj?["strings"] as JsonArray;
                return stringsNode?.Any(s => !string.IsNullOrEmpty(s?.GetValue<string>())) ?? false;
            }) ?? false;
        });

        if (!hasStrings)
        {
            progress?.Report("INFO: text.array modules exist but contain no strings - skipping text system generation.");
            return files;
        }

        // Use ModuleRenderer.RenderBatch for batch module processing
        var allInstancesJson = textArrayInstances.Select(m => m.Serialize()).ToList();
        var batchFiles = _moduleRenderer.RenderBatch(
            "text.array",
            project.TargetId,
            allInstancesJson,
            project.ProjectPath);

        files.AddRange(batchFiles);
        progress?.Report($"INFO: retruxel_text.c generated with {textArrayInstances.Count} text array(s).");

        return files;
    }

    /// <summary>
    /// Calculates the first free tile slot after all graphic tiles.
    /// Used as the starting point for the compact font.
    /// </summary>
    private static int CalculateGraphicTilesEnd(RetruxelProject project)
    {
        // Simple heuristic: reserve 256 tiles for graphics
        // In production, this would scan plane/sprite modules for actual usage
        return 256;
    }
}
