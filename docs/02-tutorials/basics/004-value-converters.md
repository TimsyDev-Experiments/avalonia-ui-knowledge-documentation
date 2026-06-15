---
tier: basics
topic: value converters
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 004 — Value Converters

**What you'll learn:** Create `IValueConverter` and `FuncValueConverter`, bind bool-to-visibility, and use multi-value converters.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

---

## 1. Simple IValueConverter (bool → Visibility)

```csharp
// Converters/BoolToVisibilityConverter.cs
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyApp.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? true : false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true;
    }
}
```

Register as a resource:

```xml
<Window.Resources>
  <converters:BoolToVisibilityConverter x:Key="BoolToVis" />
</Window.Resources>
```

Usage:

```xml
<Button Content="Save"
        IsVisible="{Binding IsReady, Converter={StaticResource BoolToVis}}" />
```

> In Avalonia, `IsVisible` binds directly to `bool` — no `Visibility.Collapsed` needed.

---

## 2. FuncValueConverter (inline, no class)

```csharp
using Avalonia.Data.Converters;

public static class Converters
{
    public static readonly FuncValueConverter<bool, bool> Inverse =
        new(value => !value);
}
```

```xml
<CheckBox IsChecked="{Binding IsDarkMode}" />
<TextBlock IsVisible="{Binding IsDarkMode, Converter={StaticResource Inverse}}"
           Text="Dark mode is enabled" />
```

No `ConvertBack` implementation needed for one-way converters.

---

## 3. Multi-binding with FuncMultiValueConverter

```csharp
public static readonly FuncMultiValueConverter<bool, bool> AllTrue =
    new(values => values.All(v => v is true));
```

```xml
<TextBlock Text="All conditions met">
  <TextBlock.IsVisible>
    <MultiBinding Converter="{StaticResource AllTrue}">
      <Binding Path="Condition1" />
      <Binding Path="Condition2" />
      <Binding Path="Condition3" />
    </MultiBinding>
  </TextBlock.IsVisible>
</TextBlock>
```

> Avalonia 12 changed `FuncMultiValueConverter` to accept `IReadOnlyList<TIn>` instead of `IEnumerable<TIn>`. You can now access `values[0]` without an intermediate collection.

---

## 4. ConverterParameter and string formatting

```csharp
public class StringFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string format && value is not null)
            return string.Format(culture, format, value);
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

```xml
<TextBlock Text="{Binding Price, Converter={StaticResource StringFormat},
                       ConverterParameter='Total: {0:C}'}" />
```

Or use `StringFormat` directly on the binding (no converter needed):

```xml
<TextBlock Text="{Binding Price, StringFormat='Total: {0:C}'}" />
```

---

## 5. Common converter locations

| Converter | Namespace | Purpose |
|---|---|---|
| `IValueConverter` | `Avalonia.Data.Converters` | Base interface |
| `FuncValueConverter<TIn, TOut>` | `Avalonia.Data.Converters` | Inline one-way |
| `FuncMultiValueConverter<TIn, TOut>` | `Avalonia.Data.Converters` | Inline multi-binding |
| `BoolConverters.And` | `Avalonia.Data.Converters` | Logical AND |
| `BoolConverters.Or` | `Avalonia.Data.Converters` | Logical OR |

---

## Key Takeaways

- `IValueConverter` for full control; `FuncValueConverter` for inline lambdas
- Avalonia uses `IsVisible` (bool), not `Visibility` enum — no converter needed for bool
- `StringFormat` on bindings often eliminates the need for a converter
- Register converters as resources to use them in XAML

---

## See Also

- [004V — Value Converters (verbose companion)](004-value-converters-verbose.md)
- [004X — Value Converters (examples)](004-value-converters-examples.md)
- [005 — Binding Modes](005-binding-modes.md)
- [Avalonia Docs: Value Converters](https://docs.avaloniaui.net/docs/data-binding/value-converters)
