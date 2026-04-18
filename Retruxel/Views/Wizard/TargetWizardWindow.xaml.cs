using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Retruxel.Core.Models.Wizard;
using Retruxel.Core.Services;
using Retruxel.Views.Wizard.Steps;

namespace Retruxel.Views.Wizard;

public partial class TargetWizardWindow : Window
{
    private readonly TargetWizardData _data = new();
    private int _currentStep = 0;
    private readonly List<UserControl> _steps = new();

    public TargetWizardWindow()
    {
        InitializeComponent();
        LoadConsoleDatabase();
        InitializeSteps();
        ShowStep(0);
    }

    private void LoadConsoleDatabase()
    {
        var dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data", "consoles-database.json");
        ConsoleDatabaseService.Load(dbPath);
    }

    private void InitializeSteps()
    {
        _steps.Add(new TargetStep1BasicInfo(_data));
        _steps.Add(new TargetStep2Specs(_data));
        _steps.Add(new TargetStep3Toolchain(_data));
        _steps.Add(new TargetStep4MainFile(_data));
        _steps.Add(new TargetStep5VariableMapping(_data));
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
            1 => "Step 2: Technical Specifications",
            2 => "Step 3: Toolchain Configuration",
            3 => "Step 4: Main File Template",
            4 => "Step 5: Variable Mapping",
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
            // Auto-fill Step2 from console database if coming from Step1
            if (_currentStep == 0 && _steps[0] is TargetStep1BasicInfo step1)
            {
                if (_steps[1] is TargetStep2Specs step2)
                {
                    step2.AutoFillFromConsole(step1.SelectedConsole);
                }
            }

            // Move to next step
            ShowStep(_currentStep + 1);
        }
        else
        {
            // Final step - generate files
            GenerateTarget();
        }
    }

    private bool ValidateCurrentStep()
    {
        return _currentStep switch
        {
            0 => !string.IsNullOrWhiteSpace(_data.TargetId) && 
                 !string.IsNullOrWhiteSpace(_data.DisplayName) &&
                 !string.IsNullOrWhiteSpace(_data.Manufacturer),
            1 => !string.IsNullOrWhiteSpace(_data.CPU) && 
                 _data.ScreenWidth > 0 && 
                 _data.ScreenHeight > 0,
            2 => !string.IsNullOrWhiteSpace(_data.Compiler) && 
                 !string.IsNullOrWhiteSpace(_data.RomExtension),
            3 => !string.IsNullOrWhiteSpace(_data.MainFileContent),
            4 => true, // Variable mapping is optional
            _ => true
        };
    }

    private void GenerateTarget()
    {
        try
        {
            // Choose output directory
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Choose output directory",
                FileName = "target_folder",
                DefaultExt = ".folder",
                Filter = "Folder|*.folder"
            };

            if (dialog.ShowDialog() != true)
                return;

            var outputDir = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (string.IsNullOrEmpty(outputDir))
                return;

            _data.OutputPath = System.IO.Path.Combine(outputDir, $"Retruxel.Target.{_data.DisplayName.Replace(" ", "")}");

            // Generate files
            var generator = new Core.Services.Wizard.TargetGenerator();
            generator.Generate(_data);

            MessageBox.Show(
                $"Target generated successfully!\n\nOutput: {_data.OutputPath}\n\nNext steps:\n1. Open the .csproj in Visual Studio\n2. Implement the methods marked with 'throw new NotImplementedException()'\n3. Build and copy the DLL to Retruxel/Targets/",
                "Success",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating target: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
