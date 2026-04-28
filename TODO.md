# Retruxel TODO - Ideias e Melhorias Futuras

Este arquivo contém ideias de melhorias arquiteturais e features planejadas para o futuro.
Diferente do CHECKLIST (tarefas imediatas), este é um registro de conceitos e visões de longo prazo.

---

## 🎨 Reestruturação do Sistema de Paletas

### Problema Atual
- Paleta está acoplada ao tileset (1 tileset = 1 paleta fixa)
- Limitação artificial (ex: SMS só pode usar 2 paletas, mas cada tile poderia usar uma diferente)
- Não representa fielmente as capacidades de cada console
- Dificulta ports entre targets

### Visão da Nova Arquitetura

#### Conceito Central
**Desacoplar paleta de tileset → Paleta por tile com otimização automática**

Cada tile tem sua própria referência de paleta. O sistema cria e otimiza paletas automaticamente baseado nas cores reais usadas.

#### Fluxo de Importação

```
1. Importar tela do emulador (LiveLink)
   ↓
2. Cada tile capturado vem com suas cores únicas (paleta temporária)
   ↓
3. PaletteOptimizer analisa:
   
   SE total_cores_unicas <= max_cores_per_palette:
      → Cria 1 paleta unificada
      → Associa todos os tiles a essa paleta única
      → Caso ideal: máxima compatibilidade entre targets
   
   SENÃO:
      → Cria N paletas (respeitando max_palettes do target)
      → Usa algoritmo de clustering (K-means) para agrupar tiles por afinidade de cores
      → Distribui tiles entre paletas criadas
      → Cada tile aponta para a paleta mais adequada
   ↓
4. Asset salvo como referência (estado original de importação)
   → Permite "reset to original" para facilitar ports
```

#### Estrutura de Dados

```csharp
// TilemapModule - Agora com referências de paleta por tile
public class TilemapModule
{
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public int[] MapData { get; set; }              // Tile indices (como antes)
    public string[] PaletteRefs { get; set; }       // NEW: Palette ID por tile
    public string TilesAssetId { get; set; }        // Referência ao asset
    public string DefaultPaletteId { get; set; }    // Paleta padrão para novos tiles
}

// AssetEntry - Apenas referência (não mais dono da paleta)
public class AssetEntry
{
    public string Id { get; set; }
    public string OriginalPaletteId { get; set; }   // Para "reset to original"
    public DateTime ImportedAt { get; set; }
    // ... outros campos existentes
}

// PaletteModule - Independente e rastreável
public class PaletteModule
{
    public string Id { get; set; }
    public byte[] Colors { get; set; }
    public int UsageCount { get; set; }             // Quantos tiles usam esta paleta
    public List<string> UsedByTiles { get; set; }   // Rastreamento opcional
    public bool IsOptimized { get; set; }           // Gerada por otimizador?
}
```

#### Edição de Paleta

**Cenário 1: Editar via Módulo Palette (Scene Editor)**
- Edita a paleta como módulo independente
- Afeta automaticamente todos os tiles que referenciam essa paleta
- Simples e direto (comportamento atual mantido)

**Cenário 2: Editar via TilemapEditor (tile específico)**

Quando usuário clica em um tile e edita sua paleta:

```
Sistema detecta: "Esta paleta é usada por 47 tiles"

┌─────────────────────────────────────────────┐
│ EDIT PALETTE                                │
├─────────────────────────────────────────────┤
│ This palette is used by 47 tiles.          │
│                                             │
│ ○ Apply to all tiles using this palette    │
│   (47 tiles will be affected)               │
│                                             │
│ ○ Create new palette for this tile only    │
│   (1 tile will use the new palette)         │
│                                             │
│ [CANCEL]  [APPLY]                           │
└─────────────────────────────────────────────┘
```

**Se "Apply to all":**
- Modifica a paleta existente in-place
- Todos os 47 tiles veem a mudança imediatamente

**Se "Create new":**
- Clona a paleta atual → Cria nova com ID único
- Tile selecionado aponta para nova paleta
- Outros 46 tiles continuam usando a paleta original

#### UI do TilemapEditor

**ComboBox de Paleta (mudança de comportamento):**

Antes:
- Mostrava "qual paleta do tilemap usar" (global)
- Seleção afetava todo o tilemap

Depois:
- Mostra "paleta do tile atualmente selecionado"
- Permite trocar paleta do tile específico
- Dropdown lista todas as paletas disponíveis no projeto
- Botão "NEW PALETTE" para criar nova

**Tile Picker (nova feature):**
- Ao selecionar tile no tileset, mostra sua paleta atual
- Permite pintar com "tile + paleta" ou apenas "tile" (mantém paleta do destino)
- Modo "Copy Palette" para copiar paleta de um tile para outro

#### PaletteOptimizer (novo serviço)

```csharp
public class PaletteOptimizer
{
    // Otimiza paletas após importação
    public OptimizationResult Optimize(
        Dictionary<int, uint[]> tilesWithColors,  // tile index → array de cores
        int maxColorsPerPalette,                  // Ex: SMS = 16
        int maxPalettes)                          // Ex: SMS = 2 (mas pode ser mais)
    {
        // 1. Extrair todas as cores únicas de todos os tiles
        var allUniqueColors = ExtractUniqueColors(tilesWithColors);
        
        // 2. Caso ideal: tudo cabe em 1 paleta
        if (allUniqueColors.Count <= maxColorsPerPalette)
        {
            return CreateSinglePalette(allUniqueColors, tilesWithColors);
        }
        
        // 3. Caso complexo: precisa de múltiplas paletas
        return CreateMultiplePalettes(
            tilesWithColors, 
            maxColorsPerPalette, 
            maxPalettes);
    }
    
    private OptimizationResult CreateSinglePalette(...)
    {
        // Cria 1 paleta com todas as cores
        // Associa todos os tiles a essa paleta
        // Retorna mapeamento tile → palette ID
    }
    
    private OptimizationResult CreateMultiplePalettes(...)
    {
        // Algoritmo de clustering (K-means ou similar)
        // Agrupa tiles com cores similares
        // Cria N paletas otimizadas
        // Distribui tiles entre paletas por afinidade
        // Retorna mapeamento tile → palette ID
    }
}

public class OptimizationResult
{
    public List<PaletteModule> Palettes { get; set; }
    public Dictionary<int, string> TileToPalette { get; set; }  // tile index → palette ID
    public int TotalColorsUsed { get; set; }
    public bool IsOptimal { get; set; }  // Conseguiu otimizar sem perda?
}
```

#### Benefícios

1. **Representa fielmente cada console**
   - SMS: Cada tile pode usar paleta diferente (não limitado a 2)
   - NES: Attribute table com 4 palettes por bloco 2x2
   - SNES: 8 palettes com 3 bits de atributo por tile
   - SG-1000: Color Table com FG/BG por grupo de 8 tiles

2. **Facilita ports entre targets**
   - Asset mantém estado original de importação
   - "Reset to original" restaura paletas importadas
   - Otimizador pode re-otimizar para novo target

3. **Flexibilidade criativa**
   - Artista pode criar paletas customizadas por tile
   - Sistema sugere otimizações mas não força
   - Permite efeitos visuais avançados

4. **Otimização automática**
   - Reduz paletas automaticamente quando possível
   - Agrupa tiles por afinidade de cores
   - Maximiza uso de hardware do target

#### Migração

**Projetos existentes:**
- Manter compatibilidade com formato antigo
- Migração automática: `paletteRef` (antigo) → `PaletteRefs[]` (novo)
- Se `paletteRef` existe, preencher array com mesmo ID para todos os tiles

**Implementação gradual:**
1. Adicionar `PaletteRefs[]` ao TilemapModule (opcional)
2. TilemapEditor detecta se array existe
3. Se não existe, usa comportamento antigo (compatibilidade)
4. Se existe, usa novo sistema de paleta por tile

---

## 🔧 Outras Ideias Futuras

### LiveLink - Detecção Automática de Endereços VDP
- Ler registradores VDP para detectar endereços de nametable/pattern/color table
- Eliminar necessidade de hardcoded addresses
- Suportar jogos que usam configurações não-padrão

### PaletteOptimizer - Ler Configurações do Target
**Problema atual:** Constantes hardcoded em `OptimizeForSms()` e `OptimizeForNes()`
```csharp
const int paletteCount = 2;        // SMS: 2 paletas
const int colorsPerPalette = 16;   // SMS: 16 cores por paleta
const int filledColors = 4;        // SMS: apenas 4 preenchidas
```

**Solução futura:** Ler do `TargetSpecs`
```csharp
public static OptimizedPalette OptimizeForTarget(
    byte[][] tiles, 
    uint[] sourceColors, 
    ITarget target,
    int maxIterations = 20)
{
    var paletteSpecs = target.Specs.Palette;
    int paletteCount = paletteSpecs.MaxPalettes;
    int colorsPerPalette = paletteSpecs.ColorsPerPalette;
    int filledColors = paletteSpecs.UsableColors; // Novo campo
    
    // Resto do código usa variáveis ao invés de constantes
}
```

**Benefícios:**
- Suporta novos targets sem modificar código
- Configuração centralizada em `TargetSpecs`
- Facilita testes com diferentes configurações

**Implementação:**
1. Adicionar campos ao `PaletteSpecs` (Core)
2. Atualizar `OptimizeForSms/Nes` para usar método genérico
3. Passar `ITarget` ao invés de string `targetId`

### Asset Pipeline - Conversores Bidirecionais
- Atualmente: Emulator → Retruxel (import only)
- Futuro: Retruxel → Emulator (export para teste)
- Permitir editar no Retruxel e testar no emulador em tempo real

### Module System - Hot Reload
- Recarregar módulos sem reiniciar aplicação
- Facilita desenvolvimento de novos módulos
- Debug mais rápido

### Code Generation - Template Inheritance
- Templates podem herdar de templates base
- Reduz duplicação entre targets similares (SMS/GG/SG-1000)
- Facilita manutenção

---

**Última atualização:** 2024-01-XX
**Status:** Ideias em discussão - não implementado
