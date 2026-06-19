---
tier: intermediate
topic: input
estimated: 12 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 057 — Gesture Recognizers

**What you'll learn:** A deep dive into Avalonia's built-in gesture recognizers — `Pinch`, `Pull`, `Scroll`, and `Swipe` — their configuration, events, and how to build custom recognizers.

**Prerequisites:** [056 — Input Events](056-input-events.md), [051 — Routed Events](051-routed-events.md)

---

## 1. Quick reference

All four recognizers attach via the `GestureRecognizers` collection and raise routed events:

| Recognizer | Events Raised | Use Case |
|------------|---------------|----------|
| `PinchGestureRecognizer` | `PinchEvent`, `PinchEndedEvent` | Zoom (two-finger pinch) |
| `PullGestureRecognizer` | `PullGestureEvent`, `PullGestureEndedEvent` | Pull-to-refresh from edge |
| `ScrollGestureRecognizer` | `ScrollGestureEvent`, `ScrollGestureEndedEvent`, `ScrollGestureInertiaStartingEvent` | Panning with inertia |
| `SwipeGestureRecognizer` | `SwipeGestureEvent`, `SwipeGestureEndedEvent` | Page navigation (flick) |

**v12 note:** The `Gestures.` prefix was removed. Use `PinchEvent` instead of `Gestures.PinchEvent`.

---

## 2. Attaching a recognizer

```xml
<Image Source="/photo.jpg">
  <Image.GestureRecognizers>
    <PinchGestureRecognizer />
    <ScrollGestureRecognizer CanHorizontallyScroll="True"
                              CanVerticallyScroll="True" />
  </Image.GestureRecognizers>
</Image>
```

```csharp
image.GestureRecognizers.Add(new PinchGestureRecognizer());
image.GestureRecognizers.Add(new ScrollGestureRecognizer
{
    CanVerticallyScroll = true,
    CanHorizontallyScroll = true,
});
```

Only one recognizer can be active at a time. When one captures a gesture, others are blocked until it completes.

---

## 3. PinchGestureRecognizer

Zoom interaction with two pointers. No configurable properties.

```xml
<Image.GestureRecognizers>
  <PinchGestureRecognizer />
</Image.GestureRecognizers>
```

```csharp
image.AddHandler(InputElement.PinchEvent, (s, e) =>
{
    double scale = e.Scale; // relative to pinch start
    ApplyZoom(scale);
});

image.AddHandler(InputElement.PinchEndedEvent, (s, e) =>
{
    // Gesture completed
});
```

`e.Scale` is relative — multiply it by the current zoom level.

---

## 4. PullGestureRecognizer

Edge-to-edge drag for pull-to-refresh. Configured with `PullDirection`.

```xml
<Border.GestureRecognizers>
  <PullGestureRecognizer PullDirection="TopToBottom" />
</Border.GestureRecognizers>
```

```csharp
border.AddHandler(InputElement.PullGestureEvent, (s, e) =>
{
    // Continuously called as pointer moves
    double pullDistance = e.Distance;
    UpdateRefreshIndicator(pullDistance);
});

border.AddHandler(InputElement.PullGestureEndedEvent, (s, e) =>
{
    // Pointer released — check if threshold crossed
    ExecuteRefresh();
});
```

| PullDirection | Description |
|---------------|-------------|
| `TopToBottom` | Drag from top edge downward |
| `BottomToTop` | Drag from bottom edge upward |
| `LeftToRight` | Drag from left edge rightward |
| `RightToLeft` | Drag from right edge leftward |

Pull requires a larger initial drag than `ScrollGestureRecognizer`, has no inertia, and tracks only one direction.

---

## 5. ScrollGestureRecognizer

Free-form panning with optional inertia.

```xml
<Image.GestureRecognizers>
  <ScrollGestureRecognizer CanHorizontallyScroll="True"
                            CanVerticallyScroll="True"
                            IsScrollInertiaEnabled="True" />
</Image.GestureRecognizers>
```

| Property | Default | Description |
|----------|---------|-------------|
| `CanHorizontallyScroll` | `true` | Enable horizontal pan |
| `CanVerticallyScroll` | `true` | Enable vertical pan |
| `IsScrollInertiaEnabled` | `true` | Continue scrolling after release |

```csharp
image.AddHandler(InputElement.ScrollGestureEvent, (s, e) =>
{
    // e.Delta is the scroll vector (pixels)
    OffsetX += e.Delta.X;
    OffsetY += e.Delta.Y;
});

image.AddHandler(InputElement.ScrollGestureInertiaStartingEvent, (s, e) =>
{
    // Customize deceleration here
});

image.AddHandler(InputElement.ScrollGestureEndedEvent, (s, e) =>
{
    // Scrolling fully stopped (including inertia)
});
```

---

## 6. SwipeGestureRecognizer

Discrete directional flick for paging. Provides velocity data. Mouse is disabled by default.

```xml
<Border.GestureRecognizers>
  <SwipeGestureRecognizer CanHorizontallySwipe="True"
                           Threshold="50"
                           IsMouseEnabled="True" />
</Border.GestureRecognizers>
```

| Property | Default | Description |
|----------|---------|-------------|
| `CanHorizontallySwipe` | `false` | Enable left/right swipes |
| `CanVerticallySwipe` | `false` | Enable up/down swipes |
| `Threshold` | `0` | Min pixels before recognition (0 = platform default) |
| `IsMouseEnabled` | `false` | Allow mouse to trigger swipes |
| `IsEnabled` | `true` | Enable/disable the recognizer |

```csharp
image.AddHandler(InputElement.SwipeGestureEvent, (s, e) =>
{
    // During swipe
    Vector delta = e.Delta;
    Vector velocity = e.Velocity;
    SwipeDirection dir = e.SwipeDirection;
});

image.AddHandler(InputElement.SwipeGestureEndedEvent, (s, e) =>
{
    // Check velocity for page navigation
    if (Math.Abs(e.Velocity.X) > 200)
    {
        if (e.Velocity.X < 0) GoNextPage();
        else GoPreviousPage();
    }
});
```

`SwipeGestureEventArgs` properties: `Id` (gesture sequence id), `Delta` (pixels since last event), `Velocity` (pixels/sec), `SwipeDirection` (`Left`, `Right`, `Up`, `Down`).

---

## 7. Custom gesture recognizers

Subclass `GestureRecognizer` and override pointer methods:

```csharp
public class TouchOnlyPanRecognizer : GestureRecognizer
{
    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Touch) return;
        // Begin tracking
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        // Calculate delta
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        // End gesture
    }
}
```

Use `e.PreventGestureRecognition()` from a pointer handler to block specific events from built-in recognizers.

---

## Key Takeaways

- Attach recognizers to `GestureRecognizers` collection — only one active at a time
- `Pinch` — two-finger zoom; `e.Scale` is relative to start
- `Pull` — edge drag for refresh; configure `PullDirection`
- `Scroll` — pan with inertia; `e.Delta` gives movement vector
- `Swipe` — flick for paging; provides `Velocity` for speed-sensitive transitions
- Custom recognizers: subclass `GestureRecognizer`, override `PointerPressed`/`Moved`/`Released`

---

## See Also

- [057V — Gesture Recognizers (verbose companion)](057-gesture-recognizers-verbose.md)
- [057E — Gesture Recognizers (examples)](057-gesture-recognizers-examples.md)
- [Avalonia Docs: Gestures](https://docs.avaloniaui.net/docs/input-interaction/gestures)
