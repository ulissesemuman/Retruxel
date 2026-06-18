using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and renders declarative CodeGens from the plugins/CodeGens/ folder.
/// Delegates to specialized classes for variable resolution, filtering, and discovery.
/// </summary>
public class ModuleRenderer
{
    private readonly string _pluginsPath;
    private readonly Dictionary<string, ITool> _tools;
    private readonly Dictionary<string, CodeGenManifest> _codeGens;
    private readonly VariableResolver _variableResolver;
    private Assembly? _targetAssembly;
    private ModuleRegistry? _moduleRegistry;
    private Dictionary<string, object> _globalVariables = new();

    private readonly Dictionary<string, int> _instanceCounters = new();
    // Tracks which sprite asset arrays have already been emitted to avoid duplicates
    // across multiple entity instances sharing the same asset.
    private readonly HashSet<string> _emittedSpriteAssets = new(StringComparer.OrdinalIgnoreCase);

    public ModuleRenderer(string pluginsPath, Assembly? targetAssembly = null, IProgress<string>? progress = null)
    {
        _pluginsPath = pluginsPath;
        _targetAssembly = targetAssembly;
        _tools = ToolDiscovery.DiscoverTools(pluginsPath);
        _codeGens = CodeGenDiscovery.DiscoverCodeGens(pluginsPath, progress);
        _variableResolver = new VariableResolver(_tools, targetAssembly);
    }

    public void ResetState()
    {
        _instanceCounters.Clear();
        _emittedSpriteAssets.Clear();
    }

    /// <summary>
    /// Sets global variables available to all CodeGen templates.
    /// Used for injecting TextAnalyzer results (fontStartTile, fontTileData, etc.).
    /// </summary>
    public void SetGlobalVariables(Dictionary<string, object> variables)
    {
        _globalVariables = variables;
        _variableResolver.SetGlobalVariables(variables);
    }

    public void SetTargetAssembly(Assembly? targetAssembly)
    {
        _targetAssembly = targetAssembly;
        _variableResolver.SetTargetAssembly(targetAssembly);
    }

    public void SetModuleRegistry(ModuleRegistry? registry)
 => _moduleRegistry = registry;

    public void SetTarget(ITarget? target)
 => _variableResolver.SetTarget(target);

    /// <summary>
    /// Registers virtual (in-memory) assets so tools can resolve them without reading disk.
    /// Used for merged plane assets created at code-gen time.
    /// </summary>
    public void SetInMemoryAssets(Dictionary<string, Models.AssetEntry> assets)
 => _variableResolver.SetInMemoryAssets(assets);

    public bool CanRender(string moduleId, string targetId)
 => _codeGens.ContainsKey(Key(targetId, moduleId));

    public GeneratedFile? RenderMainFile(
        string targetId,
        RetruxelProject project,
        IEnumerable<GeneratedFile> moduleFiles,
        Dictionary<IModule, string> triggersByElement,
        IProgress<string>? progress = null)
    {
        var key = Key(targetId, "main");

        if (!_codeGens.TryGetValue(key, out var manifest))
            return null;

        if (!manifest.IsSystemModule)
            return null;

        var filesList = moduleFiles.ToList();

        var variables = new Dictionary<string, object>
        {
            ["projectName"] = project.Name,
            ["targetId"] = project.TargetId,
            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        foreach (var (varName, varDef) in manifest.Variables)
        {
            switch (varDef.From)
            {
                case "project":
                    if (varDef.Path == "name")
                        variables[varName] = project.Name;
                    else if (varDef.Path == "targetId")
                        variables[varName] = project.TargetId;
                    else if (varDef.Path == "initialSceneId")
                    {
                        // Convert SceneId (GUID) to scene name
                        var initialScene = project.Scenes.FirstOrDefault(s => s.SceneId == project.InitialSceneId);
                        var sceneName = initialScene?.SceneName ?? "main";
                        variables[varName] = SanitizeFileName(sceneName).ToLowerInvariant();
                    }
                    break;

                case "system":
                    if (varName == "timestamp")
                        variables[varName] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    break;

                case "settings":
                    variables[varName] = _variableResolver.ResolveSettingsValue(varDef);
                    break;

                case "constant":
                    variables[varName] = varDef.Value ?? varDef.Default ?? Array.Empty<string>();
                    break;

                case "moduleFiles":
                    if (varName == "hasTextDisplay")
                    {
                        variables[varName] = filesList.Any(f => f.SourceModuleId == "text.display");
                    }
                    else if (varName == "hasGameVars")
                    {
                        variables[varName] = filesList.Any(f => f.SourceModuleId == "gamevar");
                    }
                    else
                    {
                        variables[varName] = FileFilterProcessor.ProcessModuleFiles(filesList, varDef, variables);
                    }
                    break;

                case "projectModules":
                    variables[varName] = ModuleFilterProcessor.ProcessProjectModules(project, varDef);
                    break;

                case "updateCalls":
                    variables[varName] = EventCallGenerator.GenerateUpdateCalls(filesList);
                    break;

                case "inputCalls":
                    variables[varName] = EventCallGenerator.GenerateInputCalls(filesList);
                    break;

                case "onStartCalls":
                    variables[varName] = EventCallGenerator.GenerateEventCalls(filesList, triggersByElement, "OnStart", _moduleRegistry);
                    break;

                case "onVBlankCalls":
                    var onVBlankCalls = EventCallGenerator.GenerateEventCalls(filesList, triggersByElement, "OnVBlank", _moduleRegistry);
                    variables[varName] = onVBlankCalls
                        .Cast<Dictionary<string, object>>()
                        .Select(d => d["call"].ToString())
                        .ToList();
                    break;
            }
        }

        var template = File.ReadAllText(manifest.TemplatePath);
        var content = TemplateEngine.Render(template, variables);

        return new GeneratedFile
        {
            FileName = "main.c",
            Content = content,
            FileType = GeneratedFileType.Source,
            SourceModuleId = "retruxel.core"
        };
    }

    public IEnumerable<GeneratedFile> RenderSceneFiles(
        string targetId,
        SceneData scene,
        List<GeneratedFile> moduleFiles,
        ITarget target,
        IProgress<string>? progress = null)
    {
        var key = Key(targetId, "scene");

        if (!_codeGens.TryGetValue(key, out var manifest))
        {
            yield break;
        }

        // Sanitize scene name for file names (remove spaces and special chars)
        var sanitizedName = SanitizeFileName(scene.SceneName).ToLowerInvariant();

        var variables = new Dictionary<string, object>
        {
            ["sceneName"] = sanitizedName,
            ["sceneNameUpper"] = sanitizedName.ToUpperInvariant()
        };

        // Resolve palette slots
        foreach (var (varName, varDef) in manifest.Variables)
        {
            if (varDef.From == "scenePaletteSlot" && varDef.SlotIndex.HasValue)
            {
                var slotIndex = varDef.SlotIndex.Value;

                if (slotIndex < scene.PaletteSlots.Count)
                {
                    var slot = scene.PaletteSlots[slotIndex];
                    var converter = FindPaletteConverter(target);

                    if (converter is not null)
                    {
                        var bytes = converter.ConvertColors(slot.Colors);
                        variables[varName] = string.Join(", ", bytes.Select(b => $"0x{b:X2}"));
                    }
                    else
                    {
                        variables[varName] = "0x00";
                    }
                }
            }
        }

        // Build init calls from generated files instead of module instances
        var paletteInits = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Header && f.SourceModuleId == "palette")
            .Select(f => new Dictionary<string, object>
            {
                ["header"] = f.FileName,
                ["call"] = Path.GetFileNameWithoutExtension(f.FileName) + "_init"
            })
            .ToList();

        var planeInits = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Header && f.SourceModuleId == "plane")
            .Select(f => new Dictionary<string, object>
            {
                ["header"] = f.FileName,
                ["call"] = Path.GetFileNameWithoutExtension(f.FileName) + "_init"
            })
            .ToList();

        var textStaticInits = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Header && f.SourceModuleId == "text.display")
            .Select(f => new Dictionary<string, object>
            {
                ["header"] = f.FileName,
                ["call"] = Path.GetFileNameWithoutExtension(f.FileName) + "_init"
            })
            .ToList();

        // Entity inits — called once per scene to load tiles into VRAM and set up SAT
        var entityInits = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Header &&
                        f.SourceModuleId == "entity")
            .Select(f => new Dictionary<string, object>
            {
                ["header"] = f.FileName,
                ["call"] = Path.GetFileNameWithoutExtension(f.FileName) + "_init"
            })
            .ToList();

        variables["paletteInits"] = paletteInits;
        variables["planeInits"] = planeInits;
        variables["textStaticInits"] = textStaticInits;
        variables["entityInits"] = entityInits;
        variables["hasGraphicModules"] = paletteInits.Count > 0 || planeInits.Count > 0 || textStaticInits.Count > 0;
        variables["hasEntities"] = entityInits.Count > 0;

        var template = File.ReadAllText(manifest.TemplatePath);

        yield return new GeneratedFile
        {
            FileName = $"scene_{sanitizedName}.h",
            Content = TemplateEngine.RenderBlock(template, "header", variables),
            FileType = GeneratedFileType.Header,
            SourceModuleId = "scene"
        };

        yield return new GeneratedFile
        {
            FileName = $"scene_{sanitizedName}.c",
            Content = TemplateEngine.RenderBlock(template, "source", variables),
            FileType = GeneratedFileType.Source,
            SourceModuleId = "scene"
        };
    }

    public IEnumerable<GeneratedFile> Render(
        string moduleId,
        string targetId,
        string moduleJson,
        bool isSingleton,
        string? projectPath = null,
        SceneData? currentScene = null,
        string? fileNamePrefix = null)
    {
        var key = Key(targetId, moduleId);
        if (!_codeGens.TryGetValue(key, out var manifest))
            yield break;

        // Check if this is a batch module
        if (manifest.IsBatchModule)
        {
            // Batch modules should not be rendered per-instance
            // They are rendered once via RenderBatch()
            yield break;
        }

        _variableResolver.SetCurrentScene(currentScene);
        var variables = _variableResolver.ResolveForModule(manifest.Variables, moduleJson, projectPath);

        var effectiveSingleton = _moduleRegistry?.IsModuleSingleton(moduleId) ?? isSingleton;

        if (!effectiveSingleton)
        {
            // Use fileNamePrefix as counter key when provided so that
            // player_0/player_1 and enemy_0/enemy_1 are independent sequences.
            var counterKey = !string.IsNullOrEmpty(fileNamePrefix) ? fileNamePrefix : key;

            if (!_instanceCounters.ContainsKey(counterKey))
                _instanceCounters[counterKey] = 0;

            var instanceId = _instanceCounters[counterKey]++;
            variables["entityId"] = instanceId;
            variables["instanceId"] = instanceId;

            // isFirstInstance for tile array emission:
            // For entity/enemy modules, track by spriteAssetId so that two entities
            // sharing the same asset only emit the tile array once (the first one).
            // For all other Multiple modules, fall back to instanceId == 0.
            if (variables.TryGetValue("spriteAssetId", out var assetObj) &&
                AsStringOrEmpty(assetObj) is string assetId && !string.IsNullOrEmpty(assetId))
            {
                var isFirst = _emittedSpriteAssets.Add(assetId);
                variables["isFirstInstance"]         = isFirst;
                variables["isFirstInstanceWithAsset"]    = isFirst;
                variables["isFirstInstanceWithoutAsset"] = false;
                variables["isNotFirstInstanceWithAsset"] = !isFirst;
            }
            else
            {
                var isFirst = instanceId == 0;
                variables["isFirstInstance"]             = isFirst;
                variables["isFirstInstanceWithAsset"]    = false;
                variables["isFirstInstanceWithoutAsset"] = isFirst;
                variables["isNotFirstInstanceWithAsset"] = false;
            }

            // Pre-compute VRAM half selection to avoid nested conditionals in templates.
            // SMS SAT reads tiles 0-255 when useFirstHalf=1, tiles 256-511 when useFirstHalf=0.
            if (variables.TryGetValue("startTile", out var startTileObj))
            {
                var startTileVal = startTileObj switch
                {
                    int i    => i,
                    double d => (int)d,
                    _        => 256
                };
                variables["useFirstHalfTiles"] = startTileVal < 256 ? 1 : 0;
            }
            else
            {
                variables["useFirstHalfTiles"] = 0; // default: tiles 256-511
            }
        }

        var template = File.ReadAllText(manifest.TemplatePath);

        var baseName = !string.IsNullOrEmpty(fileNamePrefix)
            ? fileNamePrefix.Replace('.', '_')
            : manifest.ModuleId.Replace('.', '_');

        yield return new GeneratedFile
        {
            FileName = effectiveSingleton
                ? $"{baseName}.h"
                : $"{baseName}_{variables["instanceId"]}.h",
            FileType = GeneratedFileType.Header,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "header", variables)
        };

        yield return new GeneratedFile
        {
            FileName = effectiveSingleton
                ? $"{baseName}.c"
                : $"{baseName}_{variables["instanceId"]}.c",
            FileType = GeneratedFileType.Source,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "source", variables)
        };

        // Optional .asm output when the manifest declares an asmTemplate
        if (!string.IsNullOrEmpty(manifest.AsmTemplatePath) && File.Exists(manifest.AsmTemplatePath))
        {
            var asmTemplate = File.ReadAllText(manifest.AsmTemplatePath);
            yield return new GeneratedFile
            {
                FileName = effectiveSingleton
                    ? $"{baseName}.asm"
                    : $"{baseName}_{variables["instanceId"]}.asm",
                FileType = GeneratedFileType.Assembly,
                SourceModuleId = moduleId,
                Content = TemplateEngine.Render(asmTemplate, variables)
            };
        }

    /// <summary>
    /// Renders a batch module (processes all instances at once).
    /// Used for modules like text.array that generate a single file for all instances.
    /// </summary>
    public IEnumerable<GeneratedFile> RenderBatch(
        string moduleId,
        string targetId,
        IEnumerable<string> allInstancesJson,
        string? projectPath = null)
    {
        var key = Key(targetId, moduleId);
        if (!_codeGens.TryGetValue(key, out var manifest))
            yield break;

        if (!manifest.IsBatchModule)
            yield break;

        var variables = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(projectPath))
            variables["projectPath"] = projectPath;

        // Inject global variables
        foreach (var (globalKey, globalValue) in _globalVariables)
            variables[globalKey] = globalValue;

        // Parse all instances as JSON elements
        var instances = allInstancesJson
            .Select(json => System.Text.Json.JsonDocument.Parse(json).RootElement)
            .Cast<object>()
            .ToList();

        variables["arrays"] = instances;

        // Resolve other variables
        foreach (var (varName, varDef) in manifest.Variables)
        {
            switch (varDef.From)
            {
                case "allInstances":
                    // Already set as "arrays"
                    break;

                case "global":
                    if (_globalVariables.TryGetValue(varDef.Path ?? varName, out var globalValue))
                        variables[varName] = globalValue;
                    else
                        variables[varName] = varDef.Default ?? "";
                    break;

                case "computed":
                    variables[varName] = _variableResolver.EvaluateComputedExpression(varDef, variables);
                    break;
            }
        }

        var template = File.ReadAllText(manifest.TemplatePath);

        yield return new GeneratedFile
        {
            FileName = $"{manifest.ModuleId.Replace('.', '_')}.h",
            FileType = GeneratedFileType.Header,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "header", variables)
        };

        yield return new GeneratedFile
        {
            FileName = $"{manifest.ModuleId.Replace('.', '_')}.c",
            FileType = GeneratedFileType.Source,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "source", variables)
        };
    }

    public IEnumerable<ITool> GetStandaloneTools()
 => _tools.Values;

    /// <summary>
    /// Returns all user-defined modules discovered from CodeGens.
    /// Used by SceneEditor to populate the "User Modules" section.
    /// </summary>
    public IEnumerable<(string ModuleId, string DisplayName, string Category, string TargetId)> GetUserModules()
    {
        return _codeGens.Values
            .Where(cg => cg.IsUserModule)
            .Select(cg => (
                cg.ModuleId,
                cg.DisplayName ?? cg.ModuleId,
                cg.Category ?? "User",
                cg.TargetId
            ));
    }

    private static string Key(string targetId, string moduleId)
 => $"{targetId}::{moduleId}".ToLowerInvariant();

    private static string AsStringOrEmpty(object? value) => value switch
    {
        string s => s,
        System.Text.Json.JsonElement j when j.ValueKind == System.Text.Json.JsonValueKind.String
            => j.GetString() ?? "",
        _ => ""
    };

    /// <summary>
    /// Sanitizes a scene name for use in C file names.
    /// Removes spaces, special characters, and converts to valid C identifier.
    /// Example: "Scene 2" → "Scene_2", "My-Scene!" → "My_Scene"
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "scene";

        // Replace spaces and invalid chars with underscore
        var sanitized = new string(name.Select(c =>
            char.IsLetterOrDigit(c) ? c : '_'
        ).ToArray());

        // Remove consecutive underscores
        while (sanitized.Contains("__"))
            sanitized = sanitized.Replace("__", "_");

        // Trim underscores from start/end
        sanitized = sanitized.Trim('_');

        // Ensure it starts with a letter (C identifier rule)
        if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            sanitized = "scene_" + sanitized;

        return string.IsNullOrEmpty(sanitized) ? "scene" : sanitized;
    }

    private static IPaletteConverter? FindPaletteConverter(ITarget target)
    {
        var targetAssembly = target.GetType().Assembly;
        var converterType = targetAssembly.GetTypes()
            .FirstOrDefault(t => typeof(IPaletteConverter).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        if (converterType is not null)
        {
            return (IPaletteConverter)Activator.CreateInstance(converterType)!;
        }

        if (target is IPaletteConverter targetConverter)
        {
            return targetConverter;
        }

        return null;
    }
}
