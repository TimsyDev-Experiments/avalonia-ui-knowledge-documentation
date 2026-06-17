---
tier: advanced
topic: layout
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 023-custom-layout-panels.md
---

# Quiz — Custom Layout Panels

```quiz
Q: What is the required ordering of layout calls for each child in a custom panel?
A. child.Arrange() then child.Measure() || Arrange cannot run before Measure because the child has not yet reported its desired size.
B. child.Measure() then child.Arrange() (correct) || Measure first determines the child's desired size; Arrange then positions the child within the allocated space using that measured result.
C. Only child.Measure() is needed || Arrange is required to set the child's final position and size within the panel.
D. Only child.Arrange() is needed || Arrange depends on a prior Measure call; skipping Measure leads to undefined layout behavior.
```

```quiz
Q: In the WrapPanel.MeasureOverride, what condition causes a child to wrap to the next row?
A. rowWidth + childWidth exceeds availableSize.Width (correct) || The wrap check compares the cumulative row width plus the next child's width against the available width.
B. The child's DesiredSize.Height exceeds the accumulated row height. || Height does not trigger wrapping; it only increases rowHeight via Math.Max.
C. rowWidth exceeds the fixed ItemWidth property. || Wrapping is based on availableSize.Width, not ItemWidth — ItemWidth is used for uniform sizing only.
D. The number of children reaches MaxItemsPerRow. || There is no count-based wrap limit; wrapping is purely driven by available width.
```

```quiz
Q: How does the RadialPanel.ArrangeOverride position children on a circle?
A. By distributing them linearly and then rotating the panel. || There is no post-rotation; positions are computed directly with trigonometry.
B. By using a random offset from the center for each child. || The placement is deterministic and evenly spaced, not random.
C. By computing evenly spaced angles with angleStep = 2π / count and using Cos/Sin for x/y coordinates. (correct) || The tutorial computes angle = i * angleStep - π/2 and arranges each child at (center.X + Radius * Cos(angle) - 20, center.Y + Radius * Sin(angle) - 20).
D. By converting a Grid layout into polar coordinates. || There is no Grid intermediary; positions are calculated directly from angles.
```

```quiz
Q: Why should layout-affecting properties like ItemWidth be registered with AffectsMeasure<WrapPanel>()?
A. To enable two-way binding on the property. || Two-way binding works on any StyledProperty; AffectsMeasure is unrelated to binding direction.
B. To automatically call InvalidateMeasure when the property changes, keeping the layout in sync. (correct) || AffectsMeasure wires the property's changed event to InvalidateMeasure so MeasureOverride runs with the updated value.
C. To set a default value of double.NaN without a constructor call. || Default values come from Register's defaultValue parameter, not from AffectsMeasure.
D. To expose the property in the DevTools property grid. || DevTools reflects any StyledProperty; AffectsMeasure is not required for visibility.
```

```quiz
Q: What does double.NaN mean for the WrapPanel's ItemWidth property?
A. The child's width is set to zero and it will not be rendered. || NaN is not zero; it is a sentinel meaning "not specified."
B. The child should use its own DesiredSize.Width rather than a fixed uniform width. (correct) || When NaN, the panel falls back to child.DesiredSize.Width, allowing each child to retain its natural size.
C. The value is invalid and an exception is thrown. || NaN is the intended default and is explicitly checked with double.IsNaN.
D. The child stretches to fill the remaining panel width. || Stretch behavior would require additional logic; NaN simply means "use native size."
```
