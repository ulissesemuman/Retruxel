using Retruxel.Tool.AudioTracker.Domain;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.AudioTracker;

public partial class AudioTrackerWindow
{
    private static readonly string[] AllNotes =
        ["B", "A#", "A", "G#", "G", "F#", "F", "E", "D#", "D", "C#", "C"];

    private static readonly int[] OctavesVisible = [4, 3];

    private const double PianoKeyH    = 24;
    private const double NoteBlockW   = 64;
    private const double NoteBlockH   = PianoKeyH;

    // ── Arranger ──────────────────────────────────────────────────────────────

    private void InitializeArranger()
    {
        ArrangerPanel.Children.Clear();

        foreach (var row in State.Arrangement)
        {
            var block = new Border
            {
                Padding         = new Thickness(12, 0, 12, 0),
                Height          = 32,
                Background      = (Brush)FindResource("BrushPrimaryContainer"),
                BorderBrush     = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Margin          = new Thickness(2, 0, 0, 0),
                Cursor          = Cursors.Hand,
                Child           = new TextBlock
                {
                    Text       = row.Label.ToUpperInvariant(),
                    FontFamily = new FontFamily("Space Grotesk"),
                    FontSize   = 10, FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("BrushOnPrimaryContainer"),
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            ArrangerPanel.Children.Add(block);
        }

        // Drop zone
        ArrangerPanel.Children.Add(new Border
        {
            MinWidth        = 120, Height = 32,
            BorderBrush     = new SolidColorBrush(Color.FromArgb(50, 0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(1),
            Margin          = new Thickness(4, 0, 0, 0),
            Child           = new TextBlock
            {
                Text          = "DROP ZONE",
                FontFamily    = new FontFamily("Space Grotesk"),
                FontSize      = 10, FontWeight = FontWeights.Bold,
                Foreground    = (Brush)FindResource("BrushOnSurfaceVariant"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment   = VerticalAlignment.Center
            }
        });
    }

    // ── Piano Keys ────────────────────────────────────────────────────────────

    private void BuildPianoKeys()
    {
        PianoKeysPanel.Children.Clear();

        foreach (int oct in OctavesVisible)
        {
            foreach (var note in AllNotes)
            {
                bool isBlack = note.Contains('#');
                PianoKeysPanel.Children.Add(new Border
                {
                    Height          = PianoKeyH,
                    Background      = isBlack
                        ? (Brush)FindResource("BrushSurfaceContainerLowest")
                        : (Brush)FindResource("BrushSurfaceContainerHigh"),
                    BorderBrush     = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Child           = new TextBlock
                    {
                        Text              = $"{note}-{oct}",
                        FontFamily        = new FontFamily("JetBrains Mono"),
                        FontSize          = 9,
                        Foreground        = (Brush)FindResource("BrushOnSurfaceVariant"),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment   = VerticalAlignment.Center,
                        Margin            = new Thickness(0, 0, 4, 0)
                    }
                });
            }
        }
    }

    // ── Piano Roll Grid ───────────────────────────────────────────────────────

    private void BuildPianoRollCanvas()
    {
        PianoRollCanvas.Children.Clear();

        int totalRows = AllNotes.Length * OctavesVisible.Length;
        double totalH = totalRows * PianoKeyH;
        double totalW = 800;

        PianoRollCanvas.Width  = totalW;
        PianoRollCanvas.Height = totalH;

        // Horizontal grid lines per note row
        var gridBrush = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255));
        gridBrush.Freeze();
        for (int r = 0; r <= totalRows; r++)
            PianoRollCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = r * PianoKeyH, X2 = totalW, Y2 = r * PianoKeyH,
                Stroke = gridBrush, StrokeThickness = 1, IsHitTestVisible = false
            });

        // Vertical beat grid (every NoteBlockW pixels)
        var beatBrush = new SolidColorBrush(Color.FromArgb(26, 0x8E, 0xFF, 0x71));
        beatBrush.Freeze();
        for (double x = 0; x <= totalW; x += NoteBlockW)
            PianoRollCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = totalH,
                Stroke = beatBrush, StrokeThickness = 1, IsHitTestVisible = false
            });

        // Playhead
        var playhead = new Line
        {
            X1 = 0, Y1 = 0, X2 = 0, Y2 = totalH,
            Stroke = new SolidColorBrush(Color.FromRgb(0x81, 0xEC, 0xFF)),
            StrokeThickness = 2, IsHitTestVisible = false, Tag = "playhead"
        };
        Canvas.SetLeft(playhead, 256);
        PianoRollCanvas.Children.Add(playhead);
    }

    private void PianoRoll_MouseDown(object sender, MouseButtonEventArgs e) { }
    private void PianoRoll_MouseUp(object sender, MouseButtonEventArgs e) { }
    private void PianoRoll_MouseMove(object sender, MouseEventArgs e) { }

    // ── Chord Matrix ──────────────────────────────────────────────────────────

    private void InitializeChordMatrix()
    {
        ChordMatrixPanel.Children.Clear();
        BuildPianoKeys();
        BuildPianoRollCanvas();

        foreach (var chord in ChordHelper.CommonChords)
        {
            var btn = new Button
            {
                Content     = chord,
                Width       = 52, Height = 36, Margin = new Thickness(2),
                FontFamily  = new FontFamily("Space Grotesk"),
                FontSize    = 13, FontWeight = FontWeights.Bold,
                Style       = (Style)FindResource("ButtonSecondary"),
                Tag         = chord
            };
            btn.Click += ChordButton_Click;
            ChordMatrixPanel.Children.Add(btn);
        }
    }

    private void ChordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string chord) return;

        var notes = ChordHelper.ResolveChord(
            chord, octave: 3, _constraints,
            instrumentIndex: (byte)Math.Max(0, State.Instruments.Count > 0 ? 0 : 0));

        if (State.Patterns.Count == 0) return;
        var pattern = State.Patterns[State.CurrentPatternIndex];

        for (int i = 0; i < notes.Count && i < pattern.Channels.Count; i++)
        {
            var ch = pattern.Channels[i];
            if (_selectedRow < ch.Rows.Length)
                ch.Rows[_selectedRow] = notes[i].Clone();
        }

        RebuildTrackerRows();
    }
}
