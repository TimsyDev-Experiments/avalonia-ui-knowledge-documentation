---
tier: basics
topic: project setup
estimated: 5 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 001 — Project Setup: Avalonia 12 with .NET

**What you'll learn:** Create a new Avalonia 12 project, add CommunityToolkit.Mvvm, enable compiled bindings, and configure DevTools.

**Prerequisites:** .NET 10 SDK installed, Avalonia VS extension (optional).

---

## 1. Create the project

```bash
dotnet new avalonia.app -n MyApp
cd MyApp
```

This creates a standard Avalonia desktop app with `App.axaml`, `MainWindow.axaml`, and `Program.cs`.

---

## 2. Add CommunityToolkit.Mvvm

```bash
dotnet add package CommunityToolkit.Mvvm --version 8.4.2
```

---

## 3. Enable compiled bindings project-wide

Edit `MyApp.csproj`. Add these properties:

```xml
<PropertyGroup>
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

> This is the default in Avalonia 12 templates, but explicitly setting it ensures all bindings are compile-time validated. Any `{Binding}` automatically becomes `{CompiledBinding}`.

---

## 4. Add DevTools support

```bash
dotnet add package AvaloniaUI.DiagnosticsSupport --version 2.2.1
```

In `Program.cs`, replace the diagnostic attachment:

```csharp
// Program.cs
using Avalonia;
using AvaloniaUI.DiagnosticsSupport;

namespace MyApp;

public static class Program
{
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseSkia()          // explicitly use Skia (Direct2D1 was removed in v12)
            .UseHarfBuzz();     // text shaper required when using UseSkia() explicitly
}
```

In `App.axaml.cs`:

```csharp
// App.axaml.cs
using Avalonia;

namespace MyApp;

public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

---

## 5. Verify the project structure

```
MyApp/
├── MyApp.csproj
├── App.axaml / App.axaml.cs
├── MainWindow.axaml / MainWindow.axaml.cs
├── ViewModels/          (create this)
│   └── MainViewModel.cs
├── Views/               (create this)
│   └── MainWindow.axaml
└── Program.cs
```

Move `MainWindow.axaml` into `Views/` and update the namespace accordingly.

---

## 6. Run the app

```bash
dotnet run
```

Press **F12** to open DevTools (requires Avalonia Plus subscription or Community license).

---

## Key Takeaways

- Avalonia 12 defaults to compiled bindings and requires .NET 8+
- `AvaloniaUI.DiagnosticsSupport` replaces the removed `Avalonia.Diagnostics`
- Explicit `UseSkia()` + `UseHarfBuzz()` avoids the "No text shaping system configured" error
- CommunityToolkit.Mvvm 8.4.2 is the current stable version

---

## See Also

- [002 — Command Binding](002-command-binding.md)
- [Avalonia Docs: Create your first project](https://docs.avaloniaui.net/docs/get-started/create-your-first-project)
- [Avalonia 12 Breaking Changes](/docs/04-migration/avalonia-11-to-12.md)
