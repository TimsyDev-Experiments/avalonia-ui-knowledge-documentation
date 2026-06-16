---
title: Quiz
topic: 001-project-setup
type: quiz
---

# Quiz: Project Setup

> Test your understanding of setting up an Avalonia 12 project with the MVVM toolkit. Select an answer for each question, then click **Complete Quiz** to see your results.

```quiz
Q: What does `BuildAvaloniaApp()` return?
A. AppBuilder (correct)
B. Application
C. Window
D. AvaloniaAppBase

Explanation: `BuildAvaloniaApp()` returns an `AppBuilder` instance. This builder is then configured with `.UsePlatformDetect()`, `.UseSkia()`, and `.UseHarfBuzz()` before being passed to a lifetime method like `.StartWithClassicDesktopLifetime()`.
```

```quiz
Q: Which MSBuild property enables compiled bindings project-wide in an Avalonia 12 `.csproj` file?
A. `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` (correct)
B. `<EnableCompiledBindings>true</EnableCompiledBindings>`
C. `<UseCompiledBindings>true</UseCompiledBindings>`
D. `<AvaloniaCompiledBindings>true</AvaloniaCompiledBindings>`

Explanation: Setting `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` in the `.csproj` enables compiled bindings without requiring `x:DataType` on every binding. This provides compile-time type checking, better performance, and fewer runtime errors.
```

```quiz
Q: Which NuGet package replaces the deprecated `Avalonia.Diagnostics` for debugging support in Avalonia 12?
A. `AvaloniaUI.DiagnosticsSupport` (correct)
B. `Avalonia.DevTools`
C. `Avalonia.Diagnostics` (same package, newer version)
D. `AvaloniaUI.DeveloperTools`

Explanation: `Avalonia.Diagnostics` has been deprecated in favor of `AvaloniaUI.DiagnosticsSupport`. The new package provides the same DevTools functionality (visual tree inspector, property editor, layout explorer) with updated API compatibility for Avalonia 12.
```

```quiz
Q: What does `.UsePlatformDetect()` do in the Avalonia app builder?
A. Detects the current platform and configures the appropriate rendering backend and windowing system (correct)
B. Detects the installed .NET runtime version
C. Configures the MVVM toolkit for the current platform
D. Installs platform-specific NuGet packages at runtime

Explanation: `.UsePlatformDetect()` examines the current operating system and selects the correct rendering backend — DirectX (via ANGLE) on Windows, Metal on macOS, Vulkan on Linux, or a software fallback. It also configures the appropriate windowing subsystem. This eliminates the need for manual `#if` platform checks.
```

```quiz
Q: What is the recommended folder structure for an Avalonia MVVM project?
A. `ViewModels/` and `Views/` directories at the project root (correct)
B. All files in the project root
C. `Models/`, `ViewModels/`, `Views/` directories
D. `src/ViewModels/` and `src/Views/` directories

Explanation: The recommended structure places `ViewModels/` and `Views/` directories at the project root level. Models are often omitted because ViewModels contain their own state via CommunityToolkit.Mvvm's source generators (`[ObservableProperty]`). This keeps the project organized without over-engineering for small to medium applications.
```
