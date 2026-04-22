using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Retruxel.Tool.Wizard.Models;

namespace Retruxel.Tool.Wizard.Views.Steps;

public partial class TargetStep5VariableMapping : UserControl
{
    private readonly TargetWizardData _data;
    private readonly ObservableCollection<VariableMappingItem> _variables = new();

    public TargetStep5VariableMapping(TargetWizardData data)
    {
        InitializeComponent();
        _data = data;
        
        // Create examples grid programmatically to avoid XAML parsing issues with {{}}
        var grid = new System.Windows.Controls.Grid();
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(200) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        
        var examples = new[]
        {
            ("{{projectName}}", "→ project.Name"),
            ("{{targetId}}", "→ project.TargetId"),
            ("{{timestamp}}", "→ DateTime.Now"),
            ("{{includeHeaders}}", "→ moduleFiles.Headers"),
            ("{{initCalls}}", "→ moduleFiles.InitCalls"),
            ("{{updateCalls}}", "→ moduleFiles.UpdateCalls")
        };
        
        for (int i = 0; i < examples.Length; i++)
        {
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            
            var varName = new System.Windows.Controls.TextBlock
            {
                Text = examples[i].Item1,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(78, 201, 176)),
                Margin = new System.Windows.Thickness(0, i > 0 ? 4 : 0, 0, 0)
            };
            System.Windows.Controls.Grid.SetRow(varName, i);
            System.Windows.Controls.Grid.SetColumn(varName, 0);
            grid.Children.Add(varName);
            
            var mapping = new System.Windows.Controls.TextBlock
            {
                Text = examples[i].Item2,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204)),
                Margin = new System.Windows.Thickness(0, i > 0 ? 4 : 0, 0, 0)
            };
            System.Windows.Controls.Grid.SetRow(mapping, i);
            System.Windows.Controls.Grid.SetColumn(mapping, 1);
            grid.Children.Add(mapping);
        }
        
        ExamplesPanel.Children.Add(grid);
        
        DetectVariables();
        VariablesListBox.ItemsSource = _variables;
    }

    private void DetectVariables()
    {
        // Regex to find {{variableName}} patterns
        var regex = new Regex(@"\{\{(\w+)\}\}");
        var matches = regex.Matches(_data.MainFileContent);

        var uniqueVariables = matches
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(v => v);

        foreach (var variable in uniqueVariables)
        {
            var mapping = _data.VariableMappings.ContainsKey(variable) 
                ? _data.VariableMappings[variable] 
                : GetDefaultMapping(variable);

            var item = new VariableMappingItem
            {
                VariableName = $"{{{{{variable}}}}}",
                Mapping = mapping
            };

            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(VariableMappingItem.Mapping))
                {
                    _data.VariableMappings[variable] = item.Mapping;
                }
            };

            _variables.Add(item);
            _data.VariableMappings[variable] = mapping;
        }
    }

    private string GetDefaultMapping(string variable)
    {
        return variable.ToLower() switch
        {
            "projectname" => "project.Name",
            "targetid" => "project.TargetId",
            "timestamp" => "DateTime.Now",
            "includeheaders" => "moduleFiles.Headers",
            "initcalls" => "moduleFiles.InitCalls",
            "updatecalls" => "moduleFiles.UpdateCalls",
            "vblankwait" => "target.VBlankWait",
            "embedheaders" => "target.EmbedHeaders",
            _ => string.Empty
        };
    }
}

public class VariableMappingItem : INotifyPropertyChanged
{
    private string _variableName = string.Empty;
    private string _mapping = string.Empty;

    public string VariableName
    {
        get => _variableName;
        set
        {
            _variableName = value;
            OnPropertyChanged(nameof(VariableName));
        }
    }

    public string Mapping
    {
        get => _mapping;
        set
        {
            _mapping = value;
            OnPropertyChanged(nameof(Mapping));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
