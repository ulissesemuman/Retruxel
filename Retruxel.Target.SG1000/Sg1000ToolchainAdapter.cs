using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Toolchain;

namespace Retruxel.Target.SG1000;

public class Sg1000ToolchainAdapter : IToolchain
{
    private readonly IToolchainBuilder _builder;

    public Sg1000ToolchainAdapter()
    {
        _builder = ToolchainOrchestrator.GetBuilder("sg1000");
    }

    public string TargetId => _builder.TargetId;
    public string DisplayName => _builder.DisplayName;
    public string Version => _builder.Version;

    public Task ExtractAsync(IProgress<string> progress) => _builder.ExtractAsync(progress);
    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress) => _builder.BuildAsync(context, progress);
    public Task<bool> VerifyAsync() => _builder.VerifyAsync();
}
