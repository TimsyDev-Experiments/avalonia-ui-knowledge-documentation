---
tier: intermediate
topic: keyboard
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 055 — Keyboard & Hotkeys

**What you'll learn:** How to define keyboard shortcuts in Avalonia using `KeyBinding` and `HotKey`, how `KeyGesture` works, scoped vs global shortcuts, and platform portability.

**Prerequisites:** [002 — Command Binding](../basics/002-command-binding.md), [051 — Routed Events](051-routed-events.md)

---

## 1. Two ways to wire a shortcut

| Approach | Scope | Use case |
|----------|-------|----------|
| `HotKey` on `ICommandSource` | Application-wide | Menu items, toolbar buttons |
| `KeyBinding` in `KeyBindings` | Focus-scoped | List-level shortcuts, window-level shortcuts |

---

## 2. HotKey (attached property)

Controls that implement `ICommandSource` (e.g., `MenuItem`, `Button`) have a `HotKey` property. The shortcut fires the control's command regardless of focus.

```xml
<MenuItem Header="_Save"
          Command="{Binding SaveCommand}"
          HotKey="Ctrl+S" />
```

Supported modifier synonyms: `Ctrl` = `Control`, `Win` = `Meta`, `Cmd` (macOS).

The underscore in `Header="_Save"` creates an access key (Alt+S on Windows/Linux). This is separate from `HotKey` — access keys require the Alt modifier and require the control to be visible in a menu.

---

## 3. KeyBinding (XAML)

`KeyBinding` maps a gesture to a command on any control's `KeyBindings` collection. The shortcut only fires when the control (or a child) has focus.

```xml
<Window.KeyBindings>
  <KeyBinding Gesture="Ctrl+N" Command="{Binding NewCommand}" />
  <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
  <KeyBinding Gesture="Delete" Command="{Binding DeleteCommand}" />
</Window.KeyBindings>
```

### Scoped KeyBindings

Attach `KeyBindings` to a specific control to scope the shortcut:

```xml
<ListBox>
  <ListBox.KeyBindings>
    <KeyBinding Gesture="Delete"
                Command="{Binding DeleteItemCommand}" />
    <KeyBinding Gesture="F2"
                Command="{Binding RenameCommand}" />
  </ListBox.KeyBindings>
</ListBox>
```

`Delete` only fires when the `ListBox` or one of its items has focus.

### CommandParameter

```xml
<KeyBinding Gesture="Ctrl+1"
            Command="{Binding SwitchTabCommand}"
            CommandParameter="0" />
```

---

## 4. KeyGesture syntax

The `Gesture` string is parsed as a `KeyGesture`. Format:

```
[Modifiers+]Key
```

| Part | Examples |
|------|----------|
| Modifiers | `Ctrl`, `Shift`, `Alt`, `Ctrl+Shift` |
| Key | `S`, `Delete`, `F1`, `Enter`, `Space`, `Escape` |

```xml
<KeyBinding Gesture="Ctrl+Shift+S" Command="{Binding SaveAsCommand}" />
<KeyBinding Gesture="F5" Command="{Binding RefreshCommand}" />
<KeyBinding Gesture="Ctrl+Alt+Del" Command="{Binding ResetCommand}" />
```

---

## 5. KeyBinding in code

```csharp
var keyBinding = new KeyBinding
{
    Gesture = new KeyGesture(Key.S, KeyModifiers.Control),
    Command = SaveCommand,
};

myWindow.KeyBindings.Add(keyBinding);
```

### KeyGesture constructor

```csharp
new KeyGesture(Key.S, KeyModifiers.Control | KeyModifiers.Shift);
```

---

## 6. CanExecute integration

`KeyBinding` respects `ICommand.CanExecute`. The command only fires when `CanExecute` returns `true`. If `CanExecute` changes while the shortcut is pressed, the binding re-evaluates automatically via `CommandManager.InvalidateRequerySuggested`.

```csharp
// Force CanExecute re-evaluation
CommandManager.InvalidateRequerySuggested();
```

---

## 7. Platform portability

| Platform | Notes |
|----------|-------|
| macOS | Use `Cmd` for platform conventions; `Ctrl` still works |
| Linux/X11 | Works; browser-reserved keys unavailable on WASM |
| Browser (WASM) | `Ctrl+T`, `Ctrl+W` and similar browser shortcuts cannot be intercepted |
| Mobile | No effect without physical keyboard |

To support both `Ctrl` and `Cmd` on macOS:

```xml
<KeyBinding Gesture="Ctrl+Z" Command="{Binding UndoCommand}" />
<KeyBinding Gesture="Cmd+Z" Command="{Binding UndoCommand}" />
```

---

## 8. Built-in key bindings

Several controls include built-in keyboard handling:

| Control | Built-in keys |
|---------|--------------|
| `TextBox` / `TextBox` | `Ctrl+Z` (undo), `Ctrl+Y`/`Ctrl+Shift+Z` (redo), `Ctrl+X` (cut), `Ctrl+C` (copy), `Ctrl+V` (paste), `Ctrl+A` (select all) |
| `ListBox` | Up/Down (selection), Home/End |
| `DataGrid` | All of the above plus Tab/Shift+Tab for cell navigation |
| `NumericUpDown` | Up/Down (increment/decrement) |

---

## Key Takeaways

- `HotKey` on `ICommandSource` for app-wide shortcuts on visible controls
- `KeyBinding` in `KeyBindings` for focus-scoped shortcuts
- Gesture syntax: `[Modifiers+]Key` — `Ctrl+S`, `Ctrl+Shift+Z`, `F1`
- `KeyBinding` respects `CanExecute` automatically
- Scope shortcuts to specific controls by adding to their `KeyBindings` collection

---

## See Also

- [055V — Keyboard & Hotkeys (verbose companion)](055-keyboard-hotkeys-verbose.md)
- [055E — Keyboard & Hotkeys (examples)](055-keyboard-hotkeys-examples.md)
- [Avalonia Docs: Keyboard and Hotkeys](https://docs.avaloniaui.net/docs/input-interaction/keyboard-and-hotkeys)
- [Avalonia Docs: Commanding](https://docs.avaloniaui.net/docs/input-interaction/commanding)
