# Mudanças Pendentes - Sistema de Favoritos e Fabricantes

## 1. Adicionar Manufacturer em todos os targets

### SMS Target (Retruxel.Target.SMS\SmsTarget.cs)
Adicionar na linha após `CpuClockHz = 3546893,`:
```csharp
        // Manufacturer
        Manufacturer = "Sega",
```

### NES Target (Retruxel.Target.NES\NesTarget.cs)
Adicionar na linha após `CpuClockHz = 1789773,`:
```csharp
        // Manufacturer
        Manufacturer = "Nintendo",
```

### Game Gear Target (Retruxel.Target.GG\SmsTarget.cs)
Adicionar na linha após `CpuClockHz`:
```csharp
        // Manufacturer
        Manufacturer = "Sega",
```

### SG-1000 Target (Retruxel.Target.SG1000\SG1000Target.cs)
Adicionar na linha após `CpuClockHz`:
```csharp
        // Manufacturer
        Manufacturer = "Sega",
```

### ColecoVision Target (Retruxel.Target.ColecoVision\ColecoVisionTarget.cs)
Adicionar na linha após `CpuClockHz`:
```csharp
        // Manufacturer
        Manufacturer = "Coleco",
```

## 2. Atualizar WelcomeView.xaml.cs

### Substituir método GetManufacturer:
```csharp
private string GetManufacturer(string displayName)
{
    if (displayName.Contains("Sega", StringComparison.OrdinalIgnoreCase)) return "Sega";
    if (displayName.Contains("Nintendo", StringComparison.OrdinalIgnoreCase)) return "Nintendo";
    if (displayName.Contains("Coleco", StringComparison.OrdinalIgnoreCase)) return "Coleco";
    return "Other";
}
```

POR:
```csharp
// Removido - usar target.Specs.Manufacturer diretamente
```

### Atualizar RenderTargets() - linha de sort:
```csharp
var sortedTargets = _currentSort switch
{
    "manufacturer" => filteredTargets.OrderBy(t => t.Specs.Manufacturer).ThenBy(t => t.DisplayName).ToList(),
    _ => filteredTargets.OrderBy(t => t.DisplayName).ToList()
};
```

### Atualizar filtros para usar Manufacturer:
```csharp
var filteredTargets = _currentFilter switch
{
    "favorites" => allTargets.Where(t => _favoriteTargets.Contains(t.TargetId)).ToList(),
    "sega" => allTargets.Where(t => t.Specs.Manufacturer == "Sega").ToList(),
    "nintendo" => allTargets.Where(t => t.Specs.Manufacturer == "Nintendo").ToList(),
    "coleco" => allTargets.Where(t => t.Specs.Manufacturer == "Coleco").ToList(),
    _ => allTargets
};
```

## 3. Adicionar strings i18n

### en.json
```json
"welcome.sort": "SORT:",
"welcome.sort.name": "Name",
"welcome.sort.manufacturer": "Manufacturer",
"welcome.filter": "FILTER:",
"welcome.filter.all": "All",
"welcome.filter.favorites": "Favorites",
"welcome.filter.sega": "Sega",
"welcome.filter.nintendo": "Nintendo",
"welcome.filter.coleco": "Coleco"
```

### pt-BR.json
```json
"welcome.sort": "ORDENAR:",
"welcome.sort.name": "Nome",
"welcome.sort.manufacturer": "Fabricante",
"welcome.filter": "FILTRAR:",
"welcome.filter.all": "Todos",
"welcome.filter.favorites": "Favoritos",
"welcome.filter.sega": "Sega",
"welcome.filter.nintendo": "Nintendo",
"welcome.filter.coleco": "Coleco"
```

## 4. Atualizar InitializeSortAndFilter() para usar i18n

```csharp
private void InitializeSortAndFilter()
{
    var loc = LocalizationService.Instance;
    
    // Sort options
    SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.name"), Tag = "name" });
    SortComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.sort.manufacturer"), Tag = "manufacturer" });
    SortComboBox.SelectedIndex = 0;

    // Filter options
    FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.all"), Tag = "all" });
    FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.favorites"), Tag = "favorites" });
    FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.sega"), Tag = "sega" });
    FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.nintendo"), Tag = "nintendo" });
    FilterComboBox.Items.Add(new ComboBoxItem { Content = loc.Get("welcome.filter.coleco"), Tag = "coleco" });
    FilterComboBox.SelectedIndex = 0;
}
```

## 5. Atualizar WelcomeView.xaml para usar i18n nos labels

```xaml
<TextBlock Text="{loc:Tr Key='welcome.sort'}" Style="{StaticResource TextLabel}" 
           VerticalAlignment="Center" Margin="0,0,8,0"/>

<TextBlock Text="{loc:Tr Key='welcome.filter'}" Style="{StaticResource TextLabel}" 
           VerticalAlignment="Center" Margin="0,0,8,0"/>
```
