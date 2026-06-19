---
tier: intermediate
topic: architecture
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 003-async-initialization.md
---

# 003X — Async Initialization Patterns: Real-World Examples

You should already have read: [003 — Async Initialization Patterns](003-async-initialization.md) for the core concepts. This file provides complete, worked examples.

---

## Example 1: Splash Screen with Real Progress (Auth + Config + Cache)

**What you'll learn:** How to build a splash screen that authenticates with a remote service, loads configuration, and hydrates a local cache — reporting real (not simulated) progress at each step.

### SplashViewModel

```csharp
public partial class SplashViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly IConfigService _config;
    private readonly ICacheService _cache;

    [ObservableProperty]
    private string _status = "Initializing...";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string? _errorMessage;

    public SplashViewModel(
        IAuthService auth,
        IConfigService config,
        ICacheService cache)
    {
        _auth = auth;
        _config = config;
        _cache = cache;
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            Status = "Checking license...";
            Progress = 0.1;
            await _auth.ValidateLicenseAsync();

            Status = "Loading configuration...";
            Progress = 0.3;
            var config = await _config.LoadAsync();
            ApplyConfiguration(config);

            Status = "Authenticating user...";
            Progress = 0.5;
            await _auth.AuthenticateAsync();

            Status = "Preparing offline cache...";
            Progress = 0.7;
            await _cache.WarmUpAsync();

            Status = "Finalizing...";
            Progress = 0.9;
            await Task.Delay(200);

            Status = "Ready";
            Progress = 1.0;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Status = "Failed";
            return false;
        }
    }

    private void ApplyConfiguration(Config config)
    {
        // Apply loaded configuration
    }
}
```

### App.axaml.cs

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splash = new SplashWindow();
        var splashVm = AppHost.Services.GetRequiredService<SplashViewModel>();
        splash.DataContext = splashVm;
        splash.Show();

        var success = await splashVm.InitializeAsync();

        if (success)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = AppHost.Services.GetRequiredService<MainViewModel>()
            };
            splash.Close();
        }
        else
        {
            splashVm.Status = "Initialization failed — check logs";
        }
    }

    base.OnFrameworkInitializationCompleted();
}
```

### What Makes This Real

- Progress values are grounded — each step corresponds to a meaningful unit of work
- Failure is handled gracefully — the splash shows an error instead of crashing
- Dependencies are injected, making the splash ViewModel testable

---

## Example 2: Lazy-Loaded Admin Panel with Concurrent-Access Safety

**What you'll learn:** How to implement a lazy-loaded feature that initialises only on first access, prevents double-loading, and handles rapid user clicks safely.

### Scenario

The admin panel is expensive to initialise (loads user list, permissions, audit log). Most users never open it, so it loads lazily.

### AdminViewModel

```csharp
public partial class AdminViewModel : ObservableObject
{
    private readonly IAdminDataService _data;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _loaded;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingStatus = "";

    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private ObservableCollection<AuditEntry> _auditLog = new();

    public AdminViewModel(IAdminDataService data)
    {
        _data = data;
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        await _lock.WaitAsync();
        try
        {
            if (_loaded) return;

            IsLoading = true;
            LoadingStatus = "Loading users...";
            var users = await _data.GetUsersAsync();
            Users = new ObservableCollection<User>(users);

            LoadingStatus = "Loading audit log...";
            var audit = await _data.GetAuditLogAsync();
            AuditLog = new ObservableCollection<AuditEntry>(audit);

            _loaded = true;
            LoadingStatus = $"Loaded {Users.Count} users, {AuditLog.Count} entries";
        }
        finally
        {
            IsLoading = false;
            _lock.Release();
        }
    }
}
```

### Shell ViewModel (Triggering Load)

```csharp
public partial class ShellViewModel : ObservableObject
{
    private readonly AdminViewModel _adminVm;

    [ObservableProperty]
    private object? _currentView;

    public ShellViewModel(AdminViewModel adminVm)
    {
        _adminVm = adminVm;
    }

    [RelayCommand]
    private async Task NavigateToAdminAsync()
    {
        if (CurrentView is not AdminViewModel)
        {
            await _adminVm.EnsureLoadedAsync();
            CurrentView = _adminVm;
        }
    }
}
```

### Admin View with Loading Overlay

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="DemoApp.Views.AdminView">
  <Grid>
    <!-- Main content -->
    <TabControl IsVisible="{Binding IsLoading, Converter={StaticResource InverseBool}}">
      <TabItem Header="Users">
        <ListBox ItemsSource="{Binding Users}" />
      </TabItem>
      <TabItem Header="Audit Log">
        <ListBox ItemsSource="{Binding AuditLog}" />
      </TabItem>
    </TabControl>

    <!-- Loading overlay -->
    <Border IsVisible="{Binding IsLoading}"
            Background="#80FFFFFF"
            VerticalAlignment="Stretch"
            HorizontalAlignment="Stretch">
      <StackPanel VerticalAlignment="Center"
                  HorizontalAlignment="Center"
                  Spacing="8">
        <ProgressBar IsIndeterminate="True"
                     Width="200" Height="20" />
        <TextBlock Text="{Binding LoadingStatus}"
                   HorizontalAlignment="Center" />
      </StackPanel>
    </Border>
  </Grid>
</UserControl>
```

---

## Example 3: Background Data Stream with Batch UI Updates

**What you'll learn:** How to stream large datasets into the UI in batches, keeping the app responsive and showing live progress.

### Scenario

A log viewer app connects to a remote service that streams log entries. The user sees entries appear in real-time while the app remains usable.

### LogStreamViewModel

```csharp
public partial class LogStreamViewModel : ObservableObject
{
    private readonly ILogStreamService _logService;
    private CancellationTokenSource? _cts;
    private const int UiBatchSize = 100;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _entries = new();

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private int _totalEntries;

    [RelayCommand]
    private async Task StartStreamAsync()
    {
        CancelStream(); // Cancel any existing stream
        _cts = new CancellationTokenSource();
        IsStreaming = true;
        Status = "Connecting...";

        var buffer = new List<LogEntry>();

        try
        {
            await foreach (var batch in _logService.StreamLogsAsync(_cts.Token))
            {
                buffer.AddRange(batch);

                if (buffer.Count >= UiBatchSize)
                {
                    foreach (var entry in buffer)
                        Entries.Add(entry);

                    TotalEntries = Entries.Count;
                    Status = $"{TotalEntries} entries loaded";
                    buffer.Clear();
                }
            }

            // Flush remaining
            foreach (var entry in buffer)
                Entries.Add(entry);

            TotalEntries = Entries.Count;
            Status = $"Complete — {TotalEntries} entries";
        }
        catch (OperationCanceledException)
        {
            Status = "Stream stopped";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
        }
        finally
        {
            IsStreaming = false;
        }
    }

    [RelayCommand]
    private void CancelStream()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
```

### Log Stream Service

```csharp
public interface ILogStreamService
{
    IAsyncEnumerable<List<LogEntry>> StreamLogsAsync(CancellationToken ct);
}

public class LogStreamService : ILogStreamService
{
    private readonly IHttpClientFactory _http;

    public LogStreamService(IHttpClientFactory http)
    {
        _http = http;
    }

    public async IAsyncEnumerable<List<LogEntry>> StreamLogsAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        var client = _http.CreateClient();
        var response = await client.GetAsync("https://api.example.com/logs/stream",
            HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var batch = new List<LogEntry>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;

            var entry = JsonSerializer.Deserialize<LogEntry>(line);
            if (entry is null) continue;

            batch.Add(entry);

            if (batch.Count >= 50)
            {
                yield return batch;
                batch = new List<LogEntry>();
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }
}
```

### View with Log Entries and Controls

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="DemoApp.Views.LogViewerView">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <Button Content="Start Stream"
              Command="{Binding StartStreamCommand}"
              IsEnabled="{Binding IsStreaming, Converter={StaticResource InverseBool}}" />
      <Button Content="Stop"
              Command="{Binding CancelStreamCommand}"
              IsEnabled="{Binding IsStreaming}" />
      <TextBlock Text="{Binding Status}"
                 VerticalAlignment="Center" />
      <TextBlock Text="{Binding TotalEntries, StringFormat='Total: {0}'}"
                 VerticalAlignment="Center" />
    </StackPanel>

    <ListBox ItemsSource="{Binding Entries}"
             VirtualizationMode="Recycling"
             Margin="8">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="models:LogEntry">
          <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock Text="{Binding Timestamp, StringFormat='{0:HH:mm:ss}'}" />
            <TextBlock Text="{Binding Level}" FontWeight="Bold" />
            <TextBlock Text="{Binding Message}" TextWrapping="Wrap" />
          </StackPanel>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
</UserControl>
```

---

## Example 4: IAsyncInitialization with Composite Initialization

**What you'll learn:** How to standardise async initialization across all ViewModels using `IAsyncInitialization` and compose them for a coordinated startup.

### Interface and Base Pattern

```csharp
public interface IAsyncInitialization
{
    Task InitializeAsync { get; }
    bool IsInitialized { get; }
}

public abstract class AsyncViewModelBase : ObservableObject, IAsyncInitialization
{
    public Task InitializeAsync { get; }

    [ObservableProperty]
    private bool _isInitialized;

    [ObservableProperty]
    private string _status = "Pending";

    protected AsyncViewModelBase()
    {
        InitializeAsync = InitializeImplAsync();
    }

    protected abstract Task InitializeImplAsync();

    protected async Task ExecuteInitAsync(Func<Task> initFunc)
    {
        try
        {
            Status = "Initializing...";
            await initFunc();
            IsInitialized = true;
            Status = "Ready";
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.Message}";
            throw;
        }
    }
}
```

### Concrete ViewModel

```csharp
public class DashboardViewModel : AsyncViewModelBase
{
    private readonly ISalesDataService _sales;

    [ObservableProperty]
    private ObservableCollection<SalesSummary> _summary = new();

    public DashboardViewModel(ISalesDataService sales)
    {
        _sales = sales;
    }

    protected override async Task InitializeImplAsync()
    {
        await ExecuteInitAsync(async () =>
        {
            var data = await _sales.GetDailySummaryAsync();
            foreach (var item in data)
                Summary.Add(item);
        });
    }
}
```

### Composite Initializer for Coordinated Startup

```csharp
public class AppInitializer : IAsyncInitialization
{
    private readonly IReadOnlyList<IAsyncInitialization> _viewModels;

    public Task InitializeAsync { get; }

    [ObservableProperty]
    private string _overallStatus = "Starting...";

    public AppInitializer(IEnumerable<IAsyncInitialization> viewModels)
    {
        _viewModels = viewModels.ToList();
        InitializeAsync = InitializeAllAsync();
    }

    private async Task InitializeAllAsync()
    {
        var total = _viewModels.Count;
        var completed = 0;

        foreach (var vm in _viewModels)
        {
            await vm.InitializeAsync;
            completed++;
            OverallStatus = $"Initialized {completed} of {total} modules";
        }

        OverallStatus = "All modules ready";
    }
}
```

### DI Registration

```csharp
services.AddTransient<DashboardViewModel>();
services.AddTransient<ReportsViewModel>();
services.AddTransient<SettingsViewModel>();

// Register all async ViewModels as IAsyncInitialization
services.AddTransient<IAsyncInitialization, DashboardViewModel>();
services.AddTransient<IAsyncInitialization, ReportsViewModel>();
services.AddTransient<IAsyncInitialization, SettingsViewModel>();

services.AddTransient<AppInitializer>();
```

### App.axaml.cs with Composite Init

```csharp
public override async void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var splash = new SplashWindow();
        var initializer = AppHost.Services.GetRequiredService<AppInitializer>();
        splash.DataContext = initializer;
        splash.Show();

        await initializer.InitializeAsync;

        desktop.MainWindow = new MainWindow
        {
            DataContext = AppHost.Services.GetRequiredService<ShellViewModel>()
        };
        splash.Close();
    }

    base.OnFrameworkInitializationCompleted();
}
```

---

## Key Takeaways

- **Splash screens** with real progress (not simulated) give users accurate feedback about startup steps. Inject services into the splash ViewModel for testability.
- **Lazy loading** with `SemaphoreSlim` guards against concurrent access. Double-check the `_loaded` flag after acquiring the lock to handle race conditions.
- **Background streaming** with batching keeps the UI responsive. Collect items into a buffer and flush to the UI collection in chunks of 50-200 items.
- **IAsyncInitialization with composition** standardises init across ViewModels. A composite initializer can coordinate startup and report overall progress.
- All patterns support `CancellationToken` for graceful cancellation and error handling.

---

## See Also

- [003 — Async Initialization Patterns](003-async-initialization.md)
- [003V — Async Initialization Patterns: In-Depth Companion](003-async-initialization-verbose.md)
- [037 — App Lifetimes and Splash Screen](../02-tutorials/advanced/037-app-lifetimes-splash-screen.md)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
