---
tier: intermediate
topic: events
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 051 — Routed Events

**What you'll learn:** How Avalonia's routed event system works — bubble, tunnel, and direct strategies — and how to handle, raise, and register custom routed events.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md), [002 — Command Binding](../basics/002-command-binding.md)

---

## 1. What is a routed event?

A routed event travels through the element tree rather than firing only on the source element. This lets a parent handle events raised by any of its children — useful for control composition, list item interaction, and custom control design.

A routed event is backed by a static `RoutedEvent` field (like a `StyledProperty` backs a dependency property) and registered with the Avalonia event system via `RoutedEvent.Register`.

---

## 2. Routing strategies

Every routed event has a strategy that determines how it travels:

| Strategy | Direction | Description |
|----------|-----------|-------------|
| `Bubble` | Source → root | Fires on the source element first, then travels up through each parent. Most common. |
| `Tunnel` | Root → source | Fires on the root first, then travels down to the source. Used for interception. |
| `Direct` | Source only | Fires only on the source element. No tree traversal. |

Many input events use a combined `Tunnel | Bubble` strategy — the event tunnels down first, then bubbles back up.

**Bubble example**: Click a `Button` inside a `StackPanel` inside a `Window`:

```
Window          ← event arrives last (bubble)
  StackPanel    ← event arrives second
    Button      ← event starts here (source)
```

**Tunnel example**: The same event during the tunnel phase travels in reverse:

```
Window          ← event starts here (tunnel)
  StackPanel    ← event arrives second
    Button      ← event arrives last (source)
```

---

## 3. Handling events

### In XAML

Attach by event name as an attribute:

```xml
<Button Click="OnButtonClick" Content="Click me" />
```

```csharp
private void OnButtonClick(object? sender, RoutedEventArgs e)
{
    // sender is the Button where the handler is attached
    // e.Source is the original source of the event
}
```

### Handing bubbled events on a parent

```xml
<StackPanel Tapped="OnStackPanelTapped">
  <Button Content="Button 1" />
  <Button Content="Button 2" />
</StackPanel>
```

```csharp
private void OnStackPanelTapped(object? sender, TappedEventArgs e)
{
    if (e.Source is Button button)
        Debug.WriteLine($"Tapped: {button.Content}");
}
```

### In code with AddHandler

```csharp
myButton.AddHandler(Button.ClickEvent, OnButtonClick);
// Unsubscribe:
myButton.RemoveHandler(Button.ClickEvent, OnButtonClick);
```

### Tunnel phase in code

Because Avalonia does not have separate `Preview*` CLR events, subscribe to the tunnel phase via `AddHandler`:

```csharp
myControl.AddHandler(
    InputElement.PointerPressedEvent,
    OnPreviewPointerPressed,
    RoutingStrategies.Tunnel);
```

---

## 4. The Handled property

Set `e.Handled = true` to stop an event from continuing to route:

```csharp
private void OnButtonClick(object? sender, RoutedEventArgs e)
{
    e.Handled = true; // Parent handlers won't receive this event
}
```

To receive events even after they are marked handled, use `handledEventsToo`:

```csharp
myPanel.AddHandler(
    Button.ClickEvent,
    OnButtonClick,
    RoutingStrategies.Bubble,
    handledEventsToo: true);
```

---

## 5. RoutedEventArgs properties

| Property | Type | Description |
|----------|------|-------------|
| `Source` | `object?` | The element that originally raised the event. |
| `Handled` | `bool` | Whether the event has been handled. Set to `true` to stop routing. |
| `Route` | `RoutingStrategies` | Current phase: `Tunnel`, `Bubble`, or `Direct`. |
| `RoutedEvent` | `RoutedEvent` | The routed event instance being raised. |

---

## 6. Custom routed events

### Basic registration

```csharp
public class MyControl : Control
{
    public static readonly RoutedEvent<RoutedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<MyControl, RoutedEventArgs>(
            nameof(ValueChanged),
            RoutingStrategies.Bubble);

    public event EventHandler<RoutedEventArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    protected virtual void OnValueChanged()
    {
        RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
    }
}
```

### Custom event args

```csharp
public class ValueChangedEventArgs : RoutedEventArgs
{
    public ValueChangedEventArgs(RoutedEvent routedEvent, double oldValue, double newValue)
        : base(routedEvent)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public double OldValue { get; }
    public double NewValue { get; }
}
```

### Cancelable event

```csharp
public static readonly RoutedEvent<CancelRoutedEventArgs> BeforeCloseEvent =
    RoutedEvent.Register<MyControl, CancelRoutedEventArgs>(
        nameof(BeforeClose), RoutingStrategies.Bubble);

public bool RequestClose()
{
    var args = new CancelRoutedEventArgs(BeforeCloseEvent, this);
    RaiseEvent(args);
    return !args.Cancel;
}
```

---

## 7. Class handlers

Class handlers respond to an event for all instances of a type. They run before instance handlers:

```csharp
static MyControl()
{
    PointerPressedEvent.AddClassHandler<MyControl>((control, args) =>
    {
        control.OnPointerPressedInternal(args);
    });
}

private void OnPointerPressedInternal(PointerPressedEventArgs args)
{
    // Runs before any instance handler on MyControl
}
```

Class handlers are useful for control implementors who need to intercept input before instance-level handlers can mark events as handled.

---

## Key Takeaways

- Events travel by strategy: `Bubble` (source → root), `Tunnel` (root → source), or `Direct` (source only)
- Subscribe to tunnel phase via `AddHandler` with `RoutingStrategies.Tunnel` — no separate `Preview*` events
- Set `e.Handled = true` to stop routing; use `handledEventsToo` to bypass
- Use `RoutedEvent.Register<TOwner, TArgs>()` to define custom routed events
- Class handlers run before instance handlers for all instances of a type

---

## See Also

- [051V — Routed Events (verbose companion)](051-routed-events-verbose.md)
- [051E — Routed Events (examples)](051-routed-events-examples.md)
- [Avalonia Docs: Events Overview](https://docs.avaloniaui.net/docs/events)
- [Avalonia Docs: Routed Events Deep Dive](https://docs.avaloniaui.net/docs/input-interaction/routed-events)
