using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Toolchain;

namespace Retruxel.Target.NES;

/// <summary>
/// Adapter that wraps an IToolchainBuilder to implement the IToolchain interface.
/// </summary>
internal class NesToolchainAdapter : IToolchain
{
    private readonly IToolchainBuilder _builder;

    public NesToolchainAdapter(IToolchainBuilder builder)
    {
        _builder = builder;
    }

    public string TargetId => _builder.TargetId;
    public string DisplayName => _builder.DisplayName;
    public string Version => _builder.Version;

    public Task ExtractAsync(IProgress<string> progress) => _builder.ExtractAsync(progress);
    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress) => _builder.BuildAsync(context, progress);
    public Task<bool> VerifyAsync() => _builder.VerifyAsync();
}
