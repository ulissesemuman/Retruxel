namespace Retruxel.Core.Models;

/// <summary>
/// Input data passed from Core to target for calculating build diagnostics.
/// The target inspects this data and returns a BuildDiagnosticsReport.
/// </summary>
public class BuildDiagnosticInput
{
    /// <summary>All source files generated in this build</summary>
    public IReadOnlyList<GeneratedFile> SourceFiles { get; }

    /// <summary>All assets generated in this build</summary>
    public IReadOnlyList<GeneratedAsset> Assets { get; }

    /// <summary>Build parameters from the project (region, romSize, etc.)</summary>
    public IReadOnlyDictionary<string, object> BuildParameters { get; }

    /// <summary>Target specs — provides hardware limits for reference</summary>
    public TargetSpecs Specs { get; }

    public BuildDiagnosticInput(
        IEnumerable<GeneratedFile> sourceFiles,
        IEnumerable<GeneratedAsset> assets,
        Dictionary<string, object> buildParameters,
        TargetSpecs specs)
    {
        SourceFiles = sourceFiles.ToList();
        Assets = assets.ToList();
        BuildParameters = buildParameters;
        Specs = specs;
    }
}
