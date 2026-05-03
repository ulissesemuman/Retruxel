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
    
    /// <summary>
    /// Lightweight ping to check if connection is alive.
    /// Returns true if connection is responsive, false otherwise.
    /// </summary>
    public async Task<bool> PingAsync()
    {
        if (_writer == null || _reader == null)
            return false;
        
        try
        {
            await _writer.WriteLineAsync("PING");
            
            var cts = new CancellationTokenSource(2000); // 2s timeout
            var response = await _reader.ReadLineAsync(cts.Token);
            
            return response?.Contains("PONG") == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]> GetScreenBufferAsync()
    {
        if (_writer == null || _reader == null || _stream == null)
            throw new InvalidOperationException("Not connected");

        Log("Sending: GET_SCREEN");
        await _writer.WriteLineAsync("GET_SCREEN");
        await _writer.FlushAsync();
        Log("Waiting for screen buffer size...");

        // Read size first
        var sizeLine = await _reader.ReadLineAsync();
        if (sizeLine == null || sizeLine == "ERROR")
            throw new IOException("Failed to get screen buffer");
        
        if (!int.TryParse(sizeLine, out int dataSize))
            throw new IOException($"Invalid size received: {sizeLine}");
        
        Log($"Expecting {dataSize} bytes of base64 data");
        
        // Read data in chunks to avoid blocking
        List<byte> dataBuffer = new List<byte>(dataSize + 1);
        byte[] readBuffer = new byte[8192];
        int totalRead = 0;
        int targetSize = dataSize + 1; // +1 for \n
        while (totalRead < targetSize)
        {
            int toRead = Math.Min(readBuffer.Length, targetSize - totalRead);
            int bytesRead = await _stream.ReadAsync(readBuffer, 0, toRead);
            
            if (bytesRead == 0)
                throw new IOException("Connection closed while reading screen buffer");
            
            dataBuffer.AddRange(readBuffer.Take(bytesRead));
            totalRead += bytesRead;
            
            if (totalRead % 65536 == 0) // Log every 64KB
                Log($"Read {totalRead}/{targetSize} bytes...");
        }
        
        Log($"Read complete: {totalRead} bytes from stream");
        
        // Convert to string (excluding the \n at the end)
        byte[] allData = dataBuffer.ToArray();
        string base64Data = System.Text.Encoding.ASCII.GetString(allData, 0, dataSize);
        Log($"Decoding {base64Data.Length} chars of base64...");
        
        var decoded = Convert.FromBase64String(base64Data);
        Log($"Decoded to {decoded.Length} bytes");
        
        // Debug: Log first pixel of lines 7 and 8
        // Line 7 starts at pixel (0, 56) = offset 57344 (256 * 56 * 4)
        // Line 8 starts at pixel (0, 64) = offset 65536 (256 * 64 * 4)
        if (decoded.Length >= 65540)
        {
            Log($"Line 7 first pixel (offset 57344): R={decoded[57344]}, G={decoded[57345]}, B={decoded[57346]}, A={decoded[57347]}");
            Log($"Line 8 first pixel (offset 65536): R={decoded[65536]}, G={decoded[65537]}, B={decoded[65538]}, A={decoded[65539]}");
        }

        return decoded;
    }
}
