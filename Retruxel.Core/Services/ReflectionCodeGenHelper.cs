using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System.Reflection;

namespace Retruxel.Core.Services;

/// <summary>
/// Helper for dynamic code generation using reflection.
/// Scans assemblies for CodeGen classes and invokes them dynamically.
/// </summary>
public static class ReflectionCodeGenHelper
{
    /// <summary>
    /// Discovers all CodeGen classes in an assembly and builds a cache.
    /// </summary>
    public static Dictionary<string, Type> BuildCodeGenCache(Assembly assembly, string targetPrefix)
    {
        var cache = new Dictionary<string, Type>();

        // Scan assembly for types ending with "CodeGen"
        var codeGenTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("CodeGen") && !t.IsAbstract && !t.IsInterface);

        foreach (var type in codeGenTypes)
        {
            // Extract module name from class name
            // SmsTextDisplayCodeGen -> text.display
            // SmsEntityCodeGen -> entity
            var className = type.Name
                .Replace(targetPrefix, "", StringComparison.OrdinalIgnoreCase)
                .Replace("CodeGen", "");

            // Convert PascalCase to dot.case
            var moduleId = ConvertPascalCaseToDotCase(className);

            // Register both with and without target prefix
            cache[moduleId] = type;
            
            if (!moduleId.Contains("."))
                cache[$"{targetPrefix.ToLower()}.{moduleId}"] = type;
        }

        return cache;
    }

    /// <summary>
    /// Generates code for a module using reflection to invoke the CodeGen.
    /// </summary>
    public static IEnumerable<GeneratedFile> GenerateCodeForModule(
        IModule module,
        Dictionary<string, Type> codeGenCache,
        Func<IEnumerable<string>, List<GeneratedFile>, IEnumerable<GeneratedFile>>? warningInjector = null)
    {
        // Try to find CodeGen for this module
        if (!codeGenCache.TryGetValue(module.ModuleId, out var codeGenType))
            return []; // No code generator found

        try
        {
            // Instantiate CodeGen with module JSON
            var codeGen = Activator.CreateInstance(codeGenType, module.Serialize());
            if (codeGen == null)
                return [];

            // Call Validate() method
            var validateMethod = codeGenType.GetMethod("Validate");
            var errors = validateMethod?.Invoke(codeGen, null) as IEnumerable<string> ?? [];

            // Call GenerateCode() and GenerateHeader() methods
            var generateCodeMethod = codeGenType.GetMethod("GenerateCode");
            var generateHeaderMethod = codeGenType.GetMethod("GenerateHeader");

            var codeFile = generateCodeMethod?.Invoke(codeGen, null) as GeneratedFile;
            var headerFile = generateHeaderMethod?.Invoke(codeGen, null) as GeneratedFile;

            var files = new List<GeneratedFile>();
            if (codeFile != null) files.Add(codeFile);
            if (headerFile != null) files.Add(headerFile);

            // Inject warnings if provided
            if (warningInjector != null)
                return warningInjector(errors, files);

            return files;
        }
        catch
        {
            return []; // Failed to instantiate or invoke
        }
    }

    /// <summary>
    /// Converts PascalCase to dot.case.
    /// TextDisplay -> text.display
    /// Entity -> entity
    /// </summary>
    private static string ConvertPascalCaseToDotCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        result.Append(char.ToLower(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                result.Append('.');
                result.Append(char.ToLower(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }
}
