---
tier: basics
topic: project setup
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 001-project-setup.md
---

# 001V — Project Setup: An In-Depth Companion

**What you'll learn in this companion:** Why each piece of the project template exists, what the default template generates, how compiled bindings work under the hood, the rationale behind `UseSkia()` and `UseHarfBuzz()`, and how to structure your project for maintainability.

**Prerequisites:** .NET 10 SDK installed.

**You should already have read:** [001 — Project Setup](001-project-setup.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Why the `dotnet new avalonia.app` Template Produces What It Does

```bash
dotnet new avalonia.app -n MyApp
```

The Avalonia project template is defined in the `Avalonia.Templates` NuGet package (installed automatically by the .NET SDK when you use an Avalonia workload). When you run this command, the template engine:

1. Creates a `.csproj` that targets a modern .NET framework (e.g., `net10.0`) with `Avalonia`, `Avalonia.Desktop`, and `Avalonia.Themes.Fluent` package references.
2. Generates `App.axaml` — the application-level XAML root. This is where global styles, global resources, and the theme (e.g., `<FluentTheme />`) are declared.
3. Generates `App.axaml.cs` — the code-behind where the `Application` subclass configures the application lifetime. The template includes the `OnFrameworkInitializationCompleted` override, which is the correct place to set the `MainWindow` for a desktop app.
4. Generates `MainWindow.axaml` and `MainWindow.axaml.cs` — the primary window. In Avalonia 12 templates, `MainWindow.axaml` already includes `x:DataType` with a stub namespace, nudging you toward compiled bindings from the start.
5. Generates `Program.cs` — the entry point. This is *not* inside a namespace by default in some template versions, but the recommended pattern wraps it.

The template does **not** create `ViewModels/` or `Views/` folders. You create those yourself. That is deliberate: the template gives you a runnable baseline with zero opinions about architecture. You opt into folder structure, MVVM frameworks, and dependency injection.

### Why `Program.cs` Uses a Separate `BuildAvaloniaApp()` Method

```csharp
public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

Separating `BuildAvaloniaApp()` from `Main()` serves two purposes:

- **Testability:** Unit tests can call `BuildAvaloniaApp()` and configure a headless platform (`UseHeadless()`), then exercise the application startup without launching a real window. If everything were inside `Main()`, the test would need to reflect on `Program` to extract the builder.
- **Override points:** Derived launchers (e.g., in Xamarin.Forms-style or single-view embedded hosts) might need to tweak the builder before calling `StartWithClassicDesktopLifetime`.

The `AppBuilder` follows the builder pattern: each call returns the same builder instance, mutated. `.UsePlatformDetect()` probes the current OS and loads the appropriate platform backend (Skia on Windows/Linux/macOS, or Angle/Metal/Vulkan depending on configuration).

---

## 2. Why CommunityToolkit.Mvvm (and What It Gives You)

```bash
dotnet add package CommunityToolkit.Mvvm --version 8.4.2
```

CommunityToolkit.Mvvm is maintained by Microsoft and is the de facto standard MVVM toolkit for .NET. It provides:

- **`ObservableObject`** — a base class that implements `INotifyPropertyChanged`. It includes `SetProperty<T>()` which checks for equality before raising the event, avoiding unnecessary UI updates.
- **`[ObservableProperty]`** — a source generator attribute. Apply it to a private field, and the generator produces a public property with change notification. Without this, you would hand-write every property using the `SetProperty` pattern shown in the original tutorial.
- **`[RelayCommand]`** — a source generator that converts a method into an `IRelayCommand` property. It handles `CanExecute`, async wrapping, cancellation-token injection, and progress reporting.
- **`ObservableValidator`** — extends `ObservableObject` with `INotifyDataErrorInfo` support for validation.
- **`WeakReferenceMessenger`** / `StrongReferenceMessenger` — a pub/sub message system for decoupled ViewModel-to-ViewModel communication.

Version 8.4.2 is pinned because source-generated code from different minor versions can produce different internal shapes. Pinning ensures all team members get identical generated code. When you upgrade, expect to regenerate.

### Why Source Generators Instead of Runtime Reflection

The `[ObservableProperty]` and `[RelayCommand]` attributes are processed at compile time by Roslyn source generators. They produce real C# code that you can see in `MyApp\obj\Debug\net10.0\generated\CommunityToolkit.Mvvm.SourceGenerators\`. Advantages over runtime reflection (e.g., `PropertyChanged.Fody`, old `INotifyPropertyChanged` weaving):

- **AOT-friendly:** No IL weaving or runtime code generation. Source generators emit code that gets compiled normally, so trimming and NativeAOT work without ceremony.
- **Debuggable:** You can step into the generated property setter. With Fody weavers, the injected IL is invisible.
- **Blazing-fast startup:** No reflection-based property discovery at runtime.

The tradeoff: the class must be `partial`. If you forget `partial`, the source generator emits a diagnostic error like `MISSING_PARTIAL_MODIFIER`.

---

## 3. Compiled Bindings: Why They Are the Default in Avalonia 12

```xml
<PropertyGroup>
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

In Avalonia 11, bindings were resolved at runtime by default. `{Binding Name}` would walk the logical tree at runtime, discover the `DataContext`, and use reflection to find `Name`. If `Name` didn't exist, you got a silent no-op or a trace message — no compile-time error. This meant renaming a ViewModel property could silently break the UI.

Avalonia 12 introduces **compiled bindings** as the default. Setting `AvaloniaUseCompiledBindingsByDefault=true` makes every `{Binding}` expression behave as if you wrote `{CompiledBinding}`. The XAML compiler generates code at build time that:

1. Reads the `x:DataType` from the nearest ancestor element that declares one.
2. Resolves the binding path against that type at compile time.
3. Emits a strongly-typed accessor (e.g., `viewModel.Name`) instead of a reflection-based lookup.

If `GreetCommand` doesn't exist on `MainViewModel`, the build **fails** with an error like `Avalonia: The property 'GreetCommand' was not found on type 'MainViewModel'`. This turns UI binding errors from silent runtime bugs into compile-time blockers — exactly what you want in a maintainable codebase.

### What You Must Do for Compiled Bindings

- Every `Control` (or `Window`, `UserControl`) in a view must set `x:DataType` to the ViewModel type.
- Every `{Binding}` path must exist on that type.
- If you use `DataTemplate`, the template root (e.g., `StackPanel`, `Grid`) must also set `x:DataType`.
- `{Binding .}` translates to `{CompiledBinding}` — binding to the DataContext itself.

### When to Use `{ReflectionBinding}` Instead

Occasionally, you need runtime-reflection binding — binding to a dynamic property, a dictionary key, or a type you don't know at compile time. Use `{ReflectionBinding Path}` to opt out of compiled binding for that one expression. In Avalonia 12, you can also set `AvaloniaUseCompiledBindingsByDefault=false` in the `.csproj` to revert to the v11 behavior globally, but this is not recommended for new projects.

---

## 4. DevTools in Avalonia 12: Why the API Changed

```bash
dotnet add package AvaloniaUI.DiagnosticsSupport --version 2.2.1
```

In Avalonia 11, you added DevTools by referencing `Avalonia.Diagnostics` and calling `.UseDevTools()` on the `AppBuilder`. This package was tightly coupled to the internal compositor, which made it impossible to use in AOT scenarios and hard to maintain across platform backends.

Avalonia 12 removed `Avalonia.Diagnostics` entirely. The replacement, `AvaloniaUI.DiagnosticsSupport`, is a separate NuGet package published outside the core Avalonia repository. It decouples the DevTools UI from the framework internals.

The key changes in `Program.cs`:

- **No `.UseDevTools()`**: The new package does not require a builder call. Just installing the package and referencing the namespace is enough.
- **No `using Avalonia.Diagnostics;`**: Replaced by `using AvaloniaUI.DiagnosticsSupport;`.
- **Explicit `UseSkia()` + `UseHarfBuzz()`**: These are not strictly part of DiagnosticsSupport, but the removal of Direct2D1 in v12 and the new text-shaping pipeline make them necessary. Explanation below.

### F12 DevTools Access

In Avalonia 12, pressing **F12** opens the DevTools overlay. This requires either an **Avalonia Plus** subscription or a **Community license** (free for individuals, open-source projects, and small businesses). Without a license, DevTools still works in debug builds but shows a reminder banner. In release builds without a license, F12 is disabled.

---

## 5. Why `UseSkia()` and `UseHarfBuzz()` Are Explicit Now

```csharp
.UseSkia()
.UseHarfBuzz();
```

In Avalonia 11, the default platform backend was Skia (on Windows, via Direct2D1; on Linux/macOS, via native Skia). In Avalonia 12:

- **Direct2D1 was removed.** The only rendering backend is Skia. Calling `.UseSkia()` is technically optional if you haven't called any other rendering-substituting method, but making it explicit documents your intent and avoids surprises if a future version adds a second backend.
- **HarfBuzz is the text shaper.** Avalonia no longer uses DirectWrite (Windows) or CoreText (macOS) for text shaping. HarfBuzz handles complex script shaping (Arabic, Devanagari, emoji sequences) cross-platform. If you call `.UseSkia()` without also calling `.UseHarfBuzz()`, you get a runtime error: `No text shaping system configured`. The two calls are a pair — they must appear together.

What these calls actually do:

1. `.UseSkia()` registers the Skia platform subsystem with the `AppBuilder`. This includes the Skia-based renderer, bitmap operations, and GPU context initialization if available.
2. `.UseHarfBuzz()` registers the HarfBuzz text shaper with the `FontManager`. It intercepts text-measurement and glyph-shaping requests and routes them through `HarfBuzzSharp`.

If you use the default template without explicitly calling `.UseSkia().UseHarfBuzz()`, the template's default `.UsePlatformDetect()` already configures them internally. You only need explicit calls if you removed `.UsePlatformDetect()` or if you want to guarantee the pair is configured.

---

## 6. The Application Lifetime: What `IClassicDesktopStyleApplicationLifetime` Does

```csharp
if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
{
    desktop.MainWindow = new MainWindow();
}
```

Avalonia supports multiple application lifetimes:

| Lifetime | Platform | Behavior |
|---|---|---|
| `IClassicDesktopStyleApplicationLifetime` | Windows, Linux, macOS | Standard single-window desktop app. `MainWindow` is the primary window; closing it exits the app. You can also use `ShutdownMode` (`OnMainWindowClose`, `OnLastWindowClose`, `OnExplicitShutdown`). |
| `ISingleViewApplicationLifetime` | Mobile, Browser, embedded | No `MainWindow`. You set `MainView` to the root `Control`. Used by Avalonia.Web (Browser) and Avalonia.Android/iOS. |
| `IControlledApplicationLifetime` | Any | Gives you `Startup` and `Exit` events without a predetermined shutdown policy. Useful for DI-hosted apps that need startup sequencing. |

The `is` pattern cast is the idiomatic way to check which lifetime is active. If you're writing a desktop-only app, you can safely cast without checking, but the conditional is defensive — it makes the same `App.axaml.cs` work in a single-view host.

Setting `desktop.MainWindow = new MainWindow();` does **not** show the window immediately. The window is shown after `base.OnFrameworkInitializationCompleted()` returns. This means you can set properties on `MainWindow` (like `DataContext`, `Width`, `Height`) before it renders, avoiding layout thrash.

---

## 7. Recommended Project Structure: Why Views/ and ViewModels/ Exist

```
MyApp/
├── App.axaml / App.axaml.cs
├── Views/
│   └── MainWindow.axaml (moved from root)
├── ViewModels/
│   └── MainViewModel.cs
├── Models/              (optional, for data classes)
├── Converters/          (optional, for IValueConverter implementations)
├── Services/            (optional, for DI-registered services)
├── Program.cs
└── MyApp.csproj
```

Separating `Views/` from `ViewModels/` enforces the MVVM pattern at the file-system level. Without this separation, it is tempting to put business logic in code-behind files. The folder boundary creates a psychological barrier: "this is a View, it only binds; this is a ViewModel, it holds state and commands."

When you move `MainWindow.axaml` into `Views/`, you must update:

1. The `x:Class` attribute in `MainWindow.axaml` to `MyApp.Views.MainWindow`.
2. The namespace in `MainWindow.axaml.cs` to `MyApp.Views`.
3. Any references to `MainWindow` in `App.axaml.cs` to `MyApp.Views.MainWindow`.

The same applies to the `x:Class` - it must match the namespace + class name exactly, or the XAML compiler produces a type-not-found error at build time.

---

## 8. Running and Debugging

```bash
dotnet run
```

This compiles and executes the app. If you've set `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`, any missing binding paths produce build errors before the app launches — no runtime surprises.

F12 DevTools gives you:
- **Visual tree inspector** — hover elements to see their bounds, margin, padding, and applied styles.
- **Property editor** — change any property at runtime and see the effect immediately.
- **Style debugging** — see which selectors matched a given element and which setters won.
- **Layout explorer** — measure-pass and arrange-pass details.

---

## Common Mistakes

1. **Forgot `partial` on the ViewModel class.** The source generators for `[ObservableProperty]` and `[RelayCommand]` require `partial class`. The error message mentions a missing partial modifier — add `partial`.
2. **Set `AvaloniaUseCompiledBindingsByDefault` but omitted `x:DataType`.** Without `x:DataType`, compiled bindings cannot resolve the binding source type, and you get a compile-time error like `Cannot find a DataType for the binding [...] Provide one via x:DataType`.
3. **Installed `Avalonia.Diagnostics` instead of `AvaloniaUI.DiagnosticsSupport`.** The old package is incompatible with Avalonia 12. Remove it and use the new package.
4. **Called `.UseSkia()` without `.UseHarfBuzz()`.** The runtime tells you: "No text shaping system configured." Add `.UseHarfBuzz()` after `.UseSkia()`.
5. **Moved `MainWindow.axaml` but forgot to update `x:Class`.** The build fails with `Type 'MyApp.MainWindow' not found` because the code-behind partial class is in namespace `MyApp.Views` but `x:Class` still says `MyApp.MainWindow`.

---

## See Also

- [001 — Project Setup (original tutorial)](001-project-setup.md)
- [001X — Project Setup (examples)](001-project-setup-examples.md)
- [002 — Command Binding](002-command-binding.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [Avalonia Docs: Create your first project](https://docs.avaloniaui.net/docs/get-started/create-your-first-project)
- [Avalonia 12 Breaking Changes](../../04-migration/avalonia-11-to-12.md)
- [CommunityToolkit.Mvvm Source Generators](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm)
