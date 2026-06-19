---
tier: reference
topic: observability
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 006-logging-patterns.md
---

# 006V — Logging Patterns: An In-Depth Companion

You should already have read: [006 — Logging Patterns](006-logging-patterns.md) for the quick-start version. This file goes deeper on every section.

## Prerequisites

- `Microsoft.Extensions.Logging` basics (abstractions package)
- Serilog configuration familiarity
- DI container usage (`AddLogging`, `UseSerilog`)

---

## 1. ILogger<T> Injection — Deep Dive

### How the logger category works

The `T` in `ILogger<T>` determines the *category name* — typically the fully qualified type name. This is how you filter logs by namespace or class:

```csharp
// Category: "MyApp.ViewModels.TodoViewModel"
public class TodoViewModel
{
    private readonly ILogger<TodoViewModel> _logger;
}
```

For static classes or factories that cannot inject `ILogger<T>`, use `ILoggerFactory`:

```csharp
public static class StartupLogger
{
    private static ILogger? _logger;

    public static void Initialize(ILoggerFactory factory)
    {
        _logger = factory.CreateLogger("MyApp.Startup");
    }

    public static void Log(string message) =>
        _logger?.LogInformation("Startup: {Message}", message);
}
```

### Logger message structs

Every call to `LogInformation`, `LogWarning`, etc. allocates a `LogEvent` struct. For high-frequency logging (e.g., every frame during a drag operation), use the `LoggerMessage.Define` pattern (source-generated in .NET 6+):

```csharp
// Static field — defined once
private static readonly Action<ILogger, int, Exception?> _itemsLoaded =
    LoggerMessage.Define<int>(
        LogLevel.Information,
        eventId: new EventId(1001, "ItemsLoaded"),
        formatString: "Loaded {ItemCount} items"
    );

public void OnItemsLoaded(int count)
{
    _itemsLoaded(_logger, count, null);
}
```

For .NET 6+, `[LoggerMessage]` source generators provide the same zero-allocation benefit with less boilerplate:

```csharp
public static partial class LogMessages
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Loaded {ItemCount} items")]
    public static partial void ItemsLoaded(this ILogger logger, int itemCount);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Failed to load items")]
    public static partial void ItemsLoadFailed(this ILogger logger, Exception ex);
}
```

Usage:

```csharp
this.Logger().ItemsLoaded(items.Count);
```

---

## 2. Structured Logging — Deep Dive

### Why placeholders matter

When you write `LogInformation("Loaded {Count} items", count)`, Serilog (or any structured logger) captures `Count` as a named property in the log event. Log backends like Seq, Elasticsearch, or Datadog index these properties, enabling queries like:

```
Count > 1000
UserId = "abc-123"
```

With string interpolation (`$"Loaded {count} items"`), the value is embedded in the message text. It cannot be queried as a numeric field — only full-text search works, which is slower and less precise.

### Destructuring complex objects

Use the `@` destructuring operator to capture object state as JSON:

```csharp
_logger.LogInformation("User logged in: {@User}", user);

// Output: User logged in: {"Id": "abc", "Name": "Alice", "Role": "Admin"}
```

Without `@`, Serilog calls `ToString()` on the object. With `@`, it serializes public properties. Use this sparingly — serializing large objects creates allocation pressure.

### Capturing collections

```csharp
var ids = new[] { 1, 2, 3 };
_logger.LogInformation("Processed items: {Ids}", ids);

// By default, collections are rendered as "[1, 2, 3]"
// In Seq/Elasticsearch, each element is individually searchable
```

---

## 3. Log Levels — Deep Dive

### Level guidelines for Avalonia applications

| Level | Logged by default? | Typical Avalonia usage |
|---|---|---|
| `Verbose` | No | Layout pass timing, visual tree traversal, binding resolution |
| `Debug` | Debug only | Command execution, navigation events, HTTP request details |
| `Information` | Yes | Startup complete, user login, file saved, workspace switch |
| `Warning` | Yes | Fallback configuration loaded, API rate limit approaching, deprecated feature used |
| `Error` | Yes | Database query failure, file I/O error, network timeout |
| `Critical` | Yes | Unhandled exception in dispatcher, database corruption, startup failure |

### Dynamic level switching at runtime

Override log levels per namespace without restart:

```csharp
// At startup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("MyApp.ViewModels.TodoViewModel", LogEventLevel.Verbose)
    .CreateLogger();

// At runtime (requires reload)
Log.CloseAndFlush();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();
```

For production debugging, consider a configuration file watcher:

```csharp
var config = new LoggerConfiguration()
    .ReadFrom.AppSettings("serilog.json", reloadOnChange: true)
    .CreateLogger();
```

---

## 4. Serilog Enrichers and LogContext — Deep Dive

### Built-in enrichers

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()          // Adds {ThreadId}
    .Enrich.WithMachineName()       // Adds {MachineName}
    .Enrich.WithEnvironmentName()   // Adds {EnvironmentName} (Development/Production)
    .Enrich.WithProperty("App", "MyAvaloniaApp") // Static property
    .Enrich.FromLogContext()        // Enables LogContext scoping
    .CreateLogger();
```

### Custom enricher

```csharp
public sealed class AvaloniaVersionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        logEvent.AddPropertyIfAbsent(factory.CreateProperty(
            "AvaloniaVersion",
            typeof(Application).Assembly.GetName().Version?.ToString() ?? "unknown"
        ));
    }
}
```

### LogContext scoping patterns

`LogContext.PushProperty` creates a using block where all log calls include the scoped property:

```csharp
public async Task ProcessOrderAsync(Guid orderId, Guid userId)
{
    using (LogContext.PushProperty("OrderId", orderId))
    using (LogContext.PushProperty("UserId", userId))
    {
        _logger.LogInformation("Starting order processing");
        // All nested calls inherit OrderId and UserId scopes
        await ValidateAsync();
        await ChargeAsync();
        await ShipAsync();
        _logger.LogInformation("Order processing complete");
    }
}
```

Nested scopes stack — inner `PushProperty` values override outer ones of the same key.

### Thread-safe caveat

`LogContext` uses `AsyncLocal<T>`, which flows with the execution context. In fire-and-forget tasks, the context may be lost:

```csharp
// Lost context — Task.Run does not capture AsyncLocal
using (LogContext.PushProperty("UserId", userId))
{
    await Task.Run(() => {
        _logger.LogInformation("Background work"); // Lost "UserId"
    });
}

// Preserved — use Task.Factory.StartNew with TaskCreationOptions.HideScheduler
// or pass properties explicitly
```

For background operations, capture the scope manually:

```csharp
using (LogContext.PushProperty("UserId", userId))
{
    var capturedProperties = LogContext.Clone();
    await Task.Run(() =>
    {
        using (capturedProperties)
        {
            _logger.LogInformation("Background work"); // Has "UserId"
        }
    });
}
```

---

## 5. Sensitive Data Redaction — Deep Dive

### Multiple redaction strategies

**Destructuring policy (per type):**

```csharp
public sealed class RedactPasswordPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is string s && s.Length > 0 && s.Any(c => c == ' '))
        {
            // Very basic — use with caution
        }
        result = null!;
        return false;
    }
}
```

**Filter (by property name):**

```csharp
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(e =>
        e.Properties.Any(p =>
            p.Key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            p.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            p.Key.Contains("secret", StringComparison.OrdinalIgnoreCase)))
    .CreateLogger();
```

**Masking operator (Serilog.Sinks.Map):**

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.ByTransforming<SecureString>(_ => "*** REDACTED ***")
    .Destructure.ByTransforming<PasswordBox>(_ => "*** REDACTED ***")
    .CreateLogger();
```

### Audit trail for redaction events

Log when redaction occurs so you know data was suppressed:

```csharp
public sealed class AuditingRedactionPolicy : IDestructuringPolicy
{
    private readonly ILogger _auditLogger;

    public AuditingRedactionPolicy(ILogger auditLogger) => _auditLogger = auditLogger;

    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is SecureString)
        {
            _auditLogger.LogWarning("Redacted SecureString value");
            result = new ScalarValue("*** REDACTED ***");
            return true;
        }
        result = null!;
        return false;
    }
}
```

---

## 6. In-App Log Viewer — Deep Dive

### Thread-safe sink

The core file's `LogEventSink` dispatches to the UI thread on every `Emit`. For high-volume logging, batch and debounce:

```csharp
public sealed class BufferedLogEventSink : ILogEventSink
{
    private readonly Channel<LogEntry> _channel =
        Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public ObservableCollection<LogEntry> Entries { get; } = new();

    public BufferedLogEventSink()
    {
        _ = ReadLoopAsync();
    }

    public void Emit(LogEvent logEvent)
    {
        _channel.Writer.TryWrite(new LogEntry
        {
            Timestamp = logEvent.Timestamp.LocalDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
        });
    }

    private async Task ReadLoopAsync()
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
```

### Filtered view

Expose observable filtered views using `ICollectionView`:

```csharp
public sealed class LogViewerViewModel
{
    private readonly BufferedLogEventSink _sink;

    public ICollectionView FilteredEntries { get; }

    [ObservableProperty]
    private string? _levelFilter;

    public LogViewerViewModel(BufferedLogEventSink sink)
    {
        _sink = sink;
        FilteredEntries = CollectionViewSource.GetDefaultView(sink.Entries);
        FilteredEntries.Filter = o =>
        {
            if (LevelFilter is null) return true;
            return ((LogEntry)o).Level == LevelFilter;
        };
    }

    partial void OnLevelFilterChanged(string? value)
    {
        FilteredEntries.Refresh();
    }
}
```

---

## 7. Conditional Compilation and Namespace Overrides — Deep Dive

### Preprocessor vs. runtime

The core file shows `#if DEBUG`. A more flexible approach is runtime level configuration:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("MyApp.ViewModels", LogEventLevel.Verbose)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
    .CreateLogger();
```

All `ViewModels` get Verbose logging in development. In production, change the config to `.MinimumLevel.Override("MyApp.ViewModels", LogEventLevel.Information)` — no recompilation needed.

### Source link and file info

Include caller information for detailed tracing:

```csharp
[LoggerMessage(EventId = 0, Level = LogLevel.Information,
    Message = "{Message} (at {FilePath}:{LineNumber})")]
public static partial void LogWithCallerInfo(
    this ILogger logger,
    string message,
    [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0);
```

---

## Key Takeaways (Expanded)

- **Always use structured placeholders** — `{Property}` enables indexed search in log backends; string interpolation kills queryability
- **ILogger<T> is injectable everywhere** — use `LoggerMessage.Define` or the `[LoggerMessage]` source generator for high-frequency paths
- **Redact at the sink level** — use destructuring policies or filters; never log `SecureString`, `PasswordBox`, or tokens
- **LogContext scopes flow with AsyncLocal** — be aware they can be lost across `Task.Run` boundaries
- **Batch your in-app log sink** — channel-based buffering avoids flooding the UI thread
- **Runtime level overrides beat `#if DEBUG`** — configure levels per-namespace in `appsettings.json` and reload without recompilation
