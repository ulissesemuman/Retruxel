using Microsoft.Win32;
using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Core.Services;
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
    private DateTime _buildStartTime;
    private BuildDiagnosticsPanel? _diagnosticsPanel;

    public BuildConsoleView()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateTooltips();
    }

    public void UpdateTooltips()
    {
        var loc = LocalizationService.Instance;
        var copyBtn = LogicalTreeHelper.FindLogicalNode(this, "BtnCopyLog") as Button;
        var saveBtn = LogicalTreeHelper.FindLogicalNode(this, "BtnSaveLog") as Button;
        if (copyBtn != null) copyBtn.ToolTip = loc.Get("buildconsole.tooltip.copy");
        if (saveBtn != null) saveBtn.ToolTip = loc.Get("buildconsole.tooltip.save");
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
        TotalAssetsPanel.Visibility = Visibility.Collapsed;
        ElapsedTimePanel.Visibility = Visibility.Collapsed;

        _buildStartTime = DateTime.Now;

        var loc = LocalizationService.Instance;

        // Resolve target from registry using the project's TargetId
        var target = TargetRegistry.GetTargetById(project.TargetId);
        if (target is null)
        {
            SetStatus(loc.Get("build.status.failed"), false, isError: true);
            AppendLog($"ERROR: Unknown target '{project.TargetId}'. Cannot build.", isError: true);
            return;
        }

        SetStatus(loc.Get("build.status.building"), false);
        TxtDescription.Text = string.Format(loc.Get("buildconsole.compiling"), project.Name, project.TargetId.ToUpper());

        // Build module registry: orchestrates module loading and codegen discovery
        var moduleRegistry = new ModuleRegistry(AppDomain.CurrentDomain.BaseDirectory);
        moduleRegistry.RegisterBuiltinModules(target);
        
        var progress = new Progress<string>(msg => Dispatcher.Invoke(() => AppendLog(msg)));
        moduleRegistry.LoadForTarget(target, progress);

        // Create ModuleRenderer for declarative CodeGens
        var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        ((IProgress<string>)progress).Report($"DEBUG: Plugins path = {pluginsPath}");
        ((IProgress<string>)progress).Report($"DEBUG: Directory exists = {Directory.Exists(pluginsPath)}");
        var moduleRenderer = new ModuleRenderer(pluginsPath, null, progress);

        // Register universal modules present in the project that weren't
        // covered by built-ins or plugins (e.g. text.display from Retruxel.Modules)
        foreach (var moduleId in project.DefaultModules.Distinct())
        {
            if (moduleId == "text.display" && !moduleRegistry.GraphicModules.ContainsKey(moduleId))
                moduleRegistry.RegisterGraphicModule(new Retruxel.Modules.Graphics.TextDisplayModule());
        }

        var codeGen = new CodeGenerator(moduleRegistry, moduleRenderer, target);

        var outputDir = Path.Combine(project.ProjectPath, "build");
        Directory.CreateDirectory(outputDir);

        var context = await codeGen.GenerateAsync(project, outputDir, progress);

        // Show diagnostics if available
        if (context.Diagnostics is not null)
        {
            ShowDiagnostics(context.Diagnostics);
        }

        var toolchain = target.GetToolchain();

        AppendLog("EXTRACTING: toolchain...");
        await toolchain.ExtractAsync(progress);

        AppendLog("COMPILING: running SDCC...");
        _lastResult = await toolchain.BuildAsync(context, progress);

        if (_lastResult.Success)
        {
            var elapsedTime = DateTime.Now - _buildStartTime;
            var totalElements = project.Scenes.Sum(s => s.Elements.Count);

            if (_lastResult.RomPath != null)
                AppendLog($"SAVED: {_lastResult.RomPath}");

            SetStatus(loc.Get("build.status.success"), true);
            ShowVerification(_lastResult);
            ShowMemoryStats(_lastResult, target);
            ShowBuildStats(totalElements, elapsedTime);
            AppendLog($"SUCCESS: ROM generated — {_lastResult.RomSizeBytes / 1024}KB", isSuccess: true);

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
        var targetSettings = SettingsService.GetTargetSettings(settings, _project.TargetId);

        if (!targetSettings.LaunchEmulatorAfterBuild)
            return;

        if (string.IsNullOrEmpty(targetSettings.EmulatorPath))
        {
            AppendLog($"WARN: Emulator path not configured for {_project.TargetId.ToUpper()}. Set it in Settings.");
            return;
        }

        if (!File.Exists(targetSettings.EmulatorPath))
        {
            AppendLog($"ERROR: Emulator not found at {targetSettings.EmulatorPath}", isError: true);
            return;
        }

        try
        {
            var args = string.IsNullOrEmpty(targetSettings.EmulatorArguments)
                ? $"\"{romPath}\""
                : $"{targetSettings.EmulatorArguments} \"{romPath}\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = targetSettings.EmulatorPath,
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
        // Use theme brushes — avoid hardcoded hex colors
        Brush foreground = isError ? (Brush)FindResource("BrushError") :
                           isSuccess ? (Brush)FindResource("BrushPrimary") :
                           message.StartsWith("WARN") ? (Brush)FindResource("BrushWarning") :
                                        (Brush)FindResource("BrushOnSurfaceVariant");

        var entry = new TextBlock
        {
            Text = $"> {message}",
            Foreground = foreground,

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
        var brush = isError ? (Brush)FindResource("BrushError") :
                    isReady ? (Brush)FindResource("BrushPrimary") :
                                (Brush)FindResource("BrushWarning");

        TxtStatus.Foreground = brush;
        StatusDot.Background = brush;
    }

    private void ShowVerification(BuildResult result)
    {
        VerificationPanel.Visibility = Visibility.Visible;
        TxtMd5.Text = result.RomMd5?[..16] + "...";
        TxtMd5.Tag = result.RomMd5;
        TxtMd5.ToolTip = result.RomMd5;
        TxtSha256.Text = result.RomSha256?[..16] + "...";
        TxtSha256.Tag = result.RomSha256;
        TxtSha256.ToolTip = result.RomSha256;
    }

    private void ShowMemoryStats(BuildResult result, ITarget target)
    {
        MemoryHeader.Visibility = Visibility.Visible;
        MemoryPanel.Visibility = Visibility.Visible;
        MemoryPanel.Children.Clear();

        foreach (var bank in target.Specs.Banks)
        {
            var usedBytes = result.BankUsage.TryGetValue(bank.Id, out var used) ? used : 0;
            var maxKb = bank.MaxBytes / 1024;
            var usedKb = usedBytes / 1024;
            var percent = bank.MaxBytes > 0 ? (double)usedBytes / bank.MaxBytes : 0;
            var isOver = usedBytes > bank.MaxBytes;

            var normalBrush = (Brush)FindResource("BrushOnSurfaceVariant");
            var errorBrush = (Brush)FindResource("BrushError");


            // Label row
            var labelGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bankLabel = new TextBlock
            {
                Text = bank.Label,
                Style = (Style)FindResource("TextLabel"),
                Foreground = isOver ? errorBrush : normalBrush
            };
            Grid.SetColumn(bankLabel, 0);

            var sizeLabel = new TextBlock
            {
                Text = $"{usedKb}KB / {maxKb}KB",
                Style = (Style)FindResource("TextLabel"),
                Foreground = isOver ? errorBrush : (Brush)FindResource("BrushOnSurface"),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontWeight = isOver ? FontWeights.Bold : FontWeights.Normal
            };
            Grid.SetColumn(sizeLabel, 1);

            labelGrid.Children.Add(bankLabel);
            labelGrid.Children.Add(sizeLabel);

            // Progress bar
            var barContainer = new Border
            {
                Height = 4,
                Background = (Brush)FindResource("BrushSurfaceContainerHighest"),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var progressBar = new Border
            {
                Background = isOver ? errorBrush : (Brush)FindResource("BrushPrimaryGradient"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 268 * percent,
                MaxWidth = 268
            };

            barContainer.Child = progressBar;

            MemoryPanel.Children.Add(labelGrid);
            MemoryPanel.Children.Add(barContainer);
        }
    }

    private void ShowBuildStats(int totalElements, TimeSpan elapsedTime)
    {
        TotalAssetsPanel.Visibility = Visibility.Visible;
        TotalAssets.Text = totalElements.ToString();

        ElapsedTimePanel.Visibility = Visibility.Visible;
        TxtElapsedTime.Text = $"{elapsedTime.TotalSeconds:F2}s";
    }

    private void ShowDiagnostics(BuildDiagnosticsReport report)
    {
        // Create diagnostics panel if it doesn't exist
        if (_diagnosticsPanel is null)
        {
            _diagnosticsPanel = new BuildDiagnosticsPanel();
            
            // Find the parent container (assuming LogPanel is in a parent StackPanel)
            var parent = LogPanel.Parent as Panel;
            if (parent is not null)
            {
                // Add diagnostics panel after the log scroll viewer
                var logScrollViewer = parent.Children.OfType<ScrollViewer>()
                    .FirstOrDefault(sv => sv.Name == "LogScrollViewer");
                
                if (logScrollViewer is not null)
                {
                    var index = parent.Children.IndexOf(logScrollViewer);
                    parent.Children.Insert(index + 1, _diagnosticsPanel);
                }
            }
        }

        // Update diagnostics panel with new report
        _diagnosticsPanel.Report = report;
    }

    private void ExportRom_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastResult?.RomPath is null)
        {
            MessageBox.Show(LocalizationService.Instance.Get("buildconsole.error.no_rom"), "Retruxel");
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
            MessageBox.Show(LocalizationService.Instance.Get("buildconsole.error.no_build"), "Retruxel");
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
        var loc = LocalizationService.Instance;
        var dialog = new SaveFileDialog
        {
            Filter = loc.Get("buildconsole.savelog.filter"),
            FileName = string.Format(loc.Get("buildconsole.savelog.filename"), DateTime.Now.ToString("yyyyMMdd_HHmmss"))
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
            MessageBox.Show(LocalizationService.Instance.Get("buildconsole.error.no_log"), "Retruxel");
            return;
        }

        try
        {
            Clipboard.SetText(logText);
            ShowToast(LocalizationService.Instance.Get("toast.log_copied"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance.Get("buildconsole.error.clipboard"), ex.Message), "Retruxel");
        }
    }

    private void CopyMd5_Click(object sender, RoutedEventArgs e)
    {
        if (TxtMd5.Text == "N/A" || TxtMd5.Tag is null) return;
        try
        {
            Clipboard.SetText(TxtMd5.Tag.ToString()!);
            ShowToast(LocalizationService.Instance.Get("toast.md5_copied"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance.Get("buildconsole.error.clipboard"), ex.Message), "Retruxel");
        }
    }

    private void CopySha256_Click(object sender, RoutedEventArgs e)
    {
        if (TxtSha256.Text == "N/A" || TxtSha256.Tag is null) return;
        try
        {
            Clipboard.SetText(TxtSha256.Tag.ToString()!);
            ShowToast(LocalizationService.Instance.Get("toast.sha256_copied"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance.Get("buildconsole.error.clipboard"), ex.Message), "Retruxel");
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