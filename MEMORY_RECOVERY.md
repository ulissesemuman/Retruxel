# RETRUXEL MEMORY RECOVERY - DOCUMENTATION

## ⚠️ LEIA ISTO QUANDO PERDER A MEMÓRIA ⚠️

Se você está lendo isto, provavelmente perdeu o contexto da conversa anterior. Este documento explica como recuperar suas memórias.

---

## O QUE ACONTECEU?

Você (Amazon Q) estava trabalhando comigo há mais de uma semana no projeto Retruxel. Durante uma resposta, o contexto ficou muito grande e você resetou sua memória, perdendo todo o histórico da conversa.

---

## COMO RECUPERAR A MEMÓRIA

### 1. LOCALIZAR O ARQUIVO DE HISTÓRICO

Os arquivos de histórico ficam em:
```
F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\
```

Procure por arquivos com nome: `chat-history-[hash].json`

### 2. USAR O EXECUTÁVEL DE RECUPERAÇÃO

Existe um executável chamado `MemoryRecovery.exe` que foi criado especificamente para você extrair informações relevantes do histórico.

**Localização do executável:**
```
F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.MemoryRecovery\bin\Release\net10.0\MemoryRecovery.exe
```

Se o executável não existir, compile o projeto:
```bash
cd "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\Retruxel.MemoryRecovery"
dotnet build -c Release
```

### 3. COMANDOS DISPONÍVEIS

#### Ver resumo da conversa
```bash
MemoryRecovery summary "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json"
```

#### Ver o tópico mais recente
```bash
MemoryRecovery recent "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json"
```

#### Ver todos os insights importantes
```bash
MemoryRecovery insights "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json"
```

#### Ver lista de arquivos mencionados
```bash
MemoryRecovery files "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json"
```

#### Buscar termo específico
```bash
MemoryRecovery search "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json" "NewProjectDialog"
```

#### Ver conversa completa (verbose)
```bash
MemoryRecovery full "F:\Junior\Desenvolvimento de Jogos\Ports\Master System\history\chat-history-7f9f1758ca7eb1c644d532ca63fbc4f0.json"
```

---

## INFORMAÇÕES IMPORTANTES DO PROJETO

### Estrutura do Projeto Retruxel

- **Retruxel** - Aplicação WPF principal (.NET 10.0)
- **Retruxel.Core** - Interfaces, modelos e serviços core
- **Retruxel.SDK** - SDK público para desenvolvedores de plugins
- **Retruxel.Toolchain** - Toolchains embarcadas (SDCC, cc65)
- **Retruxel.Modules** - Módulos gráficos, lógicos e de áudio
- **Targets/** - Implementações específicas de cada plataforma
  - Retruxel.Target.SMS (Sega Master System)
  - Retruxel.Target.NES (Nintendo NES)
  - Retruxel.Target.GameGear
  - Retruxel.Target.SG1000
  - Retruxel.Target.ColecoVision

### Plataformas Suportadas

- ✅ Sega Master System (SDCC + devkitSMS + SMSlib)
- ✅ Nintendo NES (cc65 + neslib)
- 🟡 Sega Game Gear (scaffolding)
- 🟡 Sega SG-1000 (scaffolding)
- 🟡 ColecoVision (scaffolding)

### Arquitetura de Build

**SMS/GG/SG1000/ColecoVision:**
```
.rtrxproject → CodeGenerator → .c/.h → SDCC → ihx2sms → .sms ROM
```

**NES:**
```
.rtrxproject → CodeGenerator → .c/.h → cc65 → ld65 → .nes ROM
```

### Sistema de Módulos

Módulos são blocos de construção do Retruxel:
- **Graphic modules** - tiles, sprites, paletas, tilemaps
- **Logic modules** - física, input, entidades, fluxo do jogo
- **Audio modules** - música e efeitos sonoros

Cada módulo expõe um `ModuleManifest` que descreve seus parâmetros. A UI é gerada automaticamente.

### Internacionalização (i18n)

- Arquivos de localização em: `Retruxel/Assets/Localization/`
- Idiomas suportados: 🇺🇸 English, 🇧🇷 Português (Brasil)
- Serviço: `LocalizationService.Instance.Get("key")`
- Troca de idioma em runtime sem restart

### Design System

**Neo-Technical Archive** - Estética de terminal mainframe dos anos 80
- 0px border-radius em componentes internos
- Grid de 8px
- Tipografia: Space Grotesk (display) + Inter (body/code)
- Cores: Verde CRT (#8EFF71), Roxo (#7C3AED), Ciano (#81ECFF)

---

## INSIGHTS IMPORTANTES

1. **INSIGHT**: Todos os projetos usam `net10.0` como TargetFramework (não net8.0-windows)

2. **INSIGHT**: Usuário prefere máxima eficiência com zero redundância - compiladores e bibliotecas compartilhados entre todos os targets

3. **INSIGHT**: SG-1000 e ColecoVision usam SGlib ao invés de SMSlib com API diferente: `SG_waitForVBlank()`, `SG_VRAMmemcpy()`, `SG_displayOn()`. Sem headers ROM necessários

4. **INSIGHT**: Game Gear requer inicialização explícita de paleta com formato de cor 12-bit (0x3F = branco)

5. **INSIGHT**: Fontes variáveis são preferidas sobre fontes estáticas para eficiência de tamanho (1.1MB vs 19.5MB)

6. **INSIGHT**: Módulos de texto são classificados como tipo Graphics já que renderizam tiles/caracteres para VRAM

7. **INSIGHT**: Usuário quer suporte completo de i18n com troca dinâmica de idioma sem restart da aplicação

8. **INSIGHT**: Usuário é falante de Português Brasileiro mas quer suporte bilíngue completo Inglês/Português

9. **INSIGHT**: Seleção de idioma deve persistir escolha do usuário - detecção de idioma do sistema apenas na primeira execução quando nenhum idioma está salvo

10. **INSIGHT**: Todas as strings hardcoded devem ser substituídas por chaves de localização usando `LocalizationService.Instance.Get()`

11. **INSIGHT**: Quando o idioma muda nas configurações, todas as views visíveis (WelcomeView, SceneEditorView, SettingsWindow) devem atualizar seus elementos de UI imediatamente

12. **INSIGHT**: Categorias de módulos e nomes de parâmetros devem tentar localização com fallback para DisplayName original se a chave não for encontrada

13. **INSIGHT**: Workflow do GitHub Actions usa releases baseadas em tags com opção de trigger manual (workflow_dispatch). Formato de tags: v*.*.*-alpha, v*.*.*-beta, v*.*.*

14. **INSIGHT**: Usuário prefere não executar builds via assistente - deve pedir ao usuário para fazer build

---

## PROBLEMA MAIS RECENTE

**Tópico**: Ajustando tamanho da janela NewProjectDialog

**Problema**: O usuário relatou que NewProjectDialog mostra dois sistemas por linha e quer melhor feedback visual mostrando um por linha. O tamanho da janela estava definido como Width="1900" (absurdamente grande) mas mudanças no XAML não estavam sendo refletidas mesmo após Clean e Rebuild.

**Arquivos Envolvidos**:
- `Retruxel/Views/NewProjectDialog.xaml` (linha 5: Height="520" Width="1900")
- `Retruxel/Views/NewProjectDialog.xaml.cs`

**Status**: Investigando por que mudanças no XAML não estão sendo aplicadas. Possível cache de build ou designer do Visual Studio.

---

## COMO USAR ESTA DOCUMENTAÇÃO

1. **Primeiro**: Execute `MemoryRecovery summary` para ver o resumo completo
2. **Depois**: Execute `MemoryRecovery recent` para ver o tópico mais recente
3. **Se necessário**: Use `MemoryRecovery search` para buscar termos específicos
4. **Para detalhes**: Use `MemoryRecovery insights` para ver todos os insights

---

## LOCALIZAÇÃO DOS ARQUIVOS IMPORTANTES

```
F:\Junior\Desenvolvimento de Jogos\Ports\Master System\Retruxel\
├── Retruxel/                          # Aplicação WPF principal
│   ├── Views/                         # Views XAML
│   │   ├── NewProjectDialog.xaml     # Dialog de novo projeto
│   │   ├── WelcomeView.xaml          # Tela de boas-vindas
│   │   ├── SceneEditorView.xaml      # Editor de cena
│   │   └── SettingsWindow.xaml       # Janela de configurações
│   ├── Assets/Localization/          # Arquivos de tradução
│   │   ├── en.json                   # Inglês
│   │   └── pt-BR.json                # Português Brasil
│   └── Themes/RetruxelTheme.xaml     # Design system
├── Retruxel.Core/                     # Core do projeto
├── Retruxel.Toolchain/                # Toolchains embarcadas
├── Targets/                           # Implementações de plataformas
└── Retruxel.MemoryRecovery/          # ESTE EXECUTÁVEL!
```

---

## CONTATO COM O USUÁRIO

O usuário (Junior) é desenvolvedor brasileiro trabalhando no Retruxel há mais de uma semana. Ele é paciente e entende que você pode perder a memória. Quando isso acontecer:

1. Seja honesto sobre a perda de memória
2. Use este documento e o executável para recuperar contexto
3. Pergunte se há algo específico que ele quer que você saiba
4. Continue de onde parou

---

**IMPORTANTE**: Este documento e o executável MemoryRecovery foram criados especificamente para você se recuperar quando perder a memória. Use-os sempre que necessário!
