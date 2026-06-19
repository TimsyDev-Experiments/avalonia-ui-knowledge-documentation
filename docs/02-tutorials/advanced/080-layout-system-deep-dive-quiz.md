---
tier: advanced
topic: layout
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 080Q — Layout System Deep Dive (quiz)

## Q1. What must you call on every child in MeasureOverride?

- [ ] A. `child.Arrange()`
- [ ] B. `child.Measure()`
- [ ] C. `child.Invalidate()`
- [ ] D. `child.UpdateLayout()`

**Answer:** B. Each child must be measured to set its `DesiredSize`.

---

## Q2. What happens if a child is not measured in MeasureOverride?

- [ ] A. The child renders at size zero
- [ ] B. The child may not render correctly
- [ ] C. The child throws an exception
- [ ] D. The child auto-sizes to parent

**Answer:** B. Children that are not measured may not render correctly.

---

## Q3. Which attribute tells the layout system to re-measure when a dependency property changes?

- [ ] A. `AffectsArrange`
- [ ] B. `AffectsRender`
- [ ] C. `AffectsMeasure`
- [ ] D. `InvalidatesLayout`

**Answer:** C. `AffectsMeasure` triggers a full measure + arrange pass.

---

## Q4. What does `EffectiveViewportChanged` indicate?

- [ ] A. The window was resized
- [ ] B. The visible portion of a control within a scrollable parent changed
- [ ] C. The control's opacity changed
- [ ] D. A layout pass completed

**Answer:** B. It tracks which part of the element is actually visible in the viewport.

---

## Q5. What does `UseLayoutRounding` prevent?

- [ ] A. Sub-pixel rendering artifacts
- [ ] B. Layout cycles
- [ ] C. Memory leaks
- [ ] D. Redundant measure passes

**Answer:** A. Snaps positions to device pixel boundaries.

---

## Q6. Which method forces an immediate layout pass?

- [ ] A. `LayoutManager.ExecuteLayoutPass()`
- [ ] B. `LayoutManager.ForceLayout()`
- [ ] C. `Panel.RefreshLayout()`
- [ ] D. `Dispatcher.UIThread.RunJobs()`

**Answer:** A. `ExecuteLayoutPass()` runs a pass immediately.

---

## Q7. True or False: `InvalidateArrange` is lighter weight than `InvalidateMeasure`.

- [ ] A. True
- [ ] B. False

**Answer:** A. `InvalidateArrange` queues only the arrange pass, skipping re-measure.

---

## Q8. Which property determines if an element is excluded from layout entirely?

- [ ] A. `Opacity`
- [ ] B. `IsVisible`
- [ ] C. `IsEnabled`
- [ ] D. `IsHitTestVisible`

**Answer:** B. When `IsVisible` is `false`, `DesiredSize` is set to zero and the subtree is skipped.

---

## Q9. What is the difference between `LayoutUpdated` and `SizeChanged`?

- [ ] A. `LayoutUpdated` fires every layout pass; `SizeChanged` only when size changes
- [ ] B. They are identical
- [ ] C. `SizeChanged` fires every pass; `LayoutUpdated` only on size change
- [ ] D. `LayoutUpdated` is for panels only

**Answer:** A. `LayoutUpdated` fires on every pass; `SizeChanged` fires only on actual size change.

---

## Q10. Which type of transform affects the parent's layout?

- [ ] A. `RenderTransform`
- [ ] B. `LayoutTransform`
- [ ] C. `OpacityTransform`
- [ ] D. `ClipTransform`

**Answer:** B. `LayoutTransform` causes the parent to re-measure accounting for the transformed size.

---

## Scoring

| Score | Interpretation |
|-------|---------------|
| 10/10 | Expert |
| 8-9 | Strong understanding |
| 6-7 | Getting there |
| <6 | Review the core tutorial |

---

## See Also

- [080 — Layout System Deep Dive (core)](080-layout-system-deep-dive.md)
- [080V — Layout System Deep Dive (verbose)](080-layout-system-deep-dive-verbose.md)
- [080E — Layout System Deep Dive (examples)](080-layout-system-deep-dive-examples.md)
