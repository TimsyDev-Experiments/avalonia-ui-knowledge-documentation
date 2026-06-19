---
tier: intermediate
topic: layout
estimated: 5 min
researched: 2026-06-18
avalonia-version: 12.0.4
quiz-of: 058-scrollviewer-scrollbar.md
quiz-type: comprehension
---

# 058Q — ScrollViewer & ScrollBar Quiz

**Scenario:** You are building a document viewer with scrollable content. Answer the following questions.

---

## Q1. Why does putting a `ScrollViewer` inside a `StackPanel` break scrolling?

**A1.** `StackPanel` offers infinite space in its orientation. The `ScrollViewer` never detects overflow because its parent gives it unlimited room. Use a `Grid` or a fixed-height container instead.

---

## Q2. What is the difference between `Hidden` and `Disabled` for `ScrollBarVisibility`?

**A2.** `Hidden` hides the scrollbar but still allows scrolling via touch, mouse wheel, or keyboard. `Disabled` prevents scrolling entirely in that direction.

---

## Q3. How do you scroll a `ScrollViewer` to the bottom programmatically?

**A3.**

```csharp
sv.Offset = new Vector(
    sv.Offset.X,
    sv.Extent.Height - sv.Viewport.Height);
```

---

## Q4. When should you use `BringIntoView()` vs setting `Offset` directly?

**A4.** `BringIntoView()` is for when you have a reference to the child element and want to scroll just enough to make it visible. `Offset` is for when you know the exact pixel position to scroll to.

---

## Q5. What property controls whether a scroll inside a nested `ListBox` continues to scroll the outer `ScrollViewer`?

**A5.** `ScrollViewer.IsScrollChainingEnabled` attached property (default `true`). Set to `False` on the inner control to prevent chaining.

---

## Q6. A user drags the scrollbar thumb and the content jumps to the final position instead of updating smoothly. What setting causes this?

**A6.** `IsDeferredScrollingEnabled="True"`. Content does not update until the user releases the thumb. Useful for performance with heavy content.

---

## Q7. What does `ScrollBarMaximum` equal?

**A7.** `Extent - Viewport`. It is the maximum scroll distance in pixels.

---

## Q8. How do you detect that the user has scrolled to the bottom of a `ScrollViewer`?

**A8.** Subscribe to `ScrollChanged` and check:

```csharp
bool isAtBottom = sv.Offset.Y >= sv.Extent.Height - sv.Viewport.Height - 1;
```

---

## Q9. What does `ViewportSize` on a standalone `ScrollBar` control?

**A9.** It determines the size of the thumb relative to the track, representing the proportion of visible content to total content. A larger `ViewportSize` makes the thumb larger.

---

## Q10. Your chat app prepends older messages when the user scrolls up. After prepending, the view jumps to the top. How do you fix this?

**A10.** In the `ScrollChanged` handler, detect when `ExtentDelta.Y > 0` and the user is not at the bottom. Compensate by adding `e.ExtentDelta.Y` to the current offset:

```csharp
if (e.ExtentDelta.Y > 0 && !isAtBottom)
    sv.Offset = new Vector(sv.Offset.X, sv.Offset.Y + e.ExtentDelta.Y);
```

---

## Q11. True or False: `ScrollViewer` virtualizes its content automatically.

**A11.** False. `ScrollViewer` does not virtualize. It scrolls whatever content is placed inside it. Virtualization is handled by controls like `ListBox`, `DataGrid`, or `ItemsRepeater` with virtualizing layouts.

---

## Q12. You want a horizontal image strip that auto-snaps to each image when the user stops scrolling. How do you enable this?

**A12.** Use a horizontal `ScrollViewer` with snap point support. By default, the `ScrollViewer` snaps to element boundaries when scrolling finishes.

---

## See Also

- [058 — ScrollViewer & ScrollBar (core tutorial)](058-scrollviewer-scrollbar.md)
- [058V — ScrollViewer & ScrollBar (verbose companion)](058-scrollviewer-scrollbar-verbose.md)
- [058E — ScrollViewer & ScrollBar (examples)](058-scrollviewer-scrollbar-examples.md)
