namespace Retruxel.Core.Engine;

/// <summary>
/// Platform-specific render backend interface.
/// Each target (SMS, NES, SNES) implements this interface to translate
/// platform-agnostic render commands into hardware-specific VRAM operations.
/// 
/// CRITICAL: All VRAM writes must happen exclusively through this interface
/// during VBlank. No direct VRAM access is allowed in game logic or modules.
/// </summary>
public interface IRenderBackend
{
    /// <summary>
    /// Target platform identifier (e.g., "sms", "nes", "snes").
    /// </summary>
    string TargetId { get; }

    /// <summary>
    /// Initializes the render backend and hardware.
    /// Called once at boot before any rendering.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Draws a tilemap layer to VRAM.
    /// Platform-specific: SMS merges layers, SNES uses separate BG layers.
    /// </summary>
    void DrawTilemap(TilemapLayerState state);

    /// <summary>
    /// Draws text to the screen.
    /// Platform-specific: SMS uses nametable, NES uses sprites or nametable.
    /// </summary>
    void DrawText(string text, byte x, byte y, byte color);

    /// <summary>
    /// Draws a single sprite.
    /// Platform-specific: SMS uses SAT, NES uses OAM, SNES uses OAM with priority.
    /// </summary>
    void DrawSprite(SpriteState sprite);

    /// <summary>
    /// Sets hardware scroll registers.
    /// Platform-specific: SMS has X-only scroll, NES has X/Y per nametable, SNES has per-BG scroll.
    /// </summary>
    void SetScroll(byte x, byte y);

    /// <summary>
    /// Loads palette colors into hardware.
    /// Platform-specific: SMS uses CRAM, NES uses palette RAM, SNES uses CGRAM.
    /// </summary>
    void LoadPalette(byte[] colors, byte index);

    /// <summary>
    /// Clears the screen (VRAM).
    /// Platform-specific: SMS clears nametable, NES clears nametables, SNES clears tilemaps.
    /// </summary>
    void ClearScreen();

    /// <summary>
    /// Executes all commands in the render buffer during VBlank.
    /// This is the main entry point called by the game loop.
    /// </summary>
    void ExecuteBuffer(RenderCommandBuffer buffer);

    /// <summary>
    /// Returns platform-specific constraints (max sprites, VRAM size, etc.).
    /// </summary>
    RenderBackendConstraints GetConstraints();
}

/// <summary>
/// Platform-specific rendering constraints.
/// </summary>
public class RenderBackendConstraints
{
    public int MaxSprites { get; set; } = 64;
    public int MaxTiles { get; set; } = 448;
    public int ScreenWidth { get; set; } = 256;
    public int ScreenHeight { get; set; } = 192;
    public int TileWidth { get; set; } = 8;
    public int TileHeight { get; set; } = 8;
    public int MaxPaletteColors { get; set; } = 16;
    public bool SupportsHardwareScroll { get; set; } = true;
    public bool SupportsMultipleLayers { get; set; } = false;
}
