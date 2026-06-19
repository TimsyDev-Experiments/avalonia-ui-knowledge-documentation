---
tier: intermediate
topic: data
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 060V — MultiBinding & PriorityBinding (verbose companion)

**What this covers:** Internal binding behavior, edge cases, nesting, compiled binding with MultiBinding, and fallback chain internals.

**Prerequisites:** 060 — MultiBinding & PriorityBinding core

---

## 1. MultiBinding evaluation order

Child bindings in `MultiBinding.Bindings` are evaluated left-to-right. The resulting array is passed to the converter. If any child binding fails, `FallbackValue` is used for that position (or `AvaloniaProperty.UnsetValue` if not set).

```text
Child[0] ──┐
Child[1] ──┤─── IMultiValueConverter.Convert(values, ...) ───→ Target
Child[2] ──┘
```

---

## 2. Nested MultiBinding

Avalonia supports nesting a `MultiBinding` inside another. Each nested one resolves to a single value in the parent's input array:

```xml
<MultiBinding Converter="{StaticResource ParentConverter}">
  <MultiBinding StringFormat="Name: {0} {1}">
    <Binding Path="FirstName" />
    <Binding Path="LastName" />
  </MultiBinding>
  <Binding Path="Age" />
</MultiBinding>
```

The parent converter receives two values: the formatted name string and the age.

---

## 3. Compiled bindings with MultiBinding

Each child binding can use compiled bindings if `x:DataType` is set:

```xml
<UserControl x:DataType="vm:OrderViewModel">
  <TextBlock>
    <TextBlock.Text>
      <MultiBinding StringFormat="Total: {0:C} ({1} items)">
        <Binding Path="TotalPrice" />
        <Binding Path="ItemCount" />
      </MultiBinding>
    </TextBlock.Text>
  </TextBlock>
</UserControl>
```

Set `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` to make this the default project-wide.

---

## 4. IMultiValueConverter interface

```csharp
public interface IMultiValueConverter
{
    object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture);
}
```

Note: Unlike `IValueConverter`, there is **no ConvertBack**. MultiBinding is inherently one-way.

### Behavior matrix

| Scenario | Result |
|----------|--------|
| All values valid | Passed to converter |
| Some values null | `null` in the values list |
| Some values failed | `AvaloniaProperty.UnsetValue` |
| Converter returns null | `TargetNullValue` used (if set) |
| Converter throws | `FallbackValue` used |
| Converter returns `UnsetValue` | `FallbackValue` used |

---

## 5. FuncMultiValueConverter internals

`FuncMultiValueConverter<TIn, TOut>` takes a `Func<IReadOnlyList<TIn>, TOut>`.

```csharp
public class FuncMultiValueConverter<TIn, TOut> : IMultiValueConverter
{
    private readonly Func<IReadOnlyList<TIn>, TOut> _convert;

    public FuncMultiValueConverter(Func<IReadOnlyList<TIn>, TOut> convert)
        => _convert = convert;

    public object Convert(IList<object> values, Type targetType,
        object parameter, CultureInfo culture)
    {
        var typed = values.OfType<TIn>().ToList().AsReadOnly();
        return _convert(typed)!;
    }
}
```

Key behaviors:
- Filters out non-`TIn` values with `OfType<TIn>`, so mismatched types are silently skipped
- Works best when all child bindings return the same type

---

## 6. PriorityBinding detailed behavior

`PriorityBinding` evaluates each child binding in sequence:

1. If child binding succeeds → use its value immediately, ignore remaining
2. If child binding fails or returns `AvaloniaProperty.UnsetValue` → try next
3. If all children fail → use `FallbackValue`

```csharp
// Equivalent logic
public object? ResolveValue()
{
    foreach (var binding in Children)
    {
        var result = binding.Evaluate();
        if (result != AvaloniaProperty.UnsetValue && result != null)
            return result;
    }
    return FallbackValue;
}
```

### PriorityBinding with converters

Each child binding in a `PriorityBinding` can have its own converter:

```xml
<PriorityBinding>
  <Binding Path="Popularity" Converter={StaticResource RankToStars} />
  <Binding Path="Rating" Converter={StaticResource ScoreToStars} />
  <Binding Path="DefaultStars" />
</PriorityBinding>
```

---

## 7. StringFormat placeholder syntax

| Pattern | Result |
|---------|--------|
| `{0}` | First value |
| `{0:C}` | First value as currency |
| `{0:N2}` | First value with 2 decimal places |
| `{0} — {1}` | Two values with separator |
| `{}` at start | Escapes the opening brace |
| `'{0}'` (quoted) | Wraps value in quotes |

Escaping rules:

```xml
<!-- Good -->
<MultiBinding StringFormat="{}{0} + {1} = {2}" />
<MultiBinding StringFormat='\{0\} + \{1\} = {2}' />
```

---

## 8. Debugging multi-bindings

Enable trace logging:

```csharp
AppBuilder.Configure<App>()
    .LogToTrace(LogEventLevel.Warning);
```

Common issues:

| Symptom | Cause |
|---------|-------|
| All values show 0 | Converter exception swallowed; check `FallbackValue` |
| Some values missing | Child binding path typo; check trace output |
| Converter never called | One child binding failed, `UnsetValue` propagated |
| StringFormat not applied | Make sure child bindings return strings or use convertible types |

---

## See Also

- [060 — MultiBinding & PriorityBinding (core)](060-multibinding-prioritybinding.md)
- [060E — MultiBinding & PriorityBinding (examples)](060-multibinding-prioritybinding-examples.md)
- [Avalonia Docs: MultiBinding](https://docs.avaloniaui.net/docs/data-binding/multi-binding)
- [Built-in Converters](https://docs.avaloniaui.net/docs/data-binding/built-in-data-binding-converters)
