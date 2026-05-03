using Retruxel.Core.Models;

namespace Retruxel.Core.Services;

/// <summary>
/// Processes GeneratedFile collections with filtering and transformation.
/// Handles moduleFiles variable source.
/// </summary>
internal static class FileFilterProcessor
{
    public static object ProcessModuleFiles(
        List<GeneratedFile> files,
        VariableDefinition varDef,
        Dictionary<string, object> existingVariables)
    {
        var filtered = files.AsEnumerable();

        if (!string.IsNullOrEmpty(varDef.Path))
        {
            filtered = filtered.Where(f => EvaluateFileFilter(f, varDef.Path, existingVariables));
        }

        if (varDef.GroupBy is not null)
        {
            var grouped = filtered.GroupBy(f => GetFileProperty(f, varDef.GroupBy));
            filtered = grouped.Select(g => g.First());
        }

        if (!string.IsNullOrEmpty(varDef.Transform))
        {
            return filtered.Select(f => TransformFile(f, varDef.Transform, existingVariables)).ToList();
        }

        return filtered.Select(f => f.FileName).ToList();
    }

    private static bool EvaluateFileFilter(GeneratedFile file, string filter, Dictionary<string, object> variables)
    {
        try
        {
            var expr = filter
                .Replace("fileType", $"'{file.FileType}'")
                .Replace("fileName", $"'{file.FileName}'")
                .Replace("sourceModuleId", $"'{file.SourceModuleId}'");

            var parts = expr.Split(new[] { "&&" }, StringSplitOptions.TrimEntries);
            return parts.All(part =>
            {
                if (part.Contains("=="))
                {
                    var tokens = part.Split(new[] { "==" }, StringSplitOptions.TrimEntries);
                    if (tokens.Length == 2)
                    {
                        var left = tokens[0].Trim('\'');
                        var right = tokens[1].Trim('\'');
                        return left == right;
                    }
                }

                if (part.Contains("!="))
                {
                    var tokens = part.Split(new[] { "!=" }, StringSplitOptions.TrimEntries);
                    if (tokens.Length == 2)
                    {
                        var left = tokens[0].Trim('\'');
                        var right = tokens[1].Trim('\'');
                        return left != right;
                    }
                }

                return true;
            });
        }
        catch
        {
            return true;
        }
    }

    private static string TransformFile(GeneratedFile file, string transform, Dictionary<string, object> variables)
    {
        var result = transform
            .Replace("{{fileName}}", file.FileName)
            .Replace("{{sourceModuleId}}", file.SourceModuleId);

        if (result.Contains("sourceModuleId.Replace"))
        {
            result = result.Replace("{{sourceModuleId.Replace('.', '_')}}", file.SourceModuleId.Replace(".", "_"));
        }

        if (result.Contains("Path.GetFileNameWithoutExtension"))
        {
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            result = result.Replace("{{Path.GetFileNameWithoutExtension(fileName)}}", baseName);
        }

        return result;
    }

    private static string GetFileProperty(GeneratedFile file, string propertyName)
    {
        return propertyName.ToLower() switch
        {
            "sourcemoduleid" => file.SourceModuleId,
            "filename" => file.FileName,
            "filetype" => file.FileType.ToString(),
            _ => file.SourceModuleId
        };
    }
}
