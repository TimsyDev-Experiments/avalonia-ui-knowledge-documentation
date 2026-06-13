---
tier: intermediate
topic: architecture
estimated: 10 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Pattern: Async Initialization Patterns

**What you'll learn:** Strategies for initializing application data asynchronously at startup, including splash screens, lazy loading, and background data prep with progress feedback.

**Prerequisites:** [037 -- App Lifetimes and Splash Screen](../02-tutorials/advanced/037-app-lifetimes-splash-screen.md)

---

## Problem

A desktop application needs to load configuration, authenticate with a remote service, or hydrate a local cache before the user can interact with the UI. Blocking the UI thread during startup creates a bad first impression (frozen window, "not responding" state). Running work in the background while keeping the user informed requires a structured pattern.

---

## Solution Patterns

Choose based on startup complexity:

| Pattern | When to use | User experience |
|---------|------------|----------------|
| Splash screen | Loading takes 1-10s | Branded window with progress |
| Lazy loading | Loading is per-feature | Fast initial shell, spinner on first navigation |
| Background prep | Loading is continuous | UI is usable immediately, data streams in |

---

## Pattern 1: Splash Screen with Progress

Show a splash window that reports incremental progress, then opens the main window.

### Splash ViewModel

```csharp
public partial class SplashViewModel : ObservableObject
{
    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private double _progress;

    public async Task InitializeAsync()
    {
        Status = "Loading configuration...";
        Progress = 0.1;
        await Task.Delay(300); // Simulate config load

        Status = "Connecting to service...";
        Progress = 0.4;
        await AuthenticateAsync();

        Status = "Preparing data cache...";
        Progress = 0.8;
        await PrepareCacheAsync();

        Status = "Ready";
        Progress = 1.0;
        await Task.Delay(200); // Brief pause
    }

    private async Task AuthenticateAsync()
    {
        // Real auth call
        await Task.Delay(800);
    }

    private async Task PrepareCacheAsync()
    {
        // Real cache prep
        await Task.Delay(500);
    }
}
```

### App.axaml.cs wiring

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splash = new SplashWindow();
        var splashVm = new SplashViewModel();
        splash.DataContext = splashVm;
        splash.Show();

        await splashVm.InitializeAsync();

        desktop.MainWindow = new MainWindow
        {
            DataContext = Program.AppHost.Services
                .GetRequiredService<MainViewModel>()
        };
        splash.Close();
    }
    base.OnFrameworkInitializationCompleted();
}
```

---

## Pattern 2: Lazy Feature Loading

Start the shell immediately and load expensive features on demand.

```csharp
public partial class LazyViewModel : ObservableObject
{
    private readonly IDataService _data;
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

        IsLoading = true;
        try
        {
            var result = await _data.FetchExpensiveDataAsync();
            Items = new ObservableCollection<HeavyItem>(result);
            _loaded = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

View binding:

```xml
<ContentControl Content="{Binding CurrentView}">
  <ContentControl.ContentTemplate>
    <DataTemplate x:DataType="viewModels:LazyViewModel">
      <Grid>
        <ListBox ItemsSource="{Binding Items}"
                 IsVisible="{Binding IsLoading, Converter={StaticResource InverseBool}}" />
        <Border IsVisible="{Binding IsLoading}"
                Background="#80FFFFFF">
          <ProgressBar IsIndeterminate="True"
                       Width="200" Height="20" />
        </Border>
      </Grid>
    </DataTemplate>
  </ContentControl.ContentTemplate>
</ContentControl>
```

---

## Pattern 3: Background Data Prep

Fire a background task at startup that streams data in as it becomes available, keeping the UI responsive throughout.

```csharp
public partial class StreamViewModel : ObservableObject
{
    private readonly IDataService _data;

    [ObservableProperty]
    private ObservableCollection<FeedItem> _items = new();

    [ObservableProperty]
    private string _status = "Waiting...";

    public StreamViewModel(IDataService data)
    {
        _data = data;
    }

    [RelayCommand]
    private async Task StartStreamAsync(CancellationToken ct)
    {
        Status = "Connecting...";

        await foreach (var batch in _data.StreamBatchesAsync(ct))
        {
            foreach (var item in batch)
                Items.Add(item);

            Status = $"Loaded {Items.Count} items";
        }

        Status = Items.Count > 0 ? $"Complete — {Items.Count} items" : "No data";
    }
}
```

---

## Pattern 4: IAsyncInitialization Interface

Standardize async init across ViewModels with a reusable interface:

```csharp
public interface IAsyncInitialization
{
    Task InitializeAsync { get; }
}

public partial class InitViewModel : ObservableObject, IAsyncInitialization
{
    public Task InitializeAsync { get; }

    public InitViewModel()
    {
        InitializeAsync = InitializeImplAsync();
    }

    private async Task InitializeImplAsync()
    {
        // Real async work
        await Task.Delay(500);
    }
}
```

Composition root awaits all ViewModels:

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var vm = new InitViewModel();

        // Fire and forget — UI loads, data arrives later
        _ = vm.InitializeAsync.ContinueWith(_ => { }, TaskScheduler.FromCurrentSynchronizationContext());

        desktop.MainWindow = new MainWindow { DataContext = vm };
    }
    base.OnFrameworkInitializationCompleted();
}
```

---

## Key Takeaways

- Never block the UI thread at startup — use `await` or fire-and-forget patterns
- Splash screens report progress (0-100%) for 1-10s initialization sequences
- Lazy loading keeps the shell fast and shows spinners on first feature access
- Background data prep streams results in as the app is already usable
- `IAsyncInitialization` standardizes the pattern across ViewModels
- Use `CancellationToken` for cancellable initialization callbacks

---

## See Also

- [037 -- App Lifetimes and Splash Screen](../02-tutorials/advanced/037-app-lifetimes-splash-screen.md)
- [032 -- MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [038 -- Headless Testing](../02-tutorials/advanced/038-headless-testing.md)
- [Avalonia Docs: Async Patterns](https://docs.avaloniaui.net/docs/concepts/async)
