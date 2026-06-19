---
tier: intermediate
topic: keyboard
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 055-keyboard-hotkeys.md
---

# 055V — Keyboard & Hotkeys: An In-Depth Companion

**Why this exists:** The original tutorial covers the core `KeyBinding` and `HotKey` APIs. This companion explains gesture parsing, focus-scoping internals, `CommandManager` requery mechanics, the v12 `CommandBinding`/`RoutedCommand` system, platform-specific behavior, access keys vs hotkeys, and how Avalonia's approach compares to WPF's `CommandManager`.

**Cross-reference:** Original tutorial at [055-keyboard-hotkeys.md](055-keyboard-hotkeys.md).

---

## 1. How KeyGesture is parsed

When you set `Gesture="Ctrl+Shift+S"`, Avalonia parses the string using `KeyGesture.Parse` or the type converter.

### Parsing order

1. Split on `+` delimiters
2. Each token is checked against `KeyModifiers` enum (`Control`, `Shift`, `Alt`, `Meta`)
3. The last token is parsed as a `Key` enum value

### Modifier synonyms

| Token | Resolves to |
|-------|-------------|
| `Ctrl` | `KeyModifiers.Control` |
| `Control` | `KeyModifiers.Control` |
| `Shift` | `KeyModifiers.Shift` |
| `Alt` | `KeyModifiers.Alt` |
| `Win` | `KeyModifiers.Meta` |
| `Meta` | `KeyModifiers.Meta` |
| `Cmd` | `KeyModifiers.Meta` (macOS convention) |

### Common keys

`Enter`, `Return` (both work), `Escape`, `Space`, `Tab`, `Delete`, `Backspace`, `Home`, `End`, `PageUp`, `PageDown`, `F1`–`F24`, `Left`, `Up`, `Right`, `Down`, `0`–`9`, `A`–`Z`.

---

## 2. Focus scoping deeply

`KeyBinding` only fires when the element that owns the `KeyBindings` collection (or one of its visual descendants) has focus. This is the key difference from `HotKey`.

```xml
<!-- Window-level: fires when anything in the window has focus -->
<Window.KeyBindings>
  <KeyBinding Gesture="Ctrl+F" Command="{Binding FindCommand}" />
</Window.KeyBindings>

<!-- ListBox-level: fires only when ListBox or its items have focus -->
<ListBox.KeyBindings>
  <KeyBinding Gesture="Delete" Command="{Binding DeleteItemCommand}" />
</ListBox.KeyBindings>
```

### Focus scope walk

When a key is pressed:
1. The focused element's `KeyBindings` are checked first
2. If no match, the parent's `KeyBindings` are checked
3. Continues up the visual tree to the root (Window)

This means a `KeyBinding` on a `ListBox` can match before a `KeyBinding` on the `Window` with the same gesture, effectively overriding it when the `ListBox` has focus.

### Preventing event bubbling for keys

If you want to intercept a key that also has built-in handling (e.g., `Enter` in a `TextBox`), mark the event as handled:

```csharp
protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.Enter)
    {
        ExecuteSearch();
        e.Handled = true; // prevents KeyBinding from matching
    }
}
```

---

## 3. CommandManager.InvalidateRequerySuggested

`ICommand.CanExecute` is re-evaluated automatically when:
- Keyboard focus changes
- A command is executed
- The `CommandManager.RequerySuggested` event is raised

Call `CommandManager.InvalidateRequerySuggested()` to force immediate re-evaluation:

```csharp
private void OnSelectionChanged()
{
    // Force all commands to re-check CanExecute
    CommandManager.InvalidateRequerySuggested();
}
```

### When to call it manually

- After a long-running operation completes that changes command availability
- After a property that affects `CanExecute` changes outside of a command execution
- When focus-based command availability needs to update immediately

In most cases, you do not need to call this — the MVVM command (e.g., `RelayCommand`) raises `CanExecuteChanged` automatically when the relevant properties change via `[NotifyCanExecuteChangedFor]`.

---

## 4. HotKey vs KeyBinding — detailed comparison

| Aspect | HotKey | KeyBinding |
|--------|--------|------------|
| Applies to | `ICommandSource` controls (`MenuItem`, `Button`) | Any `InputElement` via `KeyBindings` collection |
| Activation | Always active (regardless of focus) | Only when owner or child has focus |
| XAML syntax | `HotKey="Ctrl+S"` | `<KeyBinding Gesture="Ctrl+S" .../>` |
| Requires a UI element | Yes — must be on a visible control | No — can be on Window directly |
| CommandParameter | Via control's `CommandParameter` | Via `KeyBinding.CommandParameter` |
| Access key integration | Separate from `_Header` underscore syntax | Not applicable |
| Multiple gestures | One per control | Many per collection |

---

## 5. Access keys (mnemonics)

Access keys use the underscore prefix in content text:

```xml
<Menu>
  <MenuItem Header="_File">
    <MenuItem Header="_Open" Command="{Binding OpenCommand}" />
    <MenuItem Header="_Save" Command="{Binding SaveCommand}" />
  </MenuItem>
</Menu>
```

Pressing Alt+F opens the File menu, then O triggers Open, S triggers Save.

### Rules

- The underscore activates the next character as an access key
- Access keys require the Alt modifier on Windows/Linux
- Access keys only work when the menu is visible and the parent window has focus
- This is NOT the same as `HotKey` — access keys navigate menus; `HotKey` executes commands directly

---

## 6. v12: RoutedCommand and CommandBinding

Avalonia 12 introduces `RoutedCommand` and `CommandBinding` for XAML-native command routing.

### RoutedCommand

A routed command is a shared `ICommand` that routes through the element tree, letting any `CommandBinding` handle it:

```csharp
public static RoutedCommand SaveCommand { get; } = new("Save");
```

### CommandBinding

Bind a routed command to a handler on any element:

```xml
<Window xmlns:local="using:MyApp">
  <Window.CommandBindings>
    <CommandBinding Command="local:MainWindow.SaveCommand"
                    Executed="OnSaveExecuted"
                    CanExecute="OnSaveCanExecute" />
  </Window.CommandBindings>
</Window>
```

### With KeyBinding

```xml
<Window.CommandBindings>
  <CommandBinding Command="local:MainWindow.SaveCommand"
                  Executed="OnSaveExecuted" />
</Window.CommandBindings>

<Window.KeyBindings>
  <KeyBinding Gesture="Ctrl+S"
              Command="local:MainWindow.SaveCommand" />
</Window.KeyBindings>
```

`RoutedCommand` is useful for commands that need to be handled by different elements in different contexts (e.g., Copy/Paste in various controls).

---

## 7. Platform behavior matrix

| Behavior | Windows | macOS | Linux/X11 | Browser (WASM) |
|----------|---------|-------|-----------|----------------|
| Ctrl shortcuts | Ctrl+S | Ctrl+S or Cmd+S | Ctrl+S | Ctrl+S (most) |
| Cmd/Meta modifier | N/A | Cmd+S | N/A | Meta+S |
| Alt access keys | Alt+F | N/A (menus use Cmd) | Alt+F | N/A |
| F1–F12 | Full support | System-reserved (F1–F12) | Full support | Limited |
| Browser-reserved | N/A | N/A | N/A | Ctrl+T, Ctrl+W, Ctrl+N |

### Best practice for cross-platform

```xml
<!-- Bind both Ctrl and Cmd variants for macOS -->
<KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
<KeyBinding Gesture="Cmd+S" Command="{Binding SaveCommand}" />
```

Or use a single `KeyBinding` with `KeyModifiers.Meta` and test on each platform.

---

## 8. Avalonia vs WPF keyboard

| Concept | Avalonia | WPF |
|---------|----------|-----|
| XAML shortcuts | `KeyBinding` in `KeyBindings` | `KeyBinding` in `InputBindings` |
| Hotkey on controls | `HotKey` attached property | `InputGestureText` (display only) |
| Gesture string | `Ctrl+S` parsed by `KeyGesture` type converter | Same |
| Scoped shortcuts | Focus-based — walks visual tree | Focus-based — walks visual tree |
| RoutedCommand | v12+ via `RoutedCommand` class | `RoutedCommand` built-in |
| CommandBinding | v12+ | `CommandBinding` built-in |
| CommandManager | `CommandManager.InvalidateRequerySuggested()` | Same |
| Access keys | `_Header` underscore syntax | Same |

The most visible difference: WPF uses `InputBindings` (a collection on `UIElement`), while Avalonia uses `KeyBindings` with the same pattern. WPF also has `CommandBinding` as a first-class concept from the beginning; Avalonia added it in v12.

---

## See Also

- [055 — Keyboard & Hotkeys (core tutorial)](055-keyboard-hotkeys.md)
- [055E — Keyboard & Hotkeys (examples)](055-keyboard-hotkeys-examples.md)
- [Avalonia Docs: Keyboard and Hotkeys](https://docs.avaloniaui.net/docs/input-interaction/keyboard-and-hotkeys)
- [Avalonia Docs: Creating Shortcuts](https://docs.avaloniaui.net/docs/input-interaction/mouse-and-keyboard-shortcuts)
