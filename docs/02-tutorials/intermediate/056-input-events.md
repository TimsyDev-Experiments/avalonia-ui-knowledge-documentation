---
tier: intermediate
topic: input
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 056 — Input Events

**What you'll learn:** How to handle pointer (mouse/touch/stylus) and wheel events in Avalonia, understand `PointerEventArgs`, and use gesture recognizers for higher-level interactions.

**Prerequisites:** [051 — Routed Events](051-routed-events.md), [054 — Focus Management](054-focus-management.md)

---

## 1. Unified pointer model

Avalonia unifies mouse, touch, and stylus input into a single pointer event system. All devices fire the same events: `PointerPressed`, `PointerMoved`, `PointerReleased`. Check `e.Pointer.Type` to distinguish.

| PointerType | Device |
|-------------|--------|
| `Mouse` | Physical mouse |
| `Touch` | Finger touch |
| `Pen` | Stylus / digitizer pen |

---

## 2. Pointer events reference

All pointer events are routed events (bubble strategy) on `InputElement`. Subscribe in XAML or via `AddHandler`.

| Event | EventArgs | When it fires |
|-------|-----------|---------------|
| `PointerEntered` | `PointerEventArgs` | Pointer enters element bounds |
| `PointerExited` | `PointerEventArgs` | Pointer leaves element bounds |
| `PointerMoved` | `PointerEventArgs` | Pointer moves within element |
| `PointerPressed` | `PointerPressedEventArgs` | Button pressed over element |
| `PointerReleased` | `PointerReleasedEventArgs` | Button released over element |
| `PointerWheelChanged` | `PointerWheelEventArgs` | Mouse wheel scrolled |
| `PointerCaptureLost` | `PointerEventArgs` | Pointer capture taken or released |

---

## 3. Key API — PointerEventArgs

All pointer-specific event args inherit from `PointerEventArgs` (which inherits from `RoutedEventArgs`):

```csharp
// Position relative to a control
Point pos = e.GetPosition(relativeToControl);

// Pointer identity and type
IPointer ptr = e.Pointer;
PointerType type = ptr.Type;        // Mouse, Touch, Pen

// Button state at time of event
PointerPointProperties props = e.Properties;
bool isLeft = props.IsLeftButtonPressed;
bool isRight = props.IsRightButtonPressed;

// Modifier keys
KeyModifiers mods = e.KeyModifiers; // Ctrl, Shift, Alt, Meta

// Prevent gesture recognizers from processing
e.PreventGestureRecognition();
```

### PointerPressedEventArgs

```csharp
int clicks = e.ClickCount; // 1 for single, 2 for double, etc.
```

### PointerReleasedEventArgs

```csharp
MouseButton initialButton = e.InitialPressMouseButton;
// The button that was pressed when the gesture started
```

### PointerWheelEventArgs

```csharp
Vector delta = e.Delta;
// delta.X > 0 → scroll right
// delta.Y > 0 → scroll up (standard)

// Check if touchpad inertia is active
bool isInertial = e.IsInertial;
```

---

## 4. Handling pointer events

### XAML

```xml
<Border Background="Transparent"
        PointerPressed="OnPointerPressed"
        PointerMoved="OnPointerMoved"
        PointerReleased="OnPointerReleased"
        PointerWheelChanged="OnPointerWheelChanged" />
```

### Code-behind

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    Point pos = e.GetPosition(sender as Visual);
    bool isRightClick = e.GetCurrentPoint(null).Properties.IsRightButtonPressed;

    if (e.ClickCount == 2)
        Debug.WriteLine("Double-click detected");
}
```

### Subscribe via AddHandler (tunnel phase)

```csharp
myControl.AddHandler(
    InputElement.PointerPressedEvent,
    OnPreviewPointerPressed,
    RoutingStrategies.Tunnel);
```

---

## 5. Pointer capture

When you capture the pointer, the control receives all subsequent pointer events (moved, released) even if the pointer leaves the element's bounds.

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    // Capture
    e.Pointer.Capture(sender as Visual);

    // Later: release
    // e.Pointer.Capture(null);
}

private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    // Still receives events even outside bounds
    Point pos = e.GetPosition(sender as Visual);
}
```

The `PointerCaptureLost` event fires when capture is taken by another element or when the pointer is released naturally.

---

## 6. Gesture events (built-in)

These are higher-level events that do not require a `GestureRecognizer`:

| Event | Args | Description |
|-------|------|-------------|
| `Tapped` | `TappedEventArgs` | Pressed and released — a click |
| `DoubleTapped` | `TappedEventArgs` | Two taps in quick succession |
| `RightTapped` | `RightTappedEventArgs` | Right-button click |
| `Holding` | `HoldingEventArgs` | Press-and-hold; requires `IsHoldingEnabled` |

### Holding requires opt-in

```xml
<Border InputElement.IsHoldingEnabled="True"
        Holding="OnHolding" />
```

For mouse to trigger holding:

```xml
<Border InputElement.IsHoldingEnabled="True"
        InputElement.IsHoldWithMouseEnabled="True"
        Holding="OnHolding" />
```

---

## 7. Gesture recognizers

For multi-pointer or directional gestures, attach a `GestureRecognizer` to the control's `GestureRecognizers` collection.

| Recognizer | Events Raised | Use case |
|------------|---------------|----------|
| `PinchGestureRecognizer` | `PinchEvent`, `PinchEndedEvent` | Pinch-to-zoom |
| `PullGestureRecognizer` | `PullGestureEvent`, `PullGestureEndedEvent` | Pull-to-refresh from edge |
| `ScrollGestureRecognizer` | `ScrollGestureEvent`, `ScrollGestureEndedEvent`, `ScrollGestureInertiaStartingEvent` | Touch/mouse panning |
| `SwipeGestureRecognizer` | `SwipeGestureEvent`, `SwipeGestureEndedEvent` | Directional swipe for paging |

```xml
<Image Source="/photo.jpg">
  <Image.GestureRecognizers>
    <PinchGestureRecognizer />
    <ScrollGestureRecognizer CanHorizontallyScroll="True"
                              CanVerticallyScroll="True" />
  </Image.GestureRecognizers>
</Image>
```

Subscribe to gesture events via `AddHandler`:

```csharp
image.AddHandler(InputElement.PinchEvent, (s, e) =>
{
    double scale = e.Scale;
    // Apply scale transform
});
```

### v12 note

The `Gestures.` prefix was removed. Use `PinchEvent` instead of `Gestures.PinchEvent`.

---

## 8. Cursor changes

```xml
<Button Cursor="Hand" Content="Hover me" />
<Panel Cursor="Cross" />
```

Common cursors: `Arrow`, `Hand`, `IBeam`, `Cross`, `SizeAll`, `SizeNS`, `SizeWE`, `Wait`.

Set cursors in code:

```csharp
myControl.Cursor = new Cursor(StandardCursorType.Hand);
```

---

## Key Takeaways

- Unified pointer model: mouse, touch, stylus → same events
- Use `e.GetPosition(visual)` for coordinates, `e.Pointer.Type` for device
- Capture pointer with `e.Pointer.Capture(visual)` to receive events after leaving bounds
- Built-in gesture events: `Tapped`, `DoubleTapped`, `RightTapped`, `Holding`
- Gesture recognizers: `Pinch`, `Pull`, `Scroll`, `Swipe` — attach to `GestureRecognizers`
- Use `PreventGestureRecognition()` to stop pointer events from being interpreted as gestures

---

## See Also

- [056V — Input Events (verbose companion)](056-input-events-verbose.md)
- [056E — Input Events (examples)](056-input-events-examples.md)
- [Avalonia Docs: Pointer Events](https://docs.avaloniaui.net/docs/input-interaction/pointer)
- [Avalonia Docs: Gestures Overview](https://docs.avaloniaui.net/docs/input-interaction/gestures)
