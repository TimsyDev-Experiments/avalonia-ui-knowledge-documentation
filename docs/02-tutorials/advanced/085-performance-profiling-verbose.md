---
tier: advanced
topic: development
estimated: 35 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 085 — Performance & Profiling — Verbose

**Prerequisites:** [085-core](085-performance-profiling.md)

---

## 1. Virtualization in detail

### How virtualization works

The `VirtualizingStackPanel` creates controls only for items visible in the viewport. As the user scrolls:

1. Items moving off-screen have their controls recycled.
2. The recycled controls are reused for items entering the viewport.
3. The panel estimates total scroll extent from item count × estimated height.

This means the UI thread creates and measures a fixed number of controls regardless of total collection size.

### When virtualization breaks

Virtualization is disabled when the panel has infinite available space:

| Scenario | Effect |
|---|---|
| `ListBox` inside `StackPanel` | Infinite height — all items created |
| `ListBox` inside `ScrollViewer` | Infinite height (double scroll) |
| `ListBox` inside `Grid` row with `*` height | Constrained — virtualization works |
| `ListBox` with explicit `Height` | Constrained — virtualization works |

### Variable-height items

`VirtualizingStackPanel` assumes uniform item height. If items vary significantly:

- Give all items a fixed `Height` or `MinHeight` so scroll extent is accurate
- Allow content to clip or scroll internally
- Flatten hierarchical data into a single list with indent levels (like `TreeView`)

### BufferFactor trade-offs

| Factor | Extra items realized | Smoothness | Memory |
|---|---|---|---|
| 0 (default) | None | Baseline | Baseline |
| 0.5 | Half viewport | Better | Moderate |
| 1 | One full viewport | Smooth | Higher |
| 2 | Two viewports | Very smooth | Highest |

---

## 2. Layout pass mechanics

Each layout pass has two phases:

1. **Measure**: The parent asks each child for its desired size. Children traverse their own children recursively.
2. **Arrange**: The parent assigns each child its final position and size. Children arrange their children recursively.

A deeply nested tree multiplies these traversals. Flattening from 5 levels to 2 cuts the number of recursive calls by roughly 60%.

### What triggers layout

| Action | Effect |
|---|---|
| Setting `Width`/`Height` | Invalidates measure for self + parent |
| Setting `Margin`/`Padding` | Invalidates measure for self + parent |
| Setting `HorizontalAlignment` | Invalidates arrange only |
| Changing child visibility | Invalidates measure for parent |
| Changing `IsVisible` from `True` → `False` | Removes entire subtree from layout |
| Adding/removing children | Invalidates measure for parent |

Avalonia batches layout passes within a single dispatcher operation, so setting multiple properties in one code block only triggers one pass.

---

## 3. Hit testing performance

When a pointer event occurs, Avalonia walks the visual tree from root to leaf, testing each element. A control with 10,000 children causes 10,000 hit tests per pointer event.

### Reducing hit test cost

```xml
<Border Background="Transparent" IsHitTestVisible="False">
  <!-- Decorative overlay that should not capture clicks -->
</Border>
```

- Set `IsHitTestVisible="False"` on non-interactive elements
- Transparent backgrounds still participate in hit testing — always set `IsHitTestVisible="False"` on decorative overlays
- Consider custom hit-test logic using `RenderGeometry` for complex shapes (see [Hit Testing: Performance with many elements](https://docs.avaloniaui.net/docs/graphics-animation/hit-testing#performance-with-many-elements))

---

## 4. BitmapCache properties

| Property | Default | Description |
|---|---|---|
| `RenderAtScale` | `1` | Resolution multiplier. >1 = higher quality, more memory. 0 = disable caching. |
| `SnapsToDevicePixels` | `false` | Align to pixel grid for sharper cached output |
| `EnableClearType` | `false` | Enable ClearType subpixel rendering in cache |

For text-heavy cached content, enable both `SnapsToDevicePixels` and `EnableClearType`.

---

## 5. StreamGeometry vs PathGeometry

```csharp
// Efficient: StreamGeometry
var geometry = new StreamGeometry();
using (var ctx = geometry.Open())
{
    ctx.BeginFigure(new Point(0, 0), true);
    ctx.LineTo(new Point(100, 0));
    ctx.LineTo(new Point(100, 100));
    ctx.LineTo(new Point(0, 100));
    ctx.EndFigure(true);
}

// Less efficient: PathGeometry
var pathGeo = new PathGeometry();
var figure = new PathFigure { StartPoint = new Point(0, 0) };
figure.Segments.Add(new LineSegment { Point = new Point(100, 0) });
figure.Segments.Add(new LineSegment { Point = new Point(100, 100) });
figure.Segments.Add(new LineSegment { Point = new Point(0, 100) });
pathGeo.Figures.Add(figure);
```

`StreamGeometry` is a sealed, immutable, optimised representation. Use it for static shapes. `PathGeometry` is mutable and supports data binding — use it when you need to modify geometry at runtime.

---

## 6. Image sizing

Avalonia decodes images at full resolution by default. For thumbnails, resize before display:

```csharp
// In your ViewModel or service
using var stream = File.OpenRead(path);
var bitmap = Bitmap.DecodeToWidth(stream, 200); // decode to 200px width
```

```xml
<Image Source="{Binding Thumbnail}"
       RenderOptions.BitmapInterpolationMode="LowQuality" />
```

---

## 7. Binding errors

Each binding error causes:
1. Runtime reflection to attempt path resolution
2. Error logging to trace output

### Common causes

- `RelativeSource.FindAncestor` in `DataTemplate` — binding isn't resolved until template initialises
- Misspelled property names
- Missing `x:DataType` (though compiled bindings are default in Avalonia 12)

### Fix

```csharp
// Instead of RelativeSource.FindAncestor, define an attached property
public static readonly AttachedProperty<string> ParentContextProperty =
    AvaloniaProperty.RegisterAttached<MyClass, Control, string>("ParentContext",
        inherits: true);
```

Push values down the tree via property inheritance rather than walking up.

---

## 8. Profiler tool columns

| Profiler | Column | Meaning |
|---|---|---|
| Style Matching | Fast Reject Count | Selector excluded by type/name check — will never be re-evaluated |
| Style Matching | Match Attempts | Total evaluations against controls |
| Style Matching | Matches | Successful applications |
| Style Activators | Evaluations | Runtime re-evaluations of conditional selectors |
| Style Activators | Active Evaluations | Re-evaluations that activated the style |
| Resource Lookup | Total Lookups | Times this key was requested |
| Resource Lookup | Successful | Lookups that found a match |

### Interpreting profiler data

- **Style Matching**: If `Match Attempts` is high but `Matches` is low, the selector is too broad. Target concrete types (`Button` not `ContentControl`).
- **Style Activators**: If `Evaluations` is very high for a `:pointerover` selector, the control may be rapidly entering/exiting the pointer — check for nested elements that steal pointer focus.
- **Resource Lookup**: If `Total Lookups` is high but `Successful` is low, you have a missing or misspelled resource key. Use the Resources tool to verify.

---

## 9. Region dirty rect clipping

- In Avalonia 12.0, `UseRegionDirtyRectClipping` is **optional** (previously experimental).
- In Avalonia 12.1+, it is **disabled by default** because CPU cost of region tracking exceeds GPU gain on accelerated platforms.
- Enable only for software-rendered targets (embedded Linux, low-end devices without GPU).
- `MaxDirtyRects` defaults to `8`. Set to `0` or negative to bypass and use the drawing context's native region support.

---

## Key Takeaways

- Virtualization requires constrained height — always check the parent panel
- Flat layouts reduce recursive measure/arrange passes significantly
- `IsVisible="False"` removes subtree from layout; `Opacity="0"` does not
- `IsHitTestVisible="False"` on non-interactive elements reduces pointer event cost
- `StreamGeometry` is the preferred geometry type for static shapes
- Decode images at target size rather than loading full resolution for thumbnails
- Binding errors cause runtime reflection and log overhead — avoid `RelativeSource.FindAncestor`
- DevTools Profiler has three focused tabs; each helps identify different bottleneck categories
- Region clipping is opt-in and mainly for software-rendered targets
