---
tier: intermediate
topic: windows
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 016 — Window & Dialog Management

**What you'll learn:** Manage multiple windows, handle window-to-ViewModel communication, and use service-based dialog patterns with DI.

**Prerequisites:** [010 — Window Basics & Simple Dialogs](../basics/010-window-dialog-basics.md)

---

## 1. The DialogService pattern

Create an interface and implementation so ViewModels can open dialogs without knowing about views:

```csharp
// Services/IDialogService.cs
public interface IDialogService
{
    Task<T?> ShowDialog<T>(ViewModelBase viewModel, Window owner);
}
```

```csharp
// Services/DialogService.cs
using Avalonia.Controls;

public class DialogService : IDialogService
{
    private readonly Dictionary<Type, Type> _mappings = new()
    {
        { typeof(SettingsViewModel), typeof(SettingsWindow) },
        { typeof(ConfirmViewModel), typeof(ConfirmDialog) },
    };

    public async Task<T?> ShowDialog<T>(ViewModelBase viewModel, Window owner)
    {
        if (!_mappings.TryGetValue(viewModel.GetType(), out var windowType))
            throw new InvalidOperationException($"No window registered for {viewModel.GetType()}");

        var window = (Window)Activator.CreateInstance(windowType)!;
        window.DataContext = viewModel;

        return await window.ShowDialog<T?>(owner);
    }
}
```

---

## 2. DI registration

```csharp
// App.axaml.cs or composition root
services.AddSingleton<IDialogService, DialogService>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<SettingsWindow>();
```

---

## 3. Closing and returning data

```csharp
// In dialog ViewModel
[RelayCommand]
private void Confirm()
{
    // The dialog needs a reference to its window
    // Pass it via the ViewModel or use a messenger
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

---

## 4. WindowManager for complex scenarios

For apps with multiple windows (MDI-like), create a `WindowManager`:

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

---

## 5. Window state persistence

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
    // Save to file (JSON, settings, etc.)
}
```

Restore in the constructor or `OnOpened`:

```csharp
window.Opened += (_, _) =>
{
    if (settings is not null)
    {
        window.Position = new PixelPoint(settings.WindowX, settings.WindowY);
        window.Width = settings.WindowWidth;
        window.Height = settings.WindowHeight;
        window.WindowState = settings.WindowState;
    }
};
```

> `Window.WindowState` is a direct property in Avalonia 12 (was styled in v11) — you can no longer set it from a style.

---

## Key Takeaways

- `IDialogService` keeps ViewModels decoupled from window types
- Use `WeakReferenceMessenger` for ViewModel-to-window close signals
- Track open windows with a `WindowManager` for multi-window apps
- Persist window position/size in settings for user experience

---

## See Also

- [010 — Window Basics & Simple Dialogs](../basics/010-window-dialog-basics.md)
- [014 — IMessenger Patterns](014-imessenger-patterns.md)
- [016V — Window & Dialog Management (verbose companion)](016-window-dialog-management-verbose.md)
- [016E — Window & Dialog Management (examples)](016-window-dialog-management-examples.md)
- [018 — Navigation Patterns](018-navigation.md)
