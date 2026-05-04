using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Retruxel.Tool.PaletteEditor;

public partial class PaletteEditorWindow : Window
{
    // ── State ─────────────────────────────────────────────────────────────────

    private int _selectedSlotIndex = -1;
    private byte[] _currentPalette;

    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly IPaletteProvider _paletteProvider;
    private readonly Color[] _hardwareColors;

    // Optional: set when opened from a target slot context (ITarget constructor overload)
    private readonly PaletteSlotData? _paletteSlot;
    private readonly ITarget? _target;

    // Optional: set when opened from a scene module context (IPaletteProvider constructor overload)
    private readonly RetruxelProject? _project;
    private readonly string? _paletteElementId;

    // Results written on Apply
    public Dictionary<string, object>? ModuleData { get; private set; }
    public string? SelectedConnectorId { get; private set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the palette editor from a scene module context (e.g. PaletteModule, TilemapEditor).
    /// </summary>
    public PaletteEditorWindow(
        IPaletteProvider paletteProvider,
        string? callerToolId = null,
        byte[]? initialColors = null,
        string? paletteName = null,
        RetruxelProject? project = null,
        string? paletteElementId = null)
    {
        _paletteProvider = paletteProvider;
        _currentPalette = initialColors ?? new byte[paletteProvider.SlotCount];
        _hardwareColors = ConvertToWpfColors(paletteProvider.HardwareColors);
        _project = project;
        _paletteElementId = paletteElementId;

        SelectedConnectorId = callerToolId == "tilemap_editor"
            ? "palette_to_tilemap"
            : "palette_to_module";

        InitializeComponent();
        ApplyLocalization();

        if (!string.IsNullOrEmpty(paletteName))
            TxtPaletteDescription.Text = paletteName;

        InitializeUI();

        if (initialColors == null)
            LoadDefaultPalette();
        else
        {
            RefreshSlots();
            SelectSlot(0);
        }

        LocalizationService.LanguageChanged += ApplyLocalization;
    }

    /// <summary>
    /// Opens the palette editor from a target slot context (e.g. SettingsWindow, TargetConfig).
    /// </summary>
    public PaletteEditorWindow(ITarget target, PaletteSlotData slot)
    {
        _target = target;
        _paletteSlot = slot;
        _paletteProvider = new TargetPaletteProvider(target);
        _currentPalette = new byte[target.GetColorsPerSlot()];
        _hardwareColors = ConvertToWpfColors(target.GetHardwarePalette().Cast<object>().ToArray());

        for (int i = 0; i < slot.Colors.Count && i < _currentPalette.Length; i++)
            _currentPalette[i] = (byte)FindClosestColorIndex(slot.Colors[i]);

        InitializeComponent();

        TxtPaletteDescription.Text =
            $"{target.DisplayName} — {slot.Label} Palette\n{target.GetColorsPerSlot()} colors per slot";

        ApplyLocalization();
        InitializeUI();
        RefreshSlots();
        SelectSlot(0);

        LocalizationService.LanguageChanged += ApplyLocalization;
    }

    // ── Localization ──────────────────────────────────────────────────────────

    private void ApplyLocalization()
    {
        Title = $"RETRUXEL · {_loc.Get("paletteeditor.title")}";
        TxtPalettes.Text           = _loc.Get("paletteeditor.palettes");
        BtnNewPalette.Content      = _loc.Get("paletteeditor.new_palette");
        BtnDuplicate.Content       = _loc.Get("paletteeditor.duplicate");
        BtnDelete.Content          = _loc.Get("paletteeditor.delete");
        TxtSlots.Text              = _loc.Get("paletteeditor.slots");
        TxtHardwareColors.Text     = _loc.Get("paletteeditor.hardware_colors");
        TxtSlotDetail.Text         = _loc.Get("paletteeditor.slot_detail");
        TxtQuickSet.Text           = _loc.Get("paletteeditor.quick_set");
        BtnSetBlack.Content        = _loc.Get("paletteeditor.black");
        BtnSetWhite.Content        = _loc.Get("paletteeditor.white");
        BtnSetTransparent.Content  = _loc.Get("paletteeditor.transparent");
        TxtUsage.Text              = _loc.Get("paletteeditor.usage");
        BtnApply.Content           = _loc.Get("common.apply");
        BtnCancel.Content          = _loc.Get("common.cancel");

        if (_selectedSlotIndex >= 0)
            SelectSlot(_selectedSlotIndex);
    }

    // ── UI Initialization ─────────────────────────────────────────────────────

    private void InitializeUI()
    {
        BuildSlotsGrid();
        BuildHardwareColorGrid();

        BtnNewPalette.Click  += (_, _) => CreateNewPalette();
        BtnDuplicate.Click   += (_, _) => DuplicatePalette();
        BtnDelete.Click      += (_, _) => DeletePalette();

        BtnSetBlack.Click       += (_, _) => SetSlotColor(0);
        BtnSetWhite.Click       += (_, _) => SetSlotColor(_hardwareColors.Length - 1);
        BtnSetTransparent.Click += (_, _) => SetSlotColor(0);

        BtnApply.Click  += (_, _) => Apply();
        BtnCancel.Click += (_, _) => { DialogResult = false; Close(); };
    }

    private void BuildSlotsGrid()
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
            btn.Click += (_, _) => SelectSlot(slotIndex);
            SlotsGrid.Children.Add(btn);
        }
    }

    private void BuildHardwareColorGrid()
    {
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
            btn.Click += (_, _) => SetSlotColor(colorIndex);
            HardwareColorGrid.Children.Add(btn);
        }
    }

    // ── Palette Logic ─────────────────────────────────────────────────────────

    private void LoadDefaultPalette()
    {
        int step = _hardwareColors.Length / _paletteProvider.SlotCount;
        for (int i = 0; i < _paletteProvider.SlotCount; i++)
            _currentPalette[i] = (byte)(i * step);

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
            if (SlotsGrid.Children[i] is not Button btn) continue;

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

        var colorIndex = _currentPalette[index];
        var color = _hardwareColors[colorIndex];

        TxtSlotIndex.Text = string.Format(_loc.Get("paletteeditor.slot_format"), index.ToString("D2"));
        TxtSlotHex.Text   = _paletteProvider.GetColorFormat(colorIndex);
        TxtSlotRgb.Text   = string.Format(_loc.Get("paletteeditor.rgb_format"), color.R, color.G, color.B);
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

    // ── Apply / Save ──────────────────────────────────────────────────────────

    private void Apply()
    {
        // Path A: opened from target slot context — update PaletteSlotData directly
        if (_paletteSlot != null)
        {
            _paletteSlot.Colors.Clear();
            for (int i = 0; i < _currentPalette.Length; i++)
            {
                var color = _hardwareColors[_currentPalette[i]];
                _paletteSlot.Colors.Add($"#{color.R:X2}{color.G:X2}{color.B:X2}");
            }

            DialogResult = true;
            Close();
            return;
        }

        // Path B: opened from scene module context — serialize to module format
        ModuleData = new Dictionary<string, object>
        {
            ["bgColors"]     = _currentPalette.ToArray(),
            ["spriteColors"] = _currentPalette.ToArray()
        };

        DialogResult = true;
        Close();
    }

    // ── Usage Report ──────────────────────────────────────────────────────────

    private void UpdateUsageReport(int slotIndex)
    {
        UsagePanel.Children.Clear();

        if (_project == null || string.IsNullOrEmpty(_paletteElementId))
        {
            AddUsageText(_loc.Get("paletteeditor.no_usage"), "BrushOnSurfaceVariant");
            return;
        }

        var modulesUsingPalette = _project.Scenes
            .SelectMany(s => s.Elements)
            .Where(e =>
            {
                if (e.ModuleState.ValueKind is System.Text.Json.JsonValueKind.Undefined
                                           or System.Text.Json.JsonValueKind.Null)
                    return false;

                return e.ModuleState.TryGetProperty("paletteModuleId", out var id)
                    && id.GetString() == _paletteElementId;
            })
            .ToList();

        if (modulesUsingPalette.Count == 0)
        {
            AddUsageText(_loc.Get("paletteeditor.no_usage"), "BrushOnSurfaceVariant");
            return;
        }

        foreach (var module in modulesUsingPalette)
            AddUsageText($"• {module.ElementId} ({module.ModuleId})", "BrushOnSurface", new Thickness(0, 0, 0, 4));
    }

    private void AddUsageText(string text, string brushKey, Thickness margin = default)
    {
        UsagePanel.Children.Add(new TextBlock
        {
            Text = text,
            Style = (Style)FindResource("TextBody"),
            Foreground = (Brush)FindResource(brushKey),
            Margin = margin
        });
    }

    // ── Palette Management (stub) ─────────────────────────────────────────────

    private void CreateNewPalette()
    {
        // TODO: implement palette list management
        MessageBox.Show("Create new palette", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DuplicatePalette()
    {
        // TODO: implement palette list management
        MessageBox.Show("Duplicate palette", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void DeletePalette()
    {
        // TODO: implement palette list management
        MessageBox.Show("Delete palette", "Not Implemented", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Color[] ConvertToWpfColors(object[] colors)
    {
        var result = new Color[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            if (colors[i] is HardwareColor hwColor)
                result[i] = Color.FromRgb(hwColor.R, hwColor.G, hwColor.B);
        }
        return result;
    }

    private int FindClosestColorIndex(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor) || hexColor.Length < 7 || hexColor[0] != '#')
            return 0;

        int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
        int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
        int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

        int closestIndex = 0;
        int minDistance = int.MaxValue;

        for (int i = 0; i < _hardwareColors.Length; i++)
        {
            var hw = _hardwareColors[i];
            int distance = Math.Abs(hw.R - r) + Math.Abs(hw.G - g) + Math.Abs(hw.B - b);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnClosed(EventArgs e)
    {
        LocalizationService.LanguageChanged -= ApplyLocalization;
        base.OnClosed(e);
    }

    // ── Inner Types ───────────────────────────────────────────────────────────

    /// <summary>
    /// Adapter that wraps ITarget as IPaletteProvider.
    /// Used by the ITarget constructor overload.
    /// </summary>
    private sealed class TargetPaletteProvider : IPaletteProvider
    {
        private readonly ITarget _target;

        public TargetPaletteProvider(ITarget target) => _target = target;

        public string TargetId    => _target.TargetId;
        public string DisplayName => _target.DisplayName;
        public int SlotCount      => _target.GetColorsPerSlot();
        public int GridRows       => 8;
        public int GridColumns    => 8;

        public object[] HardwareColors
            => _target.GetHardwarePalette().Cast<object>().ToArray();

        public string GetColorFormat(int colorIndex)
        {
            var colors = _target.GetHardwarePalette();
            if (colorIndex < 0 || colorIndex >= colors.Count) return "0x00";

            var color = colors[colorIndex];
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
