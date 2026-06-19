---
tier: intermediate
topic: property-system
estimated: 12 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 052 — Property System

**What you'll learn:** The three Avalonia property types — `StyledProperty`, `DirectProperty`, and `AttachedProperty` — how to register them, how value precedence resolves conflicts, and how metadata and change callbacks work.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md), [007 — ObservableObject & ObservableProperty](../basics/007-observable-object-property.md), [022 — Attached Properties & Behaviors](../advanced/022-attached-properties-behaviors.md)

---

## 1. Why a custom property system?

Avalonia's property system replaces standard CLR properties with objects (`AvaloniaProperty`) that support:

- **Value precedence** — styles, animations, local values, and inheritance resolve in a fixed priority order
- **Binding** — any property can be the target of a data binding
- **Inheritance** — values propagate from parent to child in the visual tree
- **Metadata** — default values, coercion, validation, and change callbacks attached to the property itself

Every property in the system is backed by a static `AvaloniaProperty` field, analogous to WPF's `DependencyProperty`.

---

## 2. Three property types

| Type | Class | Inheritance | Styling | Performance | Use case |
|------|-------|-------------|---------|-------------|----------|
| `StyledProperty` | `StyledProperty<T>` | Yes | Yes | Good | Most UI properties — `Background`, `FontSize`, `Margin` |
| `DirectProperty` | `DirectProperty<T>` | No | No | Best | Backing-field properties — `IsPressed`, `IsFocused`, `SelectedIndex` |
| `AttachedProperty` | `AttachedProperty<T>` | Yes | Yes | Good | Extending foreign objects — `Grid.Row`, `DockPanel.Dock` |

### StyledProperty

A `StyledProperty` stores its value in the property system's internal dictionary. It participates in styling, animation, and value inheritance.

```csharp
public static readonly StyledProperty<IBrush?> BackgroundProperty =
    AvaloniaProperty.Register<MyControl, IBrush?>(nameof(Background));

public IBrush? Background
{
    get => GetValue(BackgroundProperty);
    set => SetValue(BackgroundProperty, value);
}
```

### DirectProperty

A `DirectProperty` stores its value in a CLR backing field. It does NOT participate in styling or inheritance, making it faster. Use it for internal control state.

```csharp
public static readonly DirectProperty<MyControl, bool> IsPressedProperty =
    AvaloniaProperty.RegisterDirect<MyControl, bool>(
        nameof(IsPressed),
        o => o.IsPressed,
        (o, v) => o.IsPressed = v);

private bool _isPressed;
public bool IsPressed
{
    get => _isPressed;
    private set => SetAndRaise(IsPressedProperty, ref _isPressed, value);
}
```

### AttachedProperty

An `AttachedProperty` extends types you do not own. See [022 — Attached Properties & Behaviors](../advanced/022-attached-properties-behaviors.md) for full coverage.

```csharp
public static readonly AttachedProperty<int> ColumnProperty =
    AvaloniaProperty.RegisterAttached<Grid, AvaloniaObject, int>("Column");
```

---

## 3. Registration methods

| Method | Property type | When to use |
|--------|-------------|-------------|
| `AvaloniaProperty.Register<T, TValue>` | `StyledProperty` | Default — most UI properties |
| `AvaloniaProperty.RegisterDirect<T, TValue>` | `DirectProperty` | Internal state with backing field |
| `AvaloniaProperty.RegisterAttached<TOwner, THost, TValue>` | `AttachedProperty` | Extending external types |

All three require a unique `name` matching the CLR property name for XAML serialization.

---

## 4. Value precedence

When multiple sources provide a value, the property system resolves by priority (highest to lowest):

| Priority | Source | Description |
|----------|--------|-------------|
| 1 | Animation | Active animation values |
| 2 | LocalValue | `SetValue`, XAML attribute, direct binding |
| 3 | StyleTrigger | Pseudo-class selectors (`:pointerover`, `:pressed`) |
| 4 | Template | Control template setters |
| 5 | Style | Type/class selectors |
| 6 | Inherited | Ancestor value (only for `inherits: true` properties) |
| 7 | Unset | Registered default value |

```xml
<Button Foreground="Red">  <!-- LocalValue — wins over everything below animation -->
```

If the local value is cleared via `ClearValue`, the next priority level takes effect:

```csharp
myButton.ClearValue(Button.ForegroundProperty);  // falls through to style value
```

---

## 5. Metadata

Metadata controls default values, binding modes, coercion, and validation.

### StyledPropertyMetadata

| Parameter | Type | Description |
|-----------|------|-------------|
| `defaultValue` | `T` | Used when no other source provides a value |
| `defaultBindingMode` | `BindingMode` | Default mode for bindings without explicit mode |
| `coerce` | `Func<AvaloniaObject, T, T>?` | Adjusts value before storage |
| `enableDataValidation` | `bool` | Participates in `INotifyDataErrorInfo` validation |

```csharp
public static readonly StyledProperty<double> ProgressProperty =
    AvaloniaProperty.Register<MyControl, double>(
        nameof(Progress),
        defaultValue: 0.0,
        coerce: CoerceProgress);

private static double CoerceProgress(AvaloniaObject sender, double value)
    => Math.Clamp(value, 0.0, 100.0);
```

### DirectPropertyMetadata

Only `unsetValue`, `defaultBindingMode`, and `enableDataValidation` apply. No coercion.

### OverrideMetadata for derived types

```csharp
static MySpecialButton()
{
    BackgroundProperty.OverrideDefaultValue<MySpecialButton>(Brushes.LightBlue);
}
```

---

## 6. Property change callbacks

### OnPropertyChanged override

React to any property change on the control:

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == IsExpandedProperty)
    {
        var isExpanded = change.GetNewValue<bool>();
        UpdateVisualState(isExpanded);
    }
}
```

### AddClassHandler

Static handler for all instances of a type:

```csharp
static MyControl()
{
    IsExpandedProperty.Changed.AddClassHandler<MyControl>((control, args) =>
    {
        control.OnIsExpandedChanged(args);
    });
}
```

### GetObservable

External subscription using `IObservable<T>`:

```csharp
myControl.GetObservable(MyControl.IsExpandedProperty)
    .Subscribe(isExpanded => Console.WriteLine($"Expanded: {isExpanded}"));
```

---

## 7. AddOwner pattern

Reuse an existing property on another type:

```csharp
public class MySpecialControl : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Control.BackgroundProperty.AddOwner<MySpecialControl>();

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }
}
```

This lets `MySpecialControl` participate in the same styling and inheritance chain as the original property without re-registering.

---

## 8. Inherits

Mark a property as inheritable so its value propagates down the visual tree:

```csharp
public static readonly StyledProperty<bool> IsCompactProperty =
    AvaloniaProperty.Register<MyControl, bool>(
        nameof(IsCompact),
        defaultValue: false,
        inherits: true);
```

Descendants that register ownership can read the inherited value. Built-in inherited properties include `FontSize`, `Foreground`, and `DataContext`.

---

## 9. Clearing and coercion

```csharp
// Remove local value — falls through to next priority
myControl.ClearValue(MyControl.ProgressProperty);

// Set value without creating LocalValue entry (control implementors)
myControl.SetCurrentValue(MyControl.ProgressProperty, 50.0);

// Trigger re-coercion manually
myControl.CoerceValue(MyControl.ProgressProperty);
```

`SetCurrentValue` is useful when a control updates its own property in response to user input but wants styles to remain able to override.

---

## Key Takeaways

- `StyledProperty` — default, supports styling, inheritance, and animation
- `DirectProperty` — backing field, best perf, no styling/inheritance
- `AttachedProperty` — extends foreign types (see 022)
- Value precedence: animation > local > trigger > template > style > inherited > default
- Metadata controls defaults, coercion, validation, and change callbacks
- `AddOwner` shares a property definition across types
- `SetCurrentValue` writes at current priority without creating a local override

---

## See Also

- [052V — Property System (verbose companion)](052-property-system-verbose.md)
- [052E — Property System (examples)](052-property-system-examples.md)
- [022 — Attached Properties & Behaviors](../advanced/022-attached-properties-behaviors.md)
- [Avalonia Docs: Property System Overview](https://docs.avaloniaui.net/docs/properties)
- [Avalonia Docs: Value Precedence](https://docs.avaloniaui.net/docs/properties/value-precedence)
- [Avalonia Docs: Metadata and Callbacks](https://docs.avaloniaui.net/docs/properties/metadata-and-callbacks)
