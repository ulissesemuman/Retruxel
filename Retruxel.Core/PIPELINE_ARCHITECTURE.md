# Asset Pipeline System

Sistema de conectores modulares para processamento e conversão de assets entre ferramentas do Retruxel.

## Arquitetura

```
┌─────────────┐
│  LiveLink   │ Captura VRAM do emulador
└──────┬──────┘
       │ CaptureResult
       ↓
┌─────────────────────────────┐
│ CaptureToImportedAssetPipeline │
└──────┬──────────────────────┘
       │ ImportedAssetData
       ↓
┌─────────────┐
│Import Assets│ Processa e ajusta formato
└──────┬──────┘
       │ ImportedAssetData
       ↓
┌─────────────┐
│Tilemap Edit │ Aplica ao canvas
└─────────────┘
```

## Componentes Principais

### 1. ImportedAssetData (Retruxel.Core/Models)

Formato intermediário padronizado para troca de dados entre ferramentas.

**Propriedades:**
- `Tiles` - Tiles decodificados (byte[][])
- `TilemapData` - Índices de tiles (int[])
- `Palette` - Cores ARGB (uint[])
- `TileWidth/Height` - Dimensões do tile
- `MapWidth/Height` - Dimensões do mapa
- `BitsPerPixel` - Profundidade de cor
- `SourceTargetId` - Console de origem
- `DestinationTargetId` - Console de destino (opcional)
- `Metadata` - Dados adicionais

**Métodos:**
- `IsValid(out string? errorMessage)` - Valida consistência dos dados
- `GetSummary()` - Retorna resumo para debug/log

### 2. IAssetPipeline (Retruxel.Core/Interfaces)

Interface para estágios de pipeline que podem ser encadeados.

```csharp
public interface IAssetPipeline
{
    string PipelineId { get; }
    string DisplayName { get; }
    string Description { get; }
    Type InputType { get; }
    Type OutputType { get; }
    
    object Process(object input, Dictionary<string, object>? options = null);
    bool CanAccept(Type inputType);
    bool CanProduce(Type outputType);
}
```

### 3. AssetPipelineBase<TInput, TOutput>

Classe base abstrata para facilitar implementação de pipelines.

```csharp
public abstract class AssetPipelineBase<TInput, TOutput> : IAssetPipeline<TInput, TOutput>
{
    public abstract string PipelineId { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract TOutput ProcessTyped(TInput input, Dictionary<string, object>? options = null);
}
```

### 4. PipelineRegistry (Retruxel.Core/Services)

Registro e descoberta automática de pipelines.

**Métodos:**
- `Register(IAssetPipeline)` - Registra um pipeline
- `GetPipeline(string id)` - Busca pipeline por ID
- `GetPipelinesForInput(Type)` - Busca pipelines que aceitam tipo de entrada
- `GetPipelinesForOutput(Type)` - Busca pipelines que produzem tipo de saída
- `FindPipelineChain(Type input, Type output)` - Encontra cadeia de pipelines (BFS)
- `ExecuteChain(List<IAssetPipeline>, object)` - Executa cadeia de pipelines
- `DiscoverPipelines(Assembly)` - Descobre pipelines via reflection

## Pipelines Implementados

### CaptureToImportedAssetPipeline

**Input:** `CaptureResult` (LiveLink)  
**Output:** `ImportedAssetData`

Converte dados brutos capturados do emulador para formato padronizado.

**Processamento:**
1. Copia tiles, nametable e palette
2. Detecta bits por pixel automaticamente
3. Converte nametable (ushort[]) para tilemap (int[])
4. Extrai tile index dos atributos SMS
5. Adiciona metadados de origem

**Opções:**
- `sourceEmulator` - ID do emulador de origem
- `destinationTarget` - ID do target de destino

## Uso

### Exemplo 1: Conversão Simples

```csharp
var pipeline = new CaptureToImportedAssetPipeline();
var options = new Dictionary<string, object>
{
    ["sourceEmulator"] = "emulicious",
    ["destinationTarget"] = "sms"
};

var importedData = pipeline.ProcessTyped(captureResult, options);

if (importedData.IsValid(out var error))
{
    // Usar dados importados
}
```

### Exemplo 2: Descoberta Automática

```csharp
var registry = new PipelineRegistry();
registry.DiscoverPipelines(Assembly.GetExecutingAssembly());

// Encontrar pipeline que converte CaptureResult → ImportedAssetData
var pipelines = registry.GetPipelinesForInput(typeof(CaptureResult))
    .Where(p => p.OutputType == typeof(ImportedAssetData));

var pipeline = pipelines.FirstOrDefault();
var result = pipeline?.Process(captureResult);
```

### Exemplo 3: Encadeamento Automático

```csharp
var registry = new PipelineRegistry();
registry.DiscoverPipelines(Assembly.GetExecutingAssembly());

// Encontrar cadeia automática de CaptureResult → TilemapModule
var chain = registry.FindPipelineChain(
    typeof(CaptureResult), 
    typeof(TilemapModule)
);

if (chain != null)
{
    var result = registry.ExecuteChain(chain, captureResult);
}
```

## Criando Novos Pipelines

### Passo 1: Herdar de AssetPipelineBase

```csharp
public class ImportedAssetToTilemapPipeline : AssetPipelineBase<ImportedAssetData, TilemapModule>
{
    public override string PipelineId => "imported_to_tilemap";
    public override string DisplayName => "Imported Asset → Tilemap Module";
    public override string Description => "Converts imported asset data to tilemap module";

    public override TilemapModule ProcessTyped(ImportedAssetData input, Dictionary<string, object>? options = null)
    {
        var tilemap = new TilemapModule();
        
        // Configurar tilemap com dados importados
        tilemap.MapWidth = input.MapWidth;
        tilemap.MapHeight = input.MapHeight;
        tilemap.MapData = input.TilemapData;
        
        // Salvar tiles como asset
        var tilesetPath = SaveTileset(input.Tiles, input.Palette);
        tilemap.TilesetPath = tilesetPath;
        
        return tilemap;
    }
}
```

### Passo 2: Registrar no Sistema

O pipeline será descoberto automaticamente via `DiscoverPipelines()` ou pode ser registrado manualmente:

```csharp
var registry = new PipelineRegistry();
registry.Register(new ImportedAssetToTilemapPipeline());
```

## Conversões Suportadas

| De | Para | Pipeline | Status |
|---|---|---|---|
| CaptureResult | ImportedAssetData | CaptureToImportedAssetPipeline | ✅ Implementado |
| ImportedAssetData | TilemapModule | ImportedAssetToTilemapPipeline | 🚧 Planejado |
| ImportedAssetData | SpriteModule | ImportedAssetToSpritePipeline | 🚧 Planejado |
| string (PNG) | ImportedAssetData | ImageFileToImportedAssetPipeline | 🚧 Planejado |
| TilemapModule | PNG | TilemapToImagePipeline | 🚧 Planejado |

## Limitações e Considerações

### 1. Conversão de Paletas
Cores podem ser aproximadas ao converter entre consoles com diferentes capacidades:
- SMS: 2-bit RGB (64 cores)
- NES: Paleta fixa (64 cores)
- SNES: 15-bit RGB (32768 cores)
- GBA: 15-bit RGB (32768 cores)

### 2. Diferenças de Nametable
Tamanhos variam por console:
- SMS: 32x28
- NES: 32x30
- SNES: 32x32

O pipeline ajusta/corta/expande conforme necessário.

### 3. Bits por Pixel
- NES/GB: 2bpp (4 cores)
- SMS/GG: 4bpp (16 cores)
- SNES/GBA: 4-8bpp (16-256 cores)

### 4. Metadados Perdidos
LiveLink captura dados visuais, mas não sabe o "significado" dos tiles (chão, parede, etc). Isso deve ser configurado manualmente após importação.

## Extensibilidade

O sistema de pipelines é totalmente extensível:

1. **Novos formatos de entrada:** Crie pipeline que aceita o formato
2. **Novos formatos de saída:** Crie pipeline que produz o formato
3. **Conversões customizadas:** Implemente lógica específica no pipeline
4. **Encadeamento automático:** PipelineRegistry encontra cadeias automaticamente

## Debugging

Use `ImportedAssetData.GetSummary()` para log:

```csharp
var imported = pipeline.ProcessTyped(capture);
Console.WriteLine(imported.GetSummary());
// Output: ImportedAssetData: 512 tiles (8x8), Map: 32x28, Palette: 32 colors, BPP: 4, Source: sms
```

Valide dados antes de usar:

```csharp
if (!imported.IsValid(out var error))
{
    Console.WriteLine($"Invalid data: {error}");
}
```
