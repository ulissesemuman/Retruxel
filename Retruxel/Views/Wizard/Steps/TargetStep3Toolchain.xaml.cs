using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class TargetStep3Toolchain : UserControl
{
    private readonly TargetWizardData _data;

    public TargetStep3Toolchain(TargetWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadData();
        AttachHandlers();
    }

    private void LoadData()
    {
        CompilerComboBox.SelectedIndex = _data.Compiler.ToLower() switch
        {
            "sdcc" => 0,
            "cc65" => 1,
            "custom" => 2,
            _ => 0
        };
        RomExtensionTextBox.Text = _data.RomExtension;
    }

    private void AttachHandlers()
    {
        CompilerComboBox.SelectionChanged += (s, e) =>
        {
            _data.Compiler = (CompilerComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "sdcc";
        };
        RomExtensionTextBox.TextChanged += (s, e) => 
        {
            var ext = RomExtensionTextBox.Text.Trim();
            if (!ext.StartsWith("."))
                ext = "." + ext;
            _data.RomExtension = ext;
        };
    }
}
