using System.Text;
using System.Text.RegularExpressions;

namespace Retruxel.Core.Services;

/// <summary>
/// Lightweight template engine for .c.rtrx files.
/// Supports:
/// - Variable substitution: {{variableName}}
/// - Conditional blocks: {{#if condition}}...{{/if}}
/// - Block selection: @retruxel:block name:mode=value
/// </summary>
public class TemplateEngine
{
    private static readonly Regex BlockRegex = new(
        @"// @retruxel:block\s+(?<name>\w+)(?::mode=(?<mode>[\w+]+))?\s*\n(?<content>.*?)// @retruxel:end",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex VariableRegex = new(
        @"\{\{(?<var>\w+)\}\}",
        RegexOptions.Compiled
    );

    private static readonly Regex ConditionalRegex = new(
        @"\{\{#if\s+(?<condition>\w+)\}\}(?<content>.*?)\{\{/if\}\}",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    /// <summary>
    /// Loads a template from embedded resource or file path.
    /// </summary>
    public static string LoadTemplate(string resourcePath)
    {
        var assembly = System.Reflection.Assembly.GetCallingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        
        if (stream == null)
        {
            // Fallback: try loading from file system
            if (File.Exists(resourcePath))
                return File.ReadAllText(resourcePath);
            
            throw new FileNotFoundException($"Template not found: {resourcePath}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Extracts a specific block from the template.
    /// </summary>
    public static string ExtractBlock(string template, string blockName, string? mode = null)
    {
        var matches = BlockRegex.Matches(template);

        foreach (Match match in matches)
        {
            var name = match.Groups["name"].Value;
            var blockMode = match.Groups["mode"].Success ? match.Groups["mode"].Value : null;
            var content = match.Groups["content"].Value;

            if (name == blockName)
            {
                // If mode is specified, match it
                if (mode != null && blockMode != null && blockMode != mode)
                    continue;

                // If no mode specified in query, return first match
                if (mode == null && blockMode == null)
                    return content.Trim();

                // If mode matches or no mode required
                if (mode == null || blockMode == mode)
                    return content.Trim();
            }
        }

        throw new InvalidOperationException($"Block '{blockName}' with mode '{mode}' not found in template");
    }

    /// <summary>
    /// Renders a template by substituting variables and evaluating conditionals.
    /// </summary>
    public static string Render(string template, Dictionary<string, object> variables)
    {
        var result = template;

        // 1. Process conditionals first
        result = ConditionalRegex.Replace(result, match =>
        {
            var condition = match.Groups["condition"].Value;
            var content = match.Groups["content"].Value;

            // Check if condition variable exists and is truthy
            if (variables.TryGetValue(condition, out var value))
            {
                if (value is bool boolValue && boolValue)
                    return content;
                if (value is int intValue && intValue != 0)
                    return content;
                if (value is string strValue && !string.IsNullOrEmpty(strValue))
                    return content;
            }

            return string.Empty; // Condition false, remove block
        });

        // 2. Substitute variables
        result = VariableRegex.Replace(result, match =>
        {
            var varName = match.Groups["var"].Value;
            
            if (variables.TryGetValue(varName, out var value))
                return value?.ToString() ?? string.Empty;

            // Variable not found, keep placeholder
            return match.Value;
        });

        return result;
    }

    /// <summary>
    /// Convenience method: Extract block and render in one call.
    /// </summary>
    public static string RenderBlock(string template, string blockName, Dictionary<string, object> variables, string? mode = null)
    {
        var block = ExtractBlock(template, blockName, mode);
        return Render(block, variables);
    }
}
