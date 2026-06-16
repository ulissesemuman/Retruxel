# Estrutura de Arquivos do Projeto Retruxel

```text
.
├── Retruxel.slnx
├── Plugins
│   ├── CodeGens
│   ├── Targets
│   └── Tools
├── Retruxel
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── Retruxel.csproj
│   └── ... (outras arquivos da aplicação)
├── Retruxel.Core
│   ├── Retruxel.Core.csproj
│   └── ... (bibliotecas core)
├── Retruxel.Emulation
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── EmulatorWindow.xaml
│   ├── EmulatorWindow.xaml.cs
│   └── Retruxel.Emulation.csproj
├── Retruxel.Modules
│   ├── Retruxel.Modules.csproj
│   └── ... (módulos de gráficos, lógica, áudio)
├── Retruxel.SDK
│   ├── Retruxel.SDK.csproj
│   └── RetruxelSdk.cs
├── Retruxel.Splash
│   ├── Retruxel.Splash.csproj
│   └── SplashProjectBuilder.cs
└── Retruxel.Toolchain
    ├── Retruxel.Toolchain.csproj
    └── ... (toolchain builders e compiladores)