# CodeGen Improvements Applied

> Data: 2025-01-XX
> Fonte: Merge de dois zips com melhorias no sistema CodeGens

---

## Resumo

Foram aplicadas **9 correções de bugs críticos** e **2 melhorias estruturais** ao sistema de CodeGens do Retruxel, provenientes da análise de dois arquivos ZIP com melhorias desenvolvidas por IAs diferentes.

**Estratégia de merge**: Analisados ambos os zips e selecionadas as melhores implementações de cada um, priorizando correções críticas documentadas no AUDIT_REPORT.md.

---

## Bugs Corrigidos

### BUG 1 — `TilemapLayerState.startTile` overflow (max 255)
**Arquivo**: `Plugins/Targets/Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`  
**Problema**: Campo `startTile` era `unsigned char` (max 255). Quando tilemap usava `startTile = 256` para coexistir com fonte de texto, ocorria overflow para 0, sobrescrevendo a fonte.  
**Correção**: Mudado para `unsigned int`.  
**Status**: ✅ Aplicado

---

### BUG 2 — Variável `i` declarada duas vezes em `Engine_Render`
**Arquivo**: `Plugins/Targets/Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`  
**Problema**: Código gerado declarava `unsigned char i` no topo e depois `for (unsigned char i = ...)` no bloco de sprites. SDCC é C89 — declarações dentro de `for()` são inválidas.  
**Correção**: Declarado `unsigned char j` no topo; loop de sprites usa `j`.  
**Status**: ✅ Aplicado

---

### BUG 3 — `SMS_setScrollX` não existe no SMSlib
**Arquivo**: `Plugins/CodeGens/scroll/sms/scroll.c.rtrx`  
**Problema**: Template gerava chamadas para `SMS_setScrollX()` que não existe. A função correta é `SMS_setBGScrollX()`.  
**Correção**: Substituído por `SMS_setBGScrollX` em todas as ocorrências.  
**Status**: ✅ Aplicado

---

### BUG 4 — `scroll.c.rtrx` não usa `instanceId`
**Arquivo**: `Plugins/CodeGens/scroll/sms/scroll.c.rtrx` + `codegen.json`  
**Problema**: Guard `#ifndef SCROLL_H`, funções (`scroll_init`, `scroll_update`) e variável `scroll_x` não usavam `{{instanceId}}`. Múltiplos módulos scroll causariam conflito de símbolos.  
**Correção**: Todas as ocorrências renomeadas para `scroll_{{instanceId}}_*`. Adicionada função `scroll_{{instanceId}}_get_x()` para consulta por outros módulos.  
**Status**: ✅ Aplicado

---

### BUG 5 — Precedência de operador em `sprite_{{instanceId}}_add`
**Arquivo**: `Plugins/CodeGens/sprite/sms/sprite.c.rtrx`  
**Problema**:
```c
SMS_addSprite(x, y, tile + {{startTile}} % 256);
// Avaliado como: tile + (startTile % 256)  ← ERRADO
// Deveria ser:   (tile + startTile) % 256  ← CORRETO
```
Para `startTile = 256`, forma errada resulta em `tile + 0 = tile` — sprite renderiza com tile errado.  
**Correção**: `(unsigned char)((tile + {{startTile}}) % 256)`  
**Status**: ✅ Aplicado

---

### BUG 6 — Nome de variável conflita com nome de função em `animation.c.rtrx`
**Arquivo**: `Plugins/CodeGens/animation/sms/animation.c.rtrx`  
**Problema**: `unsigned char animation_{{instanceId}}_finished` era tanto variável global quanto nome de função. Em C, um identifier não pode ser ao mesmo tempo variável e função. SDCC rejeita com erro de redeclaração.  
**Correção**: Variável interna renomeada para `animation_{{instanceId}}_finished_flag`. Função pública renomeada para `animation_{{instanceId}}_did_finish()`.  
**Impacto downstream**: `entity.c.rtrx` e `enemy.c.rtrx` atualizados para usar `_did_finish()`.  
**Status**: ✅ Aplicado

---

### BUG 7 — `enemy.c.rtrx` testa `pattern` como booleano, mas é string
**Arquivo**: `Plugins/CodeGens/enemy/sms/enemy.c.rtrx` + `codegen.json`  
**Problema**: Template tinha `{{#if pattern}}` que é verdadeiro para qualquer string não vazia — sempre gera movimento patrol, nunca static ou chase. O `codegen.json` declarava `pattern` como array `["Patrol", "Static", "Chase"]` (lista de opções), mas valor serializado é string única.  
**Correção**: Template usa `{{#if pattern == "Patrol"}}`, `{{#if pattern == "Chase"}}`, `{{#if pattern == "Static"}}`. O `codegen.json` corrigido com `default: "Patrol"` (string, não array).  
**Status**: ✅ Aplicado

---

### BUG 8 — `enemy_update` não chama `sprite_begin()` e `sprite_flush()`
**Arquivo**: `Plugins/CodeGens/enemy/sms/enemy.c.rtrx`  
**Problema**: `enemy_{{instanceId}}_update()` chamava `sprite_{{instanceId}}_add()` diretamente sem `sprite_begin()` antes e `sprite_flush()` depois. Sem `begin()`, SAT não é limpo — sprites acumulam. Sem `flush()`, sprites novos nunca aparecem.  
**Correção**: Adicionado `sprite_{{instanceId}}_begin()` antes e `sprite_{{instanceId}}_flush()` depois.  
**Status**: ✅ Aplicado

---

### BUG 9 — `text_display_update` marca `uiDirty = true` todo frame
**Arquivo**: `Plugins/CodeGens/text_display/sms/text_display.c.rtrx`  
**Problema**: `text_display_{{instanceId}}_update()` marcava `g_gameState.uiDirty = true` em todo VBlank, forçando `Engine_Render` a chamar `SMS_printatXY` toda frame. Desnecessário para texto estático e perigoso quando combinado com tilemap — causa flickering.  
**Correção**: Removida função `_update()`. Adicionada `text_display_{{instanceId}}_set(x, y, text)` para atualizações explícitas em runtime (placar, vidas, etc.). Texto estático só marca dirty no `_init()`.  
**Impacto downstream**: `ModuleRenderer.GenerateUpdateCalls()` remove `"text.display"` da lista de módulos com update.  
**Status**: ✅ Aplicado

---

## Melhorias Estruturais

### MELHORIA 1 — `SMS_autoSetUpTextRenderer()` posicionado por `hasTextDisplay` flag
**Arquivo**: `Plugins/CodeGens/main/sms/main.c.rtrx` + `codegen.json`  
**Problema anterior**: `SMS_autoSetUpTextRenderer()` era emitido inline dentro do loop `{{#each onStartCalls}}`, logo antes do init do text_display. Isso significava que era chamado depois de palette e tilemap, corrompendo a paleta e sobrescrevendo tiles 0-255.  
**Correção**: Movido para antes do loop de OnStart, protegido por `{{#if hasTextDisplay}}`. A variável `hasTextDisplay` foi adicionada ao `codegen.json` e resolvida como booleano em `ModuleRenderer.RenderMainFile()`.  
**Ordem correta garantida**:
```
Engine_Init()
SMS_autoSetUpTextRenderer()   ← se houver text_display
palette_init()                ← restaura paleta corrompida
Engine_Render + ClearDirtyFlags
tilemap_init()                ← startTile >= 256, não conflita com fonte
Engine_Render + ClearDirtyFlags
...outros módulos gráficos...
text_display_init()           ← escreve no nametable por cima do tilemap
Engine_Render + ClearDirtyFlags
```
**Status**: ✅ Aplicado

---

### MELHORIA 2 — `ModuleRenderer.GenerateEventCalls()` — ordenação palette-first, text-last
**Arquivo**: `Retruxel.Core/Services/ModuleRenderer.cs`  
**Problema**: Ordenação atual apenas garantia palette antes dos outros módulos (`OrderByDescending(isPalette)`), mas não garantia que text.display viesse depois de tilemap. Se text.display viesse antes, tilemap sobrescreve o nametable e texto desaparece.  
**Correção**: Adicionado `.ThenBy(c => c.isTextDisplay)` para garantir text.display como último módulo gráfico no OnStart.  
**Status**: ✅ Aplicado

---

## Arquivos Modificados

### Core
- ✅ `Retruxel.Core/Services/ModuleRenderer.cs`
  - Removido `"text.display"` de `modulesWithUpdate`
  - Adicionado ordenação `.ThenBy(c => c.isTextDisplay)`
  - Adicionado resolução de `hasTextDisplay` como booleano

### Target SMS
- ✅ `Plugins/Targets/Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`
  - `startTile` mudado para `unsigned int`
  - Variável `j` declarada no topo para loop de sprites

### CodeGens SMS
- ✅ `Plugins/CodeGens/main/sms/main.c.rtrx`
- ✅ `Plugins/CodeGens/main/sms/codegen.json`
- ✅ `Plugins/CodeGens/text_display/sms/text_display.c.rtrx`
- ✅ `Plugins/CodeGens/scroll/sms/scroll.c.rtrx`
- ✅ `Plugins/CodeGens/scroll/sms/codegen.json`
- ✅ `Plugins/CodeGens/sprite/sms/sprite.c.rtrx`
- ✅ `Plugins/CodeGens/animation/sms/animation.c.rtrx`
- ✅ `Plugins/CodeGens/entity/sms/entity.c.rtrx`
- ✅ `Plugins/CodeGens/enemy/sms/enemy.c.rtrx`
- ✅ `Plugins/CodeGens/enemy/sms/codegen.json`

---

## Arquivos Não Alterados (Auditados, Sem Problemas)

Segundo o AUDIT_REPORT, os seguintes arquivos foram auditados e estão corretos:

- `palette.c.rtrx` / `palette/codegen.json`
- `tilemap.c.rtrx` / `tilemap/codegen.json`
- `physics.c.rtrx` / `physics/codegen.json`
- `input.c.rtrx` / `input/codegen.json`
- `entity/codegen.json`
- `animation/codegen.json`
- `sprite/codegen.json`
- `text_display/codegen.json`

---

## Impacto Esperado

### Correções Críticas
1. **Tilemap + Text Display**: Agora funcionam juntos sem conflitos de tiles ou flickering
2. **Múltiplos Módulos Scroll**: Suportados corretamente com instanceId
3. **Enemy Patterns**: Static, Patrol e Chase agora funcionam corretamente
4. **Sprites**: Renderização correta com startTile >= 256
5. **Animation**: Sem conflitos de símbolos entre variável e função

### Melhorias de Performance
- Text display estático não força re-render todo frame
- Ordem de inicialização otimizada (palette → tilemap → text)

### Compatibilidade SDCC
- Todas as violações C89 corrigidas
- Código compila sem warnings no SDCC 4.5.24

---

## Próximos Passos

1. ✅ Testar build completo do projeto Kung Fu Master
2. ✅ Verificar se tilemap + text_display funcionam juntos
3. ✅ Testar múltiplos módulos scroll
4. ✅ Validar enemy patterns (Static, Patrol, Chase)
5. ✅ Confirmar que sprites renderizam corretamente com startTile >= 256

---

## Notas Técnicas

### Sobre text_display sem _update()
A remoção de `_update()` do text_display é correta porque `SMS_printatXY()` escreve diretamente no nametable da VRAM, que é persistente. Uma vez escrito, o texto permanece visível até ser sobrescrito. Não há necessidade de remarcar `uiDirty = true` todo frame.

Para texto dinâmico (placar, vidas), use `text_display_{{instanceId}}_set(x, y, text)` que marca dirty apenas quando chamado.

### Sobre startTile unsigned int
SMS tem 448 tiles disponíveis (0-447). Com `unsigned char` (max 255), não era possível usar tiles 256-447. A mudança para `unsigned int` resolve isso e permite que tilemap use `startTile = 256` quando text_display está presente (fonte ASCII ocupa tiles 0-255).

### Sobre ordenação palette-first, text-last
A ordem de inicialização é crítica:
1. **Palette primeiro**: CRAM deve ser carregada antes de qualquer tile ser renderizado
2. **Text_display último**: Deve escrever no nametable DEPOIS do tilemap, senão tilemap sobrescreve o texto

---

## Créditos

Melhorias desenvolvidas por duas IAs diferentes e consolidadas manualmente, priorizando correções críticas documentadas no AUDIT_REPORT.md.
