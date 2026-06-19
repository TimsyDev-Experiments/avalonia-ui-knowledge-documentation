---
tier: intermediate
topic: data
estimated: 12 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 060 — MultiBinding & PriorityBinding

**What you'll learn:** How to combine multiple binding sources into one target with `MultiBinding`, and how to set fallback chains with `PriorityBinding`.

**Prerequisites:** [004 — Binding Basics](../basics/004-binding-basics.md), [011 — Converters](../basics/011-value-converters.md)

---

## 1. MultiBinding overview

`MultiBinding` merges values from several source bindings into a single target property using a `StringFormat` or an `IMultiValueConverter`.

```xml
<TextBlock>
  <TextBlock.Text>
    <MultiBinding StringFormat="{}{0} {1}">
      <Binding Path="FirstName" />
      <Binding Path="LastName" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

Placeholders `{0}`, `{1}`, etc. map to the child bindings in order.

---

## 2. IMultiValueConverter

For logic beyond string formatting, implement `IMultiValueConverter`:

```csharp
public class AllTrueConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture)
    {
        return values.All(v => v is true);
    }
}
```

```xml
<Button IsEnabled="{MultiBinding Converter={StaticResource AllTrue}}">
  <Button.IsEnabled>
    <MultiBinding Converter="{StaticResource AllTrue}">
      <Binding Path="IsFormValid" />
      <Binding Path="HasAcceptedTerms" />
    </MultiBinding>
  </Button.IsEnabled>
</Button>
```

---

## 3. FuncMultiValueConverter

For simple inline conversion without a separate class:

```csharp
public static class Converters
{
    public static readonly FuncMultiValueConverter<string, string> FullName =
        new(parts => string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p))));
}
```

```xml
<TextBlock.Text>
  <MultiBinding Converter="{x:Static local:Converters.FullName}">
    <Binding Path="FirstName" />
    <Binding Path="LastName" />
  </MultiBinding>
</TextBlock.Text>
```

---

## 4. Binding to named elements

Child bindings support `ElementName`, `RelativeSource`, and `#name` shortcuts:

```xml
<NumericUpDown x:Name="Width" Value="100" />
<NumericUpDown x:Name="Height" Value="50" />

<TextBlock.Text>
  <MultiBinding StringFormat="Area: {0} × {1} = {2}">
    <Binding Path="Value" ElementName="Width" />
    <Binding Path="Value" ElementName="Height" />
    <Binding Path="#Width.Value"
      Converter="{x:Static local:MultiplyConverter.Instance}"
      ConverterParameter="{Binding #Height.Value}" />
  </MultiBinding>
</TextBlock.Text>
```

---

## 5. MultiBinding properties

| Property | Notes |
|----------|-------|
| `Bindings` | Collection of child `Binding` objects |
| `Converter` | `IMultiValueConverter` |
| `ConverterParameter` | Passed to converter |
| `StringFormat` | .NET format string applied before/without converter |
| `FallbackValue` | Used when binding fails |
| `TargetNullValue` | Used when converter returns null |
| `Mode` | `OneWay` or `OneTime` (no TwoWay) |

> `MultiBinding` is one-way by default. Two-way is not supported because there's no general way to reverse a multi-value conversion.

---

## 6. PriorityBinding

`PriorityBinding` evaluates a list of bindings in order and uses the first one that produces a valid value:

```xml
<TextBlock>
  <TextBlock.Text>
    <PriorityBinding>
      <Binding Path="DisplayName" />
      <Binding Path="UserName" />
      <Binding Path="Email" FallbackValue="Unknown" />
    </PriorityBinding>
  </TextBlock.Text>
</TextBlock>
```

Each binding in the list acts as a fallback. If `DisplayName` is null/empty, it tries `UserName`, then `Email`.

### When to use

| Use case | Example |
|----------|---------|
| Multiple data sources with different quality | Server data > cache > placeholder |
| UI that degrades gracefully | Full name > username > email |
| Backward-compatible properties | `NewProp` > `LegacyProp` |

### PriorityBinding limitations

- Supports only `OneWay` binding (read-only fallback chain)
- All child bindings share the same target property type
- v12: `PriorityBinding` works with compiled bindings when each child has `x:DataType`

---

## 7. Common patterns

### Visibility from multiple conditions

```csharp
public class AnyTrueConverter : IMultiValueConverter
{
    public static readonly AnyTrueConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture)
    {
        return values.Any(v => v is true);
    }
}
```

### Computed value from multiple inputs

```csharp
public class RectangleAreaConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType,
        object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is double w && values[1] is double h)
            return w * h;
        return 0.0;
    }
}
```

---

## Key Takeaways

- `MultiBinding` combines sources via `StringFormat` or `IMultiValueConverter`
- `FuncMultiValueConverter<TIn, TOut>` avoids creating a separate class
- One-way only — no two-way multi-binding
- `PriorityBinding` tries bindings in order, uses the first valid result
- Fallback chains degrade gracefully when preferred data is unavailable

---

## See Also

- [060V — MultiBinding & PriorityBinding (verbose)](060-multibinding-prioritybinding-verbose.md)
- [060E — MultiBinding & PriorityBinding (examples)](060-multibinding-prioritybinding-examples.md)
- [Avalonia Docs: MultiBinding](https://docs.avaloniaui.net/docs/data-binding/multi-binding)
- [Avalonia Docs: Data Binding Syntax](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
