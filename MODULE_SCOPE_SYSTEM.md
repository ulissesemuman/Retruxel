# Module Scope System

> Implementado em: 2025-01-XX
> Segue Regra #2: Zero Hardcoded Module Knowledge

---

## Conceito

Sistema de escopo universal para módulos que determina automaticamente onde cada módulo deve ser inicializado:

- **Project Scope**: Módulo inicializado em `main.c` OnStart, persiste entre cenas
- **Scene Scope**: Módulo inicializado em `scene_X_init()`, recarregado a cada cena

## Implementação

### 1. Interface Base (IModule)

```csharp
public interface IModule
{
    // ... propriedades existentes ...
    
    /// <summary>
    /// Default scope for this module — determines where it is initialized.
    /// Project: init in main.c OnStart, persists across scenes
    /// Scene: init in scene_X_init(), reloaded per scene
    /// </summary>
    ModuleScope DefaultScope { get; }
}

public enum ModuleScope
{
    /// <summary>
    /// Module is initialized in main.c OnStart and persists across scenes.
    /// Typical for: input, physics, sprite, animation, entity, enemy, scroll, gamevar
    /// </summary>
    Project,

    /// <summary>
    /// Module is initialized in scene_X_init() and reloaded per scene.
    /// Typical for: palette, tilemap, text.display, background
    /// </summary>
    Scene
}
```

### 2. Módulos Atualizados

Todos os módulos padrão agora definem seu escopo:

#### Scene Scope (Recarregados por cena)
- ✅ **PaletteModule**: Cores específicas da cena
- ✅ **TilemapModule**: Layout do mapa da cena
- ✅ **TextDisplayModule**: Texto estático da cena

#### Project Scope (Persistem entre cenas)
- ✅ **SpriteModule**: Gráficos de sprites compartilhados
- ✅ **InputModule**: Sistema de input global
- ✅ **PhysicsModule**: Sistema de física global
- ✅ **AnimationModule**: Animações compartilhadas
- ✅ **EntityModule**: Player (singleton, persiste)
- ✅ **EnemyModule**: Inimigos (podem persistir ou não)
- ✅ **ScrollModule**: Sistema de scroll global
- ✅ **GameVarModule**: Variáveis globais (score, lives, etc.)

## Benefícios

### 1. Zero Hardcoded Knowledge
Antes:
```csharp
// ❌ Core tinha conhecimento hardcoded de módulos específicos
if (moduleId == "palette" || moduleId == "tilemap" || moduleId == "text.display")
{
    // Scene scope
}
else if (moduleId == "input" || moduleId == "physics" || moduleId == "sprite")
{
    // Project scope
}
```

Depois:
```csharp
// ✅ Core lê a propriedade do módulo
if (module.DefaultScope == ModuleScope.Scene)
{
    // Scene scope
}
else
{
    // Project scope
}
```

### 2. Extensibilidade
Plugins de terceiros podem definir o escopo de seus módulos sem modificar o Core:

```csharp
public class CustomBackgroundModule : IGraphicModule
{
    public string ModuleId => "custom.background";
    public ModuleScope DefaultScope => ModuleScope.Scene;  // Recarrega por cena
    // ...
}
```

### 3. Flexibilidade Futura
Possibilidade de adicionar novos escopos sem breaking changes:

```csharp
public enum ModuleScope
{
    Project,
    Scene,
    Level,      // Futuro: persiste por nível, não por cena
    Persistent  // Futuro: salvo em save file
}
```

## Uso no CodeGenerator

O `CodeGenerator` e `ModuleRenderer` usarão `DefaultScope` para determinar onde colocar os `_init()` calls:

```csharp
// Pseudo-código
foreach (var module in project.Modules)
{
    if (module.DefaultScope == ModuleScope.Project)
    {
        // Adiciona ao OnStart do main.c
        mainOnStartCalls.Add($"{module.ModuleId}_init();");
    }
    else if (module.DefaultScope == ModuleScope.Scene)
    {
        // Adiciona ao scene_X_init()
        sceneInitCalls.Add($"{module.ModuleId}_init();");
    }
}
```

## UI Futura (SceneEditor)

No futuro, o SceneEditor pode permitir override do escopo padrão:

```
┌─────────────────────────────────────┐
│ Sprite Properties                   │
├─────────────────────────────────────┤
│ Name: player_sprite                 │
│ Start Tile: 256                     │
│                                     │
│ Scope: [ Project ▼ ]               │
│        ├ Project (default)          │
│        └ Scene                      │
│                                     │
│ ℹ️ Project scope: persists across   │
│   scenes. Scene scope: reloaded    │
│   per scene.                        │
└─────────────────────────────────────┘
```

## Compatibilidade

- ✅ **Backward compatible**: Módulos existentes sem `DefaultScope` podem usar valor padrão
- ✅ **Forward compatible**: Novos escopos podem ser adicionados sem quebrar código existente
- ✅ **Plugin-friendly**: Plugins de terceiros definem escopo sem modificar Core

## Próximos Passos

1. ✅ Adicionar `ModuleScope` enum e propriedade `DefaultScope` à interface `IModule`
2. ✅ Atualizar todos os módulos padrão com escopo apropriado
3. ⏳ Modificar `CodeGenerator` para usar `DefaultScope` ao gerar `main.c` e `scene_X.c`
4. ⏳ Adicionar suporte para override de escopo no SceneEditor (futuro)
5. ⏳ Documentar guidelines de escopo para desenvolvedores de plugins

## Regras de Escopo

### Project Scope
Use quando o módulo:
- Gerencia estado global (input, physics, gamevar)
- Carrega recursos compartilhados entre cenas (sprite, animation)
- Implementa sistemas singleton (entity, scroll)

### Scene Scope
Use quando o módulo:
- Define conteúdo visual específico da cena (palette, tilemap, text.display)
- Carrega recursos únicos por cena (background, music)
- Precisa ser reinicializado a cada transição de cena

---

## Notas Técnicas

### Por que Enemy é Project Scope?
Embora inimigos sejam específicos de cena, eles são gerenciados como entidades persistentes que podem ser spawned/despawned dinamicamente. O sistema de gerenciamento de inimigos persiste entre cenas, mesmo que instâncias individuais sejam criadas/destruídas.

### Por que GameVar é Project Scope?
GameVar representa variáveis globais (score, lives, timer) que devem persistir entre cenas. Mesmo que uma cena específica não use uma variável, ela deve manter seu valor para a próxima cena.

### Por que Sprite é Project Scope?
Sprites são recursos gráficos compartilhados. Carregar sprites uma vez no início do jogo e reutilizá-los entre cenas é mais eficiente do que recarregar a cada transição.
