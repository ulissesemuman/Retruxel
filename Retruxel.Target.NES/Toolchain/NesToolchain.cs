using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Target.NES.Toolchain;

/// <summary>
/// Toolchain for NES (cc65 + neslib).
/// Currently a stub - not yet implemented.
/// </summary>
public class NesToolchain : IToolchain
{
    public string TargetId => "nes";
    public string DisplayName => "cc65 + neslib";
    public string Version => "stub";

    public Task ExtractAsync(IProgress<string> progress)
    {
        progress?.Report("NES toolchain extraction not yet implemented");
        return Task.CompletedTask;
    }

    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        progress?.Report("NES toolchain not yet implemented");

        return Task.FromResult(new BuildResult
        {
            Success = false,
            RomPath = string.Empty,
            Log = new List<BuildLogEntry>
            {
                new BuildLogEntry
                {
                    Level = BuildLogLevel.Error,
                    Message = "NES toolchain not yet implemented. cc65 + neslib integration coming soon.",
                    Timestamp = DateTime.Now
                }
            }
        });
    }

    public Task<bool> VerifyAsync()
    {
        return Task.FromResult(false);
    }
}
