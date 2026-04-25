using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Windows.Threading;

namespace Retruxel.Services;

/// <summary>
/// Centralized state manager — single source of truth for all project changes.
/// Handles memory persistence, auto-save, undo/redo registration, and dirty tracking.
/// </summary>
public class StateManager
{
    private readonly ProjectManager _projectManager;
    private readonly UndoRedoStack _undoRedo;
    private readonly DispatcherTimer _autoSaveTimer;
    private readonly TimeSpan _autoSaveInterval = TimeSpan.FromSeconds(30);
    private bool _autoSaveEnabled = true;

    public event Action? StateChanged;
    public event Action<bool>? SavingStateChanged; // true = saving, false = saved

    public StateManager(ProjectManager projectManager, UndoRedoStack undoRedo)
    {
        _projectManager = projectManager;
        _undoRedo = undoRedo;

        // Load auto-save setting
        var settings = SettingsService.Load();
        _autoSaveEnabled = settings.General.AutoSaveEnabled;

        // Setup auto-save timer
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = _autoSaveInterval
        };
        _autoSaveTimer.Tick += AutoSaveTimer_Tick;
        
        if (_autoSaveEnabled)
            _autoSaveTimer.Start();
    }

    /// <summary>
    /// Enables or disables auto-save.
    /// </summary>
    public void SetAutoSaveEnabled(bool enabled)
    {
        _autoSaveEnabled = enabled;
        
        if (enabled)
            _autoSaveTimer.Start();
        else
            _autoSaveTimer.Stop();
    }

    /// <summary>
    /// Applies a state change to memory.
    /// Automatically handles: dirty flag, undo/redo registration, and auto-save for large changes.
    /// </summary>
    public async void ApplyChange(StateChange change)
    {
        System.Diagnostics.Debug.WriteLine($"[StateManager] Applying change: {change.Description} (Type: {change.Type})");

        // Execute the change in memory
        change.Execute();

        // Register in undo/redo stack if undoable
        if (change.IsUndoable && change.UndoCommand != null)
        {
            _undoRedo.Push(change.UndoCommand);
        }

        // Mark as dirty
        _projectManager.MarkDirty();

        // Notify listeners
        StateChanged?.Invoke();

        // Auto-save for large changes
        if (change.Type == ChangeType.Large)
        {
            System.Diagnostics.Debug.WriteLine($"[StateManager] Large change detected, saving immediately");
            SavingStateChanged?.Invoke(true);
            await _projectManager.SaveAsync();
            SavingStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Applies a change without executing it (used when change already happened, e.g., drag release).
    /// </summary>
    public async void RegisterChange(StateChange change)
    {
        System.Diagnostics.Debug.WriteLine($"[StateManager] Registering change: {change.Description} (Type: {change.Type})");

        // Register in undo/redo stack if undoable
        if (change.IsUndoable && change.UndoCommand != null)
        {
            _undoRedo.PushWithoutExecute(change.UndoCommand);
        }

        // Mark as dirty
        _projectManager.MarkDirty();

        // Notify listeners
        StateChanged?.Invoke();

        // Auto-save for large changes
        if (change.Type == ChangeType.Large)
        {
            System.Diagnostics.Debug.WriteLine($"[StateManager] Large change detected, saving immediately");
            SavingStateChanged?.Invoke(true);
            await _projectManager.SaveAsync();
            SavingStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Forces an immediate save to disk.
    /// </summary>
    public async Task SaveNowAsync()
    {
        if (_projectManager.HasUnsavedChanges)
        {
            System.Diagnostics.Debug.WriteLine($"[StateManager] Saving project (manual trigger)");
            SavingStateChanged?.Invoke(true);
            await _projectManager.SaveAsync();
            SavingStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Auto-save timer tick — saves if there are unsaved changes and auto-save is enabled.
    /// </summary>
    private async void AutoSaveTimer_Tick(object? sender, EventArgs e)
    {
        if (_autoSaveEnabled && _projectManager.HasUnsavedChanges)
        {
            System.Diagnostics.Debug.WriteLine($"[StateManager] Auto-save triggered (30s timer)");
            SavingStateChanged?.Invoke(true);
            await _projectManager.SaveAsync();
            SavingStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Stops the auto-save timer (call when closing project).
    /// </summary>
    public void Dispose()
    {
        _autoSaveTimer.Stop();
    }
}

/// <summary>
/// Represents a state change in the project.
/// </summary>
public class StateChange
{
    public required string Description { get; init; }
    public required ChangeType Type { get; init; }
    public required Action Execute { get; init; }
    public bool IsUndoable { get; init; } = true;
    public IUndoableCommand? UndoCommand { get; init; }
}

/// <summary>
/// Classification of state changes for auto-save behavior.
/// </summary>
public enum ChangeType
{
    /// <summary>Small change — mark dirty, don't auto-save (e.g., move element, change property)</summary>
    Small,

    /// <summary>Large change — mark dirty AND auto-save immediately (e.g., create module, import asset)</summary>
    Large
}
