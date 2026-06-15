---
tier: advanced
topic: bootstrap
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 037-app-lifetimes-splash-screen.md
---

# 037E — App Lifetimes and Splash Screen: Real-World Examples

**What this is:** Two complete scenarios that apply lifetime management, splash screens, single-instance enforcement, and startup orchestration from the tutorial.

**Prerequisites:** [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md), [037V — Verbose Companion](037-app-lifetimes-splash-screen-verbose.md)

---

## Example 1: Multi-Stage Splash with Cancellation

### Goal

Show a splash screen that reports real loading stages (config, database, plugins, services) with a cancellable progress bar. If any stage fails, display the error on the splash and let the user retry or exit. Transition to the main window only after all stages succeed.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class SplashViewModel : ObservableObject
{
    [ObservableProperty]
    private string _stageName = "Initializing...";

    [ObservableProperty]
    private int _stageProgress;

    [ObservableProperty]
    private int _totalStages = 4;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isCancelled;

    private CancellationTokenSource? _cts;

    public async Task<bool> RunStartupAsync()
    {
        IsCancelled = false;
        HasError = false;
        _cts = new CancellationTokenSource();

        var stages = new (string Name, Func<CancellationToken, Task> Action)[]
        {
            ("Loading configuration...", LoadConfigAsync),
            ("Connecting to database...", ConnectDatabaseAsync),
            ("Loading plugins...", LoadPluginsAsync),
            ("Initializing services...", InitServicesAsync),
        };

        TotalStages = stages.Length;

        for (int i = 0; i < stages.Length; i++)
        {
            if (_cts.Token.IsCancellationRequested)
            {
                IsCancelled = true;
                return false;
            }

            StageName = stages[i].Name;
            StageProgress = i + 1;

            try
            {
                await stages[i].Action(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                IsCancelled = true;
                return false;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"{stages[i].Name} failed:\n{ex.Message}";
                return false;
            }
        }

        IsCompleted = true;
        return true;
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void Retry()
    {
        HasError = false;
        ErrorMessage = null;
        StageProgress = 0;
    }

    // Simulated stage methods
    private static async Task LoadConfigAsync(CancellationToken ct)
    {
        await Task.Delay(800, ct);
    }

    private static async Task ConnectDatabaseAsync(CancellationToken ct)
    {
        await Task.Delay(1200, ct);
    }

    private static async Task LoadPluginsAsync(CancellationToken ct)
    {
        await Task.Delay(600, ct);
    }

    private static async Task InitServicesAsync(CancellationToken ct)
    {
        await Task.Delay(400, ct);
    }
}
```

### Splash Window (XAML)

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:DemoApp.ViewModels"
        x:Class="DemoApp.Views.SplashWindow"
        x:DataType="vm:SplashViewModel"
        Width="480" Height="320"
        WindowStartupLocation="CenterScreen"
        WindowDecorations="None"
        CanResize="False"
        ShowInTaskbar="False"
        Topmost="True"
        Background="#1E1E2E">
  <Grid RowDefinitions="*,Auto,Auto,Auto" Margin="40" Spacing="12">
    <!-- Logo / brand area -->
    <StackPanel Grid.Row="0" VerticalAlignment="Center"
                HorizontalAlignment="Center" Spacing="8">
      <TextBlock Text="DemoApp"
                 FontSize="28" FontWeight="Light"
                 Foreground="White"
                 HorizontalAlignment="Center" />
      <TextBlock Text="{Binding StageName}"
                 FontSize="13"
                 Foreground="#AAAAAA"
                 HorizontalAlignment="Center" />
    </StackPanel>

    <!-- Progress bar -->
    <ProgressBar Grid.Row="1"
                 Value="{Binding StageProgress}"
                 Maximum="{Binding TotalStages}"
                 Height="6" />

    <!-- Error state -->
    <Border Grid.Row="2" IsVisible="{Binding HasError}"
            Background="#33FF4444" CornerRadius="6" Padding="12">
      <StackPanel Spacing="8">
        <TextBlock Text="{Binding ErrorMessage}"
                   Foreground="#FF8888" TextWrapping="Wrap" />
        <StackPanel Orientation="Horizontal" Spacing="8">
          <Button Content="Retry"
                  Command="{Binding RetryCommand}" />
          <Button Content="Exit"
                  Command="{Binding CancelCommand}" />
        </StackPanel>
      </StackPanel>
    </Border>

    <!-- Cancel button (shown while loading) -->
    <Button Grid.Row="3" Content="Cancel"
            Command="{Binding CancelCommand}"
            IsVisible="{Binding HasError, Converter={StaticResource InvertBool}}"
            HorizontalAlignment="Center"
            Background="Transparent"
            Foreground="#888888" />
  </Grid>
</Window>
```

### App.axaml.cs — Wiring the Splash

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var splash = new SplashWindow();
        var splashVm = new SplashViewModel();
        splash.DataContext = splashVm;
        splash.Show();

        try
        {
            splashVm.RetryCommand.CanExecuteChanged += async (_, _) =>
            {
                if (splashVm.HasError)
                {
                    splash.Show(); // Re-show if hidden by error handling
                }
            };

            var success = await splashVm.RunStartupAsync();

            if (success)
            {
                var mainVm = App.Services.GetRequiredService<MainViewModel>();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainVm
                };
                desktop.MainWindow.Show();
                splash.Close();
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else if (splashVm.IsCancelled)
            {
                desktop.Shutdown();
            }
        }
        catch (Exception ex)
        {
            splashVm.ErrorMessage = $"Unexpected error: {ex.Message}";
        }
    }

    base.OnFrameworkInitializationCompleted();
}
```

### How It Works

1. **Stage-based progress** — The splash VM defines four loading stages. Each stage is a `Func<CancellationToken, Task>`. The `StageProgress` property drives a `ProgressBar` with `Maximum=4`.

2. **Cancellation support** — Each stage receives a `CancellationToken`. If the user clicks "Cancel", `_cts.Cancel()` is called, all pending `Task.Delay` (or real async operations) throw `OperationCanceledException`, and the loop exits.

3. **Error recovery** — When a stage fails, `HasError = true` and the error area becomes visible with Retry and Exit buttons. "Retry" resets the progress and re-enters the loop. "Exit" triggers shutdown.

4. **`ShutdownMode.OnExplicitShutdown`** — During splash display, the app must not shut down when the splash closes or when no main window exists. The mode transitions to `OnMainWindowClose` only after the main window is assigned.

5. **`async void` with try/catch** — `OnFrameworkInitializationCompleted` is `async void`. The entire splash logic is wrapped in try/catch. Unhandled exceptions in `async void` crash the process.

### Design Decisions and Trade-offs

- **Splash stays visible on error** — The splash window remains open on failure so the user sees the error and decides to retry or exit. Alternative: close the splash and show a separate error dialog.
- **`Retry` re-runs all stages** — The simplest implementation re-runs from the beginning. A more sophisticated version would retry only the failed stage by tracking the stage index.
- **No progress percentage** — The example uses stage-completion progress (1/4, 2/4). A real app might report sub-stage progress (e.g., "Loading plugin 5 of 20") via a separate `SubProgress` property.

---

## Example 2: Single-Instance Document Editor with Argument Forwarding

### Goal

Enforce a single running instance. When the user opens a document (via file association or drag-drop onto the executable), the second instance forwards the file path to the first instance via a named pipe, and the first instance opens the document in a new tab. The second instance then exits without showing a window.

### Program.cs — Instance Detection

```csharp
using System.IO.Pipes;
using System.Threading;
using Avalonia;
using DemoApp;

namespace DemoApp.Desktop;

class Program
{
    private const string MutexName = "DemoAppSingleInstance";
    private const string PipeName = "DemoAppPipe";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // Forward arguments to the existing instance
            if (args.Length > 0)
                ForwardToExistingInstance(args[0]);

            return;
        }

        // First instance — start the app
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ForwardToExistingInstance(string filePath)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipe.Connect(1000);
            using var writer = new StreamWriter(pipe);
            writer.Write(filePath);
        }
        catch (TimeoutException)
        {
            // Pipe server not ready — ignore silently
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

### Pipe Server — Running in the First Instance

```csharp
using System.IO.Pipes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DemoApp.Services;

public class SingleInstanceService : IDisposable
{
    private const string PipeName = "DemoAppPipe";
    private readonly Action<string> _onFileReceived;
    private CancellationTokenSource? _cts;

    public SingleInstanceService(Action<string> onFileReceived)
    {
        _onFileReceived = onFileReceived;
    }

    public void StartListening()
    {
        _cts = new CancellationTokenSource();
        _ = ListenAsync(_cts.Token);
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct);

                using var reader = new StreamReader(server);
                var filePath = await reader.ReadToEndAsync();

                if (!string.IsNullOrEmpty(filePath))
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        _onFileReceived(filePath));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                // Pipe broken — wait and retry
                await Task.Delay(500, ct);
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

### MainViewModel — Handling Incoming Files

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoApp.Services;

namespace DemoApp.ViewModels;

public partial class DocumentTab : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges;
}

public partial class MainViewModel : ObservableObject
{
    private readonly SingleInstanceService _singleInstance;

    public MainViewModel()
    {
        _singleInstance = new SingleInstanceService(OnFileReceived);
        _singleInstance.StartListening();
    }

    public ObservableCollection<DocumentTab> OpenTabs { get; } = new();

    [ObservableProperty]
    private DocumentTab? _activeTab;

    [ObservableProperty]
    private int _tabCount;

    private void OnFileReceived(string filePath)
    {
        // Check if already open
        var existing = OpenTabs.FirstOrDefault(t => t.FilePath == filePath);
        if (existing is not null)
        {
            ActiveTab = existing;
            return;
        }

        var content = File.ReadAllText(filePath);
        var tab = new DocumentTab
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            Content = content
        };

        OpenTabs.Add(tab);
        ActiveTab = tab;
        TabCount = OpenTabs.Count;
    }

    [RelayCommand]
    private void OpenFile()
    {
        // Manual file open via menu — same logic as OnFileReceived
    }

    [RelayCommand]
    private void CloseTab(DocumentTab? tab)
    {
        if (tab is null) return;
        OpenTabs.Remove(tab);
        TabCount = OpenTabs.Count;
    }
}
```

### App.axaml.cs — Wiring the Service

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainVm = new MainViewModel();
        desktop.MainWindow = new MainWindow
        {
            DataContext = mainVm
        };

        desktop.Exit += (_, _) =>
        {
            if (mainVm is IDisposable disposable)
                disposable.Dispose();
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

### How It Works

1. **Mutex on startup** — `Program.Main` creates a named `Mutex`. If `createdNew` is `false`, another instance is already running. The second instance calls `ForwardToExistingInstance` and exits.

2. **Named pipe forwarding** — `ForwardToExistingInstance` connects to the first instance's named pipe server (`NamedPipeClientStream`) and writes the file path. The pipe timeout is 1 second — if the server is not ready, the message is dropped silently.

3. **Pipe server loop** — `SingleInstanceService` runs an async loop that waits for incoming connections. Each connection reads the file path and dispatches it to `_onFileReceived` on the UI thread via `Dispatcher.UIThread.Post`.

4. **Tab deduplication** — `OnFileReceived` checks if the file path is already open in a tab. If yes, it activates the existing tab instead of opening a duplicate.

5. **Graceful shutdown** — `SingleInstanceService` implements `IDisposable`. The `Exit` event disposes the service, which cancels the pipe listener loop and releases the pipe name.

### Design Decisions and Trade-offs

- **Named pipe vs other IPC** — Named pipes work on Windows and Unix (macOS, Linux). Alternatives include TCP sockets (no OS-level access control), memory-mapped files (complex), or `WM_COPYDATA` (Windows-only). Named pipes strike the best balance.
- **No argument batching** — The pipe forwards one file path per connection. If the user selects multiple files, a new pipe connection is created per file. A production version would batch arguments or use a JSON message.
- **Mutex race window** — If the first instance crashes and the OS takes time to release the mutex, the second instance will see `createdNew = false` incorrectly. The pipe connection would fail (no server), and `TimeoutException` is caught silently. The user sees nothing happen. Add a retry loop or a second check after the pipe fails.
- **Desktop only** — Named mutexes and pipes do not work on WASM, Android, or iOS. Those platforms have OS-level single-instance enforcement (browser tabs, Android launchMode).

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Multi-Stage Splash | Example 2 — Single-Instance Editor |
|--------|--------------------------------|-------------------------------------|
| Lifetime used | `IClassicDesktopStyleApplicationLifetime` | `IClassicDesktopStyleApplicationLifetime` |
| ShutdownMode | `OnExplicitShutdown` → `OnMainWindowClose` | Default (`OnMainWindowClose`) |
| Startup orchestration | Async stages with progress | Mutex-based gating at `Program.Main` |
| Error handling | In-splash error with retry/exit | Silent drop (pipe timeout) |
| Cancellation | Yes, via `CancellationTokenSource` | N/A (second instance just exits) |
| IPC mechanism | None | Named pipe (`NamedPipeServerStream`) |
| UI feedback during startup | Splash window with stage text | None (first instance launches normally) |
| Platform support | Desktop | Desktop only (Mutex + named pipe) |
| Complexity | Medium — async void, try/catch, CancellationToken | Medium — pipe server lifecycle, Mutex |

## See Also

- [037 — App Lifetimes and Splash Screen](037-app-lifetimes-splash-screen.md) — the original tutorial
- [037V — App Lifetimes and Splash Screen (verbose companion)](037-app-lifetimes-splash-screen-verbose.md)
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — lifetime dispatch per platform
- [034 — File Pickers and Platform Services](034-file-pickers-platform-services.md) — file open in the editor
- [Avalonia Docs: Application Lifetimes](https://docs.avaloniaui.net/docs/concepts/application-lifetimes)
