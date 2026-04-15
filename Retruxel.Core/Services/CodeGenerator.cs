using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
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
    private readonly ITarget _target;

    public CodeGenerator(ModuleRegistry moduleRegistry, ITarget target)
    {
        _moduleRegistry = moduleRegistry;
        _target = target;
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

        // Reset codegen state before generation
        _moduleRegistry.ResetCodeGenState();

        // Collect all module instances from scenes
        var instancesByModule = new Dictionary<string, List<IModule>>();

        foreach (var scene in project.Scenes)
        {
            foreach (var elementData in scene.Elements)
            {
                progress?.Report($"PROC: Loading element {elementData.ElementId.Substring(0, 8)}...");

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
                module.Deserialize(elementData.ModuleState);

                if (!instancesByModule.ContainsKey(elementData.ModuleId))
                    instancesByModule[elementData.ModuleId] = new List<IModule>();

                instancesByModule[elementData.ModuleId].Add(module);
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

                // Try to get CodeGen from registry
                var codegen = _moduleRegistry.GetCodeGen(module.ModuleId, project.TargetId);
                
                List<GeneratedFile> generatedFiles;
                
                if (codegen != null)
                {
                    // Use discovered CodeGen
                    generatedFiles = codegen.Generate(moduleJson).ToList();
                    progress?.Report($"INFO: {module.ModuleId} generated via CodeGen plugin.");
                }
                else
                {
                    // Fallback 1: Ask target to translate (legacy)
                    generatedFiles = _target.GenerateCodeForModule(contextualModule).ToList();

                    if (generatedFiles.Count == 0)
                    {
                        // Fallback 2: Module's own GenerateCode() (external plugins)
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

        // Generate target-specific main entry point
        var mainFile = _target.GenerateMainFile(project, sourceFiles);
        sourceFiles.Insert(0, mainFile);

        progress?.Report($"INFO: {sourceFiles.Count} source files generated.");
        progress?.Report($"INFO: {assets.Count} assets generated.");

        return new BuildContext
        {
            TargetId = project.TargetId,
            SourceFiles = sourceFiles,
            Assets = assets,
            OutputDirectory = outputDirectory,
            BuildParameters = project.Parameters
                .Where(p => p.Key.StartsWith("target."))
                .ToDictionary(p => p.Key.Replace("target.", ""), p => p.Value)
        };
    }

    /// <summary>
    /// Wraps a module so that its Serialize() output includes context flags
    /// about which other modules are present in the project.
    ///
    /// Current flags injected:
    ///   entity → "usePhysics": true/false, "useInput": true/false
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

            if (usePhysics || useInput)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["usePhysics"] = usePhysics,
                    ["useInput"] = useInput
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
        public bool IsSingleton => inner.IsSingleton;
        public string Serialize() => serializeTransform(inner.Serialize());
        public void Deserialize(string json) => inner.Deserialize(json);
        public string GetValidationSample() => inner.GetValidationSample();
    }
}
