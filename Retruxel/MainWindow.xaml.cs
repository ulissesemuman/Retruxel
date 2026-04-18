using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Views;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Retruxel;

public partial class MainWindow : Window
{
    private readonly BuildConsoleView _buildConsoleView = new();
    private readonly ProjectManager _projectManager = new();
    private readonly ToolLoader _toolLoader;
    private bool _isDraggingOverlay = false;
    private Point _overlayDragStart;

    public MainWindow()
    {
        InitializeComponent();
        TxtVersion.Text = $"v{GetAppVersion()}";
        WelcomeView.OnProjectCreated += OnProjectCreated;
        WelcomeView.OnAboutRequested += () => ShowOverlay("ABOUT", new AboutView());

        // Initialize ToolLoader with base path
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        _toolLoader = new ToolLoader(basePath);

        // Discover tools on startup
        _toolLoader.DiscoverTools();

        // Ctrl+S to save
        KeyDown += MainWindow_KeyDown;

        // Restore window state
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RestoreWindowState();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowState();
    }

    private void RestoreWindowState()
    {
        var settings = SettingsService.Load();

        if (settings.Window.IsFirstRun)
        {
            // First run: center on screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            settings.Window.IsFirstRun = false;
            SettingsService.Save(settings);
            return;
        }

        // Validate if position is visible on any screen
        var bounds = new Rect(settings.Window.Left, settings.Window.Top,
                              settings.Window.Width, settings.Window.Height);

        // Total area of all combined monitors (Virtual Screen)
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        // Check if the saved window is contained within or at least visible on the virtual screen
        // IntersectsWith ensures the window doesn't "disappear" if a monitor has been disconnected
        bool isVisible = virtualScreen.IntersectsWith(bounds);

        if (isVisible)
        {
            Width = settings.Window.Width;
            Height = settings.Window.Height;
            Left = settings.Window.Left;
            Top = settings.Window.Top;

            if (settings.Window.IsMaximized)
                WindowState = WindowState.Maximized;
        }
        else
        {
            // Position not valid (monitor disconnected?), center on screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void SaveWindowState()
    {
        var settings = SettingsService.Load();

        settings.Window.IsMaximized = WindowState == WindowState.Maximized;

        // Save normal bounds (not maximized size)
        if (WindowState == WindowState.Normal)
        {
            settings.Window.Width = Width;
            settings.Window.Height = Height;
            settings.Window.Left = Left;
            settings.Window.Top = Top;
        }
        else
        {
            settings.Window.Width = RestoreBounds.Width;
            settings.Window.Height = RestoreBounds.Height;
            settings.Window.Left = RestoreBounds.Left;
            settings.Window.Top = RestoreBounds.Top;
        }

        SettingsService.Save(settings);
    }

    private static string GetAppVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}-alpha" : "0.0.0-alpha";
    }

    private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            await SaveProjectAsync();
            e.Handled = true;
        }
    }

    private async Task SaveProjectAsync()
    {
        if (_projectManager.CurrentProject is not null)
        {
            await _projectManager.SaveAsync();
            // TODO: Show save feedback in status bar
        }
    }

    /// <summary>
    /// Shows a view as an overlay on top of the current screen.
    /// </summary>
    private void ShowOverlay(string title, object content)
    {
        OverlayTitle.Text = title;
        OverlayContentPresenter.Content = content;
        OverlayContent.Margin = new Thickness(40); // Reset position
        OverlayLayer.Visibility = Visibility.Visible;

        // Block interaction with background
        TitleBar.IsEnabled = false;
        StatusBar.IsEnabled = false;
        WelcomeView.IsEnabled = false;
        SceneEditorView.IsEnabled = false;
    }

    /// <summary>
    /// Closes the current overlay and returns to the base layer.
    /// </summary>
    private void CloseOverlay_Click(object sender, RoutedEventArgs e)
    {
        OverlayLayer.Visibility = Visibility.Collapsed;
        OverlayContentPresenter.Content = null;

        // Re-enable interaction with background
        TitleBar.IsEnabled = true;
        StatusBar.IsEnabled = true;
        WelcomeView.IsEnabled = true;
        SceneEditorView.IsEnabled = true;
    }

    /// <summary>
    /// Called when a project is created — shows Build Console as overlay.
    /// </summary>
    private async void OnProjectCreated(RetruxelProject project)
    {
        var target = Services.TargetRegistry.GetTargetById(project.TargetId);
        if (target is null)
        {
            MessageBox.Show($"Target '{project.TargetId}' not found.", "Retruxel", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Initialize ModuleRegistry
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var moduleRegistry = new ModuleRegistry(basePath);
        moduleRegistry.RegisterBuiltinModules(target);
        moduleRegistry.LoadForTarget(target.TargetId);

        _projectManager.CurrentProject = project;

        // Show Scene Editor as main view
        WelcomeView.Visibility = Visibility.Collapsed;
        SceneEditorView.Visibility = Visibility.Visible;
        BtnHome.Visibility = Visibility.Visible;
        SceneEditorView.SetProjectManager(_projectManager);
        SceneEditorView.SetModuleRegistry(moduleRegistry);
        SceneEditorView.Initialize(project, target);

        // Save after initialization
        await _projectManager.SaveAsync();

        // Add to recent projects
        AddToRecentProjects(project);

        // Connect Generate ROM button
        SceneEditorView.OnGenerateRomRequested -= OnGenerateRomRequested;
        SceneEditorView.OnGenerateRomRequested += OnGenerateRomRequested;
    }

    private void AddToRecentProjects(RetruxelProject project)
    {
        var settings = SettingsService.Load();
        var projectPath = Path.GetFullPath(Path.Combine(project.ProjectPath, project.Name + ".rtrxproject"));

        // Remove duplicatas case-insensitive
        settings.General.RecentProjects = settings.General.RecentProjects
            .Where(p => !p.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        settings.General.RecentProjects.Insert(0, projectPath);

        if (settings.General.RecentProjects.Count > 5)
            settings.General.RecentProjects = settings.General.RecentProjects.Take(5).ToList();

        SettingsService.Save(settings);
        WelcomeView.RefreshRecentProjects();
    }

    private async void OnGenerateRomRequested(RetruxelProject project)
    {
        await SaveProjectAsync();
        ShowOverlay("BUILD CONSOLE", _buildConsoleView);
        await _buildConsoleView.BuildAsync(project);
    }

    private async void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        // Check for unsaved changes
        if (_projectManager.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before returning to the Welcome screen?",
                "Retruxel",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
                await SaveProjectAsync();
        }

        // Return to Welcome view
        SceneEditorView.Visibility = Visibility.Collapsed;
        WelcomeView.Visibility = Visibility.Visible;
        BtnHome.Visibility = Visibility.Collapsed;

        // Clear current project
        _projectManager.CurrentProject = null;

        // Refresh recent projects list
        WelcomeView.RefreshRecentProjects();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
    }

    private void TestWizard_Click(object sender, RoutedEventArgs e)
    {
        var wizardWindow = new Views.Wizard.WizardMainWindow { Owner = this };
        wizardWindow.ShowDialog();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => ToggleMaximize();

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_projectManager.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Retruxel",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
                await SaveProjectAsync();
        }

        Close();
    }

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OverlayTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
        {
            _isDraggingOverlay = true;
            _overlayDragStart = e.GetPosition(OverlayLayer);
            OverlayTitleBar.CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_isDraggingOverlay && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPos = e.GetPosition(OverlayLayer);
            var offset = currentPos - _overlayDragStart;

            var currentMargin = OverlayContent.Margin;
            OverlayContent.Margin = new Thickness(
                currentMargin.Left + offset.X,
                currentMargin.Top + offset.Y,
                currentMargin.Right - offset.X,
                currentMargin.Bottom - offset.Y);

            _overlayDragStart = currentPos;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (_isDraggingOverlay)
        {
            _isDraggingOverlay = false;
            OverlayTitleBar.ReleaseMouseCapture();
        }
    }

}