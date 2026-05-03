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

### LiveLink - Captura por Camadas de VRAM
**Problema:** Captura de tela única do SNES gera muitos tiles únicos que excedem limite do target de destino (ex: SMS).

**Causa raiz:** Cada console tem arquitetura de VRAM diferente:
- **NES:** 2 pattern tables separadas (0x0000 BG, 0x1000 Sprites) + 4 nametables
- **SNES:** VRAM unificada, múltiplos BG modes, 4 backgrounds reais
- **SMS/GG:** VRAM unificada, 1 nametable física
- **Mega Drive:** 2 scroll planes (Plane A, Plane B) + Window plane

**Solução atual (NES):** UI permite selecionar pattern table (BG, Sprites, ou Both) via ComboBox.

**Problema não resolvido (SNES):**
- Captura tudo de uma vez → Muitos tiles únicos
- Não há como separar BG1, BG2, BG3, BG4
- Tile optimizer ajuda mas não resolve completamente
- Tela pisca e fica preta quando excede limite

**Solução futura:** UI adaptável por console

```csharp
// LiveLinkWindow - UI dinâmica baseada em console detectado
private void UpdateCaptureOptionsUI(string console)
{
    switch (console)
    {
        case "nes":
            // Mostrar: Pattern Table selector (0x0000, 0x1000, Both)
            //          Nametable selector (0x2000, 0x2400, 0x2800, 0x2C00)
            PanelPatternTable.Visibility = Visibility.Visible;
            PanelNametable.Visibility = Visibility.Visible;
            PanelSnesLayers.Visibility = Visibility.Collapsed;
            break;
            
        case "snes":
            // Mostrar: BG Layer selector (BG1, BG2, BG3, BG4, All)
            //          BG Mode detector (Mode 0-7)
            PanelPatternTable.Visibility = Visibility.Collapsed;
            PanelNametable.Visibility = Visibility.Collapsed;
            PanelSnesLayers.Visibility = Visibility.Visible;
            break;
            
        case "sms":
        case "gg":
            // Captura única (1 nametable)
            PanelPatternTable.Visibility = Visibility.Collapsed;
            PanelNametable.Visibility = Visibility.Collapsed;
            PanelSnesLayers.Visibility = Visibility.Collapsed;
            break;
            
        case "megadrive":
            // Mostrar: Plane selector (Plane A, Plane B, Window, Sprites)
            // TODO: Implementar quando Mega Drive target for adicionado
            break;
    }
}
```

**Desafio técnico (SNES):**
- Detectar BG mode atual (registrador PPU $2105)
- Calcular endereços de tilemap por layer (varia por mode)
- Ler VRAM de forma seletiva por layer
- Decodificar tiles com bpp correto (2bpp, 4bpp, 8bpp dependendo do mode)

**Referência de BG Modes (SNES):**
```
Mode 0: BG1-4 (2bpp cada) - 4 layers, 4 cores cada
Mode 1: BG1-2 (4bpp), BG3 (2bpp) - 3 layers, 16+16+4 cores
Mode 2: BG1-2 (4bpp, offset-per-tile) - 2 layers, 16 cores cada
Mode 3: BG1 (8bpp), BG2 (4bpp) - 2 layers, 256+16 cores
Mode 4: BG1 (8bpp, offset-per-tile), BG2 (2bpp) - 2 layers
Mode 5: BG1-2 (4bpp, hi-res 512px) - 2 layers
Mode 6: BG1 (4bpp, hi-res, offset-per-tile) - 1 layer
Mode 7: BG1 (8bpp, rotation/scaling) - 1 layer, 256 cores
```

**Implementação futura:**
1. Adicionar `GetVramLayout()` ao `IEmulatorConnection`
2. Cada emulator retorna estrutura com layers disponíveis
3. LiveLinkWindow gera UI dinamicamente
4. Captura processa layer selecionado isoladamente
5. Tile optimizer trabalha com subset menor de tiles

**Benefícios:**
- Captura mais precisa (apenas layer desejado)
- Menos tiles únicos → Cabe no target de destino
- Melhor compreensão da estrutura do jogo original
- Facilita ports complexos (SNES → SMS)

**Status:** Arquitetura NES já implementada. SNES precisa de:
- [ ] Detecção de BG mode via registradores PPU
- [ ] Cálculo de endereços de tilemap por layer
- [ ] UI para seleção de layer (ComboBox)
- [ ] Leitura seletiva de VRAM por layer
- [ ] Decodificação com bpp correto por mode

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

### Text Display System - Otimização Automática de Tiles de Fonte
**Problema:** Fonte completa ASCII (256 tiles) desperdiça VRAM quando texto usa poucos caracteres únicos.

**Exemplo real:**
- Texto: "PRESS START"
- Fonte completa: 256 tiles (0-255)
- Caracteres únicos: A, E, P, R, S, T, espaço = apenas 7 tiles necessários
- Desperdício: 249 tiles (97.3% de VRAM não utilizada)

**Solução:** Sistema transparente que extrai apenas caracteres usados no projeto.

**Fluxo:**
1. Usuário digita texto normalmente no text_display module
2. Build analisa todos os textos do projeto
3. Extrai caracteres únicos (ex: "AEPRST ")
4. Gera fonte otimizada com apenas 7 tiles
5. Cria mapeamento char → tile index
6. Ajusta startTile de assets automaticamente
7. Persiste mapeamento no .rtrxproject

**Transparência total:**
- Usuário não vê otimização
- Não precisa gerenciar índices manualmente
- Sistema cuida de tudo automaticamente

**Benefícios:**
- "PRESS START" = 7 tiles vs 256 tiles
- Sobra espaço para mais gráficos
- Projetos complexos cabem no limite de VRAM

**Font Editor Tool (complementar):**
- Usar fonte padrão do target (devkitSMS, neslib)
- Importar fonte customizada via PNG
- Editar fonte pixel a pixel (8×8 ou 8×16)
- Preview em tempo real
- Suporte a múltiplos idiomas (ñ, ç, acentos)

**Status:** Conceito documentado - não implementado

### Module System - Hot Reload
- Recarregar módulos sem reiniciar aplicação
- Facilita desenvolvimento de novos módulos
- Debug mais rápido

### Code Generation - Template Inheritance
- Templates podem herdar de templates base
- Reduz duplicação entre targets similares (SMS/GG/SG-1000)
- Facilita manutenção

### Engine Architecture - Target-Specific Capabilities via CodeGen Variables
**Problema:** Estrutura do engine é comum (GameState, TilemapState, etc.) mas capacidades variam por target.

**Exemplos de diferenças:**
- SMS: 1 nametable física, scroll só horizontal
- NES: 4 nametables (mirroring), scroll horizontal + vertical
- SNES: 4 backgrounds reais, scroll per-layer, Mode 7
- Mega Drive: 2 scroll planes, scroll per-line

**Solução futura:** Variáveis de substituição target-specific em CodeGens

```json
// codegen.json - Variáveis do target
{
  "variables": {
    "maxSprites": { "from": "target", "path": "specs.maxSprites" },
    "maxTilemapLayers": { "from": "target", "path": "specs.maxTilemapLayers" },
    "supportsVerticalScroll": { "from": "target", "path": "specs.supportsVerticalScroll" },
    "supportsPerLayerScroll": { "from": "target", "path": "specs.supportsPerLayerScroll" }
  }
}
```

```c
// engine.c.rtrx - Template com condicionais
void Engine_Render(GameState* state) {
    {{#if supportsPerLayerScroll}}
    // SNES/Mega Drive: scroll per-layer
    for (i = 0; i < state->tilemaps.layerCount; i++) {
        SetLayerScroll(i, state->tilemaps.layers[i].scrollX, 
                          state->tilemaps.layers[i].scrollY);
    }
    {{/if}}
    
    {{#ifnot supportsPerLayerScroll}}
    // SMS/NES: scroll global
    if (state->scrollDirty) {
        SetGlobalScroll(state->scrollX, state->scrollY);
    }
    {{/ifnot}}
    
    {{#if supportsVerticalScroll}}
    // NES/SNES: vertical scroll
    scroll(state->scrollX, state->scrollY);
    {{/if}}
    
    {{#ifnot supportsVerticalScroll}}
    // SMS: só horizontal
    SMS_setBGScrollX(state->scrollX);
    // scrollY ignorado
    {{/ifnot}}
}
```

**Benefícios:**
- Engine comum mas adaptável
- Não limita targets avançados ao menor denominador comum
- Configuração declarativa via TargetSpecs
- Templates reutilizáveis com variações

**Implementação:**
1. Adicionar campos ao `TargetSpecs` (maxTilemapLayers, supportsVerticalScroll, etc.)
2. ModuleRenderer suporta `"from": "target"` em variáveis
3. Cada target define suas capacidades em `GetSpecs()`
4. Templates usam condicionais para gerar código específico

**Status:** Arquitetura atual suporta múltiplos tilemaps (MAX_TILEMAP_LAYERS=4), mas uso real depende de hardware. Variáveis target-specific permitirão otimizar geração de código por console.

**Nota sobre SMS:** Atualmente permite múltiplos tilemaps no GameState, mas SMS tem apenas 1 nametable física. O último tilemap renderizado sobrescreve os anteriores. Casos de uso válidos:
- **Scene switching**: Trocar tilemaps entre cenas (não simultâneo)
- **Tile range separation**: Cada tilemap usa faixas diferentes de startTile (ex: tilemap_0 usa 0-127, tilemap_1 usa 128-255)
- **Futuro com Visual Scripting**: Lógica condicional no Retruxel IDE permitirá controlar quando cada tilemap é carregado:
  ```
  IF player.x > 128 THEN
      tilemap_cave.load()
  ELSE
      tilemap_forest.load()
  ```
  Isso resolve o problema de sobreposição permitindo o usuário decidir qual tilemap renderizar baseado em condições de jogo.

**Implementação futura:**
1. **Visual Scripting System** - Node-based logic editor no Retruxel IDE
2. **Conditional Execution** - Módulos podem ter condições de execução (IF/ELSE/SWITCH)
3. **State Management** - Variáveis de jogo acessíveis via scripting (player.x, score, level, etc.)
4. **Code Generation** - Templates geram código C com condicionais baseados na lógica visual

**Exemplo de geração:**
```c
// Visual script: IF level == 1 THEN tilemap_forest ELSE tilemap_cave
void scene_init(void) {
    if (g_gameState.level == 1) {
        tilemap_forest_init();
    } else {
        tilemap_cave_init();
    }
}
```

---

**Última atualização:** 2024-01-XX
**Status:** Ideias em discussão - não implementado


---

## Palette System - Correções Urgentes

### Problema 1: Paleta Fantasma no .rtrxproject
**Status**: 🔴 Crítico  
**Descrição**: Quando o TilemapEditor cria uma paleta automaticamente, ela é salva no .rtrxproject mas não aparece visualmente na cena.

**Causa Raiz**:
- O método `AddElementFromData` está sendo chamado via reflexão no TilemapEditor
- Pode estar falhando silenciosamente
- O elemento é adicionado ao `_project.Scenes[0].Elements` mas não ao `_elements` da SceneEditorView

**Solução Implementada**:
- ✅ Adicionado melhor tratamento de erro com MessageBox quando AddElementFromData falha
- ✅ Adicionado debug logging para rastrear o problema
- 🔄 Pendente: Testar se o método está sendo chamado corretamente

**Próximos Passos**:
1. Verificar se `_sceneEditor` está null quando TilemapEditor tenta chamar AddElementFromData
2. Considerar passar um callback direto em vez de usar reflexão
3. Garantir que o elemento seja adicionado tanto ao projeto quanto à UI

---

### Problema 2: Paleta Criada com Cores Padrão
**Status**: 🟡 Alta Prioridade  
**Descrição**: Quando o usuário seleciona um asset no TilemapEditor e não tem paleta, o sistema abre o PaletteEditor mas com cores padrão em vez das cores do asset.

**Fluxo Atual**:
1. Usuário seleciona asset no TilemapEditor
2. Sistema detecta que não há paleta
3. Abre PaletteEditor com `sourceAsset = null`
4. PaletteEditor mostra paleta padrão (preto + branco)

**Fluxo Esperado**:
1. Usuário seleciona asset no TilemapEditor
2. Sistema detecta que não há paleta
3. **Extrai cores do asset selecionado**
4. **Se cores não são do hardware, abre PaletteOptimizationWindow primeiro**
5. Abre PaletteEditor com cores do asset (ou otimizadas)
6. Usuário ajusta se necessário
7. Paleta criada já vem com cores corretas

**Solução Implementada**:
- ✅ Modificado `OpenPaletteEditor` para buscar asset selecionado se `sourceAsset` for null
- ✅ Cores do asset são extraídas via `ExtractColorsFromAsset`
- 🔄 Pendente: Adicionar validação de cores antes de abrir PaletteEditor

**Próximos Passos**:
1. No `CmbTilesetAsset_SelectionChanged`, quando não há paleta:
   - Validar cores do asset com `ValidateAssetColors`
   - Se inválidas, abrir `PaletteOptimizationWindow` primeiro
   - Depois abrir `PaletteEditor` com cores otimizadas
2. Garantir que o fluxo seja transparente para o usuário

---

### Problema 3: Ferramenta de Otimização em Lugar Errado
**Status**: 🟢 Melhoria  
**Descrição**: `PaletteOptimizationWindow` está em `Retruxel.Tool.LiveLink` mas é uma ferramenta genérica usada por todos.

**Impacto**:
- Confusão conceitual (LiveLink é para captura de emulador)
- Dependência desnecessária do LiveLink
- Dificulta reutilização em outros contextos

**Solução Proposta**:
1. Criar novo projeto: `Retruxel.Tool.PaletteOptimizer`
2. Mover `PaletteOptimizationWindow` para lá
3. Implementar `ITool` interface
4. Atualizar referências:
   - `Retruxel.Tool.TilemapEditor`
   - `Retruxel.Tool.AssetImporter`
   - `Retruxel.Tool.LiveLink` (manter referência para uso em captura)

**Estrutura do Novo Projeto**:
```
Retruxel.Tool.PaletteOptimizer/
├── PaletteOptimizerTool.cs          # ITool implementation
├── PaletteOptimizationWindow.xaml   # UI (movido de LiveLink)
├── PaletteOptimizationWindow.xaml.cs
├── Algorithms/
│   ├── DiversityCalculator.cs      # Cálculo de diversity
│   ├── ColorQuantizer.cs           # Redução de cores
│   └── LabColorSpace.cs            # Conversão RGB ↔ LAB
└── README.md                        # Documentação do algoritmo
```

**Benefícios**:
- Ferramenta standalone e reutilizável
- Pode ser usada por qualquer tool que precise reduzir cores
- Facilita testes e manutenção
- Documentação centralizada do algoritmo de diversity

---

### Fluxo Completo Proposto: TilemapEditor → Palette

**Cenário 1: Asset com cores válidas**
```
1. Usuário seleciona asset
2. Sistema valida cores → ✅ OK
3. Sistema detecta: sem paleta
4. Abre PaletteEditor com cores do asset
5. Usuário confirma
6. Paleta criada e módulo adicionado à cena
```

**Cenário 2: Asset com cores inválidas**
```
1. Usuário seleciona asset
2. Sistema valida cores → ❌ Inválidas
3. MessageBox: "Asset contém cores não suportadas. Deseja otimizar?"
4. Se SIM:
   a. Abre PaletteOptimizationWindow
   b. Usuário ajusta diversity
   c. Asset é salvo com cores otimizadas
   d. Abre PaletteEditor com cores otimizadas
   e. Usuário confirma
   f. Paleta criada e módulo adicionado à cena
5. Se NÃO:
   a. Volta para TilemapEditor
   b. Usuário pode escolher outro asset
```

**Cenário 3: Editar paleta existente**
```
1. Usuário clica "Edit Palette"
2. Abre PaletteEditor com paleta atual
3. Usuário modifica cores
4. Confirma
5. Paleta atualizada no projeto
6. Tileset re-renderizado com nova paleta
```

---

### Checklist de Implementação

**Fase 1: Correções Críticas** (Hoje)
- [ ] Testar e corrigir AddElementFromData no TilemapEditor
- [ ] Garantir que paleta apareça visualmente na cena
- [ ] Adicionar validação de cores antes de abrir PaletteEditor
- [ ] Implementar fluxo: asset inválido → otimização → palette editor

**Fase 2: Refatoração** (Esta semana)
- [ ] Criar projeto `Retruxel.Tool.PaletteOptimizer`
- [ ] Mover `PaletteOptimizationWindow` de LiveLink
- [ ] Atualizar todas as referências
- [ ] Documentar algoritmo de diversity

**Fase 3: Melhorias** (Próxima semana)
- [ ] Adicionar preview do tileset no PaletteEditor
- [ ] Permitir ajuste fino de cores individuais
- [ ] Salvar paletas como presets reutilizáveis
- [ ] Importar paletas de outros projetos

---

### Notas Técnicas

**Por que não usar hardcode para "palette"?**
- Não há hardcode de `elementId == "palette"` no código
- O sistema usa `moduleId == "palette"` que é correto
- O problema é na criação do elemento visual, não na identificação

**Por que reflexão no TilemapEditor?**
- TilemapEditor é um plugin (DLL separado)
- SceneEditorView está no projeto principal
- Reflexão evita dependência circular
- Alternativa: passar callback `Action<SceneElementData>` no construtor

**Diversity Algorithm (LAB Color Space)**:
- RGB não é perceptualmente uniforme
- LAB simula percepção humana de cor
- Diversity = distância mínima entre cores no espaço LAB
- Valores típicos: 5-15 (baixo = mais cores, alto = menos cores)


---

## LiveLink - Métodos de Captura de Mapa

### Visão Geral

Quando você captura VRAM de uma ROM rodando, você só pega 32×28 tiles (o que está visível). Mas muitos jogos têm mapas maiores (ex: Kung Fu Master tem ~128×24). Implementamos 4 métodos para capturar o mapa completo:

---

### Método 1: Single Frame (Atual) ✅
**Status**: Implementado  
**Quando usar**: Mapa cabe em 32×28 tiles ou você quer apenas a tela atual

**Como funciona**:
1. Captura VRAM atual (32×28 tiles)
2. Renderiza com tileset e paleta
3. Exporta como PNG

**Limitações**:
- Só captura o que está visível
- Não pega o resto do mapa

---

### Método 2: Auto-Scroll 🔄
**Status**: Planejado  
**Quando usar**: Jogo tem scroll horizontal e você pode jogar a fase

**Como funciona**:
1. Usuário clica "Start Auto-Scroll Capture"
2. Joga normalmente, scrollando pela fase
3. LiveLink detecta mudanças no scroll register (VDP)
4. Captura novas colunas automaticamente
5. Monta mapa completo ao final

**Implementação**:
```csharp
// Detectar scroll
int scrollX = ReadVdpRegister(8); // Scroll X register

if (scrollX > _lastScrollX + 8) // Scrollou 1 tile
{
    byte[] newColumn = CaptureColumn(31); // Coluna da direita
    _capturedColumns.Add(newColumn);
    _lastScrollX = scrollX;
}

// Ao final
var completeMap = AssembleMap(_capturedColumns);
```

**Vantagens**:
- Captura mapa completo automaticamente
- Funciona mesmo se mapa não está na ROM
- Não precisa engenharia reversa

**Limitações**:
- Só funciona para scroll horizontal (SMS não tem scroll vertical)
- Precisa jogar a fase inteira
- Não funciona se jogo troca tiles dinamicamente

---

### Método 3: RAM Analysis 🔄
**Status**: Planejado  
**Quando usar**: Você sabe o endereço RAM onde o mapa está armazenado

**Como funciona**:
1. Usuário informa endereço RAM (ex: 0xC000)
2. Usuário informa dimensões (ex: 128×24)
3. LiveLink lê mapa completo da RAM via Mesen API
4. Renderiza com tileset e paleta

**Implementação**:
```csharp
// Ler RAM via Mesen
var mapData = _mesenConnection.SendCommand($"READ_RAM {address} {size}");

// Converter para nametable
var nametable = ParseNametable(mapData, width, height);

// Renderizar
var image = RenderNametable(nametable, tileset, palette);
```

**Vantagens**:
- Captura instantânea
- Funciona para qualquer tamanho de mapa
- Não precisa jogar

**Limitações**:
- Precisa saber endereço RAM (engenharia reversa)
- Cada jogo armazena diferente
- Pode estar comprimido

---

### Método 4: Canvas Expansion ✅
**Status**: Implementado  
**Quando usar**: Mapa está na ROM e você quer expandir bordas

**Como funciona**:
1. Usuário especifica expansão (ex: +96 tiles à direita)
2. Sistema busca padrão do nametable atual na ROM
3. Lê tiles adjacentes da ROM
4. Renderiza mapa expandido
5. Usuário confirma visualmente se pegou mapa ou lixo

**Implementação**:
```csharp
// Buscar nametable na ROM
var pattern = ExtractPattern(currentNametable, 16×12); // Centro
var romAddress = SearchPattern(romData, pattern);

// Expandir
var expandedNametable = ReadRomTiles(
    romAddress, 
    width: 32 + expandRight,
    height: 28 + expandDown
);

// Renderizar
var image = RenderNametable(expandedNametable, tileset, palette);
```

**Vantagens**:
- Não precisa jogar
- Funciona para mapas grandes na ROM
- Mostra resultado visual imediato
- Detecta largura do mapa automaticamente

**Limitações**:
- Só funciona se mapa está na ROM (não comprimido)
- Pode pegar lixo se padrão não for único
- Precisa confirmação visual do usuário

**Algoritmo de Busca**:
1. Extrai padrão 16×12 do centro do nametable (mais único)
2. Tenta larguras comuns: 32, 64, 128, 256, 40, 48, etc.
3. Busca ROM byte a byte
4. Calcula confiança (% de tiles que batem)
5. Retorna melhor match (>70% confiança)
6. Estima altura verificando onde dados ficam inválidos

---

### Comparação dos Métodos

| Método | Velocidade | Precisão | Requer Jogar | Requer Eng. Reversa | Funciona com Compressão |
|--------|-----------|----------|--------------|---------------------|------------------------|
| Single Frame | ⚡ Instantâneo | ✅ 100% | ❌ Não | ❌ Não | ✅ Sim |
| Auto-Scroll | 🐌 Lento (jogar fase) | ✅ 100% | ✅ Sim | ❌ Não | ✅ Sim |
| RAM Analysis | ⚡ Instantâneo | ✅ 100% | ❌ Não | ✅ Sim | ❌ Não |
| Canvas Expansion | ⚡ Instantâneo | ⚠️ 70-95% | ❌ Não | ❌ Não | ❌ Não |

---

### UI Proposta

```
┌─────────────────────────────────────────┐
│ CAPTURE METHOD                          │
├─────────────────────────────────────────┤
│ ○ Single Frame (32×28)                  │
│   Current VRAM only                     │
│                                         │
│ ○ Auto-Scroll                           │
│   Play and capture automatically        │
│   [Start Capture] [Stop]                │
│   Status: Captured 96 columns           │
│                                         │
│ ○ RAM Analysis                          │
│   Read from RAM address                 │
│   Address: [0xC000]  Size: [128×24]     │
│   [Capture from RAM]                    │
│                                         │
│ ● Canvas Expansion                      │
│   Expand borders by searching ROM       │
│   [Configure Expansion...]              │
│                                         │
│ [CAPTURE]                               │
└─────────────────────────────────────────┘
```

---

### Casos de Uso

**Kung Fu Master (NES → SMS)**:
1. **Método recomendado**: Canvas Expansion
2. Conectar LiveLink ao Kung Fu Master (NES)
3. Pausar no início da fase
4. Selecionar "Canvas Expansion"
5. Configurar: Right = 96 tiles (128 total)
6. Sistema busca maptable na ROM
7. Renderiza mapa 128×24 completo
8. Confirmar visualmente
9. Importar para Retruxel

**Sonic (SMS)**:
1. **Método recomendado**: Auto-Scroll
2. Conectar LiveLink ao Sonic
3. Selecionar "Auto-Scroll"
4. Jogar a fase do início ao fim
5. LiveLink captura colunas automaticamente
6. Mapa completo montado ao final

**Jogo Desconhecido**:
1. **Tentar primeiro**: Canvas Expansion (mais rápido)
2. **Se falhar**: Auto-Scroll (sempre funciona)
3. **Se souber RAM**: RAM Analysis (mais preciso)

---

### Próximos Passos

**Fase 1: Canvas Expansion** ✅
- [x] Criar `CanvasExpansionCapture.cs`
- [x] Criar `CanvasExpansionWindow.xaml`
- [x] Algoritmo de busca de padrão
- [x] Detecção automática de largura
- [x] Estimativa de altura
- [ ] Integrar com LiveLinkWindow
- [ ] Testar com Kung Fu Master

**Fase 2: Auto-Scroll** 🔄
- [ ] Detectar scroll register (VDP)
- [ ] Capturar colunas automaticamente
- [ ] Montar mapa completo
- [ ] UI de progresso
- [ ] Testar com jogos de plataforma

**Fase 3: RAM Analysis** 🔄
- [ ] UI para endereço RAM
- [ ] Ler RAM via Mesen API
- [ ] Parser de nametable
- [ ] Suporte para formatos comuns
- [ ] Testar com vários jogos

---

### Notas Técnicas

**Por que Canvas Expansion primeiro?**
- Mais útil para Kung Fu Master (use case primário)
- Não requer modificações no Mesen
- Funciona offline (só precisa ROM)
- Resultado instantâneo

**Formato de Nametable**:
- SMS: 2 bytes por tile (tile index + attributes)
- Attributes: `PCCVHTTT` (Priority, Color, VFlip, HFlip, Tile MSB)
- Largura fixa: 32 tiles
- Altura: 28 tiles (24 visíveis + 4 overscan)

**Detecção de Largura**:
- Tenta larguras comuns: 32, 64, 128, 256
- Também tenta: 40, 48, 56, 72, 80, 96, 112 (menos comuns)
- Calcula confiança para cada largura
- Escolhe melhor match

**Estimativa de Altura**:
- Verifica linha por linha
- Conta tiles válidos (não 0xFF, não 0x00, < 0x01FF)
- Para quando >50% da linha é lixo
- Mínimo: 28 tiles (VRAM height)


---

## Canvas Expansion - Limitações Conhecidas

### Kung Fu Master (NES)
**Problema**: Canvas Expansion não encontra o mapa na ROM.

**Causa**: O jogo usa **scrolling dinâmico** - o mapa completo (64×60 tiles) está na **RAM**, não na ROM. Apenas a tela atual (32×30) está na VRAM.

**Evidência** (Mesen Tilemap Viewer):
```
Size: 64x60
Size (px): 512x480
Tilemap Address: $2000
Tileset Address: $1000
Tile Format: 2 bpp
Mirroring: Vertical
```

**Por que não funciona**:
1. Canvas Expansion busca o nametable na ROM
2. Kung Fu Master carrega o mapa da ROM para RAM descompactado
3. Conforme você anda, o jogo atualiza a VRAM com novos tiles da RAM
4. O mapa completo nunca está na ROM de forma contígua

**Mega Man 2 (NES)**:
**Problema**: Canvas Expansion encontra apenas 7,3% de confiança.

**Causa**: O jogo usa **compressão RLE** no ROM. O mapa não está armazenado descomprimido.

**Evidência** (logs de debug):
```
ROM:     A1 A1 A1 A1 A1 A1 A1 A1 A1 A1 A1 A1 A1 A1 A1 A1
Pattern: AD AF B1 B3 AD AF B1 B3 CD CF 71 73 71 73 CD CF
```

O ROM contém tiles repetidos (`A1 A1 A1...`) enquanto o nametable capturado tem tiles variados. Isso indica que o mapa está comprimido no ROM e só é descompactado na RAM durante o jogo.

**Jogos que funcionam com Canvas Expansion**:
- ✅ Donkey Kong (telas estáticas)
- ✅ Pac-Man (labirintos estáticos)
- ✅ Balloon Fight (níveis estáticos)
- ✅ Telas de título e menus (geralmente descomprimidos)

**Jogos que NÃO funcionam**:
- ❌ Mega Man 2 (compressão RLE)
- ❌ Kung Fu Master (scrolling dinâmico)
- ❌ Super Mario Bros (compressão)
- ❌ Metroid (procedural generation)

**Soluções alternativas**:
1. **Auto-Scroll** (recomendado) - Jogar a fase e capturar colunas automaticamente
2. **RAM Analysis** - Ler direto da RAM se souber o endereço (requer engenharia reversa)
3. **Manual** - Capturar múltiplas telas e montar no editor de imagem

**Status**: Canvas Expansion funciona para jogos com mapas estáticos descomprimidos na ROM (ex: telas de título, menus, single-screen games). Para jogos com compressão ou scrolling dinâmico, implementar Auto-Scroll é necessário.

---

## Canvas Expansion - Suporte a Compressão RLE

### Contexto
**Quase todos os jogos comerciais de NES usam compressão** (RLE, LZ77, ou formatos proprietários). Isso significa que Canvas Expansion, na forma atual, só funciona para:
- Telas estáticas (título, menus)
- Single-screen games (Donkey Kong, Pac-Man)
- Homebrew/demos sem compressão

**Jogos comerciais populares que usam compressão:**
- Super Mario Bros (RLE proprietário Nintendo)
- Mega Man 2 (RLE customizado Capcom)
- Metroid (formato proprietário)
- Castlevania (formato proprietário)
- Zelda (formato proprietário)
- Praticamente todo jogo AAA do NES

### Solução Proposta: Database de Decompressores

**Abordagem:** Manter database com decompressores específicos por jogo conhecido.

**Estrutura:**
```
Retruxel.Tool.LiveLink/
├── Capture/
│   └── Decompression/
│       ├── IDecompressor.cs              # Interface base
│       ├── CompressionDatabase.cs        # SHA256 hash → Decompressor
│       ├── Decompressors/
│       │   ├── MegaMan2Decompressor.cs   # RLE Capcom
│       │   ├── SuperMarioBrosDecompressor.cs  # RLE Nintendo
│       │   ├── MetroidDecompressor.cs    # Formato proprietário
│       │   └── ZeldaDecompressor.cs      # Formato proprietário
│       └── Generic/
│           ├── RleFormat1.cs             # [count][value]
│           ├── RleFormat2.cs             # [value][count]
│           └── RleFormat3.cs             # [control byte][data...]
```

**Fluxo:**
```
1. Busca direta na ROM (atual)
   ↓ (se falhar com <50% confiança)
2. Calcular SHA256 da ROM
   ↓
3. Buscar no database de jogos conhecidos
   ↓ (se encontrado)
4. Usar decompressor específico
   ↓
5. Descomprimir mapa
   ↓
6. Buscar nametable no mapa descomprimido
   ↓ (se não encontrado no database)
7. Tentar heurística genérica (3-5 formatos RLE comuns)
   ↓
8. Retornar resultado (com aviso se foi heurística)
```

**Interface:**
```csharp
public interface IDecompressor
{
    string GameName { get; }              // "Mega Man 2"
    string CompressionType { get; }       // "RLE (Capcom)"
    
    byte[] Decompress(byte[] romData, int offset, int expectedSize);
    int FindMapAddress(byte[] romData);   // Onde está o mapa comprimido
    bool CanHandle(byte[] romData);       // Validação adicional
}

public class CompressionDatabase
{
    // SHA256 hash da ROM → Decompressor
    private static readonly Dictionary<string, IDecompressor> _knownGames = new()
    {
        ["A1B2C3D4..."] = new MegaMan2Decompressor(),
        ["E5F6G7H8..."] = new SuperMarioBrosDecompressor(),
    };
    
    public IDecompressor? GetDecompressor(byte[] romData)
    {
        var hash = SHA256.HashData(romData);
        var hashString = Convert.ToHexString(hash);
        return _knownGames.TryGetValue(hashString, out var dec) ? dec : null;
    }
}
```

**Algoritmo Hybrid:**
```csharp
public ExpansionResult ExpandCanvasWithDecompression(...)
{
    // 1. Busca direta (jogos sem compressão)
    var result = SearchNametableInData(...);
    if (result.Confidence >= 0.5f)
        return Success(result);
    
    // 2. Detectar se parece comprimido
    if (!LooksCompressed(searchData))
        return Failure("Map not found");
    
    // 3. Database de jogos conhecidos (100% precisão)
    var decompressor = _db.GetDecompressor(searchData);
    if (decompressor != null)
    {
        var decompressed = decompressor.Decompress(...);
        result = SearchNametableInData(nametable, decompressed, ...);
        if (result.Confidence >= 0.5f)
            return Success($"Found using {decompressor.GameName} decompressor");
    }
    
    // 4. Heurística genérica (último recurso)
    foreach (var format in _genericFormats)
    {
        var decompressed = TryDecompress(searchData, format);
        result = SearchNametableInData(nametable, decompressed, ...);
        if (result.Confidence >= 0.5f)
            return Success($"Found using generic {format.Name} (verify visually!)");
    }
    
    // 5. Falhou
    return Failure("Map is compressed but format is unknown");
}
```

**Performance:**
- Busca atual: 1-2s
- SHA256 hash: +0.1s
- Decompressão: +0.5-2s
- Busca no descomprimido: +1-2s
- **Total: 3-5s** (aceitável)

**Heurística genérica:**
- Tentar 3-5 formatos RLE comuns
- **Total: 10-15s** (lento mas viável)

### Implementação em Fases

**Fase 1: Infraestrutura** (1-2 dias)
- [ ] Criar `IDecompressor` interface
- [ ] Criar `CompressionDatabase` com SHA256 lookup
- [ ] Integrar no `ExpandCanvas()` com fallback
- [ ] Adicionar logs detalhados
- [ ] UI mostra qual decompressor foi usado

**Fase 2: Jogos Populares** (1 semana)
- [ ] Engenharia reversa do Mega Man 2 (RLE Capcom)
- [ ] Engenharia reversa do Super Mario Bros (RLE Nintendo)
- [ ] Engenharia reversa do Metroid
- [ ] Testar com ROMs reais
- [ ] Documentar formatos no wiki

**Fase 3: Heurística Genérica** (3-5 dias)
- [ ] Implementar 3-5 formatos RLE comuns
- [ ] Detector de compressão (repetition rate)
- [ ] Validação de nametable descomprimido
- [ ] Aviso visual quando usa heurística

**Fase 4: Comunidade** (contínuo)
- [ ] Documentar como adicionar novos decompressores
- [ ] Plugin system para decompressores externos
- [ ] GitHub wiki com formatos conhecidos
- [ ] Aceitar contribuições da comunidade

### Benefícios

✅ **Para jogos conhecidos:**
- 100% de precisão
- Mensagem clara: "Mega Man 2 detected - using Capcom RLE decompressor"
- Performance aceitável (3-5s)

✅ **Para jogos desconhecidos:**
- Heurística tenta formatos comuns
- Aviso: "Generic RLE detected - verify result visually"
- Melhor que nada

✅ **Escalabilidade:**
- Comunidade pode contribuir
- Não requer modificar código core
- Database cresce organicamente

### Limitações

❌ **Não é universal:**
- Cada jogo tem formato único
- Impossível cobrir 100% dos jogos
- Foco em jogos populares

❌ **Requer engenharia reversa:**
- Trabalho manual por jogo
- Pode violar ToS de alguns jogos (área cinzenta legal)
- Documentação pode ser escassa

⚠️ **Heurística pode falhar:**
- Falsos positivos (gera lixo que parece válido)
- Sempre requer confirmação visual
- Último recurso apenas

### Alternativas

**Se decompressão for muito complexa:**
1. **Auto-Scroll** - Jogar a fase e capturar automaticamente (sempre funciona)
2. **RAM Analysis** - Ler mapa descomprimido da RAM (requer endereço)
3. **Manual** - Capturar múltiplas telas e montar externamente

### Notas Técnicas

**Por que SHA256?**
- Identifica ROM exata (incluindo versão/região)
- Rápido (0.1s para 256KB)
- Evita falsos positivos

**Por que não CRC32?**
- Colisões mais prováveis
- Menos seguro
- SHA256 é padrão moderno

**Formatos RLE comuns:**
1. **[count][value]** - Ex: `05 A1` = cinco tiles 0xA1
2. **[value][count]** - Ex: `A1 05` = cinco tiles 0xA1
3. **[control][data...]** - Bit flags indicam literal vs run
4. **[escape][count][value]** - Escape byte indica run

**Detecção de compressão:**
```csharp
private bool LooksCompressed(byte[] data)
{
    // Contar bytes repetidos consecutivos
    int repeatedBytes = 0;
    for (int i = 0; i < data.Length - 1; i++)
        if (data[i] == data[i + 1])
            repeatedBytes++;
    
    float repetitionRate = (float)repeatedBytes / data.Length;
    
    // Descomprimido: >50% repetição (ex: céu, chão)
    // Comprimido: <20% repetição (dados variados)
    return repetitionRate < 0.2f;
}
```

**Status:** Documentado - não implementado. Prioridade baixa (após Auto-Scroll e RAM Analysis).


## Engine Architecture - Persistent Dirty Flags

### Problema Atual

**State-based rendering limpa dirty flags indiscriminadamente:**

```c
void Engine_ClearDirtyFlags(GameState* state) {
    state->tilemaps.dirty = false;  // ❌ Limpa mesmo se tilemap é estático
    state->uiDirty = false;         // ❌ Limpa mesmo se texto é estático
    state->spritesDirty = false;
    state->scrollDirty = false;
}
```

**Consequência:** Módulos gráficos precisam de `_update()` para remarcar dirty flags todo frame, mesmo que sejam estáticos.

**Workaround atual:**
```c
// text_display precisa de _update() só para remarcar dirty flag
void text_display_0_update(void) {
    extern GameState g_gameState;
    g_gameState.uiDirty = true;  // Remarca todo frame (ineficiente)
}
```

**Módulos afetados:**
- ✅ `text.display` - Precisa de `_update()` para manter texto visível
- ✅ `sprite` - Precisa de `_update()` se animado/movendo
- ❌ `tilemap` - Estático, não precisa de `_update()` (dirty persiste após primeiro render)
- ❌ `palette` - Estático, não precisa de `_update()` (dirty persiste após primeiro render)

**Por que tilemap/palette não precisam de update?**
- `Engine_Render()` só renderiza se `dirty == true`
- Tilemap/palette marcam `dirty = true` no `_init()`
- `Engine_ClearDirtyFlags()` limpa a flag
- **MAS** tilemap/palette já foram escritos na VRAM/CRAM
- VRAM/CRAM são persistentes - dados não desaparecem
- Próximo frame: `dirty == false` → engine não re-renderiza → VRAM mantém dados

**Por que text.display precisa de update?**
- `SMS_printatXY()` escreve direto na VRAM
- Mas `Engine_Render()` só chama `SMS_printatXY()` se `uiDirty == true`
- Se `uiDirty` for limpo e não remarcado, texto não é re-escrito
- **Problema:** Outros módulos podem sobrescrever a área de texto na VRAM
- Solução atual: `_update()` remarca `uiDirty` todo frame para garantir re-escrita

### Solução Proposta: Persistent Flags

**Adicionar flag `persistent` ao GameState:**

```c
typedef struct {
    TilemapLayerState layers[MAX_TILEMAP_LAYERS];
    unsigned char layerCount;
    bool dirty;
    bool persistent;  // NEW: Se true, dirty não é limpo automaticamente
} TilemapState;

typedef struct {
    char text[256];
    unsigned char x;
    unsigned char y;
    unsigned char color;
    bool persistent;  // NEW
} TextLayerState;
```

**Engine_ClearDirtyFlags() respeita persistent:**

```c
void Engine_ClearDirtyFlags(GameState* state) {
    // Só limpa se não for persistente
    if (!state->tilemaps.persistent)
        state->tilemaps.dirty = false;
    
    if (!state->ui.persistent)
        state->uiDirty = false;
    
    if (!state->sprites.persistent)
        state->spritesDirty = false;
    
    // Scroll sempre limpa (sempre dinâmico)
    state->scrollDirty = false;
}
```

**Módulos marcam persistent no _init():**

```c
// Text display estático - marca persistent
void text_display_0_init(void) {
    extern GameState g_gameState;
    // ... popula texto
    g_gameState.uiDirty = true;
    g_gameState.ui.persistent = true;  // NEW: Não limpar dirty flag
}

// Sprite animado - NÃO marca persistent
void sprite_0_init(void) {
    extern GameState g_gameState;
    // ... popula sprite
    g_gameState.spritesDirty = true;
    // persistent = false (default) - dirty será limpo e remarcado no _update()
}
```

**Benefícios:**

✅ **Módulos estáticos não precisam de `_update()`**
- `text_display_init()` marca persistent → dirty nunca é limpo → sempre renderiza
- Elimina chamadas desnecessárias de `_update()` todo frame

✅ **Módulos dinâmicos continuam funcionando**
- `sprite_update()` remarca dirty todo frame (persistent = false)
- `animation_update()` remarca dirty todo frame

✅ **Performance melhorada**
- Menos chamadas de função no main loop
- Menos writes na memória

### Implementação

**Fase 1: Adicionar flags ao GameState** (1 dia)
- [ ] Adicionar `bool persistent` a `TilemapState`, `TextLayerState`, `SpriteLayerState`
- [ ] Atualizar `SmsRenderBackend.GenerateEngineHeader()`

**Fase 2: Atualizar Engine_ClearDirtyFlags()** (1 dia)
- [ ] Modificar `SmsRenderBackend.GenerateEngineSource()`
- [ ] Adicionar checks `if (!state->X.persistent)` antes de limpar

**Fase 3: Atualizar templates** (2 dias)
- [ ] `text_display.c.rtrx` → Marca `persistent = true` no `_init()`
- [ ] Remover `_update()` de `text_display`
- [ ] Atualizar todos os targets (SMS, GG, SG-1000, ColecoVision, NES)

**Fase 4: Atualizar ModuleRenderer** (1 dia)
- [ ] Remover `text.display` de `modulesWithUpdate`

**Status:** Documentado - não implementado. Prioridade média (funciona mas é ineficiente).

---
