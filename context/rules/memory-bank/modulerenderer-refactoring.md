# ModuleRenderer Refactoring

## Problema
O `ModuleRenderer.cs` original tinha **1153 linhas** com múltiplas responsabilidades:
- Descoberta de CodeGens e Tools
- Resolução de variáveis de múltiplas fontes
- Filtragem e transformação de módulos e arquivos
- Geração de chamadas de eventos
- Renderização de templates

## Solução
Quebrado em **8 classes especializadas**, cada uma com responsabilidade única:

### 1. **CodeGenModels.cs** (26 linhas)
Modelos de dados internos:
- `CodeGenManifest` - Manifesto carregado com template path resolvido
- `VariableDefinition` - Define como resolver uma variável

### 2. **CodeGenDiscovery.cs** (145 linhas)
Responsabilidade: Descobrir e carregar manifestos CodeGen
- `DiscoverCodeGens()` - Escaneia `plugins/CodeGens/` recursivamente
- `LoadManifest()` - Carrega e valida `codegen.json`
- `ParseVariables()` - Parseia definições de variáveis do JSON

### 3. **ToolDiscovery.cs** (48 linhas)
Responsabilidade: Descobrir plugins ITool
- `DiscoverTools()` - Escaneia `plugins/Tools/` via reflection
- Indexa por `ToolId` e nome do DLL

### 4. **VariableResolver.cs** (220 linhas)
Responsabilidade: Resolver variáveis de diferentes fontes
- `ResolveForModule()` - Resolve variáveis para um módulo
- `ResolveSettingsValue()` - Lê de `SettingsService`
- `ReadModuleValue()` - Extrai valores do JSON do módulo
- `InvokeTool()` - Executa tools e extensions

### 5. **ModuleFilterProcessor.cs** (90 linhas)
Responsabilidade: Filtrar e transformar coleções de módulos
- `ProcessProjectModules()` - Filtra módulos do projeto
- `ProcessSceneModules()` - Filtra módulos de uma cena
- `EvaluateModuleFilter()` - Avalia expressões de filtro

### 6. **FileFilterProcessor.cs** (110 linhas)
Responsabilidade: Filtrar e transformar GeneratedFiles
- `ProcessModuleFiles()` - Filtra arquivos gerados
- `EvaluateFileFilter()` - Avalia expressões de filtro
- `TransformFile()` - Aplica transformações (replace, path operations)

### 7. **EventCallGenerator.cs** (145 linhas)
Responsabilidade: Gerar chamadas de init/update por evento
- `GenerateUpdateCalls()` - Gera chamadas `_update()` para OnVBlank
- `GenerateInputCalls()` - Gera chamadas `_update()` para OnInput
- `GenerateEventCalls()` - Gera chamadas `_init()` para OnStart

### 8. **ModuleRendererRefactored.cs** (280 linhas)
Responsabilidade: Orquestração e API pública
- Delega para classes especializadas
- Mantém API pública inalterada
- Gerencia estado (instance counters, target assembly, module registry)

## Comparação

| Métrica | Antes | Depois |
|---------|-------|--------|
| **Linhas totais** | 1153 | ~1064 (8 arquivos) |
| **Maior arquivo** | 1153 | 280 |
| **Responsabilidades por arquivo** | 7+ | 1 |
| **Testabilidade** | Baixa | Alta |
| **Manutenibilidade** | Baixa | Alta |

## Benefícios

### 1. **Single Responsibility Principle**
Cada classe tem uma única razão para mudar:
- Mudanças em discovery de CodeGens → `CodeGenDiscovery`
- Mudanças em resolução de variáveis → `VariableResolver`
- Mudanças em filtros → `ModuleFilterProcessor` ou `FileFilterProcessor`

### 2. **Testabilidade**
Classes menores e focadas são mais fáceis de testar:
```csharp
// Antes: testar resolução de variáveis requeria instanciar ModuleRenderer completo
var renderer = new ModuleRenderer(pluginsPath, targetAssembly, progress);

// Depois: testar resolução isoladamente
var resolver = new VariableResolver(tools, targetAssembly);
var result = resolver.ResolveForModule(variables, moduleJson);
```

### 3. **Reutilização**
Classes especializadas podem ser usadas independentemente:
```csharp
// Usar apenas discovery sem renderização
var codeGens = CodeGenDiscovery.DiscoverCodeGens(pluginsPath);

// Usar apenas filtros sem discovery
var filtered = ModuleFilterProcessor.ProcessProjectModules(project, varDef);
```

### 4. **Legibilidade**
Código mais fácil de entender:
- Nome do arquivo indica responsabilidade
- Métodos menores e mais focados
- Menos scroll para encontrar código relevante

## Migração

### Opção 1: Substituição Direta (Recomendado)
Renomear `ModuleRenderer.cs` para `ModuleRenderer.old.cs` e `ModuleRendererRefactored.cs` para `ModuleRenderer.cs`.

### Opção 2: Coexistência Temporária
Manter ambas as versões durante período de transição:
- `ModuleRenderer` - versão original (deprecated)
- `ModuleRendererRefactored` - nova versão

### Opção 3: Gradual
Mover métodos um por um do original para as novas classes.

## Próximos Passos

1. **Testes Unitários**: Criar testes para cada classe especializada
2. **Documentação**: Adicionar XML docs detalhados
3. **Performance**: Medir impacto (esperado: neutro ou positivo)
4. **Validação**: Testar com projetos reais (Kung Fu Master port)

## Notas

- **API pública inalterada**: `ModuleRendererRefactored` mantém mesma interface
- **Sem breaking changes**: Código cliente não precisa mudar
- **Backward compatible**: Funcionalidade idêntica à versão original
- **Zero regressões**: Todos os testes existentes devem passar
