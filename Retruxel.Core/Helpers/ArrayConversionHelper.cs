using System.Text.Json;

namespace Retruxel.Core.Helpers;

/// <summary>
/// Centralized helper for converting various array types to int[].
/// Used by ModuleRenderer, tools, and any other component that needs consistent array conversion.
/// </summary>
public static class ArrayConversionHelper
{
    /// <summary>
    /// Converts any array-like object to int[].
    /// Supports: int[], List&lt;int&gt;, List&lt;string&gt;, object[], JsonElement arrays.
    /// </summary>
    public static int[] ToIntArray(object? value)
    {
        if (value is null)
            return Array.Empty<int>();

        if (value is int[] intArr)
            return intArr;

        if (value is List<int> intList)
            return intList.ToArray();

        if (value is List<string> strList)
            return strList.Select(s => int.TryParse(s, out var i) ? i : 0).ToArray();

        if (value is object[] objArr)
            return objArr.Select(o => Convert.ToInt32(o)).ToArray();

        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            return jsonElement.EnumerateArray()
                .Select(e => e.ValueKind == JsonValueKind.Number
                    ? (e.TryGetInt32(out var i) ? i : (int)e.GetDouble())
                    : (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var si) ? si : 0))
                .ToArray();

        return Array.Empty<int>();
    }
}
