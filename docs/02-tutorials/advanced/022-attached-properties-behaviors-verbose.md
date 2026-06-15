---
tier: advanced
topic: extensibility
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 022-attached-properties-behaviors.md
---

# 022V — Attached Properties & Behaviors: An In-Depth Companion

**Why this exists.** The original tutorial shows the mechanics of registering attached properties and wiring up event-driven behaviors. This companion explains the property-change notification system, lifetime management of event subscriptions, why `AvaloniaObject` is the constraint type, and how to avoid the common pitfalls that lead to memory leaks or stale handlers.

**Read this alongside:** [022 — Attached Properties & Behaviors](022-attached-properties-behaviors.md)

---

## 1. Why attached properties exist

Attached properties let you add metadata or behavior to any `AvaloniaObject` without modifying its class. This solves a fundamental tension in UI frameworks: the grid panel does not own `Grid.Row`, yet every child element needs to store its row index.

### The `AvaloniaObject` constraint

```csharp
public static readonly AttachedProperty<string?> ToolTipProperty =
    AvaloniaProperty.RegisterAttached<ToolTipService, AvaloniaObject, string?>("ToolTip");

public static void SetToolTip(AvaloniaObject element, string? value) =>
    element.SetValue(ToolTipProperty, value);
```

The owner type parameter (`ToolTipService`) exists for property-system registration — it does not affect what types can receive the property. The property is stored on any `AvaloniaObject` (which includes `Control`, `Visual`, `StyledElement`, and any class that derives from `AvaloniaObject`).

### Why `AvaloniaObject` and not a narrower type

Registering on `AvaloniaObject` means literally any element in the tree can have a `ToolTip`. If you know your property only makes sense on `Control` (not on `Visual`), you can narrow the type:

```csharp
AvaloniaProperty.RegisterAttached<ToolTipService, Control, string?>("ToolTip");
```

This still works because `Control` extends `AvaloniaObject`. The property is simply not usable on non-`Control` types.

### How properties are stored internally

`AvaloniaObject` maintains a `AvaloniaPropertyDictionary` (a specialized hash map) per instance. Attached property values are stored in the same dictionary as regular properties — there is no separate storage. `GetValue` and `SetValue` look up the property by its `AvaloniaProperty` instance in this dictionary.

---

## 2. Property change handlers: `AddClassHandler`

```csharp
static EnterKeyBehavior()
{
    CommandProperty.Changed.AddClassHandler<TextBox>(HandleCommandChanged);
}
```

### What `AddClassHandler<T>` does

`Property.Changed` is a `AvaloniaPropertyChangedListener` — an observable that fires whenever the property changes on **any** instance of any `AvaloniaObject`. `AddClassHandler<TextBox>` filters the events to only those where the sender is a `TextBox` (or a subclass). Without the generic filter, your handler would be called for every property change on every object in the application — a severe performance hit.

### The subscription is per-property, not per-instance

This line in the static constructor subscribes once. When `CommandProperty` changes on any `TextBox` in the entire application, `HandleCommandChanged` runs. This is efficient because there is one subscription per attached property, not one per element.

### What the handler receives

`AvaloniaPropertyChangedEventArgs` contains:
- `Sender` — the object whose property changed
- `Property` — the property that changed
- `OldValue` / `NewValue` — typed as `object?`

Cast `Sender` to your expected type (`TextBox`). If `AddClassHandler<TextBox>` was used, the cast always succeeds.

---

## 3. Event handler lifecycle — the most common bug

```csharp
private static void HandleCommandChanged(TextBox sender, AvaloniaPropertyChangedEventArgs e)
{
    if (e.OldValue is not null)
        sender.KeyDown -= OnTextBoxKeyDown;

    if (e.NewValue is ICommand)
        sender.KeyDown += OnTextBoxKeyDown;
}
```

### Why unsubscribe from `OldValue`

The property can change multiple times on the same element during its lifetime:
1. `TextBox` is created (no command set)
2. Binding sets `Command` to `SearchCommand`
3. ViewModel changes, binding sets `Command` to `NewSearchCommand`

Without the `OldValue` check, step 2 subscribes `KeyDown`, and step 3 subscribes `KeyDown` again — the handler runs twice per key press.

### Why the `is not null` check

`OldValue` can be `null` (the property was never set). Trying to `-=` a null value is safe in C# (it becomes a no-op), but the check makes the intent explicit.

### Static event handlers and GC

Because the handler is a static method, the subscription `sender.KeyDown += OnTextBoxKeyDown` holds a reference to `sender` (the `TextBox`). This is fine: the `TextBox` is rooted by the visual tree anyway, so it will not be collected prematurely. If you used an instance method (non-static), the `TextBox` would keep your behavior class alive — not a leak in the typical sense, but something to be aware of if your behavior class holds large state.

### Alternative: weak event pattern

If you prefer not to have the static method pattern, you can subscribe to the `KeyDown` event with a weak handler using `AvaloniaProperty.Subscribe`:

```csharp
CommandProperty.Changed.Subscribe(args =>
{
    if (args.Sender is TextBox textBox)
    {
        // ...
    }
});
```

But `AddClassHandler` is more performant and idiomatic for the attached-behavior pattern.

---

## 4. Attached-to-visual-tree pattern

```csharp
private static void OnAutoFocusChanged(Control control, AvaloniaPropertyChangedEventArgs e)
{
    if (e.NewValue is true)
        control.AttachedToVisualTree += OnAttached;
    else
        control.AttachedToVisualTree -= OnAttached;
}
```

### Why `AttachedToVisualTree` instead of `Loaded`

Avalonia does not have a `Loaded` event. The closest equivalent is `AttachedToVisualTree`, which fires when the element is connected to the root visual. This is the right moment to call `Focus()` because:
- The element has a `VisualRoot` (the window)
- The element has been measured and arranged
- The input system is ready to route focus

### The `else` branch

When `AutoFocus` is set to `False` (or removed), the handler unsubscribes from `AttachedToVisualTree`. This prevents unnecessary invocations if the element is re-attached later (e.g., moved to a different container).

### What happens if you call `Focus()` too early

If you call `Focus()` in the property-changed handler directly (without waiting for `AttachedToVisualTree`), it fails silently because the element has no visual root. The input system cannot route focus to an element that is not in the visible tree.

---

## 5. Attached properties in style setters

```xml
<Setter Property="Watermark" Value="{Binding
    (attached:InputExtensions.Watermark),
    RelativeSource={RelativeSource Self}}" />
```

### Parentheses syntax

In Avalonia XAML, attached properties in bindings use the parenthesized syntax `(owner:PropertyName)`. This distinguishes them from regular properties. Without parentheses, the XAML parser looks for a direct property named `Watermark` on `TextBox`, not the attached one.

### Why this is powerful

This pattern lets a single style rule populate an attached property value across many elements:

```xml
<Style Selector="TextBox">
  <Setter Property="(attached:InputExtensions.Watermark)" Value="Enter text..." />
</Style>
```

Now every `TextBox` has a watermark without modifying any control. This is how you create app-wide defaults for attached properties.

---

## 6. When to use which pattern

### Attached property (data only)

Use when you need to associate data with an element:
- `Grid.Row`, `Grid.Column` — layout position data
- `ToolTipService.ToolTip` — tooltip text
- `AutomationProperties.Name` — accessibility data

These are **passive** — they store information for another system to read.

### Attached behavior (event-driven)

Use when you need to add interaction logic:
- Pressing Enter triggers a command
- Auto-focus on attach
- Long-press gesture detection
- Drag source / drop target

These are **active** — they subscribe to events and execute logic.

### When NOT to use attached behaviors

- The logic is simple one-off code-behind (use the code-behind event).
- The logic belongs in the ViewModel (put it in a command).
- The logic requires significant state (create a dedicated control).
- The logic is specific to one View/ViewModel pair and will never be reused.

---

## 7. Testing attached behaviors

```csharp
[Fact]
public void EnterKeyBehavior_InvokesCommand_OnEnter()
{
    var textBox = new TextBox();
    var executed = false;
    var command = new RelayCommand(() => executed = true);

    EnterKeyBehavior.SetCommand(textBox, command);
    textBox.RaiseEvent(new KeyEventArgs { Key = Key.Enter });

    Assert.True(executed);
}
```

In headless tests, you can set the attached property directly via the static setter and then simulate the event. The behavior class can be tested in isolation without a visual tree.

---

## Cross-links

- [016 — Property System & Attached Properties](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/16-property-system-attached-properties-behaviors-and-style-properties.md) (plugin ref)
- [020 — Custom Templated Controls](020-custom-templated-controls.md) — attached properties often complement templated controls
- [022E — Attached Properties & Behaviors (examples)](022-attached-properties-behaviors-examples.md)
- [026 — Accessibility & Automation](026-accessibility-automation.md) — `AutomationProperties` is an attached-property family
- [Avalonia Docs: Attached Properties](https://docs.avaloniaui.net/docs/data-binding/attached-properties)
