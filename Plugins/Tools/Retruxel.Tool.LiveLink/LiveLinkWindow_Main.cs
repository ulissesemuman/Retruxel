using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Tool.LiveLink.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Retruxel.Tool.LiveLink;

/// <summary>
/// LiveLink window - main class with initialization and state management.
/// Functionality split across partial classes:
/// - LiveLinkWindow_Connection.cs: Connection management
/// - LiveLinkWindow_Capture.cs: VRAM and screen capture
/// - LiveLinkWindow_Palette.cs: Palette decoding
/// - LiveLinkWindow_Preview.cs: Preview rendering
/// - LiveLinkWindow_Import.cs: Import and conversion
/// - LiveLinkWindow_UI.cs: UI management
/// - LiveLinkWindow_Specs.cs: Console specifications
/// </summary>
public partial class LiveLinkWindow : Window
{
    // Connection state
    private IEmulatorConnection? _connection;
    private readonly List<IEmulatorConnection> _availableEmulators = new();
    private static Process? _emulatorProcess;
    private static string? _lastEmulatorId;
    private static string? _lastRomPath;
    private string? _sourceConsole; // Console do emulador (nes, snes, sms, etc.)

    // Keep-alive system
    private DispatcherTimer? _keepAliveTimer;
    private bool _isReconnecting = false;
    private string? _lastHost = "127.0.0.1";
    private int _lastPort = 8888;

    // Capture state
    private CaptureResult? _lastCapture;

    // Settings and input
    private readonly Dictionary<string, object>? _input;
    private readonly bool _captureMode;
    private readonly string? _callerId;
    private AppSettings? _settings;

    // Public properties
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
}
