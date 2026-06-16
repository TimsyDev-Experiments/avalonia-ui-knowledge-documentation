---
title: Quiz
topic: 001-project-setup
type: quiz
---

# Quiz: Project Setup

> Test your understanding of setting up an Avalonia 12 project with the MVVM toolkit. Select an answer for each question, then click **Complete Quiz** to see your results.

```quiz
Q: What does `BuildAvaloniaApp()` return?
A. AppBuilder (correct) || `AppBuilder` is the return type of `BuildAvaloniaApp()`. It is a builder-pattern class that lets you chain configuration methods (`.UsePlatformDetect()`, `.UseSkia()`, `.UseHarfBuzz()`) and then pass the configured builder to a lifetime method.
B. Application || `Application` is the base class for `App.axaml.cs`, the XAML application class that you register with `.UseApplication()`. It is not the return type of `BuildAvaloniaApp()`.
C. Window || `Window` is a top-level UI element used to display content on screen, but it has no role in application startup configuration.
D. AvaloniaAppBase || This type does not exist in Avalonia's public API. It may be confused with `Application` or `AppBuilder`.

Explanation: `BuildAvaloniaApp()` returns an `AppBuilder` instance, following the builder pattern. The builder configures platform services (windowing, rendering), fonts, and the XAML application class before starting the app via `.StartWithClassicDesktopLifetime()`.
```

```quiz
Q: Which MSBuild property enables compiled bindings project-wide in an Avalonia 12 `.csproj` file?
A. `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` (correct) || This is the exact MSBuild property that enables compiled bindings at the project level, making `x:DataType` optional on individual bindings.
B. `<EnableCompiledBindings>true</EnableCompiledBindings>` || This property does not exist in Avalonia. It resembles properties from other XAML frameworks but is not recognized by the Avalonia build system.
C. `<UseCompiledBindings>true</UseCompiledBindings>` || Similar in name but not the correct property. Avalonia uses the more specific `AvaloniaUseCompiledBindingsByDefault`.
D. `<AvaloniaCompiledBindings>true</AvaloniaCompiledBindings>` || Close, but the correct name includes `Use` and `ByDefault`. Without the full name, the Avalonia XAML compiler will not enable compiled bindings.

Explanation: Setting `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` in the `.csproj` enables compiled bindings project-wide. This means bindings are checked at compile time (type safety, correct paths) and perform better at runtime, without requiring `x:DataType` on every binding expression.
```

```quiz
Q: Which NuGet package replaces the deprecated `Avalonia.Diagnostics` for debugging support in Avalonia 12?
A. `AvaloniaUI.DiagnosticsSupport` (correct) || This is the official replacement package. It is installed via `dotnet add package AvaloniaUI.DiagnosticsSupport` and configured in `Program.cs` with `.UseSkia().UseHarfBuzz()`.
B. `Avalonia.DevTools` || This package does not exist. The correct replacement is under the `AvaloniaUI.*` namespace, not `Avalonia.*`.
C. `Avalonia.Diagnostics` (same package, newer version) || `Avalonia.Diagnostics` has been deprecated and removed in Avalonia 12. Simply upgrading the version will not work — you must switch to the new package.
D. `AvaloniaUI.DeveloperTools` || The correct package name is `DiagnosticsSupport`, not `DeveloperTools`. This confusion is common because DevTools is the feature name.

Explanation: `Avalonia.Diagnostics` is deprecated. The replacement `AvaloniaUI.DiagnosticsSupport` provides the same DevTools (F12 visual tree inspector, property editor, layout explorer) with API updates for Avalonia 12. Install it and call `.UseSkia().UseHarfBuzz()` in the app builder.
```

```quiz
Q: What does `.UsePlatformDetect()` do in the Avalonia app builder?
A. Detects the current platform and configures the appropriate rendering backend and windowing system (correct) || `.UsePlatformDetect()` auto-detects Windows, macOS, or Linux and selects DirectX (via ANGLE), Metal, or Vulkan respectively, plus the correct windowing backend.
B. Detects the installed .NET runtime version || .NET runtime version detection is handled by the .NET SDK, not by Avalonia's `UsePlatformDetect()`.
C. Configures the MVVM toolkit for the current platform || The MVVM toolkit (CommunityToolkit.Mvvm) is platform-agnostic. No platform detection is needed for MVVM configuration.
D. Installs platform-specific NuGet packages at runtime || NuGet packages are resolved at build time, not runtime. `UsePlatformDetect()` selects among backends already included in the build.

Explanation: `.UsePlatformDetect()` examines the current OS and selects the correct rendering backend — DirectX via ANGLE on Windows, Metal on macOS, Vulkan on Linux, or Skia software fallback. It also configures the windowing subsystem, eliminating manual `#if` platform checks.
```

```quiz
Q: What is the recommended folder structure for an Avalonia MVVM project?
A. `ViewModels/` and `Views/` directories at the project root (correct) || The standard convention uses two folders at the project root: `ViewModels/` for all ViewModel classes and `Views/` for all Window/Control XAML files. Models are often omitted because ViewModels hold their own state.
B. All files in the project root || While this works for tiny prototypes, it does not scale. A flat structure makes it hard to separate concerns as the project grows beyond a few files.
C. `Models/`, `ViewModels/`, `Views/` directories || A `Models/` directory is common in WPF and other frameworks, but Avalonia + CommunityToolkit.Mvvm often skips it because `[ObservableProperty]` generates state management directly in ViewModels.
D. `src/ViewModels/` and `src/Views/` directories || Nesting under `src/` is a common convention in ASP.NET projects but is not the recommended layout for Avalonia desktop apps. The `dotnet new avalonia.app` template uses the root-level layout.

Explanation: The recommended structure places `ViewModels/` and `Views/` at the project root. Models are often unnecessary because CommunityToolkit.Mvvm source generators handle state binding directly in ViewModels via `[ObservableProperty]`. This keeps the project simple without over-engineering.
```
