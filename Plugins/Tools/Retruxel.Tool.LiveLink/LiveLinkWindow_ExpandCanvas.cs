using Retruxel.Tool.LiveLink.Emulators;
using Retruxel.Tool.LiveLink.Services;
using System.IO;
using System.Windows;

namespace Retruxel.Tool.LiveLink;

public partial class LiveLinkWindow
{
    private async void BtnExpandCanvas_Click(object sender, RoutedEventArgs e)
    {
        if (_lastCapture == null || _lastCapture.Nametable == null)
        {
            LogError("No nametable available for expansion");
            MessageBox.Show("No nametable captured. Use CAPTURE VRAM first.", "No Nametable", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_connection == null || !_connection.IsConnected)
        {
            LogError("Not connected to emulator");
            MessageBox.Show("Not connected to emulator. Canvas expansion requires ROM/RAM access.", "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            LogInfo("Preparing Canvas Expansion...");

            // Convert nametable to byte array
            byte[] nametableBytes = new byte[_lastCapture.Nametable.Length * 2];
            for (int i = 0; i < _lastCapture.Nametable.Length; i++)
            {
                nametableBytes[i * 2] = (byte)(_lastCapture.Nametable[i] & 0xFF);
                nametableBytes[i * 2 + 1] = (byte)((_lastCapture.Nametable[i] >> 8) & 0xFF);
            }

            LogInfo($"Nametable: {_lastCapture.Nametable.Length} tiles = {nametableBytes.Length} bytes");
            
            // Debug: Log first 32 bytes of nametable
            var nametablePreview = string.Join(" ", nametableBytes.Take(32).Select(b => $"{b:X2}"));
            LogInfo($"Nametable preview: {nametablePreview}");
            
            // Debug: Count unique tiles in nametable
            var uniqueTiles = _lastCapture.Nametable.Distinct().Count();
            LogInfo($"Unique tiles in nametable: {uniqueTiles}/{_lastCapture.Nametable.Length}");

            // Try to read ROM data from file
            byte[]? romData = null;
            if (!string.IsNullOrEmpty(_lastRomPath) && File.Exists(_lastRomPath))
            {
                LogInfo($"Reading ROM file: {Path.GetFileName(_lastRomPath)}");
                
                try
                {
                    // Read and immediately release the file
                    using (var fileStream = new FileStream(_lastRomPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        romData = new byte[fileStream.Length];
                        await fileStream.ReadAsync(romData, 0, (int)fileStream.Length);
                    }
                    
                    LogSuccess($"✓ Read {romData.Length} bytes from ROM file");
                    
                    // Debug: Log first 32 bytes of ROM
                    var romPreview = string.Join(" ", romData.Take(32).Select(b => $"{b:X2}"));
                    LogInfo($"ROM preview: {romPreview}");

                    // For NES, skip iNES header (16 bytes)
                    if (_sourceConsole == "nes" && romData.Length > 16)
                    {
                        var header = romData.Take(4).ToArray();
                        if (header[0] == 0x4E && header[1] == 0x45 && header[2] == 0x53 && header[3] == 0x1A)
                        {
                            LogInfo("Detected iNES header, skipping first 16 bytes");
                            romData = romData.Skip(16).ToArray();
                            LogInfo($"ROM size after header removal: {romData.Length} bytes");
                        }
                        else
                        {
                            LogInfo("No iNES header detected");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"Failed to read ROM file: {ex.Message}");
                    romData = null;
                }
            }
            else
            {
                LogWarning("ROM file not available");
            }

            // Try to read RAM data from emulator
            byte[]? ramData = null;
            if (_connection is MesenConnection mesenConn)
            {
                LogInfo("Reading RAM from emulator...");
                ramData = await mesenConn.ReadMemoryAsync(0x0000, 0x10000);
                LogSuccess($"✓ Read {ramData.Length} bytes from RAM");
            }
            else
            {
                LogWarning("RAM reading only supported for Mesen 2");
            }

            if (romData == null && ramData == null)
            {
                LogError("No ROM or RAM data available");
                MessageBox.Show("Cannot expand canvas: ROM file not found and RAM reading not available.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_sourceConsole))
            {
                LogError("Source console not detected");
                MessageBox.Show("Cannot expand canvas: Source console not detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var expansionWindow = new Windows.CanvasExpansionWindow(nametableBytes, romData, ramData, _sourceConsole);
            expansionWindow.Owner = this;

            if (expansionWindow.ShowDialog() != true)
            {
                LogInfo("Canvas expansion cancelled");
                return;
            }

            var result = expansionWindow.Result;
            if (result == null || !result.Success)
            {
                LogError("Canvas expansion failed");
                return;
            }

            LogSuccess($"✓ Canvas expanded to {result.ExpandedWidth}×{result.ExpandedHeight} (confidence: {result.Confidence:P0})");

            // Convert expanded nametable back to ushort[]
            var expandedNametable = new ushort[result.ExpandedNametable!.Length / 2];
            for (int i = 0; i < expandedNametable.Length; i++)
            {
                expandedNametable[i] = (ushort)(result.ExpandedNametable[i * 2] | (result.ExpandedNametable[i * 2 + 1] << 8));
            }

            // Update capture with expanded nametable
            _lastCapture = new CaptureResult
            {
                Tiles = _lastCapture.Tiles,
                Palette = _lastCapture.Palette,
                Nametable = expandedNametable,
                NametableWidth = result.ExpandedWidth,
                NametableHeight = result.ExpandedHeight,
                TileWidth = _lastCapture.TileWidth,
                TileHeight = _lastCapture.TileHeight,
                TargetId = _lastCapture.TargetId,
                Metadata = new Dictionary<string, object>(_lastCapture.Metadata)
                {
                    ["expandedCanvas"] = true,
                    ["expansionConfidence"] = result.Confidence
                }
            };

            // Render updated preview
            RenderPreview(_lastCapture);

            LogSuccess("✓ Preview updated with expanded canvas");
        }
        catch (Exception ex)
        {
            LogError($"Canvas expansion error: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
