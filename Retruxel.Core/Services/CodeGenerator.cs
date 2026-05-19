using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Retruxel.Core.Services;

/// <summary>
/// Orchestrates code generation across all active modules in a project.
/// Collects generated files from each module's target-specific translator
/// and assembles a BuildContext ready for the IToolchain to compile.
/// 
/// Functionality split across partial classes:
/// - CodeGenerator_Modules.cs: Module processing and context injection
/// - CodeGenerator_Batch.cs: Batch module processing (GameVars, TextArray)
/// - CodeGenerator_Validation.cs: Validation logic (tile conflicts)
/// - CodeGenerator_Helpers.cs: Utility methods
/// </summary>
public partial class CodeGenerator
{
    private readonly ModuleRegistry _moduleRegistry;
    private readonly ModuleRenderer _moduleRenderer;
    private readonly ITarget _target;
    private readonly TextAnalyzer _textAnalyzer = new();

    public CodeGenerator(ModuleRegistry moduleRegistry, ModuleRenderer moduleRenderer, ITarget target)
    {
        _moduleRegistry = moduleRegistry;
        _moduleRenderer = moduleRenderer;
        _target = target;

        // Set target assembly for ModuleRenderer to discover IToolExtension implementations
        _moduleRenderer.SetTargetAssembly(target.GetType().Assembly);

        // Set module registry for dynamic singleton checking
        _moduleRenderer.SetModuleRegistry(moduleRegistry);
    }

    /// <summary>
    /// Generates all source files and assets for the given project.
    /// Returns a BuildContext ready to be passed to IToolchain.BuildAsync.
    /// </summary>
    public async Task<BuildContext> GenerateAsync(
        RetruxelProject project,
        string outputDirectory,
        IProgress<string>? progress = null)
    {
        var sourceFiles = new List<GeneratedFile>();
        var assets = new List<GeneratedAsset>();

        progress?.Report("INIT: Starting code generation...");

        // Inject splash screen if enabled
        InjectSplash(project);

        // Reset renderer state before generation
        _moduleRenderer.ResetState();

        // Run TextAnalyzer before module rendering
        var fontConverter = _target.GetFontConverter();
        var graphicTilesEnd = CalculateGraphicTilesEnd(project);

        // Initial analysis with empty module list
        var textModuleJsons = new List<string>();
        var textResult = _textAnalyzer.Analyze(textModuleJsons, fontConverter, graphicTilesEnd);

        if (((List<char>)textResult["missingChars"]).Count > 0)
        {
            var missingChars = (List<char>)textResult["missingChars"];
            progress?.Report($"WARN: {missingChars.Count} character(s) not in default font: {string.Join(", ", missingChars.Take(10))}");
        }

        progress?.Report($"INFO: Compact font: {textResult["fontTileCount"]} glyphs, starting at tile {textResult["fontStartTile"]}");

        // Inject text analysis results into global variables for CodeGen templates
        var globalVariables = new Dictionary<string, object>
        {
            ["fontStartTile"] = textResult["fontStartTile"],
            ["fontTileCount"] = textResult["fontTileCount"],
            ["fontTileData"] = textResult["fontTileData"],
            ["fontTranslationTable"] = textResult["fontTranslationTable"]
        };

        _moduleRenderer.SetGlobalVariables(globalVariables);

        // Collect all module instances from scenes, grouped by trigger
        var instancesByModule = new Dictionary<string, List<IModule>>();
        var triggersByElement = new Dictionary<IModule, string>(); // Track trigger for each instance
        var elementToScene = new Dictionary<IModule, string>(); // Track which scene each module belongs to
        var elementIds = new Dictionary<IModule, string>(); // Track element IDs

        foreach (var scene in project.Scenes)
        {
            foreach (var elementData in scene.Elements)
            {
                var elementIdShort = elementData.ElementId.Length > 8
                    ? elementData.ElementId.Substring(0, 8)
                    : elementData.ElementId;
                progress?.Report($"PROC: Loading element {elementIdShort}...");

                IModule? moduleTemplate = null;

                if (_moduleRegistry.GraphicModules.TryGetValue(elementData.ModuleId, out var gm))
                    moduleTemplate = gm;
                else if (_moduleRegistry.LogicModules.TryGetValue(elementData.ModuleId, out var lm))
                    moduleTemplate = lm;
                else if (_moduleRegistry.AudioModules.TryGetValue(elementData.ModuleId, out var am))
                    moduleTemplate = am;

                if (moduleTemplate is null)
                {
                    progress?.Report($"WARN: Module {elementData.ModuleId} not found — skipping.");
                    continue;
                }

                var moduleType = moduleTemplate.GetType();
                var module = (IModule)Activator.CreateInstance(moduleType)!;

                var moduleStateJson = elementData.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                                      elementData.ModuleState.ValueKind != System.Text.Json.JsonValueKind.Null
                    ? elementData.ModuleState.GetRawText()
                    : "{}";

                module.Deserialize(moduleStateJson);

                if (!instancesByModule.ContainsKey(elementData.ModuleId))
                    instancesByModule[elementData.ModuleId] = new List<IModule>();

                instancesByModule[elementData.ModuleId].Add(module);

                // Track which scene this module belongs to
                elementToScene[module] = scene.SceneId;
                elementIds[module] = elementData.ElementId;

                // Re-run TextAnalyzer with actual module instances for accurate character extraction
                if (elementData.ModuleId == "text.array")
                {
                    var allTextModules = instancesByModule.ContainsKey("text.array")
                        ? instancesByModule["text.array"]
                        : new List<IModule>();

                    textModuleJsons = allTextModules.Select(m => m.Serialize()).ToList();
                    textResult = _textAnalyzer.Analyze(textModuleJsons, fontConverter, graphicTilesEnd);

                    globalVariables["fontStartTile"] = textResult["fontStartTile"];
                    globalVariables["fontTileCount"] = textResult["fontTileCount"];
                    globalVariables["fontTileData"] = textResult["fontTileData"];
                    globalVariables["fontTranslationTable"] = textResult["fontTranslationTable"];

                    _moduleRenderer.SetGlobalVariables(globalVariables);
                }

                // Track trigger for this instance (default to OnStart if not set)
                var trigger = string.IsNullOrEmpty(elementData.Trigger) ? "OnStart" : elementData.Trigger;
                triggersByElement[module] = trigger;
            }
        }

        // Build a set of all module IDs present in the project.
        // Used to inject context flags into modules that depend on other modules.
        var presentModuleIds = instancesByModule.Keys.ToHashSet();

        // Generate code for each module type (passing all instances)
        foreach (var (moduleId, instances) in instancesByModule)
        {
            // Skip batch modules — they are processed separately
            if (moduleId == "gamevar" || moduleId == "text.array")
                continue;

            progress?.Report($"PROC: Generating code for {instances.Count} {moduleId} instance(s)...");

            foreach (var module in instances)
            {
                // Inject context flags into the module's JSON before code generation.
                var contextualModule = InjectContextFlags(module, presentModuleIds);
                var moduleJson = contextualModule.Serialize();

                List<GeneratedFile> generatedFiles;

                // Priority 1: Try ModuleRenderer (declarative CodeGens)
                if (_moduleRenderer.CanRender(module.ModuleId, project.TargetId))
                {
                    // Get effective singleton policy from target override or module default
                    var policy = _target.GetModulePolicyOverrides()
                        .TryGetValue(module.ModuleId, out var p) ? p : module.SingletonPolicy;
                    var isSingleton = policy != SingletonPolicy.Multiple;

                    // Get the scene this module belongs to
                    var moduleScene = elementToScene.TryGetValue(module, out var sceneId)
                        ? project.Scenes.FirstOrDefault(s => s.SceneId == sceneId)
                        : null;

                    generatedFiles = _moduleRenderer.Render(
                        module.ModuleId,
                        project.TargetId,
                        moduleJson,
                        isSingleton,
                        project.ProjectPath,
                        moduleScene).ToList();
                    progress?.Report($"INFO: {module.ModuleId} generated via ModuleRenderer.");
                }
                // Priority 2: Ask target to translate (legacy fallback)
                else if (_target.GenerateCodeForModule(contextualModule).ToList() is var targetFiles && targetFiles.Count > 0)
                {
                    generatedFiles = targetFiles;
                    progress?.Report($"INFO: {module.ModuleId} generated via target.");
                }
                // Priority 3: Module's own GenerateCode() (external plugins)
                else
                {
                    generatedFiles = module switch
                    {
                        ILogicModule lm => lm.GenerateCode().ToList(),
                        IGraphicModule gm => gm.GenerateCode().ToList(),
                        IAudioModule am => am.GenerateCode().ToList(),
                        _ => []
                    };

                    if (generatedFiles.Count == 0)
                        progress?.Report($"WARN: No code generated for {moduleId} — skipping.");
                    else
                        progress?.Report($"INFO: {moduleId} generated via plugin fallback.");
                }

                // Deduplicate by filename — singleton modules (entity, enemy, scroll, etc.)
                // use fixed filenames and must not be compiled more than once.
                // Multi-instance modules (text.display) use unique names via instance counter.
                foreach (var file in generatedFiles)
                {
                    if (sourceFiles.Any(f => f.FileName == file.FileName))
                    {
                        progress?.Report($"INFO: Skipping duplicate file '{file.FileName}' for {moduleId}.");
                        continue;
                    }

                    // Tag file with scene ID if module belongs to a scene
                    if (elementToScene.TryGetValue(module, out var sceneId))
                    {
                        file.SourceSceneId = sceneId;
                        file.SourceElementId = elementIds[module];
                    }

                    sourceFiles.Add(file);
                }

                if (module is IGraphicModule gfxModule)
                    assets.AddRange(gfxModule.GenerateAssets());
                else if (module is IAudioModule audioModule)
                    assets.AddRange(audioModule.GenerateAssets());
                else if (module is ILogicModule logicModule)
                    assets.AddRange(logicModule.GenerateAssets());
            }
        }

        // Validate tile conflicts between tilemap and text.display
        ValidateTileConflicts(instancesByModule, progress);

        // Generate scene files with only the modules that belong to each scene
        foreach (var scene in project.Scenes)
        {
            // Filter sourceFiles to only include files from this specific scene
            var sceneSpecificFiles = sourceFiles
                .Where(f => f.SourceSceneId == scene.SceneId)
                .ToList();

            var sceneFiles = _moduleRenderer.RenderSceneFiles(project.TargetId, scene, sceneSpecificFiles, _target, progress);
            foreach (var file in sceneFiles)
            {
                sourceFiles.Add(file);
            }
            progress?.Report($"INFO: Scene '{scene.SceneName}' generated.");
        }

        // Generate GameVars file if gamevar module is present
        if (instancesByModule.ContainsKey("gamevar"))
        {
            var gameVarFiles = GenerateGameVarsFile(project, instancesByModule["gamevar"], progress);
            sourceFiles.AddRange(gameVarFiles);
        }

        // Generate TextArray file if text.array module is present
        if (instancesByModule.ContainsKey("text.array"))
        {
            var textArrayFiles = GenerateTextArrayFile(project, instancesByModule["text.array"], progress);
            sourceFiles.AddRange(textArrayFiles);
        }
        else
        {
            // No text.array modules - skip text system generation
            progress?.Report("INFO: No text.array modules found - skipping text system generation.");
        }

        // Generate target-specific main entry point
        // First, generate engine runtime files (engine.h, engine.c) if target provides them
        var engineFiles = _target.GenerateEngineRuntime();
        sourceFiles.AddRange(engineFiles);
        if (engineFiles.Any())
            progress?.Report($"INFO: {engineFiles.Count()} engine runtime files generated.");

        // Check if target wants to inject additional files (like splash screens)
        var systemFiles = _target.GenerateSystemFiles();
        sourceFiles.AddRange(systemFiles);
        if (systemFiles.Any())
            progress?.Report($"INFO: {systemFiles.Count()} system files generated.");

        // Priority 1: Try declarative main.c generation via ModuleRenderer
        var mainFile = _moduleRenderer.RenderMainFile(project.TargetId, project, sourceFiles, triggersByElement, progress);
        if (mainFile is not null)
        {
            progress?.Report("INFO: main.c generated via ModuleRenderer.");
        }
        else
        {
            // Priority 2: Fallback to target's hardcoded GenerateMainFile()
            mainFile = _target.GenerateMainFile(project, sourceFiles);
            progress?.Report("INFO: main.c generated via target fallback.");
        }

        sourceFiles.Insert(0, mainFile);

        progress?.Report($"INFO: {sourceFiles.Count} source files generated.");
        progress?.Report($"INFO: {assets.Count} assets generated.");

        var buildContext = new BuildContext
        {
            TargetId = project.TargetId,
            SourceFiles = sourceFiles,
            Assets = assets,
            OutputDirectory = outputDirectory,
            BuildParameters = project.Parameters
                .Where(p => p.Key.StartsWith("target."))
                .ToDictionary(p => p.Key.Replace("target.", ""), p => p.Value)
        };

        // Generate build diagnostics if target supports it
        var diagnosticInput = new BuildDiagnosticInput(
            sourceFiles,
            assets,
            buildContext.BuildParameters,
            _target.Specs);

        var diagnostics = _target.GetBuildDiagnostics(diagnosticInput);
        if (diagnostics is not null)
        {
            buildContext.Diagnostics = diagnostics;
            progress?.Report($"INFO: Build diagnostics generated — {diagnostics.Metrics.Count} metrics.");
        }

        return buildContext;
    }
}
