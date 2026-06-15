---
tier: advanced
topic: bindings
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 027-advanced-composite-bindings.md
---

# 027V — Advanced Composite Bindings: An In-Depth Companion

This companion explains the *why* and *how* behind every concept in the original tutorial. Read it alongside [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md).

---

## 1. MultiBinding — Why It Exists and How It Works

A regular binding pulls one source property into one target property. MultiBinding exists for the case where a single target depends on *multiple* source values — for example, a `TextBlock.Text` that combines `FirstName` and `LastName`, or a `Background` that depends on both status and theme.

### What each part does in the XAML

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

- `<MultiBinding>` is a collection of child bindings. Avalonia evaluates each child binding against the same `DataContext` (unless overridden with a per-binding `Source`).
- `x:DataType="vm:PersonViewModel"` — required for compiled bindings in Avalonia 12. It tells the XAML compiler what type the child bindings operate on, enabling compile-time path validation and faster runtime resolution.
- `<Binding Path="FirstName" />` — each child binding pulls a single value **and subscribes to change notifications**. When *any* child value changes, the converter is re-invoked.

### The converter contract

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

- `IReadOnlyList<object?>` contains the result of each child binding in declaration order. Index 0 = `FirstName`, index 1 = `LastName`.
- `targetType` is the type of the target property (e.g., `string` for `TextBlock.Text`). Your converter should handle the case where the target type differs from the return type.
- `parameter` is an optional static value from the XAML. Useful for mode flags.
- `CultureInfo` — always forward this to any string formatting inside the converter. Never hardcode `CultureInfo.InvariantCulture` unless explicitly desired.

### Why `IReadOnlyList` and not `object[]`?

Before Avalonia 12, the signature used `IList<object?>`. The change to `IReadOnlyList<object?>` signals that you must not modify the list (it's the binding engine's internal array). In practice, most converters treat it as read-only anyway.

### Common mistakes

- **Null-checking**: Indexing into `values` without checking bounds. If a child binding fails to resolve, the entry is `AvaloniaProperty.UnsetValue`. Check for `UnsetValue` explicitly.
- **Order dependence**: If you reorder child bindings in XAML but forget to update the converter indices, you get silent wrong values.
- **Missing `x:DataType`**: Without it, compiled bindings fall back to reflection-based binding, losing compile-time safety.

### When to use MultiBinding vs. a computed property

| Approach | When to use |
|---|---|
| MultiBinding + converter | Target property depends on properties *across different objects* (e.g., `Person.Age` and `Settings.MinimumAge`) |
| Computed property on ViewModel | All source values live on the same ViewModel and you already have one there |

For simple concatenations (`FirstName + " " + LastName`), a computed property `FullName => $"{FirstName} {LastName}"` on the ViewModel is cleaner and needs no converter. Use MultiBinding when the sources come from different origins or you cannot modify the source object.

---

## 2. PriorityBinding — Fallback Chains Explained

PriorityBinding is Avalonia's answer to the "try this, then try that" pattern. It exists for scenarios where you have multiple possible data sources of decreasing preference.

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

### How resolution works internally

1. Avalonia evaluates each child binding **in order**.
2. A binding is considered "successful" if it produces a non-null, non-`UnsetValue` result.
3. The first successful binding's value is used.
4. If that value later changes or becomes null, PriorityBinding re-evaluates from the start of the chain.

### What "first binding that resolves" means

A binding "resolves" when:
- The path exists on the source object.
- The value is not `AvaloniaProperty.UnsetValue`.
- The value is not `BindingOperations.DoNothing`.

A binding *fails* when:
- The path does not exist (throws at runtime for reflection, caught at compile time for compiled bindings).
- The value is explicitly `UnsetValue`.

### The literal fallback

```xml
<Binding Source="Unknown User" />
```

The last entry has no `Path` — it binds to the literal string `"Unknown User"` via its `Source` property. This acts as a guaranteed fallback. Without it, if all property-based bindings fail, the target would remain at its default value (empty string for `Text`).

### Common mistake: Assuming PriorityBinding notifies on changes

PriorityBinding *does* re-evaluate when properties change, but only the *active* binding's change notifications are forwarded to the target. If `DisplayName` becomes null after being set, PriorityBinding re-evaluates from position 1. If `UserName` later becomes not-null... it does NOT switch back. PriorityBinding is a one-directional fallback, not a dynamic prioritization engine.

### When to use PriorityBinding vs. a ternary in a converter

- PriorityBinding: Cleanest when the fallback chain is 3+ levels deep and lives in XAML.
- Converter: Better for binary fallback (`value ?? fallback`).

---

## 3. Binding to Indexers

Indexer bindings let you access `this[key]` properties directly from XAML.

### The source class

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

The indexer must be the class's default indexer (`this[...]`). Avalonia resolves `[Greeting]` to `this["Greeting"]`.

### The binding syntax

```xml
<TextBlock Text="{Binding [Greeting]}" x:DataType="vm:MainViewModel" />
```

- `[Greeting]` — square brackets signal an indexer binding. The value inside is the key.
- `x:DataType="vm:MainViewModel"` — **required** for compiled bindings. Without it, Avalonia 12 falls back to reflection, and `x:DataType` must be the type that owns the indexer.
- The key can also be a binding itself: `{Binding [({Binding SelectedKey})]}` is not valid syntax. For dynamic keys, use `ReflectionBinding`.

### Compiled vs. reflection for indexers

Avalonia 12's compiled bindings can resolve indexers at compile time *when the indexer's parameter and return types are statically known*. If the indexer returns `object?` or takes a non-string parameter, compiled bindings may degrade to reflection.

### When to use indexer bindings

Indexer bindings are useful for:
- Dictionary-backed ViewModel properties
- Settings/config lookup in XAML
- Data sources with dynamic property names

### Performance note

Indexer bindings are slower than direct property bindings because each get access goes through the indexer method rather than a direct property accessor. For frequently updated bindings (e.g., inside a ListBox item template), prefer direct properties.

---

## 4. Binding to Dynamic / ExpandoObject

```xml
<TextBlock Text="{ReflectionBinding UserName}"
           x:CompileBindings="False" />
```

### Why `ReflectionBinding` is required

`ExpandoObject` and `dynamic` types do not have statically resolvable members. Their property resolution happens entirely at runtime. Avalonia's compiled binding system resolves paths at compile time using `x:DataType` — impossible for a type whose shape is determined at runtime.

`ReflectionBinding` bypasses the compiled binding infrastructure entirely and uses `PropertyDescriptor` resolution at runtime:
1. At binding setup, it resolves the path via `TypeDescriptor.GetProperties`.
2. For `ExpandoObject`, it subscribes to `INotifyPropertyChanged` if the object implements it (which `ExpandoObject` does).
3. Each get access goes through `PropertyDescriptor.GetValue`, which is slower than compiled access.

### `x:CompileBindings="False"`

This disables compiled bindings for the entire element subtree. Required when you mix compiled and reflection bindings on the same element, because Avalonia 12 defaults to `CompiledBinding` everywhere.

### Code-based alternative

```csharp
var binding = new ReflectionBinding("SomeDynamicProperty");
myTextBlock.Bind(TextBlock.TextProperty, binding);
```

This is equivalent to the XAML form. `ReflectionBinding` is a subclass of `BindingBase` and works with any `BindableObject`.

### When to use

- Data sources from JavaScript interop, JSON deserialization, or dynamic APIs
- Plugin systems where property names are not known at compile time
- Prototyping where a full ViewModel is overkill

### Avoid if

- You have a known type. Use compiled bindings for type safety and performance.
- You need high-frequency updates (e.g., real-time data). Reflection overhead adds up.

---

## 5. Creating Bindings from Code (Avalonia 12 Style)

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

### `IBinding` and `InstancedBinding` removal

Avalonia 11 had `InstancedBinding` as the runtime binding object, with `IBinding` as a factory interface. Both were removed in Avalonia 12. Now:
- `CompiledBinding.Create()` returns a `BindingExpressionBase`.
- `new ReflectionBinding()` also returns a `BindingExpressionBase`.
- You apply both with the same `Bind()` method.

### `CompiledBinding.Create()` — how it works

```csharp
CompiledBinding.Create(
    (Person p) => p.FullName,
    bindingMode: BindingMode.OneWay);
```

1. The lambda `(Person p) => p.FullName` is an **expression tree**. The binder compiles it into a direct property access delegate, not a reflection call.
2. It subscribes to `INotifyPropertyChanged.PropertyChanged` on the source, mapping the expression body to the property name.
3. The `bindingMode` parameter determines when the target is updated.

The lambda must be an expression of the form `(SourceType s) => s.Property`. Any more complex expression (method call, arithmetic) will throw at runtime.

### `ReflectionBinding` — the escape hatch

```csharp
var reflection = new ReflectionBinding
{
    Path = "FullName",
    Mode = BindingMode.OneWay
};
```

This uses the same runtime resolution as `{Binding Path}` in XAML. It's useful when:
- The source type is not known at compile time.
- You need to set `Converter`, `ConverterParameter`, `StringFormat`, etc.
- You're writing code-gen or dynamic UI factories.

### When to use code-based bindings

- Creating controls programmatically (e.g., in a factory method)
- Dynamic UI where XAML is not available (plugins, scripting)
- Unit tests that need to verify binding behavior

---

## 6. Binding with Converters from Code

```csharp
// Reflection approach
var binding = new ReflectionBinding(nameof(Person.IsActive))
{
    Converter = new BoolToVisibilityConverter(),
    ConverterParameter = "inverse"
};
myControl.Bind(Control.IsVisibleProperty, binding);

// Compiled approach
var compiled = CompiledBinding.Create(
    (Person p) => p.IsActive,
    converter: new BoolToVisibilityConverter());
```

### Why both approaches exist

The reflection approach supports `ConverterParameter` and works with any binding path as a string. The compiled approach is type-safe but does not support `ConverterParameter` (the converter receives `null` as parameter).

### Converter interaction with the binding engine

When a converter is attached:
1. The binding engine evaluates the source value.
2. Calls `converter.Convert(value, targetType, parameter, culture)`.
3. The converter's return becomes the target value.
4. For two-way bindings, `ConvertBack` is called on the target-to-source path.

### Common mistake: forgetting `ConvertBack`

For two-way bindings, `IValueConverter.ConvertBack` must be implemented (or the converter must inherit from a base that throws `NotImplementedException`). Without it, two-way bindings silently fail on the reverse path — the source value never updates.

---

## 7. ObservableObject with Dynamic Indexer Properties

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

### Why this pattern exists

When you need a ViewModel that accepts arbitrary named properties at runtime — for example, a property grid, a dynamic form, or a generic data display — you cannot define each property upfront. This pattern combines Avalonia's indexer binding support (section 3) with change notification.

### How `OnPropertyChanged(key)` enables binding

`OnPropertyChanged(key)` raises `PropertyChanged` with a string that matches the binding path. For an indexer binding `{ReflectionBinding [Name]}`, Avalonia subscribes to `PropertyChanged` and checks if the event args's property name equals the key `"Name"`. When it matches, the binding re-reads the indexer value.

### The `ObservableObject` base class

`ObservableObject` from CommunityToolkit.Mvvm provides:
- `OnPropertyChanged(string)` — the protected method used here.
- `SetProperty<T>(ref T, T, string)` — not used here because we use a dictionary.
- `INotifyPropertyChanged` interface implementation.

### Limitations

- Works only with `ReflectionBinding` in XAML — compiled bindings cannot resolve `this[string]` at compile time (they need a known static property).
- No property change notification for *removing* keys. If you `Remove("Name")`, there's no `PropertyChanged` for `"Name"`. Call `OnPropertyChanged(nameof(key))` before removal, or set it to null.

---

## Key Takeaways — Why Each Matters

- **MultiBinding**: Solves the "one target, many sources" problem that a single Binding cannot.
- **PriorityBinding**: Provides declarative fallback chains without C# logic.
- **`ReflectionBinding`**: The only way to bind to dynamic/ExpandoObject data.
- **`CompiledBinding.Create()`**: Type-safe code-based bindings with no string paths.
- **Indexer bindings**: XAML access to `this[key]` patterns.
- **Dynamic ObservableObject**: Runtime-extensible ViewModel with change notification.

---

## See Also

- [027 — Advanced Composite Bindings (original)](027-advanced-composite-bindings.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [045 — Value Converters (plugin ref)](../references/45-value-converters-single-multi-and-binding-wiring.md)
- [032 — MVVM DI Wiring](032-mvvm-di-wiring.md)
- [Avalonia Docs: Data Binding Syntax](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
- [027X — Advanced Composite Bindings (examples)](027-advanced-composite-bindings-examples.md)
