## 🎮 Retruxel v0.4.0-alpha — First Public Alpha

> **Visual retro game development tool** — Build games for classic consoles without writing a single line of code.

---

### ⚠️ Alpha Release Notice

This is an **early alpha release** intended for testing and feedback. The software is functional but still under active development. Expect bugs, missing features, and breaking changes in future versions.

**Not recommended for production use yet.**

---

### ✨ What's Working

- ✅ **Multi-target support** — Build for Sega Master System and Nintendo NES
- ✅ **Visual scene editor** — Drag-and-drop canvas interface
- ✅ **Text display module** — Working on both SMS and NES targets
- ✅ **Event system** — OnStart, OnVBlank, and OnInput events
- ✅ **Full build pipeline** — One-click ROM compilation with embedded toolchains
- ✅ **Build console** — Real-time output with toast notifications
- ✅ **Emulator integration** — Launch ROMs directly from the IDE
- ✅ **Favorites system** — Mark and filter your preferred platforms
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

- Limited module library (only text display available)
- No asset editors yet (tiles, sprites, palettes)
- No plugin system yet
- Game Gear, SG-1000, and ColecoVision targets are scaffolding only
- Documentation is minimal

---

### 📦 Installation

#### **Option 1: Installer (Recommended)**
1. Download `Retruxel-0.4.0-alpha-setup.exe`
2. Run the installer and follow the wizard
3. Launch Retruxel from the Start Menu

#### **Option 2: Portable (ZIP)**
1. Download `Retruxel-0.4.0-alpha-portable.zip`
2. Extract to any folder
3. Run `Retruxel.exe`

**Note:** The toolchain will be extracted automatically on first run to `%AppData%\Retruxel\toolchain\`

---

### 🐛 Reporting Issues

Found a bug? Have a suggestion? Please open an issue on GitHub:
👉 [github.com/ulissesemuman/Retruxel/issues](https://github.com/ulissesemuman/Retruxel/issues)

---

### 🙏 Feedback Welcome

This is the first public alpha release. Your feedback is crucial for shaping the future of Retruxel. Please share your thoughts, report bugs, and suggest features!

---

### 📄 License

MIT License — See [LICENSE](https://github.com/ulissesemuman/Retruxel/blob/master/LICENSE) for details.
