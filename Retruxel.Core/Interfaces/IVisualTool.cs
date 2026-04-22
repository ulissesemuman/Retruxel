namespace Retruxel.Core.Interfaces;

/// <summary>
/// Tool with visual UI component
/// </summary>
public interface IVisualTool : ITool
{
    /// <summary>
    /// Indicates if this tool has a visual UI
    /// </summary>
    bool HasUI => true;

    /// <summary>
    /// Creates and returns the UI window for this tool
    /// </summary>
    /// <param name="input">Input parameters for the tool</param>
    /// <returns>Window instance (must be cast to actual Window type by caller)</returns>
    object CreateWindow(Dictionary<string, object> input);
}
