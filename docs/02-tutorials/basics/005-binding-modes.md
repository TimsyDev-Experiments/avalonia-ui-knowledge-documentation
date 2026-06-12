---
tier: basics
topic: binding modes
estimated: 5 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 005 — Binding Modes

**What you'll learn:** The five binding modes, when to use each, and how Avalonia 12 changes default behavior.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

---

## The modes

| Mode | Direction | Use case |
|---|---|---|
| `OneWay` | Source → Target | Display-only data (labels, read-only text) |
| `TwoWay` | Source ↔ Target | Editable inputs (TextBox, CheckBox, Slider) |
| `OneTime` | Source → Target once | Static data that never changes |
| `OneWayToSource` | Target → Source | Push UI state to VM without reading it |
| `Default` | Depends on property | Avalonia decides per property metadata |

---

## Examples

```xml
<TextBlock Text="{Binding UserName, Mode=OneWay}" />
<TextBox Text="{Binding FullName, Mode=TwoWay}" />
<TextBlock Text="{Binding StaticTitle, Mode=OneTime}" />
```

`OneWayToSource` is rare. Use it when a control writes to a property you don't need to read back:

```xml
<!-- PasswordBox doesn't support binding its Password property TwoWay -->
<PasswordBox Password="{Binding UserPassword, Mode=OneWayToSource}" />
```

---

## Default mode by control

| Control | Property | Default Mode |
|---|---|---|
| `TextBlock` | `Text` | `OneWay` |
| `TextBox` | `Text` | `TwoWay` |
| `CheckBox` | `IsChecked` | `TwoWay` |
| `Button` | `Command` | `OneWay` |
| `ItemsControl` | `ItemsSource` | `OneWay` |

---

## Avalonia 12 difference

`Mode=Default` now behaves as `TwoWay` for more properties than in 11.x due to improved metadata inference. When in doubt, be explicit:

```xml
<TextBox Text="{Binding Name, Mode=TwoWay}" />
```

---

## Key Takeaways

- `TwoWay` is the default for input controls — verify if you need explicit
- `OneTime` is a performance win for truly static data
- `OneWayToSource` is useful for write-only properties (like `PasswordBox.Password`)
- Always set `Mode` when the default isn't what you need

---

## See Also

- [004 — Value Converters](004-value-converters.md)
- [Avalonia Docs: Data Binding](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
