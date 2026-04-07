using Microsoft.Win32;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Retruxel.Views;

public partial class BuildConsoleView : UserControl
{
    private BuildResult? _lastResult;
    private RetruxelProject? _project;

    public BuildConsoleView()
    {
        InitializeComponent();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var loc = Retruxel.Core.Services.LocalizationService.Instance;
        TxtBuildTitle.Text = loc.Get("buildconsole.title.build");
        TxtConsoleTitle.Text = loc.Get("buildconsole.title.console");
        TxtDescription.Text = loc.Get("buildconsole.description.ready");
        TxtStatusLabel.Text = loc.Get("buildconsole.status");
        TxtStatus.Text = loc.Get("build.status.ready");
        TxtExportOptions.Text = loc.Get("buildconsole.export_options");
        TxtExportRom.Text = loc.Get("buildconsole.export_rom");
        TxtExportRomDesc.Text = loc.Get("buildconsole.export_rom.desc");
        TxtExportDebug.Text = loc.Get("buildconsole.export_debug");
        TxtExportDebugDesc.Text = loc.Get("buildconsole.export_debug.desc");
        TxtVerification.Text = loc.Get("buildconsole.verification");
        TxtMd5Label.Text = loc.Get("buildconsole.verification.md5");
        TxtSha256Label.Text = loc.Get("buildconsole.verification.sha256");
        TxtChecks.Text = loc.Get("buildconsole.verification.passed");
        MemoryHeader.Text = loc.Get("buildconsole.memory");
        TxtPrgRomLabel.Text = loc.Get("buildconsole.memory.prgrom");
    }

    /// <summary>
    /// Starts the build pipeline for the given project.
    /// Resolves the target from the registry, registers built-in and compatible
    /// plugin modules, then runs code generation and compilation.
    /// </summary>
    public async Task BuildAsync(RetruxelProject project)
    {
        _project = project;
        _lastResult = null;
        LogPanel.Children.Clear();
        VerificationPanel.Visibility = Visibility.Collapsed;
        MemoryHeader.Visibility = Visibility.Collapsed;
        MemoryPanel.Visibility = Visibility.Collapsed;

        var loc = Retruxel.Core.Services.LocalizationService.Instance;

        // Resolve target from registry using the project's TargetId
        var target = TargetRegistry.GetTargetById(project.TargetId);
        if (target is null)
        {
            SetStatus(loc.Get("build.status.failed"), false, isError: true);
            AppendLog($"ERROR: Unknown target '{project.TargetId}'. Cannot build.", isError: true);
            return;
        }

        SetStatus(loc.Get("build.status.building"), false);
        TxtDescription.Text = $"Compiling project '{project.Name}' for target {project.TargetId.ToUpper()}.";

        // Build module loader: built-in modules first, then compatible plugins
        var moduleLoader = new ModuleLoader(AppDomain.CurrentDomain.BaseDirectory);
        moduleLoader.RegisterBuiltinModules(target);
        moduleLoader.LoadCompatible(project.TargetId);

        // Register universal modules present in the project that weren't
        // covered by built-ins or plugins (e.g. text.display from Retruxel.Modules)
        foreach (var moduleId in project.DefaultModules.Distinct())
        {
            if (moduleId == "text.display" && !moduleLoader.LogicModules.ContainsKey(moduleId))
                moduleLoader.RegisterLogicModule(new Retruxel.Modules.Graphics.TextDisplayModule());
        }

        var progress = new Progress<string>(msg => Dispatcher.Invoke(() => AppendLog(msg)));
        var codeGen = new CodeGenerator(moduleLoader, target);

        var outputDir = Path.Combine(project.ProjectPath, "build");
        Directory.CreateDirectory(outputDir);

        var context = await codeGen.GenerateAsync(project, outputDir, progress);

        var toolchain = target.GetToolchain();

        AppendLog("EXTRACTING: toolchain...");
        await toolchain.ExtractAsync(progress);

        AppendLog("COMPILING: running SDCC...");
        _lastResult = await toolchain.BuildAsync(context, progress);

        if (_lastResult.Success)
        {
            if (_lastResult.RomPath != null)
                AppendLog($"SAVED: {_lastResult.RomPath}");

            SetStatus(loc.Get("build.status.success"), true);
            ShowVerification(_lastResult);
            ShowMemoryStats(_lastResult, target.Specs.RomMaxBytes);
            AppendLog($"SUCCESS: ROM generated — {_lastResult.RomSizeBytes / 1024}KB", true);

            await LaunchEmulatorIfConfiguredAsync(_lastResult.RomPath);
        }
        else
        {
            SetStatus(loc.Get("build.status.failed"), false, isError: true);
            AppendLog("ERROR: Build failed. Check log for details.", isError: true);
        }
    }

    private async Task LaunchEmulatorIfConfiguredAsync(string? romPath)
    {
        if (romPath is null || _project is null) return;

        var settings = await SettingsService.LoadAsync();
        
        // Get settings for the current target
        var (launchEnabled, emulatorPath, emulatorArgs) = _project.TargetId.ToLower() switch
        {
            "sms" => (settings.Targets.Sms.LaunchEmulatorAfterBuild, 
                      settings.Targets.Sms.EmulatorPath, 
                      settings.Targets.Sms.EmulatorArguments),
            "nes" => (settings.Targets.Nes.LaunchEmulatorAfterBuild, 
                      settings.Targets.Nes.EmulatorPath, 
                      settings.Targets.Nes.EmulatorArguments),
            "gg" => (settings.Targets.Gg.LaunchEmulatorAfterBuild, 
                     settings.Targets.Gg.EmulatorPath, 
                     settings.Targets.Gg.EmulatorArguments),
            "sg1000" => (settings.Targets.Sg1000.LaunchEmulatorAfterBuild, 
                         settings.Targets.Sg1000.EmulatorPath, 
                         settings.Targets.Sg1000.EmulatorArguments),
            "coleco" => (settings.Targets.Coleco.LaunchEmulatorAfterBuild, 
                         settings.Targets.Coleco.EmulatorPath, 
                         settings.Targets.Coleco.EmulatorArguments),
            _ => (false, string.Empty, string.Empty)
        };
        
        if (!launchEnabled)
            return;

        if (string.IsNullOrEmpty(emulatorPath))
        {
            AppendLog($"WARN: Emulator path not configured for {_project.TargetId.ToUpper()}. Set it in Settings.");
            return;
        }

        if (!File.Exists(emulatorPath))
        {
            AppendLog($"ERROR: Emulator not found at {emulatorPath}", isError: true);
            return;
        }

        try
        {
            var args = string.IsNullOrEmpty(emulatorArgs) 
                ? $"\"{romPath}\""
                : $"{emulatorArgs} \"{romPath}\"";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = emulatorPath,
                Arguments = args,
                UseShellExecute = true
            });
            AppendLog($"LAUNCH: Opening ROM in emulator...");
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: Failed to launch emulator — {ex.Message}", isError: true);
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

    private void ShowMemoryStats(BuildResult result, int romMaxBytes)
    {
        MemoryHeader.Visibility = Visibility.Visible;
        MemoryPanel.Visibility = Visibility.Visible;

        var maxKb = romMaxBytes / 1024;
        var percent = romMaxBytes > 0 ? (double)result.RomSizeBytes / romMaxBytes : 0;
        TxtRomSize.Text = $"{result.RomSizeBytes / 1024}KB / {maxKb}KB";
        RomSizeBar.Width = 268 * Math.Min(percent, 1.0);
    }

    private void ExportRom_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastResult?.RomPath is null)
        {
            MessageBox.Show("No ROM available. Run a build first.", "Retruxel");
            return;
        }

        var ext = Path.GetExtension(_lastResult.RomPath).TrimStart('.');
        var dialog = new SaveFileDialog
        {
            Filter = $"ROM File (*.{ext})|*.{ext}",
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

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var lines = LogPanel.Children
            .OfType<TextBlock>()
            .Select(tb => tb.Text);

        var logText = string.Join(Environment.NewLine, lines);

        if (string.IsNullOrEmpty(logText))
        {
            MessageBox.Show("No log content to copy.", "Retruxel");
            return;
        }

        try
        {
            Clipboard.SetText(logText);
            ShowToast(LocalizationService.Instance.Get("toast.log_copied"));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Retruxel");
        }
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;

        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200)
        };

        var fadeOut = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            BeginTime = TimeSpan.FromMilliseconds(2000)
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(fadeIn);
        storyboard.Children.Add(fadeOut);

        Storyboard.SetTarget(fadeIn, ToastNotification);
        Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
        Storyboard.SetTarget(fadeOut, ToastNotification);
        Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));

        storyboard.Begin();
    }
}