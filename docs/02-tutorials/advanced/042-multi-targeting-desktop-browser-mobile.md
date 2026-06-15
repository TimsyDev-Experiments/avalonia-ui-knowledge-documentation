---
tier: advanced
topic: build
estimated: 20 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 042 -- Multi-Targeting: Desktop, Browser, and Mobile

**What you'll learn:** How to structure an Avalonia project that targets desktop (Windows/macOS/Linux), browser (WASM), and mobile (Android/iOS) from a shared codebase.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md), [037 -- App Lifetimes and Splash Screen](../advanced/037-app-lifetimes-splash-screen.md)

---

## 1. Target matrix

| Platform | .NET TF | Project | Entry point | Controls |
|----------|---------|---------|-------------|----------|
| Windows | `net10.0` | `MyApp.Desktop` | `StartWithClassicDesktopLifetime` | Full |
| macOS | `net10.0` | `MyApp.Desktop` | `StartWithClassicDesktopLifetime` | Full |
| Linux | `net10.0` | `MyApp.Desktop` | `StartWithClassicDesktopLifetime` | Full |
| Browser | `net10.0` | `MyApp.Browser` | `StartWithBrowserPlatform` | Limited |
| Android | `net10.0-android` | `MyApp.Android` | `StartWithAndroidPlatform` | Limited |
| iOS | `net10.0-ios` | `MyApp.iOS` | `StartWithiOSPlatform` | Limited |

---

## 2. Solution structure

```
MyApp.sln
├── src/
│   ├── MyApp/                    # Shared library
│   │   ├── App.axaml
│   │   ├── App.axaml.cs
│   │   ├── ViewModels/
│   │   ├── Views/
│   │   ├── Models/
│   │   └── MyApp.csproj          # net10.0 class library
│   │
│   ├── MyApp.Desktop/            # Windows/macOS/Linux
│   │   ├── Program.cs
│   │   └── MyApp.Desktop.csproj  # net10.0, references MyApp
│   │
│   ├── MyApp.Browser/            # WebAssembly
│   │   ├── Program.cs
│   │   ├── AppBundle/
│   │   │   ├── index.html
│   │   │   └── main.js
│   │   └── MyApp.Browser.csproj  # net10.0-browser
│   │
│   ├── MyApp.Android/            # Android
│   │   ├── MainActivity.cs
│   │   ├── AndroidManifest.xml
│   │   └── MyApp.Android.csproj  # net10.0-android
│   │
│   └── MyApp.iOS/                # iOS
│       ├── AppDelegate.cs
│       ├── Info.plist
│       └── MyApp.iOS.csproj      # net10.0-ios
```

---

## 3. Creating the projects

```bash
# Shared library
dotnet new avalonia.mvvm -n MyApp -o src/MyApp
dotnet new classlib -n MyApp -o src/MyApp

# Desktop
dotnet new avalonia.app -n MyApp.Desktop -o src/MyApp.Desktop

# Browser
dotnet new avalonia.browser -n MyApp.Browser -o src/MyApp.Browser

# Android
dotnet new avalonia.android -n MyApp.Android -o src/MyApp.Android

# iOS
dotnet new avalonia.ios -n MyApp.iOS -o src/MyApp.iOS
```

Add project references:

```bash
dotnet add src/MyApp.Desktop reference src/MyApp/MyApp.csproj
dotnet add src/MyApp.Browser reference src/MyApp/MyApp.csproj
dotnet add src/MyApp.Android reference src/MyApp/MyApp.csproj
dotnet add src/MyApp.iOS reference src/MyApp/MyApp.csproj
```

---

## 4. Shared App.axaml.cs with lifetime abstraction

```csharp
// src/MyApp/App.axaml.cs
namespace MyApp;

public partial class App : Application
{
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
}
```

---

## 5. Desktop entry point

```csharp
// src/MyApp.Desktop/Program.cs
using Avalonia;
using MyApp;

namespace MyApp.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

---

## 6. Browser entry point

```csharp
// src/MyApp.Browser/Program.cs
using Avalonia;
using Avalonia.Web;
using MyApp;

namespace MyApp.Browser;

class Program
{
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithBrowserPlatform(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### Required packages

```xml
<PackageReference Include="Avalonia.Web" Version="12.0.4" />
<PackageReference Include="Avalonia.Web.Blazor" Version="12.0.4" />
```

### index.html

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>MyApp</title>
  <script src="main.js"></script>
  <style>
    html, body, #app { margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; }
  </style>
</head>
<body>
  <div id="app"></div>
</body>
</html>
```

### main.js

```js
import { dotnet } from './dotnet.js';

const { getAssemblyExports, getConfig } = await dotnet
    .withDiagnosticTracing(false)
    .create();

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
await dotnet.run();
```

---

## 7. Android entry point

```csharp
// src/MyApp.Android/MainActivity.cs
using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using MyApp;

namespace MyApp.Android;

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

### Required packages

```xml
<PackageReference Include="Avalonia.Android" Version="12.0.4" />
```

---

## 8. iOS entry point

```csharp
// src/MyApp.iOS/AppDelegate.cs
using Avalonia;
using Avalonia.iOS;
using Foundation;
using MyApp;

namespace MyApp.iOS;

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
// src/MyApp.iOS/Program.cs
using UIKit;

namespace MyApp.iOS;

class Program
{
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
```

### Required packages

```xml
<PackageReference Include="Avalonia.iOS" Version="12.0.4" />
```

---

## 9. Platform-specific code

Use `OperatingSystem` APIs for conditional logic:

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

Use conditional compilation (less preferred):

```csharp
#if ANDROID
    // Android-specific code
#elif IOS
    // iOS-specific code
#elif BROWSER
    // Browser-specific code
#else
    // Desktop fallback
#endif
```

### Platform-specific XAML

Use a `<Style>` with `Platform` selector:

```xml
<Style Selector="Button" Platform="android">
  <Setter Property="FontSize" Value="18" />
</Style>
<Style Selector="TextBlock" Platform="browser">
  <Setter Property="FontFamily" Value="system-ui" />
</Style>
```

---

## 10. Platform limitations

| Feature | Desktop | Browser | Android | iOS |
|---------|---------|---------|---------|-----|
| Full windowing | Yes | Single view | Single view | Single view |
| File system access | Full | Sandboxed | Sandboxed | Sandboxed |
| TrayIcon | Yes | No | No | No |
| Drag-drop between apps | Yes | Limited | Limited | Limited |
| Threading | Full | WASM limited | Full | Full |
| GPU rendering | Direct3D/Vulkan | WebGL | OpenGL ES | Metal |

---

## 11. Build and run

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

## Key takeaways

- Use separate project-per-platform with a shared `MyApp` class library
- Shared `App.axaml.cs` checks `ApplicationLifetime` type to determine platform
- Desktop: `StartWithClassicDesktopLifetime` / Browser: `StartWithBrowserPlatform` / Android: `AvaloniaMainActivity<App>` / iOS: `AvaloniaAppDelegate<App>`
- Use `OperatingSystem.IsBrowser()` / `IsAndroid()` / `IsIOS()` for conditional code
- Platform-specific XAML styles use the `Platform` selector
- Browser and mobile have sandboxed file systems — use `StorageProvider` always
- Single-view platforms cannot open multiple windows natively

---

## See Also

- [042E — Multi-Targeting: Desktop, Browser, and Mobile (examples)](042-multi-targeting-desktop-browser-mobile-examples.md)
- [042V — Multi-Targeting: Desktop, Browser, and Mobile (verbose companion)](042-multi-targeting-desktop-browser-mobile-verbose.md)
- [037 -- App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md)
- [034 -- File Pickers and Platform Services](034-file-pickers-platform-services.md)
- [039 -- NativeAOT and Trimming](039-nativeaot-trimming.md)
- [Avalonia Docs: Mobile and Browser](https://docs.avaloniaui.net/docs/platform-specific-guides/mobile-browser)
