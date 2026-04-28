using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;

namespace Retruxel.Views.SceneEditor.Helpers;

/// <summary>
/// Helper for getting and setting module parameters via reflection.
/// </summary>
public static class ModuleParameterHelper
{
    public static object? GetValue(IModule module, string paramName)
    {
        var prop = module.GetType().GetProperty(paramName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        return prop?.GetValue(module);
    }

    public static void SetValue(IModule module, string paramName, string value, ParameterType type)
    {
        var prop = module.GetType().GetProperty(paramName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);

        if (prop is null) return;

        try
        {
            object? convertedValue = type switch
            {
                ParameterType.Int => int.TryParse(value, out var i) ? i : null,
                ParameterType.Float => float.TryParse(value, out var f) ? f : null,
                ParameterType.Bool => bool.TryParse(value, out var b) ? b : null,
                ParameterType.String => value,
                _ => value
            };

            if (convertedValue is not null)
                prop.SetValue(module, convertedValue);
        }
        catch
        {
            // Ignore conversion errors
        }
    }

    public static void UpdatePosition(IModule module, int x, int y)
    {
        SetValue(module, "x", x.ToString(), ParameterType.Int);
        SetValue(module, "y", y.ToString(), ParameterType.Int);
    }
}
