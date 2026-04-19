# Retruxel Tools - Nova Estrutura

## Estrutura de Pastas

```
Retruxel/
├── Targets/                    (TODO: mover projetos target para cá)
│   ├── Retruxel.Target.SMS/
│   ├── Retruxel.Target.NES/
│   ├── Retruxel.Target.GG/
│   ├── Retruxel.Target.SG1000/
│   └── Retruxel.Target.ColecoVision/
│
└── Tools/
    ├── Retruxel.Tool.AssetImporter/        ✅ Implementado
    ├── Retruxel.Tool.FontImporter/         ✅ Implementado
    ├── Retruxel.Tool.PixelArtEditor/       🔲 Stub
    ├── Retruxel.Tool.AudioEditor/          🔲 Stub
    ├── Retruxel.Tool.ConstraintAnalyzer/   🔲 Stub
    ├── Retruxel.Tool.TilePacker/       🔲 Stub
    ├── Retruxel.Tool.CompressionEngine/    🔲 Stub
    ├── Retruxel.Tool.BankManager/          🔲 Stub
    ├── Retruxel.Tool.HardwareSimulator/    🔲 Stub
    ├── Retruxel.Tool.VisualScripting/      🔲 Stub
    ├── Retruxel.Tool.HybridCodeEditor/     🔲 Stub
    ├── Retruxel.Tool.CodeAnalyzer/         🔲 Stub
    ├── Retruxel.Tool.LiveLink/             🔲 Stub
    ├── Retruxel.Tool.Collaboration/        🔲 Stub
    ├── Retruxel.Tool.AutoPorting/          🔲 Stub
    ├── Retruxel.Tool.IntelligenceEngine/   🔲 Stub
    ├── Retruxel.Tool.PlaytestBot/          🔲 Stub
    ├── Retruxel.Tool.CreationAssistant/    🔲 Stub
    └── Retruxel.Tool.MarketingKit/         🔲 Stub
```

## Interface ITool

Localização: `Retruxel.SDK/ITool.cs`

```csharp
public interface ITool
{
    string ToolId { get; }
    string DisplayName { get; }
    string Description { get; }
    string IconPath { get; }
    string Category { get; }
    string MenuPath { get; }
    string? Shortcut { get; }
    bool RequiresProject { get; }
    string? TargetId { get; }
    bool Execute(IToolContext context);
}

public interface IToolContext
{
    Project? CurrentProject { get; }
    ITarget? ActiveTarget { get; }
    IServiceProvider Services { get; }
}
```

## Ferramentas Implementadas

### 1. Asset Importer
- **ID**: `retruxel.tool.assetimporter`
- **Categoria**: Import
- **Atalho**: Ctrl+Shift+I
- **Descrição**: Importa imagens PNG como tiles ou sprites
- **Status**: ✅ Totalmente funcional

### 2. Font Importer
- **ID**: `retruxel.tool.fontimporter`
- **Categoria**: Import
- **Atalho**: Ctrl+Shift+F
- **Descrição**: Importa fontes TrueType/OpenType como spritesheets
- **Status**: ✅ Totalmente funcional

## Ferramentas Planejadas (Stubs)

### Graphics
- **Pixel Art Editor** - Editor integrado de pixel art

### Audio
- **Audio Editor** - Editor de música e efeitos sonoros para chips de som retro

### Analysis
- **Hardware Constraint Analyzer** - Analisa e valida restrições de hardware
- **Code Analyzer** - Analisa código gerado para otimizações

### Optimization
- **Tilemap Reducer** - Otimiza tilemaps removendo tiles duplicados
- **Compression Engine** - Comprime assets usando algoritmos retro (RLE, LZ77)

### Advanced
- **Bank Manager** - Gerencia banking de ROM e mapeamento de memória
- **Hardware Tricks Simulator** - Simula efeitos de hardware (raster, sprite multiplexing)
- **Hybrid Code Editor** - Permite edição de código C/assembly junto com módulos visuais

### Logic
- **Visual Scripting** - Sistema de scripting visual baseado em grafos

### Debug
- **Live Link** - Conexão em tempo real com hardware/emulador para debugging

### Collaboration
- **Real-Time Collaboration** - Sistema de colaboração em tempo real

### Utilities
- **Auto-Porting Assistant** - Auxilia no porte de projetos entre plataformas

### AI
- **Intelligence Engine** - Analisador com IA para sugestões de código
- **Playtest Bot** - Bot automatizado para testes de jogo
- **Creation Assistant** - Assistente com IA para geração de conteúdo

### Export
- **Marketing Kit** - Gera materiais promocionais (screenshots, GIFs, press kit)

## Próximos Passos

1. ✅ Criar interface ITool no SDK
2. ✅ Criar stubs para todas as ferramentas planejadas
3. ✅ Migrar AssetImporter e FontImporter para novos projetos
4. ⏳ Mover projetos Target para pasta Targets/
5. ⏳ Implementar ToolDiscoveryService no Core
6. ⏳ Atualizar MainWindow para carregar tools dinamicamente
7. ⏳ Remover Retruxel.Tools antigo
8. ⏳ Atualizar build para copiar DLLs para /tools/

## Notas

- Plugins de terceiros não fazem parte do projeto do Visual Studio
- Plugins são conectados na versão de distribuição
- Todos os stubs implementam ITool mas lançam NotImplementedException
- AssetImporter e FontImporter são WPF (net10.0-windows)
- Demais stubs são net10.0 padrão (sem WPF)
