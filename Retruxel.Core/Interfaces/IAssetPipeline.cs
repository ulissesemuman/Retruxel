namespace Retruxel.Core.Interfaces;

/// <summary>
/// Defines a pipeline stage that can process asset data and be chained with other stages.
/// Enables modular, composable asset processing workflows.
/// </summary>
public interface IAssetPipeline
{
    /// <summary>
    /// Unique identifier for this pipeline stage (e.g., "livelink_to_imported", "imported_to_tilemap").
    /// </summary>
    string PipelineId { get; }

    /// <summary>
    /// Human-readable name for this pipeline stage.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of what this pipeline stage does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Type of data this pipeline accepts as input.
    /// </summary>
    Type InputType { get; }

    /// <summary>
    /// Type of data this pipeline produces as output.
    /// </summary>
    Type OutputType { get; }

    /// <summary>
    /// Processes input data and produces output data.
    /// </summary>
    /// <param name="input">Input data (must match InputType)</param>
    /// <param name="options">Optional processing options</param>
    /// <returns>Processed output data (matches OutputType)</returns>
    /// <exception cref="ArgumentException">If input type doesn't match InputType</exception>
    /// <exception cref="InvalidOperationException">If processing fails</exception>
    object Process(object input, Dictionary<string, object>? options = null);

    /// <summary>
    /// Checks if this pipeline can accept the given input type.
    /// </summary>
    bool CanAccept(Type inputType);

    /// <summary>
    /// Checks if this pipeline can produce the given output type.
    /// </summary>
    bool CanProduce(Type outputType);
}

/// <summary>
/// Generic version of IAssetPipeline for type-safe implementations.
/// </summary>
public interface IAssetPipeline<TInput, TOutput> : IAssetPipeline
{
    /// <summary>
    /// Type-safe processing method.
    /// </summary>
    TOutput ProcessTyped(TInput input, Dictionary<string, object>? options = null);
}
