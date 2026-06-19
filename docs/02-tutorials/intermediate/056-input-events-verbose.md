---
tier: intermediate
topic: input
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 056-input-events.md
---

# 056V — Input Events: An In-Depth Companion

**Why this exists:** The original tutorial covers the core pointer event API and gesture recognizers. This companion explores pointer identity and capture internals, `PointerPointProperties` in depth, gesture recognizer architecture, the `GetIntermediatePoints` API for touch inertia, cursor management, the WPF-to-Avalonia input model delta, and platform-specific input behavior.

**Cross-reference:** Original tutorial at [056-input-events.md](056-input-events.md).

---

## 1. Pointer identity — IPointer

Every pointing device (or finger) is assigned an `IPointer` instance when it first contacts the surface. The pointer persists across move and release events.

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    IPointer pointer = e.Pointer;
    uint id = pointer.Id;          // Stable across the gesture
    PointerType type = pointer.Type; // Mouse, Touch, Pen
    bool captured = pointer.Captured != null; // Currently captured?
}
```

### Pointer ID stability

- Each touch finger gets a unique ID for the duration of the touch
- The mouse always has the same pointer ID
- Pen devices assign a unique ID per stylus

This lets you track multi-touch: correlate `PointerPressed` → `PointerMoved` → `PointerReleased` for each finger.

---

## 2. Pointer capture in depth

When a pointer is captured to an element, that element receives all subsequent pointer events until the capture is released — even if the pointer leaves the element's bounds.

### Who can capture

```csharp
// Only the element that received PointerPressed may capture
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var element = sender as Visual;
    if (element is not null)
        e.Pointer.Capture(element);
}
```

### Why capture matters

- **Drag operations**: The dragging element continues to receive `PointerMoved` events even when the mouse leaves its bounds
- **Touch scrolling**: A `ScrollGestureRecognizer` captures internally to continue tracking
- **Button press-and-hold**: `Button` captures to detect release even if the pointer moves outside

### Capture lost

`PointerCaptureLost` fires when:
- The element releases capture via `e.Pointer.Capture(null)`
- Another element captures the pointer
- The pointer is disconnected (device removed, app loses focus)

### Checking capture state

```csharp
private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    Visual? captured = e.Pointer.Captured as Visual;
    if (captured == sender)
    {
        // We still have capture — handle drag
    }
}
```

---

## 3. PointerPointProperties deep dive

`e.Properties` (type `PointerPointProperties`) provides button and pressure state:

| Property | Type | Description |
|----------|------|-------------|
| `IsLeftButtonPressed` | `bool` | Left mouse button or primary touch contact |
| `IsRightButtonPressed` | `bool` | Right mouse button |
| `IsMiddleButtonPressed` | `bool` | Middle mouse button |
| `IsXButton1Pressed` | `bool` | First extended button |
| `IsXButton2Pressed` | `bool` | Second extended button |
| `IsBarrelButtonPressed` | `bool` | Stylus barrel button |
| `IsEraser` | `bool` | Stylus eraser end |
| `IsInverted` | `bool` | Stylus held inverted (eraser toward surface) |
| `Pressure` | `float` | 0.0–1.0 (stylus pressure) |
| `Twist` | `float` | Degrees of stylus rotation (0–359) |
| `XTilt` | `float` | Stylus tilt in X axis (-90 to 90) |
| `YTilt` | `float` | Stylus tilt in Y axis (-90 to 90) |
| `PointerUpdateKind` | `PointerUpdateKind` | Which button triggered this event |

### Pen pressure example

```csharp
private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    if (e.Pointer.Type == PointerType.Pen)
    {
        float pressure = e.Properties.Pressure; // 0.0–1.0
        float twist = e.Properties.Twist;       // degrees
        // Adjust stroke width by pressure
    }
}
```

### Distinguish left vs right click on PointerPressed

```csharp
private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
{
    var kind = e.Properties.PointerUpdateKind;

    if (kind == PointerUpdateKind.LeftButtonPressed)
        HandleLeftClick(e);
    else if (kind == PointerUpdateKind.RightButtonPressed)
        HandleRightClick(e);
}
```

---

## 4. GetIntermediatePoints — touch inertia trail

`GetIntermediatePoints` returns all pointer positions recorded since the last event. Useful for touch inertia and smooth gesture tracking:

```csharp
private void OnPointerMoved(object? sender, PointerEventArgs e)
{
    IReadOnlyList<PointerPoint> points =
        e.GetIntermediatePoints(sender as Visual);

    foreach (var pt in points)
    {
        // pt.Position, pt.Properties, pt.Timestamp
        DrawInkPoint(pt.Position, pt.Properties.Pressure);
    }
}
```

The list is empty on most mouse hardware; touch and pen devices typically return multiple points per event.

---

## 5. Gesture recognizer architecture

Gesture recognizers subclass `GestureRecognizer` and receive pointer events as protected method overrides:

```csharp
public class TouchOnlyPinchRecognizer : GestureRecognizer
{
    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Touch) return;
        // Track contact
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        // Calculate pinch scale
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        // End gesture
    }
}
```

### Recognizer lifecycle

1. Attached to `GestureRecognizers` collection
2. Receives `PointerPressed` for events on the hosting control
3. When a gesture is detected, the recognizer captures the pointer internally
4. Recognizer raises routed events (e.g., `PinchEvent`)
5. PointerReleased ends the gesture; recognizer releases capture

### Recognizer exclusivity

Only one recognizer can be active at a time. When one captures a gesture, others are prevented from activating. Use `e.PreventGestureRecognition()` in a pointer handler to exclude specific events from all recognizers.

---

## 6. ScrollGestureRecognizer configuration

```xml
<ScrollViewer>
  <ScrollGestureRecognizer CanHorizontallyScroll="True"
                            CanVerticallyScroll="True"
                            IsScrollInertiaEnabled="True" />
</ScrollViewer>
```

| Property | Default | Description |
|----------|---------|-------------|
| `CanHorizontallyScroll` | `true` | Allow horizontal scroll |
| `CanVerticallyScroll` | `true` | Allow vertical scroll |
| `IsScrollInertiaEnabled` | `true` | Continue scrolling after finger lift |

`ScrollGestureInertiaStarting` fires when the user releases the pointer and inertia begins. Use it to customize deceleration.

---

## 7. Cursor management

### Per-control cursor (most common)

```xml
<Button Cursor="Hand" Content="Click" />
```

### Override cursor at any level

```xml
<Panel Cursor="Wait"> <!-- All children show "Wait" unless overridden -->
  <Button Cursor="Arrow" Content="Fine" /> <!-- Child overrides -->
</Panel>
```

### Custom cursor from a bitmap

```csharp
using var bitmap = new Bitmap("cursor.ico");
var customCursor = new Cursor(bitmap, new PixelPoint(0, 0));
myControl.Cursor = customCursor;
```

### Platform cursor limits

- Windows: `.cur` and `.ani` files supported; `.png` may not work
- macOS: 32×32 max recommended
- Linux: Xcursor formats
- WASM: Predefined cursors only (Arrow, Hand, etc.)

---

## 8. Avalonia vs WPF input model

| Concept | Avalonia | WPF |
|---------|----------|-----|
| Input unification | Unified pointer (mouse/touch/pen) | Separate `Mouse*`, `Touch*`, `Stylus*` events |
| Event args base | `PointerEventArgs` → `RoutedEventArgs` | `MouseEventArgs` → `InputEventArgs` |
| Position accessor | `e.GetPosition(visual)` | `e.GetPosition(element)` |
| Button state | `e.Properties.IsLeftButtonPressed` | `e.LeftButton == MouseButtonState.Pressed` |
| Stylus pressure | `e.Properties.Pressure` | `e.GetStylusPoint().PressureFactor` |
| Pointer capture | `e.Pointer.Capture(visual)` | `CaptureMouse()` |
| Preview events | Use `AddHandler(..., RoutingStrategies.Tunnel)` | Separate `PreviewMouseDown` events |
| Scroll delta | `e.Delta` (Vector) — positive Y = scroll up | `e.Delta` (int) — positive = scroll up |
| Holding gesture | `InputElement.IsHoldingEnabled` attached property | Not built-in |
| Pinch/Swipe/Scroll recognizers | Built-in `GestureRecognizers` | Not built-in (need toolkit) |
| Cursor property | `Cursor` on `InputElement` | `Cursor` on `FrameworkElement` |

---

## 9. Platform difference notes

| Behavior | Windows | macOS | Linux | WASM |
|----------|---------|-------|-------|------|
| Right-click | Physical right button | Ctrl+Click or two-finger click | Physical right button | Long-press or Ctrl+Click |
| Touch pointers | 10+ simultaneous | Varies by hardware | Varies by hardware | 1+ (browser limits) |
| Stylus pressure | WinRT pen API | Apple Pencil API | Wacom tablet / libinput | Partial (W3C pointer events) |
| Scroll wheel delta | 120 units per notch | Variable per hardware | 120 units per notch | Mouse wheel events |

---

## See Also

- [056 — Input Events (core tutorial)](056-input-events.md)
- [056E — Input Events (examples)](056-input-events-examples.md)
- [Avalonia Docs: Pointer Events](https://docs.avaloniaui.net/docs/input-interaction/pointer)
- [Avalonia Docs: Gestures Overview](https://docs.avaloniaui.net/docs/input-interaction/gestures)
