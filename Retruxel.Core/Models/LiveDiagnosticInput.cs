using System.Collections.Generic;

namespace Retruxel.Core.Models;

/// <summary>
/// Input for live (pre-build) hardware constraint analysis.
/// Passed from the editor to ITarget.GetLiveDiagnostics() on every project mutation.
/// Contains only project data — no generated files needed.
/// </summary>
public class LiveDiagnosticInput
{
    /// <summary>Current scene being edited.</summary>
    public SceneData Scene { get; init; } = new();

    /// <summary>Full project — provides assets, prefabs, all scenes.</summary>
    public RetruxelProject Project { get; init; } = new();

    /// <summary>Hardware specs of the active target.</summary>
    public TargetSpecs Specs { get; init; } = new();

    /// <summary>VRAM usage already calculated by VramAllocator.Analyze().</summary>
    public VramUsageReport? VramUsage { get; init; }
}
