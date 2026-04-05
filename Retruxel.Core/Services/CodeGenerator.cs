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
        IProgress<string>? progress = null)
    {
        var sourceFiles = new List<GeneratedFile>();
        var assets = new List<GeneratedAsset>();

        progress?.Report("INIT: Starting code generation...");

        // Group module instances by ModuleId
        var instancesByModule = new Dictionary<string, List<IModule>>();

        foreach (var (elementId, serializedData) in project.ModuleStates)
        {
            var moduleId = project.DefaultModules.FirstOrDefault() ?? "unknown";
            
            progress?.Report($"PROC: Loading element {elementId.Substring(0, 8)}...");

            IModule? moduleTemplate = null;

            if (_moduleLoader.GraphicModules.TryGetValue(moduleId, out var gm))
                moduleTemplate = gm;
            else if (_moduleLoader.LogicModules.TryGetValue(moduleId, out var lm))
                moduleTemplate = lm;
            else if (_moduleLoader.AudioModules.TryGetValue(moduleId, out var am))
                moduleTemplate = am;

            if (moduleTemplate is null)
            {
                progress?.Report($"WARN: Module {moduleId} not found — skipping.");
                continue;
            }

            var moduleType = moduleTemplate.GetType();
            var module = (IModule)Activator.CreateInstance(moduleType)!;
            module.Deserialize(serializedData);

            if (!instancesByModule.ContainsKey(moduleId))
                instancesByModule[moduleId] = new List<IModule>();
            
            instancesByModule[moduleId].Add(module);
        }

        // Generate code for each module type (passing all instances)
        foreach (var (moduleId, instances) in instancesByModule)
        {
            progress?.Report($"PROC: Generating code for {instances.Count} {moduleId} instance(s)...");

            foreach (var module in instances)
            {
                var generatedFiles = _target.GenerateCodeForModule(module).ToList();

                if (generatedFiles.Count == 0)
                    progress?.Report($"WARN: No translator for {moduleId} in target '{_target.TargetId}' — skipping.");

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

        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Retruxel", "builds", Guid.NewGuid().ToString());

        return new BuildContext
        {
            TargetId = project.TargetId,
            SourceFiles = sourceFiles,
            Assets = assets,
            OutputDirectory = outputDir,
            BuildParameters = project.Parameters
                .Where(p => p.Key.StartsWith("target."))
                .ToDictionary(p => p.Key.Replace("target.", ""), p => p.Value)
        };
    }
}