---
topic: bindings
estimated: 2 min read
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Binding Modes Cheat Sheet

## The Five Modes

| Mode | Direction | Use Case |
|---|---|---|
| `OneWay` | Source → Target | Read-only display (labels, read-only text) |
| `TwoWay` | Source ↔ Target | Editable inputs (TextBox, CheckBox, Slider) |
| `OneTime` | Source → Target (once) | Static data that never changes |
| `OneWayToSource` | Target → Source | Push UI state to VM without reading back |
| `Default` | Depends on property metadata | Let Avalonia decide |

## Default Mode by Control

| Control | Property | Default Mode |
|---|---|---|
| `TextBlock` | `Text` | OneWay |
| `TextBox` | `Text` | TwoWay |
| `CheckBox` | `IsChecked` | TwoWay |
| `Button` | `Command` | OneWay |
| `ItemsControl` | `ItemsSource` | OneWay |
| `ListBox` | `SelectedItem` | TwoWay |
| `Slider` | `Value` | TwoWay |
| `DatePicker` | `SelectedDate` | TwoWay |

## Syntax

```xml
<TextBlock Text="{Binding UserName, Mode=OneWay}" />
<TextBox Text="{Binding FullName, Mode=TwoWay}" />
<TextBlock Text="{Binding StaticTitle, Mode=OneTime}" />
<PasswordBox Password="{Binding Password, Mode=OneWayToSource}" />
```

> In Avalonia 12, `{Binding}` = `{CompiledBinding}` by default. `x:DataType` is required for compiled bindings.
