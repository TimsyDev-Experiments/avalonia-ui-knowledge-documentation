---
tier: advanced
topic: layout
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 023-custom-layout-panels.md
---

# 023V — Custom Layout Panels: An In-Depth Companion

**Why this exists.** The original tutorial builds a `WrapPanel` and a `RadialPanel`. This companion explains the measure/arrange contract in detail, why `double.IsNaN` is the canonical sentinel for "auto", the performance implications of layout passes, and what happens when you break the layout invariants.

**Read this alongside:** [023 — Custom Layout Panels](023-custom-layout-panels.md)

---

## 1. The `Panel` base class

```csharp
public class WrapPanel : Panel
```

### What `Panel` provides

`Panel` is the base class for all layout containers. It extends `Control` and adds:
- `Children` property — an `ILayoutable` collection (wraps `Controls` list).
- `Background` property (inherited) — the panel can render a background behind its children.
- The pattern that `MeasureOverride` and `ArrangeOverride` iterate `Children` and call the corresponding methods on each child.

### The contract

Every panel must implement exactly two methods:

```csharp
protected override Size MeasureOverride(Size availableSize);
protected override Size ArrangeOverride(Size finalSize);
```

The framework calls these in sequence:
1. **Measure** — the panel tells each child, "You can have at most this much space." Each child reports its desired size.
2. **Arrange** — the panel tells each child, "Your final position and size are this rectangle."

These must be called in order. Calling `Arrange` without a preceding `Measure` for the same child raises an exception.

---

## 2. `MeasureOverride` in detail

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var availableWidth = availableSize.Width;
    var childAvailable = new Size(
        double.IsNaN(ItemWidth) ? availableWidth : ItemWidth,
        double.IsNaN(ItemHeight) ? double.PositiveInfinity : ItemHeight);
    ...
}
```

### What `availableSize` means

`availableSize` is the space the **parent** has allocated for this panel. For a `WrapPanel` inside a `StackPanel` with `HorizontalAlignment="Stretch"`, `availableSize.Width` is the StackPanel's width, and `availableSize.Height` is `Infinity` (meaning "you can be as tall as you need").

### The child constraint

```csharp
child.Measure(childAvailable);
```

The `childAvailable` parameter tells the child the upper bound for its size. The child can return a smaller desired size but must not exceed this bound. In the WrapPanel:
- If `ItemWidth` is set (not `NaN`), each child is constrained to exactly that width — the child's own `Measure` receives `ItemWidth` as both the available width and the bound.
- If `ItemWidth` is `NaN`, each child can be as wide as the panel itself (`availableWidth`), and the child's `DesiredSize.Width` comes from the child's own measure.

### Why `double.PositiveInfinity` for height

When `ItemHeight` is `NaN`, the WrapPanel does not constrain children vertically — they can be as tall as they need. The panel then uses the tallest child in a row to determine row height. If you constrained height to `availableSize.Height`, children that are taller than one row would wrap incorrectly.

### Accumulating desired size

```csharp
totalHeight += rowHeight;
rowWidth = 0;
rowHeight = 0;
```

The panel returns `new Size(totalWidth, totalHeight)` — the space it truly needs. The parent may or may not honor this (see Arrange pass). If the parent gives less space than desired, the panel clips content (unless `ClipToBounds = false`).

---

## 3. `ArrangeOverride` in detail

```csharp
protected override Size ArrangeOverride(Size finalSize)
```

### What `finalSize` means

`finalSize` is what the **parent** actually allocated, not necessarily the size returned by `MeasureOverride`. Common reasons for divergence:
- The parent has a fixed size and constrains children.
- The parent is a `Grid` with star-sized rows/columns.
- The parent's own arrangement logic overrides its children's desires.

### The child's arrange rect

```csharp
child.Arrange(new Rect(x, y, childWidth, childHeight));
```

The `Rect` specifies the child's position relative to the panel's origin (top-left) and the child's final size. If the child's `DesiredSize` is smaller than `childWidth`, the child can position its own content using `HorizontalAlignment`/`VerticalAlignment`.

### Return value

`ArrangeOverride` returns `finalSize` unchanged in the original. This tells the parent, "I accept the size you gave me." If you wanted to enforce a minimum size, you would return `new Size(Math.Max(finalSize.Width, MinWidth), ...)`.

---

## 4. `double.IsNaN` as the "auto" sentinel

```csharp
public static readonly StyledProperty<double> ItemWidthProperty =
    AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemWidth), double.NaN);
```

### Why `NaN` instead of `-1` or `0`

`double.NaN` has a unique property: `double.IsNaN(double.NaN)` returns `true`, and `NaN < anyNumber` is always `false`. You can safely compare:

```csharp
if (childWidth > availableWidth) // never true if childWidth is NaN
```

Using `0` as the sentinel would break here: a child with zero desired width would skip wrapping logic, and `Math.Max` with `0` is always `0`.

### The `double.IsNaN` check must be explicit

```csharp
var childWidth = double.IsNaN(ItemWidth) ? child.DesiredSize.Width : ItemWidth;
```

You cannot write `ItemWidth ?? child.DesiredSize.Width` because `double` is not nullable (unless you use `double?`). The `double.IsNaN` check is the standard pattern throughout Avalonia's own layout code (see `WrapPanel` in the framework source).

---

## 5. `AffectsMeasure` registration

The original omits property registration for layout invalidation. Every public property that influences layout **must** be listed:

```csharp
static WrapPanel()
{
    AffectsMeasure<WrapPanel>(ItemWidthProperty, ItemHeightProperty);
}
```

Without this, changing `ItemWidth` at runtime does not trigger a new measure pass. The panel continues using old child sizes, and content overlaps or leaves gaps.

`AffectsMeasure` calls `InvalidateMeasure()` on the panel, which schedules a new layout pass. The parent panel is also re-measured if its own layout depends on this panel's desired size.

---

## 6. Radial panel: why the measure differs

```csharp
protected override Size MeasureOverride(Size availableSize)
{
    var childSize = new Size(40, 40);
    foreach (Control child in Children)
        child.Measure(childSize);
    return new Size(Radius * 2 + 40, Radius * 2 + 40);
}
```

### Fixed child size

Unlike the WrapPanel, the RadialPanel gives every child a fixed measure constraint of `40x40`. This is a simplification: each child's `DesiredSize` is at most `40x40`. The panel then computes its own size as `(Radius * 2 + 40)` — the bounding box of the circle plus room for the children.

### Why this is fragile

- If a child's natural size exceeds 40 in either dimension, it is constrained and its content may be clipped.
- There is no `ItemSize` property — consumers cannot adjust the child area.
- The panel's size depends on `Radius` but does not register `RadiusProperty` with `AffectsMeasure`.

In a production panel, `Radius` should be listed in `AffectsMeasure`.

---

## 7. Radial panel arrange: coordinate math

```csharp
var angleStep = 2 * Math.PI / count;
var angle = i * angleStep - Math.PI / 2; // start at top
var x = center.X + Radius * Math.Cos(angle) - 20;
var y = center.Y + Radius * Math.Sin(angle) - 20;
```

### `- Math.PI / 2` — starting at the top

Without this offset, the first child would be at angle 0 (3 o'clock position). Subtracting 90 degrees rotates the start position to 12 o'clock. This is conventional for radial menus and pie layouts.

### `-20` — centering the child

The child's arrange rect is `40x40`. If placed at `(center.X + Radius * cos, center.Y + Radius * sin)`, the child's top-left corner is on the circle, not its center. Subtracting 20 (half of 40) shifts the child so its center is on the circle.

### Infinite/NaN protection

If `count` is `0`, `angleStep` becomes `Infinity` (division by zero). The original returns early:

```csharp
if (count == 0) return finalSize;
```

Without this guard, `Math.Sin`/`Math.Cos` with `Infinity` returns `NaN`, and `Rect` construction with `NaN` coordinates throws or produces undefined layout.

---

## 8. The layout system rules table

| Rule | Why |
|---|---|
| Always call `child.Measure()` before `child.Arrange()` | The framework validates this internally; violation throws `InvalidOperationException` |
| Do not call `InvalidateMeasure()` inside `MeasureOverride` | Infinite loop — the panel triggers re-measure, which triggers `MeasureOverride`, which triggers... |
| Return a valid `Size` (no `NaN`, no `Infinity`) from both overrides | The parent uses these values for its own layout; NaN propagates upward and causes silent failures |
| Use `PositiveInfinity` to mean "unbounded" in child measure | Children respect this: a `TextBlock` with infinite width will not wrap |
| Do not change `Children` during layout | Mutating the children collection while iterating it throws or produces undefined behavior |

---

## 9. When to override layout vs. use built-in panels

| Built-in panel | Use case |
|---|---|
| `StackPanel` | Linear arrangement (vertical or horizontal) |
| `Grid` | Row/column table layout |
| `DockPanel` | Dock-to-edge (toolbars, status bars) |
| `WrapPanel` (built-in Avalonia) | Flowing wrap layout — use this instead of writing your own |
| `RelativePanel` | Position relative to sibling elements |
| Custom panel | Circular, spiral, Masonry, and other non-rectilinear layouts |

Avalonia ships a built-in `WrapPanel` in `Avalonia.Controls`. Write a custom one only for learning or when you need specialized wrapping behavior (variable item sizes, different fill modes).

---

## Cross-links

- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — the Control base class patterns that Panel extends
- [030 — Layout Measure/Arrange & Custom Controls](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/30-layout-measure-arrange-and-custom-layout-controls.md) (plugin ref)
- [023E — Custom Layout Panels (examples)](023-custom-layout-panels-examples.md)
- [Avalonia Docs: Custom Layout](https://docs.avaloniaui.net/docs/layout/custom-layout)
