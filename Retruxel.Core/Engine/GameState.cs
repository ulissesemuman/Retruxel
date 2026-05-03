namespace Retruxel.Core.Engine;

/// <summary>
/// Central game state structure - single source of truth.
/// No direct VRAM access allowed - all rendering goes through RenderBackend.
/// </summary>
public class GameState
{
    // Layer states
    public TilemapLayerState Background { get; set; } = new();
    public TilemapLayerState? Foreground { get; set; } // Future: SNES
    public TextLayerState UI { get; set; } = new();
    public SpriteLayerState Sprites { get; set; } = new();

    // Dirty flags (optimization)
    public bool BackgroundDirty { get; set; }
    public bool ForegroundDirty { get; set; }
    public bool UIDirty { get; set; }
    public bool SpritesDirty { get; set; }

    // Global state
    public byte ScrollX { get; set; }
    public byte ScrollY { get; set; }
    public bool ScrollDirty { get; set; }

    /// <summary>
    /// Clears all dirty flags after rendering.
    /// </summary>
    public void ClearDirtyFlags()
    {
        BackgroundDirty = false;
        ForegroundDirty = false;
        UIDirty = false;
        SpritesDirty = false;
        ScrollDirty = false;
    }
}

/// <summary>
/// Tilemap layer state (background or foreground).
/// </summary>
public class TilemapLayerState
{
    public byte[]? TileData { get; set; }
    public ushort[]? MapData { get; set; }
    public byte MapWidth { get; set; }
    public byte MapHeight { get; set; }
    public byte StartTile { get; set; }
    public byte PaletteIndex { get; set; }
    public int MapX { get; set; }
    public int MapY { get; set; }
}

/// <summary>
/// Text/UI layer state.
/// </summary>
public class TextLayerState
{
    public string Text { get; set; } = string.Empty;
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte Color { get; set; }
}

/// <summary>
/// Single sprite state.
/// </summary>
public class SpriteState
{
    public byte X { get; set; }
    public byte Y { get; set; }
    public byte TileIndex { get; set; }
    public bool Visible { get; set; } = true;
    public bool FlipX { get; set; }
    public bool FlipY { get; set; }
    public byte Priority { get; set; }
}

/// <summary>
/// Sprite layer state (collection of sprites).
/// </summary>
public class SpriteLayerState
{
    public List<SpriteState> Sprites { get; set; } = new();
    public int MaxSprites { get; set; } = 64; // SMS default, varies by platform
}

/// <summary>
/// Layer types for composition order.
/// </summary>
public enum LayerType
{
    Background,   // Main tilemap
    Foreground,   // Future: SNES BG2
    UI,           // Text, HUD
    Sprites       // Entities
}
