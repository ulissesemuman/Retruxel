# Retruxel — Product Overview

Retruxel is a visual IDE for developing retro games, inspired by GB Studio. Users place modules on a canvas, configure them through an auto-generated UI, and Retruxel handles code generation, compilation, and ROM output — no terminal, no Makefile, no toolchain setup required.

Current version: **0.8.0-alpha** (active development).

## Primary Use Case

**Kung Fu Master** (NES → Master System port) is the primary validation target for all features. Every feature must be validated against the real-world complexity of porting Kung Fu Master. If it doesn't work for Kung Fu Master, it doesn't work.

## Supported Targets

| Console | Status |
|---|---|
| Sega Master System (SMS) | 🟢 Active (~65%) |
| Sega Game Gear (GG) | 🟡 Scaffolding (~15%) |
| Sega SG-1000 | 🟡 Scaffolding (~15%) |
| ColecoVision | 🟡 Scaffolding (~15%) |
| Nintendo NES | 🟢 Active (~5%) |
| SNES | 🔮 Planned |

## Design System: Neo-Technical Archive

- **Aesthetic:** Architectural Brutalism + Modern Editorial Design (1980s mainframe meets modern IDE)
- **0px border-radius** on all components — no exceptions
- **8px grid** — all spacing must align to this grid
- No 1px solid borders for sectioning — use tonal background shifts or negative space instead
- Typography: **Space Grotesk** (display/headlines) + **Inter** (body/code)
- Color base: `#0e0e0e` void background; primary accent `#8eff71` (green = Go/Build); tertiary `#81ecff` (blue = Read/Info)
- Use `on_surface_variant` (`#adaaaa`) for body text, never pure white

## Prefab System (current model)

**Prefabs** are the primary entity definition mechanism. A `PrefabData` is a reusable template shared by multiple `EntityData` instances. It replaces the legacy `EntityType` string concept.

`RetruxelProject.Prefabs` is the project-level list of all prefabs. `EntityData.PrefabId` references a prefab by its unique `PrefabId`.

### PrefabData fields

| Field | Type | Description |
|---|---|---|
| `PrefabId` | `string` | Unique C-safe identifier. Used as C name prefix in generated code. Ex: `"player"`, `"goblin"` |
| `DisplayName` | `string` | Human-readable name shown in the editor |
| `SpriteAssetId` | `string` | Asset referenced by this prefab's sprite |
| `PaletteSlot` | `int` | Palette slot index for rendering |
| `WidthTiles` / `HeightTiles` | `int` | Render grid dimensions (must match asset column layout) |
| `Actions` | `List<ActionInstance>` | Configured actions available to this prefab (walk, jump, attack, etc.) |
| `InputMapping` | `PrefabInputMapping?` | Maps hardware buttons → ActionInstance GUIDs. Null = no player input |

### EntityData (instance)

An `EntityData` is a placed instance of a Prefab in a scene. It holds only instance-level data:

- `PrefabId` — references the prefab definition
- `EntityId` — unique GUID per instance
- `Label` — display name for this instance
- `StartTileX` / `StartTileY` — spawn position
- `Visible` — active flag

**Legacy fields** (`SpriteAssetId`, `PaletteSlot`, `WidthTiles`, `HeightTiles`, `InputSlot`, `EntityType`) are kept as nullable on `EntityData` for migration — they are fallbacks when `PrefabId` resolves to nothing. New code always reads from the resolved `PrefabData`.

### Action System

Actions are reusable behavior blocks defined as declarative JSON + C templates:

- **`ActionDefinition`** — immutable, discovered at startup from `Plugins/CodeGens/actions/**/action.json`. Lives in `ActionRegistry`. Never saved in the project.
- **`ActionInstance`** — saved inside `PrefabData.Actions`. References an `ActionDefinition` by `ActionId` and stores user-configured `Parameters` (dict).
- **`ActionRegistry`** — discovered via `ActionRegistry.Discover(pluginsPath)`. Scans `actions/{actionId}/all/action.json` (cross-platform) and `actions/{actionId}/{targetId}/action.json` (target-specific override).
- Template variables use `{{prefab.id}}` as the C name prefix and `{{params.Name}}` for parameter values.

### PrefabInputMapping

Maps hardware buttons to lists of ActionInstance GUIDs within a prefab:
- `PortId` — which hardware port (e.g. `"port1"`, `"port2"`)
- `ButtonMappings` — `Dictionary<string, List<string>>` mapping button IDs to `ActionInstance.InstanceId` lists
- Multiple actions per button are supported (triggered simultaneously)
- Null `InputMapping` = NPC/enemy with no player input

## Entity System (current model)

`CodeGenerator` resolves the `PrefabData` for each `EntityData` and injects `entityState` with sprite/palette/dimension data. `PrefabId` is used as the `moduleId` for codegen template lookup (e.g. `player/sms/`, `goblin/sms/`).

**Note:** `PrefabData.Actions` and `PrefabData.InputMapping` are not yet consumed by `CodeGenerator`. The action codegen wiring (iterating actions, resolving templates via `ActionRegistry.GetTemplatePath`, emitting C functions) is the next open gap.

Variants of the same prefab share sprite asset and dimensions but have independent positions and palette slots (set on the instance via `ModuleOverrides` or scene placement).

## Input System (current model)

Input ports are defined by the target hardware via `ITarget.GetInputPorts()`. The SMS returns two controller ports (Port A and Port B), each with D-pad + 2 buttons. Each `InputButton` carries:
- `Id` — internal key used by the editor
- `Label` — shown in UI
- `DevkitConst` — C constant emitted by CodeGen (e.g. `PORT_A_KEY_1`)

On project creation, `EnsureInputPorts` copies the target defaults into `RetruxelProject.InputPorts` as `InputPortBinding[]`. The user can remap individual buttons. The prefab's `InputMapping.PortId` selects which port drives that entity.

## Property Panel (current model)

The right panel uses typed controls — not free text for everything:
- **Palette slot** → `ComboBox` listing real slots from the target (e.g. "Slot 0 — Background", "Slot 1 — Sprite")
- **Input slot** → `ComboBox` listing project input ports + "None"
- **Module enum parameters** → `ComboBox` from `ParameterDefinition.EnumOptions`
- **Module bool parameters** → `ComboBox` with Yes/No
- **Module int/string parameters** → `TextBox`

For `EntityData`, the right panel shows a `PREFAB — {ID}` header and an `✏ EDIT PREFAB` button that opens `PrefabEditorWindow`. Instance-level fields (label, position) appear below.

`BuildModuleProperties` is manifest-driven: it reads `GetManifest()` from the live module instance and renders each `ParameterDefinition` with the appropriate control type.
