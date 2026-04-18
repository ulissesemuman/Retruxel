using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;

namespace Retruxel.Views.Wizard.Steps;

public partial class CodeGenStep4Dependencies : UserControl
{
    private readonly CodeGenWizardData _data;
    private readonly List<string> _availableTools = new()
    {
        "retruxel.tool.assetimporter",
        "retruxel.tool.fontimporter",
        "retruxel.tool.pixelarteditor",
        "retruxel.tool.tilemapReducer"
    };

    public CodeGenStep4Dependencies(CodeGenWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadTools();
        UpdateSelectedToolsDisplay();
    }

    private void LoadTools()
    {
        foreach (var tool in _availableTools)
        {
            var checkbox = new CheckBox
            {
                Content = tool,
                Margin = new System.Windows.Thickness(0, 0, 0, 8),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)),
                IsChecked = _data.RequiredTools.Contains(tool)
            };

            checkbox.Checked += (s, e) =>
            {
                if (!_data.RequiredTools.Contains(tool))
                {
                    _data.RequiredTools.Add(tool);
                    UpdateSelectedToolsDisplay();
                }
            };

            checkbox.Unchecked += (s, e) =>
            {
                _data.RequiredTools.Remove(tool);
                UpdateSelectedToolsDisplay();
            };

            ToolsPanel.Children.Add(checkbox);
        }
    }

    private void UpdateSelectedToolsDisplay()
    {
        if (_data.RequiredTools.Count == 0)
        {
            SelectedToolsText.Text = "No tools selected";
            SelectedToolsText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(136, 136, 136));
        }
        else
        {
            SelectedToolsText.Text = string.Join("\n", _data.RequiredTools);
            SelectedToolsText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
        }
    }
}
