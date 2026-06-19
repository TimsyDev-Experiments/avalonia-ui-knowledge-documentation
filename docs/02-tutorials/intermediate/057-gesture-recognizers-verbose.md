---
tier: intermediate
topic: input
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 057-gesture-recognizers.md
---

# 057V — Gesture Recognizers: An In-Depth Companion

**Why this exists:** The original tutorial covers attaching and using the four built-in recognizers. This companion explores recognizer lifecycle and exclusivity, inertia physics, `ScrollGestureInertiaStarting` customization, advanced swipe velocity gating, combined recognizer conflict resolution, custom recognizer internals, and the WPF comparison.

**Cross-reference:** Original tutorial at [057-gesture-recognizers.md](057-gesture-recognizers.md).

---

## 1. Recognizer lifecycle

Each gesture recognizer follows this lifecycle:

1. **Attached** to `GestureRecognizers` collection
2. **Idle** — monitoring pointer events on the host control
3. **Active** — gesture detected; recognizer captures the pointer
4. **Raise events** — fires routed events (start, update, end)
5. **Complete** — pointer released; recognizer releases capture
6. Back to **Idle**

During the Active phase, the recognizer captures the pointer internally. Other recognizers on the same control cannot activate until the current gesture completes or is canceled.

```csharp
public class MyRecognizer : GestureRecognizer
{
    private bool _isActive;

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (_isActive) return;
        _isActive = true;
        // Recognizer captures internally
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        _isActive = false;
    }
}
```

---

## 2. Recognizer exclusivity in detail

Only one recognizer can be active at a time on a given control. The system uses a shared "gesture in progress" flag on the host `InputElement`.

### What happens when two recognizers are attached

```xml
<Image.GestureRecognizers>
  <PinchGestureRecognizer />
  <ScrollGestureRecognizer CanHorizontallyScroll="True"
                            CanVerticallyScroll="True" />
</Image.GestureRecognizers>
```

- User touches with one finger → `ScrollGestureRecognizer` activates
- User adds a second finger → `ScrollGestureRecognizer` releases; `PinchGestureRecognizer` activates
- User lifts one finger → `PinchGestureRecognizer` deactivates; `ScrollGestureRecognizer` can reactivate if pointer remains

This automatic handoff happens because the recognizers monitor pointer count changes independently.

### Preventing a recognizer from activating

Call `e.PreventGestureRecognition()` in a `PointerPressed` handler. Subsequent events in the same gesture will not activate any recognizer:

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (someCondition)
        e.PreventGestureRecognition();
}
```

---

## 3. ScrollGestureInertiaStarting

This event fires when the user releases the pointer and inertia begins. You can customize the deceleration by modifying `e.InertiaDelta`.

```csharp
image.AddHandler(InputElement.ScrollGestureInertiaStartingEvent,
    (s, e) =>
{
    // Default inertia continues at current velocity
    // Customize by setting InertiaDelta (cumulative)
    // Or handle your own deceleration and mark args handled
});
```

### Inertia physics

Scroll inertia uses a simple deceleration model: the velocity decreases by a fixed factor each frame until it reaches zero. The `ScrollGestureInertiaStartingEvent` lets you override the deceleration by tracking your own velocity curve.

---

## 4. Swipe velocity gating

`SwipeGestureRecognizer` provides `e.Velocity` as a `Vector` in pixels per second. Use it to determine whether a swipe was fast enough to trigger page navigation:

```csharp
image.AddHandler(InputElement.SwipeGestureEndedEvent, (s, e) =>
{
    // Minimum velocity threshold (pixels/sec)
    const double minSwipeVelocity = 300;

    if (Math.Abs(e.Velocity.X) > minSwipeVelocity)
    {
        if (e.Velocity.X > 0) // Rightward
            NavigateBack();
        else // Leftward
            NavigateForward();
    }
});
```

### Combining threshold and velocity

The `Threshold` property (min pixels before recognition) gates *activation*. The `Velocity` property in event args gates *action*. This two-stage approach prevents accidental navigation from slow drags.

---

## 5. Pointer type filtering in built-in recognizers

All built-in recognizers respond to all pointer types (mouse, touch, pen) — except `SwipeGestureRecognizer` where `IsMouseEnabled` defaults to `false`.

If you need device-specific gesture handling (e.g., touch only for pinch-zoom, pen only for drawing), you have two options:

1. **Pointer event handler with `PreventGestureRecognition`**: block specific pointer types before they reach recognizers
2. **Custom gesture recognizer**: subclass and filter by `e.Pointer.Type`

Option 1 is simpler for most cases:

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    if (e.Pointer.Type == PointerType.Pen)
        e.PreventGestureRecognition(); // Block recognizers for pen
}
```

---

## 6. Custom recognizer — full pattern

```csharp
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

public class HoldToActionRecognizer : GestureRecognizer
{
    private bool _tracking;
    private Point _startPoint;
    private readonly TimeSpan _holdDuration = TimeSpan.FromMilliseconds(500);

    public static readonly RoutedEvent<HoldActionEventArgs> HoldActionEvent =
        RoutedEvent.Register<HoldToActionRecognizer, HoldActionEventArgs>(
            "HoldAction", RoutingStrategies.Bubble);

    static HoldToActionRecognizer()
    {
        HoldActionEvent.AddClassHandler<HoldToActionRecognizer>(
            (r, e) => { });
    }

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (_tracking) return;
        _tracking = true;
        _startPoint = e.GetPosition(null);
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (!_tracking) return;

        Point pos = e.GetPosition(null);
        double distance = (pos - _startPoint).Length;

        // Cancel if moved too far
        if (distance > 20)
        {
            _tracking = false;
        }
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        if (!_tracking) return;
        _tracking = false;

        double distance = (e.GetPosition(null) - _startPoint).Length;

        // If released near start point after hold time
        if (distance < 20)
        {
            var args = new HoldActionEventArgs(
                HoldActionEvent, e.Pointer, e.GetPosition(null));
            e.Source?.RaiseEvent(args);
        }
    }
}

public class HoldActionEventArgs : RoutedEventArgs
{
    public HoldActionEventArgs(RoutedEvent routedEvent,
        IPointer pointer, Point position) : base(routedEvent)
    {
        Pointer = pointer;
        Position = position;
    }

    public IPointer Pointer { get; }
    public Point Position { get; }
}
```

Usage:

```xml
<Border.GestureRecognizers>
  <local:HoldToActionRecognizer />
</Border.GestureRecognizers>
```

```csharp
border.AddHandler(HoldToActionRecognizer.HoldActionEvent, (s, e) =>
{
    Debug.WriteLine($"Hold action at {e.Position}");
});
```

---

## 7. Avalonia vs WPF gestures

| Concept | Avalonia | WPF |
|---------|----------|-----|
| Built-in pinch/zoom | `PinchGestureRecognizer` | Not built-in |
| Built-in pan/scroll | `ScrollGestureRecognizer` | Built-in `ScrollViewer` |
| Pull-to-refresh | `PullGestureRecognizer` | Not built-in |
| Swipe detection | `SwipeGestureRecognizer` | Not built-in |
| Touch manipulation | `GestureRecognizers` system | `ManipulationStarting/Delta/Completed` events |
| Inertia | Built-in (`IsScrollInertiaEnabled`) | `ManipulationInertiaStarting` + `ManipulationDelta` |
| Custom recognizer | Subclass `GestureRecognizer` | Subclass `ManipulationProcessor` or use `Touch` events |
| Unified input model | Pointer events (mouse + touch + pen) | Separate mouse/touch/stylus events |

Avalonia's gesture recognizer system is more modular and extensible than WPF's manipulation event model. Rather than relying on a single `ManipulationProcessor`, Avalonia lets you compose individual recognizers per control.

---

## 8. Platform considerations

| Gesture | Windows | macOS | Linux | WASM |
|---------|---------|-------|-------|------|
| Pinch (two-finger) | Touch + Precision Touchpad | Touch + Trackpad | Touch (limited) | Touch (limited) |
| Pull (edge drag) | Touch | Touch | Touch | Touch |
| Scroll (pan) | Touch + Mouse drag | Touch + Trackpad | Touch + Mouse drag | Touch |
| Swipe (flick) | Touch (`IsMouseEnabled` off by default) | Touch + Trackpad | Touch | Touch |

`Touchpad` gestures on Windows require Precision Touchpad drivers. On macOS, the trackpad maps touch gestures natively.

---

## See Also

- [057 — Gesture Recognizers (core tutorial)](057-gesture-recognizers.md)
- [057E — Gesture Recognizers (examples)](057-gesture-recognizers-examples.md)
- [Avalonia Docs: Gestures](https://docs.avaloniaui.net/docs/input-interaction/gestures)
