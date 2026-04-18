using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Retruxel.Core.Models.Wizard;
using Retruxel.Views.Wizard.Steps;

namespace Retruxel.Views.Wizard;

public partial class ToolWizardWindow : Window
{
    private readonly ToolWizardData _data = new();
    private int _currentStep = 0;
    private readonly List<UserControl> _steps = new();

    public ToolWizardWindow()
    {
        InitializeComponent();
        InitializeSteps();
        ShowStep(0);
    }

    private void InitializeSteps()
    {
        _steps.Add(new ToolStep1BasicInfo(_data));
        _steps.Add(new ToolStep2Configuration(_data));
        _steps.Add(new ToolStep3Parameters(_data));
    }

    private void ShowStep(int stepIndex)
    {
        _currentStep = stepIndex;
        ContentArea.Content = _steps[stepIndex];

        // Update header
        StepIndicator.Text = $"{stepIndex + 1} / {_steps.Count}";
        StepTitle.Text = stepIndex switch
        {
            0 => "Step 1: Basic Information",
            1 => "Step 2: Configuration",
            2 => "Step 3: Parameters",
            _ => "Unknown Step"
        };

        // Update buttons
        BackButton.IsEnabled = stepIndex > 0;
        NextButton.Content = stepIndex == _steps.Count - 1 ? "Generate" : "Next →";
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 0)
        {
            ShowStep(_currentStep - 1);
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        // Validate current step
        if (!ValidateCurrentStep())
        {
            MessageBox.Show("Please fill in all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentStep < _steps.Count - 1)
        {
            // Move to next step
            ShowStep(_currentStep + 1);
        }
        else
        {
            // Final step - generate files
            GenerateTool();
        }
    }

    private bool ValidateCurrentStep()
    {
        return _currentStep switch
        {
            0 => !string.IsNullOrWhiteSpace(_data.ToolId) && 
                 !string.IsNullOrWhiteSpace(_data.DisplayName),
            1 => true, // Configuration is optional
            2 => true, // Parameters are optional
            _ => true
        };
    }

    private void GenerateTool()
    {
        try
        {
            // Choose output directory
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Choose output directory",
                FileName = "tool_folder",
                DefaultExt = ".folder",
                Filter = "Folder|*.folder"
            };

            if (dialog.ShowDialog() != true)
                return;

            var outputDir = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (string.IsNullOrEmpty(outputDir))
                return;

            var toolName = _data.DisplayName.Replace(" ", "");
            _data.OutputPath = System.IO.Path.Combine(outputDir, $"Retruxel.Tool.{toolName}");

            // Generate files
            var generator = new Core.Services.Wizard.ToolGenerator();
            generator.Generate(_data);

            MessageBox.Show(
                $"Tool generated successfully!\n\nOutput: {_data.OutputPath}\n\nNext steps:\n1. Open the .csproj in Visual Studio\n2. Implement the Execute() method\n3. Build and copy the DLL to Retruxel/Tools/",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating tool: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to cancel? All progress will be lost.", "Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            Close();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
