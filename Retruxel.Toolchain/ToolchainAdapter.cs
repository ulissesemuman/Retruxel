using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Toolchain;

/// <summary>
/// Generic adapter that wraps an IToolchainBuilder to implement the IToolchain interface.
/// Used by all targets to expose their toolchain builders.
/// </summary>
public class ToolchainAdapter : IToolchain
{
    private readonly IToolchainBuilder _builder;

    public ToolchainAdapter(IToolchainBuilder builder)
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
