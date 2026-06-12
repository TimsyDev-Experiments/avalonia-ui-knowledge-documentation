---
tier: basics
topic: windows
estimated: 5 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 010 — Window Basics and Simple Dialogs

**What you'll learn:** Open a second window, show a message dialog, and pass data between windows.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

---

## 1. Open a second window from a command

```csharp
[RelayCommand]
private void OpenSettings()
{
    var settings = new SettingsWindow
    {
        DataContext = new SettingsViewModel()
    };
    settings.Show();
}
```

For a modal dialog:

```csharp
[RelayCommand]
private async Task OpenSettingsDialogAsync()
{
    var settings = new SettingsWindow
    {
        DataContext = new SettingsViewModel()
    };
    await settings.ShowDialog(GetTopLevel());
}

private Window? GetTopLevel() =>
    TopLevel.GetTopLevel(/* reference to a control in the current window */);
```

> Avalonia 12 changed `TopLevel` access — use `TopLevel.GetTopLevel(Visual)` instead of casting to `TopLevel` directly.

---

## 2. ShowDialog and return a result

```csharp
// In the dialog window
public string? Result { get; set; }

[RelayCommand]
private void Confirm()
{
    Result = "User confirmed";
    Close(true);  // DialogResult
}

[RelayCommand]
private void Cancel()
{
    Close(false);
}
```

```csharp
// Calling code
var dialog = new InputDialog();
dialog.DataContext = new InputDialogViewModel();
var result = await dialog.ShowDialog<bool?>(parentWindow);
```

---

## 3. Built-in dialogs

```csharp
using Avalonia.Controls;
using Avalonia.Platform.Storage;

// File open
var files = await TopLevel.GetTopLevel(this)!.StorageProvider
    .OpenFilePickerAsync(new FilePickerOpenOptions
    {
        Title = "Select a file",
        AllowMultiple = false,
        FileTypeFilter = new[] { FileTypes.All }
    });

// Folder picker
var folders = await TopLevel.GetTopLevel(this)!.StorageProvider
    .OpenFolderPickerAsync(new FolderPickerOpenOptions());
```

> The storage provider API replaced the older `OpenFileDialog` class. It works on all platforms.

---

## 4. Window life cycle events

| Event | When |
|---|---|
| `Window.Opened` | After the window is shown |
| `Window.Closing` | Before close (can cancel) |
| `Window.Closed` | After close |
| `Window.Activated` | Window gets focus |
| `Window.Deactivated` | Window loses focus |

---

## Key Takeaways

- Use `Show()` for modeless, `ShowDialog(parent)` for modal
- `TopLevel.GetTopLevel(visual)` is the correct way to access the top level in v12
- `StorageProvider` replaces old file dialog APIs
- Close a dialog with a result: `Close(value)`

---

## See Also

- [015 — Window & Dialog Management](../intermediate/015-window-dialog-management.md)
- [048 — TopLevel, Window, and Runtime Services](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/48-toplevel-window-and-runtime-services.md)
- [Avalonia Docs: Windows](https://docs.avaloniaui.net/docs/windows/)
