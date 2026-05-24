# Retruxel — Product Overview

Retruxel is a visual IDE for developing retro games, inspired by GB Studio. Users place modules on a canvas, configure them through an auto-generated UI, and Retruxel handles code generation, compilation, and ROM output — no terminal, no Makefile, no toolchain setup required.

Current version: **0.8.0-alpha** (active development).

## Primary Use Case

**Kung Fu Master** (NES → Master System port) is the primary validation target for all features. Every feature must be validated against the real-world complexity of porting Kung Fu Master. If it doesn't work for Kung Fu Master, it doesn't work.

## Supported Targets

| Console | Status |
|---|---|
| Sega Master System (SMS) | 🟢 Active (~60%) |
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
