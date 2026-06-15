---
tier: advanced
topic: threading and async
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 044-background-services-and-progress.md
---

# 044X — Background Services and Progress Reporting: Real-World Examples

**What you'll build:** A batch file-export worker with cancellable progress reporting, and a periodic API-polling background service that pushes real-time status updates to the UI via IMessenger.

**Prerequisites:** [044 — Background Services and Progress Reporting](044-background-services-and-progress.md). The [verbose companion](044-background-services-and-progress-verbose.md) covers threading semantics, PeriodicTimer mechanics, and Channel<T> patterns in depth.

---

## Example 1: Batch File Export with Cancellable Progress

**Goal:** Export a list of documents to individual files on disk while reporting per-file progress back to the UI and allowing the user to cancel mid-operation.

### ViewModel

```csharp
// ViewModels/ExportViewModel.cs
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private readonly ExportService _service;

    [ObservableProperty]
    private int _currentFile;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isRunning;

    private CancellationTokenSource? _cts;

    public ExportViewModel(ExportService service)
    {
        _service = service;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartExportAsync()
    {
        var files = new[]
        {
            "report-q1.pdf", "report-q2.pdf",
            "report-q3.pdf", "report-q4.pdf"
        };
        TotalFiles = files.Length;
        CurrentFile = 0;
        IsRunning = true;
        StatusMessage = "Starting...";

        _cts = new CancellationTokenSource();
        var progress = new Progress<(int index, string file)>(pair =>
        {
            CurrentFile = pair.index;
            StatusMessage = $"Exporting {pair.file}...";
        });

        try
        {
            await _service.ExportFilesAsync(files, progress, _cts.Token);
            StatusMessage = "Export complete.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Export cancelled.";
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void CancelExport()
    {
        _cts?.Cancel();
    }

    private bool CanStart() => !IsRunning;
}
```

### Service

```csharp
// Services/ExportService.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Services;

public class ExportService
{
    public async Task ExportFilesAsync(
        string[] files,
        IProgress<(int index, string file)> progress,
        CancellationToken ct)
    {
        for (int i = 0; i < files.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            // Simulate file write
            await Task.Delay(800, ct);

            progress.Report((i + 1, files[i]));
        }
    }
}
```

### View

```xml
<!-- File: Views/ExportView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.ExportView"
             x:DataType="vm:ExportViewModel">

  <StackPanel Spacing="12" Margin="20">
    <TextBlock Text="{Binding StatusMessage}" />

    <ProgressBar Value="{Binding CurrentFile}"
                 Minimum="0"
                 Maximum="{Binding TotalFiles}" />

    <TextBlock Text="{Binding CurrentFile, StringFormat='File {0} of {1}'}"
               FontSize="12" Foreground="Gray" />

    <StackPanel Orientation="Horizontal" Spacing="8">
      <Button Content="Start Export"
              Command="{Binding StartExportCommand}" />
      <Button Content="Cancel"
              Command="{Binding CancelExportCommand}"
              IsEnabled="{Binding IsRunning}" />
    </StackPanel>
  </StackPanel>
</UserControl>
```

### How It Works

1. The user clicks **Start Export**. `StartExportCommand` fires on the UI thread.
2. The ViewModel creates a `CancellationTokenSource` and a `Progress<(int, string)>`. Because `Progress<T>` is constructed on the UI thread, its callback captures `DispatcherSynchronizationContext`.
3. `ExportService.ExportFilesAsync` runs on the calling thread until the first `await Task.Delay`, at which point control returns to the ViewModel's async state machine. The service loop runs on thread-pool threads after that.
4. Each iteration calls `progress.Report(...)`. `Progress<T>` posts the callback to the captured dispatcher context, so `CurrentFile` and `StatusMessage` are set on the UI thread — no explicit `Dispatcher.UIThread.Post` needed.
5. If the user clicks **Cancel**, `_cts.Cancel()` signals the token. The next `ct.ThrowIfCancellationRequested()` in the service throws `OperationCanceledException`, which the ViewModel catches and surfaces as "Export cancelled."
6. `CanExecute` for `StartExportCommand` returns `false` while `IsRunning` is true, preventing double-starts.

### Key Points

- `Progress<T>` is the simplest pattern for operation-scoped progress because it marshals automatically — no `IMessenger` registration needed.
- Cancellation uses `CancellationTokenSource` scoped to the operation. Always dispose it in `finally`.
- The service is agnostic of the UI — it takes `IProgress<T>` and `CancellationToken`, making it testable with mock progress and a cancelled token.
- Edge case: if the user cancels after the last `ThrowIfCancellationRequested` but before the loop ends, the operation completes normally. This is acceptable — partial completion with a "complete" message is better than a false "cancelled."

---

## Example 2: Periodic API Polling with Real-Time Status Push

**Goal:** A `BackgroundService` that polls a remote API every 30 seconds and pushes status changes to connected ViewModels via `IMessenger`, with the ViewModel dispatching to the UI thread.

### Message

```csharp
// Messages/ServerStatusMessage.cs
namespace MyApp.Messages;

public class ServerStatusMessage
{
    public bool IsOnline { get; }
    public int ActiveConnections { get; }
    public DateTime Timestamp { get; }

    public ServerStatusMessage(bool isOnline, int activeConnections, DateTime timestamp)
    {
        IsOnline = isOnline;
        ActiveConnections = activeConnections;
        Timestamp = timestamp;
    }
}
```

### Background Service

```csharp
// Services/HealthCheckService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using MyApp.Messages;

namespace MyApp.Services;

public sealed class HealthCheckService : BackgroundService
{
    private readonly IMessenger _messenger;
    private readonly HealthApiClient _api;

    public HealthCheckService(IMessenger messenger, HealthApiClient api)
    {
        _messenger = messenger;
        _api = api;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var status = await _api.GetHealthAsync(stoppingToken);
                _messenger.Send(new ServerStatusMessage(
                    status.IsOnline,
                    status.ActiveConnections,
                    DateTime.UtcNow));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _messenger.Send(new ServerStatusMessage(
                    false, 0, DateTime.UtcNow));
            }
        }
    }
}
```

### ViewModel

```csharp
// ViewModels/DashboardViewModel.cs
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Avalonia.Threading;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class DashboardViewModel : ObservableObject,
    IRecipient<ServerStatusMessage>
{
    [ObservableProperty]
    private bool _isServerOnline;

    [ObservableProperty]
    private int _activeConnections;

    [ObservableProperty]
    private string _lastUpdated = "";

    public DashboardViewModel()
    {
        WeakReferenceMessenger.Default.Register<ServerStatusMessage>(this);
    }

    public void Receive(ServerStatusMessage message)
    {
        // Called on the background thread — dispatch to UI
        Dispatcher.UIThread.Post(() =>
        {
            IsServerOnline = message.IsOnline;
            ActiveConnections = message.ActiveConnections;
            LastUpdated = message.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        });
    }
}
```

### Registration

```csharp
// Program.cs
builder.Services.AddSingleton<HealthApiClient>();
builder.Services.AddHostedService<HealthCheckService>();
```

### View

```xml
<!-- File: Views/DashboardView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.DashboardView"
             x:DataType="vm:DashboardViewModel">

  <StackPanel Spacing="8" Margin="20">
    <TextBlock FontSize="18" FontWeight="Bold">Server Status</TextBlock>

    <StackPanel Orientation="Horizontal" Spacing="8">
      <Rectangle Width="12" Height="12" RadiusX="6" RadiusY="6"
                 Fill="{Binding IsServerOnline, Converter={StaticResource BoolToColor}}" />
      <TextBlock Text="{Binding IsServerOnline, StringFormat='Server: {0}'}" />
    </StackPanel>

    <TextBlock Text="{Binding ActiveConnections, StringFormat='Active connections: {0}'}" />
    <TextBlock Text="{Binding LastUpdated, StringFormat='Last checked: {0}'}"
               FontSize="11" Foreground="Gray" />
  </StackPanel>
</UserControl>
```

### How It Works

1. `HealthCheckService` runs as a hosted service. Its `ExecuteAsync` loop starts on a thread-pool thread.
2. `PeriodicTimer(TimeSpan.FromSeconds(30))` yields control every 30 seconds without drift. `WaitForNextTickAsync` is cancellable via `stoppingToken`.
3. On each tick, the service calls `_api.GetHealthAsync()`, then sends a `ServerStatusMessage` via `IMessenger.Send`.
4. `DashboardViewModel` implements `IRecipient<ServerStatusMessage>` and is registered with the messenger at construction. The `Receive` method is called synchronously on the background thread — the same thread that called `_messenger.Send`.
5. Inside `Receive`, `Dispatcher.UIThread.Post` queues the property updates on the UI thread. `Post` (not `Invoke`) is used to avoid deadlocks — the background service does not wait for the UI update to complete.
6. If the API call throws (network failure, server down), the catch block sends a message with `IsOnline = false` and zero connections. The UI shows "offline" without crashing.

### Key Points

- `PeriodicTimer` keeps the 30-second interval precise regardless of how long `GetHealthAsync` takes. `Task.Delay` in a loop would drift.
- `IMessenger` decouples the background service from the ViewModel — multiple ViewModels can observe the same message.
- The ViewModel explicitly dispatches to the UI thread. This is the main trade-off vs `IProgress<T>`: more control, more boilerplate.
- Edge case: if the app shuts down mid-poll, `stoppingToken` is signalled. `WaitForNextTickAsync` observes it and exits the loop. The `OperationCanceledException` catch is a safety net for the API call itself.
- Edge case: if the dispatcher queue is full (rapfire messages), `Post` still succeeds because it never blocks. The queue processes them in order.

---

## What These Examples Demonstrate

| Scenario | Pattern | Marshalling | Best for |
|---|---|---|---|
| Batch export | `IProgress<T>` + `CancellationTokenSource` | Automatic via captured SynchronizationContext | User-triggered, operation-scoped work with known duration |
| Periodic health check | `BackgroundService` + `IMessenger` + `Dispatcher.UIThread.Post` | Manual dispatch in receiver | Long-lived, continuous background work with multi-consumer notifications |

The export example shows operation-scoped progress where the ViewModel owns the cancellation and the work is finite. The health check example shows a service that runs for the entire app lifetime and pushes updates to any interested ViewModel.

## See Also

- [044 — Background Services and Progress Reporting](044-background-services-and-progress.md)
- [044V — Verbose Companion](044-background-services-and-progress-verbose.md)
- [032 — Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [014 — IMessenger Patterns](../intermediate/014-imessenger-patterns.md)
