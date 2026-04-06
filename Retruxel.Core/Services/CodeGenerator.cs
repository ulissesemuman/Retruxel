using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

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
        var assets = new List<GeneratedAsset>();

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

        // Generate code for each module type (passing all instances)
        foreach (var (moduleId, instances) in instancesByModule)
        {
            progress?.Report($"PROC: Generating code for {instances.Count} {moduleId} instance(s)...");

            foreach (var module in instances)
            {
                // 1. Ask the target to translate this module into target-specific C code.
                // 2. If the target has no translator (returns empty), fall back to the
                //    module's own GenerateCode() — used by external plugins that ship
                //    their own C implementation directly.
                var generatedFiles = _target.GenerateCodeForModule(module).ToList();

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

                sourceFiles.AddRange(generatedFiles);

                if (module is IGraphicModule gfxModule)
                    assets.AddRange(gfxModule.GenerateAssets());
                else if (module is IAudioModule audioModule)
                    assets.AddRange(audioModule.GenerateAssets());
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
}