using Retruxel.Core.Interfaces;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Registry for discovering and managing asset pipeline stages.
/// Enables automatic pipeline chaining and composition.
/// </summary>
public class PipelineRegistry
{
    private readonly Dictionary<string, IAssetPipeline> _pipelines = new();
    private readonly Dictionary<Type, List<IAssetPipeline>> _pipelinesByInputType = new();
    private readonly Dictionary<Type, List<IAssetPipeline>> _pipelinesByOutputType = new();

    /// <summary>
    /// Registers a pipeline stage.
    /// </summary>
    public void Register(IAssetPipeline pipeline)
    {
        _pipelines[pipeline.PipelineId] = pipeline;

        if (!_pipelinesByInputType.ContainsKey(pipeline.InputType))
            _pipelinesByInputType[pipeline.InputType] = new();
        _pipelinesByInputType[pipeline.InputType].Add(pipeline);

        if (!_pipelinesByOutputType.ContainsKey(pipeline.OutputType))
            _pipelinesByOutputType[pipeline.OutputType] = new();
        _pipelinesByOutputType[pipeline.OutputType].Add(pipeline);
    }

    /// <summary>
    /// Gets a pipeline by ID.
    /// </summary>
    public IAssetPipeline? GetPipeline(string pipelineId)
    {
        return _pipelines.TryGetValue(pipelineId, out var pipeline) ? pipeline : null;
    }

    /// <summary>
    /// Gets all pipelines that can accept the given input type.
    /// </summary>
    public IEnumerable<IAssetPipeline> GetPipelinesForInput(Type inputType)
    {
        if (_pipelinesByInputType.TryGetValue(inputType, out var pipelines))
            return pipelines;

        return _pipelinesByInputType
            .Where(kvp => inputType.IsAssignableTo(kvp.Key))
            .SelectMany(kvp => kvp.Value);
    }

    /// <summary>
    /// Gets all pipelines that can produce the given output type.
    /// </summary>
    public IEnumerable<IAssetPipeline> GetPipelinesForOutput(Type outputType)
    {
        if (_pipelinesByOutputType.TryGetValue(outputType, out var pipelines))
            return pipelines;

        return _pipelinesByOutputType
            .Where(kvp => kvp.Key.IsAssignableTo(outputType))
            .SelectMany(kvp => kvp.Value);
    }

    /// <summary>
    /// Finds a pipeline chain that can convert from inputType to outputType.
    /// Returns null if no chain is possible.
    /// </summary>
    public List<IAssetPipeline>? FindPipelineChain(Type inputType, Type outputType)
    {
        // Direct match
        var directPipelines = GetPipelinesForInput(inputType)
            .Where(p => p.OutputType == outputType || p.OutputType.IsAssignableTo(outputType))
            .ToList();

        if (directPipelines.Any())
            return new List<IAssetPipeline> { directPipelines.First() };

        // BFS to find shortest chain
        var queue = new Queue<(Type currentType, List<IAssetPipeline> chain)>();
        var visited = new HashSet<Type>();

        queue.Enqueue((inputType, new List<IAssetPipeline>()));
        visited.Add(inputType);

        while (queue.Count > 0)
        {
            var (currentType, chain) = queue.Dequeue();

            foreach (var pipeline in GetPipelinesForInput(currentType))
            {
                var newChain = new List<IAssetPipeline>(chain) { pipeline };

                if (pipeline.OutputType == outputType || pipeline.OutputType.IsAssignableTo(outputType))
                    return newChain;

                if (!visited.Contains(pipeline.OutputType))
                {
                    visited.Add(pipeline.OutputType);
                    queue.Enqueue((pipeline.OutputType, newChain));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Executes a pipeline chain on the given input.
    /// </summary>
    public object ExecuteChain(List<IAssetPipeline> chain, object input, Dictionary<string, object>? options = null)
    {
        object current = input;

        foreach (var pipeline in chain)
        {
            current = pipeline.Process(current, options);
        }

        return current;
    }

    /// <summary>
    /// Discovers and registers all pipeline implementations in the given assembly.
    /// </summary>
    public void DiscoverPipelines(Assembly assembly)
    {
        var pipelineTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IAssetPipeline).IsAssignableFrom(t));

        foreach (var type in pipelineTypes)
        {
            try
            {
                if (Activator.CreateInstance(type) is IAssetPipeline pipeline)
                {
                    Register(pipeline);
                }
            }
            catch
            {
                // Skip types that can't be instantiated
            }
        }
    }

    /// <summary>
    /// Gets all registered pipelines.
    /// </summary>
    public IEnumerable<IAssetPipeline> GetAllPipelines() => _pipelines.Values;
}
