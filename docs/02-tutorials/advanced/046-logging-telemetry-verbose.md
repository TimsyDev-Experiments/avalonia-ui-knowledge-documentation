---
tier: advanced
topic: observability
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 046-logging-telemetry.md
---

# 046V — Logging & Telemetry: An In-Depth Companion

**What you'll learn in this companion:** Not just how to configure Serilog and OpenTelemetry, but why structured logging matters for desktop apps, how `ILogger<T>` integrates with DI, what `MinimumLevel.Override` actually controls, why `ActivitySource` needs a static instance, and how to build a log viewer without blocking the rendering pipeline.

**Prerequisites:** [032 — Dependency Injection for MVVM](032-mvvm-di-wiring.md), [044 — Background Services & Progress Reporting](044-background-services-and-progress.md)

**You should already have read:** [046 — Logging & Telemetry](046-logging-telemetry.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Serilog — Why Structured Logging Is Different from Text Logging

### The key difference: events, not strings

```csharp
// Bad — string interpolation
_logger.LogInformation($"Loaded {items.Count} items");

// Good — structured placeholders
_logger.LogInformation("Loaded {Count} items", items.Count);
```

With string interpolation, the log output is: `Loaded 42 items`. This is human-readable but machine-unsearchable. You cannot filter logs where `Count` is greater than 10 because `Count` is not a named field — it's embedded in text.

With structured logging, the event is stored as:
```
Template: "Loaded {Count} items"
Values:   { Count: 42 }
```

Serilog stores these as structured events. Backends like Seq, Elasticsearch, or Grafana Loki index `Count` as a numeric field. You can query: `Count > 10` or `Count < 0` (to find bugs). With interpolation, you cannot do this.

### Why this matters for a desktop app

In a server context, structured logging is table stakes. In a desktop app, you might think it's overkill — but it's not. When a user reports a bug, structured logs let you ask: "What was the error count?" or "Which file failed to process?" without asking the user to scroll through text.

---

## 2. The Serilog Bootstrap Logger

```csharp
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
```

### The bootstrap logger pattern

`Log.Logger` is Serilog's **static** global logger. It must be set before any other code runs — hence the placement before `AppBuilder.Configure<App>()` in Program.cs.

The bootstrap logger serves two purposes:
1. Captures startup errors before DI is wired (the `try/catch` around app building)
2. Becomes the source for `ILogger<T>` when `Serilog.Extensions.Hosting` is registered

When you add `UseSerilog()` (via `Serilog.Extensions.Hosting`), the DI container reads `Log.Logger` at startup and uses it as the logging provider for `ILogger<T>`. If you set `Log.Logger` after DI is built, `ILogger<T>` will not capture those initial events.

### MinimumLevel.Override — what it actually does

```csharp
.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
```

The first `.MinimumLevel.Debug()` sets the global minimum to Debug. The `.Override` calls set **namespace-specific** overrides. The namespace prefix `"Microsoft"` matches any logger whose category name starts with `Microsoft.*` — this includes ASP.NET Core host, EF Core, and other MS libraries.

Why you do this: framework code is verbose at Debug/Information. Overriding it to Warning means your application logs at Debug (verbose) but framework noise only appears at Warning or higher.

The prefix matching is exact: `"Microsoft"` matches `Microsoft.Hosting.Lifetime` but not `MyApp.MicrosoftIntegration`. Override specificity is checked in reverse order of configuration — the last matching override wins.

---

## 3. ILogger<T> — Why the Category Matters

```csharp
public MainViewModel(ILogger<MainViewModel> logger, ITodoService service)
```

`ILogger<T>` is a generic interface. The DI container resolves `ILogger<MainViewModel>` by creating an `ILogger<MainViewModel>` whose **category name** is the fully qualified type name: `MyApp.ViewModels.MainViewModel`.

This category name is what `MinimumLevel.Override` matches against. It is also included in every log event, making it possible to filter by class.

### Using ILogger in a static context

If you need logging in a static class or method, inject `ILoggerFactory` and create a logger:

```csharp
public static class StartupHelper
{
    private static ILogger _logger = Log.Logger.ForContext<StartupHelper>();

    public static void Initialize()
    {
        _logger.Information("Startup initialized");
    }
}
```

`Log.Logger.ForContext<T>()` creates a Serilog `ILogger` directly from the static logger. This bypasses DI and is appropriate for static utility code.

---

## 4. Log Levels by Namespace — Why Verbose Exists

```csharp
.MinimumLevel.Override("MyApp.ViewModels", LogEventLevel.Verbose)
```

Serilog's levels in order: Verbose, Debug, Information, Warning, Error, Fatal.

Verbose is for diagnostic tracing that would normally be too noisy even for Debug. Setting `MyApp.ViewModels` to Verbose means every `Log.Verbose(...)` call in your ViewModels will be recorded, while other namespaces at Debug level skip Verbose events.

This pattern is useful when debugging a specific layer: you crank Verbose on the namespace you're investigating while keeping everything else at normal levels.

### The cost of Verbose

Every log event — even one that is filtered out by level — still involves:
1. Evaluating the template string
2. Boxing value types for the params array
3. A level comparison check

For performance-critical paths, guard verbose calls:

```csharp
if (_logger.IsEnabled(LogEventLevel.Verbose))
    _logger.Verbose("Complex {Computation}", ComputeExpensiveValue());
```

---

## 5. OpenTelemetry — Why a Desktop App Needs Tracing

### The difference from logging

Logging records discrete events: "Item loaded", "Error occurred". Tracing records **operations with duration and causality**: "LoadItems operation took 450ms, called GetAllAsync (350ms) and ProcessResults (100ms)".

For a desktop app, traces help answer:
- "Why is the UI freezing?" — look at trace durations
- "Which database query is slow?" — filter by operation name
- "Is the slow path in the UI thread or a background service?" — check the thread/activity context

### ActivitySource — why it must be static

```csharp
private static readonly ActivitySource ActivitySource = new("MyApp");
```

`ActivitySource` is designed to be a **static singleton**. The OpenTelemetry SDK binds listeners to sources by name at startup. If you create a new `ActivitySource` on each service instance, you may create race conditions in the SDK's listener wiring.

The source name (`"MyApp"`) must match the name in the tracer provider builder:

```csharp
Sdk.CreateTracerProviderBuilder()
    .AddSource("MyApp")  // matches ActivitySource name
```

If the names don't match, the activities are created but never exported — they exist in memory but trace processors ignore them.

### Activity lifecycle

```csharp
using var activity = ActivitySource.StartActivity("TodoService.GetAll");
activity?.SetTag("items.count", 0);

var items = await _db.QueryAsync<TodoItem>("SELECT * FROM Todos");

activity?.SetTag("items.count", items.Count);
```

`StartActivity` returns null if no listener is attached (to avoid allocation overhead). The null-conditional `?.` is essential — without it, you'd get NullReferenceException in environments without tracing configured.

The `using` block ensures the activity ends (and is exported) when the operation completes. Activities are automatically timed from StartActivity to Dispose.

### Tags vs baggage vs events

- **Tags** — key/value pairs associated with the entire operation (`items.count`, `user.id`)
- **Baggage** — key/value pairs that propagate to downstream operations (e.g., `customer.id` flows from UI thread to background service to database call)
- **Events** — named timestamps within an activity (`"cache.miss"`, `"retry.attempt"`)

For a desktop app, tags are usually sufficient. Baggage is useful when tracing spans across thread or process boundaries.

---

## 6. The In-App Log Viewer — Why Not Just TextBox

### ObservableCollection on a background thread

```csharp
public void Emit(LogEvent logEvent)
{
    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
        Entries.Add(new LogEntry { ... });
        if (Entries.Count > 1000)
            Entries.RemoveAt(0);
    });
}
```

The `Emit` method is called by the Serilog pipeline, which may be on any thread. Directly modifying `ObservableCollection` from a background thread throws `NotSupportedException` because `ObservableCollection` raises collection-changed events synchronously, and those events must reach the UI thread.

The `Dispatcher.UIThread.Post` ensures the `Add` and `RemoveAt` happen on the UI thread. The cap at 1000 entries prevents unbounded memory growth — without it, a verbose user session could accumulate millions of entries.

### Why not bind directly to the sink's Entries

The `LogEventSink` is registered as a singleton. You bind via the ViewModel:

```xml
<ItemsControl ItemsSource="{Binding Sink.Entries}">
```

The ViewModel holds a reference to the sink singleton (injected via constructor). This means the ViewModel does not own the data — it just exposes it. Multiple ViewModels can observe the same sink.

---

## 7. Sensitive Data Filtering — The IDestructuringPolicy Contract

### Filter.ByExcluding vs IDestructuringPolicy

```csharp
// Filter approach — reject the entire event
.Filter.ByExcluding(e =>
    e.Properties.ContainsKey("Password") ||
    e.Properties.ContainsKey("Token"))

// Destructuring approach — redact specific types
public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
    out LogEventPropertyValue result)
{
    if (value is Credential)
    {
        result = new ScalarValue("*** REDACTED ***");
        return true;
    }
}
```

`Filter.ByExcluding` rejects the entire log event if it contains a sensitive property. This is all-or-nothing — the event is gone.

`IDestructuringPolicy` lets the event through but replaces the sensitive value. This preserves the event's context while hiding the secret. Use `IDestructuringPolicy` when you need the event for debugging but not the actual value.

### The message template text filter

```csharp
e.MessageTemplate.Text.Contains("password", StringComparison.OrdinalIgnoreCase)
```

This catches string-interpolated logs where the developer accidentally embeds a password in the message text rather than using a structured placeholder. It is a safety net, not a primary filter — structured placeholders should always be the norm.

---

## 8. Serilog Enrichers

The tutorial uses `Enrich.WithThreadId()` and `Enrich.WithMachineName()`. These add properties to every event:

- `ThreadId` — identifies which thread logged the event, critical for diagnosing UI thread vs background thread issues
- `MachineName` — less useful in a desktop app (single machine) but helpful if logs are aggregated from multiple installations

Other useful enrichers:

```csharp
.Enrich.WithEnvironmentName()    // Development/Production
.Enrich.WithProperty("App", "MyApp")  // constant property for filtering
```

Custom enrichers implement `ILogEventEnricher`:

```csharp
public class VersionEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory pf)
    {
        logEvent.AddPropertyIfAbsent(pf.CreateProperty("AppVersion",
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()));
    }
}
```

---

## 9. OpenTelemetry Exporters — Console vs OTLP

```csharp
.AddConsoleExporter()
.AddOtlpExporter(options =>
{
    options.Endpoint = new Uri("http://localhost:4317");
})
```

`.AddConsoleExporter()` prints traces to stdout — useful during development. `.AddOtlpExporter()` sends traces to an OpenTelemetry Protocol (OTLP) endpoint, typically an OpenTelemetry Collector running on localhost:4317.

For a desktop app, the typical flow is:
```
App → OTLP → Collector → Elastic/Loki/Datadog
```

The collector buffers, batches, and retries failed exports, preventing the exporter from blocking your app. For production, always use a collector rather than exporting directly to the backend.

---

## Key Takeaways

- **Structured logging** means machine-queryable events, not text blobs — always use placeholders, never string interpolation
- `ILogger<T>` category name is the FQN of `T` — use it for namespace-level filter overrides
- `MinimumLevel.Override` uses prefix matching — more specific overrides must be configured after less specific ones
- `ActivitySource` must be static and its name must match `AddSource` in the tracer provider builder
- In-app log viewers need `Dispatcher.UIThread.Post` because `LogEventSink.Emit` runs on arbitrary threads
- Cap in-memory log collections (e.g., 1000 entries) to prevent unbounded memory growth
- `IDestructuringPolicy` redacts sensitive values while preserving the event; `Filter.ByExcluding` drops the entire event
- Always guard verbose log calls with `_logger.IsEnabled()` in performance-critical paths

---

## See Also

- [046 — Logging & Telemetry (original)](046-logging-telemetry.md)
- [046X — Logging & Telemetry (examples)](046-logging-telemetry-examples.md)
- [032 — Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [044 — Background Services & Progress Reporting](044-background-services-and-progress.md)
- [Serilog docs](https://serilog.net/)
- [OpenTelemetry .NET docs](https://opentelemetry.io/docs/instrumentation/net/)
- [Seq — Structured log server](https://datalust.co/seq)
