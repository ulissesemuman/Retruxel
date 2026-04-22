# Custom Toolchain Builders

This directory is for **custom toolchain builders** that extend Retruxel's build system.

## How It Works

Retruxel discovers toolchain builders in this order:

1. **Custom builder from target** — If `ITarget.GetCustomToolchainBuilder()` returns a builder, use it
2. **Auto-discovered builders** — Scan `Plugins/Toolchains/*.dll` for `IToolchainBuilder` implementations
3. **Embedded builders** — Fallback to built-in builders (SMS, NES, GG, SG-1000, ColecoVision)

## Creating a Custom Builder

### Option 1: Override in Target (Recommended for target-specific logic)

```csharp
public class MyTarget : ITarget
{
    public string TargetId => "myconsole";
    
    public object? GetCustomToolchainBuilder()
    {
        return new MyCustomBuilder();
    }
    
    public IToolchain GetToolchain()
    {
        var customBuilder = GetCustomToolchainBuilder();
        var builder = Retruxel.Toolchain.ToolchainOrchestrator.GetBuilder(TargetId, customBuilder);
        return new Retruxel.Toolchain.ToolchainAdapter(builder);
    }
}

public class MyCustomBuilder : IToolchainBuilder
{
    public string TargetId => "myconsole";
    public string DisplayName => "My Custom Toolchain";
    public string Version => "1.0.0";
    
    public Task ExtractAsync(IProgress<string> progress) { /* ... */ }
    public Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress) { /* ... */ }
    public Task<bool> VerifyAsync() { /* ... */ }
}
```

### Option 2: Standalone Plugin (Recommended for reusable builders)

1. Create a .NET class library in `Plugins/Toolchains/Retruxel.Toolchain.MyConsole/`
2. Reference `Retruxel.Toolchain.csproj`
3. Implement `IToolchainBuilder`:

```csharp
namespace Retruxel.Toolchain.MyConsole;

public class MyConsoleToolchainBuilder : IToolchainBuilder
{
    public string TargetId => "myconsole";
    public string DisplayName => "My Console Toolchain";
    public string Version => "1.0.0";
    
    public async Task ExtractAsync(IProgress<string> progress)
    {
        // Extract embedded binaries to %AppData%\Retruxel\toolchain\
    }
    
    public async Task<BuildResult> BuildAsync(BuildContext context, IProgress<string> progress)
    {
        // Compile source files to ROM
        // Return BuildResult with success status and ROM path
    }
    
    public async Task<bool> VerifyAsync()
    {
        // Check if toolchain binaries exist
        return File.Exists(Path.Combine(toolchainPath, "compiler.exe"));
    }
}
```

4. Build the project — DLL will be copied to `Plugins/Toolchains/`
5. Retruxel will discover it automatically on startup

## When to Use Each Option

| Scenario | Recommended Approach |
|---|---|
| Target needs specialized build logic | Option 1 (Custom builder in target) |
| Multiple targets share the same toolchain | Option 2 (Standalone plugin) |
| Experimenting with new compiler | Option 1 (Quick prototype) |
| Production-ready toolchain | Option 2 (Proper plugin) |

## Example: Sharing Builders

SMS, Game Gear, SG-1000, and ColecoVision all use SDCC + devkitSMS. They don't need custom builders — they use the embedded `SmsToolchainBuilder`, `GameGearToolchainBuilder`, etc.

If you create a new Z80-based target that also uses SDCC, you can:
- Use the embedded builder (if it matches your needs)
- Create a custom builder with target-specific flags
- Create a shared builder plugin that multiple targets reference

## Notes

- Builders are cached after first discovery — restart Retruxel to reload
- Custom builders always take priority over auto-discovered builders
- If no builder is found, Retruxel throws `NotSupportedException` with a helpful message
