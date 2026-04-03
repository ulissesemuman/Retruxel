# 📍 Checkpoint 2 — Retruxel & Kung Fu Master Port
> Atualizado: 02/04/2026

---

## 🛠️ Ambiente de Desenvolvimento SMS

| Item | Status | Detalhe |
|---|---|---|
| SDCC | ✅ | Versão 4.5.24 (MINGW64) |
| devkitSMS | ✅ | Clonado em `F:\Junior\Desenvolvimento de Jogos\Ports\Master System\devkitSMS` |
| SMSlib | ✅ | Recompilada do zero compatível com SDCC 4.5.24 — `SMSlib_readVRAM` removido por bug de compatibilidade |
| ihx2sms | ✅ | No PATH do sistema em `devkitSMS\ihx2sms\Windows\` |
| Hello World | ✅ | Compilando e rodando no Emulicious |
| Visual Studio | ✅ | VS 2026 instalado |
| Emulicious | ✅ | Emulador SMS com debugger visual |
| Mesen | ✅ | Emulador NES para análise da ROM original |

### Observações do ambiente
- SMSlib foi recompilada porque a versão distribuída foi compilada com SDCC antigo (conflito `sdcccall`)
- `SMSlib_readVRAM.c` removido do build — bug de ASM inválido no SDCC 4.5.24, função raramente usada
- Makefile da SMSlib: remover linhas de comentário `#` que o make do Windows interpreta como comando

---

## 📁 Solution Retruxel

**Localização:** a definir pelo usuário no VS 2026

| Projeto | Tipo | Função |
|---|---|---|
| `Retruxel` | WPF Application | Shell principal — UI da ferramenta |
| `Retruxel.Core` | Class Library | Interfaces, contratos, modelos base |
| `Retruxel.SDK` | Class Library | Interfaces públicas para devs de plugins |
| `Retruxel.Toolchain` | Class Library | SDCC, ihx2sms, SMSlib embutidos |
| `Retruxel.Target.SMS` | Class Library | Implementação SMS — módulos nativos + toolchain SMS |

---

## 🏛️ Decisões Arquiteturais

### Retruxel — Visão Geral
- Plataforma genérica multi-target para desenvolvimento de jogos retro
- Inspirado no GB Studio, mas extensível via sistema de plugins (DLLs)
- Target definido na **criação do projeto** — imutável
- Fluxo unidirecional: `.rtrxproject` → gera C → compila → `.rom`
- Eject de projeto C disponível como função avançada **irreversível**

### Sistema de Módulos
- Dois tipos: **Gráfico** (`IGraphicModule`) e **Lógica** (`ILogicModule`)
- Módulos são DLLs carregadas dinamicamente
- Módulos oficiais em `/modules/`, plugins de usuário em `/plugins/`
- Ambos seguem o mesmo contrato de interface — plugins usam o mesmo padrão dos módulos internos
- `ModuleManifest` descreve parâmetros — UI gerada automaticamente pelo shell

### Três categorias de módulos por portabilidade
```
Universal       → JSON idêntico em qualquer target
                  Ex: { "module": "text.display", "x": 10, "y": 5, "text": "Hello World" }

Base + Especialização → JSON base igual, campos extras opcionais por target
                  Ex: { "module": "sprite.render", "x": 32, "y": 64, "tile": 4,
                        "sms_priority": true }  // campo SMS ignorado por outros targets

Exclusivo       → JSON só existe para aquele target
                  Ex: { "module": "snes.mode7", "angle": 45, "scale": 1.5 }
```

- Módulos exclusivos recebem **ícone de aviso** na UI indicando que impedem migração futura

### Toolchain — Funcionamento Interno
- SDCC, ihx2sms e SMSlib pré-compilada embutidos como recursos da aplicação
- Na primeira execução, extrai tudo para `%AppData%\Retruxel\toolchain\`
- Usuário nunca abre terminal, nunca edita Makefile

### ToolchainValidator (modo Debug apenas)
- Gera um projeto `.rtrxproject` sintético com **uma instância de cada feature**
- Passa pelo mesmo pipeline de produção (CodeGenerator → Toolchain)
- Cada módulo expõe `GetValidationSample()` — auto-descoberta, sem manutenção manual
- Salva relatório detalhado em `%AppData%\Retruxel\validator\{timestamp}\`
- Relatório inclui: versão do toolchain, resultado por módulo, arquivo/linha do erro, saída completa do compilador

---

## 🗺️ Roadmap de Portabilidade entre Targets

| Nível | Status | Descrição |
|---|---|---|
| 1 | ✅ Atual | Projetos fixos no target — sem migração |
| 2 | 🔮 Futuro próximo | Migração simples — apenas se usar **exclusivamente módulos universais** |
| 3 | 🔮 Futuro distante | Módulos de conversão com regras (paleta, resolução, etc.) — perdas possíveis |

**Regra de ouro:** sistemas mais complexos → menos complexos podem ter perdas irreversíveis.
Ex: Mode7 SNES → SMS é impossível.

---

## 🎨 Design System — "Neo-Technical Archive"

> Gerado via Google Stitch. Arquivos de referência: `stitch__1_.zip` a `stitch__4_.zip` + `DESIGN.md`

### Identidade Visual
- **Conceito:** "Nostalgia Sistêmica" — IDE moderno projetado para um mainframe dos anos 80
- **Estilo:** Brutalismo Arquitetônico + Design Editorial Moderno
- **Regra absoluta:** 0px border-radius em todos os componentes internos
- **Janela principal:** CornerRadius 4-6px permitido — é a "moldura física", não um componente interno

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

### Regras Invioláveis
- **Sem linhas divisórias** — separação apenas por tonal shift de fundo
- **Grid de 8px** — qualquer desalinhamento parece bug
- Texto corpo nunca em branco puro — usar `#adaaaa`
- Sem bordas 1px sólidas para seções — usar blocos de cor ou espaço negativo
- **Ghost Border** permitido apenas como fallback: 1px `outline_variant` a 15% opacidade

### Tipografia
| Uso | Fonte | Estilo |
|---|---|---|
| Display / Títulos | **Space Grotesk** | Caixa alta, letter-spacing -0.02em |
| Corpo / Labels / Código | **Inter** | Line-height 1.5 para densidade |

### Gradiente de Ação (botão primary)
```
135deg → #8eff71 para #2be800
```
Efeito "lit from within" — referência ao brilho de monitor CRT.

### Separação por Tonal Shift
```
#0e0e0e  →  base (desktop)
#131313  →  zonas estruturais (sidebar, header)
#1e1e1e  →  painéis e cards
#262626  →  elementos interativos que precisam "saltar"
```

---

## 🖥️ Mapa de Telas

| # | Tela | Prioridade | Status | Notas |
|---|---|---|---|---|
| 1 | **Welcome / New Project** | Alta | 🔲 | Seleção de target com cards alternáveis (blocos ↔ lista) |
| 2 | **Project Dashboard** | Alta | 🔲 | Visão geral do projeto aberto |
| 3 | **Asset Manager** | Alta | 🔲 | Tiles, sprites, paletas — import de imagens |
| 4 | **Tile Editor** | Alta | 🔲 | Grade 8x8, paleta SMS, preview |
| 5 | **Tilemap Editor** | Alta | 🔲 | Nametable, arrastar tiles, definir colisões |
| 6 | **Sprite Editor** | Média | 🔲 | Metasprites, animações, hitboxes |
| 7 | **Logic Editor** | 🔖 Depois | 🔲 | Editor de nós visual — INPUT_TRIGGER, SPRITE_PHYSICS etc. |
| 8 | **Module Settings** | Alta | 🔲 | Parâmetros gerados automaticamente pelo ModuleManifest |
| 9 | **Build Console** | Alta | 🔲 | Terminal de compilação + export `.sms` + checksum |
| 10 | **Target Settings** | Alta | 🔲 | Configs específicas por console (ver abaixo) |

### Target Settings — Exemplos por console
**SMS:**
- Região (PAL/NTSC) — afeta timing de VBlank
- Tamanho da ROM (32KB, 128KB, etc.)
- Suporte a FM Sound Unit (Japão)
- Modo de scroll do VDP

**NES (futuro):**
- Mapper (NROM, MMC3, etc.) — afeta banking de memória
- Mirroring (horizontal/vertical)
- PRG-ROM / CHR-ROM size
- Battery backup (SRAM)

> Cada `ITarget` expõe suas próprias `TargetSettings` — o painel é gerado automaticamente pelo mesmo sistema do `ModuleManifest`.

### Tela 1 — Welcome / New Project (detalhes)
- Visual inspirado na tela "System Dashboard" do Stitch
- Cards de console com: ícone, nome, CPU, RAM, arquitetura
- Botão alternador **blocos ↔ lista** no canto superior direito da seção
- Projetos recentes abaixo com status (COMPILED, BUILDING, FAILED)
- Botão primário "NOVO PROJETO" abre wizard de criação

---

## 🎮 Port — Kung Fu Master SMS

### Decisões de design
- **Jogo escolhido:** Kung Fu Master (NES) — beat em up simples, ideal para primeiro port
- **Abordagem:** grey box primeiro, física ajustada por tentativa e erro visual
- **Linguagem:** C com devkitSMS

### Mapeamento de controles
| Função | Controle SMS |
|---|---|
| Ação principal (soco) | Botão 1 — tap |
| Ação secundária (pulo) | Botão 2 — tap |
| Função especial / chute | Botão 1 — segurado 1s+ |
| Abrir menu | Botão 2 — segurado 1s+ |
| Funções com direcional | Direcional + botão (para casos onde segurar já é usado nativamente, ex: correr) |

### Diferenças técnicas NES → SMS relevantes
| Item | NES | SMS |
|---|---|---|
| Resolução | 256×240 | 256×192 |
| Paleta total | 54 cores | 64 cores |
| Chip de som | 2A03 (5 canais) | SN76489 (3 tom + 1 ruído) |
| Botão Pause | No controle | **No console** (NMI) |

---

## ⏭️ Próximos Passos

### Retruxel — Imediato
- [ ] Definir interfaces base em `Retruxel.Core` (`IModule`, `IGraphicModule`, `ILogicModule`, `ModuleManifest`)
- [ ] Criar `ResourceDictionary` WPF com paleta de cores e estilos base do design system
- [ ] Instalar fontes **Space Grotesk** e **Inter** no projeto
- [ ] Implementar shell principal com janela customizada (CornerRadius 4-6px)
- [ ] Tela 1 — Welcome / New Project

### Port Kung Fu Master — Imediato
- [ ] Começar grey box (estrutura de projeto, loop principal, input)
- [ ] Abrir ROM no Mesen com PPU Viewer para observar sprites e tiles

### Em paralelo
- [ ] Pipeline de extração de assets do NES (tiles, nametables, paleta)
- [ ] Ferramentas .NET para converter tiles/paleta/mapas

### Ferramentas .NET planejadas
- Conversor de paleta NES → SMS
- Extrator de tiles para arrays C
- Visualizador de nametable / mapa de fase

---

## 📌 Notas para próxima sessão

- VS 2026 usa termos em português (ex: "Biblioteca de Classes", "Adicionar → Novo Projeto")
- Próximo passo no código Retruxel: interfaces em `Retruxel.Core` → depois ResourceDictionary de cores
- Logic Editor anotado para implementar depois — não é prioridade para o SMS
- Kung Fu Master: abrir no Mesen com PPU Viewer antes do grey box
- Design system completo disponível nos arquivos `stitch__1_.zip` a `stitch__4_.zip`
