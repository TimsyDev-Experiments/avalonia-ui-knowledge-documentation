---
tier: intermediate
topic: windows
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 016-window-dialog-management.md
---

# 016E — Window & Dialog Management: Real-World Examples

**What this is:** Two worked examples of decoupled dialog and window management. Read [016 — Window & Dialog Management](016-window-dialog-management.md) and [016V — Verbose Companion](016-window-dialog-management-verbose.md) first.

---

## Example 1: Settings Dialog with Async Validation and Result

### Goal

Open a settings dialog where the user edits configuration values. The dialog performs async validation (e.g., test database connection) before allowing the user to save. The result is returned to the caller via `IDialogService`.

### IDialogService Implementation

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.Services;

public interface IDialogService
{
    Task<TResult?> ShowDialog<TResult>(ViewModelBase viewModel, Window owner)
        where TResult : class;
}

public class DialogService : IDialogService
{
    private readonly IServiceProvider _services;
    private readonly Dictionary<Type, Type> _mappings = new()
    {
        { typeof(SettingsViewModel), typeof(SettingsWindow) },
    };

    public DialogService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<TResult?> ShowDialog<TResult>(ViewModelBase viewModel, Window owner)
        where TResult : class
    {
        if (!_mappings.TryGetValue(viewModel.GetType(), out var windowType))
            throw new InvalidOperationException($"No window registered for {viewModel.GetType()}");

        var window = (Window)Activator.CreateInstance(windowType)!;
        window.DataContext = viewModel;

        return await window.ShowDialog<TResult?>(owner);
    }
}
```

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MyApp.Messages;

namespace MyApp.ViewModels;

public partial class SettingsViewModel : ObservableValidator
{
    private readonly IConfigService _config;
    private readonly IMessenger _messenger;

    public SettingsViewModel(IConfigService config, IMessenger messenger)
    {
        _config = config;
        _messenger = messenger;
    }

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private int _port = 8080;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionStatus = "Testing...";
        try
        {
            var ok = await _config.TestConnectionAsync(ServerUrl, Port, ApiKey);
            ConnectionStatus = ok ? "✓ Connected" : "✗ Failed";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"✗ {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        await _config.SaveAsync(ServerUrl, Port, ApiKey);
        _messenger.Send(new DialogResultMessage<SettingsResult>(
            new SettingsResult(ServerUrl, Port, ApiKey)));
    }

    [RelayCommand]
    private void Cancel()
    {
        _messenger.Send(new DialogResultMessage<SettingsResult>(null));
    }
}

public record SettingsResult(string? ServerUrl, int Port, string? ApiKey);
```

### Message Type and Code-Behind

```csharp
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MyApp.Messages;

public sealed class DialogResultMessage<T> : ValueChangedMessage<T?>
    where T : class
{
    public DialogResultMessage(T? value) : base(value) { }
}
```

```csharp
// SettingsWindow.xaml.cs
using MyApp.Messages;

namespace MyApp.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            WeakReferenceMessenger.Default
                .Register<DialogResultMessage<SettingsResult>>(this, (r, m) =>
                {
                    Close(m.Value);
                })
                .DisposeWith(disposables);
        });
    }
}
```

### XAML View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:SettingsViewModel"
        Title="Settings" Width="450" Height="350"
        WindowStartupLocation="CenterOwner">

  <Grid RowDefinitions="*,Auto" Margin="20" Spacing="12">
    <StackPanel Spacing="8">
      <TextBox Text="{Binding ServerUrl, Mode=TwoWay}"
               Watermark="Server URL" />
      <TextBox Text="{Binding Port, Mode=TwoWay}"
               Watermark="Port" />
      <TextBox Text="{Binding ApiKey, Mode=TwoWay}"
               Watermark="API Key" />

      <Button Content="Test Connection"
              Command="{Binding TestConnectionCommand}"
              IsEnabled="{Binding IsTestingConnection, Converter={StaticResource InvertBool}}" />
      <TextBlock Text="{Binding ConnectionStatus}" FontSize="12" />
    </StackPanel>

    <StackPanel Grid.Row="1" Orientation="Horizontal"
                HorizontalAlignment="Right" Spacing="8">
      <Button Content="Save"
              Command="{Binding SaveCommand}" />
      <Button Content="Cancel"
              Command="{Binding CancelCommand}" />
    </StackPanel>
  </Grid>
</Window>
```

### How It Works

1. The caller creates a `SettingsViewModel` and passes it to `IDialogService.ShowDialog<SettingsResult>(vm, owner)`.
2. `DialogService` resolves `SettingsWindow` from the mapping, sets the DataContext, and calls `ShowDialog<SettingsResult?>`.
3. The user edits settings. The "Test Connection" button runs async validation without closing the dialog.
4. On "Save", the ViewModel validates, saves, and sends a `DialogResultMessage<SettingsResult>` with the result.
5. The code-behind receives the message and calls `Close(result)`. The `ShowDialog<T>` task completes with the result.
6. On "Cancel", the ViewModel sends a `null` result. The code-behind calls `Close(null)`. The caller checks for `null` to detect cancellation.

### Design Decisions & Edge Cases

- **Why `DialogResultMessage<T>` instead of a direct close callback:** The messenger keeps the ViewModel completely unaware of the window. The same ViewModel can be used with different dialog hosting strategies (window, content dialog, test harness).
- **Why a separate `SettingsResult` record instead of the ViewModel itself:** The ViewModel may hold transient state (connection status) that should not leak to the caller. A dedicated result type documents exactly what the caller receives.
- **Edge case — user closes via X button:** `Close()` is not called. The window's `ShowDialog<T?>` completes with `default` (null). The caller handles this identically to Cancel.
- **Edge case — multiple Save clicks:** The Save command disables automatically if you add `[RelayCommand(CanExecute = nameof(CanSave))]` that checks `!HasErrors && !IsTestingConnection`. Without it, guard at the top of `SaveAsync`.

---

## Example 2: WindowManager for Tool Windows (Inspector + Log Viewer)

### Goal

An application with floating tool windows — a property inspector and a log viewer. Each tool window can be opened/closed independently. The `WindowManager` ensures only one instance per tool type exists and tracks open windows.

### WindowManager

```csharp
using Avalonia.Controls;

namespace MyApp.Services;

public interface IWindowManager
{
    void Show<TWindow>(string key, Func<Window> factory) where TWindow : Window;
    void Close(string key);
    bool IsOpen(string key);
}

public class WindowManager : IWindowManager
{
    private readonly Dictionary<string, Window> _windows = new();

    public void Show<TWindow>(string key, Func<Window> factory) where TWindow : Window
    {
        if (_windows.TryGetValue(key, out var existing))
        {
            existing.Activate();
            return;
        }

        var window = factory();
        window.Closed += (_, _) => _windows.Remove(key);
        _windows[key] = window;
        window.Show();
    }

    public void Close(string key)
    {
        if (_windows.TryGetValue(key, out var window))
        {
            window.Close();
            _windows.Remove(key);
        }
    }

    public bool IsOpen(string key) => _windows.ContainsKey(key);
}
```

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IWindowManager _windows;
    private readonly IServiceProvider _services;

    public ShellViewModel(IWindowManager windows, IServiceProvider services)
    {
        _windows = windows;
        _services = services;
    }

    [RelayCommand]
    private void OpenInspector()
    {
        _windows.Show<InspectorWindow>("inspector", () =>
        {
            var vm = _services.GetRequiredService<InspectorViewModel>();
            return new InspectorWindow { DataContext = vm };
        });
    }

    [RelayCommand]
    private void OpenLogViewer()
    {
        _windows.Show<LogViewerWindow>("log", () =>
        {
            var vm = _services.GetRequiredService<LogViewerViewModel>();
            return new LogViewerWindow { DataContext = vm };
        });
    }
}

public partial class InspectorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedElement = "(none)";

    [ObservableProperty]
    private string _properties = string.Empty;

    [RelayCommand]
    private void Refresh() { /* query selected element */ }
}

public partial class LogViewerViewModel : ObservableObject
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    [ObservableProperty]
    private string _filter = string.Empty;

    [RelayCommand]
    private void Clear() => Entries.Clear();
}

public record LogEntry(string Timestamp, string Level, string Message);
```

### XAML — Shell View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:ShellViewModel"
        Title="MyApp" Width="1024" Height="768">

  <Grid RowDefinitions="Auto,*">
    <!-- Toolbar -->
    <StackPanel Orientation="Horizontal" Spacing="8" Margin="12">
      <Button Content="Inspector"
              Command="{Binding OpenInspectorCommand}" />
      <Button Content="Log Viewer"
              Command="{Binding OpenLogViewerCommand}" />
    </StackPanel>

    <!-- Main content area -->
    <ContentControl Grid.Row="1"
                    Content="{Binding CurrentView}"
                    ContentTemplate="{StaticResource ViewLocatorTemplate}" />
  </Grid>
</Window>
```

### How It Works

1. Clicking "Inspector" calls `ShellViewModel.OpenInspectorCommand`. The `WindowManager.Show<InspectorWindow>` checks if "inspector" is already open.
2. If not open, it creates a new `InspectorWindow` via the factory (resolving the ViewModel from DI), stores it in the dictionary, and calls `Show()`.
3. If already open, it calls `Activate()` on the existing window — no duplicate window.
4. When the user closes a tool window, the `Closed` event removes its key from the dictionary. The next time the user opens it, a fresh window is created.

### Design Decisions & Edge Cases

- **Why `key` string instead of `Type` for lookup:** Multiple windows of the same type could be open (e.g., two inspector windows for different selections). A string key supports both `Type`-based deduplication ("inspector") and instance-based ("inspector:element42").
- **Why `IWindowManager` interface:** The shell ViewModel depends on an interface, not a concrete `WindowManager`. Unit tests can mock it to verify that `Show` is called with the correct key.
- **Edge case — window closed externally:** The user closes the window via Alt+F4 or the system close button. The `Closed` event fires, and the dictionary is cleaned up. No stale references remain.
- **Edge case — application shutdown:** The `WindowManager` does not need to close windows explicitly. When the main window closes, Avalonia shuts down the dispatcher, and all windows close. The dictionary is discarded.
- **Trade-off:** The `WindowManager` holds strong references to windows. If a ViewModel forgets to close its window, the window stays in memory. This is acceptable because tool windows are typically few (2–5) and have the same lifetime as the app.

---

## Comparison

| Aspect | Example 1 — Settings Dialog | Example 2 — Tool Windows |
|---|---|---|
| **Pattern** | `IDialogService` + messenger-based close | `IWindowManager` with keyed tracking |
| **Window type** | Modal dialog (`ShowDialog`) | Modeless tool window (`Show`) |
| **Return value** | Yes (`Task<TResult?>`) | No (fire-and-forget open) |
| **ViewModel-to-Window coupling** | None (messenger bridge) | None (factory creates window) |
| **Deduplication** | N/A (new dialog each time) | Yes (singleton per key) |
| **When to use** | Settings, confirmations, data entry | Inspector, log viewer, palette, toolbars |
| **Key risk** | Null result handling on cancellation | Window reference leak if `Closed` event not wired |

---

## See Also

- [016 — Window & Dialog Management (original)](016-window-dialog-management.md)
- [016V — Window & Dialog Management (verbose companion)](016-window-dialog-management-verbose.md)
- [010 — Window Basics & Simple Dialogs](../basics/010-window-dialog-basics.md)
- [014 — IMessenger Patterns](014-imessenger-patterns.md)
- [018 — Navigation Patterns](018-navigation.md)
