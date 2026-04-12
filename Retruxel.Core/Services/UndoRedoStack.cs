using Retruxel.Core.Interfaces;

namespace Retruxel.Core.Services;

/// <summary>
/// Manages the undo/redo history for editor actions.
///
/// - Push() executes a command and adds it to the undo stack.
/// - Undo() reverses the last command and moves it to the redo stack.
/// - Redo() re-applies the last undone command.
/// - Any new Push() after an Undo() clears the redo stack (standard behavior).
/// - The undo stack is capped at MaxHistorySize — oldest entries are discarded.
/// </summary>
public class UndoRedoStack
{
    private readonly LinkedList<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand>      _redoStack = new();

    /// <summary>
    /// Maximum number of undoable actions retained in history.
    /// Configurable between 20 and 100. Default: 32.
    /// </summary>
    public int MaxHistorySize { get; set; } = 32;

    /// <summary>Whether there is at least one action to undo.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Whether there is at least one action to redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Description of the next action to undo, or null.</summary>
    public string? NextUndoDescription => _undoStack.Last?.Value.Description;

    /// <summary>Description of the next action to redo, or null.</summary>
    public string? NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Raised whenever the stack state changes.
    /// The SceneEditor subscribes to update button enabled states.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Pushes a command onto the undo stack WITHOUT calling Execute().
    /// Used when the action was already performed (e.g. drag-move)
    /// and only the undo record needs to be stored.
    /// </summary>
    public void PushWithoutExecute(IUndoableCommand command)
    {
        _undoStack.AddLast(command);
        _redoStack.Clear();

        if (_undoStack.Count > MaxHistorySize)
            _undoStack.RemoveFirst();

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Executes a command and pushes it onto the undo stack.
    /// Clears the redo stack — a new action invalidates undone history.
    /// </summary>
    public void Push(IUndoableCommand command)
    {
        command.Execute();

        _undoStack.AddLast(command);
        _redoStack.Clear();

        // Enforce history limit — discard oldest entry
        if (_undoStack.Count > MaxHistorySize)
            _undoStack.RemoveFirst();

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Reverses the last executed command.
    /// No-op if the undo stack is empty.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Last!.Value;
        _undoStack.RemoveLast();

        command.Undo();
        _redoStack.Push(command);

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Re-applies the last undone command.
    /// No-op if the redo stack is empty.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();

        _undoStack.AddLast(command);

        StateChanged?.Invoke();
    }

    /// <summary>
    /// Clears both stacks. Called when a new project is opened.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }
}
