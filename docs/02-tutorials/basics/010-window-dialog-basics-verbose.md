---
tier: basics
topic: windows
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 010-window-dialog-basics.md
---

# 010V — Window Basics and Simple Dialogs: An In-Depth Companion

**What you'll learn in this companion:** How `Window` and `TopLevel` relate in the Avalonia class hierarchy, why `ShowDialog` requires a parent, how `Window.Close(bool?)` sets the dialog result, the storage provider API design rationale, and the correct pattern for lifetime management of modal windows.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

**You should already have read:** [010 — Window Basics and Simple Dialogs](010-window-dialog-basics.md) for the quick-start version. This file goes deeper on every section.

---

## 1. The `TopLevel` Class Hierarchy

```csharp
public class Window : TopLevel
public class TopLevel : ContentControl
```

`TopLevel` is the base class for any element that sits at the root of a visual tree — it has no logical parent. There are three concrete `TopLevel` subclasses:

| Class | Platform | Created by |
|---|---|---|
| `Window` | Desktop (Windows, macOS, Linux) | Application code |
| `WindowBase` | Desktop (abstract, base of `Window`) | Internal |
| `TopLevel` itself | All (abstract) | Rarely instantiated directly |

`TopLevel` provides:

- **`StorageProvider`** — access to file/folder picker APIs.
- **`Clipboard`** — system clipboard access.
- **`Launcher`** — open files, URIs, or other apps.
- **`PlatformSettings`** — OS-level settings (color scheme, layout direction, animations enabled).
- **`Screens`** — screen geometry (bounds, working area, scaling).
- **`BackRequested`** — event for back navigation (mobile, browser).

When you see `TopLevel.GetTopLevel(visual)`, it walks the visual tree upward until it finds a `TopLevel` instance. This is the correct way to get the current `Window` from any control inside it. In Avalonia 11, you could cast `TopLevel.GetTopLevel(visual)` more freely; in Avalonia 12, the `TopLevel` access pattern was tightened because some properties moved to `TopLevel` that were previously on `Window`.

---

## 2. Why `Show()` and `ShowDialog()` Behave Differently

```csharp
settings.Show();                                           // modeless
await settings.ShowDialog(GetTopLevel());                  // modal
```

### `Window.Show()`

- Shows the window non-modally.
- Returns immediately. The calling code continues executing while the new window is visible.
- The new window has its own `TopLevel` — it can be minimized, maximized, and closed independently.
- No result is returned.
- If the parent window closes, modeless child windows are **not** automatically closed (they become orphaned unless you handle `Closing` on the parent).

### `Window.ShowDialog(Window parent)`

- Shows the window modally.
- Blocks input to the **parent** window (but not the entire app — other windows in the same process remain interactive).
- Returns a `Task<bool?>` — the value passed to `Close(bool?)` in the dialog.
- The calling method must be `async` to `await` the result.
- If the parent window closes while a modal dialog is open, the dialog is also closed (the `ShowDialog` task completes with `null`).

The parent window requirement is enforced: you cannot call `ShowDialog` without a valid parent `Window`. Passing `null` throws an `ArgumentNullException`. This is a design decision to prevent the window manager from having orphan modal dialogs on the taskbar.

### ShowDialog<TResult>

If your dialog needs to return a typed result beyond `bool?`, define a custom dialog class that exposes the result as a property:

```csharp
public class InputDialog : Window
{
    public string? InputResult { get; set; }
}

// Calling code
var dialog = new InputDialog();
dialog.DataContext = new InputDialogViewModel();
var _ = await dialog.ShowDialog<InputDialog?>(parentWindow);
// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
string? result = dialog.InputResult; // After close, read the property
```

The generic parameter `<TResult>` on `ShowDialog` returns the window instance itself after closing. You read the result from the dialog's public property after the `await` completes.

---

## 3. How `Close()` Sets the Dialog Result

```csharp
public string? Result { get; set; }

[RelayCommand]
private void Confirm()
{
    Result = "User confirmed";
    Close(true);
}

[RelayCommand]
private void Cancel()
{
    Close(false);
}
```

`Window.Close(bool? dialogResult)` is inherited from `WindowBase`. The `bool?` parameter:

- `true` — the dialog was accepted (e.g., OK, Yes, Confirm).
- `false` — the dialog was rejected (e.g., Cancel, No).
- `null` — the dialog was dismissed without an explicit choice (e.g., user clicked the X button, or the parent closed).

The `ShowDialog` task completes with this value. If the user clicks the title bar Close button, `Close()` is called with no argument — internally `Close(null)` is invoked, so the task returns `null`.

### Reading the Result

```csharp
var result = await dialog.ShowDialog<bool?>(parentWindow);
if (result == true)
{
    // User confirmed
    string data = dialog.Result;
}
else
{
    // User cancelled or dismissed
}
```

The `result` is `bool?`, not `bool`. You must check for `true` explicitly. `result == true` covers both `false` and `null` as "not confirmed."

---

## 4. `TopLevel.GetTopLevel()`: Correct Usage in Avalonia 12

```csharp
private Window? GetTopLevel() =>
    TopLevel.GetTopLevel(/* reference to a control in the current window */);
```

In Avalonia 11, you could call `TopLevel.GetTopLevel(this)` from a `Window` and it returned the window. You could also cast `ApplicationLifetime` to `IClassicDesktopStyleApplicationLifetime` and access `MainWindow` directly.

In Avalonia 12, `TopLevel.GetTopLevel(Visual)` is the recommended approach because:

1. It works from any control, not just `Window`. A `UserControl` nested three levels deep can find its containing `TopLevel` without knowing its type.
2. It correctly handles single-view applications (mobile/browser) where `MainWindow` is null.
3. It is forward-compatible — if the visual tree structure changes in a future version, `GetTopLevel` still works.

### What to Pass as the Visual

Pass the control that is currently in the visual tree:

```csharp
// In a ViewModel (NO access to visual tree)
// You cannot call TopLevel.GetTopLevel here — pass a visual reference.

// In a code-behind:
private async Task OpenDialog()
{
    var dialog = new SettingsWindow();
    await dialog.ShowDialog(this); // 'this' is the Window
}

// In a code-behind of a UserControl:
private async Task OpenDialog()
{
    var parentWindow = TopLevel.GetTopLevel(this) as Window;
    if (parentWindow is not null)
    {
        var dialog = new SettingsWindow();
        await dialog.ShowDialog(parentWindow);
    }
}
```

If you are in a ViewModel (no visual tree access), you need to pass a visual reference into the command. Common patterns:

- **Inject a service** that holds a reference to the active `TopLevel`.
- **Pass the parent window as `CommandParameter`** from XAML: `CommandParameter="{Binding $parent[Window]}"`.
- **Use `ApplicationLifetime`** to get `MainWindow` (works only for `IClassicDesktopStyleApplicationLifetime` with a single main window).

---

## 5. StorageProvider: The Replacement for File Dialog Classes

```csharp
var files = await TopLevel.GetTopLevel(this)!.StorageProvider
    .OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Select a file",
        AllowMultiple = false,
        FileTypeFilter = new[] { FileTypes.All }
    });
```

In Avalonia 11, file dialogs were `OpenFileDialog` and `SaveFileDialog` — classes that returned file paths as strings. In Avalonia 12, these were removed in favor of the cross-platform `IStorageProvider` API.

### Why the API Changed

- **Cross-platform consistency:** On Windows, file dialogs are native Win32 dialogs. On macOS, they are `NSOpenPanel`. On Linux, they are portal-based (XDG Desktop Portal) or GTK dialogs. The old `OpenFileDialog` class required platform-specific handling for each backend.
- **Async-first:** `IStorageProvider` returns `IReadOnlyList<IStorageFile>` using `Task`-based calls, which integrates with async command patterns.
- **File abstraction:** `IStorageFile` wraps the platform file handle, providing `OpenReadAsync()` and other methods without exposing string paths. This avoids path-encoding issues on non-Windows platforms.

### FilePickerOpenOptions Properties

| Property | Type | Purpose |
|---|---|---|
| `Title` | `string?` | Dialog window title |
| `AllowMultiple` | `bool` | Single or multi-select |
| `FileTypeFilter` | `IReadOnlyList<FilePickerFileType>?` | Filter by extension |
| `SuggestedFileName` | `string?` | Default filename (Save dialog) |
| `SuggestedStartLocation` | `IStorageFolder?` | Starting directory |
| `ShowAllFiles` | `bool` | Show files with unlisted extensions |

### FilePickerFileType Built-ins

`Avalonia.Platform.Storage.FilePickerFileTypes` provides:

- `All` — `*.*`
- `Images` — common image extensions
- `Text` — `.txt`
- `Pdf` — `.pdf`

Create custom filters:

```csharp
new FilePickerFileType("Log files")
{
    Patterns = new[] { "*.log", "*.txt" }
};
```

---

## 6. Window Lifecycle Events: What You Can and Cannot Do

| Event | Trigger | Available actions |
|---|---|---|
| `Opened` | After the window's first render | Start animations, focus first input, set initial state |
| `Closing` | User clicks X, or `Close()` is called | Cancel the close: set `e.Cancel = true`. Show "unsaved changes?" dialog. |
| `Closed` | After the window is fully closed | Dispose resources, save state, notify other ViewModels |
| `Activated` | Window receives focus | Pause/resume UI-bound operations |
| `Deactivated` | Window loses focus | Stop animations that waste CPU |

### Closing vs Closed

```csharp
protected override void OnClosing(WindowClosingEventArgs e)
{
    base.OnClosing(e);
    if (HasUnsavedChanges)
    {
        e.Cancel = true; // prevent close
        // Show confirmation dialog instead
    }
}
```

`OnClosing` is a cancelable event. `OnClosed` is not — the window is already gone. Use `OnClosed` for cleanup:

```csharp
protected override void OnClosed(EventArgs e)
{
    base.OnClosed(e);
    _cts?.Cancel();
    _cts?.Dispose();
}
```

### Opening a Dialog from the Closing Event

You cannot `await ShowDialog` inside `OnClosing` because the window is in the middle of its close sequence. The pattern is:

```csharp
protected override async void OnClosing(WindowClosingEventArgs e)
{
    base.OnClosing(e);
    if (HasUnsavedChanges)
    {
        e.Cancel = true; // prevent initial close
        var result = await new ConfirmDialog().ShowDialog(this);
        if (result == true)
            Close(); // retry close after confirmation
    }
}
```

---

## 7. Common Mistakes

1. **Calling `ShowDialog` without a parent window.** `ShowDialog(null)` throws. Always pass a valid parent window.
2. **Not awaiting `ShowDialog`.** If you call `ShowDialog` without `await`, the method returns immediately with a `Task<bool?>`. The dialog appears, but the calling code continues executing, and the `Task` is fire-and-forget (unobserved exceptions are swallowed). Always `await` the call.
3. **Creating a new `Window` instance every time the command runs without disposing the old one.** Show a dialog → close it → show it again → the same `Window` instance is reused. If you create a new `Window` each time, ensure the old one is closed and disposed. At minimum: `new SettingsWindow().ShowDialog(parent)`.
4. **Accessing `TopLevel.GetTopLevel(this)` from a ViewModel.** ViewModels have no visual reference. Pass a `Window` reference through the command parameter or use a service.
5. **Using `OpenFileDialog` (old API).** The `Avalonia.Controls.OpenFileDialog` class was removed in Avalonia 12. Use `StorageProvider.OpenFilePickerAsync`.
6. **Forgetting to set `DataContext` on the dialog window.** The dialog window is a new `Window` instance with a default `DataContext` of `null`. If you do not set `DataContext`, all bindings inside the dialog are dead. Pass the ViewModel explicitly: `dialog.DataContext = new SettingsViewModel()`.

---

## See Also

- [010 — Window Basics and Simple Dialogs (original tutorial)](010-window-dialog-basics.md)
- [010X — Window Basics and Simple Dialogs (examples)](010-window-dialog-basics-examples.md)
- [002 — Command Binding](002-command-binding.md)
- [002V — Command Binding (verbose companion)](002-command-binding-verbose.md)
- [016 — Window & Dialog Management](../intermediate/016-window-dialog-management.md)
- [Avalonia Docs: Windows](https://docs.avaloniaui.net/docs/windows/)
