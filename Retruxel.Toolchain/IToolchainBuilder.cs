using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Toolchain;

/// <summary>
/// Interface for toolchain builders that compile source code into ROM files.
/// </summary>
public interface IToolchainBuilder
{
    string TargetId { get; }
    string DisplayName { get; }
    string Version { get; }
    
    Task ExtractAsync(IProgress<string> progress);
    Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress);
    Task<bool> VerifyAsync();
}
