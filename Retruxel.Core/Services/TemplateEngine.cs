using System.Text;
using System.Text.RegularExpressions;

namespace Retruxel.Core.Services;

/// <summary>
/// Lightweight template engine for .c.rtrx files.
/// Supports:
/// - Variable substitution: {{variableName}}
/// - Property access: {{object.property}}
/// - Arithmetic: {{a * b}}, {{a + b}}
/// - Conditional blocks: {{#if condition}}...{{/if}}
/// - Negated conditionals: {{#ifnot condition}}...{{/ifnot}}
/// - Comparisons: {{#if a > b}}, {{#if a == b}}
/// - Block selection: @retruxel:block name:mode=value
/// </summary>
public class TemplateEngine
{
    private static readonly Regex BlockRegex = new(
        @"// @retruxel:block\s+(?<name>\w+)(?::mode=(?<mode>[\w+]+))?\s*\n(?<content>.*?)// @retruxel:end",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex VariableRegex = new(
        @"\{\{(?<expr>[^}]+)\}\}",
        RegexOptions.Compiled
    );

    private static readonly Regex ConditionalRegex = new(
        @"\{\{#if\s+(?<condition>[^}]+)\}\}(?<content>.*?)\{\{/if\}\}",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex NegatedConditionalRegex = new(
        @"\{\{#ifnot\s+(?<condition>[^}]+)\}\}(?<content>.*?)\{\{/ifnot\}\}",
        RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex EachRegex = new(
        @"\{\{#each\s+(?<variable>\w+)\}\}(?<content>.*?)\{\{/each\}\}",
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

        // 1. Process each loops
        result = EachRegex.Replace(result, match =>
        {
            var varName = match.Groups["variable"].Value.Trim();
            var content = match.Groups["content"].Value;
            return ProcessEachLoop(varName, content, variables);
        });

        // 2. Process negated conditionals
        result = NegatedConditionalRegex.Replace(result, match =>
        {
            var condition = match.Groups["condition"].Value.Trim();
            var content = match.Groups["content"].Value;
            return EvaluateCondition(condition, variables) ? string.Empty : content;
        });

        // 3. Process conditionals
        result = ConditionalRegex.Replace(result, match =>
        {
            var condition = match.Groups["condition"].Value.Trim();
            var content = match.Groups["content"].Value;
            return EvaluateCondition(condition, variables) ? content : string.Empty;
        });

        // 4. Substitute variables and expressions
        result = VariableRegex.Replace(result, match =>
        {
            var expr = match.Groups["expr"].Value.Trim();
            var value = EvaluateExpression(expr, variables);
            return value?.ToString() ?? string.Empty;
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

    private static string ProcessEachLoop(string varName, string content, Dictionary<string, object> variables)
    {
        if (!variables.TryGetValue(varName, out var value))
            return string.Empty;

        var sb = new StringBuilder();

        // Handle List<string>
        if (value is List<string> list)
        {
            foreach (var item in list)
            {
                var itemVars = new Dictionary<string, object>(variables) { ["this"] = item };
                sb.AppendLine(Render(content, itemVars));
            }
        }
        // Handle string[]
        else if (value is string[] array)
        {
            foreach (var item in array)
            {
                var itemVars = new Dictionary<string, object>(variables) { ["this"] = item };
                sb.AppendLine(Render(content, itemVars));
            }
        }
        // Handle IEnumerable<object>
        else if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var itemVars = new Dictionary<string, object>(variables) { ["this"] = item };
                sb.AppendLine(Render(content, itemVars));
            }
        }

        return sb.ToString();
    }

    private static bool EvaluateCondition(string condition, Dictionary<string, object> variables)
    {
        // Handle comparisons: >, <, >=, <=, ==, !=
        var comparisonOps = new[] { "==", "!=", ">=", "<=", ">", "<" };
        foreach (var op in comparisonOps)
        {
            if (condition.Contains(op))
            {
                var parts = condition.Split(new[] { op }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = EvaluateExpression(parts[0].Trim(), variables);
                    var right = EvaluateExpression(parts[1].Trim(), variables);
                    return CompareValues(left, right, op);
                }
            }
        }

        // Simple boolean check
        var value = EvaluateExpression(condition, variables);
        return value switch
        {
            bool b => b,
            int i => i != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            Array a => a.Length > 0,
            _ => value != null
        };
    }

    private static bool CompareValues(object? left, object? right, string op)
    {
        if (left == null || right == null)
            return op == "!=" ? left != right : left == right;

        // Convert to double for numeric comparisons
        var leftNum = Convert.ToDouble(left);
        var rightNum = Convert.ToDouble(right);

        return op switch
        {
            "==" => Math.Abs(leftNum - rightNum) < 0.0001,
            "!=" => Math.Abs(leftNum - rightNum) >= 0.0001,
            ">" => leftNum > rightNum,
            "<" => leftNum < rightNum,
            ">=" => leftNum >= rightNum,
            "<=" => leftNum <= rightNum,
            _ => false
        };
    }

    private static object? EvaluateExpression(string expr, Dictionary<string, object> variables)
    {
        expr = expr.Trim();

        // Literal numbers
        if (int.TryParse(expr, out var intVal))
            return intVal;
        if (double.TryParse(expr, out var doubleVal))
            return doubleVal;

        // Arithmetic operations: *, /, +, -
        var arithmeticOps = new (string op, Func<double, double, double> func)[]
        {
            ("*", (a, b) => a * b),
            ("/", (a, b) => a / b),
            ("+", (a, b) => a + b),
            ("-", (a, b) => a - b)
        };
        foreach (var (op, func) in arithmeticOps)
        {
            if (expr.Contains(op))
            {
                var parts = expr.Split(op, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = EvaluateExpression(parts[0].Trim(), variables);
                    var right = EvaluateExpression(parts[1].Trim(), variables);
                    if (left != null && right != null)
                        return func(Convert.ToDouble(left), Convert.ToDouble(right));
                }
            }
        }

        // Property access: object.property or object.property.subproperty
        if (expr.Contains('.'))
        {
            var parts = expr.Split('.');
            object? current = null;

            if (variables.TryGetValue(parts[0], out var root))
                current = root;
            else
                return null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (current == null) return null;

                var propName = parts[i];

                // Array.length
                if (propName == "length" && current is Array arr)
                {
                    current = arr.Length;
                    continue;
                }

                // Dictionary/object property access
                if (current is Dictionary<string, object> dict && dict.TryGetValue(propName, out var val))
                {
                    current = val;
                    continue;
                }

                // Reflection fallback
                var prop = current.GetType().GetProperty(propName);
                if (prop != null)
                {
                    current = prop.GetValue(current);
                    continue;
                }

                return null;
            }

            return current;
        }

        // Simple variable lookup
        return variables.TryGetValue(expr, out var value) ? value : null;
    }
}
