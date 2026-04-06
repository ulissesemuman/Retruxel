using Retruxel.Core.Models;

namespace Retruxel.Toolchain.Builders;

/// <summary>
/// SG-1000 toolchain builder - reuses SDCC + Sega8Bit SDK.
/// Currently scaffolding only.
/// </summary>
public class Sg1000ToolchainBuilder : IToolchainBuilder
{
    public string TargetId => "sg1000";
    public string DisplayName => "SDCC + devkitSMS + SGlib (SG-1000)";
    public string Version => "4.5.24";

    public async Task ExtractAsync(IProgress<string> progress)
    {
        var smsBuilder = new SmsToolchainBuilder();
        await smsBuilder.ExtractAsync(progress);
    }

    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        throw new NotImplementedException("SG-1000 target is scaffolding only - build not implemented yet.");
    }

    public async Task<bool> VerifyAsync()
    {
        var smsBuilder = new SmsToolchainBuilder();
        return await smsBuilder.VerifyAsync();
    }
}
