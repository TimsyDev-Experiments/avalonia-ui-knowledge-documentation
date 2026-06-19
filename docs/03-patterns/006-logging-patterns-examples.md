---
tier: reference
topic: observability
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 006-logging-patterns.md
---

# 006X — Logging Patterns: Real-World Examples

## Example 1: Full Serilog Setup for an Avalonia Desktop App

A complete startup configuration with console, file, and Seq sinks.

### Program.cs

```csharp
public static class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .Enrich.WithThreadId()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "MyAvaloniaApp")
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024)
            .WriteTo.Seq("http://localhost:5341",
                apiKey: "optional-api-key")
            .CreateLogger();

        try
        {
            Log.Information("Application starting");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
```

### App.axaml.cs with DI logging

```csharp
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var collection = new ServiceCollection();
            collection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });
            // Register services and ViewModels...
            var services = collection.BuildServiceProvider();
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

### Key points

- Four sinks: Console (dev), File (retention 14 days, 10 MB per file), Seq (structured search)
- Namespace overrides filter out EF Core noise
- `Log.CloseAndFlush()` ensures buffered events are written before process exit
- `ILoggingBuilder.AddSerilog` bridges Serilog with the `Microsoft.Extensions.Logging` abstractions

---

## Example 2: Structured Logging with Scoped Context in a ViewModel

A file import operation that logs with contextual properties.

### ViewModel

```csharp
public partial class FileImportViewModel : ObservableObject
{
    private readonly ILogger<FileImportViewModel> _logger;

    [ObservableProperty]
    private string? _importPath;

    [ObservableProperty]
    private int _progress;

    public FileImportViewModel(ILogger<FileImportViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var importId = Guid.NewGuid();
        var path = ImportPath;

        _logger.LogInformation(
            "Import started {ImportId} from {Path}",
            importId, path);

        using (LogContext.PushProperty("ImportId", importId))
        using (LogContext.PushProperty("FilePath", path))
        {
            try
            {
                var totalLines = await CountLinesAsync(path);
                _logger.LogInformation(
                    "File contains {LineCount} lines", totalLines);

                int imported = 0;
                await foreach (var line in ReadLinesAsync(path))
                {
                    await ProcessLineAsync(line);
                    imported++;
                    Progress = (int)((double)imported / totalLines * 100);
                }

                _logger.LogInformation(
                    "Import complete: {ImportedCount} of {LineCount} lines",
                    imported, totalLines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Import failed after {Progress}%", Progress);
            }
        }
    }

    private async Task<int> CountLinesAsync(string path)
    {
        _logger.LogDebug("Counting lines in {Path}", path);
        using var reader = File.OpenText(path);
        int count = 0;
        while (await reader.ReadLineAsync() is not null) count++;
        return count;
    }

    private async IAsyncEnumerable<string> ReadLinesAsync(string path)
    {
        using var reader = File.OpenText(path);
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            yield return line;
        }
    }

    private Task ProcessLineAsync(string line)
    {
        _logger.LogTrace("Processing line: {LinePreview}",
            line.Length > 50 ? line[..50] + "..." : line);
        return Task.CompletedTask;
    }
}
```

### Sample output in Seq

| Timestamp | Level | Message | ImportId | FilePath | LineCount |
|---|---|---|---|---|---|
| 12:00:01 | Information | Import started ... | a1b2... | /data/file.csv | |
| 12:00:02 | Information | File contains 1000 lines | a1b2... | /data/file.csv | 1000 |
| 12:00:05 | Trace | Processing line: "2024-01-..." | a1b2... | /data/file.csv | |
| 12:00:15 | Information | Import complete: 1000 of 1000 | a1b2... | /data/file.csv | |

### Key points

- `LogContext.PushProperty` scopes `ImportId` and `FilePath` to all log calls in the block
- Structured properties enable per-import search in Seq: `ImportId = "a1b2-..."`
- `Trace` logs detailed line processing without cluttering Information-level output
- Exception is passed as the first argument to `LogError`, capturing stack trace and inner exceptions

---

## Example 3: In-App Log Viewer with Search and Filter

A debug diagnostic panel that displays live log entries with level filtering.

### Log sink

```csharp
public sealed class InMemoryLogSink : ILogEventSink
{
    private readonly Channel<LogEntry> _channel =
        Channel.CreateBounded<LogEntry>(
            new BoundedChannelOptions(200)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public InMemoryLogSink()
    {
        _ = DispatchLoopAsync();
    }

    public void Emit(LogEvent logEvent)
    {
        _channel.Writer.TryWrite(new LogEntry
        {
            Timestamp = logEvent.Timestamp.LocalDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
            Properties = logEvent.Properties.ToDictionary(
                k => k.Key, v => v.Value.ToString())
        });
    }

    private async Task DispatchLoopAsync()
    {
        await foreach (var entry in _channel.Reader.ReadAllAsync())
        {
            Dispatcher.UIThread.Post(() =>
            {
                Entries.Add(entry);
                while (Entries.Count > 1000) Entries.RemoveAt(0);
            });
        }
    }
}

public sealed record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
    public string? Exception { get; init; }
    public Dictionary<string, string>? Properties { get; init; }
}
```

### Log viewer control ViewModel

```csharp
public partial class LogViewerViewModel : ObservableObject
{
    private readonly InMemoryLogSink _sink;

    [ObservableProperty]
    private string? _levelFilter;

    [ObservableProperty]
    private string? _searchText;

    public List<LogEntry> FilteredEntries =>
        _sink.Entries.Where(e =>
                (LevelFilter is null || e.Level == LevelFilter) &&
                (SearchText is null ||
                 e.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

    public LogViewerViewModel(InMemoryLogSink sink)
    {
        _sink = sink;
        _sink.Entries.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(FilteredEntries));
        };
    }

    [RelayCommand]
    private void Clear() => _sink.Entries.Clear();
}
```

### Log viewer XAML

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:MyApp.ViewModels">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8"
                Margin="8">
      <ComboBox ItemsSource="{Binding Levels}"
                SelectedItem="{Binding LevelFilter}"
                Width="120" />
      <TextBox Text="{Binding SearchText}" Watermark="Search..."
               Width="200" />
      <Button Command="{Binding ClearCommand}" Content="Clear" />
    </StackPanel>

    <ListBox ItemsSource="{Binding FilteredEntries}"
             VirtualizationMode="None">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <StackPanel Orientation="Horizontal" Spacing="8">
            <TextBlock Text="{Binding Timestamp}" Width="140" />
            <TextBlock Text="{Binding Level}" Width="80"
                       FontWeight="Bold" />
            <TextBlock Text="{Binding Message}" />
          </StackPanel>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
</UserControl>
```

### Registration

```csharp
var sink = new InMemoryLogSink();
builder.Services.AddSingleton(sink);
builder.Services.AddTransient<LogViewerViewModel>();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(sink)
    .CreateLogger();
```

### Key points

- `Channel<LogEntry>` decouples the logging thread from the UI thread with bounded backpressure
- `CollectionChanged` triggers filter re-evaluation without polling
- The sink is registered as a singleton so the ViewModel and Serilog share the same instance
- Bounded channel with `DropOldest` prevents unbounded memory growth under high log volume

---

## Example 4: Testing Log Output with a Test Sink

Ensuring that a ViewModel logs the expected message on a save operation.

### Test sink

```csharp
public sealed class TestLogSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = new();
    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}
```

### ViewModel under test

```csharp
public partial class DocumentViewModel : ObservableObject
{
    private readonly ILogger<DocumentViewModel> _logger;

    public DocumentViewModel(ILogger<DocumentViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    private async Task SaveAsync(string path)
    {
        try
        {
            await File.WriteAllTextAsync(path, "content");
            _logger.LogInformation("Document saved to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document to {Path}", path);
        }
    }
}
```

### xUnit test

```csharp
public class DocumentViewModelTests
{
    [Fact]
    public async Task Save_logs_information_on_success()
    {
        var sink = new TestLogSink();
        var loggerFactory = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var logger = loggerFactory.ForContext<DocumentViewModel>();
        var vm = new DocumentViewModel(logger);
        var tempPath = Path.GetTempFileName();

        await vm.SaveCommand.ExecuteAsync(tempPath);

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Information, logEvent.Level);
        Assert.Contains("saved", logEvent.RenderMessage(),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Save_logs_error_on_failure()
    {
        var sink = new TestLogSink();
        var loggerFactory = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var logger = loggerFactory.ForContext<DocumentViewModel>();
        var vm = new DocumentViewModel(logger);

        await vm.SaveCommand.ExecuteAsync(""); // Invalid path

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Error, logEvent.Level);
        Assert.NotNull(logEvent.Exception);
    }

    [Fact]
    public void Log_contains_structured_properties()
    {
        var sink = new TestLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(sink)
            .CreateLogger()
            .ForContext<DocumentViewModel>();

        logger.LogInformation("Document saved to {Path}", "/tmp/doc.txt");

        var evt = Assert.Single(sink.Events);
        Assert.True(evt.Properties.ContainsKey("Path"));
        Assert.Equal("\"/tmp/doc.txt\"",
            evt.Properties["Path"].ToString());
    }
}
```

### Key points

- No mocking framework needed — `ILogEventSink` is a single-method interface
- The `TestLogSink` collects all events for assertion
- Tests verify both log level and structured property presence, not just message text
- Serilog's `LoggerConfiguration` can create a logger without file/network I/O, making tests fast
