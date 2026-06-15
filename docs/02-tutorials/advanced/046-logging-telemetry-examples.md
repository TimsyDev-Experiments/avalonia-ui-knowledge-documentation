---
tier: advanced
topic: observability
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 046-logging-telemetry.md
---

# 046X — Logging & Telemetry: Real-World Examples

**What you'll build:** An in-app diagnostic log viewer with real-time filtering, and an operation-tracing system for a document editor using OpenTelemetry spans.

**Prerequisites:** [046 — Logging & Telemetry](046-logging-telemetry.md). The [verbose companion](046-logging-telemetry-verbose.md) covers structured logging internals, ActivitySource static semantics, and sensitive-data filtering in detail.

---

## Example 1: In-App Diagnostic Log Viewer with Filtering

**Goal:** A developer panel that displays live log output from Serilog inside the running app, with namespace-level filtering so the user can drill into specific subsystems.

### Log Sink

```csharp
// Services/DiagnosticLogSink.cs
using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Serilog.Core;
using Serilog.Events;

namespace MyApp.Services;

public class DiagnosticLogSink : ILogEventSink
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Entries.Add(new LogEntry
            {
                Timestamp = logEvent.Timestamp.LocalDateTime,
                Level = logEvent.Level,
                SourceContext = GetSourceContext(logEvent),
                Message = logEvent.RenderMessage(),
            });

            if (Entries.Count > 500)
                Entries.RemoveAt(0);
        });
    }

    private static string GetSourceContext(LogEvent logEvent)
    {
        return logEvent.Properties.TryGetValue("SourceContext", out var value)
            ? value.ToString().Trim('"')
            : "";
    }
}

public record LogEntry
{
    public DateTime Timestamp { get; init; }
    public LogEventLevel Level { get; init; }
    public string SourceContext { get; init; } = "";
    public string Message { get; init; } = "";
}
```

### ViewModel

```csharp
// ViewModels/DiagnosticsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using MyApp.Services;

namespace MyApp.ViewModels;

public partial class DiagnosticsViewModel : ObservableObject
{
    private readonly DiagnosticLogSink _sink;

    [ObservableProperty]
    private string _levelFilter = "All";

    [ObservableProperty]
    private string _namespaceFilter = "";

    public ObservableCollection<LogEntry> FilteredEntries { get; } = new();

    public string[] LevelOptions { get; } = { "All", "Verbose", "Debug", "Information", "Warning", "Error", "Fatal" };

    public DiagnosticsViewModel(DiagnosticLogSink sink)
    {
        _sink = sink;
        _sink.Entries.CollectionChanged += (_, _) => ApplyFilters();
    }

    partial void OnLevelFilterChanged(string value) => ApplyFilters();
    partial void OnNamespaceFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _sink.Entries.AsEnumerable();

        if (LevelFilter != "All" && Enum.TryParse<LogEventLevel>(LevelFilter, out var level))
            query = query.Where(e => e.Level == level);

        if (!string.IsNullOrWhiteSpace(NamespaceFilter))
            query = query.Where(e =>
                e.SourceContext.Contains(NamespaceFilter, StringComparison.OrdinalIgnoreCase));

        FilteredEntries.Clear();
        foreach (var entry in query.Take(200))
            FilteredEntries.Add(entry);
    }
}
```

### Serilog Configuration

```csharp
// Program.cs
var diagnosticSink = new DiagnosticLogSink();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithThreadId()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Sink(diagnosticSink)
    .CreateLogger();

builder.Services.AddSingleton(diagnosticSink);
```

### View

```xml
<!-- File: Views/DiagnosticsView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:s="clr-namespace:System;assembly=mscorlib"
             x:Class="MyApp.Views.DiagnosticsView"
             x:DataType="vm:DiagnosticsViewModel">

  <DockPanel Margin="8">
    <StackPanel DockPanel.Dock="Top" Spacing="6" Margin="0,0,0,8">
      <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="Level:" VerticalAlignment="Center" />
        <ComboBox ItemsSource="{Binding LevelOptions}"
                  SelectedItem="{Binding LevelFilter}"
                  Width="120" />
        <TextBlock Text="Namespace:" VerticalAlignment="Center" />
        <TextBox Text="{Binding NamespaceFilter}"
                 Watermark="Filter by namespace..."
                 Width="200" />
      </StackPanel>
    </StackPanel>

    <ListBox ItemsSource="{Binding FilteredEntries}"
             ScrollViewer.VirtualizationMode="None">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="services:LogEntry">
          <Grid ColumnDefinitions="70,80,1*,Auto" Gap="6">
            <TextBlock Text="{Binding Timestamp, StringFormat='{}{0:HH:mm:ss}'}" />
            <TextBlock Text="{Binding Level}"
                       FontWeight="Bold"
                       Foreground="{Binding Level, Converter={StaticResource LevelToColor}}" />
            <TextBlock Text="{Binding Message}"
                       TextTrimming="CharacterEllipsis" />
            <TextBlock Text="{Binding SourceContext}"
                       FontSize="10" Foreground="Gray" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </DockPanel>
</UserControl>
```

### How It Works

1. `DiagnosticLogSink` is registered as a singleton and passed to Serilog's configuration via `.WriteTo.Sink(diagnosticSink)`. Every Serilog event flows through `Emit`.
2. `Emit` is called on whatever thread produced the log event. It uses `Dispatcher.UIThread.Post` to safely add entries to the `ObservableCollection` on the UI thread.
3. The collection is capped at 500 entries. When exceeded, the oldest entry is removed — this prevents unbounded memory growth in a long-running session.
4. `DiagnosticsViewModel` subscribes to `CollectionChanged` on the sink's `Entries`. On any change, it re-applies the level and namespace filters and rebuilds `FilteredEntries`.
5. The XAML binds the `ListBox` to `FilteredEntries`. The `LevelToColor` converter maps `LogEventLevel` to a brush color (e.g., Error → red, Warning → orange).
6. `Enrich.FromLogContext()` captures `SourceContext` from the `ILogger<T>` category name, which is the `T` fully qualified type name. The namespace filter matches against this.

### Key Points

- The sink is separated from the ViewModel. Multiple ViewModels (or a future file exporter) could observe the same sink independently.
- Filtering is done on the ViewModel, not in XAML. This keeps XAML simple and the filter logic testable.
- Edge case: if logs arrive faster than the dispatcher can process them, `Post` queues them and they are processed in order. The 500-entry cap prevents the queue from accumulating indefinitely.
- Edge case: the namespace filter is case-insensitive partial match. Searching `"ViewModels"` matches `"MyApp.ViewModels.DashboardViewModel"`.

---

## Example 2: Operation Tracing in a Document Editor

**Goal:** Trace document save and load operations with nested spans, duration tracking, and error tagging using OpenTelemetry.

### Tracer and Service

```csharp
// Services/DocumentService.cs
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MyApp.Services;

public class DocumentService
{
    private static readonly ActivitySource ActivitySource = new("DocumentEditor");

    public async Task<Document> LoadAsync(string filePath)
    {
        using var activity = ActivitySource.StartActivity("DocumentService.Load");
        activity?.SetTag("file.path", filePath);

        try
        {
            // Simulate reading from disk
            await Task.Delay(200);

            var doc = new Document();

            using (var parseActivity = ActivitySource.StartActivity("ParseContent"))
            {
                parseActivity?.SetTag("content.size", doc.RawSize);
                await Task.Delay(100);
            }

            activity?.SetTag("load.result", "success");
            return doc;
        }
        catch (Exception ex)
        {
            activity?.SetTag("load.result", "error");
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    public async Task<bool> SaveAsync(Document doc, string filePath)
    {
        using var activity = ActivitySource.StartActivity("DocumentService.Save");
        activity?.SetTag("file.path", filePath);
        activity?.SetTag("content.size", doc.RawSize);

        try
        {
            await Task.Delay(150);
            activity?.SetTag("save.result", "success");
            return true;
        }
        catch (Exception ex)
        {
            activity?.SetTag("save.result", "error");
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}

public class Document
{
    public long RawSize => 42_000;
}
```

### ViewModel

```csharp
// ViewModels/DocumentEditorViewModel.cs
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MyApp.Services;

namespace MyApp.ViewModels;

public partial class DocumentEditorViewModel : ObservableObject
{
    private readonly DocumentService _documentService;
    private readonly ILogger<DocumentEditorViewModel> _logger;

    [ObservableProperty]
    private string _filePath = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public DocumentEditorViewModel(
        DocumentService documentService,
        ILogger<DocumentEditorViewModel> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task OpenDocumentAsync()
    {
        IsBusy = true;
        StatusMessage = "Opening...";
        _logger.LogInformation("Opening document {Path}", FilePath);

        try
        {
            var doc = await _documentService.LoadAsync(FilePath);
            StatusMessage = $"Loaded: {FilePath}";
            _logger.LogInformation("Document loaded successfully");
        }
        catch (Exception ex)
        {
            StatusMessage = "Open failed";
            _logger.LogError(ex, "Failed to open {Path}", FilePath);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOperate))]
    private async Task SaveDocumentAsync()
    {
        IsBusy = true;
        StatusMessage = "Saving...";

        try
        {
            var doc = new Document();
            var result = await _documentService.SaveAsync(doc, FilePath);
            StatusMessage = result ? "Saved" : "Save returned false";
        }
        catch (Exception ex)
        {
            StatusMessage = "Save failed";
            _logger.LogError(ex, "Failed to save {Path}", FilePath);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanOperate() => !IsBusy && !string.IsNullOrWhiteSpace(FilePath);
}
```

### OpenTelemetry Setup

```csharp
// Program.cs
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("DocumentEditor"))
    .AddSource("DocumentEditor")  // matches ActivitySource name
    .AddConsoleExporter()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();
```

### View

```xml
<!-- File: Views/DocumentEditorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.DocumentEditorView"
             x:DataType="vm:DocumentEditorViewModel">

  <StackPanel Spacing="12" Margin="20">
    <TextBox Text="{Binding FilePath}"
             Watermark="Enter file path..." />

    <StackPanel Orientation="Horizontal" Spacing="8">
      <Button Content="Open"
              Command="{Binding OpenDocumentCommand}" />
      <Button Content="Save"
              Command="{Binding SaveDocumentCommand}" />
    </StackPanel>

    <ProgressBar IsIndeterminate="{Binding IsBusy}"
                 IsVisible="{Binding IsBusy}" />

    <TextBlock Text="{Binding StatusMessage}" />
  </StackPanel>
</UserControl>
```

### How It Works

1. `DocumentService` defines a static `ActivitySource` named `"DocumentEditor"`. Every `StartActivity` call creates a span.
2. `LoadAsync` creates a top-level span. Inside it, `ParseContent` creates a nested child span. Spans measure elapsed time automatically from `StartActivity` to `Dispose`.
3. Tags are set with `activity?.SetTag(...)`. The `?.` is essential — if no listener is attached, `StartActivity` returns null and the property writes are no-ops.
4. On error, tags are set for `error.message` and the activity status is set to `ActivityStatusCode.Error`. The exception is rethrown so the caller also sees the failure.
5. The ViewModel logs via `ILogger<T>` at the same boundaries. Structured logging events capture the same context but as discrete events, not timed spans.
6. The tracer provider is configured with `AddSource("DocumentEditor")` — this must match the `ActivitySource` name exactly or spans will be created but never exported.
7. In development, `AddConsoleExporter()` prints spans to stdout. In production, `AddOtlpExporter()` sends to a local OpenTelemetry Collector which forwards to Datadog, Grafana, or similar.

### Key Points

- Nested spans (`LoadAsync` → `ParseContent`) create a parent-child hierarchy. Trace viewers show the call tree and per-operation duration.
- Tags are cheap — use them liberally to annotate spans. Avoid storing large objects; store identifiers or counts instead.
- `ActivityStatusCode.Error` marks the span as failed in the trace viewer. Without it, a span that threw an exception still shows as "OK" — only the error tags would hint at failure.
- The `using` pattern ensures spans are closed even on exception paths (the `catch` block sets error state, then `Dispose` ends the span).
- Edge case: if `AddSource("DocumentEditor")` is misspelled or missing, `StartActivity` returns null. All `activity?.` calls are no-ops — the app works without tracing, just no spans are exported.

---

## What These Examples Demonstrate

| Scenario | Mechanim | Output | Best for |
|---|---|---|---|
| Log viewer | Custom `ILogEventSink` + `ObservableCollection` | In-app UI with live filter | Developer diagnostics, support tools |
| Operation tracing | `ActivitySource` + nested spans | Console/OTLP export | Performance analysis, root-cause investigation |

The log viewer shows how to funnel structured log events into the running app for immediate inspection — useful during development or for power-user diagnostics. The tracing example shows how to instrument service boundaries with timing and error context, enabling offline analysis of slow or failing operations.

## See Also

- [046 — Logging & Telemetry](046-logging-telemetry.md)
- [046V — Verbose Companion](046-logging-telemetry-verbose.md)
- [044 — Background Services & Progress Reporting](044-background-services-and-progress.md)
- [Serilog docs](https://serilog.net/)
- [OpenTelemetry .NET docs](https://opentelemetry.io/docs/instrumentation/net/)
