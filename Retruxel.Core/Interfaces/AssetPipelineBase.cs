namespace Retruxel.Core.Interfaces;

/// <summary>
/// Base implementation of IAssetPipeline that provides common functionality.
/// Inherit from this class to create custom pipeline stages.
/// </summary>
public abstract class AssetPipelineBase<TInput, TOutput> : IAssetPipeline<TInput, TOutput>
{
    public abstract string PipelineId { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }

    public Type InputType => typeof(TInput);
    public Type OutputType => typeof(TOutput);

    public abstract TOutput ProcessTyped(TInput input, Dictionary<string, object>? options = null);

    public object Process(object input, Dictionary<string, object>? options = null)
    {
        if (input is not TInput typedInput)
        {
            throw new ArgumentException(
                $"Invalid input type. Expected {typeof(TInput).Name}, got {input?.GetType().Name ?? "null"}",
                nameof(input));
        }

        return ProcessTyped(typedInput, options)!;
    }

    public bool CanAccept(Type inputType)
    {
        return inputType == typeof(TInput) || inputType.IsAssignableTo(typeof(TInput));
    }

    public bool CanProduce(Type outputType)
    {
        return outputType == typeof(TOutput) || typeof(TOutput).IsAssignableTo(outputType);
    }
}
