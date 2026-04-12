## 🎮 Retruxel v0.5.0-alpha — Favorites, Multi-Scene & UX Improvements

> **Visual retro game development tool** — Build games for classic consoles without writing a single line of code.

---

### ⚠️ Alpha Release Notice

This is an **early alpha release** intended for testing and feedback. The software is functional but still under active development. Expect bugs, missing features, and breaking changes in future versions.

**Not recommended for production use yet.**

---

### 🆕 What's New in v0.5.0-alpha

#### Major Features
- ⭐ **Favorites System** — Star your preferred platforms and filter by favorites across all views
- 🎬 **Multi-Scene Management** — Create, rename, and delete scenes with dynamic tab interface
- 💾 **Window State Persistence** — Window size, position, and maximized state are now saved between sessions
- 🎛️ **Reusable Target Grid Control** — Unified target selection component with sort/filter capabilities

#### Improvements
- 📊 **Dynamic Target Count** — Real-time display of available platforms in WelcomeView
- 🔄 **Global Event Synchronization** — Favorites and language changes sync across all open views
- ✨ **Enhanced Hover Effects** — Improved visual feedback for target and project cards
- 📏 **Scrollable Target Grid** — Limited to 400px height with automatic scrolling

#### Bug Fixes
- 🔧 **Fixed SMS Entity Linker Errors** — Corrected function naming (sms_entity_init/update)
- 🔗 **Resolved Undefined Global References** — Build pipeline now generates correct function calls

#### Localization
- 🌍 **Build Console i18n** — Save log dialog now fully localized (EN/PT-BR)
- 📝 **New Translation Keys** — Added buildconsole.savelog.filter and buildconsole.savelog.filename

---

### ✨ What's Working

- ✅ **Multi-target support** — Build for Sega Master System and Nintendo NES
- ✅ **Visual scene editor** — Drag-and-drop canvas interface with multi-scene support
- ✅ **Text display module** — Working on both SMS and NES targets
- ✅ **Entity module** — Player entity with movement and collision (SMS)
- ✅ **Event system** — OnStart, OnVBlank, and OnInput events
- ✅ **Full build pipeline** — One-click ROM compilation with embedded toolchains
- ✅ **Build console** — Real-time output with toast notifications
- ✅ **Emulator integration** — Launch ROMs directly from the IDE
- ✅ **Favorites system** — Mark and filter your preferred platforms
- ✅ **Window persistence** — Remembers size, position, and state
- ✅ **Internationalization** — English and Portuguese (Brazil) with runtime switching
- ✅ **Zero setup** — Toolchains (SDCC + cc65) extracted automatically on first run

---

### 🎯 Supported Platforms

| Console | Status | Toolchain |
|---------|--------|-----------|
| 🟢 **Sega Master System** | Active | SDCC 4.5.24 + devkitSMS + SMSlib |
| 🟢 **Nintendo NES** | Active | cc65 + neslib |
| 🟡 **Sega Game Gear** | Scaffolding | SDCC 4.5.24 + devkitSMS + SMSlib |
| 🟡 **Sega SG-1000** | Scaffolding | SDCC 4.5.24 + devkitSMS + SMSlib |
| 🟡 **ColecoVision** | Scaffolding | SDCC 4.5.24 + devkitSMS + SMSlib |

---

### 🚧 Known Limitations

- Limited module library (text display and entity modules available)
- No asset editors yet (tiles, sprites, palettes)
- No plugin system yet
- Game Gear, SG-1000, and ColecoVision targets are scaffolding only
- Documentation is minimal

---

### 📦 Installation

#### **Option 1: Installer (Recommended)**
1. Download `Retruxel-0.5.0-alpha-setup.exe`
2. Run the installer and follow the wizard
3. Launch Retruxel from the Start Menu

#### **Option 2: Portable (ZIP)**
1. Download `Retruxel-0.5.0-alpha-portable.zip`
2. Extract to any folder
3. Run `Retruxel.exe`

**Note:** The toolchain will be extracted automatically on first run to `%AppData%\Retruxel\toolchain\`

---

### 🔄 Upgrading from v0.4.0-alpha

This release includes breaking changes to the project file format. Projects created in v0.4.0-alpha may need to be recreated.

**Backup your projects before upgrading.**

---

### 🐛 Reporting Issues

Found a bug? Have a suggestion? Please open an issue on GitHub:
👉 [github.com/ulissesemuman/Retruxel/issues](https://github.com/ulissesemuman/Retruxel/issues)

---

### 🙏 Feedback Welcome

Your feedback is crucial for shaping the future of Retruxel. Please share your thoughts, report bugs, and suggest features!

---

### 📄 License

MIT License — See [LICENSE](https://github.com/ulissesemuman/Retruxel/blob/master/LICENSE) for details.
