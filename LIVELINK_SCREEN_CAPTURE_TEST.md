# LiveLink Screen Capture - Test Checklist

## Status: ✅ Pronto para teste

### Componentes implementados:

1. ✅ **PaletteOptimizer.cs** - K-means clustering para otimização de paleta
   - SMS: 2 palettes × 16 cores (primeiras 4 preenchidas)
   - NES: 4 palettes × 4 cores
   - Atribuição automática de tiles para palettes

2. ✅ **ScreenToTilesConverter.cs** - Conversão de screen buffer para tiles
   - Divide imagem em tiles 8×8
   - Extrai cores únicas por tile
   - Gera nametable 1:1 (sem deduplicação)
   - Remapeia pixels para índices de palette

3. ✅ **LiveLinkWindow.xaml.cs** - Integração no botão CAPTURE SCREEN
   - Captura screen buffer via Mesen Lua
   - Converte para tiles + palette + nametable
   - Armazena em `_lastCapture` para importar

## Teste passo a passo:

### 1. Preparação
```bash
cd "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel"
dotnet build
dotnet run --project Retruxel
```

### 2. Abrir LiveLink
- Tools → LiveLink
- Ou abrir de um módulo que suporte capture

### 3. Conectar ao emulador
- SELECT ROM & CONNECT
- Escolher ROM NES (ex: Mega Man 2)
- Aguardar conexão

### 4. Capturar screen
- Deixar o jogo rodar até uma cena interessante
- Clicar **CAPTURE SCREEN**

### 5. Verificar logs esperados
```
> Starting screen capture...
> Requesting screen buffer...
> [Mesen] Sending: GET_SCREEN
> [Mesen] Waiting for screen buffer size...
> [Mesen] Expecting 327680 bytes of base64 data
> [Mesen] Read 65536/327681 bytes...
> [Mesen] Read 131072/327681 bytes...
> [Mesen] Read 196608/327681 bytes...
> [Mesen] Read 262144/327681 bytes...
> [Mesen] Read 327680/327681 bytes...
> [Mesen] Read complete: 327681 bytes from stream
> [Mesen] Decoding 327680 chars of base64...
> [Mesen] Decoded to 245760 bytes
> ✓ Received screen buffer: 245760 bytes
> Screen dimensions: 256×240 (expected 245760 bytes)
> ✓ Screen capture complete!
> Converting screen to tiles...
> ✓ Converted to 960 tiles, 16 colors
> Nametable: 32×30
> ✓ Ready to import!
```

### 6. Verificar preview
- Preview deve mostrar a tela capturada
- Botão IMPORT deve estar habilitado

### 7. Importar (se disponível)
- Clicar IMPORT
- Verificar se módulos são criados

## Possíveis problemas e soluções:

### ❌ "Blocking após 196608 bytes"
**Causa**: Socket timeout no Lua
**Solução**: ✅ Já corrigido (modo blocking temporário)

### ❌ "Screen-to-tiles conversion not yet implemented"
**Causa**: Código antigo ainda presente
**Solução**: ✅ Já substituído por ScreenToTilesConverter

### ❌ "Palette optimizer crashes"
**Causa**: Cores insuficientes ou k-means falha
**Solução**: Adicionar fallback para casos extremos

### ❌ "Preview mostra imagem errada"
**Causa**: Conversão RGBA → BGRA incorreta
**Solução**: Verificar ordem dos bytes no bitmap

## Testes adicionais:

### Teste 1: NES → NES (mesmo console)
- Capturar de ROM NES
- Importar para projeto NES
- Verificar se cores ficam idênticas

### Teste 2: NES → SMS (cross-console)
- Capturar de ROM NES
- Importar para projeto SMS
- Verificar se cores são convertidas para SMS palette
- Deve mostrar aviso sobre conversão

### Teste 3: SMS → SMS (mesmo console)
- Capturar de ROM SMS
- Importar para projeto SMS
- Verificar se palettes SMS são geradas corretamente

### Teste 4: Cena com muitas cores
- Capturar cena complexa (ex: title screen)
- Verificar se k-means agrupa cores similares
- Verificar se não ultrapassa limites (NES: 16 cores, SMS: 8 cores preenchidas)

## Métricas de sucesso:

✅ Captura completa sem timeout
✅ Conversão para tiles sem crash
✅ Palette otimizada gerada
✅ Preview renderizado corretamente
✅ Botão IMPORT habilitado
✅ Logs claros e informativos

## Próximos passos após teste:

1. **Se funcionar perfeitamente**:
   - Documentar no README
   - Adicionar ao changelog
   - Considerar release

2. **Se tiver bugs menores**:
   - Corrigir e testar novamente
   - Adicionar tratamento de erros

3. **Se tiver problemas de performance**:
   - Otimizar k-means (reduzir iterações)
   - Adicionar progress bar
   - Fazer conversão em background thread

## Notas:

- **K-means iterations**: Atualmente 20, pode reduzir para 10 se for lento
- **Tile deduplication**: Não implementado (nametable 1:1)
- **Palette limit enforcement**: Não implementado (pode gerar mais cores que o limite)
- **Cross-console color conversion**: Não implementado (usa cores RGB diretas)

Esses pontos podem ser melhorados em versões futuras.
