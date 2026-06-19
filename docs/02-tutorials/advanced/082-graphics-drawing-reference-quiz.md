---
tier: advanced
topic: rendering
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 082Q — Graphics & Drawing Reference (quiz)

## Q1. Which method on `DrawingContext` applies a uniform transparency to subsequent draws?

- [ ] A. `PushClip()`
- [ ] B. `PushTransform()`
- [ ] C. `PushOpacity()`
- [ ] D. `PushTransparency()`

**Answer:** C. `PushOpacity(double)` applies a uniform opacity to all subsequent draw operations until disposed.

---

## Q2. Which brush type paints an area using the rendered output of another control?

- [ ] A. `ImageBrush`
- [ ] B. `VisualBrush`
- [ ] C. `LinearGradientBrush`
- [ ] D. `RadialGradientBrush`

**Answer:** B. `VisualBrush` captures the visual appearance of any control and uses it as fill.

---

## Q3. What does `CombinedGeometry GeometryCombineMode="Exclude"` do?

- [ ] A. Removes both geometries from rendering
- [ ] B. Shows only the non-overlapping area of both geometries
- [ ] C. Shows area in the first geometry but not in the second
- [ ] D. Shows area not in either geometry

**Answer:** C. `Exclude` keeps area from Geometry1 that does not overlap Geometry2.

---

## Q4. Which geometry type is most performant for static icon paths?

- [ ] A. `PathGeometry`
- [ ] B. `StreamGeometry`
- [ ] C. `RectangleGeometry`
- [ ] D. `CombinedGeometry`

**Answer:** B. `StreamGeometry` is lightweight, immutable, and optimized for static shapes.

---

## Q5. How do you create a dashed line on a Shape control?

- [ ] A. `StrokeDash="5 3"`
- [ ] B. `StrokeDashArray="5,3"`
- [ ] C. `DashArray="5,3"`
- [ ] D. `StrokePattern="5,3"`

**Answer:** B. `StrokeDashArray` is a comma-separated collection of doubles defining the dash pattern.

---

## Q6. Which effect applies to any Visual (not just Border)?

- [ ] A. `BoxShadow`
- [ ] B. `DropShadowEffect`
- [ ] C. `CornerRadius`
- [ ] D. `BorderThickness`

**Answer:** B. `DropShadowEffect` works on any Visual through the `Effect` property. `BoxShadow` only applies to `Border` and `ContentPresenter`.

---

## Q7. What is the purpose of `AffectsRender<T>(AvaloniaProperty)`?

- [ ] A. It prevents the control from being rendered
- [ ] B. It automatically calls `InvalidateVisual()` when the property changes
- [ ] C. It forces a layout pass when the property changes
- [ ] D. It optimizes rendering by skipping the property

**Answer:** B. `AffectsRender` registers a property change handler that calls `InvalidateVisual()`, ensuring `Render` is called when the data changes.

---

## Q8. In `ArcSegment`, what parameter determines whether the arc is the larger (>180°) segment?

- [ ] A. `IsLargeArc`
- [ ] B. `SweepDirection`
- [ ] C. `RotationAngle`
- [ ] D. `Size`

**Answer:** A. `IsLargeArc` selects between the smaller and larger arc paths between the same start and end points.

---

## Q9. Which `TextLayout` feature is NOT supported by `FormattedText`?

- [ ] A. Font size
- [ ] B. Text color
- [ ] C. `TextAlignment.Justify`
- [ ] D. Typeface selection

**Answer:** C. Only `TextLayout` supports justified text alignment. `FormattedText` does not.

---

## Q10. What does `canvas.Clear()` do in SkiaSharp `ICustomDrawOperation.Render`?

- [ ] A. Clears only the last drawn shape
- [ ] B. Fills the canvas with transparent pixels
- [ ] C. Fills the entire canvas with the specified color
- [ ] D. Resets the transform matrix

**Answer:** C. `canvas.Clear(SKColors.White)` fills the entire canvas with white. Without arguments it fills with transparent black.

---

## Q11. How do you create a donut (ring) shape with a GeometryGroup?

- [ ] A. Use two overlapping circles with `FillRule="NonZero"`
- [ ] B. Use two overlapping circles with `FillRule="EvenOdd"`
- [ ] C. Use `CombinedGeometry` with `Union` mode
- [ ] D. Use two separate `EllipseGeometry` objects

**Answer:** B. `EvenOdd` alternates fill for overlapping regions, creating a hole in the center where the circles overlap.

---

## Q12. Why might `RenderTargetBitmap.Render(control)` produce an empty image?

- [ ] A. The control is not attached to a visual tree
- [ ] B. The pixel size is too small
- [ ] C. The DPI is set to zero
- [ ] D. The control has no `Render` override

**Answer:** A. `RenderTargetBitmap.Render` requires the target control to be attached to a visible window. Use the headless platform for off-screen rendering.
