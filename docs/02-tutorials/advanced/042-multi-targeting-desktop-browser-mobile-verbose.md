---
tier: advanced
topic: build
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 042-multi-targeting-desktop-browser-mobile.md
---

# 042V — Multi-Targeting: Desktop, Browser, and Mobile: An In-Depth Companion

**Why this exists.** The original tutorial walks through creating a multi-project solution with platform-specific entry points. This companion explains why the separate-project-per-platform structure is preferred over multi-targeting a single `.csproj`, what each entry-point method does at the platform level, how the shared `App.axaml.cs` dispatches to the correct lifetime, why platform-specific code should prefer `OperatingSystem` APIs over `#if` directives, and what the practical limitations are per platform.

**Read this alongside:** [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md)

---

## 1. Why separate projects, not multi-targeting

The original uses one `.csproj` per platform. You could also use a single `.csproj` with multiple target frameworks:

```xml
<TargetFrameworks>net10.0;net10.0-browser;net10.0-android;net10.0-ios</TargetFrameworks>
```

**Why separate projects is better:**

| Concern | Separate projects | Single multi-target |
|---------|------------------|---------------------|
| Entry point | One `Main`/`Activity` per project | Conditional `#if` for each platform — fragile |
| Dependencies | Each `.csproj` includes only needed packages | All packages included for all platforms — NuGet may restore platform-incompatible packages |
| Build time | Only target platforms are built | Always builds all target frameworks |
| Debugging | One launch profile per project | One launch profile — switching targets requires reconfiguration |
| Tooling | Platform-specific project types (Android `.csproj` with `<OutputType>Exe</OutputType>`) | Same project type for all — some tools (Android manifest, Info.plist) are harder to embed |

Use separate projects for real-world applications. Use single multi-target only for small utility libraries.

---

## 2. Solution structure — what each project contains

```
src/
├── MyApp/                    # Shared library (net10.0 classlib)
│   ├── App.axaml / App.axaml.cs
│   ├── ViewModels/
│   ├── Views/
│   ├── Models/
│   └── MyApp.csproj
│
├── MyApp.Desktop/            # Desktop launcher
│   ├── Program.cs
│   └── MyApp.Desktop.csproj
│
├── MyApp.Browser/            # WASM launcher
│   ├── Program.cs
│   ├── AppBundle/index.html, main.js
│   └── MyApp.Browser.csproj
│
├── MyApp.Android/            # Android launcher
│   ├── MainActivity.cs
│   ├── AndroidManifest.xml
│   └── MyApp.Android.csproj
│
└── MyApp.iOS/                # iOS launcher
    ├── AppDelegate.cs
    ├── Program.cs
    ├── Info.plist
    └── MyApp.iOS.csproj
```

Each launcher project:

- Has a `Program.Main` (or `MainActivity` / `AppDelegate`).
- References `MyApp` (the shared library).
- Configures the `AppBuilder` with platform-specific settings.
- Contains only the code needed to start the platform — no ViewModels, no Views.

The shared `MyApp` project is a plain `net10.0` class library. It does not reference platform-specific packages (`Avalonia.Web`, `Avalonia.Android`, `Avalonia.iOS`). Those are referenced only by the launcher projects.

---

## 3. Shared `App.axaml.cs` — lifetime dispatch

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow
        {
            DataContext = new MainViewModel()
        };
    }
    else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
    {
        singleView.MainView = new MainView
        {
            DataContext = new MainViewModel()
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

`ApplicationLifetime` is set by the entry-point method called in `Program.cs`:

- `StartWithClassicDesktopLifetime()` → `IClassicDesktopStyleApplicationLifetime`
- `StartWithBrowserPlatform()` → `ISingleViewApplicationLifetime`
- `AvaloniaMainActivity<App>` → `ISingleViewApplicationLifetime`
- `AvaloniaAppDelegate<App>` → `ISingleViewApplicationLifetime`

The `if/else if` dispatches to the correct view type. Desktop gets a `Window`; all other platforms get a `Control` (`MainView`).

**Common mistake:** using `switch` or multiple separate `if` statements. The lifetimes are mutually exclusive — `ISingleViewApplicationLifetime` and `IClassicDesktopStyleApplicationLifetime` are never both true.

**For shared ViewModel construction:** both branches create `new MainViewModel()`. If the ViewModel requires platform-specific dependencies, use DI to resolve it:

```csharp
var vm = App.Services.GetRequiredService<MainViewModel>();
```

---

## 4. Desktop entry point — `StartWithClassicDesktopLifetime`

```csharp
[STAThread]
public static void Main(string[] args) =>
    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
```

`[STAThread]` is required on Windows. It sets the apartment state to Single-Threaded Apartment, which is required for COM interop (Windows clipboard, drag-drop, file dialogs). On Linux and macOS, the attribute is ignored.

`StartWithClassicDesktopLifetime`:

1. Creates the `Application` instance.
2. Configures the platform rendering backend (Direct3D 11 on Windows, Metal on macOS, Vulkan/GL on Linux).
3. Starts the `Dispatcher` main loop.
4. Returns when `ApplicationLifetime.Shutdown()` is called or the last window closes.

---

## 5. Browser entry point — `StartWithBrowserPlatform`

```csharp
class Program
{
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithBrowserPlatform(args);
}
```

`StartWithBrowserPlatform`:

1. Configures the `Avalonia.Web` backend.
2. Renders the `MainView` inside the `<div id="app">` element defined in `index.html`.
3. Starts the Blazor UI synchronization loop (if using `Avalonia.Web.Blazor`).
4. Does **not** call `Environment.Exit` — the browser tab continues running until the user closes it.

**Required packages:**

```xml
<PackageReference Include="Avalonia.Web" Version="12.0.4" />
```

`Avalonia.Web` includes the WASM rendering backend. `Avalonia.Web.Blazor` (optional) adds Blazor interop for calling .NET from JavaScript and vice versa.

**Common mistake:** forgetting to set the `<div id="app">` in `index.html`. The browser backend looks for this specific ID and fails silently (blank page) if it is missing.

**main.js explanation:**

```js
const { getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();
```

This is the standard .NET WASM bootstrap. `dotnet.create()` initializes the Mono WASM runtime and loads the .NET assemblies. `dotnet.run()` starts the application's `Main` entry point. The browser platform hooks into the Mono WASM rendering loop.

---

## 6. Android entry point — `AvaloniaMainActivity<App>`

```csharp
[Activity(Label = "MyApp", Theme = "@style/MyTheme.NoActionBar",
          Icon = "@drawable/icon", MainLauncher = true,
          ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        builder.UsePlatformDetect()
               .WithInterFont()
               .LogToTrace();
}
```

`AvaloniaMainActivity<App>` is an Android `Activity` subclass. It:

1. Creates the `Application` instance.
2. Attaches the Avalonia `MainView` as the activity content view.
3. Handles Android lifecycle events (onPause, onResume, onDestroy).
4. Routes Android configuration changes (orientation, screen size) to the Avalonia layout system.

`CustomizeAppBuilder` is the hook for configuring the `AppBuilder` before the application starts. Always call `UsePlatformDetect()` — on Android, this selects the ANGLE (OpenGL ES) rendering backend.

**Required package:**

```xml
<PackageReference Include="Avalonia.Android" Version="12.0.4" />
```

---

## 7. iOS entry point — `AvaloniaAppDelegate<App>`

```csharp
[Register("AppDelegate")]
public class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        builder.UsePlatformDetect()
               .WithInterFont()
               .LogToTrace();
}
```

```csharp
class Program
{
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
```

`AvaloniaAppDelegate<App>` is an iOS `UIApplicationDelegate` subclass. It:

1. Responds to `FinishedLaunching` to start the Avalonia application.
2. Attaches the `MainView` to the iOS view hierarchy.
3. Handles iOS lifecycle events.

The `UIApplication.Main(args, null, typeof(AppDelegate))` call is the standard iOS entry point — it creates the `UIApplication` and passes control to the `AppDelegate`.

**Required package:**

```xml
<PackageReference Include="Avalonia.iOS" Version="12.0.4" />
```

---

## 8. Platform-specific code — `OperatingSystem` APIs

```csharp
if (OperatingSystem.IsBrowser())
{
    // No file system access; use storage provider
}
else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
{
    // Touch-friendly interactions
}
else
{
    // Desktop: keyboard/mouse primary
}
```

`OperatingSystem` is a .NET 10+ API that uses runtime feature flags to determine the platform. It does not require conditional compilation. The JIT (or AOT compiler) evaluates the conditions and may eliminate unreachable branches.

**Advantages over `#if`:**

- Readable — code is not littered with preprocessor directives.
- Testable — branch coverage tools see all paths.
- AOT-friendly — the NativeAOT compiler evaluates the condition at compile time and removes dead branches (because `OperatingSystem.IsBrowser()` returns a constant false for desktop and vice versa).

**Use `#if` only when:**

- Referencing types that exist only on one platform (e.g., `Android.OS.Bundle`).
- Using APIs that require an Android-specific `using` directive that would not compile on other platforms.

---

## 9. Platform-specific XAML

```xml
<Style Selector="Button" Platform="android">
  <Setter Property="FontSize" Value="18" />
</Style>
```

**Note:** the `Platform` selector syntax in Avalonia is `:platform(android)`, not `Platform="android"`. The correct form is:

```xml
<Style Selector="Button:platform(android)">
  <Setter Property="FontSize" Value="18" />
</Style>
```

The `:platform()` pseudo-class selects elements based on the `Platform` property of the `TopLevel`. Available platforms: `android`, `ios`, `browser`, `windows`, `linux`, `macos`.

This selector only works at runtime — the XAML compiler does not validate it at compile time.

---

## 10. Platform limitations table — what "Limited" means

| Feature | Desktop | Browser | Android | iOS |
|---------|---------|---------|---------|-----|
| Full windowing | Yes | Single view | Single view | Single view |
| File system access | Full | Sandboxed | Sandboxed | Sandboxed |
| TrayIcon | Yes | No | No | No |
| Drag-drop between apps | Yes | Limited | Limited | Limited |
| Threading | Full | WASM limited | Full | Full |
| GPU rendering | Direct3D/Vulkan | WebGL | OpenGL ES | Metal |

- **Sandboxed file system:** browser and mobile apps cannot enumerate directories or access arbitrary paths. Use `IStorageProvider` for all file operations (see [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md)).
- **Limited drag-drop:** browser supports drag-drop only within the page. Android supports drag-drop only within the app. iOS requires the drag-drop delegate APIs.
- **WASM limited threading:** .NET WASM runs on a single thread. `Task.Run` runs on the same thread. `Dispatcher.UIThread` is always the current thread. `lock` statements work but do not provide true parallelism.

---

## 11. Build and run — per-platform commands

```bash
# Desktop
dotnet run --project src/MyApp.Desktop

# Browser
dotnet run --project src/MyApp.Browser
# Navigate to http://localhost:5000

# Android
dotnet build -t:Run -f net10.0-android --project src/MyApp.Android

# iOS (requires macOS + Xcode)
dotnet build -t:Run -f net10.0-ios --project src/MyApp.iOS
```

- **Desktop:** `dotnet run` launches the native executable.
- **Browser:** `dotnet run` starts a Kestrel web server that serves the WASM payload and the `index.html` page.
- **Android:** `-t:Run` builds, deploys to an attached device/emulator, and launches the activity. Requires Android SDK and an emulator or USB-connected device.
- **iOS:** building and running requires a Mac with Xcode. Use `dotnet build -t:Run -f net10.0-ios` to deploy to a connected iOS device or simulator.

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| `dotnet new` for `MyApp` | Two competing commands (`avalonia.mvvm` + `classlib`) | Only one should be used — `avalonia.mvvm` creates the full MVVM project |
| Platform selector in XAML | `Platform="android"` | Correct syntax is `:platform(android)` pseudo-class selector |
| `rd.xml` in NativeAOT section | Referenced as valid | Not supported by NativeAOT's ILLink — see 039V for correct format |
| Multi-target vs separate | Separate shown | Why separate is preferred (build time, dependencies, debugging) |

---

## See Also

- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — the original tutorial
- [042E — Multi-Targeting: Desktop, Browser, and Mobile (examples)](042-multi-targeting-desktop-browser-mobile-examples.md)
- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — lifetime dispatch in shared App.axaml.cs
- [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md) — cross-platform file access
- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md) — AOT publishing per platform
- [Avalonia Docs: Mobile and Browser](https://docs.avaloniaui.net/docs/platform-specific-guides/mobile-browser)
