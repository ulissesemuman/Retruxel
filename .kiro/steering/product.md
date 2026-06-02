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

## Entity System (current model)

Entities are the primary game object. `EntityData` owns everything about a game character:

- **Sprite** — `SpriteAssetId` points to an asset in `project.Assets`; `WidthTiles`/`HeightTiles` define the render grid (must match the asset column layout)
- **Palette** — `PaletteSlot` selects which scene palette slot renders the sprite
- **Input** — `InputSlot` indexes into `RetruxelProject.InputPorts` (-1 = NPC/enemy with no player input)
- **Behavior** — `ModuleOverrides` holds per-entity parameter overrides (physics speed, AI pattern, etc.)
- **Type** — `EntityType` drives which CodeGen template is used (`"entity"` → `entity/sms/`, `"enemy"` → `enemy/sms/`)

Variants of the same `EntityType` share the sprite asset and dimensions but have independent positions and palette slots.

## Input System (current model)

Input ports are defined by the target hardware via `ITarget.GetInputPorts()`. The SMS returns two controller ports (Port A and Port B), each with D-pad + 2 buttons. Each `InputButton` carries:
- `Id` — internal key used by the editor
- `Label` — shown in UI
- `DevkitConst` — C constant emitted by CodeGen (e.g. `PORT_A_KEY_1`)

On project creation, `EnsureInputPorts` copies the target defaults into `RetruxelProject.InputPorts` as `InputPortBinding[]`. The user can remap individual buttons. The entity's `InputSlot` selects which port drives that entity.

## Property Panel (current model)

The right panel uses typed controls — not free text for everything:
- **Palette slot** → `ComboBox` listing real slots from the target (e.g. "Slot 0 — Background", "Slot 1 — Sprite")
- **Input slot** → `ComboBox` listing project input ports + "None"
- **Module enum parameters** → `ComboBox` from `ParameterDefinition.EnumOptions`
- **Module bool parameters** → `ComboBox` with Yes/No
- **Module int/string parameters** → `TextBox`

`BuildModuleProperties` is manifest-driven: it reads `GetManifest()` from the live module instance and renders each `ParameterDefinition` with the appropriate control type.
