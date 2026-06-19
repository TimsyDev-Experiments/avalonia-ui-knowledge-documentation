---
tier: intermediate
topic: input
estimated: 5 min
researched: 2026-06-18
avalonia-version: 12.0.4
quiz-of: 056-input-events.md
quiz-type: comprehension
---

# 056Q — Input Events Quiz

**Scenario:** You are building a drawing app in Avalonia that supports mouse, touch, and stylus input. Answer the following questions.

---

## Q1. How does Avalonia unify mouse, touch, and stylus input?

**A1.** All three device types fire the same pointer events (`PointerPressed`, `PointerMoved`, `PointerReleased`) on the unified `InputElement` class. Use `e.Pointer.Type` (returns `Mouse`, `Touch`, or `Pen`) to distinguish.

---

## Q2. Why would you call `e.Pointer.Capture(visual)` in a `PointerPressed` handler?

**A2.** To ensure the element continues receiving `PointerMoved` and `PointerReleased` events even when the pointer leaves the element's bounds. Essential for drag and drawing operations.

---

## Q3. What event fires when pointer capture is lost? Name two scenarios that trigger it.

**A3.** `PointerCaptureLost`. It fires when: (1) the element releases capture via `e.Pointer.Capture(null)`, (2) another element captures the pointer, or (3) the pointer is disconnected.

---

## Q4. How do you get the pointer position relative to a specific control?

**A4.** `e.GetPosition(control as Visual)` returns the position as a `Point` in the control's coordinate space.

---

## Q5. Which property on `PointerEventArgs` tells you if the Shift key was held during the event?

**A5.** `e.KeyModifiers` — check `e.KeyModifiers.HasFlag(KeyModifiers.Shift)`.

---

## Q6. What does `PointerPressedEventArgs.ClickCount` give you?

**A6.** The number of clicks at the current location (1 = single click, 2 = double click, etc.).

---

## Q7. How do you check which mouse button triggered a `PointerReleased` event?

**A7.** Use `e.InitialPressMouseButton` (the button that was pressed when the gesture started). Alternatively, check `e.Properties.PointerUpdateKind` on the pressed event.

---

## Q8. Name the four built-in gesture events that do NOT require a `GestureRecognizer`.

**A8.** `Tapped` (click), `DoubleTapped` (double-click), `RightTapped` (right-click), and `Holding` (press-and-hold, requires `InputElement.IsHoldingEnabled="True"`).

---

## Q9. What is the v12 change regarding gesture event names?

**A9.** The `Gestures.` prefix was removed. Use `PinchEvent` instead of `Gestures.PinchEvent`, `ScrollGestureEvent` instead of `Gestures.ScrollGestureEvent`, etc.

---

## Q10. Two gesture recognizers are attached to the same control. What happens when the user starts a pinch?

**A10.** The `PinchGestureRecognizer` captures the gesture, preventing the other recognizer from activating. Only one recognizer can be active at a time.

---

## Q11. How would you prevent a specific pointer event from being interpreted by gesture recognizers?

**A11.** Call `e.PreventGestureRecognition()` on the `PointerEventArgs`. This excludes the current event and subsequent events in the gesture from all recognizers.

---

## Q12. What does `PointerWheelEventArgs.Delta` represent?

**A12.** A `Vector` where `Delta.X` is the horizontal scroll amount (positive = right) and `Delta.Y` is the vertical scroll amount (positive = up). Typical mouse scroll gives ~120 units per notch.

---

## See Also

- [056 — Input Events (core tutorial)](056-input-events.md)
- [056V — Input Events (verbose companion)](056-input-events-verbose.md)
- [056E — Input Events (examples)](056-input-events-examples.md)
