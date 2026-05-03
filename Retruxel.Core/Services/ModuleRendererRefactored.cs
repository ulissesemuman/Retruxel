using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and renders declarative CodeGens from the plugins/CodeGens/ folder.
/// Delegates to specialized classes for variable resolution, filtering, and discovery.
/// </summary>
public class ModuleRendererRefactored
{
    private readonly string _pluginsPath;
    private readonly Dictionary<string, ITool> _tools;
    private readonly Dictionary<string, CodeGenManifest> _codeGens;
    private readonly VariableResolver _variableResolver;
    private Assembly? _targetAssembly;
    private ModuleRegistry? _moduleRegistry;

    private readonly Dictionary<string, int> _instanceCounters = new();

    public ModuleRendererRefactored(string pluginsPath, Assembly? targetAssembly = null, IProgress<string>? progress = null)
    {
        _pluginsPath = pluginsPath;
        _targetAssembly = targetAssembly;
        _tools = ToolDiscovery.DiscoverTools(pluginsPath);
        _codeGens = CodeGenDiscovery.DiscoverCodeGens(pluginsPath, progress);
        _variableResolver = new VariableResolver(_tools, targetAssembly);
    }

    public void ResetState() => _instanceCounters.Clear();

    public void SetTargetAssembly(Assembly? targetAssembly)
    {
        _targetAssembly = targetAssembly;
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
                        variables[varName] = project.InitialSceneId;
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
        IProgress<string>? progress = null)
    {
        var key = Key(targetId, "scene");

        if (!_codeGens.TryGetValue(key, out var manifest))
            yield break;

        var variables = new Dictionary<string, object>
        {
            ["sceneName"] = scene.SceneName,
            ["sceneNameUpper"] = scene.SceneName.ToUpperInvariant()
        };

        var sceneModules = new List<IModule>();
        foreach (var element in scene.Elements)
        {
            if (_moduleRegistry == null) continue;

            IModule? moduleTemplate = null;
            if (_moduleRegistry.GraphicModules.TryGetValue(element.ModuleId, out var gm))
                moduleTemplate = gm;
            else if (_moduleRegistry.LogicModules.TryGetValue(element.ModuleId, out var lm))
                moduleTemplate = lm;
            else if (_moduleRegistry.AudioModules.TryGetValue(element.ModuleId, out var am))
                moduleTemplate = am;

            if (moduleTemplate == null) continue;

            var moduleType = moduleTemplate.GetType();
            var module = (IModule)Activator.CreateInstance(moduleType)!;
            
            var moduleStateJson = element.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                                  element.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Null
                ? element.ModuleState.GetRawText()
                : "{}";
            
            module.Deserialize(moduleStateJson);
            sceneModules.Add(module);
        }

        foreach (var (varName, varDef) in manifest.Variables)
        {
            if (varDef.From == "scene")
            {
                if (varDef.Path == "name")
                    variables[varName] = varDef.Transform == "upper" 
                        ? scene.SceneName.ToUpperInvariant() 
                        : scene.SceneName;
            }
            else if (varDef.From == "sceneModules")
            {
                variables[varName] = ModuleFilterProcessor.ProcessSceneModules(sceneModules, varDef);
            }
        }

        var template = File.ReadAllText(manifest.TemplatePath);

        yield return new GeneratedFile
        {
            FileName = $"scene_{scene.SceneName}.h",
            Content = TemplateEngine.RenderBlock(template, "header", variables),
            FileType = GeneratedFileType.Header,
            SourceModuleId = "scene"
        };

        yield return new GeneratedFile
        {
            FileName = $"scene_{scene.SceneName}.c",
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
        string? projectPath = null)
    {
        var key = Key(targetId, moduleId);
        if (!_codeGens.TryGetValue(key, out var manifest))
            yield break;

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

    public IEnumerable<ITool> GetStandaloneTools()
        => _tools.Values;

    private static string Key(string targetId, string moduleId)
        => $"{targetId}::{moduleId}".ToLowerInvariant();
}
