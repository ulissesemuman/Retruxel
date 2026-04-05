using Microsoft.Win32;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Target.SMS;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Views;

public partial class BuildConsoleView : UserControl
{
    private BuildResult? _lastResult;
    private RetruxelProject? _project;

    public BuildConsoleView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Starts the build pipeline for the given project.
    /// </summary>
    public async Task BuildAsync(RetruxelProject project)
    {
        _project = project;
        _lastResult = null;
        LogPanel.Children.Clear();
        VerificationPanel.Visibility = Visibility.Collapsed;
        MemoryHeader.Visibility = Visibility.Collapsed;
        MemoryPanel.Visibility = Visibility.Collapsed;

        SetStatus("BUILDING...", false);
        TxtDescription.Text = $"Compiling project '{project.Name}' for target {project.TargetId.ToUpper()}.";

        // Reset static counters before build
        Retruxel.Target.SMS.Modules.Text.SmsTextDisplayCodeGen.ResetCounter();

        var target = new SmsTarget();
        var moduleLoader = new ModuleLoader(AppDomain.CurrentDomain.BaseDirectory);

        // Only register module templates if there are instances to process
        if (project.ModuleStates.Count > 0)
        {
            foreach (var moduleId in project.DefaultModules.Distinct())
            {
                if (moduleId == "text.display")
                {
                    var template = new Retruxel.Modules.Text.TextDisplayModule();
                    moduleLoader.RegisterLogicModule(template);
                }
            }
        }

        var progress = new Progress<string>(msg => Dispatcher.Invoke(() => AppendLog(msg)));
        var codeGen = new CodeGenerator(moduleLoader, target);
        var context = await codeGen.GenerateAsync(project, progress);

        var toolchain = target.GetToolchain();

        AppendLog("EXTRACTING: toolchain...");
        await toolchain.ExtractAsync(progress);

        AppendLog("COMPILING: running SDCC...");
        _lastResult = await toolchain.BuildAsync(context, progress);

        if (_lastResult.Success)
        {
            // Copy ROM to project directory
            if (!string.IsNullOrEmpty(project.ProjectPath) && _lastResult.RomPath != null)
            {
                var destRomPath = Path.Combine(project.ProjectPath, $"{project.Name}.sms");
                File.Copy(_lastResult.RomPath, destRomPath, overwrite: true);
                AppendLog($"SAVED: {destRomPath}");
            }

            SetStatus("BUILD SUCCESSFUL", true);
            ShowVerification(_lastResult);
            ShowMemoryStats(_lastResult);
            AppendLog($"SUCCESS: ROM generated — {_lastResult.RomSizeBytes / 1024}KB", true);
        }
        else
        {
            SetStatus("BUILD FAILED", false, isError: true);
            AppendLog("ERROR: Build failed. Check log for details.", isError: true);
        }
    }

    private void AppendLog(string message, bool isSuccess = false, bool isError = false)
    {
        var color = isError ? "#FF4444" :
                    isSuccess ? "#8EFF71" :
                    message.StartsWith("WARN") ? "#FFAA00" : "#ADAAAA";

        var entry = new TextBlock
        {
            Text = $"> {message}",
            Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(color)),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 2)
        };

        LogPanel.Children.Add(entry);
        LogScrollViewer.ScrollToBottom();
    }

    private void SetStatus(string status, bool isReady, bool isError = false)
    {
        TxtStatus.Text = status;
        var color = isError ? "#FF4444" : isReady ? "#8EFF71" : "#FFAA00";
        TxtStatus.Foreground = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(color));
        StatusDot.Background = TxtStatus.Foreground;
    }

    private void ShowVerification(BuildResult result)
    {
        VerificationPanel.Visibility = Visibility.Visible;
        TxtMd5.Text = result.RomMd5?[..8] + "...";
        TxtSha256.Text = result.RomSha256?[..8] + "...";
    }

    private void ShowMemoryStats(BuildResult result)
    {
        MemoryHeader.Visibility = Visibility.Visible;
        MemoryPanel.Visibility = Visibility.Visible;

        var maxRom = 32 * 1024;
        var percent = (double)result.RomSizeBytes / maxRom;
        TxtRomSize.Text = $"{result.RomSizeBytes / 1024}KB / 32KB";
        RomSizeBar.Width = 268 * percent;
    }

    private void ExportRom_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastResult?.RomPath is null)
        {
            MessageBox.Show("No ROM available. Run a build first.", "Retruxel");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "SMS ROM (*.sms)|*.sms",
            FileName = _project?.Name ?? "output"
        };

        if (dialog.ShowDialog() == true)
            File.Copy(_lastResult.RomPath, dialog.FileName, overwrite: true);
    }

    private void ExportDebug_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastResult?.RomPath is null)
        {
            MessageBox.Show("No build available. Run a build first.", "Retruxel");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP Archive (*.zip)|*.zip",
            FileName = $"{_project?.Name ?? "output"}_debug"
        };

        if (dialog.ShowDialog() == true)
        {
            var buildDir = Path.GetDirectoryName(_lastResult.RomPath)!;
            ZipFile.CreateFromDirectory(buildDir, dialog.FileName);
        }
    }

    private void ExportLog_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"retruxel_build_log_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() != true) return;

        var lines = LogPanel.Children
            .OfType<TextBlock>()
            .Select(tb => tb.Text);

        File.WriteAllLines(dialog.FileName, lines);
    }
}