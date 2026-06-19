---
tier: advanced
topic: development
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 085 — Performance & Profiling — Quiz

**Prerequisites:** [085-core](085-performance-profiling.md)

---

### Q1: Which parent panel disables ListBox virtualization by giving it infinite height?

<details>
<summary>Answer</summary>

`StackPanel`. Because `StackPanel` gives children infinite space in its layout direction, the `ListBox` gets infinite height and creates all items. Use a `Grid` row with `*` height or an explicit `Height` instead.

</details>

---

### Q2: What does `BufferFactor` on `VirtualizingStackPanel` control, and what is the default value?

<details>
<summary>Answer</summary>

`BufferFactor` controls how many extra viewport heights of items are kept realized beyond the visible area. Default is `0`. A value of `1` keeps one full viewport above and below — smoother scrolling at higher memory cost.

</details>

---

### Q3: Which technique is more efficient for layout: nested `StackPanel`s or a single `Grid`?

<details>
<summary>Answer</summary>

A single `Grid` with rows and columns. It avoids the recursive measure/arrange overhead of multiple nested `StackPanel`s.

</details>

---

### Q4: Why does `IsVisible="False"` perform better than `Opacity="0"`?

<details>
<summary>Answer</summary>

`IsVisible="False"` removes the entire subtree from both layout (measure/arrange) and rendering. `Opacity="0"` keeps the element in layout and rendering passes — it's still measured and drawn, just invisible.

</details>

---

### Q5: True or False: Setting `IsHitTestVisible="False"` on a transparent overlay improves pointer event performance.

<details>
<summary>Answer</summary>

True. Transparent controls still participate in hit testing by default. Setting `IsHitTestVisible="False"` excludes them from the visual tree walk during pointer events.

</details>

---

### Q6: Which geometry type uses less memory and is preferred for static shapes?

<details>
<summary>Answer</summary>

`StreamGeometry`. It is sealed, immutable, and optimised for static shapes. `PathGeometry` is mutable but uses more memory — use it only when you need to modify geometry at runtime.

</details>

---

### Q7: Name three profiler tabs in the DevTools Profiler tool and what each measures.

<details>
<summary>Answer</summary>

1. **Style Matching** — measures selector evaluation during control creation. Identifies overly broad selectors.
2. **Style Activators** — measures runtime re-evaluation of conditional selectors (`:pointerover`, `:focus`, etc.).
3. **Resource Lookup** — measures resource resolution by key. Identifies missing or misspelled resource keys.

</details>

---

### Q8: A style selector shows 10,000 match attempts but only 120 matches. What does this indicate?

<details>
<summary>Answer</summary>

The selector is overly broad — it's being tested against many controls that don't match. Narrow the selector by targeting a concrete control type (e.g., `TextBlock.h1` instead of just `.h1`).

</details>

---

### Q9: What is the recommended alternative to `RelativeSource.FindAncestor` inside a `DataTemplate`?

<details>
<summary>Answer</summary>

Define an attached property with `inherits: true` and push values down the visual tree via property inheritance. This avoids binding errors that occur when `FindAncestor` can't resolve during template initialisation.

</details>

---

### Q10: Write a pattern that decodes an image to a specific width for thumbnail display.

<details>
<summary>Answer</summary>

```csharp
using var stream = File.OpenRead(path);
var thumbnail = Bitmap.DecodeToWidth(stream, 200);
```

```xml
<Image Source="{Binding Thumbnail}" />
```

</details>

---

### Q11: Why would you increase `MaxGpuResourceSizeBytes` in `SkiaOptions`?

<details>
<summary>Answer</summary>

The default GPU resource cache is approximately 28 MB. If your app works with large images, tilesets, or many cached visuals, exceeding this limit forces re-upload to the GPU each frame, causing stuttering. Increase it (e.g., 256 MB) to keep resources cached longer.

</details>

---

### Q12: What is the purpose of `UseRegionDirtyRectClipping` and when should you enable it?

<details>
<summary>Answer</summary>

It enables region-based dirty-rect clipping for more accurate repaint areas. Enable it for software-rendered targets (embedded Linux, low-end devices without GPU) where reducing painted area matters more than the CPU cost of region tracking. It is disabled by default in Avalonia 12.1+.

</details>
