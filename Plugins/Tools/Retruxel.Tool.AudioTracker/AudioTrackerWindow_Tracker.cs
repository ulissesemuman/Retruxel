using Retruxel.Tool.AudioTracker.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.AudioTracker;

public partial class AudioTrackerWindow
{
    private int _selectedRow = 0;

    private void InitializeTrackerHeaders()
    {
        TrackerHeaderGrid.ColumnDefinitions.Clear();
        TrackerHeaderGrid.Children.Clear();

        TrackerHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

        var rowHeader = new TextBlock
        {
            Text = "ROW", FontSize = 9, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Space Grotesk"),
            Foreground = (Brush)FindResource("BrushOnSurfaceVariant"),
            TextAlignment = TextAlignment.Center, Padding = new Thickness(4)
        };
        Grid.SetColumn(rowHeader, 0);
        TrackerHeaderGrid.Children.Add(rowHeader);

        for (int c = 0; c < _constraints.ChannelCount; c++)
        {
            TrackerHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var brush = c == 0 ? (Brush)FindResource("BrushPrimary")
                      : c == 1 ? (Brush)FindResource("BrushTertiary")
                      : (Brush)FindResource("BrushSecondary");
            var header = new TextBlock
            {
                Text = _constraints.Channels[c].Name.ToUpperInvariant(),
                FontSize = 9, FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Space Grotesk"),
                Foreground = brush, TextAlignment = TextAlignment.Center, Padding = new Thickness(4)
            };
            Grid.SetColumn(header, c + 1);
            TrackerHeaderGrid.Children.Add(header);
        }
    }

    internal void RebuildTrackerRows()
    {
        TrackerRowsPanel.Children.Clear();
        if (State.Patterns.Count == 0) return;

        var pattern = State.Patterns[State.CurrentPatternIndex];
        TxtPatternMatrix.Text = $"PATTERN_MATRIX [PAT.{State.CurrentPatternIndex:D2}]";
        TxtPatternPos.Text    = $"POS: {State.CurrentPatternIndex:D2}/{State.Patterns.Count - 1:D2}";

        for (int row = 0; row < TrackPatternChannel.RowCount; row++)
            TrackerRowsPanel.Children.Add(BuildTrackerRow(pattern, row));

        UpdatePatternMatrix();
        UpdateMemoryUsage();
    }

    private Grid BuildTrackerRow(AudioPattern pattern, int row)
    {
        bool isActive  = row == State.PlayheadRow;
        bool isMeasure = row % 4 == 0;

        var grid = new Grid { Tag = row, Height = 28, Background = Brushes.Transparent };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        for (int c = 0; c < _constraints.ChannelCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Row number
        var rowNum = new TextBlock
        {
            Text = row.ToString("X2"), FontFamily = new FontFamily("JetBrains Mono"),
            FontSize = 11, TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = isActive ? (Brush)FindResource("BrushOnPrimary") : (Brush)FindResource("BrushTertiary"),
            Background = isActive ? (Brush)FindResource("BrushPrimary")   : (Brush)FindResource("BrushSurfaceContainerLow"),
            Padding = new Thickness(4, 0, 4, 0)
        };
        Grid.SetColumn(rowNum, 0);
        grid.Children.Add(rowNum);

        for (int c = 0; c < _constraints.ChannelCount; c++)
        {
            NoteEvent evt = (c < pattern.Channels.Count && row < pattern.Channels[c].Rows.Length)
                ? pattern.Channels[c].Rows[row] : new NoteEvent();

            var cell = BuildCell(evt, row, c, isActive);
            Grid.SetColumn(cell, c + 1);
            grid.Children.Add(cell);
        }

        if (isMeasure)
            grid.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromArgb(77, 0x8E, 0xFF, 0x71)),
                BorderThickness = new Thickness(0, 0, 0, 1), IsHitTestVisible = false
            });

        grid.MouseLeftButtonDown += (_, _) => { _selectedRow = row; RebuildTrackerRows(); };
        return grid;
    }

    private Border BuildCell(NoteEvent evt, int row, int channel, bool isActive)
    {
        string note = evt.Note;
        string inst = evt.InstrumentIndex == 0xFF ? "--" : evt.InstrumentIndex.ToString("X2");
        string vol  = evt.Volume == 0xFF ? "." : evt.Volume.ToString("X1");
        string fx   = evt.EffectCommand == 0 ? "..." : evt.EffectCommand.ToString("X3");

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) };

        void Seg(string text, Brush color) => panel.Children.Add(new TextBlock
        {
            Text = text, FontFamily = new FontFamily("JetBrains Mono"), FontSize = 11,
            Foreground = isActive ? (Brush)FindResource("BrushOnPrimary") : color,
            Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center
        });

        Seg(note, note == "---"
            ? new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            : (Brush)FindResource("BrushPrimary"));
        Seg(inst, (Brush)FindResource("BrushOnSurfaceVariant"));
        Seg(vol,  (Brush)FindResource("BrushTertiary"));
        Seg(fx,   fx == "..." ? new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)) : (Brush)FindResource("BrushError"));

        return new Border
        {
            Child = panel,
            BorderBrush = new SolidColorBrush(Color.FromArgb(26, 255, 255, 255)),
            BorderThickness = new Thickness(0, 0, 1, 0), VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private void UpdatePatternMatrix()
    {
        PatternMatrixGrid.Columns.Clear();
        PatternMatrixGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "ORD", Binding = new System.Windows.Data.Binding("Order"), Width = 48
        });

        for (int c = 0; c < _constraints.ChannelCount; c++)
            PatternMatrixGrid.Columns.Add(new DataGridTextColumn
            {
                Header  = _constraints.Channels[c].Name,
                Binding = new System.Windows.Data.Binding($"[Ch{c}]"),
                Width   = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

        var rows = new List<Dictionary<string, string>>();
        for (int i = 0; i < State.Arrangement.Count; i++)
        {
            var row = new Dictionary<string, string> { ["Order"] = i.ToString("X2") };
            for (int c = 0; c < _constraints.ChannelCount; c++)
                row[$"Ch{c}"] = State.Arrangement[i].ChannelPatterns.TryGetValue(c, out var pid)
                    ? pid.ToString("X2") : "--";
            rows.Add(row);
        }

        PatternMatrixGrid.ItemsSource = rows;
    }

    private void PatternMatrix_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
}
