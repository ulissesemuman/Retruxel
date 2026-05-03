using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Text;
using System.Text.Json.Nodes;

namespace Retruxel.Core.Services;

/// <summary>
/// Orchestrates code generation across all active modules in a project.
/// Collects generated files from each module's target-specific translator
/// and assembles a BuildContext ready for the IToolchain to compile.
/// </summary>
public class CodeGenerator
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

        // Reset renderer state before generation
        _moduleRenderer.ResetState();

        // Run TextAnalyzer before module rendering
        var fontConverter = DiscoverFontConverter();
        var graphicTilesEnd = CalculateGraphicTilesEnd(project);
        var textResult = _textAnalyzer.Analyze(project, Enumerable.Empty<IModule>(), fontConverter, graphicTilesEnd);
        
        if (textResult.MissingChars.Count > 0)
        {
            progress?.Report($"WARN: {textResult.MissingChars.Count} character(s) not in default font: {string.Join(", ", textResult.MissingChars.Take(10))}");
        }
        
        progress?.Report($"INFO: Compact font: {textResult.TileCount} glyphs, starting at tile {textResult.FontStartTile}");

        // Inject text analysis results into global variables for CodeGen templates
        var globalVariables = new Dictionary<string, object>
        {
            ["fontStartTile"] = textResult.FontStartTile,
            ["fontTileCount"] = textResult.TileCount,
            ["fontTileData"] = FormatByteArray(textResult.TileData),
            ["fontTranslationTable"] = FormatByteArray(textResult.TranslationTable)
        };
        
        _moduleRenderer.SetGlobalVariables(globalVariables);

        // Collect all module instances from scenes, grouped by trigger
        var instancesByModule = new Dictionary<string, List<IModule>>();
        var triggersByElement = new Dictionary<IModule, string>(); // Track trigger for each instance

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
                
                // Re-run TextAnalyzer with actual module instances for accurate character extraction
                if (elementData.ModuleId == "text.array")
                {
                    var allModules = instancesByModule.Values.SelectMany(x => x).ToList();
                    textResult = _textAnalyzer.Analyze(project, allModules, fontConverter, graphicTilesEnd);
                    
                    globalVariables["fontStartTile"] = textResult.FontStartTile;
                    globalVariables["fontTileCount"] = textResult.TileCount;
                    globalVariables["fontTileData"] = FormatByteArray(textResult.TileData);
                    globalVariables["fontTranslationTable"] = FormatByteArray(textResult.TranslationTable);
                    
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

                    generatedFiles = _moduleRenderer.Render(
                        module.ModuleId,
                        project.TargetId,
                        moduleJson,
                        isSingleton,
                        project.ProjectPath).ToList();
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

        // Generate scene files
        foreach (var scene in project.Scenes)
        {
            var sceneFiles = _moduleRenderer.RenderSceneFiles(project.TargetId, scene, sourceFiles, progress);
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
    /// Validates tile conflicts between tilemap and text.display modules.
    /// SMS_autoSetUpTextRenderer() loads ASCII font into tiles 0-255.
    /// If tilemap startTile < 256, it will overwrite the font.
    /// </summary>
    private static void ValidateTileConflicts(
        Dictionary<string, List<IModule>> instancesByModule,
        IProgress<string>? progress)
    {
        // Check if both tilemap and text.display are present
        var hasTilemap = instancesByModule.ContainsKey("tilemap");
        var hasTextDisplay = instancesByModule.ContainsKey("text.display");

        if (!hasTilemap || !hasTextDisplay)
            return;

        // Check each tilemap instance for startTile < 256
        foreach (var tilemapModule in instancesByModule["tilemap"])
        {
            var json = tilemapModule.Serialize();
            try
            {
                var node = JsonNode.Parse(json) as JsonObject;
                if (node is null) continue;

                var startTile = node["startTile"]?.GetValue<int>() ?? 0;

                if (startTile < 256)
                {
                    progress?.Report($"WARN: Tilemap startTile={startTile} conflicts with text.display font (tiles 0-255).");
                    progress?.Report($"WARN: Set tilemap startTile >= 256 to avoid overwriting the ASCII font.");
                    progress?.Report($"WARN: Text will appear corrupted if tilemap overwrites font tiles.");
                }
            }
            catch
            {
                // Skip validation if JSON parsing fails
            }
        }
    }

    /// <summary>
    /// Wraps a module so that its Serialize() output includes context flags
    /// about which other modules are present in the project.
    ///
    /// Current flags injected:
    ///   entity → "usePhysics", "useInput", "useAnimation"
    ///   enemy  → "useAnimation"
    ///   physics → "useTilemap"
    ///
    /// The wrapper only modifies Serialize() — all other IModule members
    /// delegate to the original module unchanged.
    /// </summary>
    private static IModule InjectContextFlags(IModule module, HashSet<string> presentModuleIds)
    {
        if (module.ModuleId == "entity")
        {
            var usePhysics = presentModuleIds.Contains("physics");
            var useInput = presentModuleIds.Contains("input");
            var useAnimation = presentModuleIds.Contains("animation");

            if (usePhysics || useInput || useAnimation)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["usePhysics"] = usePhysics,
                    ["useInput"] = useInput,
                    ["useAnimation"] = useAnimation
                }));
        }

        if (module.ModuleId == "enemy")
        {
            var useAnimation = presentModuleIds.Contains("animation");

            if (useAnimation)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["useAnimation"] = useAnimation
                }));
        }

        if (module.ModuleId == "physics")
        {
            var useTilemap = presentModuleIds.Contains("tilemap");

            if (useTilemap)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["useTilemap"] = useTilemap
                }));
        }

        return module;
    }

    /// <summary>
    /// Parses a JSON string, merges extra flags into the root object, and returns
    /// the modified JSON string.
    /// </summary>
    private static string InjectFlags(string json, Dictionary<string, bool> flags)
    {
        try
        {
            var node = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
            foreach (var (key, value) in flags)
                node[key] = value;
            return node.ToJsonString();
        }
        catch
        {
            return json; // If parsing fails, return original unchanged
        }
    }

    /// <summary>
    /// Discovers the IFontConverter implementation for the current target.
    /// Returns a default converter if none found.
    /// </summary>
    private IFontConverter DiscoverFontConverter()
    {
        var targetAssembly = _target.GetType().Assembly;
        var converterType = targetAssembly.GetTypes()
            .FirstOrDefault(t => typeof(IFontConverter).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        if (converterType is not null)
        {
            return (IFontConverter)Activator.CreateInstance(converterType)!;
        }
        
        // Fallback: return a simple pass-through converter
        return new DefaultFontConverter();
    }

    /// <summary>
    /// Calculates the first free tile slot after all graphic tiles.
    /// Used as the starting point for the compact font.
    /// </summary>
    private static int CalculateGraphicTilesEnd(RetruxelProject project)
    {
        // Simple heuristic: reserve 256 tiles for graphics
        // In production, this would scan tilemap/sprite modules for actual usage
        return 256;
    }

    /// <summary>
    /// Formats a byte array as a C array initializer string.
    /// Example: "0x3C, 0x42, 0x42, 0x3C"
    /// </summary>
    private static string FormatByteArray(byte[] data)
    {
        if (data.Length == 0) return "";
        
        var chunks = data.Select((b, i) => new { Byte = b, Index = i })
            .GroupBy(x => x.Index / 16)
            .Select(g => string.Join(", ", g.Select(x => $"0x{x.Byte:X2}")));
        
        return string.Join(",\n    ", chunks);
    }

    /// <summary>
    /// Default font converter that passes through raw bitmap data.
    /// Used as fallback when target doesn't provide IFontConverter.
    /// </summary>
    private class DefaultFontConverter : IFontConverter
    {
        public string TargetId => "default";
        
        public byte[] ConvertGlyphs(IEnumerable<(char Character, byte[] Bitmap)> glyphs)
        {
            return glyphs.SelectMany(g => g.Bitmap).ToArray();
        }
    }

    /// <summary>
    /// Wraps an IModule, overriding Serialize() to inject additional JSON properties.
    /// All other members delegate to the wrapped module.
    /// </summary>
    private sealed class ContextualModule(IModule inner, Func<string, string> serializeTransform) : IModule
    {
        public string ModuleId => inner.ModuleId;
        public string DisplayName => inner.DisplayName;
        public string Category => inner.Category;
        public ModuleType Type => inner.Type;
        public string[] Compatibility => inner.Compatibility;
        public SingletonPolicy SingletonPolicy => inner.SingletonPolicy;
        public ModuleScope DefaultScope => inner.DefaultScope;
        public string Serialize() => serializeTransform(inner.Serialize());
        public void Deserialize(string json) => inner.Deserialize(json);
        public string GetValidationSample() => inner.GetValidationSample();
    }
}
