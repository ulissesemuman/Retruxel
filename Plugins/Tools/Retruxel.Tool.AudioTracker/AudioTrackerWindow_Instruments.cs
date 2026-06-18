using Retruxel.Tool.AudioTracker.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Retruxel.Tool.AudioTracker;

public partial class AudioTrackerWindow
{
    private int _selectedInstrumentIndex = 0;

    private void InitializeInstruments()
    {
        InstrumentListBox.Items.Clear();

        for (int i = 0; i < State.Instruments.Count; i++)
        {
            var inst = State.Instruments[i];
            var item = new StackPanel { Orientation = Orientation.Horizontal, Tag = i };

            item.Children.Add(new TextBlock
            {
                Text       = inst.Index.ToString("X2"),
                FontFamily = new FontFamily("JetBrains Mono"),
                FontSize   = 9,
                Opacity    = 0.6,
                Margin     = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            item.Children.Add(new TextBlock
            {
                Text     = inst.Name.ToUpperInvariant(),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });

            InstrumentListBox.Items.Add(item);
        }

        if (InstrumentListBox.Items.Count > 0)
            InstrumentListBox.SelectedIndex = 0;
    }

    private void InstrumentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InstrumentListBox.SelectedIndex < 0) return;
        FlushCurrentInstrument();
        _selectedInstrumentIndex = InstrumentListBox.SelectedIndex;
        LoadInstrumentToAdsr(_selectedInstrumentIndex);
    }

    private void LoadInstrumentToAdsr(int index)
    {
        if (index < 0 || index >= State.Instruments.Count) return;
        var inst = State.Instruments[index];
        TxtA.Text = inst.Attack.ToString();
        TxtD.Text = inst.Decay.ToString();
        TxtS.Text = inst.Sustain.ToString();
        TxtR.Text = inst.Release.ToString();
        DrawAdsrCurve(inst);
    }

    private void FlushCurrentInstrument()
    {
        if (_selectedInstrumentIndex < 0 || _selectedInstrumentIndex >= State.Instruments.Count) return;
        var inst = State.Instruments[_selectedInstrumentIndex];
        if (byte.TryParse(TxtA.Text, out byte a)) inst.Attack  = a;
        if (byte.TryParse(TxtD.Text, out byte d)) inst.Decay   = d;
        if (byte.TryParse(TxtS.Text, out byte s)) inst.Sustain = s;
        if (byte.TryParse(TxtR.Text, out byte r)) inst.Release = r;
    }

    private void Adsr_Changed(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing || _selectedInstrumentIndex >= State.Instruments.Count) return;
        FlushCurrentInstrument();
        DrawAdsrCurve(State.Instruments[_selectedInstrumentIndex]);
    }

    private void DrawAdsrCurve(InstrumentState inst)
    {
        AdsrCanvas.Children.Clear();
        double w = AdsrCanvas.ActualWidth > 0 ? AdsrCanvas.ActualWidth : 218;
        double h = AdsrCanvas.ActualHeight > 0 ? AdsrCanvas.ActualHeight : 120;

        // Normalize ADSR (0-15) to pixel positions
        double aX = (inst.Attack  / 15.0) * w * 0.25;
        double dX = aX + (inst.Decay   / 15.0) * w * 0.25;
        double sY = h - (inst.Sustain  / 15.0) * h;
        double rX = dX + (inst.Release / 15.0) * w * 0.50;

        var points = new PointCollection
        {
            new Point(0, h),
            new Point(aX, 4),
            new Point(dX, sY),
            new Point(Math.Min(rX, w - 4), sY),
            new Point(w, h)
        };

        var poly = new Polyline
        {
            Points          = points,
            Stroke          = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            StrokeThickness = 1.5,
            IsHitTestVisible = false
        };
        AdsrCanvas.Children.Add(poly);

        // Draggable nodes
        foreach (var pt in new[] { new Point(aX, 4), new Point(dX, sY), new Point(Math.Min(rX, w - 4), sY) })
        {
            var node = new Ellipse
            {
                Width = 6, Height = 6,
                Fill  = Brushes.White,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(node, pt.X - 3);
            Canvas.SetTop(node,  pt.Y - 3);
            AdsrCanvas.Children.Add(node);
        }
    }
}
