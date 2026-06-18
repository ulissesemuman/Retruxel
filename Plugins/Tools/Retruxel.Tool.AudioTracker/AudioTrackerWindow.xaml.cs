using Retruxel.Tool.AudioTracker.Domain;
using Retruxel.Tool.AudioTracker.Models;
using System.Windows;
using System.Windows.Input;

namespace Retruxel.Tool.AudioTracker;

public partial class AudioTrackerWindow : Window
{
    private readonly AudioTargetConstraints _constraints;
    private readonly string _targetId;
    private bool _isExpertMode = false;
    private bool _isPlaying = false;
    private bool _isInitializing = true;

    public AudioProjectState State { get; private set; }

    public AudioTrackerWindow(
        AudioProjectState state,
        AudioTargetConstraints constraints,
        string targetId)
    {
        State        = state;
        _constraints = constraints;
        _targetId    = targetId;

        InitializeComponent();

        TxtChipStatus.Text = $"[Status: SYSTEM_READY_IDLE] | [Chip: {constraints.ChipName}]";
        TxtBpm.Text = state.Bpm.ToString();
        TxtSpd.Text = state.Speed.ToString("D2");

        _isInitializing = false;

        InitializeInstruments();
        InitializeChordMatrix();
        InitializeArranger();
        InitializeTrackerHeaders();
        RebuildTrackerRows();
        UpdateMemoryUsage();
    }

    // ── Window chrome ─────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); } catch { }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        FlushCurrentInstrument();
        DialogResult = true;
        Close();
    }

    // ── Transport ─────────────────────────────────────────────────────────────

    private void BtnPlay_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = true;
        TxtFooterStatus.Text = "STATUS: PLAYING";
    }

    private void BtnStop_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = false;
        State.PlayheadRow = 0;
        TxtFooterStatus.Text = "STATUS: STOPPED";
        RebuildTrackerRows();
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        _isPlaying = false;
        TxtFooterStatus.Text = "STATUS: PAUSED";
    }

    private void BtnLoop_Click(object sender, RoutedEventArgs e) { }

    // ── Mode toggle ───────────────────────────────────────────────────────────

    private void BtnModeBasic_Click(object sender, RoutedEventArgs e)  => SetMode(false);
    private void BtnModeExpert_Click(object sender, RoutedEventArgs e) => SetMode(true);

    private void SetMode(bool expert)
    {
        _isExpertMode       = expert;
        ViewBasic.Visibility  = expert ? Visibility.Collapsed : Visibility.Visible;
        ViewExpert.Visibility = expert ? Visibility.Visible   : Visibility.Collapsed;
        BtnModeBasic.Style  = (System.Windows.Style)FindResource(expert ? "ButtonGhost"   : "ButtonPrimary");
        BtnModeExpert.Style = (System.Windows.Style)FindResource(expert ? "ButtonPrimary" : "ButtonGhost");
    }

    // ── BPM / SPD ─────────────────────────────────────────────────────────────

    private void TxtBpm_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (byte.TryParse(TxtBpm.Text, out byte bpm)) State.Bpm = bpm;
    }

    private void TxtSpd_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (byte.TryParse(TxtSpd.Text, out byte spd)) State.Speed = spd;
    }

    // ── Memory ────────────────────────────────────────────────────────────────

    internal void UpdateMemoryUsage()
    {
        // Rough estimate: patterns * channels * 64 rows * 4 bytes
        int bytes = State.Patterns.Count * _constraints.ChannelCount * TrackPatternChannel.RowCount * 4;
        double kb  = bytes / 1024.0;
        TxtMemUsage.Text  = $"{kb:F1}KB / 64KB";
        TxtFooterMem.Text = $"MEM: {kb:F1}KB / 64KB  PAT: {State.Patterns.Count:D2}  INS: {State.Instruments.Count:D2}";

        double ratio = System.Math.Min(1.0, kb / 64.0);
        MemBar.Width = ratio * 218; // approximate sidebar inner width
    }
}
