---
tier: advanced
topic: custom controls
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 021-custom-controls-from-scratch.md
---

# Quiz — Custom Controls from Scratch

```quiz
Q: Which base class should you extend for a completely lookless control that overrides Render for all drawing?
A. UserControl || UserControl composes existing controls and does not support Render override for custom drawing.
B. Control (correct) || Control is the direct Visual subclass with no template, designed for custom Render overrides.
C. TemplatedControl || TemplatedControl provides a ControlTemplate system — it is not lookless.
D. Decorator || Decorator wraps a single child and does not support standalone custom rendering.
```

```quiz
Q: What does calling AffectsRender<SignaturePad>(BackgroundProperty) in the static constructor do?
A. It registers BackgroundProperty to trigger InvalidateMeasure when changed. || AffectsRender does not affect measure — it triggers re-render, not re-layout.
B. It makes the property read-only in the XAML compiler. || AffectsRender has no effect on property accessors or XAML compilation.
C. It automatically calls InvalidateVisual when BackgroundProperty changes. (correct) || AffectsRender wires property changes to InvalidateVisual so the control redraws without manual calls.
D. It binds the BackgroundProperty to a default render brush. || AffectsRender only invalidates the visual; it does not set values or bind properties.
```

```quiz
Q: In the SignaturePad.OnPointerPressed method, why is e.Pointer.Capture(this) called?
A. To prevent the pointer from moving outside the control's bounds. || Capture does not restrict pointer movement — it routes subsequent events to the capturing element.
B. To ensure all subsequent pointer events (Move, Release) are routed to this control even if the pointer leaves its bounds. (correct) || Pointer capture redirects all follow-up events to the capturer, enabling drag tracking across element boundaries.
C. To change the cursor icon to a crosshair. || Capture does not affect the cursor appearance.
D. To make the control focusable via pointer. || Focus and capture are separate concepts; Focus() is needed for keyboard focus.
```

```quiz
Q: The SignaturePad stores stroke points in a List<Point>. What must be called after adding a point in OnPointerMoved so the stroke appears on screen?
A. Measure(availableSize) || Measure triggers layout, not rendering; it would not draw the new point.
B. InvalidateArrange() || InvalidateArrange triggers re-layout; the stroke is rendered during Render, not arrange.
C. InvalidateVisual() (correct) || InvalidateVisual marks the visual as dirty so that Render is called on the next frame, drawing the accumulated points.
D. base.OnPointerMoved(e) || The base call passes the event up but does not trigger a re-render by itself.
```

```quiz
Q: What does MeasureOverride return to communicate the control's desired size to the layout system?
A. The availableSize parameter unchanged. || The control must return its desired size, which may differ from the available space.
B. A Size computed from default dimensions (300 x 150) clamped by the availableSize constraint. (correct) || The tutorial returns new Size(300, 150) but reduces each dimension when availableSize is smaller.
C. Size.Infinity in both dimensions. || Returning infinity requests unbounded space; the default behavior uses finite defaults.
D. The final arranged size from ArrangeOverride. || Arrange is called after Measure and has not yet run when MeasureOverride executes.
```
