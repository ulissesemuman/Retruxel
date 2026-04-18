using System.Windows.Media.Imaging;

namespace Retruxel.Tool.FontImporter.Services;

/// <summary>
/// The result of a successful font import.
/// Handed back to the caller (Asset Manager, future font module) after
/// the user clicks IMPORT FONT.
///
/// The caller is responsible for:
///   - Saving SourceTtfPath to the project's assets folder
///   - Encoding Spritesheet to PNG and saving it alongside the .ttf
///   - Recording this font in the project's asset registry
/// </summary>
public class FontImportResult
{
    /// <summary>
    /// Absolute path to the source .ttf/.otf file chosen by the user.
    /// This file should be copied into the project's assets folder.
    /// </summary>
    public string SourceTtfPath { get; init; } = string.Empty;

    /// <summary>Tile width in pixels used during rasterization.</summary>
    public int TileWidth { get; init; }

    /// <summary>Tile height in pixels used during rasterization.</summary>
    public int TileHeight { get; init; }

    /// <summary>
    /// Ordered list of Unicode codepoints included in the spritesheet.
    /// Position in this list maps to tile index in the spritesheet
    /// (left-to-right, top-to-bottom, 16 columns per row).
    /// </summary>
    public List<int> Codepoints { get; init; } = [];

    /// <summary>
    /// The generated spritesheet as a WPF BitmapSource.
    /// Call FontRasterizer.EncodeToPng(Spritesheet) to get raw bytes for saving.
    /// </summary>
    public BitmapSource Spritesheet { get; init; } = null!;

    /// <summary>Number of columns per row in the spritesheet. Always 16.</summary>
    public int ColumnsPerRow { get; init; } = 16;

    /// <summary>Total number of rows in the spritesheet.</summary>
    public int Rows => (int)Math.Ceiling(Codepoints.Count / (double)ColumnsPerRow);

    /// <summary>Spritesheet total width in pixels.</summary>
    public int SheetWidthPx => TileWidth * ColumnsPerRow;

    /// <summary>Spritesheet total height in pixels.</summary>
    public int SheetHeightPx => TileHeight * Rows;
}
