using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Tool.LiveLink.Emulators;
using Retruxel.Tool.LiveLink.Pipelines;
using Retruxel.Lib.ImageProcessing;
using Retruxel.Tool.LiveLink.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Retruxel.Tool.LiveLink;

public partial class LiveLinkWindow : Window
{
    private IEmulatorConnection? _connection;
    private CaptureResult? _lastCapture;
    private readonly List<IEmulatorConnection> _availableEmulators = new();
    private readonly Dictionary<string, object>? _input;
    private readonly bool _captureMode;
    private readonly string? _callerId;
    private AppSettings? _settings;
    private static Process? _emulatorProcess;
    private static string? _lastEmulatorId;
    private static string? _lastRomPath;
    private string? _sourceConsole; // Console do emulador (nes, snes, sms, etc.)

    // Keep-alive system
    private DispatcherTimer? _keepAliveTimer;
    private bool _isReconnecting = false;
    private string? _lastHost = "127.0.0.1";
    private int _lastPort = 8888;

    public Dictionary<string, object>? ModuleData { get; private set; }

    public LiveLinkWindow(Dictionary<string, object>? input = null)
    {
        InitializeComponent();
        _input = input;
        _captureMode = input?.ContainsKey("mode") == true && input["mode"].ToString() == "capture";
        _callerId = input?.ContainsKey("callerId") == true ? input["callerId"].ToString() : null;

        DiscoverEmulators();

        if (_captureMode)
        {
            Title = "LIVE LINK — CAPTURE MODE";
            BtnImport.Content = "RETURN CAPTURE";
        }

        Loaded += async (s, e) =>
        {
            _settings = await SettingsService.LoadAsync();
            LogInfo("LiveLink initialized");
        };

        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Stop keep-alive timer
        _keepAliveTimer?.Stop();
        _keepAliveTimer = null;

        // Disconnect if still connected
        if (_connection?.IsConnected == true)
        {
            try
            {
                _connection.DisconnectAsync().Wait();
            }
            catch { }
        }

        // Kill emulator process when closing LiveLink window
        if (_emulatorProcess != null && !_emulatorProcess.HasExited)
        {
            try
            {
                LogInfo($"Closing emulator (PID: {_emulatorProcess.Id})...");
                _emulatorProcess.Kill();
                _emulatorProcess.WaitForExit(2000);
                _emulatorProcess.Dispose();
                _emulatorProcess = null;
                _lastEmulatorId = null;
            }
            catch (Exception ex)
            {
                LogWarning($"Failed to close emulator: {ex.Message}");
            }
        }
    }

    private void DiscoverEmulators()
    {
        _availableEmulators.Add(new MesenConnection());
        _availableEmulators.Add(new EmuliciousConnection());
        _availableEmulators.Add(new MgbaConnection());
    }

    private async void BtnConnect_Click(object sender, RoutedEventArgs e)
    {
        // DISCONNECT: Close connection and kill emulator
        if (_connection?.IsConnected == true)
        {
            BtnConnect.IsEnabled = false;
            LogInfo("Disconnecting from emulator...");

            try
            {
                await _connection.DisconnectAsync();
            }
            catch { }

            _connection = null;
            BtnConnect.Content = "SELECT ROM & CONNECT";
            BtnConnect.IsEnabled = true;
            BtnCaptureVRAM.IsEnabled = false;
            BtnCaptureScreen.IsEnabled = false;
            TxtStatus.Text = "Disconnected";
            TxtStatus.Foreground = Brushes.Gray;

            // Kill emulator
            if (_emulatorProcess != null && !_emulatorProcess.HasExited)
            {
                try
                {
                    LogInfo($"Closing emulator (PID: {_emulatorProcess.Id})...");
                    _emulatorProcess.Kill();
                    _emulatorProcess.WaitForExit(2000);
                    _emulatorProcess.Dispose();
                    LogInfo("Emulator closed");
                }
                catch (Exception ex)
                {
                    LogWarning($"Failed to close emulator: {ex.Message}");
                }
                finally
                {
                    _emulatorProcess = null;
                    _lastEmulatorId = null;
                }
            }

            // Stop keep-alive timer
            _keepAliveTimer?.Stop();
            _keepAliveTimer = null;

            return;
        }

        // CONNECT: Select ROM and launch emulator
        try
        {
            if (_settings == null)
            {
                _settings = await SettingsService.LoadAsync();
            }

            // Select ROM
            var romPath = SelectRomFile();
            if (string.IsNullOrEmpty(romPath))
            {
                LogInfo("ROM selection cancelled");
                return;
            }

            // Disable button immediately after ROM selection
            BtnConnect.IsEnabled = false;
            BtnCaptureVRAM.IsEnabled = false;
            BtnCaptureScreen.IsEnabled = false;
            BtnValidateSmsColors.IsEnabled = false;
            BtnExpandCanvas.IsEnabled = false;

            LogInfo($"Selected ROM: {Path.GetFileName(romPath)}");

            // Detect source console from ROM extension
            _sourceConsole = DetectConsoleFromRom(romPath);
            _lastRomPath = romPath;

            LogInfo($"Detected console: {_sourceConsole.ToUpper()}");

            // Show/hide console-specific options
            if (_sourceConsole == "nes")
            {
                PanelTileOptions.Visibility = Visibility.Visible;
                PanelNametableOptions.Visibility = Visibility.Visible;
            }
            else
            {
                PanelTileOptions.Visibility = Visibility.Collapsed;
                PanelNametableOptions.Visibility = Visibility.Collapsed;
            }

            // Detect emulator
            var emulator = DetectEmulatorByRom(romPath);
            if (emulator == null)
            {
                LogError($"No compatible emulator found for {Path.GetExtension(romPath)}");
                MessageBox.Show(
                    $"No LiveLink emulator supports this ROM format: {Path.GetExtension(romPath)}\n\n" +
                    "Supported formats:\n" +
                    "• .nes, .sfc, .smc, .sms, .gg, .sg (Mesen 2)\n" +
                    "• .gb, .gbc, .gba (Mesen 2 - experimental)\n" +
                    "• .ws, .wsc, .pce (Mesen 2 - experimental)\n" +
                    "• .col (Emulicious)",
                    "Unsupported ROM Format",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            LogInfo($"Detected emulator: {emulator.DisplayName}");
            _connection = emulator;

            // Set log callback for MesenConnection
            if (_connection is MesenConnection mesenConn)
            {
                mesenConn.SetLogCallback(msg => LogInfo($"[Mesen] {msg}"));
            }

            // Close old emulator if exists
            if (_emulatorProcess != null && !_emulatorProcess.HasExited)
            {
                LogInfo($"Closing previous emulator (PID: {_emulatorProcess.Id})...");
                try
                {
                    _emulatorProcess.Kill();
                    _emulatorProcess.WaitForExit(2000);
                    _emulatorProcess.Dispose();
                }
                catch { }
                _emulatorProcess = null;
            }

            // Launch emulator
            var emulatorKey = _connection.EmulatorId;
            if (!_settings.Targets.ContainsKey(emulatorKey))
            {
                LogError("Emulator not configured");
                MessageBox.Show(
                    $"Emulator '{_connection.DisplayName}' not configured in settings.\n\n" +
                    "Configure the emulator path in Settings → LiveLink.",
                    "Emulator Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var emulatorSettings = _settings.Targets[emulatorKey];
            var emulatorPath = emulatorSettings.LiveLinkEmulatorPath;

            if (string.IsNullOrEmpty(emulatorPath) || !File.Exists(emulatorPath))
            {
                LogError("Emulator not configured");
                MessageBox.Show(
                    $"Emulator path not configured for '{_connection.DisplayName}'.\n\n" +
                    "Configure the emulator path in Settings → LiveLink.",
                    "Emulator Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            LogInfo("Launching emulator...");
            TxtStatus.Text = "Launching emulator...";

            var scriptPath = ScriptExtractor.GetScriptPath(_connection.EmulatorId);
            var args = $"\"{romPath}\"";

            if (!string.IsNullOrEmpty(scriptPath) && File.Exists(scriptPath))
            {
                args += $" \"{scriptPath}\"";
                LogInfo($"Loading Lua script: {Path.GetFileName(scriptPath)}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = emulatorPath,
                Arguments = args,
                UseShellExecute = true
            };

            _emulatorProcess = Process.Start(startInfo);
            _lastEmulatorId = _connection.EmulatorId;
            LogInfo($"Launched {_connection.DisplayName} (PID: {_emulatorProcess?.Id})");
            LogInfo("Waiting for emulator to initialize...");

            await Task.Delay(3000);

            // Connect
            LogInfo("Attempting connection to emulator...");
            bool connected = await _connection.ConnectAsync();

            if (connected)
            {
                BtnConnect.Content = "DISCONNECT";
                BtnConnect.IsEnabled = true;
                BtnCaptureVRAM.IsEnabled = true;
                BtnCaptureScreen.IsEnabled = true;
                BtnValidateSmsColors.IsEnabled = true;
                BtnExpandCanvas.IsEnabled = false;
                TxtStatus.Text = $"Connected to {_connection.DisplayName}";
                TxtStatus.Foreground = Brushes.LimeGreen;
                LogSuccess($"✓ Connected to {_connection.DisplayName}");

                // Start keep-alive timer
                StartKeepAlive();
            }
            else
            {
                BtnConnect.Content = "SELECT ROM & CONNECT";
                BtnConnect.IsEnabled = true;
                BtnCaptureVRAM.IsEnabled = false;
                BtnCaptureScreen.IsEnabled = false;
                BtnValidateSmsColors.IsEnabled = false;
                LogError("Connection failed - check if emulator is running and script loaded");
                TxtStatus.Text = "Connection failed";
                TxtStatus.Foreground = Brushes.OrangeRed;
            }
        }
        catch (Exception ex)
        {
            _connection = null;
            BtnConnect.Content = "SELECT ROM & CONNECT";
            BtnConnect.IsEnabled = true;
            BtnCaptureVRAM.IsEnabled = false;
            BtnCaptureScreen.IsEnabled = false;
            BtnValidateSmsColors.IsEnabled = false;
            LogError($"Error: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Error";
            TxtStatus.Foreground = Brushes.Red;
        }
    }

    private string GetDebugApiInstructions(string emulatorId)
    {
        return emulatorId switch
        {
            "emulicious" => "The debug API should be enabled automatically.\n\nIf connection fails:\n1. Open Emulicious\n2. Go to Tools → Debugger\n3. The debug API will be available on port 58870",
            "mesen" => "REQUIRED STEP: Enable Network Access\n\nFor security, Mesen blocks network access by default.\n\nIn Mesen:\n1. Go to Debug → Script Window\n2. Go to Settings → Restrictions\n3. Check these options:\n   ☑ Allow access to I/O and OS functions\n   ☑ Allow network access\n\nThen restart Retruxel and try connecting again.",
            "mgba" => "1. Open mGBA\n2. Go to Tools → Settings → Emulation\n3. Enable 'Enable debugging'\n4. The API will be available on port 8888",
            _ => "Check the emulator documentation for debug API instructions."
        };
    }

    /// <summary>
    /// Starts keep-alive timer to monitor connection and auto-reconnect if dropped.
    /// Checks every 5 seconds in background.
    /// </summary>
    private void StartKeepAlive()
    {
        // Stop existing timer if any
        _keepAliveTimer?.Stop();

        _keepAliveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };

        _keepAliveTimer.Tick += async (s, e) => await KeepAliveCheck();
        _keepAliveTimer.Start();

        System.Diagnostics.Debug.WriteLine("[LiveLink] Keep-alive started (5s interval)");
    }

    /// <summary>
    /// Checks if connection is still alive. If not, attempts to reconnect.
    /// Runs in background without blocking UI.
    /// </summary>
    private async Task KeepAliveCheck()
    {
        // Skip if already reconnecting
        if (_isReconnecting)
            return;

        // Skip if no connection was established
        if (_connection == null)
            return;

        // Check if connection is alive with real ping
        bool isAlive = false;

        if (_connection is MesenConnection mesenConn)
        {
            isAlive = await mesenConn.PingAsync();
        }
        else
        {
            // Fallback for other emulators - check IsConnected
            isAlive = _connection.IsConnected;
        }

        if (isAlive)
        {
            return; // Connection OK
        }

        // Connection lost - attempt reconnect
        _isReconnecting = true;

        try
        {
            LogWarning("⚠ Connection lost - attempting to reconnect...");
            TxtStatus.Text = "Reconnecting...";
            TxtStatus.Foreground = Brushes.Orange;

            // Try to reconnect
            bool reconnected = await _connection.ConnectAsync(_lastHost, _lastPort);

            if (reconnected)
            {
                LogSuccess("✓ Reconnected successfully");
                TxtStatus.Text = $"Connected to {_connection.DisplayName}";
                TxtStatus.Foreground = Brushes.LimeGreen;
            }
            else
            {
                LogError("✗ Reconnection failed - will retry in 5s");
                TxtStatus.Text = "Connection lost (retrying...)";
                TxtStatus.Foreground = Brushes.OrangeRed;
            }
        }
        catch (Exception ex)
        {
            LogError($"Reconnection error: {ex.Message}");
            TxtStatus.Text = "Connection lost (retrying...)";
            TxtStatus.Foreground = Brushes.OrangeRed;
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    private async void BtnCaptureVRAM_Click(object sender, RoutedEventArgs e)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            LogError("Not connected to emulator");
            MessageBox.Show("Not connected to emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnCaptureVRAM.IsEnabled = false;
        BtnCaptureScreen.IsEnabled = false;
        BtnExpandCanvas.IsEnabled = false;

        try
        {
            LogInfo("Starting capture...");

            var capture = new CaptureResult();

            if (ChkCaptureTiles.IsChecked == true)
            {
                if (string.IsNullOrEmpty(_sourceConsole))
                {
                    LogError("Source console not detected");
                    return;
                }

                // Use hardware specs based on console (independent of Retruxel targets)
                var specs = GetConsoleSpecs(_sourceConsole);
                if (specs == null)
                {
                    LogError($"No specs available for console: {_sourceConsole}");
                    return;
                }
                int bpp = (int)Math.Log2(specs.ColorsPerTile);
                int bytesPerTile = (specs.TileWidth * specs.TileHeight * bpp) / 8;

                // NES: Allow selecting pattern table
                if (_sourceConsole == "nes")
                {
                    int patternTableIndex = CmbPatternTable.SelectedIndex;
                    uint startAddr = 0x0000;
                    int totalTiles = 256;

                    if (patternTableIndex == 0) // 0x0000 (BG)
                    {
                        startAddr = 0x0000;
                        totalTiles = 256;
                        LogInfo("Pattern Table: 0x0000 (BG, 256 tiles)");
                    }
                    else if (patternTableIndex == 1) // 0x1000 (Sprites)
                    {
                        startAddr = 0x1000;
                        totalTiles = 256;
                        LogInfo("Pattern Table: 0x1000 (Sprites, 256 tiles)");
                    }
                    else // Both
                    {
                        startAddr = 0x0000;
                        totalTiles = 512;
                        LogInfo("Pattern Table: Both (512 tiles)");
                    }

                    int vramSize = totalTiles * bytesPerTile;
                    LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
                    LogInfo($"Requesting tiles from VRAM (0x{startAddr:X4})...");

                    byte[] vramData = await _connection.ReadVramAsync(startAddr, vramSize);
                    LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

                    if (vramData.Length < vramSize)
                    {
                        LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
                        totalTiles = vramData.Length / bytesPerTile;
                    }

                    LogInfo($"Decoding {totalTiles} tiles ({bpp}bpp)...");
                    capture.Tiles = TileConverter.Decode(
                        vramData,
                        totalTiles,
                        bpp,
                        specs.TileWidth,
                        specs.TileHeight,
                        TileFormat.Planar,
                        InterleaveMode.Tile);
                    LogSuccess($"Decoded {capture.Tiles.Length} tiles");
                }
                else if (_sourceConsole == "snes")
                {
                    // SNES: Try to detect BG mode, but default to 4bpp if detection fails
                    int detectedBpp = 4; // Default to 4bpp (most common)

                    try
                    {
                        LogInfo("Attempting to read SNES BG mode from PPU registers...");
                        // Note: Reading PPU registers during runtime may not work reliably
                        // For now, we'll use 4bpp as default and let user know
                        LogWarning("SNES BG mode auto-detection not yet implemented - using 4bpp default");
                    }
                    catch
                    {
                        LogWarning("Could not detect SNES BG mode - using 4bpp default");
                    }

                    // Recalculate based on detected bpp
                    int detectedBytesPerTile = (specs.TileWidth * specs.TileHeight * detectedBpp) / 8;

                    // SNES: Start with smaller capture to test (256 tiles = 8KB)
                    int totalTiles = 256; // Start with 256 tiles to test
                    int vramSize = totalTiles * detectedBytesPerTile;

                    LogInfo($"Using {detectedBpp}bpp mode: {totalTiles} tiles × {detectedBytesPerTile} bytes/tile = {vramSize} bytes");
                    LogInfo($"Requesting tiles from VRAM (0x0000)...");

                    byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
                    LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

                    // Debug: Log first 64 bytes to check if data looks valid
                    var preview = string.Join(" ", vramData.Take(64).Select(b => b.ToString("X2")));
                    LogInfo($"First 64 bytes: {preview}");

                    // Check if data looks like valid tile data (not all zeros or all 0xFF)
                    int zeroCount = vramData.Take(256).Count(b => b == 0x00);
                    int ffCount = vramData.Take(256).Count(b => b == 0xFF);
                    if (zeroCount > 240 || ffCount > 240)
                    {
                        LogWarning($"⚠ VRAM data looks suspicious: {zeroCount} zeros, {ffCount} 0xFF in first 256 bytes");
                        LogWarning($"⚠ This may indicate VRAM is not initialized or wrong memory type");
                    }

                    if (vramData.Length < vramSize)
                    {
                        LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / detectedBytesPerTile} tiles");
                        totalTiles = vramData.Length / detectedBytesPerTile;
                    }

                    LogInfo($"Decoding {totalTiles} tiles ({detectedBpp}bpp, line-interleaved)...");
                    capture.Tiles = TileConverter.Decode(
                        vramData,
                        totalTiles,
                        detectedBpp,
                        specs.TileWidth,
                        specs.TileHeight,
                        TileFormat.Planar,
                        InterleaveMode.Line); // SNES uses line-interleaved (bitplane pairs)
                    LogSuccess($"Decoded {capture.Tiles.Length} tiles");
                }
                else if (_sourceConsole == "gb" || _sourceConsole == "gbc")
                {
                    // GB/GBC: 8KB VRAM (384 tiles), 2bpp
                    int totalTiles = specs.MaxTilesInVram;
                    int vramSize = totalTiles * bytesPerTile;

                    LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
                    LogInfo($"Requesting tiles from VRAM...");

                    byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
                    LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

                    if (vramData.Length < vramSize)
                    {
                        LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
                        totalTiles = vramData.Length / bytesPerTile;
                    }

                    LogInfo($"Decoding {totalTiles} tiles ({bpp}bpp, InterleaveMode.Line)...");
                    capture.Tiles = TileConverter.Decode(
                        vramData,
                        totalTiles,
                        bpp,
                        specs.TileWidth,
                        specs.TileHeight,
                        TileFormat.Planar,
                        InterleaveMode.Line); // GB uses line-interleaved like SMS
                    LogSuccess($"Decoded {capture.Tiles.Length} tiles");
                }
                else if (_sourceConsole == "gba" || _sourceConsole == "ws" || _sourceConsole == "wsc" || _sourceConsole == "pce")
                {
                    LogWarning($"{_sourceConsole.ToUpper()} tile capture not yet fully implemented");
                }
                else if (_sourceConsole == "sms" || _sourceConsole == "gg")
                {
                    // SMS/GG: 4bpp, line-interleaved
                    int totalTiles = specs.MaxTilesInVram;
                    int vramSize = totalTiles * bytesPerTile;

                    LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
                    LogInfo($"Requesting tiles from VRAM...");

                    byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
                    LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

                    if (vramData.Length < vramSize)
                    {
                        LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
                        totalTiles = vramData.Length / bytesPerTile;
                    }

                    LogInfo($"Decoding {totalTiles} tiles (4bpp, InterleaveMode.Line)...");
                    capture.Tiles = TileConverter.Decode(
                        vramData,
                        totalTiles,
                        bpp,
                        specs.TileWidth,
                        specs.TileHeight,
                        TileFormat.Planar,
                        InterleaveMode.Line);
                    LogSuccess($"Decoded {capture.Tiles.Length} tiles");
                }
                else if (_sourceConsole == "sg1000")
                {
                    // SG-1000: 1bpp (TMS9918), tile-interleaved
                    int totalTiles = specs.MaxTilesInVram;
                    int vramSize = totalTiles * bytesPerTile;

                    LogInfo($"VRAM calculation: {totalTiles} tiles × {bytesPerTile} bytes/tile = {vramSize} bytes");
                    LogInfo($"Requesting tiles from VRAM...");

                    byte[] vramData = await _connection.ReadVramAsync(0x0000, vramSize);
                    LogSuccess($"✓ Received: {vramData.Length} bytes (expected {vramSize})");

                    if (vramData.Length < vramSize)
                    {
                        LogWarning($"⚠ Received less data than expected, adjusting tile count to {vramData.Length / bytesPerTile} tiles");
                        totalTiles = vramData.Length / bytesPerTile;
                    }

                    LogInfo($"Decoding {totalTiles} tiles (1bpp, InterleaveMode.Tile)...");
                    capture.Tiles = TileConverter.Decode(
                        vramData,
                        totalTiles,
                        1, // 1bpp for SG-1000
                        specs.TileWidth,
                        specs.TileHeight,
                        TileFormat.Planar,
                        InterleaveMode.Tile);
                    LogSuccess($"Decoded {capture.Tiles.Length} tiles");
                }
                else
                {
                    LogWarning($"{_sourceConsole.ToUpper()} tile capture not yet implemented");
                }
            }

            if (ChkCaptureNametable.IsChecked == true)
            {
                if (_sourceConsole == "nes")
                {
                    // NES: Allow selecting which nametable to capture
                    int nametableIndex = CmbNametable.SelectedIndex;
                    uint[] nametableAddresses = { 0x2000, 0x2400, 0x2800, 0x2C00 };
                    uint nametableAddr = nametableAddresses[nametableIndex];

                    LogInfo($"Requesting NES nametable from VRAM (0x{nametableAddr:X4})...");
                    byte[] nametableData = await _connection.ReadVramAsync(nametableAddr, 32 * 30);
                    LogSuccess($"✓ Nametable received: {nametableData.Length} bytes");

                    // Read attribute table (64 bytes after nametable)
                    uint attributeAddr = nametableAddr + 0x3C0; // 960 bytes after nametable start
                    LogInfo($"Requesting NES attribute table from VRAM (0x{attributeAddr:X4})...");
                    byte[] attributeData = await _connection.ReadVramAsync(attributeAddr, 64);
                    LogSuccess($"✓ Attribute table received: {attributeData.Length} bytes");

                    LogInfo("Decoding nametable...");
                    capture.Nametable = NametableDecoder.Decode(nametableData, 32, 30, 1);
                    capture.NametableWidth = 32;
                    capture.NametableHeight = 30;

                    // Store attribute table in metadata
                    capture.Metadata["attributeTable"] = attributeData;

                    LogSuccess($"Decoded nametable: {capture.NametableWidth}x{capture.NametableHeight}");
                }
                else if (_sourceConsole == "snes")
                {
                    // SNES: Tilemap at various locations depending on mode
                    // For now, skip nametable capture for SNES
                    LogWarning("SNES nametable capture not yet implemented");
                }
                else if (_sourceConsole == "sms" || _sourceConsole == "gg")
                {
                    // SMS/GG nametable at 0x3800 (32x28, 2 bytes per entry)
                    LogInfo("Requesting SMS/GG nametable from VRAM (0x3800)...");
                    byte[] nametableData = await _connection.ReadVramAsync(0x3800, 32 * 28 * 2);
                    LogSuccess($"✓ Nametable received: {nametableData.Length} bytes");

                    LogInfo("Decoding nametable...");
                    capture.Nametable = NametableDecoder.Decode(nametableData, 32, 28, 2);
                    capture.NametableWidth = 32;
                    capture.NametableHeight = 28;
                    LogSuccess($"Decoded nametable: {capture.NametableWidth}x{capture.NametableHeight}");
                }
                else if (_sourceConsole == "sg1000")
                {
                    // SG-1000 (TMS9918) nametable
                    // TMS9918 Name Table is typically at 0x3800 (not 0x1800)
                    // Address is configurable via VDP registers, but 0x3800 is most common
                    LogInfo("Requesting SG-1000 nametable from VRAM (0x3800)...");
                    byte[] nametableData = await _connection.ReadVramAsync(0x3800, 32 * 24);
                    LogSuccess($"✓ Nametable received: {nametableData.Length} bytes");

                    // Debug: Log first 32 bytes to verify data
                    var preview = string.Join(" ", nametableData.Take(32).Select(b => b.ToString("X2")));
                    LogInfo($"First 32 bytes: {preview}");

                    LogInfo("Decoding nametable...");
                    capture.Nametable = NametableDecoder.Decode(nametableData, 32, 24, 1);
                    capture.NametableWidth = 32;
                    capture.NametableHeight = 24;

                    // Debug: Log first 32 decoded entries
                    var decodedPreview = string.Join(" ", capture.Nametable.Take(32).Select(n => n.ToString("X2")));
                    LogInfo($"First 32 decoded: {decodedPreview}");

                    LogSuccess($"Decoded nametable: {capture.NametableWidth}x{capture.NametableHeight}");
                }
            }

            if (ChkCapturePalette.IsChecked == true)
            {
                // NES: Palette in PPU memory at 0x3F00 (32 bytes)
                // SMS: Palette in CRAM via CPU memory at 0xC000 (32 bytes)
                if (_sourceConsole == "nes")
                {
                    LogInfo("Requesting NES palette from PPU (0x3F00)...");
                    byte[] paletteData = await _connection.ReadVramAsync(0x3F00, 32);
                    LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

                    LogInfo("Decoding NES palette...");
                    capture.Palette = DecodeNesPalette(paletteData);
                    LogSuccess($"Decoded {capture.Palette.Length} colors");
                }
                else if (_sourceConsole == "snes")
                {
                    LogInfo("Requesting SNES palette from CGRAM...");
                    // SNES CGRAM: 512 bytes (256 colors × 2 bytes)
                    // Mesen 2 exposes this via ReadCramAsync or palette memory type
                    byte[] paletteData;
                    if (_connection is MesenConnection mesenConn)
                    {
                        // Try reading CGRAM via Mesen's palette interface
                        paletteData = await mesenConn.ReadCramAsync(256); // 256 colors
                    }
                    else
                    {
                        paletteData = await _connection.ReadMemoryAsync(0x0000, 512);
                    }
                    LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

                    LogInfo("Decoding SNES palette...");
                    capture.Palette = DecodeSnesPalette(paletteData);
                    LogSuccess($"Decoded {capture.Palette.Length} colors");
                }
                else if (_sourceConsole == "sms")
                {
                    LogInfo("Requesting SMS palette from CRAM...");

                    byte[] paletteData;
                    if (_connection is MesenConnection mesenConn)
                    {
                        paletteData = await mesenConn.ReadCramAsync(32);
                    }
                    else
                    {
                        paletteData = await _connection.ReadMemoryAsync(0xC000, 32);
                    }

                    LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

                    LogInfo("Decoding SMS palette (6-bit RGB)...");
                    capture.Palette = DecodeSmsPalette(paletteData);
                    LogSuccess($"Decoded {capture.Palette.Length} colors");
                }
                else if (_sourceConsole == "sg1000")
                {
                    LogInfo("Using SG-1000 fixed TMS9918 palette...");

                    // TMS9918 has a fixed 16-color palette (no CRAM)
                    // Colors are: Transparent, Black, Medium Green, Light Green, Dark Blue, Light Blue,
                    // Dark Red, Cyan, Medium Red, Light Red, Dark Yellow, Light Yellow,
                    // Dark Green, Magenta, Gray, White
                    uint[] tms9918Palette = new uint[16]
                    {
                        0x00000000, // 0: Transparent
                        0xFF000000, // 1: Black
                        0xFF21C842, // 2: Medium Green
                        0xFF5EDC78, // 3: Light Green
                        0xFF5455ED, // 4: Dark Blue
                        0xFF7D76FC, // 5: Light Blue
                        0xFFD4524D, // 6: Dark Red
                        0xFF42EBF5, // 7: Cyan
                        0xFFFC5554, // 8: Medium Red
                        0xFFFF7978, // 9: Light Red
                        0xFFD4C154, // 10: Dark Yellow
                        0xFFE6CE80, // 11: Light Yellow
                        0xFF21B03B, // 12: Dark Green
                        0xFFC95BBA, // 13: Magenta
                        0xFFCCCCCC, // 14: Gray
                        0xFFFFFFFF  // 15: White
                    };

                    capture.Palette = tms9918Palette;
                    LogSuccess($"Loaded TMS9918 fixed palette: {capture.Palette.Length} colors");

                    // Read Color Table
                    // Graphics Mode I: 32 bytes at 0x2000 (1 byte per 8 tiles)
                    // Graphics Mode II: 6144 bytes at 0x2000 (8 bytes per tile, 768 tiles)
                    // Try reading more to detect mode
                    LogInfo("Requesting SG-1000 Color Table from VRAM (0x2000)...");
                    byte[] colorTableData = await _connection.ReadVramAsync(0x2000, 2048); // Read first 2KB to check
                    LogSuccess($"✓ Color Table received: {colorTableData.Length} bytes");

                    // Debug: Log first 32 bytes
                    var ctPreview = string.Join(" ", colorTableData.Take(32).Select(b => b.ToString("X2")));
                    LogInfo($"First 32 bytes: {ctPreview}");

                    // Debug: Log bytes 32-64 to check if Mode II
                    var ctPreview2 = string.Join(" ", colorTableData.Skip(32).Take(32).Select(b => b.ToString("X2")));
                    LogInfo($"Bytes 32-64: {ctPreview2}");

                    // Check if Graphics Mode II (bytes should vary after first 32)
                    bool isGraphicsModeII = !colorTableData.Skip(32).Take(32).All(b => b == colorTableData[0]);
                    LogInfo($"Graphics Mode: {(isGraphicsModeII ? "Mode II (per-line colors)" : "Mode I (per-8-tiles colors)")}");

                    // Store Color Table in metadata for later processing
                    capture.Metadata["colorTable"] = colorTableData;
                    capture.Metadata["graphicsMode"] = isGraphicsModeII ? "mode2" : "mode1";
                }
                else if (_sourceConsole == "gg")
                {
                    LogInfo("Requesting Game Gear palette from CRAM...");

                    byte[] paletteData;
                    if (_connection is MesenConnection mesenConn)
                    {
                        // Game Gear: 32 colors × 2 bytes = 64 bytes
                        paletteData = await mesenConn.ReadCramAsync(32);
                        LogInfo($"Game Gear palette: requested 32 colors, received {paletteData.Length} bytes");
                    }
                    else
                    {
                        paletteData = await _connection.ReadMemoryAsync(0xC000, 64);
                    }

                    LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

                    LogInfo("Decoding Game Gear palette (12-bit RGB)...");
                    capture.Palette = DecodeGameGearPalette(paletteData);
                    LogSuccess($"Decoded {capture.Palette.Length} colors");
                }
                else if (_sourceConsole == "gb" || _sourceConsole == "gbc")
                {
                    LogInfo("Requesting GB/GBC palette from CRAM...");

                    byte[] paletteData;
                    if (_connection is MesenConnection mesenConn)
                    {
                        paletteData = await mesenConn.ReadCramAsync(32);
                    }
                    else
                    {
                        paletteData = await _connection.ReadMemoryAsync(0xFF47, 3);
                    }

                    LogSuccess($"✓ Palette received: {paletteData.Length} bytes");

                    LogInfo("Decoding GB/GBC palette (15-bit RGB)...");
                    capture.Palette = DecodeGameBoyPalette(paletteData);
                    LogSuccess($"Decoded {capture.Palette.Length} colors");
                }
                else if (_sourceConsole == "gba" || _sourceConsole == "ws" || _sourceConsole == "wsc" || _sourceConsole == "pce")
                {
                    LogWarning($"{_sourceConsole.ToUpper()} palette capture not yet implemented");
                }
            }

            _lastCapture = capture;
            BtnImport.IsEnabled = true;

            // Enable Canvas Expansion if nametable was captured
            bool hasNametable = capture.Nametable != null &&
                               capture.NametableWidth > 0 &&
                               capture.NametableHeight > 0;
            BtnExpandCanvas.IsEnabled = hasNametable;

            if (hasNametable)
            {
                LogInfo($"Canvas Expansion available for {capture.NametableWidth}×{capture.NametableHeight} nametable");
            }

            // Render preview
            RenderPreview(capture);

            LogSuccess("✓ Capture complete!");
        }
        catch (Exception ex)
        {
            LogError($"Capture failed: {ex.Message}");
        }
        finally
        {
            BtnCaptureVRAM.IsEnabled = true;
            BtnCaptureScreen.IsEnabled = true;
        }
    }

    private uint[] DecodeSmsPalette(byte[] paletteData)
    {
        // SMS: 6-bit RGB (2 bits per channel) - format: 00BBGGRR
        var palette = new uint[paletteData.Length];
        for (int i = 0; i < paletteData.Length; i++)
        {
            byte smsColor = paletteData[i];
            byte r = (byte)(((smsColor >> 0) & 0x03) * 85);
            byte g = (byte)(((smsColor >> 2) & 0x03) * 85);
            byte b = (byte)(((smsColor >> 4) & 0x03) * 85);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeGameGearPalette(byte[] paletteData)
    {
        // Game Gear: 12-bit RGB (4 bits per channel) - 2 bytes per color, little-endian
        // Format: 0000BBBBGGGGRRRR
        var palette = new uint[paletteData.Length / 2];
        for (int i = 0; i < palette.Length; i++)
        {
            ushort ggColor = (ushort)(paletteData[i * 2] | (paletteData[i * 2 + 1] << 8));
            byte r = (byte)(((ggColor >> 0) & 0x0F) * 17);
            byte g = (byte)(((ggColor >> 4) & 0x0F) * 17);
            byte b = (byte)(((ggColor >> 8) & 0x0F) * 17);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeGameBoyPalette(byte[] paletteData)
    {
        // GB/GBC: 15-bit RGB (5-5-5), 2 bytes per color, little-endian
        var palette = new uint[paletteData.Length / 2];
        for (int i = 0; i < palette.Length; i++)
        {
            ushort color15 = (ushort)(paletteData[i * 2] | (paletteData[i * 2 + 1] << 8));
            byte r = (byte)(((color15 >> 0) & 0x1F) * 8);
            byte g = (byte)(((color15 >> 5) & 0x1F) * 8);
            byte b = (byte)(((color15 >> 10) & 0x1F) * 8);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeSnesPalette(byte[] paletteData)
    {
        // SNES: 15-bit RGB (5-5-5), 2 bytes per color, little-endian
        var palette = new uint[paletteData.Length / 2];
        for (int i = 0; i < palette.Length; i++)
        {
            ushort color15 = (ushort)(paletteData[i * 2] | (paletteData[i * 2 + 1] << 8));
            byte r = (byte)(((color15 >> 0) & 0x1F) * 8);
            byte g = (byte)(((color15 >> 5) & 0x1F) * 8);
            byte b = (byte)(((color15 >> 10) & 0x1F) * 8);
            palette[i] = (uint)((0xFF << 24) | (r << 16) | (g << 8) | b);
        }
        return palette;
    }

    private uint[] DecodeNesPalette(byte[] paletteData)
    {
        // NES master palette (64 colors)
        uint[] nesMasterPalette = new uint[64]
        {
            0xFF666666, 0xFF002A88, 0xFF1412A7, 0xFF3B00A4, 0xFF5C007E, 0xFF6E0040, 0xFF6C0600, 0xFF561D00,
            0xFF333500, 0xFF0B4800, 0xFF005200, 0xFF004F08, 0xFF00404D, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFADADAD, 0xFF155FD9, 0xFF4240FF, 0xFF7527FE, 0xFFA01ACC, 0xFFB71E7B, 0xFFB53120, 0xFF994E00,
            0xFF6B6D00, 0xFF388700, 0xFF0C9300, 0xFF008F32, 0xFF007C8D, 0xFF000000, 0xFF000000, 0xFF000000,
            0xFFFFFEFF, 0xFF64B0FF, 0xFF9290FF, 0xFFC676FF, 0xFFF36AFF, 0xFFFE6ECC, 0xFFFE8170, 0xFFEA9E22,
            0xFFBCBE00, 0xFF88D800, 0xFF5CE430, 0xFF45E082, 0xFF48CDDE, 0xFF4F4F4F, 0xFF000000, 0xFF000000,
            0xFFFFFEFF, 0xFFC0DFFF, 0xFFD3D2FF, 0xFFE8C8FF, 0xFFFBC2FF, 0xFFFEC4EA, 0xFFFECCC5, 0xFFF7D8A5,
            0xFFE4E594, 0xFFCFEF96, 0xFFBDF4AB, 0xFFB3F3CC, 0xFFB5EBF2, 0xFFB8B8B8, 0xFF000000, 0xFF000000
        };

        var palette = new uint[paletteData.Length];

        // Debug: Log raw palette data
        var rawIndices = string.Join(" ", paletteData.Select(b => $"{b:X2}"));
        LogInfo($"NES palette raw bytes: {rawIndices}");

        for (int i = 0; i < paletteData.Length; i++)
        {
            byte colorIndex = (byte)(paletteData[i] & 0x3F); // NES palette is 6-bit
            palette[i] = nesMasterPalette[colorIndex];
        }

        // Debug: Count unique colors
        var uniqueColors = palette.Distinct().ToList();
        LogInfo($"NES palette: {palette.Length} total entries, {uniqueColors.Count} unique colors");

        // Debug: Show which palette slots use which colors
        for (int pal = 0; pal < 8; pal++)
        {
            var palColors = new List<string>();
            for (int col = 0; col < 4; col++)
            {
                int idx = pal * 4 + col;
                if (idx < paletteData.Length)
                {
                    byte rawIdx = paletteData[idx];
                    palColors.Add($"{rawIdx:X2}");
                }
            }
            LogInfo($"  Palette {pal}: [{string.Join(", ", palColors)}]");
        }

        return palette;
    }

    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCapture == null)
        {
            LogError("No capture data available");
            return;
        }

        try
        {
            // Open palette optimization preview window
            LogInfo("Opening palette optimization preview...");

            // Use the SAME bitmap that's being displayed in ImgPreview
            var previewBitmap = ImgPreview.Source as BitmapSource;
            if (previewBitmap == null)
            {
                LogError("No preview image available");
                return;
            }

            // Determine target color count based on destination target
            int targetColorCount = 16; // Default
            if (_input?.TryGetValue("targetId", out var targetObj) == true)
            {
                var targetId = targetObj?.ToString();
                targetColorCount = targetId switch
                {
                    "sms" => 32,  // 2 palettes × 16 colors (hardware max)
                    "gg" => 32,   // 2 palettes × 16 colors (hardware max)
                    "nes" => 16,  // 4 palettes × 4 colors (hardware max)
                    "snes" => 256, // 8 palettes × 32 colors (common mode)
                    "gb" => 32,   // 8 palettes × 4 colors
                    "gbc" => 64,  // 8 palettes × 4 colors (BG) + 8 palettes × 4 colors (sprites)
                    _ => 16
                };
            }

            // Open preview window - it will handle optimization internally
            var previewWindow = new Windows.PaletteOptimizationWindow(
                previewBitmap,
                targetColorCount,
                ChkUseLab.IsChecked == true,
                _input?.TryGetValue("targetId", out var targetIdForPreview) == true ? targetIdForPreview?.ToString() ?? "sms" : "sms");

            previewWindow.Owner = this;

            if (previewWindow.ShowDialog() != true)
            {
                LogInfo("Import cancelled by user");
                return;
            }

            // User confirmed - get the optimized bitmap and palette from preview
            var optimizedBitmap = previewWindow.OptimizedBitmap;
            var optimizedPalette = previewWindow.OptimizedPalette;
            double selectedDiversity = previewWindow.SelectedDiversity;

            LogInfo($"User selected diversity: {selectedDiversity:F2}");
            LogInfo($"Optimized palette: {optimizedPalette.Count} colors");

            // Check if capture has nametable (tilemap) or is tileset-only
            bool hasNametable = _lastCapture.Nametable != null &&
                               _lastCapture.NametableWidth > 0 &&
                               _lastCapture.NametableHeight > 0;

            LogInfo($"Capture type: {(hasNametable ? "Tilemap (with nametable)" : "Tileset only (no nametable)")}");

            CaptureResult optimizedCapture;

            if (hasNametable)
            {
                // Convert optimized bitmap back to tiles + nametable
                LogInfo("Converting optimized image to tiles...");

                // Extract pixels from optimized bitmap
                var optimizedPixels = ExtractPixelsFromBitmap(optimizedBitmap);

                // Convert to CaptureResult format
                optimizedCapture = ConvertBitmapToCapture(optimizedBitmap, optimizedPalette, _lastCapture);
            }
            else
            {
                // Tileset-only mode: keep original tiles, just update palette
                LogInfo("Tileset-only mode: using original tiles with optimized palette");

                var newPalette = optimizedPalette.Select(c =>
                    0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

                optimizedCapture = new CaptureResult
                {
                    Tiles = _lastCapture.Tiles,
                    Palette = newPalette,
                    Nametable = Array.Empty<ushort>(),
                    NametableWidth = 0,
                    NametableHeight = 0,
                    TileWidth = _lastCapture.TileWidth,
                    TileHeight = _lastCapture.TileHeight,
                    TargetId = _lastCapture.TargetId,
                    Metadata = new Dictionary<string, object>(_lastCapture.Metadata)
                };
            }

            LogInfo("Converting capture to standardized format...");

            // Use pipeline to convert CaptureResult → ImportedAssetData
            var pipeline = new CaptureToImportedAssetPipeline();
            var options = new Dictionary<string, object>
            {
                ["sourceEmulator"] = _connection?.EmulatorId ?? "unknown",
                ["destinationTarget"] = _input?.TryGetValue("targetId", out var target) == true ? target : null!,
                ["useLab"] = ChkUseLab.IsChecked == true,
                ["diversity"] = selectedDiversity
            };

            var importedData = pipeline.ProcessTyped(optimizedCapture, options);

            // If no nametable, pass the optimized bitmap to be saved directly
            if (!hasNametable)
            {
                options["optimizedBitmap"] = optimizedBitmap;
                options["originalPalette"] = _lastCapture.Palette; // Original RGB palette from emulator
                LogInfo("Passing optimized bitmap for direct PNG save (tileset-only mode)");
            }

            LogSuccess($"Converted: {importedData.GetSummary()}");

            if (!importedData.IsValid(out var errorMessage))
            {
                LogError($"Validation failed: {errorMessage}");
                return;
            }

            // Return data based on caller
            if (_captureMode && !string.IsNullOrEmpty(_callerId))
            {
                LogSuccess($"Returning imported data to {_callerId}");

                // Store options in metadata so they can be passed to the pipeline
                if (!hasNametable)
                {
                    importedData.Metadata["optimizedBitmap"] = optimizedBitmap;
                    importedData.Metadata["originalPalette"] = _lastCapture.Palette;
                    LogInfo("Stored optimized bitmap in ImportedAssetData metadata");
                }

                DialogResult = true;
                ModuleData = new Dictionary<string, object>
                {
                    ["callerId"] = _callerId,
                    ["importedAssetData"] = importedData
                };
                Close();
                return;
            }

            LogInfo("Import to project not yet implemented");
        }
        catch (Exception ex)
        {
            LogError($"Import failed: {ex.Message}");
        }
    }

    private CaptureResult ConvertBitmapToCapture(BitmapSource bitmap, List<(byte R, byte G, byte B)> palette, CaptureResult originalCapture)
    {
        // Convert palette to uint[]
        var paletteUint = palette.Select(c =>
            0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

        // Create palette lookup for fast color-to-index conversion
        var paletteLookup = new Dictionary<uint, byte>();
        for (int i = 0; i < paletteUint.Length; i++)
        {
            paletteLookup[paletteUint[i]] = (byte)i;
        }

        // Extract pixels from bitmap
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        BitmapSource convertedBitmap = bitmap;
        if (bitmap.Format != System.Windows.Media.PixelFormats.Bgra32)
        {
            convertedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        }

        int stride = width * 4;
        byte[] pixels = new byte[height * stride];
        convertedBitmap.CopyPixels(pixels, stride, 0);

        // Convert pixels to tiles using original nametable structure
        int tileSize = 8;
        var tiles = new byte[originalCapture.Tiles.Length][];

        for (int tileIdx = 0; tileIdx < tiles.Length; tileIdx++)
        {
            tiles[tileIdx] = new byte[tileSize * tileSize];
        }

        // Map pixels to tiles based on nametable
        for (int ty = 0; ty < originalCapture.NametableHeight; ty++)
        {
            for (int tx = 0; tx < originalCapture.NametableWidth; tx++)
            {
                int nametableIdx = ty * originalCapture.NametableWidth + tx;
                if (nametableIdx >= originalCapture.Nametable.Length)
                    continue;

                ushort tileIdx = originalCapture.Nametable[nametableIdx];
                if (tileIdx >= tiles.Length)
                    continue;

                for (int py = 0; py < tileSize; py++)
                {
                    for (int px = 0; px < tileSize; px++)
                    {
                        int x = tx * tileSize + px;
                        int y = ty * tileSize + py;

                        if (x >= width || y >= height)
                            continue;

                        int pixelOffset = y * stride + x * 4;
                        byte b = pixels[pixelOffset + 0];
                        byte g = pixels[pixelOffset + 1];
                        byte r = pixels[pixelOffset + 2];

                        uint color = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;

                        byte colorIdx = paletteLookup.TryGetValue(color, out var idx) ? idx : (byte)0;

                        int tilePixelIdx = py * tileSize + px;
                        tiles[tileIdx][tilePixelIdx] = colorIdx;
                    }
                }
            }
        }

        return new CaptureResult
        {
            Tiles = tiles,
            Palette = paletteUint,
            Nametable = originalCapture.Nametable,
            NametableWidth = originalCapture.NametableWidth,
            NametableHeight = originalCapture.NametableHeight,
            TileWidth = originalCapture.TileWidth,
            TileHeight = originalCapture.TileHeight,
            TargetId = originalCapture.TargetId,
            Metadata = new Dictionary<string, object>(originalCapture.Metadata)
        };
    }

    private List<(byte R, byte G, byte B)> ExtractPixelsFromBitmap(BitmapSource bitmap)
    {
        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        BitmapSource convertedBitmap = bitmap;
        if (bitmap.Format != System.Windows.Media.PixelFormats.Bgra32)
        {
            convertedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        }

        int stride = width * 4;
        byte[] pixels = new byte[height * stride];

        convertedBitmap.CopyPixels(pixels, stride, 0);

        var result = new List<(byte R, byte G, byte B)>();
        for (int i = 0; i < pixels.Length; i += 4)
        {
            result.Add((pixels[i + 2], pixels[i + 1], pixels[i]));
        }

        return result;
    }

    private CaptureResult ApplyOptimizedPaletteToCapture(CaptureResult originalCapture, List<(byte R, byte G, byte B)> optimizedPalette)
    {
        // Convert optimized palette to uint[]
        var newPalette = optimizedPalette.Select(c =>
            0xFF000000u | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B).ToArray();

        // Remap tile color indices to new palette
        var newTiles = new byte[originalCapture.Tiles.Length][];

        for (int i = 0; i < originalCapture.Tiles.Length; i++)
        {
            var oldTile = originalCapture.Tiles[i];
            var newTile = new byte[oldTile.Length];

            for (int j = 0; j < oldTile.Length; j++)
            {
                byte oldColorIdx = oldTile[j];
                if (oldColorIdx < originalCapture.Palette.Length)
                {
                    uint oldColor = originalCapture.Palette[oldColorIdx];
                    byte oldR = (byte)((oldColor >> 16) & 0xFF);
                    byte oldG = (byte)((oldColor >> 8) & 0xFF);
                    byte oldB = (byte)(oldColor & 0xFF);

                    // Find closest color in new palette
                    int closestIdx = 0;
                    double minDist = double.MaxValue;

                    for (int k = 0; k < optimizedPalette.Count; k++)
                    {
                        var newColor = optimizedPalette[k];
                        double dist = Math.Sqrt(
                            Math.Pow(oldR - newColor.R, 2) +
                            Math.Pow(oldG - newColor.G, 2) +
                            Math.Pow(oldB - newColor.B, 2));

                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestIdx = k;
                        }
                    }

                    newTile[j] = (byte)closestIdx;
                }
            }

            newTiles[i] = newTile;
        }

        // Create new capture with optimized palette
        return new CaptureResult
        {
            Tiles = newTiles,
            Palette = newPalette,
            Nametable = originalCapture.Nametable,
            NametableWidth = originalCapture.NametableWidth,
            NametableHeight = originalCapture.NametableHeight,
            TileWidth = originalCapture.TileWidth,
            TileHeight = originalCapture.TileHeight,
            TargetId = originalCapture.TargetId,
            Metadata = new Dictionary<string, object>(originalCapture.Metadata)
        };
    }

    private System.Windows.Media.Imaging.BitmapSource? CreatePreviewBitmap(CaptureResult capture)
    {
        // This creates a bitmap with ORIGINAL source console colors (not mapped to target hardware)
        if (capture.Tiles == null || capture.Tiles.Length == 0 || capture.Palette == null || capture.Palette.Length == 0)
            return null;

        if (capture.Nametable == null || capture.NametableWidth == 0 || capture.NametableHeight == 0)
            return null;

        int tileSize = 8;
        int width = capture.NametableWidth * tileSize;
        int height = capture.NametableHeight * tileSize;

        // Detect console type and palette format
        bool isNes = capture.Metadata.ContainsKey("attributeTable");
        byte[]? nesAttributeTable = isNes ? capture.Metadata["attributeTable"] as byte[] : null;

        bool hasMultiplePalettes = capture.Palette.Length > 16;
        int colorsPerPalette = hasMultiplePalettes ? 16 : capture.Palette.Length;

        LogInfo($"CreatePreviewBitmap: Console={(_sourceConsole ?? "unknown")}, Palette has {capture.Palette.Length} colors, hasMultiplePalettes={hasMultiplePalettes}, colorsPerPalette={colorsPerPalette}");

        if (isNes && nesAttributeTable != null)
        {
            LogInfo($"NES mode: Using attribute table ({nesAttributeTable.Length} bytes)");
        }

        // Debug: Log first few nametable entries
        var sampleEntries = capture.Nametable.Take(10).Select(e => $"0x{e:X4}");
        LogInfo($"Sample nametable entries: {string.Join(", ", sampleEntries)}");

        var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
            width, height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);

        bitmap.Lock();

        unsafe
        {
            byte* ptr = (byte*)bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            for (int ty = 0; ty < capture.NametableHeight; ty++)
            {
                for (int tx = 0; tx < capture.NametableWidth; tx++)
                {
                    int nametableIdx = ty * capture.NametableWidth + tx;
                    if (nametableIdx >= capture.Nametable.Length)
                        continue;

                    ushort nametableEntry = capture.Nametable[nametableIdx];

                    ushort tileIdx;
                    bool hFlip = false;
                    bool vFlip = false;
                    byte paletteIdx = 0;

                    if (isNes && nesAttributeTable != null)
                    {
                        // NES: Nametable entry is just tile index
                        tileIdx = nametableEntry;

                        // Get palette from attribute table
                        // Each attribute byte controls 4x4 tiles (2 bits per 2x2 tile group)
                        int attrX = tx / 4;
                        int attrY = ty / 4;
                        int attrIdx = attrY * 8 + attrX; // 8 attribute bytes per row (32 tiles / 4)

                        if (attrIdx < nesAttributeTable.Length)
                        {
                            byte attrByte = nesAttributeTable[attrIdx];

                            // Determine which 2x2 quadrant within the 4x4 block
                            int quadX = (tx % 4) / 2; // 0 or 1
                            int quadY = (ty % 4) / 2; // 0 or 1
                            int quadrant = quadY * 2 + quadX; // 0=TL, 1=TR, 2=BL, 3=BR

                            // Extract 2 bits for this quadrant
                            paletteIdx = (byte)((attrByte >> (quadrant * 2)) & 0x03);
                        }
                    }
                    else
                    {
                        // SMS/GG: Extract tile attributes from nametable entry
                        tileIdx = (ushort)(nametableEntry & 0x1FF); // Bits 0-8: tile index
                        hFlip = (nametableEntry & 0x200) != 0;        // Bit 9: horizontal flip
                        vFlip = (nametableEntry & 0x400) != 0;        // Bit 10: vertical flip
                        paletteIdx = (byte)((nametableEntry >> 11) & 0x01); // Bit 11: palette select
                    }

                    if (tileIdx >= capture.Tiles.Length)
                        continue;

                    var tile = capture.Tiles[tileIdx];

                    for (int py = 0; py < tileSize; py++)
                    {
                        for (int px = 0; px < tileSize; px++)
                        {
                            // Apply flip transformations
                            int srcPx = hFlip ? (tileSize - 1 - px) : px;
                            int srcPy = vFlip ? (tileSize - 1 - py) : py;
                            int pixelIdx = srcPy * tileSize + srcPx;

                            if (pixelIdx >= tile.Length)
                                continue;

                            byte colorIdx = tile[pixelIdx];

                            // Apply palette offset
                            int finalColorIdx = colorIdx;
                            if (isNes)
                            {
                                // NES: 4 palettes of 4 colors each (indices 0-3 per palette)
                                // Palette 0 at indices 0-3, Palette 1 at 4-7, etc.
                                finalColorIdx = (paletteIdx * 4) + colorIdx;
                            }
                            else if (hasMultiplePalettes && paletteIdx > 0)
                            {
                                // SMS/GG: 2 palettes of 16 colors each
                                finalColorIdx = colorIdx + (paletteIdx * colorsPerPalette);
                            }

                            if (finalColorIdx >= capture.Palette.Length)
                                continue;

                            uint color = capture.Palette[finalColorIdx];

                            int x = tx * tileSize + px;
                            int y = ty * tileSize + py;
                            int offset = y * stride + x * 4;

                            ptr[offset + 0] = (byte)((color >> 0) & 0xFF);  // B
                            ptr[offset + 1] = (byte)((color >> 8) & 0xFF);  // G
                            ptr[offset + 2] = (byte)((color >> 16) & 0xFF); // R
                            ptr[offset + 3] = (byte)((color >> 24) & 0xFF); // A
                        }
                    }
                }
            }
        }

        bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
        bitmap.Unlock();
        bitmap.Freeze();

        return bitmap;
    }

    private void BtnCopyLog_Click(object sender, RoutedEventArgs e)
    {
        var lines = LogPanel.Children
            .OfType<TextBlock>()
            .Select(tb => tb.Text);

        var logText = string.Join(Environment.NewLine, lines);

        if (string.IsNullOrEmpty(logText))
        {
            MessageBox.Show("No log to copy.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Clipboard.SetText(logText);
            LogInfo("Log copied to clipboard");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to copy log: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogPanel.Children.Clear();
        LogInfo("Log cleared");
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void AppendLog(string message, Brush foreground)
    {
        var entry = new TextBlock
        {
            Text = $"> {message}",
            Foreground = foreground,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 2)
        };

        LogPanel.Children.Add(entry);
        LogScrollViewer.ScrollToBottom();
    }

    private void LogInfo(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)));
    private void LogSuccess(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)));
    private void LogWarning(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)));
    private void LogError(string message) => AppendLog(message, new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)));

    private string? SelectRomFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select ROM File",
            Filter = "All Supported ROMs|*.nes;*.sfc;*.smc;*.sms;*.gg;*.sg;*.col;*.gb;*.gbc;*.gba;*.ws;*.wsc;*.pce|" +
                     "NES ROMs (*.nes)|*.nes|" +
                     "SNES ROMs (*.sfc, *.smc)|*.sfc;*.smc|" +
                     "SMS/GG/SG-1000 ROMs (*.sms, *.gg, *.sg)|*.sms;*.gg;*.sg|" +
                     "ColecoVision ROMs (*.col)|*.col|" +
                     "Game Boy ROMs (*.gb, *.gbc)|*.gb;*.gbc|" +
                     "GBA ROMs (*.gba)|*.gba|" +
                     "WonderSwan ROMs (*.ws, *.wsc)|*.ws;*.wsc|" +
                     "PC Engine ROMs (*.pce)|*.pce|" +
                     "All Files (*.*)|*.*"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>
    /// Detects the console type from ROM file extension.
    /// This is independent of Retruxel targets - it only identifies the emulated console.
    /// </summary>
    private string DetectConsoleFromRom(string romPath)
    {
        var extension = Path.GetExtension(romPath).ToLowerInvariant();
        return extension switch
        {
            ".nes" => "nes",
            ".sfc" => "snes",
            ".smc" => "snes",
            ".sms" => "sms",
            ".gg" => "gg",
            ".sg" => "sg1000",
            ".col" => "coleco",
            ".gb" => "gb",
            ".gbc" => "gbc",
            ".gba" => "gba",
            ".ws" => "ws",
            ".wsc" => "wsc",
            ".pce" => "pce",
            _ => "unknown"
        };
    }

    private IEmulatorConnection? DetectEmulatorByRom(string romPath)
    {
        var extension = Path.GetExtension(romPath).ToLowerInvariant();

        return extension switch
        {
            ".nes" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".sfc" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".smc" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".sms" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".gg" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".sg" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".gb" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".gbc" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".gba" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".ws" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".wsc" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".pce" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "mesen"),
            ".col" => _availableEmulators.FirstOrDefault(e => e.EmulatorId == "emulicious"),
            _ => null
        };
    }

    /// <summary>
    /// Returns hardware specs for a given console based on real hardware specifications.
    /// These specs are independent of Retruxel target implementations.
    /// LiveLink always uses these specs to ensure accurate capture from emulator.
    /// </summary>
    private TargetSpecs? GetConsoleSpecs(string console)
    {
        return console switch
        {
            "nes" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 240,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 4, // 2bpp
                VramRegions = new[]
                {
                    new VramRegion("pattern0", "Pattern Table 0", 0, 255),
                    new VramRegion("pattern1", "Pattern Table 1", 256, 511)
                },
                MaxSpritesOnScreen = 64
            },
            "snes" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 224,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp (most common mode)
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 2047)
                },
                MaxSpritesOnScreen = 128
            },
            "sms" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 192,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("background", "Background", 0, 255),
                    new VramRegion("sprites", "Sprites", 256, 447)
                },
                MaxSpritesOnScreen = 64
            },
            "sg1000" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 192,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 2, // 1bpp (TMS9918)
                VramRegions = new[]
                {
                    new VramRegion("background", "Background", 0, 255),
                    new VramRegion("sprites", "Sprites", 256, 511)
                },
                MaxSpritesOnScreen = 32
            },
            "gg" => new TargetSpecs
            {
                ScreenWidth = 160,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("background", "Background", 0, 255),
                    new VramRegion("sprites", "Sprites", 256, 447)
                },
                MaxSpritesOnScreen = 64
            },
            "gb" or "gbc" => new TargetSpecs
            {
                ScreenWidth = 160,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 4, // 2bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 383)
                },
                MaxSpritesOnScreen = 40
            },
            "gba" => new TargetSpecs
            {
                ScreenWidth = 240,
                ScreenHeight = 160,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 256, // 8bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 1023)
                },
                MaxSpritesOnScreen = 128
            },
            "ws" or "wsc" => new TargetSpecs
            {
                ScreenWidth = 224,
                ScreenHeight = 144,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 511)
                },
                MaxSpritesOnScreen = 128
            },
            "pce" => new TargetSpecs
            {
                ScreenWidth = 256,
                ScreenHeight = 224,
                TileWidth = 8,
                TileHeight = 8,
                ColorsPerTile = 16, // 4bpp
                VramRegions = new[]
                {
                    new VramRegion("vram", "VRAM", 0, 2047)
                },
                MaxSpritesOnScreen = 64
            },
            _ => null
        };
    }

    private void RenderPreview(CaptureResult capture)
    {
        if (capture.Tiles == null || capture.Tiles.Length == 0)
        {
            ImgPreview.Source = null;
            return;
        }

        // If we have nametable AND palette, render the full scene (tilemap)
        if (capture.Nametable != null && capture.NametableWidth > 0 && capture.NametableHeight > 0 && capture.Palette != null)
        {
            var sceneBitmap = CreatePreviewBitmap(capture);
            if (sceneBitmap != null)
            {
                ImgPreview.Source = sceneBitmap;
                LogInfo("Preview: Rendered tilemap (nametable + tiles + palette)");
                return;
            }
        }

        // Otherwise render tiles in a grid (fallback when no nametable or no palette)
        LogInfo("Preview: Rendering tiles in grid (no nametable or no palette)");
        RenderTilesInGrid(capture);
    }

    private void RenderTilesInGrid(CaptureResult capture)
    {
        int tilesPerRow = 16;
        int tileSize = 8;
        int rows = (capture.Tiles.Length + tilesPerRow - 1) / tilesPerRow;

        int width = tilesPerRow * tileSize;
        int height = rows * tileSize;

        var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
            width, height, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null);

        bitmap.Lock();

        unsafe
        {
            byte* ptr = (byte*)bitmap.BackBuffer;
            int stride = bitmap.BackBufferStride;

            // For NES sprite tiles without nametable, cycle through sprite palettes (4-7)
            bool isNesSprites = _sourceConsole == "nes" && capture.Palette != null && capture.Palette.Length == 32;
            int nesPaletteOffset = isNesSprites ? 16 : 0; // Sprite palettes start at index 16

            for (int tileIdx = 0; tileIdx < capture.Tiles.Length; tileIdx++)
            {
                var tile = capture.Tiles[tileIdx];
                int tileX = (tileIdx % tilesPerRow) * tileSize;
                int tileY = (tileIdx / tilesPerRow) * tileSize;

                // Cycle through palettes for variety
                int paletteNum = (tileIdx / tilesPerRow) % 4; // Change palette every row

                for (int py = 0; py < tileSize; py++)
                {
                    for (int px = 0; px < tileSize; px++)
                    {
                        int pixelIdx = py * tileSize + px;
                        if (pixelIdx >= tile.Length)
                            continue;

                        byte colorIdx = tile[pixelIdx];

                        uint color;
                        if (capture.Palette != null)
                        {
                            int finalColorIdx = colorIdx;

                            if (isNesSprites)
                            {
                                // NES sprites: use sprite palettes (4-7)
                                finalColorIdx = nesPaletteOffset + (paletteNum * 4) + colorIdx;
                            }

                            if (finalColorIdx < capture.Palette.Length)
                            {
                                color = capture.Palette[finalColorIdx];
                            }
                            else
                            {
                                // Fallback to grayscale
                                byte gray = (byte)(colorIdx * 85);
                                color = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;
                            }
                        }
                        else
                        {
                            // Fallback to grayscale
                            byte gray = (byte)(colorIdx * 85);
                            color = 0xFF000000u | ((uint)gray << 16) | ((uint)gray << 8) | gray;
                        }

                        int x = tileX + px;
                        int y = tileY + py;
                        int offset = y * stride + x * 4;

                        ptr[offset + 0] = (byte)((color >> 0) & 0xFF);  // B
                        ptr[offset + 1] = (byte)((color >> 8) & 0xFF);  // G
                        ptr[offset + 2] = (byte)((color >> 16) & 0xFF); // R
                        ptr[offset + 3] = (byte)((color >> 24) & 0xFF); // A
                    }
                }
            }
        }

        bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
        bitmap.Unlock();
        bitmap.Freeze();

        ImgPreview.Source = bitmap;
    }

    private async void BtnCaptureScreen_Click(object sender, RoutedEventArgs e)
    {
        if (_connection == null || !_connection.IsConnected)
        {
            LogError("Not connected to emulator");
            MessageBox.Show("Not connected to emulator.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_connection is not MesenConnection mesenConn)
        {
            LogError("Screen capture only supported for Mesen 2");
            MessageBox.Show("Screen capture is currently only supported for Mesen 2.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        BtnCaptureVRAM.IsEnabled = false;
        BtnCaptureScreen.IsEnabled = false;
        BtnExpandCanvas.IsEnabled = false;

        try
        {
            LogInfo("Starting screen capture...");

            LogInfo("Requesting screen buffer...");
            byte[] screenBuffer = await mesenConn.GetScreenBufferAsync();
            LogSuccess($"✓ Received screen buffer: {screenBuffer.Length} bytes");

            // Get console specs for screen dimensions
            var specs = GetConsoleSpecs(_sourceConsole!);
            if (specs == null)
            {
                LogError($"No specs available for console: {_sourceConsole}");
                return;
            }

            int width = specs.ScreenWidth;
            int height = specs.ScreenHeight;
            int expectedSize = width * height * 4; // RGBA

            LogInfo($"Screen dimensions: {width}x{height} (expected {expectedSize} bytes)");

            if (screenBuffer.Length != expectedSize)
            {
                LogWarning($"⚠ Buffer size mismatch: got {screenBuffer.Length}, expected {expectedSize}");
            }

            // Convert screen buffer to bitmap for preview
            var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                width, height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null);

            bitmap.Lock();

            unsafe
            {
                byte* ptr = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIdx = (y * width + x) * 4;
                        int dstIdx = y * stride + x * 4;

                        if (srcIdx + 3 < screenBuffer.Length)
                        {
                            ptr[dstIdx + 0] = screenBuffer[srcIdx + 2]; // B
                            ptr[dstIdx + 1] = screenBuffer[srcIdx + 1]; // G
                            ptr[dstIdx + 2] = screenBuffer[srcIdx + 0]; // R
                            ptr[dstIdx + 3] = screenBuffer[srcIdx + 3]; // A
                        }
                    }
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
            bitmap.Freeze();

            ImgPreview.Source = bitmap;
            LogSuccess("✓ Screen capture complete!");

            // Convert screen to tiles + palette + nametable
            LogInfo("Converting screen to tiles...");

            // Get destination target from project (not source console)
            string? destinationTarget = null;
            if (_input?.TryGetValue("targetId", out var targetObj) == true)
            {
                destinationTarget = targetObj?.ToString();
            }

            var conversion = ScreenToTilesConverter.Convert(
                screenBuffer,
                width,
                height,
                destinationTarget ?? _sourceConsole!);

            LogSuccess($"✓ Converted to {conversion.Tiles.Length} tiles, {conversion.Palette.Length} colors");
            LogInfo($"Nametable: {conversion.NametableWidth}×{conversion.NametableHeight}");

            // Log color distribution
            var colorGroups = conversion.Palette.GroupBy(c => c).OrderByDescending(g => g.Count());
            LogInfo($"Color distribution: {string.Join(", ", colorGroups.Take(5).Select(g => $"#{g.Key:X6} ({g.Count()}x)"))}");

            // Check tile limit for DESTINATION target (not source console)
            if (!string.IsNullOrEmpty(destinationTarget))
            {
                var targetSpecs = GetConsoleSpecs(destinationTarget);
                if (targetSpecs != null)
                {
                    int maxTiles = targetSpecs.MaxTilesInVram;
                    if (conversion.Tiles.Length > maxTiles)
                    {
                        LogWarning($"⚠ Generated {conversion.Tiles.Length} tiles, but target {destinationTarget.ToUpper()} supports max {maxTiles} tiles");
                        LogWarning($"⚠ Use Tile Optimizer tool to deduplicate and reduce tile count");
                    }
                    else
                    {
                        LogInfo($"✓ Tile count ({conversion.Tiles.Length}) is within {destinationTarget.ToUpper()} limit ({maxTiles})");
                    }
                }
            }
            else
            {
                LogInfo($"No destination target specified - skipping tile limit check");
            }

            // Store in capture result
            _lastCapture = new CaptureResult
            {
                Tiles = conversion.Tiles,
                Palette = conversion.Palette,
                Nametable = conversion.Nametable,
                NametableWidth = conversion.NametableWidth,
                NametableHeight = conversion.NametableHeight,
                TileWidth = 8,
                TileHeight = 8,
                TargetId = _sourceConsole!,
                Metadata = new Dictionary<string, object>
                {
                    ["source"] = "screen_capture",
                    ["tilePaletteAssignments"] = conversion.TilePaletteAssignments
                }
            };

            BtnImport.IsEnabled = true;
            BtnExpandCanvas.IsEnabled = true;
            LogSuccess("✓ Ready to import!");
        }
        catch (Exception ex)
        {
            LogError($"Screen capture failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[LiveLink] Screen capture error: {ex}");
        }
        finally
        {
            BtnCaptureVRAM.IsEnabled = true;
            BtnCaptureScreen.IsEnabled = true;
        }
    }

    private void BtnValidateSmsColors_Click(object sender, RoutedEventArgs e)
    {
        var previewBitmap = ImgPreview.Source as BitmapSource;
        if (previewBitmap == null)
        {
            LogError("No preview image available");
            MessageBox.Show("No preview image available. Capture a screen first.", "No Image", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            LogInfo("Validating SMS colors...");

            var result = SmsColorValidator.ValidateImage(previewBitmap);

            if (result.IsValid)
            {
                LogSuccess($"✓ Image contains ONLY Master System colors ({result.TotalColors} unique colors)");
                MessageBox.Show(
                    $"✓ Valid SMS Image\n\n" +
                    $"All {result.TotalColors} colors are from the Master System palette (64 colors, 6-bit RGB).\n\n" +
                    $"This image can be used directly without color conversion.",
                    "SMS Color Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                LogWarning($"⚠ Image contains {result.InvalidColors} non-SMS colors (total: {result.TotalColors} unique colors)");

                // Show first 10 invalid colors
                var invalidSample = string.Join(", ", result.InvalidColorList.Take(10).Select(c => $"RGB({c.R},{c.G},{c.B})"));
                if (result.InvalidColorList.Count > 10)
                    invalidSample += $"... and {result.InvalidColorList.Count - 10} more";

                LogInfo($"Invalid colors: {invalidSample}");

                MessageBox.Show(
                    $"⚠ Invalid SMS Colors\n\n" +
                    $"Found {result.InvalidColors} colors that are NOT in the Master System palette.\n" +
                    $"Total unique colors: {result.TotalColors}\n\n" +
                    $"Master System uses 6-bit RGB (64 colors total).\n" +
                    $"Valid RGB values: 0, 85, 170, 255 for each channel.\n\n" +
                    $"Examples of invalid colors:\n{invalidSample}\n\n" +
                    $"Use palette optimization to convert to SMS colors.",
                    "SMS Color Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            LogError($"Validation failed: {ex.Message}");
            MessageBox.Show($"Validation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}