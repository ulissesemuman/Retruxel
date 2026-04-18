using Retruxel.Core.Services;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Retruxel;

public partial class SplashScreen : Window
{
    private const double ProgressBarMaxWidth = 280;

    public SplashScreen()
    {
        InitializeComponent();
        VersionLabel.Text = $"v{GetAppVersion()}";
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private static string GetAppVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}-alpha" : "0.0.0-alpha";
    }

    /// <summary>
    /// Runs the initialization sequence, reporting progress to the splash screen.
    /// Calls onComplete when finished so the caller can show the main window.
    /// </summary>
    public async Task RunAsync(Func<IProgress<string>, Task> initAction, Action onComplete)
    {
        var progress = new Progress<string>(message => Dispatcher.Invoke(() =>
        {
            AddLogEntry(message);
        }));

        await initAction(progress);
        await AnimateToComplete();
        onComplete();
    }

    /// <summary>
    /// Adds a log entry to the terminal-style log panel.
    /// </summary>
    public void AddLogEntry(string message)
    {
        var entry = new TextBlock
        {
            Text = $"> {message}",
            Style = (Style)FindResource("TextCode"),
            Margin = new Thickness(0, 0, 0, 2),
            TextWrapping = TextWrapping.Wrap
        };

        LogPanel.Children.Add(entry);
        UpdateProgress(LogPanel.Children.Count);
    }

    /// <summary>
    /// Updates the progress bar and percentage label based on step count.
    /// </summary>
    private void UpdateProgress(int steps)
    {
        var percent = Math.Min(steps * 8, 95);
        PercentLabel.Text = $"{percent}%";
        StatusLabel.Text = percent < 50 ? LocalizationService.Instance.Get("splash.loading_core") :
                           percent < 80 ? LocalizationService.Instance.Get("splash.mapping_memory") :
                                          LocalizationService.Instance.Get("splash.mounting_kernel");
        ProgressBar.Width = ProgressBarMaxWidth * percent / 100.0;
    }

    /// <summary>
    /// Animates the progress bar to 100% and updates labels on completion.
    /// </summary>
    private async Task AnimateToComplete()
    {
        PercentLabel.Text = "100%";
        StatusLabel.Text = LocalizationService.Instance.Get("splash.ready");
        ProgressBar.Width = ProgressBarMaxWidth;
        ProgressBar.Background = (Brush)FindResource("BrushPrimary");
        await Task.Delay(600);
    }
}