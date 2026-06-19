---
topic: properties
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Property System Quick Ref

## Property types

| Type | Inheritance | Styling | Default | Use case |
|---|---|---|---|---|
| `StyledProperty<T>` | Yes | Yes | `AvaloniaProperty.Register` | Most controls |
| `DirectProperty<T>` | No | No | `AvaloniaProperty.RegisterDirect` | CLR-backed, perf-critical |
| `AttachedProperty<T>` | N/A | Yes | `AvaloniaProperty.RegisterAttached` | Extending foreign types |

## Registration

```csharp
// Styled
public static readonly StyledProperty<double> FontSizeProperty =
    AvaloniaProperty.Register<TextElement, double>(nameof(FontSize), defaultValue: 12.0);

// Direct
public static readonly DirectProperty<MyCtrl, bool> IsActiveProperty =
    AvaloniaProperty.RegisterDirect<MyCtrl, bool>(nameof(IsActive), o => o.IsActive, (o, v) => o.IsActive = v);

// Attached
public static readonly AttachedProperty<bool> IsDraggableProperty =
    AvaloniaProperty.RegisterAttached<DragBehavior, Control, bool>("IsDraggable");
```

## Value precedence (highest → lowest)

1. Animation
2. Local value (`SetValue`, `{Binding}`)
3. Style triggers / pseudo-classes
4. Style setters
5. Theme (ControlTheme) default
6. Inherited default
7. Registered default

## Metadata

```csharp
.Register<MyClass, double>(nameof(Width), 100.0,
    inherits: false,
    defaultBindingMode: BindingMode.TwoWay,
    validate: v => v >= 0);
```

## Change callbacks

```csharp
.OnPropertyChanged<double>((s, e) =>
    Console.WriteLine($"{e.Property.Name}: {e.OldValue} → {e.NewValue}"));
```
