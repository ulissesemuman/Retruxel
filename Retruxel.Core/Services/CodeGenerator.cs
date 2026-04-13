using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Retruxel.Core.Services;

/// <summary>
/// Orchestrates code generation across all active modules in a project.
/// Collects generated files from each module's target-specific translator
/// and assembles a BuildContext ready for the IToolchain to compile.
/// </summary>
public class CodeGenerator
{
    private readonly ModuleLoader _moduleLoader;
    private readonly ITarget _target;

    public CodeGenerator(ModuleLoader moduleLoader, ITarget target)
    {
        _moduleLoader = moduleLoader;
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
        var assets      = new List<GeneratedAsset>();

        progress?.Report("INIT: Starting code generation...");

        // Reset target-specific state before generation
        _target.ResetCodeGenerationState();

        // Collect all module instances from scenes
        var instancesByModule = new Dictionary<string, List<IModule>>();

        foreach (var scene in project.Scenes)
        {
            foreach (var elementData in scene.Elements)
            {
                progress?.Report($"PROC: Loading element {elementData.ElementId.Substring(0, 8)}...");

                IModule? moduleTemplate = null;

                if (_moduleLoader.GraphicModules.TryGetValue(elementData.ModuleId, out var gm))
                    moduleTemplate = gm;
                else if (_moduleLoader.LogicModules.TryGetValue(elementData.ModuleId, out var lm))
                    moduleTemplate = lm;
                else if (_moduleLoader.AudioModules.TryGetValue(elementData.ModuleId, out var am))
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
                // This allows CodeGens to produce different output depending on which
                // other modules are present — e.g. entity uses physics when available.
                var contextualModule = InjectContextFlags(module, presentModuleIds);

                // 1. Ask the target to translate this module into target-specific C code.
                // 2. If the target has no translator (returns empty), fall back to the
                //    module's own GenerateCode() — used by external plugins that ship
                //    their own C implementation directly.
                var generatedFiles = _target.GenerateCodeForModule(contextualModule).ToList();

                if (generatedFiles.Count == 0)
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
            TargetId        = project.TargetId,
            SourceFiles     = sourceFiles,
            Assets          = assets,
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
    ///   sms.entity → "usePhysics": true/false, "useInput": true/false
    ///
    /// The wrapper only modifies Serialize() — all other IModule members
    /// delegate to the original module unchanged.
    /// </summary>
    private static IModule InjectContextFlags(IModule module, HashSet<string> presentModuleIds)
    {
        if (module.ModuleId == "sms.entity")
        {
            var usePhysics = presentModuleIds.Contains("sms.physics");
            var useInput   = presentModuleIds.Contains("sms.input");

            if (usePhysics || useInput)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["usePhysics"] = usePhysics,
                    ["useInput"]   = useInput
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
        public string      ModuleId            => inner.ModuleId;
        public string      DisplayName         => inner.DisplayName;
        public string      Category            => inner.Category;
        public ModuleType  Type                => inner.Type;
        public string[]    Compatibility       => inner.Compatibility;
        public bool        IsSingleton         => inner.IsSingleton;
        public string      Serialize()         => serializeTransform(inner.Serialize());
        public void        Deserialize(string json) => inner.Deserialize(json);
        public string      GetValidationSample() => inner.GetValidationSample();
    }
}
