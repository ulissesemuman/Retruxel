using Retruxel.Core.Interfaces;

namespace Retruxel.Core.Services;

/// <summary>
/// Registry for all available toolchains.
/// Each target registers its toolchain on startup.
/// The CodeGenerator uses this to resolve the correct toolchain for a project.
/// </summary>
public class ToolchainManager
{
    private readonly Dictionary<string, IToolchain> _toolchains = new();

    /// <summary>
    /// Registers a toolchain for a specific target.
    /// Called by each ITarget implementation during application startup.
    /// </summary>
    public void Register(IToolchain toolchain)
    {
        _toolchains[toolchain.TargetId] = toolchain;
    }

    /// <summary>
    /// Returns the toolchain registered for the given target.
    /// Throws if no toolchain is registered for that target.
    /// </summary>
    public IToolchain GetToolchain(string targetId)
    {
        if (_toolchains.TryGetValue(targetId, out var toolchain))
            return toolchain;

        throw new InvalidOperationException(
            $"No toolchain registered for target '{targetId}'. " +
            $"Ensure the target assembly is loaded before building.");
    }

    /// <summary>
    /// Returns true if a toolchain is registered for the given target.
    /// </summary>
    public bool HasToolchain(string targetId)
        => _toolchains.ContainsKey(targetId);

    /// <summary>
    /// Returns all registered target IDs.
    /// </summary>
    public IEnumerable<string> RegisteredTargets
        => _toolchains.Keys;
}