# TilemapEditorWindow Refactoring Plan

## Status: 📋 PLANNED

**Current file size**: 1850 lines
**Target**: Reduce to ~400-500 lines (70-75% reduction)

## Analysis

O TilemapEditorWindow é um arquivo grande com múltiplas responsabilidades:
- Gerenciamento de UI (inicialização, configuração)
- Gerenciamento de tileset (carregar, renderizar, extrair tiles)
- Gerenciamento de tilemap (pintar, limpar, preencher)
- Gerenciamento de paleta (criar, editar, aplicar)
- Gerenciamento de assets (importar, otimizar)
- Gerenciamento de camadas (layers)
- Exportação (PNG, Base64)
- Integração com LiveLink
- Zoom e visualização

## Proposed Structure

### Helper Classes (3)
1. **TilemapData.cs** - Estrutura de dados do tilemap (layers, dimensões)
2. **TilesetRenderer.cs** - Renderização de tileset e extração de tiles
3. **TilemapSerializer.cs** - Conversão Base64 ↔ int[]

### Partial Classes (8)
1. **TilemapEditorWindow_Initialization.cs** - InitializeUI, LoadModuleData, ConfigurePaletteUI
2. **TilemapEditorWindow_Tileset.cs** - LoadTilesetImage, PopulateTilesetGrid, ExtractTile, UpdateTileSelection
3. **TilemapEditorWindow_Canvas.cs** - RenderCanvas, PaintTile, Canvas_Mouse* events
4. **TilemapEditorWindow_Layers.cs** - Gerenciamento de camadas (CmbLayers_SelectionChanged)
5. **TilemapEditorWindow_Palette.cs** - OpenPaletteEditor, CmbPalette_*, BtnEditPalette_Click
6. **TilemapEditorWindow_Assets.cs** - LoadAssets, BtnImportAsset_Click, CmbTilesetAsset_SelectionChanged
7. **TilemapEditorWindow_Tools.cs** - BtnOptimize_Click, BtnExportPng_Click, BtnImportFromLiveLink_Click
8. **TilemapEditorWindow_Actions.cs** - BtnSave_Click, BtnClear_Click, BtnFill_Click, Zoom buttons

## Implementation Order

1. ✅ Create refactoring plan
2. ⏳ Create helper classes (TilemapData, TilesetRenderer, TilemapSerializer)
3. ⏳ Create partial classes (8 files)
4. ⏳ Remove duplicate code from main file
5. ⏳ Test compilation
6. ⏳ Test functionality

## Benefits

- Redução de ~70% no tamanho do arquivo principal
- Separação clara de responsabilidades
- Facilita manutenção e adição de features
- Código mais testável
- Reutilização de helpers em outros editores

## Notes

- Todas as classes parciais devem estar no namespace `Retruxel.Tool.TilemapEditor`
- Helper classes em `Retruxel.Tool.TilemapEditor.Helpers`
- Manter compatibilidade com interface existente (ModuleData, LoadModuleData)
