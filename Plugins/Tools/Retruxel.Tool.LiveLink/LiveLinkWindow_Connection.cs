using Retruxel.Core.Interfaces;
using Retruxel.Core.Services;
using Retruxel.Tool.LiveLink.Emulators;
using Retruxel.Tool.LiveLink.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// Connection management: emulator discovery, connection, keep-alive, ROM selection.
/// </summary>
public partial class LiveLinkWindow
{
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

        Debug.WriteLine("[LiveLink] Keep-alive started (5s interval)");
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
            bool reconnected = await _connection.ConnectAsync(_lastHost ?? "127.0.0.1", _lastPort);

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
}
