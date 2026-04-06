# Retruxel — Referência do Projeto

Ferramenta visual para desenvolvimento de jogos retro (inspirada no GB Studio), extensível via plugins (DLLs). Primeiro target: **Sega Master System**.

---

## Solution (.NET / WPF)

| Projeto | Tipo | Função |
|---|---|---|
| `Retruxel` | WPF Application | Shell principal — UI |
| `Retruxel.Core` | Class Library | Interfaces e contratos base |
| `Retruxel.SDK` | Class Library | Interfaces públicas para plugins |
| `Retruxel.Toolchain` | Class Library | SDCC + ihx2sms + SMSlib embutidos |
| `Retruxel.Target.SMS` | Class Library | Implementação específica do SMS |

---

## Arquitetura

- Fluxo: `.rtrxproject` → gera C → SDCC compila → `.rom`
- Target fixo na criação do projeto — imutável
- Eject para C disponível como função avançada irreversível
- Toolchain extraída automaticamente para `%AppData%\Retruxel\toolchain\` na primeira execução

### Sistema de Módulos
- Tipos: `IGraphicModule`, `ILogicModule` e `IAudioModule`
- DLLs carregadas dinamicamente — oficiais em `/modules/`, plugins em `/plugins/`
- `ModuleManifest` descreve parâmetros — UI gerada automaticamente pelo shell
- Três categorias: **Universal**, **Base + Especialização**, **Exclusivo** (ícone de aviso na UI)

### ToolchainValidator (modo Debug)
- Gera projeto sintético com uma instância de cada feature
- Cada módulo expõe `GetValidationSample()` — auto-descoberta
- Relatório salvo em `%AppData%\Retruxel\validator\{timestamp}\`

---

## Design System — "Neo-Technical Archive"

**Conceito:** "Nostalgia Sistêmica" — IDE moderno com estética de mainframe dos anos 80
**Estilo:** Brutalismo Arquitetônico + Design Editorial Moderno
**Documentação completa:** `docs/DESIGN.md`

### Regras Invioláveis
- **0px border-radius** em todos os componentes internos (janela principal: 4-6px permitido)
- **Sem linhas divisórias 1px sólidas** — separação apenas por tonal shift de fundo
- **Grid de 8px** — qualquer desalinhamento parece bug
- Texto corpo nunca em branco puro — usar `#adaaaa` (evita halation)
- Ghost Border apenas como fallback: 1px `outline_variant` a 15% opacidade
- Componentes aninhados sempre mais claros que o pai (layering principle)
- Sombras massivas e suaves para modais: `0px 24px 48px rgba(0,0,0,0.5)`

### Paleta de Cores
| Token | Hex | Uso |
|---|---|---|
| `surface` | `#0e0e0e` | Fundo base |
| `surface-container-low` | `#131313` | Sidebar, header |
| `surface-container-high` | `#1e1e1e` | Cards, painéis |
| `surface-container-highest` | `#262626` | Elementos interativos |
| `primary` | `#8eff71` | Ação principal, sucesso, build |
| `primary_dim` | `#2be800` | Hover do primary |
| `secondary` | `#7c3aed` | Roxo — blocos de lógica |
| `tertiary` | `#81ecff` | Ciano — informação, leitura |
| `on_surface_variant` | `#adaaaa` | Texto corpo |
| `on_primary` | `#000000` | Texto sobre botão primary |

Gradiente botão primary: `135deg → #8eff71 para #2be800`

### Tipografia
| Uso | Fonte | Arquivo |
|---|---|---|
| Display / Títulos | Space Grotesk | `SpaceGrotesk-VariableFont_wght.ttf` |
| Corpo / Labels / Código | Inter | `Inter-VariableFont_opsz,wght.ttf` |

Fontes em `Retruxel/Fonts/` com Build Action **Resource**.

```xml
FontFamily="/Retruxel;component/Fonts/SpaceGrotesk-VariableFont_wght.ttf#Space Grotesk"
FontFamily="/Retruxel;component/Fonts/Inter-VariableFont_opsz,wght.ttf#Inter"
```

---

## Mapa de Telas

| # | Tela | Prioridade | Status |
|---|---|---|---|
| 1 | Welcome / New Project | Alta | 🔲 |
| 2 | Project Dashboard | Alta | 🔲 |
| 3 | Asset Manager | Alta | 🔲 |
| 4 | Tile Editor | Alta | 🔲 |
| 5 | Tilemap Editor | Alta | 🔲 |
| 6 | Sprite Editor | Média | 🔲 |
| 7 | Logic Editor | 🔖 Depois | 🔲 |
| 8 | Module Settings | Alta | 🔲 |
| 9 | Build Console | Alta | 🔲 |
| 10 | Target Settings | Alta | 🔲 |

- Tela 1: cards de console (blocos ↔ lista), projetos recentes com status (COMPILED, BUILDING, FAILED)
- Target Settings gerado automaticamente via `ITarget` — mesmo sistema do `ModuleManifest`

---

## Ambiente SMS

| Item | Detalhe |
|---|---|
| SDCC | 4.5.24 (MINGW64) |
| devkitSMS | `F:\Junior\Desenvolvimento de Jogos\Ports\Master System\devkitSMS` |
| SMSlib | Recompilada — `SMSlib_readVRAM` removido (bug SDCC 4.5.24) |
| Emulador | Emulicious (SMS) + Mesen (NES para análise) |

---

## Port — Kung Fu Master (NES → SMS)

- Beat em up simples — abordagem grey box primeiro
- Linguagem: C com devkitSMS
- Resolução: 256×240 (NES) → 256×192 (SMS)
- Pause: no controle (NES) → no console via NMI (SMS)

### Controles SMS
| Função | Controle |
|---|---|
| Soco | Botão 1 — tap |
| Pulo | Botão 2 — tap |
| Chute | Botão 1 — segurado 1s+ |
| Menu | Botão 2 — segurado 1s+ |

---

## Retruxel.Core — Implementado

### Interfaces (`Retruxel.Core/Interfaces/`)
| Interface | Função |
|---|---|
| `IModule` | Contrato base — `ModuleId`, `Serialize/Deserialize`, `GetValidationSample()` |
| `IGraphicModule` | `CreateEditorViewModel()`, `GenerateCode()`, `GenerateAssets()` |
| `ILogicModule` | `GetManifest()`, `GenerateCode()` |
| `IAudioModule` | Chip de som, canais, `CreateEditorViewModel()`, `GenerateCode()`, `GenerateAssets()` |
| `ITarget` | Specs, toolchain, módulos builtin, templates, settings |
| `IToolchain` | `ExtractAsync()`, `BuildAsync()`, `VerifyAsync()` |

### Models (`Retruxel.Core/Models/`)
| Classe | Função |
|---|---|
| `ModuleManifest` + `ParameterDefinition` + `ParameterType` | UI auto-gerada pelo shell |
| `RetruxelProject` | Estado completo do `.rtrxproject` |
| `BuildContext` | Entrada do pipeline de build |
| `BuildResult` + `BuildLogEntry` + `BuildLogLevel` | Saída do pipeline de build |
| `GeneratedFile` | Arquivo `.c`/`.h` gerado por um módulo |
| `GeneratedAsset` | Asset binário gerado por um módulo |
| `ProjectTemplate` | Templates do wizard de novo projeto |
| `TargetSpecs` | Specs de hardware do console |

### Services (`Retruxel.Core/Services/`)
| Classe | Função |
|---|---|
| `CodeGenerator` | Orquestra geração de código de todos os módulos, gera `main.c` |
| `ModuleLoader` | Carrega DLLs de `/modules/` e `/plugins/` via reflection |
| `ProjectManager` | CRUD do `.rtrxproject` com serialização JSON, evento `ProjectChanged` |

---

## Próximos Passos

1. `ResourceDictionary` WPF com paleta e estilos base
2. Shell principal com janela customizada (CornerRadius 4-6px)
3. Tela 1 — Welcome / New Project
4. Grey box Kung Fu Master (loop principal + input)

### Ferramentas .NET planejadas
- Conversor de paleta NES → SMS
- Extrator de tiles para arrays C
- Visualizador de nametable / mapa de fase

---

## Notas
- VS 2026 usa termos em português ("Biblioteca de Classes", "Adicionar → Novo Projeto")
- Logic Editor adiado — não é prioridade para o SMS
- Design system completo nos arquivos `stitch__1_.zip` a `stitch__4_.zip` + `DESIGN.md`
