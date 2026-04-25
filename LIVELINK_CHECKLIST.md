# LiveLink Multi-Console Support Checklist

## 🎯 Consoles Suportados

### ✅ NES (Nintendo Entertainment System)
- [x] Tiles (2bpp, InterleaveMode.Tile)
- [x] Palette (64-color master palette)
- [x] Nametable (32x30)
- [x] Pattern Table selection (0x0000 BG / 0x1000 Sprites / Both)
- [x] Preview OK
- [x] Import OK

### 🟡 SNES (Super Nintendo)
- [x] Tiles (4bpp default, InterleaveMode.Line)
- [x] Palette (15-bit RGB, 256 colors)
- [ ] **BG Mode Detection** - Ler registrador $2105 para detectar 2bpp/4bpp/8bpp automaticamente
- [ ] Nametable (não implementado)
- [ ] **Preview com pontos coloridos aleatórios** - investigar causa
- [ ] Import OK

### ✅ SMS (Sega Master System)
- [x] Tiles (4bpp, InterleaveMode.Line)
- [x] Palette (6-bit RGB, 32 colors)
- [x] Nametable (32x28, 2 bytes/entry, 0x3800)
- [x] Preview OK
- [x] Import OK

### 🟡 Game Gear
- [x] Tiles (4bpp, InterleaveMode.Line)
- [x] Palette (12-bit RGB, 32 colors, 2 bytes/color)
- [x] Nametable (32x28, 2 bytes/entry, 0x3800)
- [ ] **Conversão de cores muito diferente** - verificar se 12-bit RGB está correto
- [ ] Preview OK (verificar)
- [ ] Import OK (verificar)

### 🟡 SG-1000 (Sega SG-1000)
- [x] Tiles (1bpp, InterleaveMode.Tile)
- [x] Palette (TMS9918 fixed 16-color palette)
- [ ] **Nametable** - Endereço incorreto (0x1800 retorna zeros, testando 0x3800)
- [x] Preview OK (tiles)
- [x] Tiles renderizam em branco/preto (fix temporário)
- [ ] **Color Table** - Implementar leitura da Color Table separada (0x2000, 32 bytes) para cores corretas

### 🟡 GB/GBC (Game Boy / Game Boy Color)
- [x] Tiles (2bpp, InterleaveMode.Line)
- [x] Palette (15-bit RGB)
- [ ] **VRAM Banking** - Adicionar UI para selecionar Banco 0 ou Banco 1
- [ ] Nametable (implementado?)
- [x] Preview OK
- [ ] **Tilemap vazio na importação** - investigar

### 🔴 GBA (Game Boy Advance)
- [ ] Tiles (não implementado)
- [ ] Palette (não implementado)
- [ ] Nametable (não implementado)
- [ ] Preview (não implementado)
- [ ] Import (não implementado)

### 🔴 WonderSwan / WonderSwan Color
- [ ] Tiles (não implementado)
- [ ] Palette (não implementado)
- [ ] Nametable (não implementado)
- [ ] Preview (não implementado)
- [ ] Import (não implementado)

### 🔴 PC Engine (TurboGrafx-16)
- [ ] Tiles (não implementado)
- [ ] Palette (não implementado)
- [ ] Nametable (não implementado)
- [ ] Preview (não implementado)
- [ ] Import (não implementado)

---

## 🚨 Issues Prioritários

### 1. 🟡 SG-1000 Tiles Pretos (PARCIALMENTE RESOLVIDO)
**Status**: ✅ Fix temporário aplicado  
**Descrição**: Tiles importavam todos pretos  
**Causa 1**: Pipeline não detectava 1bpp corretamente → ✅ Resolvido  
**Causa 2**: Paleta TMS9918 tem índice 0=transparente, 1=preto → 🟡 Fix temporário (usa branco/preto)  
**Solução Definitiva**: Implementar leitura da Color Table (0x2000, 32 bytes) que define foreground/background por grupo de tiles  
**Commits**: 69ee772, 5aea109

### 2. 🟡 SNES Preview com Pontos Coloridos
**Status**: Pendente  
**Descrição**: Preview mostra pontos coloridos aleatórios ao invés de tiles  
**Possível Causa**: BG mode incorreto ou decodificação de tiles errada  
**Próximos Passos**:
- [ ] Implementar leitura de registrador $2105 para detectar BG mode
- [ ] Verificar se InterleaveMode.Line está correto para todos os modos

### 3. 🟡 GBC Tilemap Vazio na Importação
**Status**: Pendente  
**Descrição**: Preview OK mas tilemap vazio após importação  
**Possível Causa**: Pipeline não está convertendo nametable corretamente  
**Próximos Passos**:
- [ ] Verificar se nametable está sendo capturado
- [ ] Verificar CaptureToImportedAssetPipeline para GB/GBC

### 4. 🟡 Game Gear Conversão de Cores
**Status**: Pendente  
**Descrição**: Cores muito diferentes do esperado  
**Possível Causa**: Conversão 12-bit RGB pode estar incorreta  
**Próximos Passos**:
- [ ] Verificar fórmula de conversão (atualmente: (valor & 0x0F) * 17)
- [ ] Comparar com cores reais do Game Gear

---

## 🎯 Features Pendentes

### SNES BG Mode Detection
- [ ] Ler registrador $2105 (BGMODE)
- [ ] Detectar 2bpp/4bpp/8bpp automaticamente
- [ ] Ajustar captura de tiles baseado no modo detectado

### GBC VRAM Banking
- [ ] Adicionar ComboBox na UI para selecionar Banco 0 ou Banco 1
- [ ] Modificar ReadVramAsync para aceitar parâmetro de banco
- [ ] Atualizar Lua script para suportar banking

### SG-1000 Color Table
- [ ] Implementar leitura da Color Table (0x2000, 32 bytes)
- [ ] Decodificar foreground/background colors por tile group
- [ ] Aplicar cores aos tiles monocromáticos

---

## 📝 Notas Técnicas

### Formatos de Paleta
- **NES**: 6-bit index → 64-color master palette
- **SNES**: 15-bit RGB (5-5-5), little-endian
- **SMS**: 6-bit RGB (2-2-2), format: 00BBGGRR
- **Game Gear**: 12-bit RGB (4-4-4), format: 0000BBBBGGGGRRRR, little-endian
- **SG-1000**: Fixed TMS9918 16-color palette (hardware-defined)
- **GB/GBC**: 15-bit RGB (5-5-5), little-endian

### Formatos de Tiles
- **NES**: 2bpp, InterleaveMode.Tile
- **SNES**: 2/4/8bpp, InterleaveMode.Line
- **SMS/GG**: 4bpp, InterleaveMode.Line
- **SG-1000**: 1bpp, InterleaveMode.Tile
- **GB/GBC**: 2bpp, InterleaveMode.Line

### Formatos de Nametable
- **NES**: 32x30, 1 byte/entry
- **SNES**: Varia por BG mode
- **SMS/GG**: 32x28, 2 bytes/entry, address 0x3800
- **SG-1000**: 32x24, 1 byte/entry, address 0x1800
