using System.IO;
using System.Reflection;

namespace Retruxel.Tool.Wizard.Services;

/// <summary>
/// Loads template files from the Templates directory.
/// </summary>
public static class TemplateLoader
{
    public static string LoadTemplate(string templateName)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        var templatePath = Path.Combine(assemblyDir!, "Templates", templateName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templateName}", templatePath);
        }

        return File.ReadAllText(templatePath);
    }
}
