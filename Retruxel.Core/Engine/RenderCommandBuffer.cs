namespace Retruxel.Core.Engine;

/// <summary>
/// Platform-agnostic render command types.
/// Commands are generated during game logic and executed by the RenderBackend during VBlank.
/// </summary>
public enum RenderCommandType
{
    DrawTilemap,
    DrawText,
    DrawSprite,
    SetScroll,
    LoadPalette,
    ClearScreen
}

/// <summary>
/// Single render command with associated data.
/// </summary>
public class RenderCommand
{
    public RenderCommandType Type { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Buffer of render commands to be executed during VBlank.
/// Filled during game logic, consumed by RenderBackend.
/// </summary>
public class RenderCommandBuffer
{
    private readonly List<RenderCommand> _commands = new();
    public const int MaxCommands = 256;

    public int Count => _commands.Count;
    public IReadOnlyList<RenderCommand> Commands => _commands;

    /// <summary>
    /// Adds a render command to the buffer.
    /// </summary>
    public void AddCommand(RenderCommandType type, object? data = null)
    {
        if (_commands.Count >= MaxCommands)
        {
            throw new InvalidOperationException($"Render command buffer overflow (max {MaxCommands})");
        }

        _commands.Add(new RenderCommand
        {
            Type = type,
            Data = data
        });
    }

    /// <summary>
    /// Clears all commands after execution.
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
    }
}

/// <summary>
/// Render command data structures.
/// </summary>
public class DrawTilemapCommand
{
    public TilemapLayerState State { get; set; } = new();
}

public class DrawTextCommand
{
    public string Text { get; set; } = string.Empty;
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte Color { get; set; }
}

public class DrawSpriteCommand
{
    public SpriteState Sprite { get; set; } = new();
}

public class SetScrollCommand
{
    public byte X { get; set; }
    public byte Y { get; set; }
}

public class LoadPaletteCommand
{
    public byte[] Colors { get; set; } = Array.Empty<byte>();
    public byte Index { get; set; }
}
