using Retruxel.Core.Interfaces;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Retruxel.Tool.LiveLink.Emulators;

/// <summary>
/// Mesen emulator connection via debug API.
/// Supports SMS, Game Gear, SG-1000, and NES.
/// </summary>
public class MesenConnection : IEmulatorConnection
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private Action<string>? _logCallback;

    public string EmulatorId => "mesen";
    public string DisplayName => "Mesen 2";
    public string[] SupportedTargets => new[] { "sms", "gg", "sg1000", "nes", "snes" };
    public bool IsConnected => _client?.Connected == true;
    
    public void SetLogCallback(Action<string> callback)
    {
        _logCallback = callback;
    }
    
    private void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[MesenConnection] {message}");
        _logCallback?.Invoke(message);
    }

    public async Task<bool> ConnectAsync(string host = "127.0.0.1", int port = 8888)
    {
        try
        {
            Log($"Connecting to {host}:{port}...");
            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            _reader = new StreamReader(_stream, new UTF8Encoding(false), leaveOpen: true); // No BOM
            _writer = new StreamWriter(_stream, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true }; // No BOM
            Log("TCP connection established");

            Log("Sending: HELLO FROM RETRUXEL");
            await _writer.WriteLineAsync("HELLO FROM RETRUXEL");
            Log("Message sent, waiting for response...");

            var cts = new CancellationTokenSource(5000);
            
            try
            {
                var response = await _reader.ReadLineAsync(cts.Token);
                Log($"Received: {response}");
                return response?.Contains("OK") == true;
            }
            catch (OperationCanceledException)
            {
                Log("Timeout waiting for response (5s)");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log($"Connection error: {ex.Message}");
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
            _reader = null;
            _writer = null;
            _stream = null;
            _client = null;
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _reader?.Dispose();
        _reader = null;
        
        _writer?.Dispose();
        _writer = null;
        
        if (_stream != null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        _client?.Dispose();
        _client = null;
    }

    public async Task<byte[]> ReadMemoryAsync(uint address, int length)
    {
        if (_writer == null || _reader == null)
            throw new InvalidOperationException("Not connected");

        await _writer.WriteLineAsync($"READ_MEM:{address}:{length}");
        var response = await _reader.ReadLineAsync();
        return Convert.FromBase64String(response ?? "");
    }
    
    public async Task<byte[]> ReadCramAsync(int length)
    {
        if (_writer == null || _reader == null)
            throw new InvalidOperationException("Not connected");

        Log($"Sending: READ_CRAM:{length}");
        await _writer.WriteLineAsync($"READ_CRAM:{length}");
        Log("Waiting for CRAM data...");
        
        var response = await _reader.ReadLineAsync();
        if (response == null)
            throw new IOException("No response received");
        
        Log($"Received {response.Length} chars (base64)");
        var decoded = Convert.FromBase64String(response);
        Log($"Decoded to {decoded.Length} bytes");
        
        return decoded;
    }

    public async Task<byte[]> ReadVramAsync(uint address, int length)
    {
        if (_writer == null || _reader == null)
            throw new InvalidOperationException("Not connected");

        Log($"Sending: READ_VRAM:{address}:{length}");
        await _writer.WriteLineAsync($"READ_VRAM:{address}:{length}");
        Log("Waiting for VRAM data...");

        var response = await _reader.ReadLineAsync();
        if (response == null)
            throw new IOException("No response received");
        
        Log($"Received {response.Length} chars (base64)");
        var decoded = Convert.FromBase64String(response);
        Log($"Decoded to {decoded.Length} bytes");

        return decoded;
    }

    public async Task<EmulatorState> GetStateAsync()
    {
        // Not implemented for simple text protocol
        return new EmulatorState
        {
            IsPaused = false,
            IsRunning = true,
            LoadedRom = "Unknown"
        };
    }
}
