using System.Windows.Data;
using System.Windows.Markup;
using Retruxel.Core.Services;

namespace Retruxel.Localization;

/// <summary>
/// XAML markup extension for localized strings.
/// Usage: {loc:Tr Key='welcome.title'}
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class TrExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return "[MISSING_KEY]";

        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
