using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Toolchain;

namespace Retruxel.Target.ColecoVision;

public class ColecoToolchainAdapter : IToolchain
{
    private readonly IToolchainBuilder _builder;

    public ColecoToolchainAdapter()
    {
        _builder = ToolchainOrchestrator.GetBuilder("colecovision");
    }

    public string TargetId => _builder.TargetId;
    public string DisplayName => _builder.DisplayName;
    public string Version => _builder.Version;

    public Task ExtractAsync(IProgress<string> progress) => _builder.ExtractAsync(progress);
    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress) => _builder.BuildAsync(context, progress);
    public Task<bool> VerifyAsync() => _builder.VerifyAsync();
}
