---
tier: intermediate
topic: messaging
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 014-imessenger-patterns.md
---

# 014E — IMessenger Patterns: Real-World Examples

**What this is:** Two worked examples demonstrating decoupled ViewModel communication with `WeakReferenceMessenger`. Read [014 — IMessenger Patterns](014-imessenger-patterns.md) and [014V — Verbose Companion](014-imessenger-patterns-verbose.md) first.

---

## Example 1: Tab Activation with Navigation Tokens

### Goal

A tabbed document interface where each tab is a separate ViewModel. When a document tab is selected, it broadcasts an activation message on a scoped channel so other ViewModels (status bar, breadcrumb, title bar) can react without knowing about the tab's ViewModel type.

### Message Types

```csharp
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MyApp.Messages;

public sealed class DocumentActivatedMessage : ValueChangedMessage<string>
{
    public DocumentActivatedMessage(string documentId, string title)
        : base(documentId)
    {
        Title = title;
    }

    public string Title { get; }
}

public static class Channels
{
    public const string Document = "Document";
    public const string Status = "Status";
}
```

### Sender — DocumentTabViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class DocumentTabViewModel : ObservableObject
{
    private readonly IMessenger _messenger;

    public DocumentTabViewModel(IMessenger messenger, string documentId, string title)
    {
        _messenger = messenger;
        DocumentId = documentId;
        Title = title;
    }

    public string DocumentId { get; }
    public string Title { get; }

    [RelayCommand]
    private void Activate()
    {
        _messenger.Send(
            new DocumentActivatedMessage(DocumentId, Title),
            Channels.Document);
    }

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _statusText = "Ready";
}
```

### Receiver — StatusBarViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class StatusBarViewModel : ObservableObject,
    IRecipient<DocumentActivatedMessage>
{
    public StatusBarViewModel()
    {
        WeakReferenceMessenger.Default
            .Register<DocumentActivatedMessage>(this, Channels.Document);
    }

    public void Receive(DocumentActivatedMessage message)
    {
        CurrentDocument = message.Title;
        DocumentCount = _openDocuments;
    }

    [ObservableProperty]
    private string _currentDocument = "(none)";

    [ObservableProperty]
    private int _documentCount;
}
```

### Receiver — TitleBarViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class TitleBarViewModel : ObservableObject,
    IRecipient<DocumentActivatedMessage>
{
    public TitleBarViewModel()
    {
        WeakReferenceMessenger.Default
            .Register<DocumentActivatedMessage>(this, Channels.Document);
    }

    public void Receive(DocumentActivatedMessage message)
    {
        WindowTitle = $"{message.Title} — MyApp";
    }

    [ObservableProperty]
    private string _windowTitle = "MyApp";
}
```

### XAML — Shell View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:ShellViewModel">

  <Grid RowDefinitions="Auto,*,Auto">
    <!-- Title bar (receives DocumentActivatedMessage) -->
    <ContentControl Grid.Row="0"
                    Content="{Binding TitleBar}"
                    ContentTemplate="{StaticResource ViewLocatorTemplate}" />

    <!-- Document tabs -->
    <TabControl Grid.Row="1" ItemsSource="{Binding Documents}">
      <TabControl.ItemTemplate>
        <DataTemplate x:DataType="vm:DocumentTabViewModel">
          <TextBlock Text="{Binding Title}" />
        </DataTemplate>
      </TabControl.ItemTemplate>
      <TabControl.ContentTemplate>
        <DataTemplate x:DataType="vm:DocumentTabViewModel">
          <!-- Clicking the tab calls ActivateCommand -->
          <Button Content="Activate"
                  Command="{Binding ActivateCommand}" />
        </DataTemplate>
      </TabControl.ContentTemplate>
    </TabControl>

    <!-- Status bar (receives DocumentActivatedMessage) -->
    <ContentControl Grid.Row="2"
                    Content="{Binding StatusBar}"
                    ContentTemplate="{StaticResource ViewLocatorTemplate}" />
  </Grid>
</Window>
```

### How It Works

1. When the user selects a document tab, `ActivateCommand` fires. The `DocumentTabViewModel` sends a `DocumentActivatedMessage` on the `Channels.Document` channel.
2. `StatusBarViewModel` and `TitleBarViewModel` are both registered as recipients for `DocumentActivatedMessage` on the same channel. Both receive the message and update their respective properties.
3. The token (channel) scoping prevents a `DocumentActivatedMessage` from being intercepted by other message handlers in the app that might also listen for this message type but belong to a different subsystem.
4. Each receiver is independent — adding a new receiver (e.g., a telemetry tracker) requires no changes to the sender.

### Design Decisions & Edge Cases

- **Why token-based channel instead of separate message types:** `DocumentActivatedMessage` is the same logical event. The token differentiates the document subsystem from, say, a settings subsystem that might also use `DocumentActivatedMessage`. This avoids type explosion.
- **`IMessenger` injection vs `WeakReferenceMessenger.Default`:** Constructor injection lets unit tests substitute a mock messenger. `Default` is convenient but couples the ViewModel to the static instance.
- **Edge case — tab closed while message in flight:** The messenger dispatches synchronously, so the message is delivered before `Activate` returns. If the tab is closed immediately after, the receivers have already processed the event.
- **Edge case — receiver not yet created:** If `StatusBarViewModel` is lazy-created and not registered when the message is sent, it never receives it. Initialize receivers before the UI is interactive, or use `ObservableRecipient` with `IsActive` to buffer messages.

---

## Example 2: Background Task Progress with Multiple Receivers

### Goal

A long-running export operation broadcasts progress updates. The progress bar ViewModel, the status bar ViewModel, and a log window all receive progress messages independently.

### Message Types

```csharp
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MyApp.Messages;

public sealed class ExportProgressMessage : ValueChangedMessage<int>
{
    public ExportProgressMessage(int progress, string stage)
        : base(progress)
    {
        Stage = stage;
    }

    public string Stage { get; }
    public int Percent => Value;
}

public sealed class ExportCompletedMessage : ValueChangedMessage<bool>
{
    public ExportCompletedMessage(bool success, string? error = null)
        : base(success)
    {
        Error = error;
    }

    public string? Error { get; }
}
```

### Sender — ExportViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isExporting;

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsExporting) return;
        IsExporting = true;

        try
        {
            var total = 100;
            for (var i = 1; i <= total; i++)
            {
                // Simulate work
                await Task.Delay(50);

                var stage = i < 30 ? "Preparing"
                    : i < 70 ? "Processing"
                    : "Finalizing";

                WeakReferenceMessenger.Default
                    .Send(new ExportProgressMessage(i, stage));
            }

            WeakReferenceMessenger.Default
                .Send(new ExportCompletedMessage(true));
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default
                .Send(new ExportCompletedMessage(false, ex.Message));
        }
        finally
        {
            IsExporting = false;
        }
    }
}
```

### Receiver 1 — ProgressBarViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class ProgressBarViewModel : ObservableObject,
    IRecipient<ExportProgressMessage>,
    IRecipient<ExportCompletedMessage>
{
    public ProgressBarViewModel()
    {
        WeakReferenceMessenger.Default
            .Register<ExportProgressMessage>(this);
        WeakReferenceMessenger.Default
            .Register<ExportCompletedMessage>(this);
    }

    public void Receive(ExportProgressMessage message)
    {
        Progress = message.Percent;
        Stage = message.Stage;
        IsIndeterminate = false;
    }

    public void Receive(ExportCompletedMessage message)
    {
        if (message.Value)
        {
            Progress = 100;
            Stage = "Complete";
        }
        else
        {
            Stage = $"Failed: {message.Error}";
        }
    }

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private string _stage = "Idle";

    [ObservableProperty]
    private bool _isIndeterminate;
}
```

### Receiver 2 — StatusBarViewModel (same as Example 1, extended)

```csharp
public partial class StatusBarViewModel : ObservableObject,
    IRecipient<ExportProgressMessage>,
    IRecipient<ExportCompletedMessage>,
    IRecipient<DocumentActivatedMessage>
{
    public StatusBarViewModel()
    {
        WeakReferenceMessenger.Default
            .Register<ExportProgressMessage>(this);
        WeakReferenceMessenger.Default
            .Register<ExportCompletedMessage>(this);
        WeakReferenceMessenger.Default
            .Register<DocumentActivatedMessage>(this, Channels.Document);
    }

    public void Receive(ExportProgressMessage message)
    {
        ExportStatus = $"{message.Stage}... {message.Percent}%";
    }

    public void Receive(ExportCompletedMessage message)
    {
        ExportStatus = message.Value ? "Export complete" : $"Export failed";
    }

    public void Receive(DocumentActivatedMessage message)
    {
        CurrentDocument = message.Title;
    }

    [ObservableProperty]
    private string _currentDocument = "(none)";

    [ObservableProperty]
    private string _exportStatus = "Ready";
}
```

### XAML — Progress Bar View

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:vm="using:MyApp.ViewModels"
            x:DataType="vm:ProgressBarViewModel"
            Spacing="8" Margin="20">

  <!-- Progress bar with dynamic fill -->
  <ProgressBar Value="{Binding Progress}"
               Minimum="0" Maximum="100"
               Height="24" />

  <!-- Stage label -->
  <TextBlock Text="{Binding Stage}"
             FontSize="14"
             HorizontalAlignment="Center" />
</StackPanel>
```

### How It Works

1. `ExportViewModel` runs the export on the UI thread (or marshals updates to it). For each percentage point, it sends `ExportProgressMessage` with the current value and stage name.
2. `ProgressBarViewModel` receives the message and updates `Progress` and `Stage`. The `ProgressBar` binding re-evaluates.
3. `StatusBarViewModel` receives the same message and updates its own `ExportStatus` text — independently of the progress bar ViewModel.
4. When the export finishes (or fails), `ExportCompletedMessage` is sent. Both receivers handle completion: the progress bar shows "Complete" or "Failed", the status bar shows a summary.
5. The `DocumentActivatedMessage` from Example 1 also routes to `StatusBarViewModel` via the `Channels.Document` token — no conflict because the message types are different.

### Design Decisions & Edge Cases

- **Why two message types instead of one with IsComplete flag:** Separating `ExportProgressMessage` from `ExportCompletedMessage` lets receivers opt into one or both. A logger might only care about completion; a progress bar cares about both.
- **Sync dispatch on UI thread:** The messenger dispatches on the caller's thread. Because `ExportAsync` runs the loop on the UI thread (no `Task.Run`), the receivers process each message on the UI thread and can update properties directly. If the export were on a background thread, receivers would need `Dispatcher.UIThread.Post`.
- **Edge case — rapid progress updates (50ms):** The messenger invokes each receiver synchronously. With 100 iterations and 2 receivers, the loop adds ~1ms overhead per iteration — negligible. If there were 50 receivers, batch progress updates (e.g., every 5%) or use a coalescing mechanism.
- **Edge case — ExportViewModel is destroyed mid-export:** If the user closes the window, the ViewModel may be garbage collected. `WeakReferenceMessenger` handles this — the GC collects the dead ViewModel, and the messenger skips it silently.
- **Trade-off:** The sender knows the message types but not the receivers. This is the core decoupling benefit. The trade-off is that debugging message flows is harder — use named message types and a messenger interceptor in debug builds.

---

## Comparison

| Aspect | Example 1 — Tab Activation | Example 2 — Export Progress |
|---|---|---|
| **Message type** | `ValueChangedMessage<string>` with channel token | `ValueChangedMessage<int>` + separate completion message |
| **Channel/token** | Yes (`Channels.Document`) | No (global) |
| **Number of receivers** | 2 (status bar, title bar) | 2 (progress bar, status bar) |
| **Threading** | UI thread (user action) | UI thread (async loop) |
| **Lifetime** | Receivers live for app duration | Progress bar may be created/destroyed per export |
| **Sender lifetime** | Per-tab, transient | Singleton or scoped to export session |
| **When to use** | Scoped subsystem communication | Broadcast events, multiple UI surfaces |
| **Key risk** | Token mismatch (sender/receiver use different channel) | Receiver not registered before first message |

---

## See Also

- [014 — IMessenger Patterns (original)](014-imessenger-patterns.md)
- [014V — IMessenger Patterns (verbose companion)](014-imessenger-patterns-verbose.md)
- [013 — Data Validation](013-data-validation.md)
- [016 — Window & Dialog Management](016-window-dialog-management.md)
- [CommunityToolkit.Mvvm Docs: Messenger](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
