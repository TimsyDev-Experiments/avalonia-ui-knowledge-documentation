---
tier: advanced
topic: layout
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 080V — Layout System Deep Dive (verbose companion)

**What this covers:** MeasureCore internals, ArrangeCore margin/alignment logic, custom panel edge cases, layout pass debugging, and the bounding box model.

**Prerequisites:** 080 — Layout System Deep Dive core

---

## 1. MeasureCore vs MeasureOverride

The public `Measure()` method calls `MeasureCore()`, which applies native properties (`IsVisible`, `Clip`) and then calls `MeasureOverride()`. If `IsVisible` is `false`, the control is excluded from layout entirely — `DesiredSize` is set to zero and the subtree is skipped.

```csharp
// Pseudo-flow
Measure(Size available)
  → MeasureCore(available)
    → if (!IsVisible) { DesiredSize = default; return; }
    → apply Margin, Width, Height, Min/Max constraints
    → MeasureOverride(constrainedSize)
    → store DesiredSize
```

### Margin, alignment, and the constraint

The constraint passed to `MeasureOverride` is the available size **minus** margins. During `ArrangeCore`, additional offset is applied for alignment:

```csharp
// ArrangeCore applies:
// 1. Margin around the child
// 2. HorizontalAlignment → offset within layout slot
// 3. VerticalAlignment → offset within layout slot
// Child does NOT need to fill the allocated slot
```

---

## 2. Custom panel edge cases

### Zero-size children

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    double maxW = 0, maxH = 0;
    foreach (var child in Children)
    {
        child.Measure(Size.Infinity); // ask each child its natural size
        maxW = Math.Max(maxW, child.DesiredSize.Width);
        maxH = Math.Max(maxH, child.DesiredSize.Height);
    }
    return new Size(maxW, maxH);
}
```

### Infinite constraint handling

Pass `Size.Infinity` to let children report their natural size. Be prepared for children that return infinity back (e.g., `TextBlock` with no wrapping).

### Measuring subsets (visible children only)

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    foreach (var child in Children)
    {
        if (child.IsVisible)
            child.Measure(availableSize);
        else
            child.Measure(Size.Empty); // still required
    }
    return availableSize;
}
```

---

## 3. Layout pass debugging

### DevTools layout tab

Open DevTools (F12) → Layout tab. Shows:
- Computed bounds and margin box
- Layout validity flags
- Measure/arrange timestamps

### Manual logging

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    Debug.WriteLine($"ArrangeOverride: {finalSize}, Children: {Children.Count}");
    return base.ArrangeOverride(finalSize);
}
```

### LayoutInformation

```csharp
var constraints = LayoutInformation.GetAvailableSize(control);
LayoutInformation.GetLayoutSlot(control); // final allocated rect
```

---

## 4. Bounding box model

Each element has a box model in this order (inside-out):

```
Margin → Border → Padding → Content
```

| Property | Affects |
|----------|---------|
| `Margin` | Measure constraint |
| `BorderThickness` | Arrange size |
| `Padding` | Content area |
| `Width`/`Height` | Explicit override of desired size |

---

## 5. LayoutUpdated event

Fires on each layout pass completion for a given element:

```csharp
LayoutUpdated += (s, e) =>
{
    var bounds = Bounds; // updated after layout
};
```

Compare with `SizeChanged`:

| Event | Fires |
|-------|-------|
| `LayoutUpdated` | Every layout pass (frequent) |
| `SizeChanged` | Only when size actually changes |

---

## 6. RenderTransform vs LayoutTransform

| Transform | Effect |
|-----------|--------|
| `RenderTransform` | Visual only — no layout impact |
| `LayoutTransform` | Affects layout — parent re-measures |

```xml
<Border LayoutTransform="{Binding RotateScale}">
  <!-- Parent accounts for the transformed size -->
</Border>
```

---

## See Also

- [080 — Layout System Deep Dive (core)](080-layout-system-deep-dive.md)
- [080E — Layout System Deep Dive (examples)](080-layout-system-deep-dive-examples.md)
- [Avalonia API: LayoutInformation](https://docs.avaloniaui.net/api/avalonia/layout/layoutinformation)
- [Avalonia API: LayoutHelper](https://docs.avaloniaui.net/api/avalonia/layout/layouthelper)
