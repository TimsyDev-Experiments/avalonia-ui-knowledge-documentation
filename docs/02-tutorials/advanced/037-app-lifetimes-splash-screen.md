---
tier: advanced
topic: bootstrap
estimated: 30 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 037 -- App Lifetimes and Splash Screen

**What you'll learn:** How Avalonia application lifetimes work on desktop, mobile, and browser targets, with patterns for splash screens, single-instance enforcement, and graceful shutdown.

**Prerequisites:** [001 -- Project Setup](/docs/02-tutorials/basics/001-project-setup.md)

---

## 1. Lifetime model overview

Avalonia provides three lifetime implementations:

| Lifetime | Platform | Entry point |
|----------|----------|-------------|
| `IClassicDesktopStyleApplicationLifetime` | Windows, macOS, Linux | `StartWithClassicDesktopLifetime` |
| `ISingleViewApplicationLifetime` | iOS, Android, Browser | `StartWithLinuxLifetime` (wrong — use `UsePlatformDetect`) |
| `IControlledApplicationLifetime` | Desktop | Manual shutdown control |

## 2. Classic desktop lifetime (standard)

```csharp
public static void Main(string[] args)
{
    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}

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

private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
{
    // Save unsaved data
}
```

## 3. Shutdown modes

```csharp
desktop.ShutdownMode = ShutdownMode.OnMainWindowClose; // Default
desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;  // Closes when all windows close
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown; // Manual only
```

Use `OnExplicitShutdown` for tray apps that should stay alive without windows:

```csharp
desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

Call `desktop.Shutdown()` when ready to exit.

## 4. Controlled lifetime (advanced)

```csharp
// Program.cs
public static void Main(string[] args)
{
    BuildAvaloniaApp()
        .StartWithControlledLifetime(args);
}

// App.axaml.cs
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

## 5. Single-instance enforcement

```csharp
public static void Main(string[] args)
{
    using var mutex = new Mutex(true, "MyAppInstance", out bool createdNew);
    if (!createdNew)
    {
        // Focus existing instance (platform-specific)
        return;
    }

    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}
```

For a robust implementation, use pipe communication to pass arguments to the existing instance.

## 6. Splash screen pattern

Create a lightweight splash window:

```xml
<!-- SplashWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        Width="400" Height="300"
        WindowStartupLocation="CenterScreen"
        WindowDecorations="None"
        CanResize="False"
        ShowInTaskbar="False"
        Background="#1E1E2E"
        Topmost="True">
  <Border VerticalAlignment="Center" HorizontalAlignment="Center">
    <StackPanel Spacing="16" Orientation="Vertical">
      <Image Source="avares://DemoApp/Assets/splash-icon.png"
             Width="64" Height="64" />
      <TextBlock Text="DemoApp" FontSize="24"
                 Foreground="White" HorizontalAlignment="Center" />
      <ProgressBar IsIndeterminate="True" Width="200" />
      <TextBlock Text="{Binding Status}" FontSize="12"
                 Foreground="#AAAAAA" HorizontalAlignment="Center" />
    </StackPanel>
  </Border>
</Window>
```

Show and close from `App.axaml.cs`:

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

## 7. Single-view lifetime (mobile/browser)

```csharp
// Program.cs
public static void Main(string[] args)
{
    BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
}

// App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
    {
        singleView.MainView = new MainView();
    }
    base.OnFrameworkInitializationCompleted();
}
```

## 8. Shutdown from a ViewModel

```csharp
[RelayCommand]
private void ExitApplication()
{
    if (Application.Current?.ApplicationLifetime
        is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.Shutdown();
    }
}
```

## Key takeaways

- `IClassicDesktopStyleApplicationLifetime` is the standard desktop lifetime; use `ISingleViewApplicationLifetime` for mobile/browser
- `ShutdownMode` controls when the app exits (main window, last window, or explicit)
- `IControlledApplicationLifetime` gives manual startup/exit events
- Single-instance enforcement uses `Mutex` at the OS level
- Splash screens use a lightweight `Window` shown before the main window
- Use `Shutdown()` in ViewModels through `Application.Current.ApplicationLifetime`
