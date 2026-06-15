---
tier: advanced
topic: bindings
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 027 — Advanced Composite Bindings

**What you'll learn:** MultiBinding, priority binding, binding to indexers, dynamic data sources, and creating custom binding expressions.

**Prerequisites:** [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)

---

## 1. MultiBinding

Combine multiple source properties into a single target:

```xml
<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{StaticResource FullNameConverter}"
                  x:DataType="vm:PersonViewModel">
      <Binding Path="FirstName" />
      <Binding Path="LastName" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

```csharp
public class FullNameConverter : IMultiValueConverter
{
    public object? Convert(IReadOnlyList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values[0] is string first && values[1] is string last)
            return $"{first} {last}";
        return null;
    }
}
```

> In Avalonia 12, the `IReadOnlyList<TIn>` gives you indexed access without converting to an intermediate collection.

---

## 2. PriorityBinding (fallback chain)

Try bindings in order — use the first one that resolves:

```xml
<TextBlock>
  <TextBlock.Text>
    <PriorityBinding x:DataType="vm:MainViewModel">
      <Binding Path="DisplayName" />
      <Binding Path="UserName" />
      <Binding Path="Email" />
      <Binding Source="Unknown User" />
    </PriorityBinding>
  </TextBlock.Text>
</TextBlock>
```

If `DisplayName` is null/empty, falls to `UserName`, then `Email`, then the literal fallback.

---

## 3. Binding to indexers

```csharp
public class SettingsCollection
{
    public string this[string key]
    {
        get => /* lookup */;
        set => /* store */;
    }
}
```

```xml
<TextBlock Text="{Binding [Greeting]}" x:DataType="vm:MainViewModel" />
<!-- or with a key parameter: -->
<TextBlock Text="{Binding [ThemeColor]}" />
```

Requires `x:DataType` to point to the owning ViewModel, and the indexed property is resolved via compiled binding in Avalonia 12.

---

## 4. Binding to dynamic / ExpandoObject

```xml
<!-- Must use ReflectionBinding for dynamic data -->
<TextBlock Text="{ReflectionBinding UserName}"
           x:CompileBindings="False" />
```

Or in code:

```csharp
var binding = new ReflectionBinding("SomeDynamicProperty");
myTextBlock.Bind(TextBlock.TextProperty, binding);
```

---

## 5. Creating bindings from code (Avalonia 12 style)

```csharp
using Avalonia.Data;

// Compiled (type-safe)
var compiled = CompiledBinding.Create(
    (Person p) => p.FullName,
    bindingMode: BindingMode.OneWay);

// Reflection (runtime-resolved)
var reflection = new ReflectionBinding
{
    Path = "FullName",
    Mode = BindingMode.OneWay
};

// Apply to a control
myTextBlock.Bind(TextBlock.TextProperty, compiled);
```

The `IBinding` interface and `InstancedBinding` class were removed in Avalonia 12. Use `BindingBase` / `BindingExpressionBase`.

---

## 6. Binding with converters from code

```csharp
var binding = new ReflectionBinding(nameof(Person.IsActive))
{
    Converter = new BoolToVisibilityConverter(),
    ConverterParameter = "inverse"
};

myControl.Bind(Control.IsVisibleProperty, binding);
```

Or with compiled binding:

```csharp
var binding = CompiledBinding.Create(
    (Person p) => p.IsActive,
    converter: new BoolToVisibilityConverter());
```

---

## 7. ObservableObject with INotifyPropertyChanged for dynamic properties

```csharp
public partial class DynamicViewModel : ObservableObject
{
    private readonly Dictionary<string, object?> _props = new();

    public object? this[string key]
    {
        get => _props.GetValueOrDefault(key);
        set
        {
            _props[key] = value;
            OnPropertyChanged(key);
        }
    }
}
```

Works with `{ReflectionBinding [Key]}` in XAML.

---

## Key Takeaways

- `MultiBinding` combines multiple source values into one target
- `PriorityBinding` creates a fallback chain of bindings
- Use `ReflectionBinding` for dynamic/ExpandoObject data
- In C#, use `CompiledBinding.Create()` for type-safe, `new ReflectionBinding()` for runtime
- Indexer bindings work with compiled bindings when the indexer is typed
- `ObservableObject` can be extended for dynamic property scenarios

---

## See Also

- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [004 — Value Converters](../basics/004-value-converters.md) — converter fundamentals extended by composite bindings
- [Avalonia Docs: Data Binding Syntax](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
- [027V — Advanced Composite Bindings (verbose companion)](027-advanced-composite-bindings-verbose.md)
- [027X — Advanced Composite Bindings (examples)](027-advanced-composite-bindings-examples.md)
