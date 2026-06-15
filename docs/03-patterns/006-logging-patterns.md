---
tier: reference
topic: observability
estimated: 10 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# Pattern 006 -- Logging Patterns

Common logging patterns for Avalonia applications, from basic console output to structured telemetry.

---

## 1. Constructor injection of ILogger<T>

```csharp
public partial class TodoViewModel : ObservableObject
{
    private readonly ILogger<TodoViewModel> _logger;
    private readonly ITodoService _service;

    public TodoViewModel(ILogger<TodoViewModel> logger, ITodoService service)
    {
        _logger = logger;
        _service = service;
        _logger.LogInformation("ViewModel initialized");
    }
}
```

Register `ILogger<T>` via DI. The logging provider (Serilog, Console, etc.) is configured once at startup.

## 2. Structured logging (always use placeholders)

```csharp
// Good — structured, searchable
_logger.LogInformation("Loaded {Count} items for user {UserId}", items.Count, userId);

// Bad — unstructured, not searchable in SEQ/Elastic
_logger.LogInformation($"Loaded {items.Count} items for user {userId}");
```

Structured properties (`Count`, `UserId`) are indexed by log backends, enabling queries like `Count > 100`.

## 3. Log levels by concern

| Level | When to use |
|---|---|
| `Verbose` / `Debug` | Developer diagnostics, method entry/exit, detailed state dumps |
| `Information` | Normal operations: startup, user actions, successful saves |
| `Warning` | Unexpected but handled: fallback values, retries, deprecated API usage |
| `Error` | Recoverable failures: file not found, network timeout, validation failures |
| `Critical` | Unrecoverable: database corruption, out-of-memory, startup failure |

```csharp
_logger.LogWarning("File {Path} not found, using default configuration", path);
_logger.LogError(ex, "Failed to save item {Id}", itemId);
_logger.LogCritical(ex, "Application failed to start");
```

## 4. Contextual logging with Serilog enrichers

```csharp
Log.Logger = new LoggerConfiguration()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "MyAvaloniaApp")
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### Scoped context with `LogContext`

```csharp
using (LogContext.PushProperty("UserId", userId))
using (LogContext.PushProperty("TransactionId", Guid.NewGuid()))
{
    _logger.LogInformation("Processing order {OrderId}", orderId);
    // All log events within this scope include UserId and TransactionId
}
```

## 5. Sensitive data redaction

```csharp
// Filter by property name
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(e =>
        e.Properties.ContainsKey("Password") ||
        e.Properties.ContainsKey("AccessToken"))
    .CreateLogger();

// Or use a custom destructuring policy
public class SensitiveDataPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is SecureString || value is PasswordBox)
        {
            result = new ScalarValue("*** REDACTED ***");
            return true;
        }
        result = null!;
        return false;
    }
}
```

## 6. In-app log viewer sink

From [tutorial 046](../02-tutorials/advanced/046-logging-telemetry.md):

```csharp
public sealed class LogEventSink : ILogEventSink
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Entries.Add(new LogEntry
            {
                Timestamp = logEvent.Timestamp.LocalDateTime,
                Level = logEvent.Level.ToString(),
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString(),
            });
            while (Entries.Count > 1000) Entries.RemoveAt(0);
        });
    }
}
```

## 7. Performance: async logging

Serilog's file and network sinks are already asynchronous by default — `WriteTo.File` buffers and flushes on a background thread. For custom sinks:

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Async(a => a.File("logs/app-.log"))
    .CreateLogger();
```

Avoid expensive operations in `Emit` — the logging pipeline blocks the calling thread.

## 8. Conditional compilation

```csharp
#if DEBUG
    _logger.LogDebug("Binding context: {Context}", bindingContext);
#endif
```

Or configure minimum level at runtime per namespace (no `#if` needed):

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("MyApp.ViewModels", LogEventLevel.Verbose)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .CreateLogger();
```

## 9. Testing with logged assertions

```csharp
// Use a test sink to verify log output
public class LogTestSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = new();
    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}

[Fact]
public void Save_logs_information()
{
    var sink = new LogTestSink();
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Sink(sink)
        .CreateLogger();

    var vm = new TodoViewModel(/* with logger */);
    vm.SaveCommand.Execute(null);

    Assert.Contains(sink.Events, e =>
        e.Level == LogEventLevel.Information &&
        e.MessageTemplate.Text.Contains("saved"));
}
```

## Key takeaways

- Always inject `ILogger<T>` — never create loggers manually
- Use structured placeholders (`{Property}`), not string interpolation
- Redact sensitive data at the sink level before it's written
- `LogContext.PushProperty` scopes contextual data to a block
- Test log output with a custom `ILogEventSink` in unit tests
- File and network sinks are async by default in Serilog

---

## See Also

- [046 -- Logging & Telemetry](../02-tutorials/advanced/046-logging-telemetry.md)
- [032 -- Dependency Injection for MVVM](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Serilog docs](https://serilog.net/)
