using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Retruxel.Core.Services;

namespace Retruxel.Tool.PaletteEditor;

public partial class PaletteEditorWindow : Window
{
    private int _selectedSlotIndex = -1;
    private readonly byte[] _currentPalette = new byte[16];
    private readonly LocalizationService _loc = LocalizationService.Instance;
    
    private static readonly byte[] SmsColorLevels = { 0, 85, 170, 255 };
    private static readonly Color[] SmsHardwareColors = GenerateSmsColors();

    public Dictionary<string, object>? ModuleData { get; private set; }

    public PaletteEditorWindow()
    {
        InitializeComponent();
        ApplyLocalization();
        InitializeUI();
        LoadDefaultPalette();
        
        LocalizationService.LanguageChanged += ApplyLocalization;
    }

    private void ApplyLocalization()
    {
        Title = $"RETRUXEL · {_loc.Get("paletteeditor.title")}";
        TxtPalettes.Text = _loc.Get("paletteeditor.palettes");
        BtnNewPalette.Content = _loc.Get("paletteeditor.new_palette");
        BtnDuplicate.Content = _loc.Get("paletteeditor.duplicate");
        BtnDelete.Content = _loc.Get("paletteeditor.delete");
        TxtSlots.Text = _loc.Get("paletteeditor.slots");
        TxtHardwareColors.Text = _loc.Get("paletteeditor.hardware_colors");
        TxtSlotDetail.Text = _loc.Get("paletteeditor.slot_detail");
        TxtQuickSet.Text = _loc.Get("paletteeditor.quick_set");
        BtnSetBlack.Content = _loc.Get("paletteeditor.black");
        BtnSetWhite.Content = _loc.Get("paletteeditor.white");
        BtnSetTransparent.Content = _loc.Get("paletteeditor.transparent");
        TxtUsage.Text = _loc.Get("paletteeditor.usage");
        
        if (_selectedSlotIndex >= 0)
            SelectSlot(_selectedSlotIndex);
    }

    private void InitializeUI()
    {
        for (int i = 0; i < 16; i++)
        {
            var slotIndex = i;
            var btn = new Button
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(2),
                BorderThickness = new Thickness(0),
                Tag = slotIndex
            };
            btn.Click += (s, e) => SelectSlot(slotIndex);
            SlotsGrid.Children.Add(btn);
        }

        for (int i = 0; i < 64; i++)
        {
            var colorIndex = i;
            var btn = new Button
            {
                Width = 48,
                Height = 48,
                Margin = new Thickness(1),
                Background = new SolidColorBrush(SmsHardwareColors[i]),
                BorderThickness = new Thickness(0),
                Tag = colorIndex
            };
            btn.Click += (s, e) => SetSlotColor(colorIndex);
            HardwareColorGrid.Children.Add(btn);
        }

        BtnNewPalette.Click += (s, e) => CreateNewPalette();
        BtnDuplicate.Click += (s, e) => DuplicatePalette();
        BtnDelete.Click += (s, e) => DeletePalette();
        
        BtnSetBlack.Click += (s, e) => SetSlotColor(0);
        BtnSetWhite.Click += (s, e) => SetSlotColor(63);
        BtnSetTransparent.Click += (s, e) => SetSlotColor(0);
    }

    private void LoadDefaultPalette()
    {
        for (int i = 0; i < 16; i++)
        {
            _currentPalette[i] = (byte)(i * 4);
        }
        
        RefreshSlots();
        SelectSlot(0);
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < 16; i++)
        {
            if (SlotsGrid.Children[i] is Button btn)
            {
                var smsColor = SmsHardwareColors[_currentPalette[i]];
                btn.Background = new SolidColorBrush(smsColor);
            }
        }
    }

    private void SelectSlot(int index)
    {
        _selectedSlotIndex = index;
        
        for (int i = 0; i < 16; i++)
        {
            if (SlotsGrid.Children[i] is Button btn)
            {
                if (i == index)
                {
                    btn.BorderThickness = new Thickness(2);
                    btn.BorderBrush = (Brush)FindResource("BrushPrimary");
                }
                else
                {
                    btn.BorderThickness = new Thickness(0);
                }
            }
        }
        
        var smsColorIndex = _currentPalette[index];
        var color = SmsHardwareColors[smsColorIndex];
        
        TxtSlotIndex.Text = string.Format(_loc.Get("paletteeditor.slot_format"), index.ToString("D2"));
        TxtSlotHex.Text = string.Format(_loc.Get("paletteeditor.sms_format"), smsColorIndex.ToString("X2"));
        TxtSlotRgb.Text = string.Format(_loc.Get("paletteeditor.rgb_format"), color.R, color.G, color.B);
        ColorPreview.Background = new SolidColorBrush(color);
        
        UpdateUsageReport(index);
    }

    private void SetSlotColor(int smsColorIndex)
    {
        if (_selectedSlotIndex < 0) return;
        
        _currentPalette[_selectedSlotIndex] = (byte)smsColorIndex;
        RefreshSlots();
        SelectSlot(_selectedSlotIndex);
    }

    private void UpdateUsageReport(int slotIndex)
    {
        UsagePanel.Children.Clear();
        
        var txt = new TextBlock
        {
            Text = _loc.Get("paletteeditor.no_usage"),
            Style = (Style)FindResource("TextBody"),
            Foreground = (Brush)FindResource("BrushTextSecondary")
        };
        UsagePanel.Children.Add(txt);
    }

    private void CreateNewPalette()
    {
        MessageBox.Show("Create new palette", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DuplicatePalette()
    {
        MessageBox.Show("Duplicate palette", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeletePalette()
    {
        MessageBox.Show("Delete palette", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static Color[] GenerateSmsColors()
    {
        var colors = new Color[64];
        int index = 0;
        
        for (int b = 0; b < 4; b++)
        {
            for (int g = 0; g < 4; g++)
            {
                for (int r = 0; r < 4; r++)
                {
                    colors[index++] = Color.FromRgb(
                        SmsColorLevels[r],
                        SmsColorLevels[g],
                        SmsColorLevels[b]
                    );
                }
            }
        }
        
        return colors;
    }

    protected override void OnClosed(EventArgs e)
    {
        LocalizationService.LanguageChanged -= ApplyLocalization;
        base.OnClosed(e);
    }
}
