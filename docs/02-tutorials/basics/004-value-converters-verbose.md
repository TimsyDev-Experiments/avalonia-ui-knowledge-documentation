---
tier: basics
topic: value converters
estimated: 25-30 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 004-value-converters.md
---

# 004V — Value Converters: An In-Depth Companion

**What you'll learn in this companion:** The `IValueConverter` contract in detail, why `ConvertBack` exists, how `FuncValueConverter` eliminates boilerplate, the difference between value converters and `StringFormat`, how multi-value converters work under the hood, and when not to use a converter at all.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

**You should already have read:** [004 — Value Converters](004-value-converters.md) for the quick-start version. This file goes deeper on every section.

---

## 1. The `IValueConverter` Contract: What Each Method Means

```csharp
public interface IValueConverter
{
    object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
    object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
```

### `Convert(object? value, Type targetType, object? parameter, CultureInfo culture)`

Called when data travels **from source to target** (ViewModel → View).

- **`value`:** The raw value from the binding source property. Can be `null` if the source property is `null` and the binding allows nulls.
- **`targetType`:** The type the binding target expects. For `TextBlock.Text`, this is `string`. For `Button.IsVisible`, this is `bool`. Your converter can use this to return different types based on context — a converter that returns both visibility and text formats is valid but usually a bad idea.
- **`parameter`:** The `ConverterParameter` from XAML. Often `null`. Can be any type — in XAML, you typically pass constant strings or numbers. For more complex parameter types, you need a markup extension.
- **`culture`:** The current culture from `AvaloniaLocator.Current.GetService<CultureInfo>()`. Defaults to `CultureInfo.CurrentCulture`. Use this for locale-sensitive formatting (date formats, number separators).

The return value is passed to the target property. If you return `null` and the target is a value type, Avalonia applies the target property's `FallbackValue` or default.

### `ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)`

Called when data travels **from target to source** (View → ViewModel). This only happens with `Mode=TwoWay` or `Mode=OneWayToSource` bindings.

- **`value`:** The value from the target property (e.g., the updated text from a `TextBox`).
- **`targetType`:** The type the source property expects (e.g., `int`, `string`, `DateTime`).
- **`parameter` / `culture`:** Same as `Convert`.

If your converter does not support `ConvertBack`, throw `NotSupportedException`. Do not return `null` — that silently replaces the user's input with `null`. Throwing `NotSupportedException` causes the binding to log a warning and skip the conversion.

### Why Both Methods Exist on the Same Interface

`IValueConverter` bundles both directions because a converter often needs to be reversible. A `bool → Visibility` converter in WPF needs `ConvertBack` to round-trip. In Avalonia, `IsVisible` is `bool` — no converter needed for visibility. But for a string-to-int converter used with `TextBox.Text`, `ConvertBack` is essential: the user types text, and the converter must parse it back to an `int` for the ViewModel.

Splitting into separate `IValueConverter` (one-way) and `IValueConverterBack` (two-way) would make the binding system more complex, so they're paired. The convention is: if you don't support two-way conversion, throw `NotSupportedException` from `ConvertBack`.

---

## 2. Why Avalonia Does Not Need a Boolean-to-Visibility Converter

```csharp
// In WPF: Visibility.Collapsed/Hidden/Visible
// In Avalonia: bool (IsVisible)
```

WPF uses a `Visibility` enum with three states: `Visible`, `Hidden` (invisible but occupies layout space), and `Collapsed` (invisible and takes no space). This requires a converter to translate `bool` to `Visibility`.

Avalonia simplifies this: the property is `IsVisible` (a `bool`). If you need "hidden but still takes space," set `Opacity="0"` or `IsHitTestVisible="False"` instead. There is no separate `Hidden` state because:
- "Invisible but still occupies space" is a layout concern, not a visibility concern.
- The three-state visibility in WPF causes confusion and bugs (developers forget to handle `Hidden` vs `Collapsed`).
- Using `bool` eliminates the most-asked beginner question: "why is my converter not working for Visibility?"

This means your converter library for Avalonia has fewer entry-level converters.

---

## 3. `FuncValueConverter<TIn, TOut>`: How the Source Generator Alternative Works

```csharp
public static readonly FuncValueConverter<bool, bool> Inverse =
    new(value => !value);
```

`FuncValueConverter<TIn, TOut>` is a sealed class (not a source generator) that implements `IValueConverter`. Its constructor takes a `Func<TIn?, TOut?>` for `Convert` and an optional `Func<TOut?, TIn?>` for `ConvertBack`.

Internally, `FuncValueConverter` does:

```csharp
public sealed class FuncValueConverter<TIn, TOut> : IValueConverter
{
    private readonly Func<TIn?, TOut?> _convert;
    private readonly Func<TOut?, TIn?>? _convertBack;

    public FuncValueConverter(Func<TIn?, TOut?> convert)
    {
        _convert = convert;
    }

    public FuncValueConverter(Func<TIn?, TOut?> convert, Func<TOut?, TIn?> convertBack)
    {
        _convert = convert;
        _convertBack = convertBack;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TIn tin)
            return _convert(tin);
        return default(TOut);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_convertBack is not null && value is TOut tout)
            return _convertBack(tout);
        throw new NotSupportedException();
    }
}
```

The type parameters `TIn` and `TOut` provide compile-time type safety. If you pass an `int` to a `FuncValueConverter<bool, bool>`, the `Convert` method returns `default(bool)` (false) instead of crashing — it checks `value is TIn` and skips conversion if the types don't match. This is silent. If you need strict type enforcement, add a guard at the top of your lambda.

### When to Use `FuncValueConverter` Instead of `IValueConverter`

| Use `IValueConverter` when | Use `FuncValueConverter` when |
|---|---|
| You need `ConvertBack` with non-trivial logic | You only need `Convert` |
| You need to inspect `targetType`, `parameter`, or `culture` | Your conversion is a simple expression |
| You want a named, documented class | You want inline definition, no separate file |
| The conversion involves resources or services | The conversion is a pure function |

`FuncValueConverter` shines for stateless, single-expression conversions. For anything that touches `parameter` or `culture`, use `IValueConverter` — the `FuncValueConverter` lambda does not receive those parameters.

---

## 4. Multi-Binding and `FuncMultiValueConverter<TIn, TOut>`

```csharp
public static readonly FuncMultiValueConverter<bool, bool> AllTrue =
    new(values => values.All(v => v is true));
```

`MultiBinding` aggregates multiple binding sources into a single value. The `FuncMultiValueConverter` receives an `IReadOnlyList<TIn>` — all the resolved values from the child bindings, in order.

In Avalonia 12, `FuncMultiValueConverter<TIn, TOut>` changed its input type from `IEnumerable<TIn>` (evaluated lazily, consumed once) to `IReadOnlyList<TIn>` (indexable, repeatable). This is important:

- With `IEnumerable`, calling `.Count()` or `.ElementAt()` enumerated the sequence, which was fine for one pass but caused issues if the converter's `values` parameter was enumerated multiple times.
- With `IReadOnlyList`, you can access `values[0]`, `values[1]`, etc., directly and repeatedly without performance penalty.

If you wrote a custom `IMultiValueConverter` for Avalonia 11, the interface also changed: the `Convert` method now receives `IReadOnlyList<object?>` instead of `IEnumerable<object?>`.

### When Not to Use MultiBinding

If you only need to combine two or three bools, consider an `AND` or `OR` from `BoolConverters`:

```csharp
using Avalonia.Data.Converters;

// Built-in
BoolConverters.And  // IMultiValueConverter
BoolConverters.Or   // IMultiValueConverter
```

These are already registered in the `Avalonia.Data.Converters` namespace and can be used as `{StaticResource BoolConverters.And}` — provided you add them as a resource. They reduce the need for custom multi-converters.

---

## 5. `ConverterParameter` and `StringFormat`: When to Use Each

### `ConverterParameter`

```xml
<TextBlock Text="{Binding Price, Converter={StaticResource StringFormat},
                       ConverterParameter='Total: {0:C}'}" />
```

`ConverterParameter` is passed to `IValueConverter.Convert` as the `parameter` argument. It is evaluated at XAML load time and stored as an `object`. Because the parameter is set in XAML, it must be a constant or a `StaticResource` — it cannot be a binding.

Common uses for `ConverterParameter`:
- Format strings (as shown).
- Enum values: `ConverterParameter=FullName` to control which property of a person object is displayed.
- Threshold values for comparison converters: `ConverterParameter=18` in an age-to-adult converter.

### `StringFormat` on the Binding

```xml
<TextBlock Text="{Binding Price, StringFormat='Total: {0:C}'}" />
```

`StringFormat` is a property of the `Binding` class itself, handled before any converter runs. The flow is:

1. Binding resolves source value (e.g., `decimal 19.99`).
2. Converter runs (if any) on the source value.
3. `StringFormat` applies to the converter output (or the raw value if no converter).
4. Result is written to the target property.

Because `StringFormat` runs after the converter, you can combine them: a converter that returns a `DateTime?` can be followed by `StringFormat='{0:yyyy-MM-dd}'`.

**When to use `StringFormat` over a converter:**

- Purely visual formatting (currency, dates, numbers, percentages).
- No logic, no conditional formatting.
- The format is determined by the view, not the ViewModel.

**When to use a converter:**

- Conditional logic (e.g., "show 'Yes' if true, 'No' if false").
- Type transformation (int → string with unit suffix).
- The formatting depends on state beyond the value itself.

---

## 6. How to Register Converters for Compiled Bindings

```xml
<Window.Resources>
  <converters:BoolToVisibilityConverter x:Key="BoolToVis" />
</Window.Resources>
```

With compiled bindings, `{Binding IsReady, Converter={StaticResource BoolToVis}}` requires:

1. `BoolToVis` must exist as a resource with key `BoolToVis` in the lookup chain.
2. The compiled binding generates code that calls `((IValueConverter)resources["BoolToVis"]).Convert(...)`.
3. At build time, the compiler does not verify that `BoolToVis` resolves to a valid `IValueConverter` — that is still a runtime check.

To make converter references compile-time safe, define converters as static properties and reference them with `x:Static`:

```xml
<Window.Resources>
  <x:Static Member="local:Converters.Inverse" x:Key="Inverse" />
</Window.Resources>
```

This requires the `x:Static` markup extension to be available (it is in Avalonia 12). The resource is resolved at XAML load time, but the reference to `Converters.Inverse` is compile-time validated.

---

## 7. Common Mistakes

1. **Returning `AvaloniaProperty.UnsetValue` from `Convert`.** This special sentinel tells the binding system to use `FallbackValue` (or target property default). Return it when the conversion is invalid (e.g., `null` input that cannot be converted). Do **not** return `null` for value-type targets (int, bool, etc.) — `null` causes a binding error.
2. **Throwing exceptions from `Convert`.** Exceptions in converters are caught by the binding system and logged as warnings. The target property retains its previous value. Throwing does not crash the app, but it does silently swallow the error. Return `AvaloniaProperty.UnsetValue` instead to signal "no valid conversion."
3. **Ignoring `targetType`.** If the converter is used with multiple target types (e.g., both `TextBlock.Text` and `TextBox.Text`), ignoring `targetType` can produce wrong results. For a `bool` converter, `TextBlock.Text` expects `string`, `IsVisible` expects `bool`. Use `targetType` to switch behavior.
4. **Using `ConverterParameter` for non-constant values.** `ConverterParameter` is evaluated once at XAML load. If you need a dynamic parameter, use a `MultiBinding` with both the value and the parameter as separate bindings.

---

## See Also

- [004 — Value Converters (original tutorial)](004-value-converters.md)
- [004X — Value Converters (examples)](004-value-converters-examples.md)
- [005 — Binding Modes](005-binding-modes.md)
- [005V — Binding Modes (verbose companion)](005-binding-modes-verbose.md)
- [Avalonia Docs: Value Converters](https://docs.avaloniaui.net/docs/data-binding/value-converters)
