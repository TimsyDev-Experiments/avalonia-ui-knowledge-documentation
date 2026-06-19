---
topic: input
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Q11 — Input Gestures

## Gesture string syntax

```
{Modifiers}+{Key}
{Modifiers}+{MouseButton}
{Modifiers}+{ScrollDirection}
```

Modifier order does not matter. Use `+` between parts (no spaces).

## Modifiers

| String | Key |
|---|---|
| `Ctrl` | Control |
| `Alt` | Alt |
| `Shift` | Shift |
| `Meta` | Windows / Command (⌘) |
| `Ctrl+Shift` | Control + Shift |

## Keys

Use any `Key` enum value: `A`–`Z`, `F1`–`F24`, `Space`, `Enter`, `Escape`, `Delete`, `Tab`, `Left`, `Up`, `Right`, `Down`, `OemPlus`, `OemMinus`, `D0`–`D9`, `NumPad0`–`NumPad9`.

## Mouse gestures

| String | Action |
|---|---|
| `Click` | Left click |
| `DoubleClick` | Double-left-click |
| `MiddleClick` | Middle mouse button |
| `Ctrl+Click` | Ctrl + left click |

## Scroll gestures

| String | Action |
|---|---|
| `ScrollHorizontal` | Scroll wheel horizontal |
| `ScrollVertical` | Scroll wheel vertical |
| `Shift+ScrollVertical` | Shift + scroll = horizontal scroll |

## Attaching gestures

### In XAML

```xml
<Button Content="Open" Command="{Binding OpenCommand}">
  <Button.KeyBindings>
    <KeyBinding Gesture="Ctrl+O" Command="{Binding OpenCommand}" />
  </Button.KeyBindings>
</Button>

<MenuItem Header="Open" Command="{Binding OpenCommand}"
          Gesture="Ctrl+O" />
```

### In code

```csharp
var keyBinding = new KeyBinding
{
    Gesture = new KeyGesture(Key.S, KeyModifiers.Ctrl),
    Command = new RelayCommand(Save)
};
window.KeyBindings.Add(keyBinding);
```

### On the Application (global hotkeys)

```xml
<Application xmlns="https://github.com/avaloniaui">
  <Application.KeyBindings>
    <KeyBinding Gesture="Ctrl+Shift+F12"
                Command="{Binding GlobalDiagnosticsCommand}" />
  </Application.KeyBindings>
</Application>
```

## KeyGesture constructor

```csharp
new KeyGesture(Key.O, KeyModifiers.Ctrl | KeyModifiers.Shift)
```

| Enum | Values |
|---|---|
| `Key` | All keyboard keys |
| `KeyModifiers` | `None`, `Alt`, `Ctrl`, `Shift`, `Meta` |

## Common shortcuts

| Gesture | Typical Binding |
|---|---|
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` or `Ctrl+Y` | Redo |
| `Ctrl+C` | Copy |
| `Ctrl+V` | Paste |
| `Ctrl+X` | Cut |
| `Ctrl+S` | Save |
| `Ctrl+O` | Open |
| `Ctrl+P` | Print |
| `F5` | Refresh |
| `Ctrl+F` | Find |

On macOS, use `Meta` instead of `Ctrl`: `Meta+S` for ⌘S.
