using System.IO;

namespace Retruxel.Core.Services.Wizard;

/// <summary>
/// Loads wizard template files from Assets/WizardTemplates directory.
/// </summary>
public static class WizardTemplateLoader
{
    public static string LoadTemplate(string templateName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var templatePath = Path.Combine(baseDir, "Assets", "WizardTemplates", templateName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template not found: {templateName}", templatePath);
        }

        return File.ReadAllText(templatePath);
    }
}
