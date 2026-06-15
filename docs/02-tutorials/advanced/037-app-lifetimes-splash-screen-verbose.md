---
tier: advanced
topic: bootstrap
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 037-app-lifetimes-splash-screen.md
---

# 037V — App Lifetimes and Splash Screen: An In-Depth Companion

**Why this exists.** The original tutorial maps out the three lifetime types, shutdown modes, single-instance enforcement, and splash screens. This companion explains why Avalonia needs separate lifetimes per platform, how `ShutdownMode` interacts with the OS window manager, what `IControlledApplicationLifetime` gives you over the classic lifetime, why the mutex pattern has race conditions, and how the splash window timing avoids a blank flash.

**Read this alongside:** [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md)

---

## 1. Why three lifetimes exist

Different platforms expose fundamentally different application lifecycle models:

| Platform | Lifecycle | Why separate? |
|----------|-----------|---------------|
| Desktop (Win/Linux/macOS) | Process runs until user/OS kills it | Classic windowing with multiple windows |
| Browser (WASM) | Single-page app, page lifecycle | No window manager — one `ContentView` in a `<div>` |
| Mobile (Android/iOS) | Activity/Fragment/AppDelegate lifecycle | OS can pause, resume, kill at any time |

Avalonia's three lifetimes map to these models:

- **`IClassicDesktopStyleApplicationLifetime`**: for desktop apps that own their window lifecycle. Provides `MainWindow`, `ShutdownMode`, `Exit` event.
- **`ISingleViewApplicationLifetime`**: for mobile and WASM. Provides `MainView` — a single `Control` that fills the platform surface.
- **`IControlledApplicationLifetime`**: for desktop apps that need manual startup/exit orchestration. Exposes `Startup` and `Exit` events without assuming a main window.

`UsePlatformDetect()` picks the correct lifetime for the current platform. You can also force a lifetime by calling `StartWithClassicDesktopLifetime(args)` or the platform-specific methods.

---

## 2. `IClassicDesktopStyleApplicationLifetime` — what it controls

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow();
        desktop.Exit += OnExit;
        desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
    base.OnFrameworkInitializationCompleted();
}
```

`MainWindow` is not just a convenience property — it drives `ShutdownMode.OnMainWindowClose`. When the main window closes, the lifetime checks the shutdown mode and calls `Shutdown()` if appropriate.

`Shutdown()` does the following:

1. Raises the `Exit` event (synchronous).
2. Calls `Environment.Exit(exitCode)` on desktop.
3. On browser/mobile, `Exit` is not available — the app cannot force the browser tab or Android activity to close.

**Common mistake:** assigning `MainWindow` after `OnFrameworkInitializationCompleted` returns. The lifetime caches the reference at assignment time. Changing it later does not affect shutdown behavior.

---

## 3. ShutdownMode options

```csharp
desktop.ShutdownMode = ShutdownMode.OnMainWindowClose; // Default
desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

- **`OnMainWindowClose`** (default): shuts down when `MainWindow` closes. Other windows (tool windows, dialogs) do not keep the app alive. Good for most apps.
- **`OnLastWindowClose`**: tracks all `Window` instances opened via `Window.Show()`. When the last one closes, the app shuts down. Essential for apps with multiple document windows (like MDI-style editors).
- **`OnExplicitShutdown`**: the app never shuts down automatically. Call `desktop.Shutdown()` programmatically. Required for tray-icon apps that should stay alive with zero windows. Also useful for splash screen workflows where `MainWindow` is assigned after the splash.

The lifetime tracks "application windows" — windows that were shown (via `Show()` or `ShowDialog()`). The `OnLastWindowClose` mode does not count windows that were created but never shown.

---

## 4. `IControlledApplicationLifetime` — why use it

```csharp
public static void Main(string[] args)
{
    BuildAvaloniaApp()
        .StartWithControlledLifetime(args);
}
```

`IControlledApplicationLifetime` gives you `Startup` and `Exit` events instead of the `OnFrameworkInitializationCompleted` pattern. Use it when:

- You need to run custom code **before** any window is created.
- You want to control when `Startup` and `Exit` fire independently of window lifecycle.
- You are embedding Avalonia inside a larger application (e.g., a plugin host, a game engine UI layer).

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IControlledApplicationLifetime controlled)
    {
        controlled.Startup += (_, _) =>
        {
            var mw = new MainWindow();
            mw.Show();
        };
        controlled.Exit += (_, args) =>
        {
            args.ApplicationExitCode = 0;
        };
    }
    base.OnFrameworkInitializationCompleted();
}
```

Note that `Startup` fires after `OnFrameworkInitializationCompleted` returns. The `MainWindow.Show()` call in the event handler is standard — the `OnFrameworkInitializationCompleted` method sets up the handler, and the event fires when the framework is ready.

**For most apps, `IClassicDesktopStyleApplicationLifetime` is sufficient.** Use controlled lifetime only when the event-based startup model is required.

---

## 5. Single-instance enforcement — why `Mutex` is not enough

```csharp
using var mutex = new Mutex(true, "MyAppInstance", out bool createdNew);
if (!createdNew)
{
    // Focus existing instance (platform-specific)
    return;
}
```

A named `Mutex` is an OS-level synchronization primitive. `new Mutex(true, name, out createdNew)` attempts to create a named mutex. If `createdNew` is `false`, another process already holds the mutex.

**Limitations:**

- **No argument forwarding:** the second instance exits without passing its arguments to the first instance. Implement a pipe server (named pipe, TCP) in the first instance to receive arguments.
- **Race condition on crash:** if the first instance crashes without releasing the mutex, the OS auto-releases it after a timeout (usually ~30 seconds). The second instance cannot start until then.
- **Desktop only:** WASM, Android, and iOS do not support named mutexes. On those platforms, single-instance is handled by the OS.

**Robust single-instance:** use a named pipe server in the first instance. The second instance attempts to connect to the pipe, sends its arguments, and exits. The first instance processes the arguments (opens a document, focuses a window).

---

## 6. Splash screen — why it works

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splash = new SplashWindow();
        splash.Show();

        // Simulate background loading
        await Task.Delay(2000);

        desktop.MainWindow = new MainWindow();
        splash.Close();
    }
    base.OnFrameworkInitializationCompleted();
}
```

**Why this pattern works:**

1. `splash.Show()` creates the OS window handle and presents the window immediately. The platform compositor renders the splash independently of the main window's loading.
2. The `await Task.Delay(2000)` (or any async initialization) yields control back to the UI thread. The splash window remains responsive (renders, handles paint events).
3. `desktop.MainWindow = new MainWindow()` assigns the main window. At this point, `ShutdownMode.OnMainWindowClose` recognizes it as the shutdown trigger.
4. `splash.Close()` destroys the splash window. The main window is now the only visible window.

**Critical detail:** `OnFrameworkInitializationCompleted` is `async void` — it cannot be `async Task` because the Avalonia framework expects a void return. `async void` means exceptions thrown inside cannot be caught by the caller. Wrap the body in `try/catch`:

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    try
    {
        // splash logic
    }
    catch (Exception ex)
    {
        // Log, show error dialog, or shutdown gracefully
    }
    finally
    {
        base.OnFrameworkInitializationCompleted();
    }
}
```

**Common mistake:** blocking the UI thread during splash initialization with `.Result` or `Wait()`. The splash window freezes and the OS may show "Application not responding". Use `await` for all async operations.

---

## 7. Splash window design — what makes it "lightweight"

```xml
<Window Width="400" Height="300"
        WindowStartupLocation="CenterScreen"
        WindowDecorations="None"
        CanResize="False"
        ShowInTaskbar="False"
        Background="#1E1E2E"
        Topmost="True">
```

A splash window is intentionally minimal:

- **`WindowDecorations="None"`:** no title bar — avoids OS decoration overhead and makes the window look like a splash, not a real window.
- **`CanResize="False"`:** the window manager does not allocate resize handles or hit-test regions for edges.
- **`ShowInTaskbar="False"`:** the window does not appear in the OS taskbar. The user does not Alt+Tab to it.
- **`Topmost="True"`:** the splash appears above all windows, including the desktop. On Windows, this sets `WS_EX_TOPMOST`.

**Why not use a `SplashScreen` class like WPF?** Avalonia does not have a built-in `SplashScreen` API. The lightweight window pattern is the canonical approach.

---

## 8. Single-view lifetime — how it differs

```csharp
if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
{
    singleView.MainView = new MainView();
}
```

`MainView` is a `Control`, not a `Window`. On WASM, this control is attached to a `<div>` in the HTML page. On Android, it fills the `AvaloniaMainActivity` content area. On iOS, it fills the `AvaloniaAppDelegate` view.

**What you lose:**

- No `ShowDialog`/`Show` — only one view at a time.
- No `ShutdownMode` — the app lives as long as the page/activity.
- No system tray, no multiple windows.
- No `TopLevel.Clipboard` access without user gesture (browser restriction).

**What you must do differently:**

- Use overlay dialogs (see [035 — Custom Dialogs and Window Management](035-custom-dialogs-window-management.md)).
- Use `StorageProvider` for all file access (no direct file paths).
- Check `OperatingSystem.IsBrowser()` before calling desktop-only APIs.

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| Table entry for `ISingleViewApplicationLifetime` | Shows `StartWithClassicDesktopLifetime` | Incorrect — single-view uses `StartWithBrowserPlatform`/etc. |
| `Mutex` single-instance | Presented as pattern | No argument forwarding; desktop-only; race-condition on crash |
| `async void` splash | Shown without error handling | Exceptions are uncatchable — always wrap in try/catch |
| Controlled lifetime | Shown as alternative | Only needed for event-based startup; use classic for most apps |

---

## See Also

- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — the original tutorial
- [037E — App Lifetimes and Splash Screen (examples)](037-app-lifetimes-splash-screen-examples.md)
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — lifetime dispatch in multi-target projects
- [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md) — platform-aware service access
- [Avalonia Docs: Application Lifetimes](https://docs.avaloniaui.net/docs/concepts/application-lifetimes)
