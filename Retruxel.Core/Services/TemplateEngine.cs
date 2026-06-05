using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
/// - Nested {{#each}} loops
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

    // Kept for reference but not used in Render — replaced by ReplaceEachBlocks
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
            var name      = match.Groups["name"].Value;
            var blockMode = match.Groups["mode"].Success ? match.Groups["mode"].Value : null;
            var content   = match.Groups["content"].Value;

            if (name == blockName)
            {
                if (mode != null && blockMode != null && blockMode != mode)
                    continue;

                if (mode == null && blockMode == null)
                    return content.Trim();

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

        // 1. Process each loops (nested-aware depth-counting parser)
        result = ReplaceEachBlocks(result, variables);

        // 2. Process negated conditionals
        result = NegatedConditionalRegex.Replace(result, match =>
        {
            var condition = match.Groups["condition"].Value.Trim();
            var content   = match.Groups["content"].Value;
            return EvaluateCondition(condition, variables) ? string.Empty : content;
        });

        // 3. Process conditionals
        result = ConditionalRegex.Replace(result, match =>
        {
            var condition = match.Groups["condition"].Value.Trim();
            var content   = match.Groups["content"].Value;
            return EvaluateCondition(condition, variables) ? content : string.Empty;
        });

        // 4. Substitute variables and expressions
        result = VariableRegex.Replace(result, match =>
        {
            var expr  = match.Groups["expr"].Value.Trim();
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

    /// <summary>
    /// Processes {{#each}} loops with proper nesting support by manually finding
    /// the matching {{/each}} tag using a depth counter.
    /// </summary>
    private static string ReplaceEachBlocks(string template, Dictionary<string, object> variables)
    {
        const string openTag  = "{{#each ";
        const string closeTag = "{{/each}}";

        var result = new StringBuilder();
        int pos = 0;

        while (pos < template.Length)
        {
            int start = template.IndexOf(openTag, pos, StringComparison.Ordinal);
            if (start < 0)
            {
                result.Append(template, pos, template.Length - pos);
                break;
            }

            result.Append(template, pos, start - pos);

            int nameStart = start + openTag.Length;
            int nameEnd   = template.IndexOf("}}", nameStart, StringComparison.Ordinal);
            if (nameEnd < 0) { result.Append(template, start, template.Length - start); break; }

            var varName      = template.Substring(nameStart, nameEnd - nameStart).Trim();
            int contentStart = nameEnd + 2;

            // Find matching {{/each}} accounting for nesting
            int depth      = 1;
            int searchPos  = contentStart;
            int contentEnd = -1;

            while (searchPos < template.Length && depth > 0)
            {
                int nextOpen  = template.IndexOf(openTag,  searchPos, StringComparison.Ordinal);
                int nextClose = template.IndexOf(closeTag, searchPos, StringComparison.Ordinal);

                if (nextClose < 0) break;

                if (nextOpen >= 0 && nextOpen < nextClose)
                {
                    depth++;
                    searchPos = nextOpen + openTag.Length;
                }
                else
                {
                    depth--;
                    if (depth == 0)
                        contentEnd = nextClose;
                    searchPos = nextClose + closeTag.Length;
                }
            }

            if (contentEnd < 0)
            {
                result.Append(template, start, template.Length - start);
                break;
            }

            var content = template.Substring(contentStart, contentEnd - contentStart);
            result.Append(ProcessEachLoop(varName, content, variables));

            pos = contentEnd + closeTag.Length;
        }

        return result.ToString();
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
        // Handle JsonElement arrays — convert eagerly to avoid ObjectDisposedException
        else if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
            {
                var itemVars = new Dictionary<string, object>(variables);
                var d = JsonElementToDictionary(item);
                foreach (var (k, v) in d) itemVars[k] = v;
                itemVars["this"] = d.Count > 0
                    ? (object)d
                    : (item.ValueKind == System.Text.Json.JsonValueKind.String ? item.GetString() ?? "" : "");
                sb.AppendLine(Render(content, itemVars));
            }
        }
        // Handle IEnumerable<object>
        else if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var itemVars = new Dictionary<string, object>(variables) { ["this"] = item };

                if (item is Dictionary<string, object> dictItem)
                {
                    foreach (var (k, v) in dictItem)
                        itemVars[k] = v;
                }
                else if (item is System.Text.Json.JsonElement je2)
                {
                    var d = JsonElementToDictionary(je2);
                    foreach (var (k, v) in d) itemVars[k] = v;
                }
                else if (item is not null and not string)
                {
                    foreach (var prop in item.GetType().GetProperties(
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        var v = prop.GetValue(item);
                        if (v is not null)
                            itemVars[prop.Name] = v;
                    }
                }

                sb.AppendLine(Render(content, itemVars));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts a JsonElement (object or array) into a Dictionary for template variable injection.
    /// </summary>
    private static Dictionary<string, object> JsonElementToDictionary(System.Text.Json.JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
            return dict;

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String  => (object)(prop.Value.GetString() ?? ""),
                System.Text.Json.JsonValueKind.True    => true,
                System.Text.Json.JsonValueKind.False   => false,
                System.Text.Json.JsonValueKind.Number  => prop.Value.TryGetInt32(out var i) ? i : (object)prop.Value.GetDouble(),
                System.Text.Json.JsonValueKind.Array   => prop.Value.EnumerateArray()
                    .Select(e => e.ValueKind == System.Text.Json.JsonValueKind.String
                        ? (object)(e.GetString() ?? "")
                        : (object)JsonElementToDictionary(e))
                    .ToList<object>(),
                System.Text.Json.JsonValueKind.Object  => (object)JsonElementToDictionary(prop.Value),
                _ => (object)""
            };
        }
        return dict;
    }

    private static bool EvaluateCondition(string condition, Dictionary<string, object> variables)
    {
        var comparisonOps = new[] { "==", "!=", ">=", "<=", ">", "<" };
        foreach (var op in comparisonOps)
        {
            if (condition.Contains(op))
            {
                var parts = condition.Split(new[] { op }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left  = EvaluateExpression(parts[0].Trim(), variables);
                    var right = EvaluateExpression(parts[1].Trim(), variables);
                    return CompareValues(left, right, op);
                }
            }
        }

        var value = EvaluateExpression(condition, variables);
        return value switch
        {
            bool b => b,
            int i  => i != 0,
            double d => d != 0,
            string s => !string.IsNullOrEmpty(s),
            Array a  => a.Length > 0,
            System.Text.Json.JsonElement j when j.ValueKind == System.Text.Json.JsonValueKind.True   => true,
            System.Text.Json.JsonElement j when j.ValueKind == System.Text.Json.JsonValueKind.False  => false,
            System.Text.Json.JsonElement j when j.ValueKind == System.Text.Json.JsonValueKind.Number => j.GetDouble() != 0,
            System.Text.Json.JsonElement j when j.ValueKind == System.Text.Json.JsonValueKind.String => !string.IsNullOrEmpty(j.GetString()),
            System.Text.Json.JsonElement j when j.ValueKind == System.Text.Json.JsonValueKind.Null   => false,
            System.Text.Json.JsonElement => true,
            _ => value != null
        };
    }

    private static bool CompareValues(object? left, object? right, string op)
    {
        if (left == null || right == null)
            return op == "!=" ? left != right : left == right;

        var leftNum  = Convert.ToDouble(left);
        var rightNum = Convert.ToDouble(right);

        return op switch
        {
            "==" => Math.Abs(leftNum - rightNum) < 0.0001,
            "!=" => Math.Abs(leftNum - rightNum) >= 0.0001,
            ">"  => leftNum > rightNum,
            "<"  => leftNum < rightNum,
            ">=" => leftNum >= rightNum,
            "<=" => leftNum <= rightNum,
            _    => false
        };
    }

    private static object? EvaluateExpression(string expr, Dictionary<string, object> variables)
    {
        expr = expr.Trim();

        if (int.TryParse(expr, out var intVal))
            return intVal;
        if (double.TryParse(expr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
            return doubleVal;

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
                    var left  = EvaluateExpression(parts[0].Trim(), variables);
                    var right = EvaluateExpression(parts[1].Trim(), variables);
                    if (left != null && right != null)
                        return func(Convert.ToDouble(left), Convert.ToDouble(right));
                }
            }
        }

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

                if (propName == "length" && current is Array arr)
                {
                    current = arr.Length;
                    continue;
                }

                if (current is Dictionary<string, object> dict && dict.TryGetValue(propName, out var val))
                {
                    current = val;
                    continue;
                }

                if (current is System.Text.Json.JsonElement je)
                {
                    if (je.TryGetProperty(propName, out var jeProp))
                    {
                        current = jeProp.ValueKind switch
                        {
                            System.Text.Json.JsonValueKind.String => (object)(jeProp.GetString() ?? ""),
                            System.Text.Json.JsonValueKind.True   => true,
                            System.Text.Json.JsonValueKind.False  => false,
                            System.Text.Json.JsonValueKind.Number => jeProp.TryGetInt32(out var n) ? n : (object)jeProp.GetDouble(),
                            _ => jeProp
                        };
                        continue;
                    }
                    return null;
                }

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

        return variables.TryGetValue(expr, out var value) ? value : null;
    }
}
