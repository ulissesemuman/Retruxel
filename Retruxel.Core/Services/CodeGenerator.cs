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
/// - CodeGenerator_Splash.cs: Splash screen injection
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

        // Set target for input port resolution in VariableResolver
        _moduleRenderer.SetTarget(target);

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
        var assets      = new List<GeneratedAsset>();

        progress?.Report("INIT: Starting code generation...");

        // Inject splash screen if enabled
        InjectSplash(project);

        // Reset renderer state before generation
        _moduleRenderer.ResetState();

        // Run TextAnalyzer before module rendering
        var fontConverter   = _target.GetFontConverter();
        var graphicTilesEnd = CalculateGraphicTilesEnd(project);

        var textModuleJsons = new List<string>();
        var textResult      = _textAnalyzer.Analyze(textModuleJsons, fontConverter, graphicTilesEnd);

        if (((List<char>)textResult["missingChars"]).Count > 0)
        {
            var missingChars = (List<char>)textResult["missingChars"];
            progress?.Report($"WARN: {missingChars.Count} character(s) not in default font: {string.Join(", ", missingChars.Take(10))}");
        }

        progress?.Report($"INFO: Compact font: {textResult["fontTileCount"]} glyphs, starting at tile {textResult["fontStartTile"]}");

        var globalVariables = new Dictionary<string, object>
        {
            ["fontStartTile"]        = textResult["fontStartTile"],
            ["fontTileCount"]        = textResult["fontTileCount"],
            ["fontTileData"]         = textResult["fontTileData"],
            ["fontTranslationTable"] = textResult["fontTranslationTable"]
        };

        _moduleRenderer.SetGlobalVariables(globalVariables);

        // Pre-populate in-memory assets
        var inMemoryAssets = new Dictionary<string, Models.AssetEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in project.Assets)
            inMemoryAssets[asset.Id] = asset;
        _moduleRenderer.SetInMemoryAssets(inMemoryAssets);

        // Pre-build allocations
        foreach (var scene in project.Scenes)
        {
            try
            {
                var vram = VramAllocator.Allocate(scene, _target, project.Assets);
                if (!vram.FitsInVram)
                    progress?.Report($"WARN: Scene '{scene.SceneName}' VRAM usage exceeds target limit.");

                foreach (var (assetId, offset) in vram.TileOffsets)
                    globalVariables[$"vramOffset_{assetId}"] = offset;

                var sat = SatAllocator.Allocate(scene, _target, project);
                foreach (var (entityId, slot) in sat)
                    globalVariables[$"satIndex_{entityId}"] = slot;

                progress?.Report($"INFO: VRAM allocated — {vram.TotalTilesUsed} tiles. SAT allocated — {sat.Count} entities.");
            }
            catch (BuildException ex)
            {
                progress?.Report($"ERROR: {ex.Message}");
            }
        }

        _moduleRenderer.SetGlobalVariables(globalVariables);

        var instancesByModule  = new Dictionary<string, List<IModule>>();
        var triggersByElement  = new Dictionary<IModule, string>();
        var elementToScene     = new Dictionary<IModule, string>();
        var elementIds         = new Dictionary<IModule, string>();
        var originalJsonByModule = new Dictionary<IModule, string>();

        // Helper: instantiate and register a module
        void RegisterModuleData(
            string moduleId,
            System.Text.Json.JsonElement moduleState,
            string elementId,
            string? sceneId,
            string trigger)
        {
            IModule? moduleTemplate = null;

            if (_moduleRegistry.GraphicModules.TryGetValue(moduleId, out var gm))
                moduleTemplate = gm;
            else if (_moduleRegistry.LogicModules.TryGetValue(moduleId, out var lm))
                moduleTemplate = lm;
            else if (_moduleRegistry.AudioModules.TryGetValue(moduleId, out var am))
                moduleTemplate = am;

            if (moduleTemplate is null)
            {
                progress?.Report($"WARN: Module {moduleId} not found — skipping.");
                return;
            }

            var module = (IModule)Activator.CreateInstance(moduleTemplate.GetType())!;

            var moduleStateJson = moduleState.ValueKind != System.Text.Json.JsonValueKind.Undefined &&
                                  moduleState.ValueKind != System.Text.Json.JsonValueKind.Null
                ? moduleState.GetRawText()
                : "{}";

            module.Deserialize(moduleStateJson);

            if (!instancesByModule.ContainsKey(moduleId))
                instancesByModule[moduleId] = new List<IModule>();

            instancesByModule[moduleId].Add(module);
            originalJsonByModule[module] = moduleStateJson;

            if (sceneId is not null)
                elementToScene[module] = sceneId;

            elementIds[module]      = elementId;
            triggersByElement[module] = trigger;

            if (moduleId == "text.array")
            {
                textModuleJsons = instancesByModule["text.array"].Select(m => m.Serialize()).ToList();
                textResult      = _textAnalyzer.Analyze(textModuleJsons, fontConverter, graphicTilesEnd);

                globalVariables["fontStartTile"]        = textResult["fontStartTile"];
                globalVariables["fontTileCount"]        = textResult["fontTileCount"];
                globalVariables["fontTileData"]         = textResult["fontTileData"];
                globalVariables["fontTranslationTable"] = textResult["fontTranslationTable"];

                _moduleRenderer.SetGlobalVariables(globalVariables);
            }
        }

        // 1. Project-level modules
        foreach (var modData in project.Modules)
        {
            if (!modData.Enabled) continue;

            progress?.Report($"PROC: Loading project module {modData.ModuleId}...");

            RegisterModuleData(
                moduleId:    modData.ModuleId,
                moduleState: modData.State,
                elementId:   modData.ModuleId,
                sceneId:     null,
                trigger:     "OnStart");
        }

        // 2. Scene-level typed collections + legacy Elements
        foreach (var scene in project.Scenes)
        {
            // 2a. Scene module overrides
            foreach (var modData in scene.ModuleOverrides)
            {
                if (!modData.Enabled) continue;

                progress?.Report($"PROC: Loading scene override {modData.ModuleId} in '{scene.SceneName}'...");

                RegisterModuleData(
                    moduleId:    modData.ModuleId,
                    moduleState: modData.State,
                    elementId:   modData.ModuleId,
                    sceneId:     scene.SceneId,
                    trigger:     "OnStart");
            }

            // 2b. Typed plane layers
            foreach (var plane in scene.Planes)
            {
                var visibleLayers = plane.Layers.Where(l => l.Visible).ToList();
                if (visibleLayers.Count == 0) continue;

                progress?.Report($"PROC: Merging {visibleLayers.Count} layer(s) for plane '{plane.PlaneId}' in '{scene.SceneName}'...");

                var baseLayer = visibleLayers[0];
                int mapWidth  = baseLayer.Width;
                int mapHeight = baseLayer.Height;
                int cellCount = mapWidth * mapHeight;
                int tileSize  = _target.Specs.TileWidth;

                int nextOffset = 0;
                var layerOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var layer in visibleLayers)
                {
                    if (string.IsNullOrEmpty(layer.AssetId) || layerOffsets.ContainsKey(layer.AssetId)) continue;
                    layerOffsets[layer.AssetId] = nextOffset;
                    var asset = project.Assets.FirstOrDefault(a => a.Id == layer.AssetId);
                    nextOffset += asset?.GenerationParams?.TileCount ?? 0;
                }

                var mergedList = Enumerable.Range(0, cellCount)
                    .Select(_ => new Core.Models.TileEntry { TileIndex = -1 })
                    .ToArray();

                for (int layerIdx = visibleLayers.Count - 1; layerIdx >= 0; layerIdx--)
                {
                    var layer  = visibleLayers[layerIdx];
                    int offset = (!string.IsNullOrEmpty(layer.AssetId) && layerOffsets.TryGetValue(layer.AssetId, out var lo)) ? lo : 0;

                    for (int i = 0; i < cellCount && i < layer.Tiles.Count; i++)
                    {
                        var tile = layer.Tiles[i];
                        if (!tile.IsEmpty && mergedList[i].TileIndex < 0)
                            mergedList[i] = new Core.Models.TileEntry
                            {
                                TileIndex = tile.TileIndex + offset,
                                FlipH     = tile.FlipH,
                                FlipV     = tile.FlipV,
                                Rotation  = tile.Rotation
                            };
                    }
                }

                var distinctAssets = visibleLayers
                    .Where(l => !string.IsNullOrEmpty(l.AssetId))
                    .Select(l => l.AssetId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(id => project.Assets.FirstOrDefault(a => a.Id == id))
                    .Where(a => a?.GenerationParams?.MapIndex != null)
                    .ToList();

                string mergedAssetId;
                if (distinctAssets.Count <= 1)
                {
                    mergedAssetId = distinctAssets.Count == 1 ? distinctAssets[0]!.Id : string.Empty;
                }
                else
                {
                    mergedAssetId = $"plane_{plane.PlaneId}_merged";

                    int totalTiles  = distinctAssets.Sum(a => a!.GenerationParams!.TileCount);
                    int mergedW     = tileSize;
                    int mergedH     = totalTiles * tileSize;
                    var mergedIndex = new byte[mergedW * mergedH];

                    int destTileIdx = 0;
                    foreach (var srcAsset in distinctAssets)
                    {
                        var gp        = srcAsset!.GenerationParams!;
                        int srcW      = gp.OptimizedWidth;
                        int srcTilesX = srcW / tileSize;
                        int srcCount  = gp.TileCount;

                        for (int t = 0; t < srcCount; t++)
                        {
                            int srcTileX = t % srcTilesX;
                            int srcTileY = t / srcTilesX;

                            for (int row = 0; row < tileSize; row++)
                            {
                                int srcBase = (srcTileY * tileSize + row) * srcW + srcTileX * tileSize;
                                int dstBase = (destTileIdx * tileSize + row) * mergedW;
                                Array.Copy(gp.MapIndex, srcBase, mergedIndex, dstBase, tileSize);
                            }
                            destTileIdx++;
                        }
                    }

                    var virtualAsset = new Core.Models.AssetEntry
                    {
                        Id           = mergedAssetId,
                        FileName     = mergedAssetId + ".png",
                        RelativePath = string.Empty,
                        GenerationParams = new Core.Models.AssetGenerationParams
                        {
                            MapIndex        = mergedIndex,
                            OptimizedWidth  = mergedW,
                            OptimizedHeight = mergedH,
                            TileCount       = totalTiles,
                            Palette         = distinctAssets[0]!.GenerationParams!.Palette
                        }
                    };

                    if (!project.Assets.Any(a => a.Id == mergedAssetId))
                        project.Assets.Add(virtualAsset);
                    else
                        project.Assets[project.Assets.FindIndex(a => a.Id == mergedAssetId)] = virtualAsset;

                    inMemoryAssets[mergedAssetId] = virtualAsset;
                    _moduleRenderer.SetInMemoryAssets(inMemoryAssets);
                }

                var planeState = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    tilesAssetId = mergedAssetId,
                    mapWidth     = mapWidth,
                    mapHeight    = mapHeight,
                    paletteSlot  = plane.PaletteSlot,
                    tiles        = mergedList.Select(t => new
                    {
                        tileIndex = t.TileIndex,
                        flipH     = t.FlipH,
                        flipV     = t.FlipV,
                        rotation  = t.Rotation
                    }).ToArray()
                });

                RegisterModuleData(
                    moduleId:    "plane",
                    moduleState: planeState,
                    elementId:   visibleLayers[0].LayerId,
                    sceneId:     scene.SceneId,
                    trigger:     "OnStart");
            }

            // 2c. Typed entities
            foreach (var entity in scene.Entities)
            {
                progress?.Report($"PROC: Loading entity '{entity.Label}' in '{scene.SceneName}'...");

                // Resolve Prefab for type-level properties
                var prefab        = project.Prefabs.FirstOrDefault(p => p.PrefabId == entity.PrefabId);
                var spriteAssetId = prefab?.SpriteAssetId ?? entity.SpriteAssetId ?? string.Empty;
                var paletteSlot   = prefab?.PaletteSlot   ?? entity.PaletteSlot   ?? 1;
                var widthTiles    = prefab?.WidthTiles     ?? entity.WidthTiles     ?? 2;
                var heightTiles   = prefab?.HeightTiles    ?? entity.HeightTiles    ?? 2;

                int startTile = globalVariables.TryGetValue($"vramOffset_{spriteAssetId}", out var vramObj)
                    ? (int)vramObj : 0;
                int satIndex  = globalVariables.TryGetValue($"satIndex_{entity.EntityId}", out var satObj)
                    ? (int)satObj : 0;

                var entityState = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    spriteAssetId = spriteAssetId,
                    paletteSlot   = paletteSlot,
                    startTileX    = entity.StartTileX * _target.Specs.TileWidth,
                    startTileY    = entity.StartTileY * _target.Specs.TileHeight,
                    startTile     = startTile,
                    satIndex      = satIndex,
                    widthTiles    = widthTiles,
                    heightTiles   = heightTiles
                });

                // Use PrefabId as moduleId if it maps to a known module, else fall back to legacy EntityType
                var moduleId = !string.IsNullOrEmpty(entity.PrefabId) ? entity.PrefabId
                             : !string.IsNullOrEmpty(entity.EntityType) ? entity.EntityType
                             : "entity";

                RegisterModuleData(
                    moduleId:    moduleId,
                    moduleState: entityState,
                    elementId:   entity.EntityId,
                    sceneId:     scene.SceneId,
                    trigger:     "OnVBlank");
            }

            // 2d. Typed text arrays
            foreach (var textArray in scene.TextArrays)
            {
                progress?.Report($"PROC: Loading text array '{textArray.Label}' in '{scene.SceneName}'...");

                var textState = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    entries = textArray.Entries
                });

                RegisterModuleData(
                    moduleId:    "text.array",
                    moduleState: textState,
                    elementId:   textArray.TextId,
                    sceneId:     scene.SceneId,
                    trigger:     "OnStart");
            }

            // 2e. Legacy flat Elements
            foreach (var elementData in scene.Elements)
            {
                var elementIdShort = elementData.ElementId.Length > 8
                    ? elementData.ElementId.Substring(0, 8)
                    : elementData.ElementId;
                progress?.Report($"PROC: Loading legacy element {elementIdShort}...");

                RegisterModuleData(
                    moduleId:    elementData.ModuleId,
                    moduleState: elementData.ModuleState,
                    elementId:   elementData.ElementId,
                    sceneId:     scene.SceneId,
                    trigger:     string.IsNullOrEmpty(elementData.Trigger) ? "OnStart" : elementData.Trigger);
            }
        }

        var presentModuleIds = instancesByModule.Keys.ToHashSet();

        // Generate code for each module type
        foreach (var (moduleId, instances) in instancesByModule)
        {
            if (moduleId == "gamevar" || moduleId == "text.array")
                continue;

            progress?.Report($"PROC: Generating code for {instances.Count} {moduleId} instance(s)...");

            foreach (var module in instances)
            {
                var baseJson         = originalJsonByModule.TryGetValue(module, out var orig) ? orig : module.Serialize();
                var contextualModule = InjectContextFlags(module, presentModuleIds);
                var moduleJson       = MergeJson(baseJson, contextualModule.Serialize());

                List<GeneratedFile> generatedFiles;

                if (_moduleRenderer.CanRender(module.ModuleId, project.TargetId))
                {
                    var policy     = _target.GetModulePolicyOverrides()
                        .TryGetValue(module.ModuleId, out var p) ? p : module.SingletonPolicy;
                    var isSingleton = policy != SingletonPolicy.Multiple;

                    var moduleScene = elementToScene.TryGetValue(module, out var sceneId)
                        ? project.Scenes.FirstOrDefault(s => s.SceneId == sceneId)
                        : null;

                    generatedFiles = _moduleRenderer.Render(
                        module.ModuleId, project.TargetId, moduleJson,
                        isSingleton, project.ProjectPath, moduleScene).ToList();
                    progress?.Report($"INFO: {module.ModuleId} generated via ModuleRenderer.");
                }
                else if (_target.GenerateCodeForModule(contextualModule).ToList() is var targetFiles && targetFiles.Count > 0)
                {
                    generatedFiles = targetFiles;
                    progress?.Report($"INFO: {module.ModuleId} generated via target.");
                }
                else
                {
                    generatedFiles = module switch
                    {
                        ILogicModule lm   => lm.GenerateCode().ToList(),
                        IGraphicModule gm => gm.GenerateCode().ToList(),
                        IAudioModule am   => am.GenerateCode().ToList(),
                        _                 => []
                    };

                    if (generatedFiles.Count == 0)
                        progress?.Report($"WARN: No code generated for {moduleId} — skipping.");
                    else
                        progress?.Report($"INFO: {moduleId} generated via plugin fallback.");
                }

                foreach (var file in generatedFiles)
                {
                    if (sourceFiles.Any(f => f.FileName == file.FileName))
                    {
                        progress?.Report($"INFO: Skipping duplicate file '{file.FileName}' for {moduleId}.");
                        continue;
                    }

                    if (elementToScene.TryGetValue(module, out var sceneId))
                    {
                        file.SourceSceneId   = sceneId;
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

        ValidateTileConflicts(instancesByModule, progress);

        // Generate scene files
        foreach (var scene in project.Scenes)
        {
            var sceneSpecificFiles = sourceFiles
                .Where(f => f.SourceSceneId == scene.SceneId)
                .ToList();

            var sceneFiles = _moduleRenderer.RenderSceneFiles(project.TargetId, scene, sceneSpecificFiles, _target, progress);
            foreach (var file in sceneFiles)
                sourceFiles.Add(file);

            progress?.Report($"INFO: Scene '{scene.SceneName}' generated.");
        }

        if (instancesByModule.ContainsKey("gamevar"))
            sourceFiles.AddRange(GenerateGameVarsFile(project, instancesByModule["gamevar"], progress));

        if (instancesByModule.ContainsKey("text.array"))
            sourceFiles.AddRange(GenerateTextArrayFile(project, instancesByModule["text.array"], progress));
        else
            progress?.Report("INFO: No text.array modules found - skipping text system generation.");

        var engineFiles = _target.GenerateEngineRuntime();
        sourceFiles.AddRange(engineFiles);
        if (engineFiles.Any())
            progress?.Report($"INFO: {engineFiles.Count()} engine runtime files generated.");

        var systemFiles = _target.GenerateSystemFiles();
        sourceFiles.AddRange(systemFiles);
        if (systemFiles.Any())
            progress?.Report($"INFO: {systemFiles.Count()} system files generated.");

        var mainFile = _moduleRenderer.RenderMainFile(project.TargetId, project, sourceFiles, triggersByElement, progress);
        if (mainFile is not null)
        {
            progress?.Report("INFO: main.c generated via ModuleRenderer.");
        }
        else
        {
            mainFile = _target.GenerateMainFile(project, sourceFiles);
            progress?.Report("INFO: main.c generated via target fallback.");
        }

        sourceFiles.Insert(0, mainFile);

        progress?.Report($"INFO: {sourceFiles.Count} source files generated.");
        progress?.Report($"INFO: {assets.Count} assets generated.");

        var buildContext = new BuildContext
        {
            TargetId        = project.TargetId,
            SourceFiles     = sourceFiles,
            Assets          = assets,
            OutputDirectory = outputDirectory
        };

        var diagnosticInput = new BuildDiagnosticInput(
            sourceFiles, assets, buildContext.BuildParameters, _target.Specs);

        var diagnostics = _target.GetBuildDiagnostics(diagnosticInput);
        if (diagnostics is not null)
        {
            buildContext.Diagnostics = diagnostics;
            progress?.Report($"INFO: Build diagnostics generated — {diagnostics.Metrics.Count} metrics.");
        }

        return buildContext;
    }
}
