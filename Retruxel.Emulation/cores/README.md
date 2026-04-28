# Libretro Cores

Coloque os cores libretro (.dll) nesta pasta.

## Download

Baixe cores em:
https://buildbot.libretro.com/nightly/windows/x86_64/latest/

## Cores recomendados para Retruxel:

### Genesis Plus GX (SMS/GG/SG-1000/Genesis/Mega Drive)
- Arquivo: `genesis_plus_gx_libretro.dll`
- Suporta: .sms, .gg, .sg, .md, .gen

### Mesen (NES)
- Arquivo: `mesen_libretro.dll`
- Suporta: .nes

### Snes9x (SNES)
- Arquivo: `snes9x_libretro.dll`
- Suporta: .sfc, .smc

### Gambatte (Game Boy / Game Boy Color)
- Arquivo: `gambatte_libretro.dll`
- Suporta: .gb, .gbc

## Como usar:

1. Baixe o core desejado do link acima
2. Coloque o arquivo .dll nesta pasta
3. No Retruxel Emulator, clique em "LOAD CORE"
4. Selecione o arquivo .dll
5. Clique em "LOAD ROM" e selecione sua ROM
6. Clique em "RUN" para começar a emulação

## Estrutura esperada:

```
Retruxel.Emulation/
├── cores/
│   ├── genesis_plus_gx_libretro.dll  ← SMS/GG/SG-1000
│   ├── mesen_libretro.dll             ← NES
│   ├── snes9x_libretro.dll            ← SNES
│   └── gambatte_libretro.dll          ← GB/GBC
```

## Nota:

Os cores são **opcionais** - você escolhe quais baixar baseado nos consoles que quer emular.
