---
tier: advanced
topic: threading and async
estimated: 20 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 044 -- Background Services and Progress Reporting

**What you'll learn:** Run background work in a long-lived hosted service, report progress back to the UI thread, handle cancellation, and wire a background worker into an Avalonia application using DI.

**Prerequisites:** [008 -- RelayCommand](../basics/008-relay-command.md), [032 -- Dependency Injection for MVVM](032-mvvm-di-wiring.md)

---

## 1. The hosted service pattern

A background service is a class that runs continuously on a non-UI thread, typically processing a queue or polling a resource. The standard .NET base class is `BackgroundService` (from `Microsoft.Extensions.Hosting.Abstractions`).

```csharp
public sealed class FileWatcherService : BackgroundService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly string _watchPath;

    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
        _watchPath = Path.Combine(Environment.CurrentDirectory, "incoming");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileWatcherService started, watching {Path}", _watchPath);
        Directory.CreateDirectory(_watchPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            var files = Directory.GetFiles(_watchPath, "*.csv");
            foreach (var file in files)
            {
                _logger.LogInformation("Processing {File}", file);
                // simulate processing
                await Task.Delay(500, stoppingToken);
                File.Move(file, Path.ChangeExtension(file, ".done"));
            }

            await Task.Delay(3000, stoppingToken);
        }
    }
}
```

Register in `Program.cs`:

```csharp
// Program.cs
builder.Services.AddHostedService<FileWatcherService>();
```

For Avalonia, the host runs on the DI container you already own. The file watcher above lives entirely on a background thread — it never touches the UI.

## 2. Background work that reports to the UI

When a background service needs to update the UI (e.g., show progress, add items to a list), it sends messages or raises events that the ViewModel observes.

### 2a. Via IMessenger

```csharp
public class ProgressMessage
{
    public int Percent { get; }
    public string Status { get; }
    public ProgressMessage(int percent, string status) => (Percent, Status) = (percent, status);
}

public sealed class ExportService : BackgroundService
{
    private readonly IMessenger _messenger;

    public ExportService(IMessenger messenger) => _messenger = messenger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _messenger.Send(new ProgressMessage(0, "Starting export..."));
        int total = 100;

        for (int i = 1; i <= total; i++)
        {
            await Task.Delay(50, stoppingToken);
            _messenger.Send(new ProgressMessage(i * 100 / total, $"Processing item {i}"));
        }

        _messenger.Send(new ProgressMessage(100, "Export complete"));
    }
}
```

ViewModel:

```csharp
public partial class MainViewModel : ObservableObject, IRecipient<ProgressMessage>
{
    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private string _progressStatus = "";

    public MainViewModel(IMessenger messenger)
    {
        messenger.Register<ProgressMessage>(this, (r, m) =>
        {
            // Runs on the sender's thread — must dispatch to UI.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ((MainViewModel)r).ProgressPercent = m.Percent;
                ((MainViewModel)r).ProgressStatus = m.Status;
            });
        });
    }

    void IRecipient<ProgressMessage>.Receive(ProgressMessage message)
    {
        // Not used — using lambda registration above for clarity.
    }
}
```

### 2b. Via progress callback

The service accepts an `IProgress<T>` whose `Progress<T>` handler marshal to the UI thread by constructing it on the UI thread:

```csharp
public sealed class ProcessingService
{
    private readonly ILogger _logger;

    public ProcessingService(ILogger<ProcessingService> logger) => _logger = logger;

    public async Task RunAsync(IProgress<(int Percent, string Status)> progress,
                               CancellationToken ct)
    {
        for (int i = 1; i <= 100; i++)
        {
            await Task.Delay(30, ct);
            progress.Report((i, $"Processing chunk {i}"));
        }
    }
}
```

ViewModel:

```csharp
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ProcessingService _service;

    [ObservableProperty]
    private int _percent;

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private bool _isRunning;

    public MainViewModel(ProcessingService service) => _service = service;

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        IsRunning = true;
        var progress = new Progress<(int, string)>(pair =>
        {
            // Progress<T> captures SynchronizationContext on construction.
            // In Avalonia this is DispatcherSynchronizationContext.
            (Percent, Status) = pair;
        });

        await _service.RunAsync(progress, CancellationToken.None);
        IsRunning = false;
        Status = "Done";
    }

    private bool CanStart() => !IsRunning;
}
```

Because `Progress<T>` captures the current `SynchronizationContext` at construction time, the callback always executes on the UI thread.

## 3. Long-running operations with cancellation

```csharp
[ObservableProperty]
private CancellationTokenSource? _cts;

[RelayCommand]
private async Task StartLongProcessAsync()
{
    _cts = new CancellationTokenSource();
    try
    {
        await _service.RunAsync(Progress, _cts.Token);
    }
    catch (OperationCanceledException)
    {
        Status = "Cancelled";
    }
}

[RelayCommand]
private void Cancel()
{
    _cts?.Cancel();
    _cts = null;
}
```

Wire the cancel button:

```xml
<Button Command="{Binding Cancel}"
        IsEnabled="{Binding IsRunning}">
  Cancel
</Button>
```

## 4. Periodic background refresh (timer-based)

```csharp
public sealed class WeatherRefreshService : BackgroundService
{
    private readonly IMessenger _messenger;
    private readonly WeatherApi _api;

    public WeatherRefreshService(IMessenger messenger, WeatherApi api)
    {
        _messenger = messenger;
        _api = api;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var data = await _api.FetchCurrentAsync(stoppingToken);
            _messenger.Send(new WeatherUpdatedMessage(data));
        }
    }
}
```

`PeriodicTimer` (introduced in .NET 6) is preferable to `Task.Delay` in a loop because it does not drift and does not accumulate when the body takes longer than the interval.

## 5. Queue-based processing (producer / consumer)

```csharp
public sealed class ProcessingQueue
{
    private readonly Channel<string> _channel =
        Channel.CreateBounded<string>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public ChannelWriter<string> Writer => _channel.Writer;
    public ChannelReader<string> Reader => _channel.Reader;
}
```

```csharp
public sealed class QueueProcessor : BackgroundService
{
    private readonly ProcessingQueue _queue;
    private readonly IMessenger _messenger;

    public QueueProcessor(ProcessingQueue queue, IMessenger messenger)
    {
        _queue = queue;
        _messenger = messenger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            // Process item...
            _messenger.Send(new ItemProcessedMessage(item));
        }
    }
}
```

Registration:

```csharp
builder.Services.AddSingleton<ProcessingQueue>();
builder.Services.AddHostedService<QueueProcessor>();
```

## 6. Dispatcher considerations

| Pattern | UI thread safety | Best for |
|---|---|---|
| `IProgress<T>` + `Progress<T>` | Yes (via captured context) | One-shot background work |
| `IMessenger.Send` | No (caller dispatches) | Hosted services, multi-consumer |
| `Dispatcher.UIThread.Post` / `Invoke` | Yes (explicit) | Any code that has a dispatcher reference |
| `AsyncRelayCommand` | Yes (command execution) | Short-to-medium ViewModel-triggered work |

When using `IMessenger` from a background thread, the receiver must dispatch to the UI thread before updating observable properties or binding sources.

## Key takeaways

- `BackgroundService` / `ExecuteAsync` runs entirely on a background thread — never touch UI directly
- Use `IMessenger` to notify the UI layer from a hosted service; the receiver dispatches
- Use `IProgress<T>` / `Progress<T>` for operation-scoped progress; it marshals automatically
- `PeriodicTimer` is the correct tool for fixed-interval background loops
- Register services with `AddHostedService<T>()` on the DI container
- Always forward cancellation tokens; wrap cancellable operations in `try/catch OperationCanceledException`

---

## See Also

- [032 -- Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [008 -- RelayCommand](../basics/008-relay-command.md)
- [.NET docs: BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/background-service)
- [044V -- Background Services and Progress Reporting (verbose companion)](044-background-services-and-progress-verbose.md)
- [044X -- Background Services and Progress Reporting (examples)](044-background-services-and-progress-examples.md)
