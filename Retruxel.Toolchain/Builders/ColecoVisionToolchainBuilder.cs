using Retruxel.Core.Models;

namespace Retruxel.Toolchain.Builders;

/// <summary>
/// ColecoVision toolchain builder - reuses SDCC + Sega8Bit SDK (or Coleco-specific libs).
/// Currently scaffolding only.
/// </summary>
public class ColecoVisionToolchainBuilder : IToolchainBuilder
{
    public string TargetId => "colecovision";
    public string DisplayName => "SDCC + ColecoVision libs";
    public string Version => "4.5.24";

    public async Task ExtractAsync(IProgress<string> progress)
    {
        var smsBuilder = new SmsToolchainBuilder();
        await smsBuilder.ExtractAsync(progress);
    }

    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        throw new NotImplementedException("ColecoVision target is scaffolding only - build not implemented yet.");
    }

    public async Task<bool> VerifyAsync()
    {
        var smsBuilder = new SmsToolchainBuilder();
        return await smsBuilder.VerifyAsync();
    }
}
