---
tier: intermediate
topic: input
estimated: 5 min
researched: 2026-06-18
avalonia-version: 12.0.4
quiz-of: 057-gesture-recognizers.md
quiz-type: comprehension
---

# 057Q — Gesture Recognizers Quiz

**Scenario:** You are building a cross-platform media viewer app with touch and mouse support. Answer the following questions.

---

## Q1. You attach `PinchGestureRecognizer` and `ScrollGestureRecognizer` to the same Image. What happens when the user touches with one finger?

**A1.** `ScrollGestureRecognizer` activates (one finger = pan). `PinchGestureRecognizer` waits for a second finger. Only one recognizer can be active at a time.

---

## Q2. When the user adds a second finger during a scroll gesture, what happens?

**A2.** `ScrollGestureRecognizer` deactivates and `PinchGestureRecognizer` activates. The recognizers independently monitor pointer count and hand off automatically.

---

## Q3. What does `e.Scale` represent in a `PinchEvent` handler? How should you apply it?

**A3.** `e.Scale` is the relative scale from the start of the pinch. Multiply it by the current zoom level: `double newZoom = currentZoom * e.Scale`.

---

## Q4. A pull gesture is not being recognized. What is the most likely cause?

**A4.** The `PullDirection` may be wrong, or the pointer might not be starting from the correct edge. `PullGestureRecognizer` requires the drag to begin at the configured edge of the control.

---

## Q5. What is the difference between `ScrollGestureRecognizer` and `PullGestureRecognizer`?

**A5.** `ScrollGestureRecognizer` is for free-form panning with inertia; `PullGestureRecognizer` is for deliberate single-direction edge-to-edge drags (like pull-to-refresh) without inertia. Pull requires a larger activation distance.

---

## Q6. Why does `SwipeGestureRecognizer` not respond to mouse by default? How do you enable it?

**A6.** Because `IsMouseEnabled` defaults to `false` — swipes are typically touch-only. Set `IsMouseEnabled="True"` to allow mouse to trigger swipe detection.

---

## Q7. Your swipe-based carousel is too sensitive — accidental slow drags trigger page changes. How do you fix it?

**A7.** Increase the `Threshold` property (min pixels before recognition) and check `e.Velocity` in the handler — only navigate if velocity exceeds a minimum threshold (e.g., 300 px/s).

---

## Q8. How would you prevent a stylus from triggering gesture recognizers in a drawing app?

**A8.** Handle `PointerPressed` on the control and call `e.PreventGestureRecognition()` when `e.Pointer.Type == PointerType.Pen`. This blocks all recognizers for pen input.

---

## Q9. When does `ScrollGestureInertiaStartingEvent` fire?

**A9.** It fires when the user releases the pointer after a scroll gesture and inertia is about to begin. You can customize deceleration in this handler.

---

## Q10. Name the three methods you override when building a custom gesture recognizer.

**A10.** `PointerPressed`, `PointerMoved`, and `PointerReleased` — all inherited from `GestureRecognizer`.

---

## Q11. A custom recognizer needs to raise a routed event. What is the correct pattern?

**A11.** Register a `RoutedEvent<TEventArgs>` on the recognizer class, call `RaiseEvent()` on the source element. Subscribe with `AddHandler` on the target control.

---

## Q12. True or False: A gesture recognizer continues to receive pointer events even when the pointer leaves the host control's bounds.

**A12.** True — once the recognizer captures the gesture (after activation), it receives pointer events until the gesture completes, even if the pointer leaves the control bounds.

---

## See Also

- [057 — Gesture Recognizers (core tutorial)](057-gesture-recognizers.md)
- [057V — Gesture Recognizers (verbose companion)](057-gesture-recognizers-verbose.md)
- [057E — Gesture Recognizers (examples)](057-gesture-recognizers-examples.md)
