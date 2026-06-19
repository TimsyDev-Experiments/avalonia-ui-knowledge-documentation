---
tier: intermediate
topic: property-system
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 052-property-system.md
---

# 052V — Property System: An In-Depth Companion

**Why this exists:** The original tutorial covers the three property types, registration, and value precedence. This companion deep-dives into the internal architecture, coercion vs validation, `SetValue` vs `SetCurrentValue` semantics, the `BindingPriority` enum, property value inheritance mechanics, the `AddOwner` pattern in detail, and how Avalonia's property system compares to WPF's.

**Cross-reference:** Original tutorial at [052-property-system.md](052-property-system.md).

---

## 1. How `StyledProperty` stores values internally

Unlike a CLR property which stores a value in a field, a `StyledProperty` stores values in a per-instance `EffectiveValue` dictionary keyed by `StyledProperty`. Each entry can hold values at multiple priority levels.

When you call `GetValue`, the property system:

1. Scans priority levels from animation down to unset
2. Returns the first value found at any level
3. If no value is found at any level, returns the registered default from metadata

The dictionary entries are lazily allocated — a property only uses memory if a value is actually set at a non-default priority.

### Priority bits

The `BindingPriority` enum defines the numeric levels:

```csharp
public enum BindingPriority
{
    Animation = -1,
    LocalValue = 0,
    StyleTrigger = 1,
    Template = 2,
    Style = 3,
    Inherited = 4,
    Unset = 5,
}
```

Lower numeric value = higher priority. Animations use `-1` to outrank everything.

---

## 2. `SetCurrentValue` semantic details

`SetCurrentValue` writes the value at the current effective priority rather than at `LocalValue`. This is designed for control implementors who want to reflect transient UI state without preventing styles from overriding.

```csharp
// Scenario: slider thumb position during drag
// SetCurrentValue writes at LocalValue if no style or animation is active.
// If an animation is running, it writes at the animation level.
// If a style trigger is active, it writes at StyleTrigger level.
mySlider.SetCurrentValue(Slider.ValueProperty, newValue);
```

Contrast with `SetValue`:

| Method | Writes at | Style can override? | Use case |
|--------|-----------|-------------------|----------|
| `SetValue` | Always `LocalValue` | No | User/app explicitly setting a value |
| `SetCurrentValue` | Current effective priority | Yes | Control updating own state in response to input |

### When to use each

```csharp
// User explicitly sets a value — use SetValue
myButton.SetValue(Button.ForegroundProperty, Brushes.Red);

// Control internally tracking pointer position — use SetCurrentValue
SetCurrentValue(ValueProperty, newValue);

// Avoid SetValue in control internals unless you intend to block styles
```

---

## 3. Coercion vs validation

Both are defined in metadata but serve different purposes.

### Coercion

The coerce callback runs every time the effective value changes, regardless of source. It receives the proposed value and returns the adjusted value.

```csharp
coerce: (sender, value) =>
{
    // Clamp to valid range — adjustment is silent
    return Math.Clamp(value, min, max);
}
```

Coercion is **not** called on the initial default value. It fires on every subsequent change. You can trigger re-coercion with `CoerceValue()`.

### Validation

The validate callback runs at registration time only. It returns `true` (accept) or `false` (reject). Rejected values throw an `InvalidOperationException`.

```csharp
validate: v => v > 0
```

Key differences:

| Aspect | Coercion | Validation |
|--------|----------|------------|
| When called | Every effective value change | Registration only |
| Can adjust | Yes — returns adjusted value | No — true/false only |
| On invalid | Silently adjusts | Throws exception |
| Per-type override | Via `OverrideMetadata` | Not possible — set once at registration |

---

## 4. OverrideMetadata in depth

Derived types can change metadata for properties defined in base classes. This must happen in the static constructor before any instance is created.

```csharp
public class RoundedButton : Button
{
    static RoundedButton()
    {
        // Option 1: Override default value only
        CornerRadiusProperty.OverrideDefaultValue<RoundedButton>(new CornerRadius(8));

        // Option 2: Override full metadata with coercion
        BackgroundProperty.OverrideMetadata<RoundedButton>(
            new StyledPropertyMetadata<IBrush?>(
                Brushes.White,
                coerce: CoerceRoundedButtonBackground));
    }

    private static IBrush? CoerceRoundedButtonBackground(
        AvaloniaObject sender, IBrush? value)
    {
        // Enforce minimum opacity
        if (value is ISolidColorBrush solid)
            return new SolidColorBrush(solid.Color, Math.Max(solid.Opacity, 0.5));
        return value;
    }
}
```

`OverrideMetadata` allows changing:
- `defaultValue`
- `defaultBindingMode`
- `coerce` callback
- `enableDataValidation`

You cannot change `validate` — that is fixed at registration.

---

## 5. Property value inheritance mechanics

When a property is registered with `inherits: true`, the property system performs an ancestor walk during `GetValue` if no value is found at any higher priority level.

### Walk order

```
Window (FontSize=18)
  StackPanel
    TextBlock (no local FontSize) → checks parent StackPanel → StackPanel has no FontSize → checks Window → Window has FontSize=18 → returns 18
```

The walk follows the **visual tree**, not the logical tree. `DataContext` uses the same inheritance mechanism, which is why setting `DataContext` on a `Window` makes it available to all children.

### Performance

Each inheritance walk is O(depth) in the visual tree. For deep trees, the property system caches the resolved value per instance. The cache is invalidated when any ancestor changes the property.

### Making descendants read an inherited property

A descendant type must register ownership to read the inherited value:

```csharp
// Base type defines the property
public class MyControl : Control
{
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<MyControl, bool>(
            nameof(IsCompact), inherits: true);
}

// Unrelated descendant registers ownership
public class MyListBoxItem : ListBoxItem
{
    public static readonly StyledProperty<bool> IsCompactProperty =
        MyControl.IsCompactProperty.AddOwner<MyListBoxItem>();

    public bool IsCompact
    {
        get => GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }
}
```

Without `AddOwner`, `MyListBoxItem.GetValue(IsCompactProperty)` would throw because the property is not registered on that type.

---

## 6. DirectProperty in depth

`DirectProperty` is Avalonia's answer to WPF's `ReadOnlyDependencyProperty` — a lightweight property that does not participate in styling, animation, or inheritance.

### When to use DirectProperty

- Internal control state (`IsPressed`, `IsFocused`, `IsSelected`)
- Properties that change frequently and need maximum performance
- Properties that should never be set by styles

### Registration with SetAndRaise

```csharp
public static readonly DirectProperty<MyControl, bool> IsActiveProperty =
    AvaloniaProperty.RegisterDirect<MyControl, bool>(
        nameof(IsActive),
        o => o.IsActive,                     // getter
        (o, v) => o.IsActive = v,            // setter (optional — omit for read-only)
        defaultBindingMode: BindingMode.OneWay);

private bool _isActive;
public bool IsActive
{
    get => _isActive;
    set => SetAndRaise(IsActiveProperty, ref _isActive, value);
}
```

`SetAndRaise` stores the value in the field and raises `PropertyChanged` for the `DirectProperty`. The property system does NOT store the value — it delegates entirely to the CLR field.

### Read-only DirectProperty

Omit the setter parameter to create a read-only property:

```csharp
public static readonly DirectProperty<MyControl, bool> IsPressedProperty =
    AvaloniaProperty.RegisterDirect<MyControl, bool>(
        nameof(IsPressed),
        o => o.IsPressed);

private bool _isPressed; // set internally
public bool IsPressed => _isPressed;
```

External code can bind to `IsPressed` but cannot set it.

---

## 7. AddOwner pattern details

`AddOwner` lets a type register an existing `StyledProperty` as its own without re-creating the property definition. Both types share the same storage key, so values set through either type affect the same underlying entry.

```csharp
// Control.BackgroundProperty is registered on Control
// Button.AddOwner makes it available on Button too
public static readonly StyledProperty<IBrush?> BackgroundProperty =
    Control.BackgroundProperty.AddOwner<Button>();
```

### Metadata in AddOwner

You can supply overridden metadata at the same time:

```csharp
public static readonly StyledProperty<IBrush?> BackgroundProperty =
    Control.BackgroundProperty.AddOwner<MySpecialButton>(
        new StyledPropertyMetadata<IBrush?>(Brushes.Gold));
```

### Sharing across assemblies

`AddOwner` enables a library to define a property and let app-level controls consume it:

```csharp
// Library.dll
public class ThemeProperties
{
    public static readonly StyledProperty<bool> IsCompactProperty =
        AvaloniaProperty.Register<ThemeProperties, bool>(
            "IsCompact", inherits: true);
}

// App.exe
public class MyPanel : Panel
{
    public static readonly StyledProperty<bool> IsCompactProperty =
        ThemeProperties.IsCompactProperty.AddOwner<MyPanel>();
}
```

---

## 8. Change callback performance

`OnPropertyChanged` fires for every property change on the control. For high-frequency properties, check the property identity first:

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    // Must call base
    base.OnPropertyChanged(change);

    // Fast path — check property identity before GetNewValue
    if (change.Property == OpacityProperty)
    {
        // Opacity changed — invalidate specific state
    }

    // For DirectProperty, use GetOldValue/GetNewValue with type inference
    if (change.Property == IsPressedProperty)
    {
        var isPressed = change.GetNewValue<bool>();
        UpdatePressedState(isPressed);
    }
}
```

### AddClassHandler vs OnPropertyChanged

| Mechanism | Scope | Runs for | Best for |
|-----------|-------|----------|----------|
| `OnPropertyChanged` override | Single type | All properties | General-purpose change handling |
| `AddClassHandler` | All instances of type | Single property | Focusing on one property across the type hierarchy |

---

## 9. Avalonia vs WPF property system

| Concept | Avalonia | WPF |
|---------|----------|-----|
| Property field type | `StyledProperty<T>`, `DirectProperty<T>` | `DependencyProperty` |
| Registration | Generic methods (`Register<T, TValue>`) | Non-generic with `typeof()` |
| Value precedence | Animation → Local → Trigger → Template → Style → Inherited → Default | Animation → Coercion → Active → Local → Trigger → Template → Style → Inherited → Default |
| Coercion | Per-metadata callback | `CoerceValueCallback` in metadata |
| Read-only property | RegisterDirect without setter | `RegisterReadOnly` with `DependencyPropertyKey` |
| Property inheritance | `inherits: true` parameter | `FrameworkPropertyMetadataOptions.Inherits` |
| Attached property | `RegisterAttached<TOwner, THost, TValue>` | `RegisterAttached` with `typeof()` |
| Validation | `validate` callback at registration | `ValidateValueCallback` |
| `SetCurrentValue` | Writes at current effective priority | Same concept |
| Metadata override | `OverrideMetadata<T>` | `OverrideMetadata(typeof(T), ...)` |

The most visible difference: Avalonia uses generic registration, making it type-safe without `typeof()` casts. Avalonia also separates `StyledProperty` (full-featured) and `DirectProperty` (lightweight) at the type level, whereas WPF uses a single `DependencyProperty` class.

---

## See Also

- [052 — Property System (core tutorial)](052-property-system.md)
- [052E — Property System (examples)](052-property-system-examples.md)
- [022 — Attached Properties & Behaviors](../advanced/022-attached-properties-behaviors.md)
- [Avalonia Docs: Property System Overview](https://docs.avaloniaui.net/docs/properties)
- [Avalonia Docs: Metadata and Callbacks](https://docs.avaloniaui.net/docs/properties/metadata-and-callbacks)
- [Avalonia Docs: Property Value Inheritance](https://docs.avaloniaui.net/docs/properties/property-value-inheritance)
