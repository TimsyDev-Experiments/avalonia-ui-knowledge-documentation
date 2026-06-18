---
tier: intermediate
topic: events
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 051-routed-events.md
---

# 051V — Routed Events: An In-Depth Companion

**Why this exists:** The original tutorial covers what routed events are and how to use them. This companion explains the architecture behind the event routing system, how Avalonia's approach differs from WPF's, and the advanced patterns you need for custom control development.

**Cross-reference:** Original tutorial at [051-routed-events.md](051-routed-events.md).

---

## 1. Why routed events exist

Event routing solves three problems that standard CLR events cannot handle well in a UI framework:

**Control composition and encapsulation.** A `Button` contains a `ContentPresenter` which may hold a `StackPanel` containing an `Image` and a `TextBlock`. When the user clicks the `Image`, the click must still reach the `Button`'s `Click` handler. Routed events make this work — the event originates on the `Image` (the deepest hit-testable element) and bubbles up through the tree to the `Button`.

**Singular handler attachment points.** Without routing, every child in a list would need its own handler. With routing, a single handler on the parent `ListBox` can process events for all items via `e.Source`.

**Class-level handling before instance handlers.** A `TextBox` class can intercept `PointerPressed` before any app-level handler sees it, to manage caret placement or selection. This is essential for maintaining control invariants.

---

## 2. How the event route is built

When `RaiseEvent` is called, Avalonia builds an `EventRoute` by walking the visual tree from the source element to the root. The route contains the ordered list of elements that will receive the event, determined by the `RoutingStrategies` flags.

The route traversal order:

1. **Class handlers** on each element fire before instance handlers
2. **Tunnel phase** (if `RoutingStrategies.Tunnel` is set): root → source
3. **Bubble phase** (if `RoutingStrategies.Bubble` is set): source → root

If both tunnel and bubble are set (the default for input events), the tunnel phase runs first, then the bubble phase. Both phases share the same `RoutedEventArgs` instance — changes made during tunnel are visible during bubble.

```
Step 1: Tunnel phase (if Tunnel flag set)
  Window.AddHandler(PointerPressedEvent, handler, RoutingStrategies.Tunnel)
    → fires first
  StackPanel.AddHandler(PointerPressedEvent, handler, RoutingStrategies.Tunnel)
    → fires second
  Button.AddHandler(PointerPressedEvent, handler, RoutingStrategies.Tunnel)
    → fires third

Step 2: Bubble phase (if Bubble flag set)
  Button.AddHandler(PointerPressedEvent, handler, RoutingStrategies.Bubble)
    → fires fourth
  StackPanel.AddHandler(PointerPressedEvent, handler, RoutingStrategies.Bubble)
    → fires fifth
  Window.AddHandler(PointerPressedEvent, handler, RoutingStrategies.Bubble)
    → fires sixth
```

---

## 3. Avalonia vs WPF event differences

### No separate Preview events

WPF exposes tunnel events as `Preview*` CLR events (e.g., `PreviewKeyDown`). Avalonia has no `Preview` events — tunnel and bubble share the same `RoutedEvent` instance. Subscribe to the tunnel phase via `AddHandler` with `RoutingStrategies.Tunnel`.

### Generic RoutedEvent\<T\>

Avalonia's `RoutedEvent<TEventArgs>` is generic, providing type-safe event args without requiring a separate delegate type:

```csharp
// Avalonia — generic, strongly typed
public static readonly RoutedEvent<PointerPressedEventArgs> TapEvent =
    RoutedEvent.Register<MyControl, PointerPressedEventArgs>(
        "Tap", RoutingStrategies.Bubble);
```

Compare with WPF's approach which requires passing `typeof()` for both the owner and the delegate.

### Click routing

WPF's `Button.Click` is a routed event. Avalonia's `Button.Click` is also a routed event, but Avalonia's `Button` inherits from `Avalonia.Controls.Button` which derives from `Avalonia.Controls.Primitives.ButtonBase`. The event is backed by `ButtonBase.ClickEvent`.

The delegate signature is `EventHandler<RoutedEventArgs>`, not a dedicated `RoutedEventHandler` delegate type as in WPF.

---

## 4. Qualified event names in XAML

You can attach a handler for a child's routed event on a parent that does not own the event:

```xml
<StackPanel Button.Click="CommonClickHandler">
  <Button Name="YesButton">Yes</Button>
  <Button Name="NoButton">No</Button>
</StackPanel>
```

The syntax is `OwnerType.EventName`. The handler receives events for all `Button` children. This works because routed events permit any `Interactive` element to subscribe to any `RoutedEvent` — the owner type qualification just helps the XAML compiler resolve the event.

---

## 5. The handledEventsToo mechanism

When `e.Handled = true`, typical handlers along the rest of the route do not fire. However, the event is not truly "stopped" — handlers registered with `handledEventsToo: true` still receive it. This is important for:

- **Diagnostics/logging**: an outer handler can observe all events without interfering
- **Control composition**: a parent custom control may need to see events that its child marked handled
- **Gesture recognition**: the gesture system often needs to observe pointer events regardless of handled state

```csharp
// This handler fires even if a child set e.Handled = true
this.AddHandler(
    InputElement.PointerPressedEvent,
    OnPointerPressedAnywhere,
    RoutingStrategies.Bubble,
    handledEventsToo: true);
```

---

## 6. The EventRoute class

`EventRoute` is the internal data structure that holds the ordered list of event targets. You rarely need to interact with it directly, but understanding its behavior helps debug routing issues:

```csharp
// Diagnostic: check if a route has handlers
bool hasHandlers = eventRoute.HasHandlers;
```

`RoutedEventRegistry.Instance.GetAllRegistered()` returns all registered routed events — useful for auditing event usage across the application:

```csharp
foreach (var ev in RoutedEventRegistry.Instance.GetAllRegistered())
{
    Console.WriteLine($"{ev.Name} (Owner: {ev.OwnerType.Name})");
}
```

---

## 7. Pointer capture and event routing

Pointer capture redirects all subsequent pointer events to the capturing element, bypassing normal hit testing. This is essential for drag operations, custom scroll, and ink/annotation scenarios:

```csharp
protected override void OnPointerPressed(PointerPressedEventArgs e)
{
    base.OnPointerPressed(e);
    e.Pointer.Capture(this);
}

protected override void OnPointerReleased(PointerReleasedEventArgs e)
{
    base.OnPointerReleased(e);
    e.Pointer.Capture(null);
}
```

When a pointer is captured, the `PointerMoved` event fires on the capturing element regardless of where the pointer actually is on screen. This is why drag-and-drop works even when the user moves the mouse rapidly outside the control bounds.

Only one element can hold pointer capture at a time. If another element calls `Capture()`, the previous capturer receives `PointerCaptureLost`.

---

## 8. RoutedEvent observables

Routed events expose `Raised` and `RouteFinished` observables for reactive-style monitoring:

```csharp
// Observe every time PointerPressed is raised anywhere
IDisposable sub = InputElement.PointerPressedEvent.Raised
    .Subscribe(tuple =>
    {
        var (sender, args) = tuple;
        Debug.WriteLine($"Pointer pressed on {sender}");
    });

// Observe when a route finishes (after all handlers ran)
IDisposable done = InputElement.PointerPressedEvent.RouteFinished
    .Subscribe(args => Debug.WriteLine($"Handled: {args.Handled}"));
```

These are useful for profiling, analytics, or implementing cross-cutting concern handlers without modifying every control.

---

## 9. Route diagnostics

The `HasRaisedSubscriptions` property tells you whether `Raised` has any observers — useful for conditionally enabling expensive diagnostics:

```csharp
if (PointerPressedEvent.HasRaisedSubscriptions)
{
    // Only do extra work if someone is listening
}
```

`InteractiveExtensions.GetInteractiveParent()` provides a fast parent hop for event-path inspection without walking the full visual tree.

---

## Key Takeaways

- Route order: class handlers → tunnel phase → bubble phase
- Avalonia uses `RoutedEvent<T>` (generic) instead of WPF's non-generic `RoutedEvent`
- No `Preview*` events — use `AddHandler` with `RoutingStrategies.Tunnel`
- `handledEventsToo` lets you observe events past the handled barrier
- Pointer capture redirects all pointer events to the capturer
- `Raised` / `RouteFinished` observables enable cross-cutting event monitoring

---

## See Also

- [051 — Routed Events](051-routed-events.md)
- [051E — Routed Events (examples)](051-routed-events-examples.md)
- [Avalonia Docs: Events Overview](https://docs.avaloniaui.net/docs/events)
- [Avalonia Docs: Input Events](https://docs.avaloniaui.net/docs/events/input-events)
- [Avalonia Docs: Routed Events Deep Dive](https://docs.avaloniaui.net/docs/input-interaction/routed-events)
