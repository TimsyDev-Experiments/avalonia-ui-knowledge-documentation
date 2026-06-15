---
tier: advanced
topic: observability
estimated: 20 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# 046 -- Logging & Telemetry

**What you'll learn:** wire Serilog for structured file + console logging, add OpenTelemetry tracing, configure log levels per namespace, and build a UI log viewer.

**Prerequisites:** [032 -- Dependency Injection for MVVM](032-mvvm-di-wiring.md)

---

## 1. Serilog setup

```shell
dotnet add package Serilog
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Extensions.Hosting
```

```csharp
// Program.cs — before builder is constructed
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    builder = AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();              // still useful for DevTools
    // ... build and run
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

The `Serilog.Extensions.Hosting` package makes Serilog the provider for `ILogger<T>` throughout DI.

## 2. ILogger<T> in ViewModels and Services

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly ITodoService _service;

    public MainViewModel(ILogger<MainViewModel> logger, ITodoService service)
    {
        _logger = logger;
        _service = service;
        _logger.LogInformation("MainViewModel initialized");
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        _logger.LogDebug("Starting item load");
        try
        {
            var items = await _service.GetAllAsync();
            Items = new ObservableCollection<TodoItem>(items);
            _logger.LogInformation("Loaded {Count} items", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load items");
        }
    }
}
```

Use structured placeholders (`{Count}`) — never string interpolation. Structured logging makes the values searchable in Seq, Elastic, or any structured log backend.

## 3. Log levels by namespace

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Error)
    .MinimumLevel.Override("MyApp.ViewModels", LogEventLevel.Verbose)
    .CreateLogger();
```

## 4. OpenTelemetry tracing

```shell
dotnet add package OpenTelemetry
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

```csharp
// Program.cs — after builder creation
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("AvaloniaApp"))
    .AddSource("MyApp")
    .AddConsoleExporter()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();
```

Create a tracer and use it in services:

```csharp
public class TodoService
{
    private static readonly ActivitySource ActivitySource = new("MyApp");

    public async Task<List<TodoItem>> GetAllAsync()
    {
        using var activity = ActivitySource.StartActivity("TodoService.GetAll");
        activity?.SetTag("items.count", 0);

        var items = await _db.QueryAsync<TodoItem>("SELECT * FROM Todos");

        activity?.SetTag("items.count", items.Count);
        return items;
    }
}
```

## 5. In-app log viewer

```csharp
public sealed class LogEventSink : ILogEventSink
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        // Render to UI thread if called from background
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Entries.Add(new LogEntry
            {
                Timestamp = logEvent.Timestamp.LocalDateTime,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString()
            });

            if (Entries.Count > 1000)
                Entries.RemoveAt(0);
        });
    }
}

public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Message { get; init; } = "";
    public string? Exception { get; init; }
}
```

Register the sink:

```csharp
var sink = new LogEventSink();
Log.Logger = new LoggerConfiguration()
    .WriteTo.Sink(sink)
    .CreateLogger();

// Register the sink as a singleton so ViewModels can bind to it
builder.Services.AddSingleton(sink);
```

Bind in XAML:

```xml
<ItemsControl ItemsSource="{Binding Sink.Entries}">
  <ItemsControl.ItemTemplate>
    <DataTemplate>
      <StackPanel Orientation="Horizontal" Spacing="6">
        <TextBlock Text="{Binding Timestamp, StringFormat='{}{HH:mm:ss}'}" />
        <TextBlock Text="{Binding Level}" FontWeight="Bold" />
        <TextBlock Text="{Binding Message}" />
      </StackPanel>
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

## 6. Sensitive data filtering

```csharp
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(e =>
        e.Properties.ContainsKey("Password") ||
        e.Properties.ContainsKey("Token") ||
        e.MessageTemplate.Text.Contains("password", StringComparison.OrdinalIgnoreCase))
    .CreateLogger();
```

Or use a custom `IDestructuringPolicy` to redact sensitive types:

```csharp
public class CredentialDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is Credential)
        {
            result = new ScalarValue("*** REDACTED ***");
            return true;
        }
        result = null!;
        return false;
    }
}
```

## Key takeaways

- Use `ILogger<T>` via constructor injection for all ViewModels and services
- Use structured logging placeholders — never string interpolation
- Configure `MinimumLevel.Override` per namespace to reduce noise from framework code
- `LogEventSink` captures logs in-memory for a UI log viewer (cap at N entries)
- OpenTelemetry `ActivitySource` adds distributed tracing; export to OTLP for Grafana or Datadog
- Filter or destructure sensitive data at the sink level

---

## See Also

- [032 -- Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [044 -- Background Services & Progress Reporting](044-background-services-and-progress.md)
- [Serilog docs](https://serilog.net/)
- [OpenTelemetry .NET docs](https://opentelemetry.io/docs/instrumentation/net/)
- [046V -- Logging & Telemetry (verbose companion)](046-logging-telemetry-verbose.md)
- [046X -- Logging & Telemetry (examples)](046-logging-telemetry-examples.md)
