using Retruxel.Core.Interfaces;
using Retruxel.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.PaletteEditor;

public partial class PaletteEditorWindow : Window
{
    private int _selectedSlotIndex = -1;
    private byte[] _currentPalette;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly IPaletteProvider _paletteProvider;
    private readonly Color[] _hardwareColors;

    public Dictionary<string, object>? ModuleData { get; private set; }
    public string? SelectedConnectorId { get; private set; }
    
    private readonly Core.Models.RetruxelProject? _project;
    private readonly string? _paletteElementId;

    public PaletteEditorWindow(IPaletteProvider paletteProvider, string? callerToolId = null, byte[]? initialColors = null, string? paletteName = null, Core.Models.RetruxelProject? project = null, string? paletteElementId = null)
    {
        _paletteProvider = paletteProvider;
        _currentPalette = initialColors ?? new byte[paletteProvider.SlotCount];
        _hardwareColors = ConvertToWpfColors(paletteProvider.HardwareColors);
        _project = project;
        _paletteElementId = paletteElementId;
        
        // Debug: Log what we received
        System.Diagnostics.Debug.WriteLine($"[PaletteEditor] Received initialColors: {(initialColors == null ? "null" : $"[{string.Join(",", initialColors)}]")} (length: {initialColors?.Length ?? 0})");
        System.Diagnostics.Debug.WriteLine($"[PaletteEditor] Palette name: {paletteName ?? "null"}");
        
        SelectedConnectorId = callerToolId == "tilemap_editor" 
            ? "palette_to_tilemap" 
            : "palette_to_module";

        InitializeComponent();
        ApplyLocalization();
        
        if (!string.IsNullOrEmpty(paletteName))
            TxtPaletteName.Text = paletteName;
        
        InitializeUI();
        
        if (initialColors == null)
        {
            System.Diagnostics.Debug.WriteLine("[PaletteEditor] Loading default palette");
            LoadDefaultPalette();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[PaletteEditor] Using provided colors");
            RefreshSlots();
            SelectSlot(0);
        }

        LocalizationService.LanguageChanged += ApplyLocalization;
    }

    private static Color[] ConvertToWpfColors(object[] colors)
    {
        var wpfColors = new Color[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] is Retruxel.Core.Models.HardwareColor hwColor)
            {
                wpfColors[i] = Color.FromRgb(hwColor.R, hwColor.G, hwColor.B);
            }
        }
        return wpfColors;
    }

    private void ApplyLocalization()
    {
        Title = $"RETRUXEL · {_loc.Get("paletteeditor.title")}";
        TxtPalettes.Text = _loc.Get("paletteeditor.palettes");
        TxtPaletteName.Text = string.IsNullOrEmpty(TxtPaletteName.Text) ? "Palette 1" : TxtPaletteName.Text;
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
        BtnApply.Content = _loc.Get("common.apply");
        BtnCancel.Content = _loc.Get("common.cancel");

        if (_selectedSlotIndex >= 0)
            SelectSlot(_selectedSlotIndex);
    }

    private void InitializeUI()
    {
        for (int i = 0; i < _paletteProvider.SlotCount; i++)
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

        for (int i = 0; i < _hardwareColors.Length; i++)
        {
            var colorIndex = i;
            var btn = new Button
            {
                Width = 48,
                Height = 48,
                Margin = new Thickness(1),
                Background = new SolidColorBrush(_hardwareColors[i]),
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
        BtnSetWhite.Click += (s, e) => SetSlotColor(_hardwareColors.Length - 1);
        BtnSetTransparent.Click += (s, e) => SetSlotColor(0);
        
        BtnApply.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(TxtPaletteName.Text))
            {
                MessageBox.Show("Please enter a palette name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Save in module format (bgColors/spriteColors) instead of editor format (name/colors)
            ModuleData = new Dictionary<string, object>
            {
                ["bgColors"] = _currentPalette.ToArray(),
                ["spriteColors"] = _currentPalette.ToArray()  // For now, use same palette for both
            };
            
            System.Diagnostics.Debug.WriteLine($"[PaletteEditor] Saving palette with {_currentPalette.Length} colors: [{string.Join(", ", _currentPalette)}]");
            
            DialogResult = true;
            Close();
        };
        BtnCancel.Click += (s, e) => { DialogResult = false; Close(); };
    }

    private void LoadDefaultPalette()
    {
        for (int i = 0; i < _paletteProvider.SlotCount; i++)
        {
            _currentPalette[i] = (byte)(i * (_hardwareColors.Length / _paletteProvider.SlotCount));
        }

        RefreshSlots();
        SelectSlot(0);
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < _paletteProvider.SlotCount; i++)
        {
            if (SlotsGrid.Children[i] is Button btn)
            {
                var color = _hardwareColors[_currentPalette[i]];
                btn.Background = new SolidColorBrush(color);
            }
        }
    }

    private void SelectSlot(int index)
    {
        _selectedSlotIndex = index;

        for (int i = 0; i < _paletteProvider.SlotCount; i++)
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

        var colorIndex = _currentPalette[index];
        var color = _hardwareColors[colorIndex];

        TxtSlotIndex.Text = string.Format(_loc.Get("paletteeditor.slot_format"), index.ToString("D2"));
        TxtSlotHex.Text = _paletteProvider.GetColorFormat(colorIndex);
        TxtSlotRgb.Text = string.Format(_loc.Get("paletteeditor.rgb_format"), color.R, color.G, color.B);
        ColorPreview.Background = new SolidColorBrush(color);

        UpdateUsageReport(index);
    }

    private void SetSlotColor(int colorIndex)
    {
        if (_selectedSlotIndex < 0) return;

        _currentPalette[_selectedSlotIndex] = (byte)colorIndex;
        RefreshSlots();
        SelectSlot(_selectedSlotIndex);
    }

    private void UpdateUsageReport(int slotIndex)
    {
        UsagePanel.Children.Clear();
        
        if (_project == null || string.IsNullOrEmpty(_paletteElementId))
        {
            var txt = new TextBlock
            {
                Text = _loc.Get("paletteeditor.no_usage"),
                Style = (Style)FindResource("TextBody"),
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant")
            };
            UsagePanel.Children.Add(txt);
            return;
        }
        
        var modulesUsingPalette = _project.Scenes
            .SelectMany(s => s.Elements)
            .Where(e => 
            {
                if (e.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Undefined ||
                    e.ModuleState.ValueKind == System.Text.Json.JsonValueKind.Null)
                    return false;
                    
                if (e.ModuleState.TryGetProperty("paletteModuleId", out var paletteId))
                {
                    return paletteId.GetString() == _paletteElementId;
                }
                return false;
            })
            .ToList();
        
        if (modulesUsingPalette.Count == 0)
        {
            var txt = new TextBlock
            {
                Text = _loc.Get("paletteeditor.no_usage"),
                Style = (Style)FindResource("TextBody"),
                Foreground = (Brush)FindResource("BrushOnSurfaceVariant")
            };
            UsagePanel.Children.Add(txt);
        }
        else
        {
            foreach (var module in modulesUsingPalette)
            {
                var txt = new TextBlock
                {
                    Text = $"• {module.ElementId} ({module.ModuleId})",
                    Style = (Style)FindResource("TextBody"),
                    Foreground = (Brush)FindResource("BrushOnSurface"),
                    Margin = new Thickness(0, 0, 0, 4)
                };
                UsagePanel.Children.Add(txt);
            }
        }
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

    protected override void OnClosed(EventArgs e)
    {
        LocalizationService.LanguageChanged -= ApplyLocalization;
        base.OnClosed(e);
    }
}
