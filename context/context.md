# Retruxel — Contexto de Sessão
> Atualizado: 2026-05-31 | Versão do projeto: 0.8.0-alpha

---

## Visão Geral

IDE visual para desenvolvimento de jogos retro, inspirado no GB Studio. Usuário coloca módulos no canvas, configura via UI auto-gerada, e o Retruxel cuida de codegen, compilação e ROM — sem terminal, sem Makefile.

**Fluxo de build:**
`.rtrxproject` → `CodeGenerator` → `.c/.h` → SDCC → ihx2sms → `.sms ROM`

**Caso de uso primário:** Port do Kung Fu Master (NES → SMS).

---

## Stack

- **.NET 10 / C# 13 / WPF** (`net10.0-windows`)
- **SkiaSharp 3.116.1** — renderização de tiles/sprites no editor
- **SDCC 4.5.24** — compilador C para SMS/GG/SG-1000/ColecoVision
- **SMSlib / devkitSMS** — runtime SMS embutido no toolchain
- **cc65 / neslib** — toolchain NES embutido
- Solution: `Retruxel.slnx` (formato VS 2022+)

---

## Estrutura da Solution

```
Retruxel/                    ← WPF shell (startup)
Retruxel.Core/               ← Interfaces, models, services
Retruxel.SDK/                ← Re-exporta Core para plugins
Retruxel.Modules/            ← Módulos padrão portáveis
Retruxel.Toolchain/          ← Adaptador de toolchain
Retruxel.Emulation/          ← Integração LibRetro
Plugins/
  Targets/
    Retruxel.Target.SMS/     ← ✅ Ativo (~60%)
    Retruxel.Target.NES/     ← ✅ Ativo (~5%)
    Retruxel.Target.GG/      ← 🟡 Scaffolding
    Retruxel.Target.SG1000/  ← 🟡 Scaffolding
    Retruxel.Target.ColecoVision/ ← 🟡 Scaffolding
  Tools/
    Retruxel.Tool.TilemapEditor/
    Retruxel.Tool.SpriteEditor/
    Retruxel.Tool.AssetImporter/
    Retruxel.Tool.PaletteEditor/
    Retruxel.Tool.LiveLink/
    Retruxel.Tool.TextArrayEditor/
    Retruxel.Lib.PaletteHelpers/
    Retruxel.Lib.ImageProcessing/
  CodeGens/
    entity/{sms,gg,sg1000,nes}/   ← codegen.json + entity.c.rtrx
    sprite/{sms,gg,sg1000,coleco}/
    plane/{sms,gg,sg1000,coleco,nes}/
    scene/{sms}/
    main/{sms,nes}/
    animation/, physics/, input/, scroll/, hud/, ...
```

---

## Modelo de Dados — Entidades Relevantes

### `EntityData` (em `SceneData.Entities`)
```csharp
EntityId      string   // GUID da instância
Label         string   // nome da variante (ex: "Grunt")
EntityType    string   // tipo (ex: "enemy_grunt") → seleciona CodeGen
SpriteAssetId string   // asset compartilhado entre variantes do mesmo tipo
PaletteSlot   int      // slot de paleta desta variante (0 ou 1 no SMS)
StartTileX    int      // posição X inicial em tiles
StartTileY    int      // posição Y inicial em tiles
WidthTiles    int      // largura em tiles 8×8 (compartilhado pelo tipo)
HeightTiles   int      // altura em tiles 8×8 (compartilhado pelo tipo)
Visible       bool
ModuleOverrides List<ProjectModuleData>
State         JsonElement
```

**Modelo de variantes:** múltiplas `EntityData` com o mesmo `EntityType` compartilham
`SpriteAssetId`, `WidthTiles`, `HeightTiles`. Cada variante tem seu próprio `PaletteSlot`
e posição. A árvore agrupa por `EntityType`.

### `SceneData`
```
PaletteSlots  List<PaletteSlotData>   // slot 0 = BG, slot 1 = Sprite (SMS)
Planes        List<PlaneData>          // planos de hardware (SMS: 1 plano "bg")
Entities      List<EntityData>         // entities agrupadas por EntityType na UI
TextArrays    List<TextArrayData>
ModuleOverrides List<ProjectModuleData>
```

### `PlaneData` / `PlaneLayerData`
- `PlaneData.PaletteSlot` — paleta do plano inteiro
- `PlaneLayerData` — sem `PaletteSlot` (pertence ao plano, não à layer)
- `TileEntry.PaletteSlot` — override por tile (SMS: 0 ou 1)

---

## Pipeline de Build

### Ordem de execução em `CodeGenerator.GenerateAsync()`
1. `VramAllocator.Allocate(scene, target, assets)` → `VramAllocation` (TileOffsets por assetId)
2. `SatAllocator.Allocate(scene, target)` → `IReadOnlyDictionary<entityId, satSlot>`
3. Resultados injetados como `globalVariables`: `vramOffset_{assetId}`, `satIndex_{entityId}`
4. Registro de módulos: project-level → scene overrides → plane layers → **entities (trigger: OnVBlank)** → text arrays → legacy elements
5. CodeGen por módulo: ModuleRenderer (declarativo) → ITarget.GenerateCodeForModule → IModule.GenerateCode
6. `RenderSceneFiles` → `scene_{name}.c/.h` (inclui `entity_N_init()`)
7. Engine runtime, system files, main.c

### Triggers
- `OnStart` → `_init()` calls (via `onStartCalls` no main.c)
- `OnVBlank` → `_update()` calls (via `onVBlankCalls` no main.c)
- Entities usam `OnVBlank` — init é chamado em `scene_init()`, não no main

### `ModuleRenderer.Render()` — variáveis injetadas para entities
```
instanceId                int     — contador global por moduleId
isFirstInstance           bool    — true se primeira instância com este spriteAssetId
isFirstInstanceWithAsset  bool    — true: define o array de tiles
isNotFirstInstanceWithAsset bool  — true: emite extern
isFirstInstanceWithoutAsset bool  — true: usa placeholder
useFirstHalfTiles         int     — 1 se startTile < 256, 0 se >= 256
```
`_emittedSpriteAssets` (HashSet) rastreia quais assets já foram emitidos — reset em `ResetState()`.

---

## CodeGen Templates — Regras Críticas

### TemplateEngine — limitações
- **Não suporta condicionais aninhadas** (`{{#if A}}...{{#if B}}...{{/if}}...{{/if}}`)
- O regex `{{#if}}...{{/if}}` é non-greedy — o `{{/if}}` mais próximo fecha o `{{#if}}` mais interno
- **Solução:** pré-computar variáveis compostas no C# (ex: `isFirstInstanceWithAsset`) e usar condicionais planas no template
- Suporta: `{{#if}}`, `{{#ifnot}}`, `{{#each}}`, `{{var}}`, `{{a * b}}`, `{{a > b}}`

### entity/sms/entity.c.rtrx (v2.5.0)
- Usa `isFirstInstanceWithAsset` / `isNotFirstInstanceWithAsset` / `isFirstInstanceWithoutAsset`
- Usa `useFirstHalfTiles` (pré-computado) em vez de `{{#if startTile >= 256}}`
- Array de tiles emitido apenas uma vez por `spriteAssetId` — variantes usam `extern`
- `SMS_loadTiles` e `SMS_useFirstHalfTilesforSprites` apenas na primeira instância

### scene/sms/scene.c.rtrx
- Inclui headers de entities (`entityInits`)
- Chama `entity_N_init()` dentro de `scene_init()` (após plane init)
- `SMS_loadBGPalette` e `SMS_loadSpritePalette` sempre chamados (mesmo sem módulos gráficos)

---

## Árvore de Entities — Estrutura Visual

```
▼ ENTITIES                              [+]
  ▼ enemy_grunt   ✏ + ✕               ← EntityType: asset + dimensões
      Grunt  P0  👁 ⚙ ✕               ← variante: PaletteSlot=0
      Guard  P1  👁 ⚙ ✕               ← variante: PaletteSlot=1
  ▼ player        ✏ + ✕
      Player P1  👁 ⚙ ✕
```

- `+` na seção ENTITIES → dialog pede EntityType → cria tipo + primeira variante
- `+` no nó pai → `AddEntityVariant()` → copia asset/dimensões, próximo palette slot
- `✕` no nó pai → `RemoveEntityType()` → remove todas as variantes
- Click no nó pai → propriedades do tipo (Sprite Asset, Width, Height)
- Click na variante → propriedades da variante (Name, Palette Slot, Start X/Y)

---

## Painel de Propriedades — Entity

**Seção "ENTITY TYPE — {type}":**
- Sprite Asset → propaga para todas as variantes do mesmo tipo
- Width (tiles) → propaga para todas as variantes
- Height (tiles) → propaga para todas as variantes

**Seção "VARIANT":**
- Name → `entity.Label`
- Palette Slot → `entity.PaletteSlot`
- Start X (tile) → `entity.StartTileX`
- Start Y (tile) → `entity.StartTileY`

---

## Bugs Corrigidos Nesta Sessão

| # | Bug | Arquivo(s) |
|---|---|---|
| 1 | `entity_.h` — instanceId vazio porque `EntityModule.SingletonPolicy = Global` | `EntityModule.cs` |
| 2 | `};` solto no .c gerado — condicional aninhada no template | `entity.c.rtrx` |
| 3 | `scene_main_init()` sem `#include "scene_main.h"` no main.c | `main.c.rtrx` |
| 4 | `entity_update()` nunca chamado — trigger era `OnStart`, mudado para `OnVBlank` | `CodeGenerator.cs` |
| 5 | `entity_init()` nunca chamado — não estava em `scene_init()` | `ModuleRenderer.cs`, `scene.c.rtrx` |
| 6 | Paleta de sprites não persistia — `OpenPaletteSlotEditor` não chamava `MarkDirty()` | `SceneEditorView_Properties.cs` |
| 7 | `EventCallGenerator` gerava `_init()` para todos os triggers — OnVBlank precisa de `_update()` | `EventCallGenerator.cs` |
| 8 | Array de tiles duplicado quando duas variantes compartilham o mesmo asset | `ModuleRenderer.cs`, `entity.c.rtrx` |

---

## Regras de Design (Invioláveis)

- **0px border-radius** em todos os componentes — exceto `Border` externo de janelas modais (6px)
- **Sem linhas divisórias 1px** — separação por tonal shift
- **Grid de 8px** — sem exceções
- Texto corpo: `BrushOnSurfaceVariant` (`#adaaaa`) — nunca branco puro
- `StackPanel.Spacing` não existe no WPF — usar `Margin` nos filhos
- `BrushSurfaceDim` / `BrushSurfaceContainerLowest` não existem — usar `BrushSurface` (`#0e0e0e`)
- **Nunca renderizar PNG em runtime** — usar `MapIndex` (`byte[]` de índices de paleta)
- **Nunca usar `VramRegionId`** — usar `PlaneId` + `VramAllocator`
- **Nunca adicionar `PaletteSlot` em `PlaneLayerData`** — pertence a `PlaneData` ou `TileEntry`

---

## Paleta de Cores (Tokens WPF)

| Token | Hex | Uso |
|---|---|---|
| `BrushSurface` | `#0e0e0e` | Fundo base (mais escuro disponível) |
| `BrushSurfaceContainerLow` | `#131313` | Sidebar, header |
| `BrushSurfaceContainerHigh` | `#1e1e1e` | Cards, painéis |
| `BrushSurfaceContainerHighest` | `#262626` | Elementos interativos |
| `BrushPrimary` | `#8eff71` | Ação principal, build, sucesso |
| `BrushTertiary` | `#81ecff` | Informação |
| `BrushOnSurface` | `#ffffff` | Texto principal |
| `BrushOnSurfaceVariant` | `#adaaaa` | Texto corpo |
| `BrushError` | `#ff4444` | Erros |
| `BrushWarning` | `#ffaa00` | Avisos |
| `BrushSuccess` | `#8eff71` | Sucesso (= Primary) |

---

## SMS — Especificações Técnicas Relevantes

- VRAM: 16KB total → 14336 bytes para tiles (448 tiles × 32 bytes)
- SAT: 64 sprites máx na tela, 8 por scanline
- Paletas: 2 slots (slot 0 = BG, slot 1 = Sprite) × 16 cores cada
- `SpritePalettes = 2` → máximo 2 variantes de cor de sprite visíveis simultaneamente
- VRAM half: tiles 0-255 → `SMS_useFirstHalfTilesforSprites(1)`, tiles 256-511 → `(0)`
- `SMS_initSprites()` + `SMS_copySpritestoSAT()` devem ser chamados a cada frame

---

## Próximos Passos Sugeridos

- [ ] Testar build com 2 variantes do mesmo EntityType (Grunt + Guard) — verificar extern/define
- [ ] Implementar `ShowEntityPickerDialog` com lista de tipos existentes (reusar tipo já criado)
- [ ] Preview de entity no canvas usando `MapIndex` em vez de PNG direto
- [ ] Validação de `SpritePalettes` no editor — avisar se variantes excedem slots disponíveis
- [ ] `enemy` module CodeGen — análogo ao `entity` mas com comportamento de IA
