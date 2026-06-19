---
tier: advanced
topic: layout
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 080 — Layout System Deep Dive

**What you'll learn:** The two-pass layout system — measure and arrange — and how to create custom panels, work with `EffectiveViewportChanged`, and debug layout passes.

**Prerequisites:** [023 — Custom Layout Panels](023-custom-layout-panels.md)

---

## 1. The two-pass layout system

Avalonia layout completes two passes for each element: **measure** and **arrange**.

```
Measure(constraint) → MeasureOverride(constraint) → DesiredSize
Arrange(finalRect)  → ArrangeOverride(finalSize) → final layout
```

### Pass 1 — Measure

The parent calls `child.Measure(availableSize)`. Your override must call `child.Measure()` on every child and return the desired size:

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    foreach (var child in Children)
        child.Measure(availableSize);

    return availableSize; // or a calculated size
}
```

### Pass 2 — Arrange

The parent calls `child.Arrange(finalRect)`. Your override positions each child and returns `finalSize`:

```csharp
protected override Size ArrangeOverride(Size finalSize)
{
    foreach (var child in Children)
        child.Arrange(new Rect(finalSize));

    return finalSize;
}
```

---

## 2. Layoutable properties

| Property | Description |
|----------|-------------|
| `Width` / `Height` | Explicit size |
| `MinWidth` / `MaxWidth` | Size constraints |
| `Margin` | Space around element |
| `HorizontalAlignment` | Stretch, Left, Center, Right |
| `VerticalAlignment` | Stretch, Top, Center, Bottom |
| `DesiredSize` | Result of measure pass |
| `UseLayoutRounding` | Snap to pixel boundaries |
| `IsMeasureValid` / `IsArrangeValid` | Layout validity flags |

```xml
<Button Width="100" Height="32"
        Margin="8,4"
        HorizontalAlignment="Center"
        UseLayoutRounding="True" />
```

---

## 3. AffectsMeasure / AffectsArrange

When creating a custom panel, register property affinities so changes trigger re-layout:

```csharp
public static readonly StyledProperty<double> SpacingProperty =
    AvaloniaProperty.Register<MyPanel, double>(nameof(Spacing), 0);

static MyPanel()
{
    SpacingProperty.OverrideMetadata<MyPanel>(
        new StyledPropertyMetadata<double>(
            defaultValues: new(0),
            coerce: CoerceSpacing));
    AffectsMeasure<MyPanel>(SpacingProperty);
}
```

- `AffectsMeasure` — property change invalidates measure + re-layouts entire subtree
- `AffectsArrange` — property change invalidates arrange only (lighter weight)

---

## 4. EffectiveViewportChanged (v12)

Track which portion of a control is actually visible inside a scrollable parent:

```csharp
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    EffectiveViewportChanged += OnEffectiveViewportChanged;
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    EffectiveViewportChanged -= OnEffectiveViewportChanged;
}

private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
{
    bool isVisible = e.EffectiveViewport.Intersects(new Rect(Bounds.Size));
    // Toggle visibility, load/unload data, etc.
}
```

---

## 5. LayoutManager

The `ILayoutManager` orchestrates passes. Access it via `TopLevel`:

```csharp
var layoutManager = TopLevel.GetTopLevel(control)?.LayoutManager;
layoutManager?.ExecuteLayoutPass(); // force immediate pass
```

Events:

```csharp
layoutManager.LayoutUpdated += (s, e) =>
{
    // React after each layout pass completes
};
```

---

## 6. Layout rounding and pixel snapping

`UseLayoutRounding` (default `true`) snaps positions to device pixels, preventing sub-pixel rendering artifacts.

```csharp
// Disable for sub-pixel positioning (e.g., smooth animations)
UseLayoutRounding = false;
```

Pixel snapping works with `LayoutHelper`:

```csharp
double snapped = LayoutHelper.RoundLayoutUp(value, dpiScale);
```

---

## 7. Invalidation chain

```csharp
InvalidateMeasure();   // queue measure + arrange pass
InvalidateArrange();   // queue arrange pass only (lighter)
```

Calling `InvalidateMeasure` cascades — it marks the element's measure as invalid, which triggers the parent to re-measure this element, and so on up the tree.

---

## 8. Layout zones and overlay layer

Elements with high `ZIndex` or `Popup`-related parents render in the overlay layer, which is arranged separately from the main layout pass.

```xml
<Panel>
  <Button ZIndex="10" Content="On top" />
  <Button ZIndex="1" Content="Below" />
</Panel>
```

---

## Key Takeaways

- Always call `Measure` on all children in `MeasureOverride` and `Arrange` on all children in `ArrangeOverride`
- Use `AffectsMeasure`/`AffectsArrange` for custom DP layout invalidation
- `EffectiveViewportChanged` is the v12 way to track visibility in scrollable regions
- `LayoutManager.ExecuteLayoutPass()` forces an immediate pass (rarely needed)
- `UseLayoutRounding` prevents sub-pixel artifacts

---

## See Also

- [080V — Layout System Deep Dive (verbose)](080-layout-system-deep-dive-verbose.md)
- [080E — Layout System Deep Dive (examples)](080-layout-system-deep-dive-examples.md)
- [Avalonia Docs: Custom Panel](https://docs.avaloniaui.net/docs/custom-controls/custom-panel)
- [Avalonia Docs: Layout Overview](https://docs.avaloniaui.net/docs/basics/user-interface/building-layouts/)
- [Avalonia API: Layoutable](https://docs.avaloniaui.net/api/avalonia/layout/layoutable)
- [Avalonia API: ILayoutManager](https://docs.avaloniaui.net/api/avalonia/layout/ilayoutmanager)
