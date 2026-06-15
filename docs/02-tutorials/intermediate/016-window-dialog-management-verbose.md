---
tier: intermediate
topic: windows
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 016-window-dialog-management.md
---

# 016V — Window & Dialog Management: An In-Depth Companion

**Why this exists:** The original tutorial shows the `DialogService` and `WindowManager` patterns. This companion explains *why the service-based pattern matters for testability*, *what each part of the dialog lifecycle does internally*, *how to handle dialog results without code-behind coupling*, and *when to use WindowManager vs. simpler alternatives*.

**Cross-reference:** Original tutorial at [016-window-dialog-management.md](016-window-dialog-management.md).

---

## 1. Why a DialogService — the ViewModel-first argument

```csharp
public interface IDialogService
{
    Task<T?> ShowDialog<T>(ViewModelBase viewModel, Window owner);
}
```

A ViewModel that opens a window directly (e.g., `new SettingsWindow { DataContext = this }.ShowDialog(owner)`) has three problems:

1. **Testability:** Unit tests cannot verify that the correct window opens — they would need a UI thread and a windowing platform.
2. **Coupling:** The ViewModel references a concrete `Window` subclass. Changing the window implementation means changing the ViewModel.
3. **DI bypass:** If the window has dependencies (e.g., a `SettingsViewModel` that needs `IConfigService`), the ViewModel must resolve them itself.

The `IDialogService` pattern solves all three:

- **Testability:** Tests mock `IDialogService` and verify that `ShowDialog<SettingsViewModel>` was called with the right argument.
- **Coupling:** The ViewModel knows only `IDialogService`, not any window type.
- **DI integration:** The `DialogService` resolves the window from the DI container, which supplies its dependencies.

**The mapping dictionary** in `DialogService`:

```csharp
private readonly Dictionary<Type, Type> _mappings = new()
{
    { typeof(SettingsViewModel), typeof(SettingsWindow) },
    { typeof(ConfirmViewModel), typeof(ConfirmDialog) },
};
```

This is the ViewModel-to-View mapping. When `ShowDialog<SettingsResult>(settingsViewModel, owner)` is called, the service looks up `settingsViewModel.GetType()` in the dictionary, finds `SettingsWindow`, creates an instance, assigns the `DataContext`, and calls `ShowDialog<T>`.

**Why not use the view locator pattern here?** A view locator (see [018 — Navigation](018-navigation.md)) maps ViewModel types to View types globally. The `DialogService` mapping serves the same purpose but scoped to dialogs. You could reuse the view locator for dialogs, but dialogs often have different lifetime requirements (modal vs. modeless, specific window properties like dialog frame).

---

## 2. Dialog lifecycle — what ShowDialog does internally

```csharp
return await window.ShowDialog<T?>(owner);
```

**What happens step by step:**

1. The `Window` is shown as a modal dialog. The calling thread (if UI thread) is not blocked — `ShowDialog` returns a `Task<T?>`.
2. A new dispatcher run loop starts for the dialog window. It processes input, layout, and rendering for the dialog independently from the owner.
3. The owner window is disabled (cannot receive input) but continues to render and process non-input messages.
4. When the user interacts with the dialog (e.g., clicks Close), the code-behind calls `Close(value)`.
5. `Close(value)` sets the dialog's `DialogResult` to `value`, closes the window, and completes the `Task<T?>`.
6. The caller resumes: `var result = await ShowDialog<T?>(...)`.

**Important:** You must call `ShowDialog` from the UI thread. Calling it from a background thread throws `InvalidOperationException`. Use `Dispatcher.UIThread.InvokeAsync` to marshal if needed.

---

## 3. Messenger-based close — the code-behind compromise

```csharp
// In dialog ViewModel
[RelayCommand]
private void Confirm()
{
    WeakReferenceMessenger.Default
        .Send(new DialogResultMessage(true));
}

// In dialog code-behind
public ConfirmDialog()
{
    InitializeComponent();
    this.WhenActivated(disposables =>
    {
        WeakReferenceMessenger.Default
            .Register<DialogResultMessage>(this, (r, m) =>
            {
                Close(m.Value);
            })
            .DisposeWith(disposables);
    });
}
```

**Why this pattern exists:** A ViewModel should not know about the `Window` class — it has no reference to the window to call `Close()`. The messenger bridges the gap: the VM sends a message, the code-behind receives it and calls `Close()` on the window.

**The role of `WhenActivated`:** This is an Avalonia reactive lifecycle helper. The lambda passed to `WhenActivated` runs when the view is attached to a visual tree (i.e., when the `Window` or `UserControl` is loaded). It receives an `IDisposable` accumulator — any disposable you add (like the messenger registration) is disposed when the view is unloaded.

**Why `.DisposeWith(disposables)`:** It ensures the messenger registration is automatically unregistered when the dialog closes. Without it, the registration would survive in the messenger until garbage collection (if using `WeakReferenceMessenger`) or forever (if using `StrongReferenceMessenger`).

**Alternative — passing a callback to the ViewModel:** Instead of using the messenger, you can pass an `Action<T>` callback to the ViewModel's constructor:

```csharp
public partial class ConfirmViewModel( Action<bool> onClose) : ViewModelBase
{
    [RelayCommand]
    private void Confirm()
    {
        onClose(true);
    }
}
```

And create the ViewModel in the code-behind:

```csharp
public ConfirmDialog()
{
    InitializeComponent();
    var vm = new ConfirmViewModel(result => Close(result));
    DataContext = vm;
}
```

This avoids the messenger entirely. The tradeoff: the ViewModel depends on a delegate, which is testable but slightly more coupled than a message.

---

## 4. WindowManager — why you need it for multi-window apps

```csharp
public class WindowManager
{
    private readonly Dictionary<string, Window> _openWindows = new();

    public void OpenOrActivate(string key, Func<Window> factory)
    {
        if (_openWindows.TryGetValue(key, out var existing))
        {
            existing.Activate();
            return;
        }

        var window = factory();
        window.Closed += (_, _) => _openWindows.Remove(key);
        _openWindows[key] = window;
        window.Show();
    }
}
```

**What this does:** Instead of creating a new window every time (which would pile up windows), `WindowManager` tracks open windows by key. If the window is already open, it brings it to the foreground (`Activate()`). If not, it creates one and registers it.

**When you need this:**

- Tool windows (inspector, properties panel, log viewer) — one instance per tool.
- Document windows (MDI-like) — one per document, reusing existing open documents.
- Floating panels that should not be duplicated.

**What happens to the window reference:** The `WindowManager` holds a strong reference to the `Window`. The `Closed` event handler removes it from the dictionary. Without the `Closed` handler, the dictionary would accumulate dead window references even after the user closes them.

**Thread safety:** The `WindowManager` is used from the UI thread only. If you need to open/close from background threads, marshal via `Dispatcher.UIThread`.

---

## 5. Window state persistence — the save/restore pattern

```csharp
public void SaveWindowState(Window window)
{
    var settings = new AppSettings
    {
        WindowX = window.Position.X,
        WindowY = window.Position.Y,
        WindowWidth = window.Width,
        WindowHeight = window.Height,
        WindowState = window.WindowState
    };
    // Save to file
}
```

**Why persist window state:** Users expect windows to reopen at the same position and size they left them. Persistence stores the window geometry in settings (JSON, registry, etc.) and restores it on the next launch.

**Position caveats:**

- `Window.Position` is in screen coordinates. If the user moves the window to a secondary monitor that is later disconnected, the restored position may place the window off-screen. Validate the position against `Screens.All`:

```csharp
var screens = Screens.All;
var isOnScreen = screens.Any(s =>
    s.Bounds.Contains(new PixelPoint(settings.WindowX, settings.WindowY)));
if (!isOnScreen)
{
    // Fall back to center of primary screen
    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
}
```

**State limitations in Avalonia 12:**

- `WindowState` is a direct property (not styled) in v12. You can set it in code but not in a style.
- `Window.WindowState` cannot be set before the window is shown. Set it in the `Opened` event handler.
- Window minimize/maximize animation may misbehave if you restore position while the window is minimized. Restore `WindowState` first, then position.

---

## 6. Alternative patterns

| Pattern | When to use |
|---|---|
| **DialogService (shown)** | Simple modal dialogs, single-window apps |
| **WindowManager (shown)** | Multiple windows, tool windows, MDI-like apps |
| **ContentDialog (WinUI-style)** | In-app modal overlays (non-window dialogs) |
| **TaskCompletionSource** | Async dialog result without a service |
| **Messenger-based orchestration** | Complex multi-step dialogs across VMs |

**ContentDialog alternative:** If you do not want to open a separate window (e.g., for a quick confirmation), use a `ContentControl` overlay within the same window. This avoids the overhead of a second window and the platform's window management (taskbar entries, alt-tab, etc.).

---

## 7. Common mistakes

**Mistake 1: Null owner in ShowDialog.**

`ShowDialog<T>` requires a non-null `Window owner`. Passing null throws. If the current window is not directly available, use `TopLevel.GetTopLevel(control)` or `VisualRoot` as the owner.

**Mistake 2: DialogService with transient windows.**

If `SettingsWindow` is registered as singleton, the same window instance is reused for every dialog — it cannot be shown twice. Register windows as transient.

**Mistake 3: Forgetting to handle dialog cancellation.**

If the user closes the dialog via the system close button (X) or Alt+F4, `Close()` is not called explicitly. The `ShowDialog<T>` task completes with `default(T?)` (null for reference types). Always check for null:

```csharp
var result = await _dialogService.ShowDialog<SettingsResult>(vm, owner);
if (result is null) return;  // user cancelled
```

**Mistake 4: Multiple ShowDialog calls on the same window.**

A window cannot be shown as a dialog twice. Once closed, it cannot be re-shown. Create a new window instance each time.

---

## Key Takeaways

- `IDialogService` abstracts window creation, keeping ViewModels testable and decoupled from window types.
- `ShowDialog<T>` returns a `Task<T?>`. Null means the user cancelled.
- Use `WeakReferenceMessenger` (or a callback) to let the ViewModel signal the window to close, since the VM has no window reference.
- `WindowManager` tracks open windows by key, preventing duplicates in multi-window apps.
- Persist window position/size and validate against `Screens.All` to handle monitor configuration changes.
- Register windows as transient in DI — each dialog needs a fresh instance.

---

## See Also

- [016 — Window & Dialog Management (original)](016-window-dialog-management.md)
- [010 — Window Basics & Simple Dialogs](../basics/010-window-dialog-basics.md)
- [014 — IMessenger Patterns](014-imessenger-patterns.md)
- [016E — Window & Dialog Management (examples)](016-window-dialog-management-examples.md)
- [018 — Navigation Patterns](018-navigation.md) (uses view locator for mapping, similar to dialog mapping)
