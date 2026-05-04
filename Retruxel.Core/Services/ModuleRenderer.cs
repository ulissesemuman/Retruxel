using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
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

    public ModuleRenderer(string pluginsPath, Assembly? targetAssembly = null, IProgress<string>? progress = null)
    {
        _pluginsPath = pluginsPath;
        _targetAssembly = targetAssembly;
        _tools = ToolDiscovery.DiscoverTools(pluginsPath);
        _codeGens = CodeGenDiscovery.DiscoverCodeGens(pluginsPath, progress);
        _variableResolver = new VariableResolver(_tools, targetAssembly);
    }

    public void ResetState() => _instanceCounters.Clear();

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
                    variables[varName] = varDef.Default ?? new string[0];
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
        System.Diagnostics.Debug.WriteLine($"[RenderSceneFiles] START for scene '{scene.SceneName}'");
        System.Diagnostics.Debug.WriteLine($"[RenderSceneFiles] Scene has {scene.PaletteSlots.Count} palette slots");
        
        var key = Key(targetId, "scene");

        if (!_codeGens.TryGetValue(key, out var manifest))
        {
            System.Diagnostics.Debug.WriteLine($"[RenderSceneFiles] No scene codegen found for key: {key}");
            yield break;
        }
        
        System.Diagnostics.Debug.WriteLine($"[RenderSceneFiles] Found scene codegen manifest");

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
                System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] Resolving palette slot {varDef.SlotIndex.Value} for variable '{varName}'");
                
                var slotIndex = varDef.SlotIndex.Value;
                System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] Scene has {scene.PaletteSlots.Count} palette slots");
                
                if (slotIndex < scene.PaletteSlots.Count)
                {
                    var slot = scene.PaletteSlots[slotIndex];
                    System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] Slot {slotIndex} has {slot.Colors.Count} colors");
                    
                    var converter = FindPaletteConverter(target);
                    System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] Converter found: {converter != null}");
                    
                    if (converter is not null)
                    {
                        var bytes = converter.ConvertColors(slot.Colors);
                        variables[varName] = string.Join(", ", bytes.Select(b => $"0x{b:X2}"));
                        System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] Generated palette hex: {variables[varName]}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] ERROR: No palette converter found!");
                        variables[varName] = "0x00";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ModuleRenderer] ERROR: Slot index {slotIndex} out of range!");
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

        var tilemapInits = moduleFiles
            .Where(f => f.FileType == GeneratedFileType.Header && f.SourceModuleId == "tilemap")
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

        variables["paletteInits"] = paletteInits;
        variables["tilemapInits"] = tilemapInits;
        variables["textStaticInits"] = textStaticInits;
        variables["hasGraphicModules"] = paletteInits.Count > 0 || tilemapInits.Count > 0 || textStaticInits.Count > 0;

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
        SceneData? currentScene = null)
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
            if (!_instanceCounters.ContainsKey(key))
                _instanceCounters[key] = 0;

            var instanceId = _instanceCounters[key]++;
            variables["instanceId"] = instanceId;
            variables["isFirstInstance"] = instanceId == 0;
        }

        var template = File.ReadAllText(manifest.TemplatePath);

        yield return new GeneratedFile
        {
            FileName = effectiveSingleton
                ? $"{manifest.ModuleId.Replace('.', '_')}.h"
                : $"{manifest.ModuleId.Replace('.', '_')}_{variables["instanceId"]}.h",
            FileType = GeneratedFileType.Header,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "header", variables)
        };

        yield return new GeneratedFile
        {
            FileName = effectiveSingleton
                ? $"{manifest.ModuleId.Replace('.', '_')}.c"
                : $"{manifest.ModuleId.Replace('.', '_')}_{variables["instanceId"]}.c",
            FileType = GeneratedFileType.Source,
            SourceModuleId = moduleId,
            Content = TemplateEngine.RenderBlock(template, "source", variables)
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
        System.Diagnostics.Debug.WriteLine($"[FindPaletteConverter] Searching for IPaletteConverter in assembly: {target.GetType().Assembly.GetName().Name}");
        
        var targetAssembly = target.GetType().Assembly;
        var converterType = targetAssembly.GetTypes()
            .FirstOrDefault(t => typeof(IPaletteConverter).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        if (converterType is not null)
        {
            System.Diagnostics.Debug.WriteLine($"[FindPaletteConverter] Found converter type: {converterType.Name}");
            return (IPaletteConverter)Activator.CreateInstance(converterType)!;
        }

        System.Diagnostics.Debug.WriteLine($"[FindPaletteConverter] No converter found, checking if target implements IPaletteConverter");
        if (target is IPaletteConverter targetConverter)
        {
            System.Diagnostics.Debug.WriteLine($"[FindPaletteConverter] Target itself implements IPaletteConverter");
            return targetConverter;
        }

        System.Diagnostics.Debug.WriteLine($"[FindPaletteConverter] ERROR: No palette converter available!");
        return null;
    }
}
