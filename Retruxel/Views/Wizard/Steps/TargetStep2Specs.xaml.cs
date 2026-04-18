using System.Windows.Controls;
using Retruxel.Core.Models.Wizard;
using Retruxel.Core.Models;

namespace Retruxel.Views.Wizard.Steps;

public partial class TargetStep2Specs : UserControl
{
    private readonly TargetWizardData _data;

    public TargetStep2Specs(TargetWizardData data)
    {
        InitializeComponent();
        _data = data;
        LoadData();
        AttachHandlers();
    }

    public void AutoFillFromConsole(ConsoleSpec? console)
    {
        if (console == null) return;

        CpuTextBox.Text = console.CPU;
        CpuClockTextBox.Text = console.CpuClockHz.ToString();
        RamTextBox.Text = console.RamBytes.ToString();
        VramTextBox.Text = console.VramBytes.ToString();
        ScreenWidthTextBox.Text = console.ScreenWidth.ToString();
        ScreenHeightTextBox.Text = console.ScreenHeight.ToString();
        TileWidthTextBox.Text = console.TileWidth.ToString();
        TileHeightTextBox.Text = console.TileHeight.ToString();
        TotalColorsTextBox.Text = console.TotalColors.ToString();
        BitsPerChannelTextBox.Text = console.ColorDepthBitsPerChannel.ToString();
        ColorsPerPaletteTextBox.Text = console.ColorsPerPalette.ToString();
        MaxSpritesTextBox.Text = console.MaxSpritesOnScreen.ToString();
        SpritesPerScanlineTextBox.Text = console.SpritesPerScanline.ToString();
        MaxSpritesOnScreenTextBox.Text = console.MaxSpritesOnScreen.ToString();
        SoundChipTextBox.Text = console.SoundChip;
        ToneChannelsTextBox.Text = console.SoundToneChannels.ToString();
        NoiseChannelsTextBox.Text = console.SoundNoiseChannels.ToString();
    }

    private void LoadData()
    {
        CpuTextBox.Text = _data.CPU;
        CpuClockTextBox.Text = _data.CpuClockHz.ToString();
        RamTextBox.Text = _data.RamBytes.ToString();
        VramTextBox.Text = _data.VramBytes.ToString();
        ScreenWidthTextBox.Text = _data.ScreenWidth.ToString();
        ScreenHeightTextBox.Text = _data.ScreenHeight.ToString();
        TileWidthTextBox.Text = _data.TileWidth.ToString();
        TileHeightTextBox.Text = _data.TileHeight.ToString();
        TotalColorsTextBox.Text = _data.TotalColors.ToString();
        BitsPerChannelTextBox.Text = _data.ColorDepthBitsPerChannel.ToString();
        ColorsPerPaletteTextBox.Text = _data.ColorsPerPalette.ToString();
        MaxSpritesTextBox.Text = _data.MaxSpritesOnScreen.ToString();
        SpritesPerScanlineTextBox.Text = _data.SpritesPerScanline.ToString();
        MaxSpritesOnScreenTextBox.Text = _data.MaxSpritesOnScreen.ToString();
        SoundChipTextBox.Text = _data.SoundChip;
        ToneChannelsTextBox.Text = _data.SoundToneChannels.ToString();
        NoiseChannelsTextBox.Text = _data.SoundNoiseChannels.ToString();
    }

    private void AttachHandlers()
    {
        CpuTextBox.TextChanged += (s, e) => _data.CPU = CpuTextBox.Text.Trim();
        CpuClockTextBox.TextChanged += (s, e) => 
        {
            if (long.TryParse(CpuClockTextBox.Text, out long clock))
                _data.CpuClockHz = clock;
        };
        RamTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(RamTextBox.Text, out int ram))
                _data.RamBytes = ram;
        };
        VramTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(VramTextBox.Text, out int vram))
                _data.VramBytes = vram;
        };
        ScreenWidthTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(ScreenWidthTextBox.Text, out int width))
                _data.ScreenWidth = width;
        };
        ScreenHeightTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(ScreenHeightTextBox.Text, out int height))
                _data.ScreenHeight = height;
        };
        TileWidthTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(TileWidthTextBox.Text, out int width))
                _data.TileWidth = width;
        };
        TileHeightTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(TileHeightTextBox.Text, out int height))
                _data.TileHeight = height;
        };
        TotalColorsTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(TotalColorsTextBox.Text, out int colors))
                _data.TotalColors = colors;
        };
        BitsPerChannelTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(BitsPerChannelTextBox.Text, out int bits))
                _data.ColorDepthBitsPerChannel = bits;
        };
        ColorsPerPaletteTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(ColorsPerPaletteTextBox.Text, out int colors))
                _data.ColorsPerPalette = colors;
        };
        MaxSpritesTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(MaxSpritesTextBox.Text, out int sprites))
                _data.MaxSpritesOnScreen = sprites;
        };
        SpritesPerScanlineTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(SpritesPerScanlineTextBox.Text, out int sprites))
                _data.SpritesPerScanline = sprites;
        };
        MaxSpritesOnScreenTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(MaxSpritesOnScreenTextBox.Text, out int sprites))
                _data.MaxSpritesOnScreen = sprites;
        };
        SoundChipTextBox.TextChanged += (s, e) => _data.SoundChip = SoundChipTextBox.Text.Trim();
        ToneChannelsTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(ToneChannelsTextBox.Text, out int channels))
                _data.SoundToneChannels = channels;
        };
        NoiseChannelsTextBox.TextChanged += (s, e) => 
        {
            if (int.TryParse(NoiseChannelsTextBox.Text, out int channels))
                _data.SoundNoiseChannels = channels;
        };
    }
}
