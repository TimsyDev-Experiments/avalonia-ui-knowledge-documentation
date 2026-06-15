---
tier: basics
topic: binding modes
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 005-binding-modes.md
---

# 005V — Binding Modes: An In-Depth Companion

**What you'll learn in this companion:** How each binding mode affects the data flow lifecycle, why `Default` mode exists and how Avalonia resolves it per-property, the performance characteristics of each mode, and the interaction between binding modes and compiled bindings.

**Prerequisites:** [002 — Command Binding](002-command-binding.md)

**You should already have read:** [005 — Binding Modes](005-binding-modes.md) for the quick-start version. This file goes deeper on every section.

---

## 1. The Binding Pipe: How Data Flows from Source to Target

Every binding in Avalonia is an instance of the `Binding` class (or its compiled equivalent `CompiledBinding`). When a binding is attached to a target property, the framework:

1. **Resolves the source:** Walks the logical tree to find the `DataContext`, starting at the target element and going up. With compiled bindings, the source is the `x:DataType` type instance — the binding stores a strong reference to the accessor.
2. **Subscribes to change notifications:** If the source implements `INotifyPropertyChanged`, the binding subscribes to `PropertyChanged` and filters for the bound property name. This subscription is the mechanism that makes `OneWay` and `TwoWay` bindings update when the source changes.
3. **Reads the initial value:** Calls `PropertyInfo.GetValue(source)` (reflection) or the compiled accessor to get the current value.
4. **Runs the converter:** If a converter is specified, calls `IValueConverter.Convert(initialValue, targetType, parameter, culture)`.
5. **Writes to the target:** Sets the target `AvaloniaProperty` to the converted value.
6. **Listens for target changes (TwoWay only):** If the mode is `TwoWay`, the binding also subscribes to the target property's change event (via `AvaloniaPropertyChangedEventArgs`) and propagates changes back to the source through `ConvertBack`.

### Mode Determines Which Steps Execute

| Mode | Subscribe source | Subscribe target | Write target initially | Write source on change |
|---|---|---|---|---|
| `OneTime` | No | No | Yes | No |
| `OneWay` | Yes | No | Yes | No |
| `TwoWay` | Yes | Yes | Yes | Yes |
| `OneWayToSource` | No | Yes | No | Yes |

`OneTime` is the cheapest: it reads once and never listens. `OneWay` is the most common for display data. `TwoWay` is only needed for editable controls.

---

## 2. Why `OneTime` Exists and When It Matters

```xml
<TextBlock Text="{Binding StaticTitle, Mode=OneTime}" />
```

`OneTime` reads the source value once when the binding is first activated (typically when the `DataContext` is set) and never reads it again. The binding does **not** subscribe to `INotifyPropertyChanged` for that property. This has two consequences:

1. **Performance:** No subscription means no delegate allocation, no event handler registration, and no re-evaluation on every property change. For a list with 10,000 items where each item has a `Name` that never changes, `OneTime` avoids 10,000 event subscriptions.
2. **Staleness:** If the source property changes after the initial read, the target never updates. Use `OneTime` only when you are certain the value will not change.

A common pattern: use `OneTime` for titles, labels, and static metadata that is part of the object's identity and does not change through the object's lifetime.

### Interaction with Compiled Bindings

With compiled bindings, `Mode=OneTime` changes the generated code: the accessor reads the property directly without subscribing to `PropertyChanged`. This produces slightly smaller and faster IL. The compiler knows at build time that no change listener is needed.

---

## 3. `Default` Mode: How Avalonia Chooses the Actual Mode

```xml
<TextBox Text="{Binding Name}" />  <!-- Mode=Default -->
```

When you omit `Mode`, it defaults to `Default`. `Default` means: "ask the target property's metadata what the default mode should be." Each `AvaloniaProperty` can declare a `DefaultBindingMode` in its metadata:

```csharp
public static readonly StyledProperty<string> TextProperty =
    AvaloniaProperty.Register<TextBox, string>("Text", 
        defaultBindingMode: BindingMode.TwoWay);
```

For `TextBox.Text`, the `defaultBindingMode` is `TwoWay`. For `TextBlock.Text`, it's `OneWay`. For `Button.Command`, it's `OneWay`. This metadata is set by the control author and is stored in the `AvaloniaProperty`'s `PropertyMetadata`.

### Avalonia 12's Improved Metadata Inference

In Avalonia 11, many properties defaulted to `OneWay` even when `TwoWay` made more sense (e.g., `Slider.Value`). In Avalonia 12, the framework team audited the default binding modes for all built-in controls and changed many to `TwoWay` where appropriate. This means:

- `Default` in v12 may behave differently from v11 for the same property.
- If you relied on `Default` being `OneWay` for a specific property in v11, your app may behave differently in v12.
- **Mitigation:** Always be explicit about binding mode on input controls. Write `Mode=TwoWay` on every `TextBox`, `CheckBox`, `Slider`, and `ComboBox` binding. This makes the mode visible in XAML and immune to framework default changes.

---

## 4. `OneWayToSource`: When the Target Writes but Never Reads

```xml
<PasswordBox Password="{Binding UserPassword, Mode=OneWayToSource}" />
```

`PasswordBox.Password` is intentionally not bindable as `TwoWay` for security reasons: the framework does not want the password stored in memory as a plain `string` in the ViewModel without explicit developer intent. The property metadata sets `DefaultBindingMode` to `OneWayToSource`.

When `Mode=OneWayToSource`:

- The binding reads the initial value from the source (one time) and writes it to the target.
- It subscribes to the **target** property's change events.
- When the target changes (user types a password), it writes the new value to the source via `ConvertBack`.
- It does **not** subscribe to source changes — if `UserPassword` is updated in code, the `PasswordBox` shows the old value.

This is a write-only pipe: View → ViewModel only. You cannot use it to pre-populate a `PasswordBox`.

### Other OneWayToSource Scenarios

- **A `Slider` whose value is written to a ViewModel but never read back** (uncommon but valid for one-shot configuration).
- **A `CheckBox` that sets a filter flag** where the filter is only read on explicit "Apply" button press.

In general, if you need to read the value back (e.g., restore from a saved setting), use `TwoWay`.

---

## 5. Binding Mode Interaction with Converters

Converters sit between the source value and the target value. The mode determines how many times converters are called:

- **OneWay:** `Convert` is called once on initial load, and again every time the source property changes.
- **TwoWay:** `Convert` is called on load and on source changes. `ConvertBack` is called every time the target property changes.
- **OneTime:** `Convert` is called once at load. `ConvertBack` is never called.
- **OneWayToSource:** `Convert` is called once at load (initial push). `ConvertBack` is called every time the target changes.

If your converter is expensive (file I/O, large allocation), avoid `OneWay` or `TwoWay` in hot paths (e.g., inside `DataTemplate` items that update rapidly).

---

## 6. Common Mistakes

1. **Assuming `Default` always means `OneWay`.** It does not — for `TextBox.Text`, it means `TwoWay`. Check the property metadata before omitting `Mode`.
2. **Using `TwoWay` on read-only properties.** If the binding target property is read-only (e.g., `TextBlock.Text` is write-only from XAML perspective), `TwoWay` degrades to `OneWay`. The binding system logs a warning.
3. **Using `OneWayToSource` on a property whose source does not implement `INotifyPropertyChanged`.** The write direction (target → source) still works (it calls the setter), but if other UI elements need to read that same source property, they will not see the updated value because no change notification is raised.
4. **Mixing `Mode` and compiled bindings with wrong types.** If `Mode=TwoWay` but the source property is `IReadOnlyList<T>` (no setter), the compiled binding emits a build error because it cannot find a setter. Reflection bindings would silently fail at runtime.
5. **Binding `PasswordBox.Password` with `Mode=TwoWay`.** The `PasswordBox` throws an `InvalidOperationException` if you try — the property is explicitly protected against two-way binding for security.

---

## See Also

- [005 — Binding Modes (original tutorial)](005-binding-modes.md)
- [005X — Binding Modes (examples)](005-binding-modes-examples.md)
- [004 — Value Converters](004-value-converters.md)
- [004V — Value Converters (verbose companion)](004-value-converters-verbose.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [Avalonia Docs: Data Binding](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
