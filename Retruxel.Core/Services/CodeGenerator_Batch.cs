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

        // Collect all GameVar data
        var vars = gameVarInstances.Select(m =>
        {
            var json = m.Serialize();
            var node = JsonNode.Parse(json) as JsonObject;
            return new Dictionary<string, object>
            {
                ["name"] = node?["name"]?.GetValue<string>() ?? "myVar",
                ["type"] = node?["type"]?.GetValue<string>() ?? "int",
                ["initialValue"] = node?["initialValue"]?.GetValue<string>() ?? "0",
                ["showInHud"] = node?["showInHud"]?.GetValue<bool>() ?? false
            };
        }).ToList();

        // Check if any var is int or byte
        var hasIntOrByte = vars.Any(v =>
        {
            var type = v["type"].ToString();
            return type == "int" || type == "byte";
        });

        // Build variables for template
        var variables = new Dictionary<string, object>
        {
            ["vars"] = vars,
            ["hasIntOrByte"] = hasIntOrByte
        };

        // Try to render via ModuleRenderer
        if (_moduleRenderer.CanRender("gamevar", project.TargetId))
        {
            // For batch modules, we need a special render path
            // For now, manually construct the template rendering
            var key = $"{project.TargetId}::gamevar".ToLowerInvariant();
            var templatePath = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
                "Plugins", "CodeGens", "gamevar", project.TargetId, "gamevars.c.rtrx");

            if (File.Exists(templatePath))
            {
                var template = File.ReadAllText(templatePath);
                var content = TemplateEngine.Render(template, variables);

                files.Add(new GeneratedFile
                {
                    FileName = "gamevars.c",
                    Content = content,
                    FileType = GeneratedFileType.Source,
                    SourceModuleId = "gamevar"
                });

                progress?.Report($"INFO: gamevars.c generated with {vars.Count} variable(s).");
            }
        }

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
