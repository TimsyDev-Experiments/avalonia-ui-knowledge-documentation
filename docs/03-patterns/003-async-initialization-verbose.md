---
tier: intermediate
topic: architecture
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 003-async-initialization.md
---

# 003V — Async Initialization Patterns: An In-Depth Companion

You should already have read: [003 — Async Initialization Patterns](003-async-initialization.md) for the quick-start version. This file goes deeper on every section.

---

## Prerequisites

- [003 — Async Initialization Patterns](003-async-initialization.md)
- [037 — App Lifetimes and Splash Screen](../02-tutorials/advanced/037-app-lifetimes-splash-screen.md)
- Familiarity with `CancellationToken`, `IAsyncEnumerable<T>`, and `Task`

---

## 1. Why Async Initialization Is Hard

Desktop applications face a tension that web applications do not: the user sees a window immediately. Every millisecond between the window appearing and the UI becoming responsive is a negative experience.

### The Blocking-UI-Thread Problem

```csharp
// BAD — blocks the UI thread
public override void OnFrameworkInitializationCompleted()
{
    Thread.Sleep(2000); // Freezes the window
    LoadConfig(); // Synchronous I/O blocks the UI thread
}
```

The OS detects that the UI thread is not processing messages. After a few seconds, Windows paints a "Not Responding" overlay. On macOS, the spinning beach ball appears.

### The Fire-and-Forget Problem

```csharp
// BAD — crashes on exception
public override void OnFrameworkInitializationCompleted()
{
    _ = LoadConfigAsync(); // Fire and forget — exceptions crash the process
    ShowMainWindow();
}
```

Unobserved exceptions from fire-and-forget tasks that run on the synchronization context can crash the application silently.

### The Race-Condition Problem

```csharp
// BAD — race condition between init and user interaction
public partial class MainViewModel
{
    // User clicks a button before _data is populated
    private List<Config> _data;

    public async Task InitAsync()
    {
        _data = await FetchConfigAsync();
    }

    public void ProcessItem(int id)
    {
        var item = _data.First(x => x.Id == id); // NullReferenceException if init not done
    }
}
```

---

## 2. Splash Screen — Deep Dive

### 2.1 Thread Affinity

The splash window must be created on the UI thread. Any `await` in the initialization sequence must capture the UI synchronization context so the progress properties can update the bound UI.

```csharp
public async Task InitializeAsync()
{
    // Runs on UI thread — splash window is bound to this ViewModel
    Status = "Loading configuration...";
    Progress = 0.1;

    // await does not block the UI thread
    // Continuation captures SynchronizationContext by default
    await Task.Delay(300);

    // This line runs back on the UI thread — safe to update Status
    Status = "Connecting to service...";
    Progress = 0.4;
}
```

If you offload CPU-bound work to a background thread, marshal progress back:

```csharp
public async Task InitializeAsync()
{
    Status = "Processing data...";
    Progress = 0.3;

    var result = await Task.Run(() =>
    {
        // CPU-bound work on thread pool
        for (int i = 0; i < 100; i++)
        {
            Thread.Sleep(50);
            // Cannot update UI properties here — not on UI thread
        }
        return ComputeResult();
    });

    // Back on UI thread — safe to update
    Status = "Processing complete";
    Progress = 0.6;
}
```

### 2.2 Progress Reporting via IProgress<T>

For cleaner separation, use `IProgress<T>`:

```csharp
public class SplashViewModel : ObservableObject
{
    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private double _progress;

    public async Task InitializeAsync(IProgress<(string Status, double Progress)> progress)
    {
        progress.Report(("Loading configuration...", 0.1));
        await Task.Delay(300);

        progress.Report(("Connecting to service...", 0.4));
        await AuthenticateAsync();

        progress.Report(("Preparing cache...", 0.8));
        await PrepareCacheAsync();

        progress.Report(("Ready", 1.0));
    }

    private async Task AuthenticateAsync()
    {
        // Real auth call
        await Task.Delay(800);
    }

    private async Task PrepareCacheAsync()
    {
        await Task.Delay(500);
    }
}
```

App.axaml.cs wires the progress reporter:

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splash = new SplashWindow();
        var splashVm = new SplashViewModel();
        splash.DataContext = splashVm;
        splash.Show();

        var progress = new Progress<(string, double)>(update =>
        {
            splashVm.Status = update.Item1;
            splashVm.Progress = update.Item2;
        });

        await splashVm.InitializeAsync(progress);

        desktop.MainWindow = new MainWindow
        {
            DataContext = AppHost.Services.GetRequiredService<MainViewModel>()
        };
        splash.Close();
    }
    base.OnFrameworkInitializationCompleted();
}
```

### 2.3 Cancelling the Splash

Add cancellation support so the splash can be aborted:

```csharp
public partial class SplashViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private double _progress;

    public async Task InitializeAsync(CancellationToken ct)
    {
        try
        {
            Status = "Loading configuration...";
            Progress = 0.1;
            await Task.Delay(300, ct);

            Status = "Connecting to service...";
            Progress = 0.4;
            await AuthenticateAsync(ct);

            Status = "Preparing cache...";
            Progress = 0.8;
            await PrepareCacheAsync(ct);

            Status = "Ready";
            Progress = 1.0;
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}
```

---

## 3. Lazy Loading — Deep Dive

### 3.1 Guard Pattern

The core file uses a simple `_loaded` guard. For thread safety in async scenarios:

```csharp
public partial class LazyViewModel : ObservableObject
{
    private readonly IDataService _data;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool _loaded;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<HeavyItem>? _items;

    public LazyViewModel(IDataService data)
    {
        _data = data;
    }

    [RelayCommand]
    private async Task LoadHeavyFeatureAsync()
    {
        if (_loaded) return;

        // Prevent concurrent loads even if user clicks rapidly
        await _loadLock.WaitAsync();
        try
        {
            if (_loaded) return; // Double-check after acquiring lock
            IsLoading = true;
            var result = await _data.FetchExpensiveDataAsync();
            Items = new ObservableCollection<HeavyItem>(result);
            _loaded = true;
        }
        finally
        {
            IsLoading = false;
            _loadLock.Release();
        }
    }
}
```

### 3.2 Lazy Loading with Prefetch Hint

Prefetch data in the background while showing the shell, then cache it so lazy loading completes instantly:

```csharp
public class PrefetchService
{
    private readonly IDataService _data;
    private Task<List<HeavyItem>>? _prefetchTask;

    public PrefetchService(IDataService data)
    {
        _data = data;
    }

    public void StartPrefetch()
    {
        // Fire background task immediately at startup
        _prefetchTask = _data.FetchExpensiveDataAsync();
    }

    public async Task<List<HeavyItem>> GetPrefetchedAsync()
    {
        if (_prefetchTask is null)
            return await _data.FetchExpensiveDataAsync();

        return await _prefetchTask;
    }
}
```

Wiring at startup:

```csharp
var prefetch = new PrefetchService(dataService);
prefetch.StartPrefetch(); // Starts immediately, shell shows first

// Later, in the lazy ViewModel:
var data = await _prefetch.GetPrefetchedAsync();
// Returns almost instantly because data is already being fetched
```

---

## 4. Background Data Prep — Deep Dive

### 4.1 Streaming with IAsyncEnumerable

The core file shows `StreamBatchesAsync`. Here is a complete implementation:

```csharp
public class DataService : IDataService
{
    public async IAsyncEnumerable<List<FeedItem>> StreamBatchesAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var page = 0;
        const int pageSize = 50;

        while (!ct.IsCancellationRequested)
        {
            var batch = await FetchPageAsync(page++, pageSize, ct);

            if (batch.Count == 0)
                yield break;

            yield return batch; // Stream each batch to the UI
        }
    }

    private async Task<List<FeedItem>> FetchPageAsync(
        int page, int pageSize, CancellationToken ct)
    {
        // Simulated API call with pagination
        await Task.Delay(200, ct);
        return Enumerable.Range(page * pageSize, pageSize)
            .Select(i => new FeedItem { Id = i, Text = $"Item {i}" })
            .ToList();
    }
}
```

### 4.2 Batching and Merging for UI Performance

When the UI runs `await foreach`, it processes each batch on the UI thread. If the batches are too small, the UI thread spends too much time updating the collection. Batch merging:

```csharp
public partial class StreamViewModel : ObservableObject
{
    private readonly IDataService _data;
    private const int VisualBatchSize = 200; // Update UI every 200 items

    [ObservableProperty]
    private ObservableCollection<FeedItem> _items = new();

    [ObservableProperty]
    private string _status = "Waiting...";

    [RelayCommand]
    private async Task StartStreamAsync(CancellationToken ct)
    {
        Status = "Connecting...";
        var buffer = new List<FeedItem>();

        await foreach (var batch in _data.StreamBatchesAsync(ct))
        {
            buffer.AddRange(batch);

            // Only update UI when we have enough items
            if (buffer.Count >= VisualBatchSize)
            {
                foreach (var item in buffer)
                    Items.Add(item);

                Status = $"Loaded {Items.Count} items";
                buffer.Clear();
            }
        }

        // Flush remaining items
        if (buffer.Count > 0)
        {
            foreach (var item in buffer)
                Items.Add(item);
        }

        Status = Items.Count > 0
            ? $"Complete — {Items.Count} items"
            : "No data";
    }
}
```

---

## 5. IAsyncInitialization — Deep Dive

### 5.1 Proper Exception Handling

The core file shows a simple fire-and-forget pattern. For production, add exception handling:

```csharp
public interface IAsyncInitialization
{
    Task InitializeAsync { get; }
}

public partial class InitViewModel : ObservableObject, IAsyncInitialization
{
    public Task InitializeAsync { get; }

    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    public InitViewModel(IDataService data)
    {
        InitializeAsync = InitializeImplAsync(data);
    }

    private async Task InitializeImplAsync(IDataService data)
    {
        try
        {
            Status = "Loading...";
            await data.LoadAsync();
            Status = "Ready";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            Status = "Error";
        }
    }
}
```

### 5.2 Awaiting in the Composition Root

Instead of fire-and-forget, the composition root can await the initialization before showing the window:

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm = AppHost.Services.GetRequiredService<InitViewModel>();

        // Show a loading window while initialising
        var splash = new MiniSplash { DataContext = vm };
        splash.Show();

        try
        {
            await vm.InitializeAsync;
        }
        catch
        {
            // Log and show error window instead
        }

        desktop.MainWindow = new MainWindow { DataContext = vm };
        splash.Close();
    }
    base.OnFrameworkInitializationCompleted();
}
```

### 5.3 Composite Initialization for Multiple ViewModels

When multiple ViewModels need initialization, compose them:

```csharp
public class CompositeInitializer : IAsyncInitialization
{
    private readonly IReadOnlyList<IAsyncInitialization> _initList;

    public Task InitializeAsync { get; }

    public CompositeInitializer(IEnumerable<IAsyncInitialization> initList)
    {
        _initList = initList.ToList();
        InitializeAsync = InitializeAllAsync();
    }

    private async Task InitializeAllAsync()
    {
        foreach (var init in _initList)
        {
            await init.InitializeAsync;
        }
    }
}
```

---

## 6. Choosing the Right Pattern

| Factor | Splash Screen | Lazy Loading | Background Prep | IAsyncInitialization |
|--------|---------------|--------------|-----------------|----------------------|
| Startup time before UI | 1-10s (blocked) | Instant | Instant | Configurable |
| User perception | "App is loading" | "App is fast" | "App is already useful" | Depends on composition |
| Complexity | Medium | Low | Medium | Low |
| Progress feedback | Yes (0-100%) | Per-feature spinner | Counters/stats | Manual |
| Best for | Auth, config downloads | Heavy reports, admin panels | Feeds, search indexes | Standardising init |
| Cancellation | Yes | N/A | Yes | Yes |

---

## 7. Key Takeaways (Expanded)

- **Never block the UI thread at startup.** Use `await`, fire-and-forget with careful exception handling, or fire-and-forget with `IAsyncInitialization`.
- **Splash screens** provide branded progress feedback for initialization sequences that take 1-10 seconds. Use `IProgress<T>` for clean separation.
- **Lazy loading** keeps the shell fast. Show spinners on first feature access. Use `SemaphoreSlim` for concurrent-access safety.
- **Background data prep** streams results in while the app is already usable. Batch small chunks into larger UI updates to avoid overwhelming the dispatcher.
- **`IAsyncInitialization`** standardises async init across ViewModels. It supports composition for multi-ViewModel apps and composable error handling.
- **Always support `CancellationToken`** in initialization code so the user or the system can abort long-running startup operations.

---

## See Also

- [003 — Async Initialization Patterns](003-async-initialization.md) (core file)
- [037 — App Lifetimes and Splash Screen](../02-tutorials/advanced/037-app-lifetimes-splash-screen.md)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Avalonia Docs: Async Patterns](https://docs.avaloniaui.net/docs/concepts/async)
