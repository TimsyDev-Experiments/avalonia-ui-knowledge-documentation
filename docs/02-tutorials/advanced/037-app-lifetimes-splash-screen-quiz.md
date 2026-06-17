---
tier: advanced
topic: bootstrap
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 037-app-lifetimes-splash-screen.md
---

# Quiz — App Lifetimes & Splash Screen

```quiz
Q: Which lifetime interface should be used for a cross-platform application that targets desktop, browser, and mobile?
A. IClassicDesktopStyleApplicationLifetime for desktop, ISingleViewApplicationLifetime for mobile/browser (correct) || Desktop uses ClassicDesktop with MainWindow; mobile and browser use SingleView with a MainView.
B. IControlledApplicationLifetime for all targets || ControlledLifetime is desktop-only and requires manual Show() calls, unsuitable for single-view platforms.
C. ISingleViewApplicationLifetime for all targets || SingleView does not support multiple windows or desktop-specific features like tray icons.
D. IApplicationLifetime directly || IApplicationLifetime is the base interface with limited functionality; you must cast to the concrete lifetime.
Explanation: Use IClassicDesktopStyleApplicationLifetime for desktop and ISingleViewApplicationLifetime for mobile/browser. Use UsePlatformDetect() and the appropriate StartWith* method.
```

```quiz
Q: What ShutdownMode should be used for a system-tray application that must remain running when no windows are open?
A. ShutdownMode.OnExplicitShutdown (correct) || The app stays alive until Shutdown() is called explicitly, even with zero open windows.
B. ShutdownMode.OnMainWindowClose || The app exits when MainWindow closes, even if the tray icon is still active.
C. ShutdownMode.OnLastWindowClose || The app exits when the last window closes, which is undesirable for tray-only operation.
D. ShutdownMode.Never || There is no Never value in the ShutdownMode enum.
Explanation: OnExplicitShutdown gives full control; call desktop.Shutdown() when the user exits via the tray menu.
```

```quiz
Q: Which code correctly enforces a single application instance?
A. 
```csharp
using var mutex = new Mutex(true, "MyAppInstance", out bool createdNew);
if (!createdNew) return;
```
 (correct) || A named Mutex is OS-global. If createdNew is false, another instance owns the mutex, so the new process exits.
B. `if (Process.GetProcessesByName("MyApp").Length > 1) return;` || This checks process count by name, but a race condition exists between the check and the process starting.
C. `Application.Current.Properties["SingleInstance"] = true;` || Application.Properties is in-process only and does not prevent a second instance.
D. `desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;` || ShutdownMode controls how the current process exits; it does not prevent other instances from starting.
Explanation: A named Mutex provides OS-level mutual exclusion. The first instance creates it; subsequent instances see createdNew=false and exit.
```

```quiz
Q: In the splash-screen pattern, why must Show() be called on the splash Window before assigning MainWindow?
A. Show() renders the splash window on screen while background initialization runs; setting MainWindow later transitions to the primary UI (correct) || The splash window is shown immediately; after async work completes, MainWindow is assigned and the splash is closed.
B. Show() registers the window in the visual tree so TopLevel.GetTopLevel works on splash controls || TopLevel works without Show; the splash must be visible before the user sees it.
C. Setting MainWindow automatically closes all other windows, so the splash must be shown first || Assigning MainWindow does not close other windows; splash.Close() is called explicitly.
D. Show() initializes the XAML compiler for the application || The XAML compiler initializes at app start, not on Show().
Explanation: Show the splash, perform async loading, then assign MainWindow (which shows it) and close the splash.
```

```quiz
Q: How does a ViewModel access the application lifetime to call Shutdown() without referencing Avalonia.Controls directly?
A. 
```csharp
if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    desktop.Shutdown();
```
 (correct) || Application.Current.ApplicationLifetime returns the lifetime instance; cast to the desktop interface and call Shutdown().
B. `Window.Current.Shutdown();` || There is no Window.Current or Shutdown on Window.
C. `App.Current.Shutdown();` || Application has a Shutdown method but it is not the correct way; use the lifetime's Shutdown to respect shutdown mode.
D. `TopLevel.GetTopLevel(Application.Current).Shutdown();` || TopLevel.GetTopLevel requires a visual control, not Application.
Explanation: Application.Current.ApplicationLifetime provides the active lifetime. Cast to IClassicDesktopStyleApplicationLifetime and call Shutdown().
```
