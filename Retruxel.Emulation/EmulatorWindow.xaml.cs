using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Retruxel.Emulation.LibRetro;

namespace Retruxel.Emulation;

public partial class EmulatorWindow : Window
{
    private LibRetroCore? _core;
    private WriteableBitmap? _screenBitmap;
    private bool _isRunning;
    private CancellationTokenSource? _runCts;
    private byte[]? _lastFrameBuffer;
    private int _lastFrameWidth;
    private int _lastFrameHeight;

    public EmulatorWindow()
    {
        InitializeComponent();
        
        BtnLoadCore.Click += BtnLoadCore_Click;
        BtnLoadRom.Click += BtnLoadRom_Click;
        BtnRun.Click += BtnRun_Click;
        BtnPause.Click += BtnPause_Click;
        BtnReset.Click += BtnReset_Click;
        BtnDumpVram.Click += BtnDumpVram_Click;
    }

    private void BtnLoadCore_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Libretro Core",
            Filter = "Libretro Core (*.dll)|*.dll|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _core?.Dispose();
            _core = new LibRetroCore();

            if (!_core.LoadCore(dialog.FileName))
            {
                TxtStatus.Text = "Failed to load core";
                MessageBox.Show("Failed to load libretro core.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _core.OnVideoRefresh += OnVideoRefresh;

            string coreName = Marshal.PtrToStringAnsi(_core.SystemInfo.library_name) ?? "Unknown";
            string coreVersion = Marshal.PtrToStringAnsi(_core.SystemInfo.library_version) ?? "Unknown";
            
            TxtStatus.Text = $"Loaded: {coreName} v{coreVersion}";
            BtnLoadRom.IsEnabled = true;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading core: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnLoadRom_Click(object sender, RoutedEventArgs e)
    {
        if (_core == null)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Select ROM",
            Filter = "ROM Files (*.sms;*.gg;*.sg;*.nes;*.sfc;*.smc;*.gb;*.gbc)|*.sms;*.gg;*.sg;*.nes;*.sfc;*.smc;*.gb;*.gbc|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            if (!_core.LoadGame(dialog.FileName))
            {
                TxtStatus.Text = "Failed to load ROM";
                MessageBox.Show("Failed to load ROM.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            uint width = _core.AvInfo.geometry.base_width;
            uint height = _core.AvInfo.geometry.base_height;
            double fps = _core.AvInfo.timing.fps;

            _screenBitmap = new WriteableBitmap(
                (int)width, (int)height, 96, 96,
                PixelFormats.Bgra32, null);
            
            ImgScreen.Source = _screenBitmap;

            TxtStatus.Text = $"ROM loaded: {Path.GetFileName(dialog.FileName)} | {width}x{height} @ {fps:F2} FPS";
            BtnRun.IsEnabled = true;
            BtnReset.IsEnabled = true;
            BtnDumpVram.IsEnabled = true;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading ROM: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        if (_core == null || _isRunning)
            return;

        _isRunning = true;
        BtnRun.IsEnabled = false;
        BtnPause.IsEnabled = true;
        
        _runCts = new CancellationTokenSource();
        Task.Run(() => EmulationLoop(_runCts.Token));
    }

    private void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        _isRunning = false;
        _runCts?.Cancel();
        BtnRun.IsEnabled = true;
        BtnPause.IsEnabled = false;
        TxtStatus.Text = "Paused";
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _core?.Reset();
        TxtStatus.Text = "Reset";
    }

    private void BtnDumpVram_Click(object sender, RoutedEventArgs e)
    {
        if (_core == null)
            return;

        try
        {
            string report = "Memory Report:\n\n";

            // Check libretro memory
            var memoryTypes = new[]
            {
                (RetroMemory.RETRO_MEMORY_VIDEO_RAM, "VRAM (libretro)"),
                (RetroMemory.RETRO_MEMORY_SYSTEM_RAM, "System RAM"),
                (RetroMemory.RETRO_MEMORY_SAVE_RAM, "Save RAM")
            };

            foreach (var (type, name) in memoryTypes)
            {
                byte[]? mem = _core.GetMemory(type);
                if (mem != null && mem.Length > 0)
                    report += $"✓ {name}: {mem.Length} bytes\n";
                else
                    report += $"✗ {name}: Not available\n";
            }

            // Check captured frame buffer
            if (_lastFrameBuffer != null)
            {
                report += $"✓ Frame Buffer: {_lastFrameBuffer.Length} bytes ({_lastFrameWidth}×{_lastFrameHeight})\n";
                report += $"\nFrame buffer contains rendered pixels (XRGB8888).\n";
                report += $"This is the final output, not raw VRAM tiles.\n\n";
                report += $"To extract tiles, we need to:\n";
                report += $"1. Decode pixels back to tiles (8×8)\n";
                report += $"2. Detect unique tiles\n";
                report += $"3. Extract palette from colors\n\n";
                report += $"First 64 pixels (BGRA format):\n";
                report += string.Join(" ", _lastFrameBuffer.Take(256).Select(b => b.ToString("X2")));
            }
            else
            {
                report += $"✗ Frame Buffer: Not captured yet (click RUN first)\n";
            }

            MessageBox.Show(report, "Memory Dump", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void EmulationLoop(CancellationToken ct)
    {
        double targetFrameTime = 1000.0 / _core!.AvInfo.timing.fps;
        
        while (_isRunning && !ct.IsCancellationRequested)
        {
            var startTime = DateTime.Now;
            
            _core.Run();
            
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            var sleepTime = (int)(targetFrameTime - elapsed);
            
            if (sleepTime > 0)
                Thread.Sleep(sleepTime);
        }
    }

    private void OnVideoRefresh(IntPtr data, uint width, uint height, UIntPtr pitch)
    {
        if (_screenBitmap == null || data == IntPtr.Zero)
            return;

        // Store frame buffer for analysis
        int frameSize = (int)(height * pitch);
        if (_lastFrameBuffer == null || _lastFrameBuffer.Length != frameSize)
        {
            _lastFrameBuffer = new byte[frameSize];
        }
        Marshal.Copy(data, _lastFrameBuffer, 0, frameSize);
        _lastFrameWidth = (int)width;
        _lastFrameHeight = (int)height;

        Dispatcher.Invoke(() =>
        {
            _screenBitmap.Lock();

            unsafe
            {
                byte* src = (byte*)data;
                byte* dst = (byte*)_screenBitmap.BackBuffer;
                int srcPitch = (int)pitch;
                int dstPitch = _screenBitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    Buffer.MemoryCopy(
                        src + y * srcPitch,
                        dst + y * dstPitch,
                        dstPitch,
                        Math.Min(srcPitch, dstPitch));
                }
            }

            _screenBitmap.AddDirtyRect(new Int32Rect(0, 0, (int)width, (int)height));
            _screenBitmap.Unlock();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _isRunning = false;
        _runCts?.Cancel();
        _core?.Dispose();
        base.OnClosed(e);
    }
}
