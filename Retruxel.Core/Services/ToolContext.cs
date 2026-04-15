using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Core.Services;

/// <summary>
/// Implementation of IToolContext that provides context to tools when they are executed.
/// </summary>
public class ToolContext : IToolContext
{
    public RetruxelProject? CurrentProject { get; init; }
    public ITarget? ActiveTarget { get; init; }
    public IServiceProvider Services { get; init; } = null!;
}
