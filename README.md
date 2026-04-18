# Retruxel

<p align="center">
  <img src="Retruxel/Assets/Images/Logo/full_logo.png" alt="Full Logo" width="40%">
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10">
  <img src="https://img.shields.io/badge/WPF-Windows-0078D4?logo=windows&logoColor=white" alt="WPF">
  <img src="https://img.shields.io/github/v/release/ulissesemuman/Retruxel?include_prereleases&label=version" alt="Version">
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License">
  <img src="https://img.shields.io/badge/Status-Beta-blue" alt="Status">
  <img src="https://img.shields.io/badge/Platforms-SMS%20%7C%20NES%20%7C%20GG%20%7C%20SG1000%20%7C%20Coleco-blueviolet" alt="Platforms">
</p>

> Visual retro game development tool — build games for classic consoles without writing a single line of code.
> 
> Inspired by [GB Studio](https://www.gbstudio.dev/)

---

<!-- Application screenshot -->
![Retruxel Screenshot](Retruxel/Assets/Images/Readme/builder.png)

---

## What is Retruxel?

Retruxel is a visual IDE for developing retro games, inspired by [GB Studio](https://www.gbstudio.dev/). Instead of writing C or assembly by hand, you use a visual editor to place modules, configure parameters through an auto-generated UI, and Retruxel handles the rest — generating C code, compiling it with the embedded toolchain, and producing a ready-to-run ROM file.

No terminal. No Makefile. No toolchain setup. Just open and start building.

---

## ✨ Features

- 🎮 **Visual game editor** — place modules on canvas and configure them through auto-generated UI
- 🎯 **Multi-target support** — build for multiple retro consoles from a single project
- ⚙️ **Zero setup** — toolchains embedded and extracted automatically on first run
- 🧩 **Module system** — graphic, logic and audio modules as building blocks (early stage)
- 🔁 **Portable modules** — universal modules keep your project target-agnostic for future migration
- 🏗️ **Auto-generated UI** — module parameters are described via `ModuleManifest`, no UI code needed
- 📦 **One-click ROM export** — full build pipeline from project to ROM file
- ⭐ **Favorites system** — mark and filter your preferred target platforms
- 🌍 **Multilingual** — interface available in multiple languages with runtime switching
- 🚀 **Emulator integration** — launch ROMs directly from the IDE

---

## 🎯 Supported Targets

| Console | Status | Progress | Toolchain |
|---|---|---|---|
| Sega Master System | 🟢 Active | ~60% | SDCC 4.5.24 + devkitSMS + SMSlib |
| Sega Game Gear | 🟡 Scaffolding | ~15% | SDCC 4.5.24 + devkitSMS + SMSlib |
| Sega SG-1000 | 🟡 Scaffolding | ~15% | SDCC 4.5.24 + devkitSMS + SMSlib |
| ColecoVision | 🟡 Scaffolding | ~15% | SDCC 4.5.24 + devkitSMS + SMSlib |
| Nintendo NES | 🟢 Active | ~5% | cc65 + neslib |
| SNES | 🔮 Planned | 0% | — |

---

## 🏛️ Architecture

Retruxel is built on .NET 10 / WPF and organized as a multi-project solution with a plugin-based architecture:

| Project | Type | Role |
|---|---|---|
| `Retruxel` | WPF Application | Main shell — UI, navigation and orchestration |
| `Retruxel.Core` | Class Library | Interfaces, models, services and code generation engine |
| `Retruxel.SDK` | Class Library | Public interfaces for plugin developers |
| `Retruxel.Toolchain` | Class Library | Embedded toolchains with centralized adapter |
| `Retruxel.Modules` | Class Library | Standard portable module definitions |
| `Plugins/Targets/*` | Class Libraries | Platform-specific implementations (SMS, NES, GG, SG-1000, ColecoVision) |
| `Plugins/Tools/*` | Class Libraries | Standalone tools (image processing, preprocessors, asset converters) |
| `Plugins/CodeGens/*` | JSON + Templates | Declarative code generators (`.c.rtrx` templates + `codegen.json` manifests) |

### Build Pipeline

**SMS / Game Gear / SG-1000 / ColecoVision:**
```
.rtrxproject  →  ModuleRenderer (JSON+templates)  →  .c / .h files  →  SDCC  →  ihx2sms  →  ROM
                      ↓
              Tool invocation (preprocessors, asset converters)
```

**NES:**
```
.rtrxproject  →  ModuleRenderer (JSON+templates)  →  .c / .h files  →  cc65  →  ld65  →  .nes ROM
                      ↓
              Tool invocation (preprocessors, asset converters)
```

**Generation Priority:**
1. ModuleRenderer (declarative JSON + `.c.rtrx` templates)
2. CodeGen DLL (legacy compiled generators)
3. Target.GenerateCodeForModule() (target-specific fallback)
4. Module.GenerateCode() (module-level fallback)

### Module System

Modules are the building blocks of every Retruxel project. There are three types:

- **Graphic modules** — tiles, sprites, palettes, tilemaps
- **Logic modules** — physics, input, entities, game flow
- **Audio modules** — music and sound effects for the target sound chip

Each module exposes a `ModuleManifest` that describes its parameters. The shell reads this manifest and auto-generates the configuration UI — no WPF knowledge required to write a module.

**Standard modules** (`Retruxel.Modules`) are portable definitions designed for cross-platform migration. These modules generate platform-agnostic JSON that can be rendered to any target.

**Console-specific modules** can be added via plugins in `Plugins/Targets/` to leverage unique hardware features (e.g., SMS VDP modes, NES PPU attributes).

Code generation is declarative: each module has a `codegen.json` manifest and `.c.rtrx` template files in `Plugins/CodeGens/[module]/[target]/`. The `ModuleRenderer` processes templates with variable substitution, conditionals, loops, and tool invocation.

#### Portability categories

| Category | Description |
|---|---|
| **Universal** | Identical JSON output on any target — fully portable |
| **Base + Specialization** | Shared base with optional target-specific fields |
| **Exclusive** | Target-locked — marked with a warning icon in the UI |

---

## 🎨 Design System

Retruxel uses a custom design system called **Neo-Technical Archive** — a modern IDE aesthetic inspired by 1980s mainframe terminals.

- **Style:** Architectural Brutalism + Modern Editorial Design
- **0px border-radius** on all internal components
- **8px grid** — no exceptions
- Separation by tonal background shift — no divider lines
- Typography: **Space Grotesk** (display) + **Inter** (body/code)

---

## 🚀 Getting Started

> ⚠️ Retruxel is currently in beta development (v0.7.2-beta).

### Option 1: Download Installer (Recommended)

Download the latest installer from [Releases](https://github.com/ulissesemuman/Retruxel/releases). The toolchain is extracted automatically on first run to `%AppData%\Retruxel\toolchain\`.

### Option 2: Build from Source

1. Clone the repository
2. Open `Retruxel.slnx` in Visual Studio 2022+
3. Build and run the `Retruxel` project
4. The toolchain is extracted automatically on first run to `%AppData%\Retruxel\toolchain\`

### Current Status

- ✅ Project creation and management
- ✅ Multi-target infrastructure with 6 platforms (SMS, GG, SG-1000, ColecoVision, NES, SNES planned)
- ✅ Visual scene editor with canvas
- ✅ Declarative code generation system (JSON + `.c.rtrx` templates)
- ✅ Tool system with preprocessors and asset converters
- ✅ ModuleRenderer with template engine (conditionals, loops, nested properties, arithmetic)
- ✅ Event system (OnStart, OnVBlank, OnInput)
- ✅ ROM compilation pipeline (SMS + NES)
- ✅ SMS splash screen with fade effects
- ✅ Build console with real-time output and toast notifications
- ✅ Emulator integration with configurable launch settings
- ✅ Favorites system with sort and filter capabilities
- ✅ Internationalization (i18n) system with runtime language switching
- ✅ Dynamic manufacturer discovery and filtering
- ✅ Centralized toolchain adapter
- 🚧 Standard modules (text display, tilemap, sprite, palette — early stage)
- 🚧 Asset editors (planned)
- 🚧 Visual scripting system (planned)

---

## 🌍 Internationalization

Retruxel supports multiple languages with automatic detection and runtime switching.

### Supported Languages

- 🇺🇸 English
- 🇧🇷 Português (Brasil)

### Adding a New Language

1. Create a new JSON file in `Retruxel/Assets/Localization/` with the language code as filename (e.g., `es.json` for Spanish)
2. Use this structure:

```json
{
  "_metadata": {
    "code": "es"
  },
  "strings": {
    "app.title": "RETRUXEL",
    "welcome.title": "SELECCIÓN DE TARGET",
    ...
  }
}
```

3. The language will appear automatically in Settings → General → Language
4. Language names are automatically localized using .NET `CultureInfo` based on your OS language

### How It Works

- Language files are discovered automatically on startup
- Display names are pulled from the operating system (e.g., "Español" on Spanish Windows, "Spanish" on English Windows)
- No restart required — switch languages instantly in Settings
- Fallback to English if selected language file is missing

---

## 🧩 Writing a Plugin

Retruxel supports three types of plugins:

### 1. Declarative Code Generators (Recommended)

Create a folder in `Plugins/CodeGens/[module_id]/[target_id]/` with:

**codegen.json** — manifest describing inputs and outputs:
```json
{
  "moduleId": "mymodule",
  "targetId": "sms",
  "templates": [
    { "template": "mymodule.c.rtrx", "output": "mymodule.c" },
    { "template": "mymodule.h.rtrx", "output": "mymodule.h" }
  ]
}
```

**mymodule.c.rtrx** — C template with variable substitution:
```c
void {{functionName}}() {
    {{#if enabled}}
    // Generated code
    {{#each items}}
    process_item({{this}});
    {{/each}}
    {{/if}}
}
```

Supports: `{{variable}}`, `{{#if}}`, `{{#ifnot}}`, `{{#each}}`, `{{object.property}}`, `{{a * b}}`, tool invocation.

### 2. Tools (Asset Converters, Preprocessors)

Create a .NET class library in `Plugins/Tools/` that implements `ITool`:

```csharp
public class MyTool : ITool
{
    public string ToolId => "mytool";
    public string DisplayName => "My Tool";
    public string Category => "Conversion";
    public bool IsStandalone => true;
    public bool RequiresProject => false;
    
    public Dictionary<string, object> Execute(Dictionary<string, object> input)
    {
        // Process input and return results
        return new Dictionary<string, object> { ["output"] = result };
    }
}
```

### 3. Target Platforms

Create a .NET class library in `Plugins/Targets/` that implements `ITarget`:

```csharp
public class MyTarget : ITarget
{
    public string TargetId => "myconsole";
    public string DisplayName => "My Console";
    public string Manufacturer => "MyCompany";
    // Implement ITarget interface methods
}
```

Retruxel discovers plugins automatically via reflection on startup.

---

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
