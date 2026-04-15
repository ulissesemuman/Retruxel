using Retruxel.Core.Interfaces;

namespace Retruxel.Core.Commands;

// ── AddElementCommand ─────────────────────────────────────────────────────────

/// <summary>
/// Records the addition of a module element to the scene canvas.
/// Undo removes it; Redo re-adds it.
/// </summary>
public class AddElementCommand : IUndoableCommand
{
    public string Description { get; }

    private readonly Action _add;
    private readonly Action _remove;

    /// <param name="description">Ex: "Add Player Entity"</param>
    /// <param name="add">Action that adds the element to the canvas and scene data.</param>
    /// <param name="remove">Action that removes it.</param>
    public AddElementCommand(string description, Action add, Action remove)
    {
        Description = description;
        _add = add;
        _remove = remove;
    }

    public void Execute() => _add();
    public void Undo() => _remove();
}

// ── RemoveElementCommand ──────────────────────────────────────────────────────

/// <summary>
/// Records the removal of a module element from the scene canvas.
/// Undo re-adds it; Redo removes it again.
/// </summary>
public class RemoveElementCommand : IUndoableCommand
{
    public string Description { get; }

    private readonly Action _remove;
    private readonly Action _restore;

    /// <param name="description">Ex: "Remove Enemy Entity"</param>
    /// <param name="remove">Action that removes the element.</param>
    /// <param name="restore">Action that restores it.</param>
    public RemoveElementCommand(string description, Action remove, Action restore)
    {
        Description = description;
        _remove = remove;
        _restore = restore;
    }

    public void Execute() => _remove();
    public void Undo() => _restore();
}

// ── MoveElementCommand ────────────────────────────────────────────────────────

/// <summary>
/// Records moving an element on the canvas from one tile position to another.
/// Stores previous and new positions so Undo can revert precisely.
/// </summary>
public class MoveElementCommand : IUndoableCommand
{
    public string Description { get; }

    private readonly Action<int, int> _moveTo;
    private readonly int _prevX;
    private readonly int _prevY;
    private readonly int _newX;
    private readonly int _newY;

    /// <param name="description">Ex: "Move Player Entity"</param>
    /// <param name="moveTo">Action(tileX, tileY) that repositions the element.</param>
    /// <param name="prevX">Tile X before the move.</param>
    /// <param name="prevY">Tile Y before the move.</param>
    /// <param name="newX">Tile X after the move.</param>
    /// <param name="newY">Tile Y after the move.</param>
    public MoveElementCommand(
        string description,
        Action<int, int> moveTo,
        int prevX, int prevY,
        int newX, int newY)
    {
        Description = description;
        _moveTo = moveTo;
        _prevX = prevX;
        _prevY = prevY;
        _newX = newX;
        _newY = newY;
    }

    public void Execute() => _moveTo(_newX, _newY);
    public void Undo() => _moveTo(_prevX, _prevY);
}

// ── ChangePropertyCommand ─────────────────────────────────────────────────────

/// <summary>
/// Records a property value change in the module properties panel.
/// Stores the previous and new serialized values so Undo can restore exactly.
/// </summary>
public class ChangePropertyCommand : IUndoableCommand
{
    public string Description { get; }

    private readonly Action<string> _apply;
    private readonly string _previousValue;
    private readonly string _newValue;

    /// <param name="description">Ex: "Change speed to 4"</param>
    /// <param name="apply">Action(value) that applies the value to the module.</param>
    /// <param name="previousValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    public ChangePropertyCommand(
        string description,
        Action<string> apply,
        string previousValue,
        string newValue)
    {
        Description = description;
        _apply = apply;
        _previousValue = previousValue;
        _newValue = newValue;
    }

    public void Execute() => _apply(_newValue);
    public void Undo() => _apply(_previousValue);
}
