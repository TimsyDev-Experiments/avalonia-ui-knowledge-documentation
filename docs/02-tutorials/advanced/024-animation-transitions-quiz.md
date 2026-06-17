---
tier: advanced
topic: animation
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 024-animation-transitions.md
---

# Quiz — Animation & Transitions

```quiz
Q: What is the conceptual difference between a Transition and a Style Animation?
A. Transitions are defined in code; Style Animations are defined in XAML. || Both can be declared in XAML — Transitions via element.Transitions and Style Animations via Style.Animations.
B. Transitions automatically animate property value changes; Style Animations are triggered by selector matches such as pseudo-classes. (correct) || Transitions respond to any property change; Style Animations activate when the style selector (e.g., /pressed/) applies.
C. Transitions are GPU-accelerated; Style Animations run on the UI thread. || Performance depends on the property being animated (RenderTransform is always GPU), not on the mechanism.
D. Transitions support only double properties; Style Animations support all types. || Transitions include many types: BrushTransition, ColorTransition, CornerRadiusTransition, etc.
```

```quiz
Q: Which Transition type would you use to animate a Border's CornerRadius from 4 to 16?
A. DoubleTransition || CornerRadius is a struct with TopLeft, TopRight, BottomLeft, BottomRight, not a single double.
B. ThicknessTransition || ThicknessTransition expects Thickness (left, top, right, bottom), not CornerRadius.
C. CornerRadiusTransition (correct) || CornerRadiusTransition handles the CornerRadius type directly with smooth interpolation of each corner.
D. SizeTransition || SizeTransition handles Width/Height; CornerRadius is a different type.
```

```quiz
Q: After await animation.RunAsync(target, null) completes in the FadeOut example, why does the next line set target.IsVisible = false?
A. Because RunAsync throws if the target is still visible. || RunAsync completes normally; setting IsVisible is a separate step.
B. Because FillMode.Forward holds the final opacity at 0 but does not hide the element from hit testing or keyboard navigation. (correct) || The element remains invisible-only; IsVisible = false removes it from layout and interaction entirely.
C. Because RunAsync resets all properties to their pre-animation values. || FillMode.Forward preserves the end state; properties are not reset automatically.
D. Because the animation failed and the element is in an invalid state. || The animation succeeds; hiding the element is intentional cleanup after the fade completes.
```

```quiz
Q: Why is RenderTransform preferred over LayoutTransform for animated scale or rotation?
A. RenderTransform operates on the compositor thread and does not trigger re-layout of the element's subtree. (correct) || RenderTransform only affects the visual output; LayoutTransform would re-arrange siblings and re-measure children on every frame.
B. RenderTransform supports matrix operations; LayoutTransform only supports basic transforms. || Both support the same set of transform operations (scale, rotate, translate, matrix).
C. LayoutTransform is not available in Avalonia 12. || LayoutTransform still exists and is valid when the transform must affect layout (e.g., rotated text in a constrained cell).
D. RenderTransform can only animate from code-behind. || The tutorial shows RenderTransform animations declared in XAML styles with Style.Animations.
```

```quiz
Q: What is the default behavior in Avalonia 12 for animations on a control that becomes hidden?
A. Animations continue running on a low-priority background thread. || They do not continue — they are stopped to save CPU.
B. Animations are stopped entirely, reducing unnecessary compositor work. (correct) || The tutorial states: "Avalonia 12 transitions stop ticking when the control is hidden (improved CPU usage)."
C. Animations are paused and automatically resume when the control becomes visible. || The default is stopped, not paused; use PlaybackBehavior="Always" to override.
D. Animations switch to FixedTimeStep mode to conserve resources. || There is no time-step fallback; animations simply stop ticking.
```
