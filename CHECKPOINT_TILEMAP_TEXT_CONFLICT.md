# Checkpoint: Resolução de Conflito Tilemap + Text Display (GameState Architecture)

## Contexto da Sessão

Implementação da arquitetura GameState para SMS causou conflito entre módulos tilemap e text_display. Ambos renderizavam com tiles da fonte ao invés de seus respectivos tiles.

---

## Problema Identificado

### Sintoma Inicial
- **Tilemap + Text juntos**: Ambos renderizavam com tileset da fonte (letras)
- **Só Text**: Funcionava corretamente
- **Só Tilemap**: Tela preta (não renderizava)

### Causa Raiz
Múltiplos problemas técnicos no `SmsRenderBackend.cs` e arquitetura de renderização:

1. **SMS_loadTiles() com parâmetro errado**: Passava `tileDataSize / 32` (número de tiles), mas SMSlib espera **bytes**
2. **SMS_loadTileMap() vs SMS_loadTileMapArea()**: Usava função errada que só carregava 10 linhas
3. **SMS_initSprites() faltando**: Sprites acumulavam entre frames
4. **Conflito de nomes**: `SpriteState` existia no Engine e no módulo
5. **GameState não inicializado**: Valores aleatórios na memória causavam comportamento imprevisível

---

## Arquitetura: Antes vs Depois do GameState

### ANTES (Direct VRAM)
```c
void tilemap_0_init(void) {
    SMS_loadTiles(tiles, 0, 80);      // Escreve VRAM imediatamente
    SMS_loadTileMap(0, 0, map, 960);  // Escreve nametable imediatamente
}

void main(void) {
    SMS_autoSetUpTextRenderer();  // Carrega fonte 0-255
    tilemap_0_init();              // Sobrescreve tiles 0-79 UMA VEZ
    text_display_0_init();         // Usa fonte 80-255 (não sobrescrita)
    
    while(1) {
        SMS_waitForVBlank();
        // Nada aqui - sem re-render
    }
}
```
**Funcionava** porque cada módulo escrevia VRAM uma vez e nunca mais.

### DEPOIS (GameState + Deferred Rendering)
```c
void tilemap_0_init(void) {
    g_gameState.background.tileData = tiles;
    g_gameState.backgroundDirty = true;  // Marca para renderizar depois
}

void main(void) {
    // Inicializa GameState com zeros
    memset(&g_gameState, 0, sizeof(GameState));
    
    tilemap_0_init();              // Só marca dirty
    Engine_Render(&g_gameState);   // Renderiza agora
    Engine_ClearDirtyFlags();      // Limpa flags
    
    while(1) {
        SMS_waitForVBlank();
        Engine_Render(&g_gameState);  // Re-renderiza se dirty
    }
}
```
**Problema**: Se algo marcar `backgroundDirty` novamente, tiles são recarregados.

---

## Correções Implementadas

### 1. SMS_loadTiles() - Parâmetro Correto
**Arquivo**: `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`

**Antes**:
```csharp
sb.AppendLine("            SMS_loadTiles(state->background.tileData, ");
sb.AppendLine("                         state->background.startTile, ");
sb.AppendLine("                         state->background.tileDataSize / 32);");
```

**Depois**:
```csharp
sb.AppendLine("            SMS_loadTiles(state->background.tileData, ");
sb.AppendLine("                         state->background.startTile, ");
sb.AppendLine("                         state->background.tileDataSize);");
```

**Motivo**: SMSlib espera tamanho em **bytes**, não número de tiles.

---

### 2. SMS_loadTileMapArea() - Função Correta
**Arquivo**: `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`

**Antes**:
```csharp
sb.AppendLine("            SMS_loadTileMap(state->background.mapX, ");
sb.AppendLine("                           state->background.mapY, ");
sb.AppendLine("                           state->background.mapData, ");
sb.AppendLine("                           state->background.mapDataSize / sizeof(unsigned int));");
```

**Depois**:
```csharp
sb.AppendLine("            SMS_loadTileMapArea(state->background.mapX, ");
sb.AppendLine("                               state->background.mapY, ");
sb.AppendLine("                               state->background.mapData, ");
sb.AppendLine("                               state->background.mapWidth, ");
sb.AppendLine("                               state->background.mapHeight);");
```

**Motivo**: `SMS_loadTileMap()` carrega entries consecutivas (só 10 linhas). `SMS_loadTileMapArea()` carrega área retangular completa.

---

### 3. SMS_initSprites() Adicionado
**Arquivo**: `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`

**Antes**:
```csharp
sb.AppendLine("    if (state->spritesDirty) {");
sb.AppendLine("        for (unsigned char i = 0; i < state->sprites.count && i < 64; i++) {");
```

**Depois**:
```csharp
sb.AppendLine("    if (state->spritesDirty) {");
sb.AppendLine("        SMS_initSprites();");
sb.AppendLine("        for (unsigned char i = 0; i < state->sprites.count && i < 64; i++) {");
```

**Motivo**: SMSlib exige `SMS_initSprites()` antes de `SMS_addSprite()` em cada frame.

---

### 4. SpriteState → SpriteRenderState
**Arquivo**: `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`

**Antes**:
```csharp
sb.AppendLine("} SpriteState;");
sb.AppendLine("    SpriteState sprites[64];");
sb.AppendLine("            SpriteState* sprite = &state->sprites.sprites[i];");
```

**Depois**:
```csharp
sb.AppendLine("} SpriteRenderState;");
sb.AppendLine("    SpriteRenderState sprites[64];");
sb.AppendLine("            SpriteRenderState* sprite = &state->sprites.sprites[i];");
```

**Motivo**: Evita conflito com `SpriteState` do módulo Sprite.

---

### 5. GameState Inicialização com Zeros
**Arquivo**: `Plugins/CodeGens/main/sms/main.c.rtrx`

**Antes**:
```c
GameState g_gameState;

void main(void) {
    splash_show();
```

**Depois**:
```c
GameState g_gameState;

void main(void) {
    // Initialize game state to zero
    unsigned char* ptr = (unsigned char*)&g_gameState;
    unsigned int i;
    for (i = 0; i < sizeof(GameState); i++) {
        ptr[i] = 0;
    }

    splash_show();
```

**Motivo**: SDCC não suporta `= {0}`. Valores não inicializados causavam comportamento imprevisível.

---

### 6. Engine_Render() Após Cada Módulo Gráfico OnStart
**Arquivo**: `Retruxel.Core/Services/ModuleRenderer.cs`

**Modificação**: `GenerateEventCalls()` agora retorna objetos com flags:
```csharp
calls.Add(new Dictionary<string, object>
{
    ["call"] = $"    {baseName}_init();",
    ["isGraphicModule"] = isGraphic,
    ["isTextDisplay"] = isTextDisplay
});
```

**Módulos gráficos**: tilemap, sprite, text.display, palette, background, animation

**Arquivo**: `Plugins/CodeGens/main/sms/main.c.rtrx`

```c
// OnStart: Initialize modules
{{#each onStartCalls}}
{{this.call}}
{{#if this.isGraphicModule}}
    Engine_Render(&g_gameState);
    Engine_ClearDirtyFlags(&g_gameState);
{{/if}}
{{/each}}
```

**Motivo**: Garante que cada módulo gráfico renderiza imediatamente após init, evitando conflitos.

---

### 7. SMS_autoSetUpTextRenderer() Removido Temporariamente
**Arquivo**: `Plugins/CodeGens/main/sms/main.c.rtrx`

**Removido**:
```c
// Setup text renderer (must be called before any tile loading)
SMS_autoSetUpTextRenderer();
```

**Motivo**: Teste para verificar se tilemap funciona sozinho sem interferência da fonte.

---

## Estado Atual dos Testes

### ✅ Tilemap Sozinho (sem text_display)
**Esperado**: Tilemap completo renderiza com cores corretas
**Status**: Aguardando teste após correções

### ❌ Tilemap + Text Juntos
**Problema não resolvido**: Conflito de tiles 0-79
- `SMS_autoSetUpTextRenderer()` carrega fonte em tiles 0-255
- Tilemap com `startTile=0` sobrescreve tiles 0-79
- Texto que usa letras A-O (tiles 65-79) renderiza com tiles do tilemap

---

## Próximos Passos

### 1. Testar Tilemap Sozinho
Fazer build **sem módulo text_display** e verificar se renderiza corretamente.

### 2. Resolver Conflito Tilemap + Text
**Opções**:

**A) Ajustar startTile automaticamente**
- Detectar presença de text_display na cena
- Se presente, forçar `tilemap.startTile >= 256`
- Implementar em `CodeGenerator` ou `ModuleRenderer`

**B) Validação em tempo de design**
- Adicionar warning no SceneEditor quando tilemap usa startTile < 256 e há text_display
- Usuário ajusta manualmente

**C) Chamar SMS_autoSetUpTextRenderer() condicionalmente**
- Só chamar se houver módulo text_display na cena
- Chamar **antes** de qualquer módulo OnStart
- Tilemap deve usar startTile >= 256 obrigatoriamente

### 3. Implementar Solução Recomendada (Opção C)

**Modificar**: `Plugins/CodeGens/main/sms/codegen.json`
```json
{
  "variables": {
    "hasTextDisplay": {
      "from": "moduleFiles",
      "path": "sourceModuleId == 'text.display'"
    }
  }
}
```

**Modificar**: `Plugins/CodeGens/main/sms/main.c.rtrx`
```c
{{#if hasTextDisplay}}
    // Setup text renderer (loads font in tiles 0-255)
    SMS_autoSetUpTextRenderer();
{{/if}}

// OnStart: Initialize modules
{{#each onStartCalls}}
{{this.call}}
{{#if this.isGraphicModule}}
    Engine_Render(&g_gameState);
    Engine_ClearDirtyFlags(&g_gameState);
{{/if}}
{{/each}}
```

**Adicionar validação**: `TilemapModule` ou `CodeGenerator`
```csharp
if (scene.HasModule("text.display") && tilemap.StartTile < 256)
{
    warnings.Add("Tilemap startTile < 256 conflicts with text_display font (tiles 0-255). Set startTile >= 256.");
}
```

---

## Arquivos Modificados

1. `Retruxel.Target.SMS/Rendering/SmsRenderBackend.cs`
   - SMS_loadTiles: bytes ao invés de tile count
   - SMS_loadTileMapArea: função correta com width/height
   - SMS_initSprites: adicionado antes do loop
   - SpriteState → SpriteRenderState

2. `Retruxel.Core/Services/ModuleRenderer.cs`
   - GenerateEventCalls: retorna objetos com flags isGraphicModule/isTextDisplay

3. `Plugins/CodeGens/main/sms/main.c.rtrx`
   - GameState inicialização com zeros (loop manual)
   - Engine_Render após cada módulo gráfico OnStart
   - SMS_autoSetUpTextRenderer removido temporariamente

---

## Insights Importantes

### Por que funcionava antes do GameState?
Cada módulo escrevia VRAM **uma vez** durante init. Não havia re-render no loop VBlank. Conflitos de tile eram permanentes mas previsíveis.

### Por que falha com GameState?
Renderização é **deferred** e pode acontecer **múltiplas vezes**. Se `backgroundDirty` é marcado novamente (scroll, animação), tiles são recarregados, sobrescrevendo outros elementos.

### Lição arquitetural
**State-based rendering** exige **particionamento de VRAM**. Elementos que coexistem (tilemap + texto) não podem compartilhar a mesma região de tiles. Solução: tilemap usa tiles 256+, texto usa tiles 0-255.

---

## Referências Técnicas

### SMSlib Funções Relevantes
```c
void SMS_loadTiles(const void *src, unsigned int tilefrom, unsigned int size);
// size = bytes, não tile count

void SMS_loadTileMap(unsigned char x, unsigned char y, 
                     const unsigned int *src, unsigned int count);
// count = número de entries consecutivas

void SMS_loadTileMapArea(unsigned char x, unsigned char y,
                         const unsigned int *src,
                         unsigned char width, unsigned char height);
// Carrega área retangular

void SMS_autoSetUpTextRenderer(void);
// Carrega fonte ASCII em tiles 0-255

void SMS_initSprites(void);
// Limpa SAT - obrigatório antes de SMS_addSprite() em cada frame
```

### SMS Hardware Constraints
- **Tiles**: 448 slots (0-447)
- **Font**: Ocupa tiles 0-255 quando `SMS_autoSetUpTextRenderer()` é chamado
- **Nametable**: 32×28 tiles visíveis (32×32 total)
- **Sprites**: 64 máximo, 8 por linha

---

## Status Final
✅ Correções técnicas implementadas
⏳ Aguardando teste de tilemap sozinho
❌ Conflito tilemap+texto ainda não resolvido (requer decisão de design)

---

## Conversação Chave

### Descoberta do Problema Real

**Usuário**: "Então na situação antes do state deu certo porque todas as letras de RETRUXEL são acima de 79?"

**Resposta**: NÃO! A fonte ASCII mapeia caracteres assim:
- Tile 65 = 'A'
- Tile 69 = 'E' ← **dentro da faixa 0-79 sobrescrita!**
- Tile 82 = 'R'

"RETRUXEL" usa tiles 65-88. Se o tilemap carrega tiles 0-79, ele sobrescreve letras A-O (65-79).

**Então por que funcionava antes?** Deveria ter dado errado também!

**Usuário**: "Era aí que eu queria chegar, se fosse esse o problema também teria dado erro antes."

### Testes Reveladores

**Teste 1**: Só texto → ✅ Funciona
**Teste 2**: Só tilemap → ❌ Tela preta
**Teste 3**: Tilemap + texto → ❌ Ambos com tiles da fonte

**Conclusão**: O problema não era sobrescrita de tiles, mas **SMS_loadTiles() falhando completamente** devido a parâmetros errados.

### Insight Final

**Usuário**: "Eu só vou seguir para uma solução assim quando você me explicar porque antes de implementar state isso não ocorria."

A diferença fundamental:
- **Antes**: Cada módulo escrevia VRAM **uma vez** durante init. Sem re-render.
- **Depois**: Renderização **deferred** pode acontecer **múltiplas vezes** no loop VBlank.

O problema real não era a arquitetura GameState em si, mas **bugs de implementação** no `SmsRenderBackend.cs` que só foram expostos quando testamos renderização múltipla.
