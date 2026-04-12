namespace Retruxel.Core.Interfaces;

/// <summary>
/// Contract for a reversible editor action.
/// All user actions that modify project state implement this interface
/// and are managed by the UndoRedoStack.
/// </summary>
public interface IUndoableCommand
{
    /// <summary>
    /// Human-readable description shown in the undo/redo history.
    /// Ex: "Move Player Entity", "Change speed to 4", "Add Tilemap"
    /// </summary>
    string Description { get; }

    /// <summary>Applies the action.</summary>
    void Execute();

    /// <summary>Reverses the action, restoring previous state.</summary>
    void Undo();
}
