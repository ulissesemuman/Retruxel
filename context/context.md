# Retruxel — Contexto Amazon Q
> Atualizado: 2026-04-06 | Versão do projeto: 0.4.0-alpha

---

## Visão Geral

IDE visual para desenvolvimento de jogos retro, inspirado no GB Studio. Suporte multi-target: Sega Master System (ativo) e Nintendo NES (ativo).

**Fluxos de build:**
- SMS/GG/SG-1000/ColecoVision: `.rtrxproject` → CodeGenerator → `.c/.h` → SDCC → ihx2sms → `.sms ROM`
- NES: `.rtrxproject` → CodeGenerator → `.c/.h` → cc65 → ld65 → `.nes ROM`

---

## Solution — Projetos

| Projeto | Tipo | Estado |
|---|---|---|
| `Retruxel` | WPF Application | ✅ Funcional |
| `Retruxel.Core` | Class Library | ✅ Completo |
| `Retruxel.SDK` | Class Library | ✅ Stub (re-exporta Core) |
| `Retruxel.Modules` | Class Library | ✅ TextDisplayModule |
| `Retruxel.Target.SMS` | Class Library | ✅ Funcional |
| `Retruxel.Target.NES` | Class Library | ✅ Funcional (cc65 + neslib) |
| `Retruxel.Target.GameGear` | Class Library | 🟡 Scaffolding |
| `Retruxel.Target.SG1000` | Class Library | 🟡 Scaffolding |
| `Retruxel.Target.ColecoVision` | Class Library | 🟡 Scaffolding |
| `Retruxel.Tools` | Class Library | 🚧 FontRasterizer (incompleto) |

---

## Retruxel.Core — Estado Atual

### Interfaces (`/Interfaces/`)
| Interface | Estado | Notas |
|---|---|---|
| `IModule` | ✅ | ModuleId, Serialize/Deserialize, GetValidationSample |
| `IGraphicModule` | ✅ | CreateEditorViewModel, GenerateCode, GenerateAssets |
| `ILogicModule` | ✅ | GetManifest, GenerateCode |
| `IAudioModule` | ✅ | ChipName, ToneChannels, NoiseChannels, CreateEditorViewModel, GenerateCode, GenerateAssets |
| `ITarget` | ✅ | GetHardwarePalette, GetToolchain, GetBuiltinModules, GetTemplates, GetSettingsDefinitions, GenerateCodeForModule, GenerateMainFile |
| `IToolchain` | ✅ | ExtractAsync, BuildAsync, VerifyAsync |

### Models (`/Models/`)
| Classe | Estado | Notas |
|---|---|---|
| `ModuleManifest` + `ParameterDefinition` + `ParameterType` | ✅ | ParameterType: Int, Float, Bool, Enum, SpriteRef, TileRef, AudioRef, String |
| `RetruxelProject` | ✅ | FormatVersion, Name, ProjectPath, TargetId, Scenes, Parameters, ModuleStates (deprecated) |
| `SceneData` + `SceneElementData` | ✅ | SceneId, SceneName, Elements (ElementId, ModuleId, TileX, TileY, Trigger, ModuleState) |
| `BuildContext` | ✅ | BuildId, TargetId, SourceFiles, Assets, BuildParameters, OutputDirectory |
| `BuildResult` + `BuildLogEntry` + `BuildLogLevel` | ✅ | Success, RomPath, RomSizeBytes, RomMd5, RomSha256, Log |
| `GeneratedFile` + `GeneratedFileType` | ✅ | Source / Header |
| `GeneratedAsset` + `GeneratedAssetType` | ✅ | Tiles, Palette, Tilemap, Sprites, Audio, Raw |
| `HardwareColor` | ✅ | record(R,G,B), ToHex(), FromHex() |
| `AppSettings` | ✅ | General (Language, ShowWelcomeOnStartup, LastProjectLocation, ShowAllModules, RecentProjects, FavoriteTargets), Appearance (FontSize), Toolchain (ShowToolchainWarnings), Targets (Sms, Nes, GameGear, Sg1000, ColecoVision) — cada target com EmulatorPath, EmulatorArguments, LaunchEmulatorAfterBuild |
| `ProjectTemplate` | ✅ | TemplateId, DisplayName, Description, PreviewImagePath, DefaultModules, DefaultParameters |
| `TargetSpecs` | ✅ | Screen, Tiles, Colors/Palettes, Sprites, Memory, CPU, Sound, Manufacturer |

### Services (`/Services/`)
| Classe | Estado | Notas |
|---|---|---|
| `CodeGenerator` | ✅ | Itera scenes → instancia módulos → chama target.GenerateCodeForModule → gera main.c |
| `ModuleLoader` | ✅ | Carrega DLLs de `/modules/` e `/plugins/` via reflection; RegisterLogicModule manual |
| `ProjectManager` | ✅ | CreateProject, SaveAsync, LoadAsync, Close, MarkDirty, ClearDirtyFlag, evento ProjectChanged |
| `LocalizationService` | ✅ | Singleton, DiscoverLanguages, DetectSystemLanguage, Load, Get, indexer this[key] |
| `SettingsService` | ✅ | Static, LoadAsync/Load, SaveAsync/Save — persiste em `%AppData%\Retruxel\settings.json` |
| `ToolchainManager` | ✅ | Register, GetToolchain, HasToolchain, RegisteredTargets |
| `TargetRegistry` | ✅ | Singleton, Register, GetTarget, GetAllTargets, GetManufacturers, Initialize — descobre fabricantes dinamicamente |

---

## Retruxel.Modules — Estado Atual

| Módulo | ModuleId | Tipo | Estado |
|---|---|---|---|
| `TextDisplayModule` | `text.display` | ILogicModule | ✅ Universal — Serialize/Deserialize JSON, GetManifest, GetValidationSample |

---

## Retruxel.Target.SMS — Estado Atual

### SmsTarget
- TargetId: `"sms"` | Manufacturer: `"Sega"` | CPU: Zilog Z80 @ 3.546MHz | RAM: 8KB | VRAM: 16KB
- Paleta: 64 cores (2-bit RGB, 4 níveis por canal)
- Tela: 256×192 | Tiles: 8×8 | MaxTilesVRAM: 448
- Sprites: 8×8 (ou 8×16), max 64 na tela, 8 por scanline
- Som: SN76489 (3 tom + 1 ruído)
- Templates: `sms.blank`, `sms.platformer`, `sms.beatemup`
- Settings: region (NTSC/PAL), romSize (32/128/256/512KB), fmSound (bool)
- `GenerateCodeForModule`: só `text.display` implementado
- `GenerateMainFile`: gera `main.c` com headers, init calls, loop VBlank, ROM header SDSC

### SmsToolchain
- Extrai binários embutidos para `%AppData%\Retruxel\toolchain\sms\`
- Build: SDCC compila cada `.c` → `.rel`, linka tudo → `.ihx`, ihx2sms → `.sms`
- Flags: `-mz80 --no-std-crt0 --sdcccall 1 --data-loc 0xC000`
- Calcula MD5 e SHA-256 do ROM gerado
- Suporta supressão de warnings via settings
- **Nota:** SMSlib foi recompilada do zero para SDCC 4.5.24 — `SMSlib_readVRAM` removido por bug de compatibilidade

### SmsTextDisplayCodeGen
- Gera `text_display_{id}.c` e `text_display_{id}.h`
- Instância 0: inicializa VRAM, autoSetUpTextRenderer, displayOn
- Valida limites: X 0-31, Y 0-23
- `ResetCounter()` deve ser chamado antes de cada build (chamado por SmsTarget.ResetCodeGenerationState)

---

## Retruxel.Target.NES — Estado Atual

### NesTarget
- TargetId: `"nes"` | Manufacturer: `"Nintendo"` | CPU: Ricoh 2A03 @ 1.789MHz | RAM: 2KB | VRAM: 2KB
- Paleta: 54 cores (sistema fixo)
- Tela: 256×240 | Tiles: 8×8 | MaxTilesVRAM: 512
- Sprites: 8×8 (ou 8×16), max 64 na tela, 8 por scanline
- Som: 2A03 (2 pulse + 1 triangle + 1 noise + 1 DMC)
- Templates: `nes.blank`
- Settings: region (NTSC/PAL), mapper (0-NROM)
- `GenerateCodeForModule`: só `text.display` implementado
- `GenerateMainFile`: gera `main.c` com headers, init calls, loop VBlank, declara `oam_off` (zero page)
- **ResetCodeGenerationState**: chama `NesTextDisplayCodeGen.ResetCounter()`

### NesToolchain
- Extrai binários embutidos para `%AppData%\Retruxel\toolchain\nes\`
- Binários: cc65, ca65, ld65, ar65 (cc65 toolchain)
- Build: cc65 compila cada `.c` → `.s`, ca65 monta `.s` → `.o`, cria `nes_config.s` com defines (NES_MAPPER, NES_PRG_BANKS, NES_CHR_BANKS, NES_MIRRORING), ld65 linka tudo → `.nes`
- Config: `nes.cfg` com ZP=$00FE (254 bytes), PRG ROM, CHR ROM
- Calcula MD5 e SHA-256 do ROM gerado
- Organiza output: ROM na raiz de `build/`, sources em `build/src/`

### NesTextDisplayCodeGen
- Gera `text_display_{instanceId}.c` e `text_display_{instanceId}.h` (suporta múltiplas instâncias)
- Instância 0: inicializa PPU, ppu_on_all
- Valida limites: X 0-31, Y 0-29
- `ResetCounter()` deve ser chamado antes de cada build

### Recursos Embutidos
- **bin/**: ar65.exe, ca65.exe, cc65.exe, ld65.exe
- **include/**: crt0.s, nes.h, neslib.h, neslib.sinc, peekpoke.h, stdint.h, longbranch.mac, zeropage.inc, display.sinc, famitone2.sinc
- **lib/**: nes.cfg, nes.lib

---

## Retruxel (WPF Shell) — Estado Atual

### Estrutura
```
MainWindow
├── TitleBar (drag, minimize, maximize, close, settings, home button)
├── Content
│   ├── WelcomeView (visível por padrão)
│   └── SceneEditorView (visível quando projeto aberto)
└── StatusBar (BUILD: READY | versão)
```

### Overlay System
- `OverlayLayer` sobre o conteúdo principal
- Backdrop semi-transparente + modal arrastável
- Usado para: BuildConsole, About, (futuro: outros diálogos)

### Views Implementadas
| View | Estado | Notas |
|---|---|---|
| `WelcomeView` | ✅ | Cards grid/lista, projetos recentes, drag-and-drop .rtrxproject, sidebar, sort (Name/Manufacturer), filter (All/Favorites/Sega/Nintendo/Coleco), favorites system (star icons), dynamic manufacturer discovery |
| `SceneEditorView` | ✅ | Canvas 256×192, paleta de módulos, painel de eventos, painel de propriedades |
| `BuildConsoleView` | ✅ | Terminal de log, export ROM, export debug ZIP, verificação MD5/SHA256, stats de memória, toast notifications, emulator launch (target-specific) |
| `NewProjectDialog` | ✅ | Nome, localização, target, templates |
| `TargetSelectionDialog` | ✅ | Seleção de target antes do NewProjectDialog |
| `SettingsWindow` | ✅ | General (idioma, welcome), Appearance (theme display), Toolchain (global warnings), SMS/NES/GG/SG1000/Coleco tabs (emulador, launch after build) |
| `AboutView` | ✅ | Informações do app |
| `SplashScreen` | ✅ | Boot sequence animada, progress bar, log terminal |

### TargetRegistry
- Registra: `SmsTarget`, `NesTarget`, `GameGearTarget`, `Sg1000Target`, `ColecoVisionTarget`
- `Initialize()`: descobre fabricantes dinamicamente de todos os targets registrados
- `GetManufacturers()`: retorna lista única de fabricantes
- Usado por WelcomeView para listar targets e filtros dinâmicos

### Localization
- Arquivos JSON em `Retruxel/Assets/Localization/`
- Idiomas: `en.json`, `pt-BR.json`
- Extensão XAML: `{loc:Tr Key='chave'}`
- Runtime switching sem restart

### Tema (RetruxelTheme.xaml)
- Fontes: Space Grotesk (display), Inter (body)
- Cores: surface #0e0e0e → highest #262626, primary #8eff71, secondary #7c3aed, tertiary #81ecff
- Estilos: TextDisplay, TextHeadline, TextLabel, TextBody, TextCode
- Botões: ButtonPrimary (gradiente verde), ButtonSecondary (roxo)
- Controles: ToggleSwitch, TextBox, ComboBox, ScrollBar customizados
- Regra: 0px border-radius em todos os componentes internos

---

## Ambiente de Desenvolvimento

| Item | Status | Localização |
|---|---|---|
| SDCC | ✅ 4.5.24 (MINGW64) | Embutido no toolchain SMS |
| cc65 | ✅ 2.19 | Embutido no toolchain NES |
| devkitSMS | ✅ | `F:\Junior\Desenvolvimento de Jogos\Ports\Master System\devkitSMS` |
| SMSlib | ✅ | Recompilada para SDCC 4.5.24 |
| neslib | ✅ | Embutida no toolchain NES |
| ihx2sms | ✅ | No PATH do sistema |
| Emulicious | ✅ | Emulador SMS com debugger |
| Mesen | ✅ | Emulador NES (para análise) |

---

## Port — Kung Fu Master (NES → SMS)

**Status:** Grey box planejado

### Decisões de Design
- Beat em up simples — ideal para primeiro port
- Abordagem: grey box primeiro, física ajustada visualmente
- Linguagem: C com devkitSMS

### Mapeamento de Controles SMS
| Função | Controle |
|---|---|
| Soco | Botão 1 — tap |
| Pulo | Botão 2 — tap |
| Chute | Botão 1 — segurado 1s+ |
| Menu | Botão 2 — segurado 1s+ |

### Diferenças Técnicas NES → SMS
| Item | NES | SMS |
|---|---|---|
| Resolução | 256×240 | 256×192 |
| Paleta | 54 cores | 64 cores |
| Som | 2A03 (5 canais) | SN76489 (3 tom + 1 ruído) |
| Pause | No controle | **No console (NMI)** |

---

## Problemas Conhecidos / Dívidas Técnicas

| # | Problema | Severidade | Arquivo |
|---|---|---|---|
| 1 | Targets scaffolding (GG, SG1000, Coleco) não têm implementação real — apenas estrutura | 🟡 Média | `Retruxel.Target.GameGear/`, `Retruxel.Target.SG1000/`, `Retruxel.Target.ColecoVision/` |
| 2 | `ModuleStates` em `RetruxelProject` marcado como DEPRECATED mas ainda presente | 🟢 Baixa | `Retruxel.Core/Models/RetruxelProject.cs` |
| 3 | `Retruxel.Tools/FontRasterizer` existe mas não integrado em nenhum fluxo | 🟢 Baixa | `Retruxel.Tools/` |
| 4 | Biblioteca de módulos limitada — apenas TextDisplayModule implementado | 🟡 Média | `Retruxel.Modules/` |
| 5 | Sem asset editors (tiles, sprites, paletas) | 🟡 Média | — |

---

## Próximos Passos (Ordenados por Prioridade)

### Imediato
- [x] **Generalizar SceneEditorView** — usar ModuleLoader + ModuleManifest para UI genérica
- [x] **NES Target completo** — toolchain real (cc65 + neslib)
- [x] **Multi-target infrastructure** — 5 plataformas registradas
- [x] **Favorites system** — star icons, filter, persist
- [x] **Dynamic manufacturer discovery** — sem hardcode
- [x] **Emulator integration** — launch target-specific
- [x] **Toast notifications** — feedback visual
- [x] **GitHub Actions** — automated releases com instalador
- [ ] **Kung Fu Master grey box** — estrutura de projeto, loop principal, input

### Curto Prazo
- [ ] **Asset Manager** (Tela 3) — import de imagens, tiles, sprites, paletas
- [ ] **Tile Editor** (Tela 4) — grade 8×8, paleta SMS, preview
- [ ] **Tilemap Editor** (Tela 5) — nametable, arrastar tiles, colisões
- [ ] **Módulo de tiles SMS** — `IGraphicModule` para tiles/paleta

### Médio Prazo
- [ ] **Sprite Editor** (Tela 6)
- [ ] **ToolchainValidator** (modo Debug) — projeto sintético com `GetValidationSample()` de cada módulo
- [ ] **GG/SG1000/Coleco Targets completos** — implementação real além de scaffolding
- [ ] **Ferramentas .NET** — conversor de paleta NES→SMS, extrator de tiles, visualizador de nametable
- [ ] **Mais módulos** — sprite, input, collision, audio

### Longo Prazo
- [ ] **Plugin system** — auto-descoberta de DLLs em `/plugins/`
- [ ] **Logic Editor** (Tela 7) — editor de nós visual
- [ ] **Migração entre targets** — portabilidade de projetos universais

---

## Regras de Design (Invioláveis)

- **0px border-radius** em todos os componentes internos (janela: 4-6px permitido)
- **Sem linhas divisórias 1px** — separação por tonal shift de fundo
- **Grid de 8px** — sem exceções
- Texto corpo: `#adaaaa` (nunca branco puro)
- Ghost Border: 1px `#26ADAAAA` apenas como fallback
- Componentes aninhados sempre mais claros que o pai

## Paleta de Cores (Tokens)

| Token WPF | Hex | Uso |
|---|---|---|
| `BrushSurface` | `#0e0e0e` | Fundo base |
| `BrushSurfaceContainerLow` | `#131313` | Sidebar, header, status bar |
| `BrushSurfaceContainerHigh` | `#1e1e1e` | Cards, painéis |
| `BrushSurfaceContainerHighest` | `#262626` | Elementos interativos |
| `BrushPrimary` | `#8eff71` | Ação principal, sucesso, build |
| `BrushPrimaryDim` | `#2be800` | Hover do primary |
| `BrushSecondary` | `#7c3aed` | Roxo — lógica, botões secundários |
| `BrushTertiary` | `#81ecff` | Ciano — informação |
| `BrushOnSurfaceVariant` | `#adaaaa` | Texto corpo |
| `BrushError` | `#ff4444` | Erros |
| `BrushWarning` | `#ffaa00` | Avisos |

---

## Sistema de Módulos — Categorias de Portabilidade

| Categoria | Descrição | Exemplo |
|---|---|---|
| **Universal** | JSON idêntico em qualquer target — totalmente portável | `{"module": "text.display", "x": 10, "y": 5, "text": "Hello"}` |
| **Base + Especialização** | JSON base compartilhado + campos opcionais por target | `{"module": "sprite.render", "x": 32, "y": 64, "tile": 4, "sms_priority": true}` |
| **Exclusivo** | JSON só existe para aquele target — ícone de aviso na UI | `{"module": "snes.mode7", "angle": 45, "scale": 1.5}` |

---

## Arquivos de Referência

| Arquivo | Conteúdo |
|---|---|
| `RELEASE_v0.4.0-alpha.md` | Release notes da primeira versão pública |
| `installer.iss` | Script Inno Setup para instalador Windows |
| `.github/workflows/release-with-installer.yml` | GitHub Actions para releases automáticas |
| `.amazonq/context.md` | Este arquivo — contexto consolidado |

---

## Git Workflow

- **Branch `master`**: Versões estáveis, releases
- **Branch `dev`**: Desenvolvimento ativo, features integradas
- **Releases automáticas**: Push na `master` → GitHub Actions → ZIP + Instalador
- **Convenção de commits**: `feat:`, `fix:`, `docs:`, `chore:` para changelog automático
