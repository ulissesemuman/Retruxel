using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
    private readonly ActionRegistry? _actionRegistry;

    public CodeGenerator(ModuleRegistry moduleRegistry, ModuleRenderer moduleRenderer, ITarget target,
        ActionRegistry? actionRegistry = null)
    {
        _moduleRegistry = moduleRegistry;
        _moduleRenderer = moduleRenderer;
        _target = target;
        _actionRegistry = actionRegistry;

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
                var vram = VramAllocator.Allocate(scene, _target, project.Assets, project);
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
            string trigger,
            string? overrideCodegenModuleId = null)
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

            // If the codegen should use a different moduleId (e.g. prefab name → entity DLL),
            // track the override so the render loop uses the right codegen folder.
            var effectiveModuleId = overrideCodegenModuleId ?? moduleId;

            if (!instancesByModule.ContainsKey(effectiveModuleId))
                instancesByModule[effectiveModuleId] = new List<IModule>();

            instancesByModule[effectiveModuleId].Add(module);
            originalJsonByModule[module] = moduleStateJson;

            if (sceneId is not null)
                elementToScene[module] = sceneId;

            elementIds[module]        = elementId;
            triggersByElement[module] = trigger;

            if (effectiveModuleId == "text.array")
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

        // 2. Scene-level typed collections
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

                // Resolve collision settings from visible layers.
                // First layer with HasCollision = true wins for the action.
                // solidTiles is a placeholder — per-tile marking is owned by the TilemapEditor state.
                var collisionLayer   = visibleLayers.FirstOrDefault(l => l.HasCollision);
                bool hasCollision    = collisionLayer is not null;
                string collisionAction      = collisionLayer?.CollisionAction ?? string.Empty;
                string collisionActionLabel = string.IsNullOrEmpty(collisionAction) ? "solid" : collisionAction;
                string collisionActionUpper = collisionActionLabel.ToUpperInvariant();
                var solidTilesUnion  = visibleLayers
                    .Where(l => l.HasCollision)
                    .SelectMany(_ => Array.Empty<int>())
                    .Distinct()
                    .ToArray();

                var planeState = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    tilesAssetId         = mergedAssetId,
                    mapWidth             = mapWidth,
                    mapHeight            = mapHeight,
                    paletteSlot          = plane.PaletteSlot,
                    hasCollision         = hasCollision,
                    collisionAction      = collisionAction,
                    collisionActionLabel = collisionActionLabel,
                    collisionActionUpper = collisionActionUpper,
                    solidTiles           = solidTilesUnion,
                    tiles                = mergedList.Select(t => new
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
            var entityInstanceCounter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

                // Build action instances — auto-inject transitive dependencies first.
                var prefabIdForCodegen = prefab?.PrefabId ?? string.Empty;
                var actionsForCodegen = new List<object>();

                // Collect all actions including auto-injected dependencies (de-duplicated)
                var effectiveActions = new List<ActionInstance>();
                if (prefab?.Actions is { Count: > 0 } prefabActions)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var actionInstance in prefabActions)
                    {
                        // Inject transitive dependencies before the action itself
                        if (_actionRegistry is not null)
                        {
                            foreach (var depId in _actionRegistry.GetAllDependencies(actionInstance.ActionId))
                            {
                                if (!seen.Add(depId)) continue;
                                // Only add if not already in prefab actions and not a scene action
                                var alreadyInPrefab = prefabActions.Any(a => a.ActionId.Equals(depId, StringComparison.OrdinalIgnoreCase));
                                var isSceneAction   = scene.SceneActions.Any(a => a.ActionId.Equals(depId, StringComparison.OrdinalIgnoreCase))
                                                   && !(prefab?.IgnoredSceneActions.Contains(depId, StringComparer.OrdinalIgnoreCase) ?? false);
                                if (!alreadyInPrefab && !isSceneAction)
                                    effectiveActions.Add(new ActionInstance { ActionId = depId });
                            }
                        }
                        if (seen.Add(actionInstance.ActionId))
                            effectiveActions.Add(actionInstance);
                    }
                }

                // Add scene-scoped actions that this prefab doesn't ignore
                // and doesn't already define explicitly (prefab takes priority over scene).
                var prefabActionIds = effectiveActions
                    .Select(a => a.ActionId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var sceneActionsForEntity = scene.SceneActions
                    .Where(sa => !prefabActionIds.Contains(sa.ActionId))
                    .Where(sa => !(prefab?.IgnoredSceneActions.Contains(sa.ActionId, StringComparer.OrdinalIgnoreCase) ?? false))
                    .ToList();

                if (effectiveActions.Count > 0 || sceneActionsForEntity.Count > 0)
                {
                    var allActions = effectiveActions.Concat(sceneActionsForEntity);
                    foreach (var actionInstance in allActions)
                    {
                        var actionDef = _actionRegistry?.GetById(actionInstance.ActionId);
                        // Merge defaults with user-configured values
                        var resolvedParams = new Dictionary<string, object>();
                        if (actionDef is not null)
                        {
                            foreach (var paramDef in actionDef.Parameters)
                                resolvedParams[paramDef.Name] = paramDef.Default;
                        }
                        foreach (var (k, v) in actionInstance.Parameters)
                            resolvedParams[k] = v;

                        var actionBody    = "";
                        var actionDecl    = "";
                        var actionReset   = "";
                        var actionPhysics = "";
                        var templatePath = _actionRegistry?.GetTemplatePath(actionInstance.ActionId, project.TargetId);
                        if (templatePath is not null && File.Exists(templatePath))
                        {
                            var codegenModuleId = !string.IsNullOrEmpty(prefabIdForCodegen) ? prefabIdForCodegen : "entity";
                            if (!entityInstanceCounter.TryGetValue(codegenModuleId, out var currentCount))
                                currentCount = 0;
                            var actionTemplate = File.ReadAllText(templatePath);
                            var paramVars = resolvedParams
                                .ToDictionary(kv => $"params.{kv.Key}", kv => kv.Value);
                            paramVars["params"]   = (object)resolvedParams;
                            paramVars["entityId"] = currentCount;
                            actionBody  = TemplateEngine.Render(TemplateEngine.ExtractBlock(actionTemplate, "source"), paramVars);
                            actionDecl  = TemplateEngine.Render(TemplateEngine.ExtractBlock(actionTemplate, "header"), paramVars);
                            // reset block is optional — only some actions define it
                            try { actionReset = TemplateEngine.Render(TemplateEngine.ExtractBlock(actionTemplate, "reset"), paramVars); }
                            catch { actionReset = ""; }
                            // physics block is optional — gravity defines it, others don't
                            try { actionPhysics = TemplateEngine.Render(TemplateEngine.ExtractBlock(actionTemplate, "physics"), paramVars); }
                            catch { actionPhysics = ""; }
                        }

                        actionsForCodegen.Add(new
                        {
                            actionGuid    = actionInstance.InstanceId,
                            actionId      = actionInstance.ActionId,
                            parameters    = resolvedParams,
                            actionBody,
                            actionDecl,
                            actionReset,
                            actionPhysics
                        });
                    }
                }

                // Build input mappings: button → list of action function calls.
                // Resolves DevkitConst from project.InputPorts and action name from instances.
                var inputMappingsForCodegen = new List<object>();
                if (prefab?.InputMapping is { } inputMapping)
                {
                    var port = project.InputPorts.FirstOrDefault(p => p.Id == inputMapping.PortId);
                    if (port is not null)
                    {
                        foreach (var (buttonId, instanceIds) in inputMapping.ButtonMappings)
                        {
                            var button = port.Buttons.FirstOrDefault(b => b.Id == buttonId);
                            if (button is null) continue;

                            var calls = instanceIds
                                .Select(iid => prefab.Actions.FirstOrDefault(a => a.InstanceId == iid))
                                .Where(a => a is not null)
                                .Select(a =>
                                {
                                    var def = _actionRegistry?.GetById(a!.ActionId);
                                    // Merge defaults + overrides to check onPress
                                    var p = new Dictionary<string, object>();
                                    if (def is not null)
                                        foreach (var pd in def.Parameters)
                                            p[pd.Name] = pd.Default;
                                    foreach (var (k, v) in a!.Parameters)
                                        p[k] = v;

                                    bool isOnPress = p.TryGetValue("onPress", out var op) && op is bool b && b;
                                    return new
                                    {
                                        actionId  = a.ActionId,
                                        callArgs  = GetActionCallArgs(a.ActionId, buttonId),
                                        keysFunc  = isOnPress ? "SMS_getKeysPressed" : "SMS_getKeysStatus"
                                    };
                                })
                                .ToList<object>();

                            if (calls.Count == 0) continue;

                            inputMappingsForCodegen.Add(new
                            {
                                devkitConst = button.DevkitConst,
                                buttonLabel = button.Label,
                                actionCalls = calls
                            });
                        }
                    }
                }

                var hasPrefabActions   = actionsForCodegen.Count > 0;
                var hasInputMapping    = inputMappingsForCodegen.Count > 0;

                var entityState = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    spriteAssetId      = spriteAssetId,
                    paletteSlot        = paletteSlot,
                    startTileX         = entity.StartTileX * _target.Specs.TileWidth,
                    startTileY         = entity.StartTileY * _target.Specs.TileHeight,
                    startTile          = startTile,
                    satIndex           = satIndex,
                    widthTiles         = widthTiles,
                    heightTiles        = heightTiles,
                    prefabId           = prefabIdForCodegen,
                    hasPrefabActions   = hasPrefabActions,
                    hasInputMapping    = hasInputMapping,
                    actions            = actionsForCodegen,
                    inputMappings      = inputMappingsForCodegen
                });

                // Use PrefabId as moduleId if it maps to a known module, else fall back to legacy EntityType
                var moduleId = !string.IsNullOrEmpty(entity.PrefabId) ? entity.PrefabId
                             : !string.IsNullOrEmpty(entity.EntityType) ? entity.EntityType
                             : "entity";

                // For prefab-based entities: the prefab may have its own codegen folder
                // (e.g. player/sms/codegen.json), but there is no IModule DLL for it.
                // Fall back to the "entity" module DLL as the registry proxy so
                // RegisterModuleData doesn't abort — the prefabModuleId is passed in the
                // state and used by CanRender to locate the per-prefab codegen first,
                // then fall back to entity/sms if it doesn't exist.
                var registryModuleId = moduleId;
                if (!string.IsNullOrEmpty(entity.PrefabId))
                {
                    bool hasPrefabDll = _moduleRegistry.GraphicModules.ContainsKey(moduleId)
                                     || _moduleRegistry.LogicModules.ContainsKey(moduleId)
                                     || _moduleRegistry.AudioModules.ContainsKey(moduleId);
                    if (!hasPrefabDll)
                        registryModuleId = "entity";
                }

                RegisterModuleData(
                    moduleId:    registryModuleId,
                    moduleState: entityState,
                    elementId:   entity.EntityId,
                    sceneId:     scene.SceneId,
                    trigger:     "OnVBlank",
                    overrideCodegenModuleId: moduleId != registryModuleId ? moduleId : null);

                // Advance counter to stay in sync with ModuleRenderer's instanceCounters
                var counterKey = moduleId != registryModuleId ? moduleId : registryModuleId;
                entityInstanceCounter[counterKey] = entityInstanceCounter.GetValueOrDefault(counterKey) + 1;
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

                // For prefab-based entities, try the prefab-specific codegen (player/sms)
                // first, then fall back to entity/sms if no dedicated folder exists.
                var renderModuleId = moduleId;
                if (!_moduleRenderer.CanRender(moduleId, project.TargetId) && moduleId != "entity")
                {
                    // Check if this is a prefab that should fall back to entity codegen
                    if (_moduleRenderer.CanRender("entity", project.TargetId))
                    {
                        renderModuleId = "entity";
                        progress?.Report($"INFO: No codegen found for '{moduleId}' — using 'entity' codegen as fallback.");
                    }
                }

                if (_moduleRenderer.CanRender(renderModuleId, project.TargetId))
                {
                    var policy     = _target.GetModulePolicyOverrides()
                        .TryGetValue(renderModuleId, out var p) ? p : module.SingletonPolicy;
                    var isSingleton = policy != SingletonPolicy.Multiple;

                    var moduleScene = elementToScene.TryGetValue(module, out var sceneId)
                        ? project.Scenes.FirstOrDefault(s => s.SceneId == sceneId)
                        : null;

                    generatedFiles = _moduleRenderer.Render(
                        renderModuleId, project.TargetId, moduleJson,
                        isSingleton, project.ProjectPath, moduleScene).ToList();
                    progress?.Report($"INFO: {moduleId} generated via ModuleRenderer (codegen: {renderModuleId}).");
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
