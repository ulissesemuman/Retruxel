# LiveLink → Tilemap Editor Integration

Integração completa do pipeline de captura de assets do emulador para o editor de tilemaps.

## Fluxo Completo

```
┌─────────────┐
│  LiveLink   │ Usuário conecta ao emulador e captura VRAM
└──────┬──────┘
       │ CaptureResult (tiles, nametable, palette)
       ↓
┌──────────────────────────────┐
│ CaptureToImportedAssetPipeline │ Converte para formato padronizado
└──────┬───────────────────────┘
       │ ImportedAssetData
       ↓
┌──────────────────────────────┐
│ ImportedAssetToTilemapPipeline │ Salva tiles como PNG, prepara dados
└──────┬───────────────────────┘
       │ Dictionary<string, object>
       ↓
┌─────────────┐
│Tilemap Edit │ Carrega asset e aplica tilemap ao canvas
└─────────────┘
```

## Componentes

### 1. LiveLink (Retruxel.Tool.LiveLink)

**Responsabilidades:**
- Conectar ao emulador via TCP
- Capturar tiles, nametable e paleta da VRAM
- Converter dados brutos para `CaptureResult`
- Usar pipeline para converter `CaptureResult` → `ImportedAssetData`
- Retornar `ImportedAssetData` ao caller

**Botões:**
- **CONNECT** - Conecta ao emulador (lança automaticamente se necessário)
- **CAPTURE** - Captura dados da VRAM
- **IMPORT TO PROJECT** - Converte e retorna dados via pipeline

**Modo de operação:**
```csharp
var liveLinkInput = new Dictionary<string, object>
{
    ["mode"] = "capture",
    ["targetId"] = "sms"
};

var liveLinkWindow = (Window)liveLinkTool.CreateWindow(liveLinkInput);
if (liveLinkWindow.ShowDialog() == true)
{
    var moduleData = liveLinkWindow.ModuleData;
    var importedData = (ImportedAssetData)moduleData["importedAssetData"];
    // Usar importedData...
}
```

### 2. CaptureToImportedAssetPipeline (Retruxel.Tool.LiveLink)

**Input:** `CaptureResult`  
**Output:** `ImportedAssetData`

**Processamento:**
1. Copia tiles, nametable e palette
2. Detecta bits por pixel automaticamente (2bpp, 4bpp, 8bpp)
3. Converte nametable SMS (ushort[]) para tilemap (int[])
4. Extrai tile index dos atributos (bits 0-8)
5. Adiciona metadados de origem

**Opções:**
- `sourceEmulator` - ID do emulador (ex: "emulicious")
- `destinationTarget` - ID do target de destino (ex: "sms")

### 3. ImportedAssetToTilemapPipeline (Retruxel.Tool.TilemapEditor)

**Input:** `ImportedAssetData`  
**Output:** `Dictionary<string, object>` (formato do Tilemap Editor)

**Processamento:**
1. Gera ID único para o asset (ex: "livelink_tileset_1")
2. Cria bitmap com tiles arranjados em grid (16 tiles por linha para SMS)
3. Desenha cada tile no bitmap usando a paleta capturada
4. Salva bitmap como PNG em `assets/graphics/`
5. Cria `AssetEntry` e adiciona ao projeto
6. Retorna dados formatados para o editor

**Opções obrigatórias:**
- `project` - Instância do `RetruxelProject`
- `projectPath` - Caminho absoluto do projeto
- `targetId` - ID do target (opcional, usa `SourceTargetId` se não fornecido)

**Output:**
```csharp
{
    "tilesAssetId": "livelink_tileset_1",
    "mapWidth": 32,
    "mapHeight": 28,
    "mapData": int[],
    "palette": uint[],
    "asset": AssetEntry
}
```

### 4. Tilemap Editor (Retruxel.Tool.TilemapEditor)

**Botão:** `IMPORT FROM LIVELINK`

**Fluxo:**
1. Abre LiveLink em modo capture
2. Aguarda usuário capturar dados
3. Recebe `ImportedAssetData` do LiveLink
4. Usa `ImportedAssetToTilemapPipeline` para processar
5. Salva projeto (persiste novo asset)
6. Atualiza lista de assets
7. Auto-seleciona o novo asset
8. Carrega tilemap data no canvas
9. Oferece criar paleta a partir das cores capturadas

## Uso Passo a Passo

### No Tilemap Editor:

1. Clique em **IMPORT FROM LIVELINK**
2. LiveLink abre automaticamente
3. Selecione o emulador (Emulicious, Mesen, etc)
4. Clique em **CONNECT** (emulador será lançado se necessário)
5. Aguarde conexão ser estabelecida
6. Clique em **CAPTURE** para capturar VRAM
7. Clique em **IMPORT TO PROJECT**
8. LiveLink fecha e retorna ao Tilemap Editor
9. Asset é criado automaticamente e selecionado
10. Tilemap é carregado no canvas
11. Opcionalmente, crie paleta a partir das cores capturadas

## Conversões Automáticas

### Tiles
- **Entrada:** byte[][] (pixels decodificados, valores 0-15 para 4bpp)
- **Processamento:** Arranja tiles em grid 16x32, desenha usando paleta
- **Saída:** PNG salvo em `assets/graphics/livelink_tileset_N.png`

### Nametable
- **Entrada:** ushort[] (SMS format: tile index + attributes)
- **Processamento:** Extrai tile index (bits 0-8), ignora attributes
- **Saída:** int[] (tile IDs, -1 para vazio)

### Paleta
- **Entrada:** uint[] (cores ARGB 0xAARRGGBB)
- **Processamento:** Mantém formato ARGB
- **Saída:** uint[] (usado para desenhar tiles no PNG)

## Limitações

1. **Apenas layer 0:** Atualmente importa apenas para o layer atual (layer 0)
2. **Sem atributos:** Atributos SMS (flip, palette) são ignorados
3. **Paleta única:** Importa uma paleta global, não per-tile
4. **Tamanho fixo:** Usa dimensões capturadas (32x28 para SMS)
5. **Sem sprites:** Captura apenas background tiles

## Extensibilidade

Para adicionar suporte a outros consoles:

1. **Adicionar emulator connection** em `Retruxel.Tool.LiveLink/Emulators/`
2. **Criar script Lua** em `Retruxel.Tool.LiveLink/Resources/`
3. **Ajustar endereços VRAM** em `LiveLinkWindow.xaml.cs` (BtnCapture_Click)
4. **Atualizar `DecodePalette()`** para formato de paleta do console
5. **Ajustar `VramDecoder`** se formato de tiles for diferente

## Debugging

### LiveLink Log
Todas as operações são logadas no console do LiveLink:
- Tentativas de conexão
- Lançamento do emulador
- Carregamento de scripts Lua
- Solicitação de dados (tiles, nametable, palette)
- Recebimento e decodificação
- Conversão via pipeline

### Validação
`ImportedAssetData.IsValid()` valida:
- Tiles não vazios
- Dimensões válidas (width/height > 0)
- Tamanho de tilemap consistente (mapData.Length == mapWidth * mapHeight)
- Pixels por tile consistente (tile.Length == tileWidth * tileHeight)

### Erros Comuns

**"No data received from LiveLink"**
- LiveLink foi fechado sem clicar em IMPORT
- Solução: Capture dados e clique em IMPORT TO PROJECT

**"Invalid imported asset data"**
- Dados capturados estão incompletos ou corrompidos
- Solução: Verifique conexão com emulador e tente capturar novamente

**"Failed to load tileset"**
- PNG não foi salvo corretamente
- Solução: Verifique permissões de escrita em `assets/graphics/`

**"Emulator not configured"**
- Caminho do emulador não está configurado em Settings → LiveLink
- Solução: Configure o caminho do emulador nas configurações

## Próximos Passos

- [ ] Suporte para múltiplos layers
- [ ] Importar atributos SMS (flip, palette per-tile)
- [ ] Importar sprites (OAM)
- [ ] Captura de múltiplos frames (animações)
- [ ] Export de tilemap de volta para emulador (hot-reload)
- [ ] Suporte para NES, Game Boy, SNES, GBA
