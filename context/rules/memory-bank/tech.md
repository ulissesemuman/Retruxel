# Retruxel - Technology Stack

## Programming Languages

### C# / .NET 10.0
- **Framework**: .NET 10.0 (latest)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Language Features**: C# 13 with nullable reference types, implicit usings
- **Target**: `net10.0` for libraries, `net10.0-windows` for WPF projects

### C (Generated Code)
- **SMS/GG/SG-1000/ColecoVision**: C89/C99 compiled with SDCC
- **NES**: C89 compiled with cc65
- Generated from `.c.rtrx` templates via ModuleRenderer

### XAML
- WPF views and controls
- Custom design system (Neo-Technical Archive)

## Build System

### Solution Structure
- **Solution File**: `Retruxel.slnx` (Visual Studio 2022+ format)
- **Build Tool**: MSBuild via .NET SDK
- **IDE**: Visual Studio 2022 or later

### Project Types
- **WPF Application**: `<OutputType>WinExe</OutputType>`
- **Class Libraries**: `<OutputType>Library</OutputType>`
- **Target Framework**: `<TargetFramework>net10.0</TargetFramework>` or `net10.0-windows`

### Build Configuration
```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

### Custom Build Targets
Main application (`Retruxel.csproj`) includes post-build tasks:

1. **CopyModulesToFolder** - Copies `Retruxel.Modules.dll` to `modules/` folder
2. **CopyToolsToPluginsFolder** - Copies tool DLLs to `Plugins/Tools/`
3. **CopyCodeGensToPluginsFolder** - Copies JSON/template files to `Plugins/CodeGens/`
4. **CopyTargetsToPluginsFolder** - Copies target DLLs to `Plugins/Targets/`
5. **CleanPluginDllsFromRoot** - Removes plugin DLLs from root bin folder

## Embedded Toolchains

### SDCC (Small Device C Compiler)
- **Version**: 4.5.24
- **Target**: Z80 CPU (SMS, Game Gear, SG-1000, ColecoVision)
- **Location**: `Retruxel.Toolchain/Compilers/Sdcc/`
- **Components**: sdcc.exe, sdasz80.exe, sdldz80.exe

### cc65
- **Target**: 6502 CPU (NES)
- **Location**: `Retruxel.Toolchain/Compilers/Cc65/`
- **Components**: cc65.exe, ca65.exe, ld65.exe

### SDKs and Libraries

**devkitSMS** (SMS/GG/SG-1000):
- Location: `Retruxel.Toolchain/SDKs/Sega8Bit/devkitSMS/`
- Provides: SMSlib, crt0 startup code

**SMSlib**:
- Location: `Retruxel.Toolchain/SDKs/Sega8Bit/SMSlib/`
- Hardware abstraction for Sega 8-bit consoles

**neslib** (NES):
- Location: `Retruxel.Toolchain/SDKs/Nes/neslib/`
- NES hardware abstraction

**ColecoVision SDK**:
- Location: `Retruxel.Toolchain/SDKs/Coleco/`

### Utilities

**ihx2sms** (SMS/GG/SG-1000):
- Location: `Retruxel.Toolchain/Utils/Sega/ihx2sms.exe`
- Converts Intel HEX to SMS ROM format

**makebin** (ColecoVision):
- Location: `Retruxel.Toolchain/Utils/Sega/makebin.exe`
- Converts binary formats

**ld65** (NES):
- Location: `Retruxel.Toolchain/Utils/Nes/ld65.exe`
- Linker for cc65 toolchain

## Dependencies

### NuGet Packages
- **SkiaSharp** - 2D graphics library for image processing
- Standard .NET 10 libraries (System.*, Microsoft.*)

### External Libraries
- **LibRetro** - Emulator core integration (P/Invoke to native DLL)
  - Location: `Retruxel.Emulation/cores/genesis_plus_gx_libretro.dll`

## Development Commands

### Build Solution
```bash
# From solution root
dotnet build Retruxel.slnx

# Or in Visual Studio
Build → Build Solution (Ctrl+Shift+B)
```

### Run Application
```bash
# From Retruxel project folder
dotnet run --project Retruxel/Retruxel.csproj

# Or in Visual Studio
Debug → Start Debugging (F5)
```

### Clean Build
```bash
dotnet clean Retruxel.slnx
```

### Publish Release
```bash
dotnet publish Retruxel/Retruxel.csproj -c Release -o publish/
```

### Build Installer
```bash
# Requires Inno Setup installed
iscc installer.iss
```

## Runtime Environment

### Target Platform
- **OS**: Windows 10/11
- **Architecture**: x64 (primary), x86, ARM64 (via .NET runtime)

### Runtime Requirements
- .NET 10 Runtime (bundled with installer)
- Windows desktop environment (WPF dependency)

### Application Data
- **Toolchain Cache**: `%AppData%\Retruxel\toolchain\`
- **Settings**: `%AppData%\Retruxel\settings.json`
- **Recent Projects**: Stored in settings

## Code Generation Pipeline

### Template Processing
1. **Input**: `.c.rtrx` template + `codegen.json` manifest
2. **Processing**: `ModuleRenderer` + `TemplateEngine`
3. **Output**: `.c` and `.h` files

### Template Syntax
- Variable substitution: `{{variableName}}`
- Conditionals: `{{#if condition}}...{{/if}}`
- Negation: `{{#ifnot condition}}...{{/ifnot}}`
- Loops: `{{#each array}}...{{/each}}`
- Nested properties: `{{object.property}}`
- Arithmetic: `{{width * height}}`
- Loop index: `{{@index}}`

### Tool Invocation
Tools execute during code generation:
```json
{
  "variables": {
    "tileData": {
      "from": "tool",
      "tool": "png_to_tiles_sms",
      "toolInput": {
        "imagePath": "spritePath",
        "tileWidth": 8,
        "tileHeight": 8
      }
    }
  }
}
```

## Compilation Pipeline

### SMS/GG/SG-1000/ColecoVision
```
.c files → SDCC → .rel files → sdldz80 → .ihx → ihx2sms → .sms ROM
```

### NES
```
.c files → cc65 → .s files → ca65 → .o files → ld65 → .nes ROM
```

## Version Information

### Current Version
- **Version**: 0.8.0-alpha
- **Assembly Version**: 0.8.0.0
- **File Version**: 0.8.0.0

### Version History
- v0.8.0-alpha - Current (state-based rendering, engine architecture)
- v0.7.2-alpha - Previous release
- v0.7.1-alpha
- v0.7.0-alpha
- v0.6.0-alpha
- v0.5.0-alpha
- v0.4.0-alpha - Initial public release

## Design System

### Typography
- **Display Font**: Space Grotesk (variable weight)
- **Body/Code Font**: Inter (variable weight)
- **Location**: `Retruxel/Assets/Fonts/`

### Visual Style
- **Theme**: Neo-Technical Archive (1980s mainframe aesthetic)
- **Design Philosophy**: Architectural Brutalism + Modern Editorial
- **Border Radius**: 0px on all internal components
- **Grid**: 8px base unit
- **Separation**: Tonal background shifts (no divider lines)

### Localization
- **Supported Languages**: English (en), Portuguese Brazil (pt-BR)
- **Format**: JSON files in `Assets/Localization/`
- **Runtime Switching**: Yes, via LocalizationService
- **Fallback**: English

## Plugin System

### Discovery Mechanism
- **Method**: Reflection-based at startup
- **Scan Paths**: 
  - `Plugins/Targets/*.dll`
  - `Plugins/Tools/*.dll`
  - `modules/*.dll`
  - `Plugins/CodeGens/**/codegen.json`

### Plugin Interfaces
- `ITarget` - Platform implementations
- `ITool` - Asset converters and editors
- `IModule` - Game logic modules
- `IToolExtension` - Target-specific tool extensions
- `IRenderBackend` - Platform rendering engines
- `IToolchain` - Compiler abstractions

### Plugin Loading
```csharp
// Automatic discovery via ServiceLocator
var targets = TargetRegistry.GetAllTargets();
var tools = ToolRegistry.GetAllTools();
var modules = ModuleRegistry.GetAllModules();
```

## Testing Strategy

### Current Status
- No automated test suite (alpha stage)
- Manual testing via Kung Fu Master port
- Build validation through ROM compilation

### Planned
- Unit tests for Core services
- Integration tests for build pipeline
- UI automation tests for WPF views
