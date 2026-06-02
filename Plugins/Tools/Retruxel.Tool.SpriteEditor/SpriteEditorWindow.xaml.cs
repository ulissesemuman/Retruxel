using System.Windows;

namespace Retruxel.Tool.SpriteEditor;

/// <summary>
/// SpriteEditorWindow — partial class root.
/// Implementation split across:
///   _Main.cs          — fields, constructor
///   _Asset.cs         — asset selector
///   _Tileset.cs       — tileset canvas (TilesetRenderer pipeline, same as TilemapEditor)
///   _Canvas.cs        — composition canvas
///   _Animation.cs     — animation preview
///   _Frames.cs        — frame list
///   _Hitboxes.cs      — hitbox editor
///   _PaletteSlot.cs   — palette slot selector
///   _Zoom.cs          — zoom controls
///   _Initialization.cs — save/load/serialization
///   _LiveLink.cs      — LiveLink integration
/// </summary>
public partial class SpriteEditorWindow : Window
{
    private double _canvasZoom  = 1.0;
    private double _tilesetZoom = 1.0;
}
