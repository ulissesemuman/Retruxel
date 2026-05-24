# SceneEditor Refactoring - Implementation Guide

## Status: Backend Complete ✅ | UI Pending ⏳

O backend para Scene CodeGen, GameVar Module e nova estrutura do main.c está **100% implementado e funcional**.

## O que foi implementado (Backend)

### 1. Scene CodeGen
- ✅ `ITarget.GenerateSceneTransitionPreamble()` e `GenerateSceneTransitionPostamble()`
- ✅ `SmsTarget` implementa transição com `SMS_displayOff()`, `SMS_VRAMmemsetW()`, `SMS_displayOn()`
- ✅ `ModuleRenderer.RenderSceneFile()` gera `scene_<name>.c` com `scene_<name>_init()`
- ✅ `CodeGenerator` integra geração de cenas no pipeline
- ✅ Templates em `Plugins/CodeGens/scene/sms/`

### 2. GameVar Module
- ✅ `GameVarModule` completo com parâmetros (name, type, initialValue, showInHud)
- ✅ CodeGen batch em `Plugins/CodeGens/gamevar/sms/` gera `gamevars.h` + `gamevars.c`
- ✅ `CodeGenerator.GenerateGameVarsFile()` processa todas as variáveis em lote
- ✅ Utilitário `retruxel_uint_to_str()` gerado automaticamente quando necessário

### 3. Main.c Refatorado
- ✅ Nova estrutura: OnStart (projeto) → scene_init() → loop (OnInput + OnVBlank)
- ✅ `RetruxelProject.InitialSceneId` property
- ✅ `ModuleRenderer` suporta:
  - `from: "project"` com `initialSceneId`
  - `from: "projectModules"` com filtros
  - `from: "scene"` e `from: "sceneModules"`
  - `transform: "upper"`, `"initCall"`, `"hasAny"`, `"moduleId"`
- ✅ Templates atualizados em `Plugins/CodeGens/main/sms/`

## O que falta (UI - SceneEditor)

### Mudanças Necessárias no XAML

#### 1. Remover Painel de Eventos (Bottom)
```xml
<!-- REMOVER: Grid.Row="2" Grid.Column="1" - Events Panel -->
<Border Grid.Row="2" Grid.Column="1" ...>
    <TextBlock Text="{loc:Tr Key='scene.events'}"/>
    <StackPanel x:Name="EventsPanel"/>
</Border>
```

#### 2. Substituir Sidebar Esquerdo
Trocar o painel atual (Modules/Assets tabs) por estrutura hierárquica:

```xml
<TreeView x:Name="ProjectStructureTree" Grid.Row="1" Grid.Column="0">
    <TreeViewItem Header="PROJECT" IsExpanded="True">
        <TreeViewItem Header="Scenes" IsExpanded="True">
            <TreeViewItem Header="● main (initial)"/>
            <TreeViewItem Header="+ Add Scene"/>
        </TreeViewItem>
        <TreeViewItem Header="OnStart Modules" IsExpanded="True">
            <TreeViewItem Header="[Input]"/>
            <TreeViewItem Header="[Physics]"/>
            <TreeViewItem Header="+ Add"/>
        </TreeViewItem>
        <TreeViewItem Header="Variables" IsExpanded="True">
            <TreeViewItem Header="score: int = 0"/>
            <TreeViewItem Header="lives: byte = 3"/>
            <TreeViewItem Header="+ Add"/>
        </TreeViewItem>
    </TreeViewItem>
    
    <TreeViewItem Header="SCENE: main" IsExpanded="True">
        <TreeViewItem Header="Palette">
            <TreeViewItem Header="[palette_0]"/>
        </TreeViewItem>
        <TreeViewItem Header="Tilemap Layers">
            <TreeViewItem Header="[tilemap_0]"/>
        </TreeViewItem>
        <TreeViewItem Header="Text (static)">
            <TreeViewItem Header="[text_display_0]"/>
        </TreeViewItem>
    </TreeViewItem>
</TreeView>
```

#### 3. Ajustar Grid Layout
```xml
<Grid.RowDefinitions>
    <RowDefinition Height="40"/>   <!-- Top bar -->
    <RowDefinition Height="*"/>    <!-- Main content -->
    <!-- REMOVER: <RowDefinition Height="200"/> Events panel -->
</Grid.RowDefinitions>
```

### Mudanças Necessárias no Code-Behind

#### 1. Remover Lógica de Eventos
- `SceneEditorView_Events.cs` - Remover ou refatorar
- Métodos relacionados a drag-and-drop de módulos para zonas de evento

#### 2. Adicionar Lógica de Estrutura
```csharp
// SceneEditorView_Structure.cs (novo arquivo)
private void PopulateProjectStructure()
{
    // PROJECT level
    var projectNode = new TreeViewItem { Header = "PROJECT" };
    
    // Scenes
    var scenesNode = new TreeViewItem { Header = "Scenes" };
    foreach (var scene in _project.Scenes)
    {
        var isInitial = scene.SceneId == _project.InitialSceneId;
        var sceneNode = new TreeViewItem 
        { 
            Header = $"{(isInitial ? "● " : "")}{scene.SceneName}{(isInitial ? " (initial)" : "")}"
        };
        scenesNode.Items.Add(sceneNode);
    }
    scenesNode.Items.Add(new TreeViewItem { Header = "+ Add Scene" });
    projectNode.Items.Add(scenesNode);
    
    // OnStart Modules
    var onStartNode = new TreeViewItem { Header = "OnStart Modules" };
    var onStartModules = GetProjectModules("input", "physics", "sprite", "animation");
    foreach (var module in onStartModules)
    {
        onStartNode.Items.Add(new TreeViewItem { Header = $"[{module.ModuleId}]" });
    }
    onStartNode.Items.Add(new TreeViewItem { Header = "+ Add" });
    projectNode.Items.Add(onStartNode);
    
    // Variables
    var varsNode = new TreeViewItem { Header = "Variables" };
    var gameVars = GetGameVarModules();
    foreach (var gv in gameVars)
    {
        varsNode.Items.Add(new TreeViewItem { Header = $"{gv.Name}: {gv.Type} = {gv.InitialValue}" });
    }
    varsNode.Items.Add(new TreeViewItem { Header = "+ Add" });
    projectNode.Items.Add(varsNode);
    
    ProjectStructureTree.Items.Add(projectNode);
    
    // SCENE level
    var currentScene = GetCurrentScene();
    var sceneStructNode = new TreeViewItem { Header = $"SCENE: {currentScene.SceneName}" };
    
    // Palette
    var paletteNode = new TreeViewItem { Header = "Palette" };
    foreach (var pal in GetSceneModules(currentScene, "palette"))
    {
        paletteNode.Items.Add(new TreeViewItem { Header = $"[{pal.ModuleId}]" });
    }
    sceneStructNode.Items.Add(paletteNode);
    
    // Tilemap Layers
    var tilemapNode = new TreeViewItem { Header = "Tilemap Layers" };
    foreach (var tm in GetSceneModules(currentScene, "tilemap"))
    {
        tilemapNode.Items.Add(new TreeViewItem { Header = $"[{tm.ModuleId}]" });
    }
    sceneStructNode.Items.Add(tilemapNode);
    
    // Text (static)
    var textNode = new TreeViewItem { Header = "Text (static)" };
    foreach (var txt in GetSceneModules(currentScene, "text.display").Where(t => !t.Dynamic))
    {
        textNode.Items.Add(new TreeViewItem { Header = $"[{txt.ModuleId}]" });
    }
    sceneStructNode.Items.Add(textNode);
    
    ProjectStructureTree.Items.Add(sceneStructNode);
}
```

#### 3. Classificação Automática de Módulos
```csharp
private string GetModuleDestination(string moduleId)
{
    return moduleId switch
    {
        "palette" => "SCENE > Palette",
        "tilemap" => "SCENE > Tilemap Layers",
        "text.display" => "SCENE > Text (static)",
        "input" => "PROJECT > OnStart Modules",
        "physics" => "PROJECT > OnStart Modules",
        "sprite" => "PROJECT > OnStart Modules",
        "animation" => "PROJECT > OnStart Modules",
        "entity" or "enemy" or "scroll" => "CANVAS", // Aparecem no canvas
        "gamevar" => "PROJECT > Variables",
        _ => "CANVAS"
    };
}

private void OnModuleDrop(string moduleId, Point position)
{
    var destination = GetModuleDestination(moduleId);
    
    if (destination.StartsWith("PROJECT") || destination.StartsWith("SCENE"))
    {
        // Adicionar automaticamente à estrutura correta
        AddModuleToStructure(moduleId, destination);
        RefreshProjectStructure();
    }
    else
    {
        // Adicionar ao canvas (entity, enemy, scroll)
        AddModuleToCanvas(moduleId, position);
    }
}
```

### Arquivos a Modificar

1. **SceneEditorView.xaml**
   - Remover `Grid.Row="2"` (Events panel)
   - Substituir sidebar por TreeView de estrutura
   - Ajustar RowDefinitions

2. **SceneEditorView.xaml.cs**
   - Remover referências a EventsPanel
   - Adicionar ProjectStructureTree

3. **SceneEditorView_Events.cs**
   - Remover ou refatorar completamente

4. **SceneEditorView_Structure.cs** (novo)
   - Implementar PopulateProjectStructure()
   - Implementar GetModuleDestination()
   - Implementar OnModuleDrop()

5. **SceneEditorView_ModulePalette.cs**
   - Atualizar para mostrar destino automático no tooltip

## Como Testar o Backend (Sem UI)

O backend está funcional e pode ser testado diretamente:

```csharp
// 1. Criar projeto com InitialSceneId
var project = new RetruxelProject
{
    Name = "Test Game",
    TargetId = "sms",
    InitialSceneId = "main",
    Scenes = new List<SceneData>
    {
        new SceneData
        {
            SceneId = "main",
            SceneName = "main",
            Elements = new List<SceneElementData>
            {
                // Adicionar palette, tilemap, etc.
            }
        }
    }
};

// 2. Adicionar GameVar modules
var gameVarElement = new SceneElementData
{
    ElementId = Guid.NewGuid().ToString(),
    ModuleId = "gamevar",
    ModuleState = JsonSerializer.SerializeToElement(new
    {
        name = "score",
        type = "int",
        initialValue = "0"
    })
};
project.Scenes[0].Elements.Add(gameVarElement);

// 3. Gerar código
var codeGen = new CodeGenerator(moduleRegistry, moduleRenderer, target);
var buildContext = await codeGen.GenerateAsync(project, outputDir, progress);

// 4. Verificar arquivos gerados
// - scene_main.c com scene_main_init()
// - gamevars.c com g_score
// - main.c com scene_main_init() call
```

## Prioridade de Implementação

1. **Alta**: Remover painel de eventos (quebra a arquitetura antiga)
2. **Alta**: Adicionar TreeView de estrutura (mostra nova organização)
3. **Média**: Classificação automática de módulos (UX improvement)
4. **Baixa**: Drag-and-drop refinado (polish)

## Notas Importantes

- O backend **não depende** da UI - a geração de código funciona independentemente
- A UI atual ainda funciona, mas não reflete a nova arquitetura
- Módulos adicionados manualmente ao projeto serão classificados corretamente na geração
- A refatoração da UI é **cosmética** - não afeta a funcionalidade do build
