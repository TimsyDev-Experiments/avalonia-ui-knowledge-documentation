---
tier: advanced
topic: animation
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 081Q — Animation System Deep Dive (quiz)

## Q1. Which FillMode prevents a control from snapping back to its original value after an animation ends?

- [ ] A. `None`
- [ ] B. `Backward`
- [ ] C. `Forward`
- [ ] D. `Both`

**Answer:** C. `FillMode.Forward` persists the last interpolated value.

---

## Q2. How do you make an animation repeat indefinitely?

- [ ] A. `IterationCount="0"`
- [ ] B. `IterationCount="Infinite"`
- [ ] C. `PlaybackDirection="Alternate"`
- [ ] D. `FillMode="Both"`

**Answer:** B. `IterationCount="Infinite"` causes unbounded looping.

---

## Q3. What happens when `animation.RunAsync(target, ct)` is called with an infinite animation?

- [ ] A. It throws an exception
- [ ] B. It returns a completed task immediately
- [ ] C. The returned task never completes unless cancelled
- [ ] D. It runs once and ignores IterationCount

**Answer:** C. The task never completes because the animation loops forever. Pass a `CancellationToken` with a timeout to stop it.

---

## Q4. Why won't a WPF-style `RotateTransform` work with `TransformOperationsTransition`?

- [ ] A. `RotateTransform` is not supported in Avalonia
- [ ] B. Transitions only work with CSS shorthand transform strings
- [ ] C. `RotateTransform` is read-only
- [ ] D. It works fine

**Answer:** B. WPF-style transform objects cannot transition. Use CSS shorthand like `rotate(45deg)` with `TransformOperationsTransition`.

---

## Q5. Which animation layer runs on the render thread?

- [ ] A. `Style.Animations`
- [ ] B. `Transitions`
- [ ] C. `Animation.RunAsync()`
- [ ] D. Composition animations (`ElementComposition`)

**Answer:** D. Composition animations execute on the render thread, avoiding UI thread impact.

---

## Q6. How do you create a custom CSS-ease cubic bezier in XAML?

- [ ] A. `<Easing CubicBezier="0.25,0.1,0.25,1.0" />`
- [ ] B. `<SplineEasing X1="0.25" Y1="0.1" X2="0.25" Y2="1.0" />`
- [ ] C. `<BezierEasing P1="0.25,0.1" P2="0.25,1.0" />`
- [ ] D. `<CustomEasing Curve="ease" />`

**Answer:** B. `SplineEasing` takes four control point values. The `KeySpline` shorthand on `KeyFrame` also works.

---

## Q7. A property type cannot be animated. Why?

- [ ] A. The property is read-only
- [ ] B. The type has no registered interpolator in `PropertyMetadata`
- [ ] C. Animations only work on `double` properties
- [ ] D. The control must implement `IAnimatable`

**Answer:** B. Each `AvaloniaProperty` must have an `Interpolator` registered in its metadata for animation to work.

---

## Q8. What does `PlaybackBehavior="Always"` do?

- [ ] A. Forces the animation to ignore the easing function
- [ ] B. Keeps the animation running even when the control is hidden
- [ ] C. Makes the animation play in reverse
- [ ] D. Prevents the animation from being cancelled

**Answer:** B. In Avalonia 12, animations on hidden controls stop by default. `PlaybackBehavior="Always"` overrides that.

---

## Q9. How do you implement a custom page transition?

- [ ] A. Subclass `PageTransitionBase`
- [ ] B. Implement `IPageTransition`
- [ ] C. Override `OnPageChanged`
- [ ] D. Use `CustomTransition` markup extension

**Answer:** B. `IPageTransition` has a single `Start(Visual? from, Visual? to, bool forward, CancellationToken ct)` method.

---

## Q10. What is the key difference between `Style.Animations` and `Transitions`?

- [ ] A. Transitions are faster
- [ ] B. Style.Animations use keyframes; Transitions react to property value changes
- [ ] C. There is no difference
- [ ] D. Transitions only work in code-behind

**Answer:** B. `Style.Animations` are keyframe-based sequences driven by selectors. `Transitions` automatically animate any change to a target property.

---

## Q11. Which of these properties can composition animations animate?

- [ ] A. `Opacity`
- [ ] B. `Offset` (Vector3D)
- [ ] C. `Size`
- [ ] D. All of the above

**Answer:** D. Composition animations support `Opacity` (scalar), `Offset` (Vector3), and `Size` (Vector2) on `CompositionVisual`.

---

## Q12. What happens if two concurrent `Animation.RunAsync` calls target the same property?

- [ ] A. The first animation wins and the second is ignored
- [ ] B. Both run and the last value set wins per frame
- [ ] C. An exception is thrown
- [ ] D. The values are averaged

**Answer:** B. Multiple animations can target the same property concurrently. The last animation to set the property value wins for that frame.
