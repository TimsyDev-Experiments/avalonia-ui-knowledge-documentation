---
tier: advanced
topic: rendering
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 025-compositor-custom-visuals.md
---

# 025V — Compositor & Custom Visuals: An In-Depth Companion

**Why this exists.** The original tutorial introduces `Visual` subclasses, `CompositorAnimation`, and frame callbacks for a particle system. This companion explains what the compositor actually is (separate thread, immediate-mode renderer), the contract between `Visual` and `Control`, when compositor animations are worth the complexity, and why `RenderOpen()` is rarely what you need.

**Read this alongside:** [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)

---

## 1. What the compositor is and is not

### The compositor architecture

Avalonia's rendering runs on two threads:

```
UI Thread                    Compositor Thread (render)
───────────────              ─────────────────────────
Property changes             Reads dirty list
Layout (measure/arrange)     Applies transforms
InvalidateVisual()           Calls Render(DrawingContext)
Event handling               Compositor transforms
                             Hit-testing
                             Clipping
```

The **compositor** is a retained-mode scene graph that lives on the render thread. When you set `RenderTransform`, `Opacity`, `Clip`, or `IsVisible` on a `Visual`, you are modifying properties that the compositor reads directly — without calling `Render`.

### What happens on a property change

1. UI thread: `ScaleTransform.ScaleX = 1.5` → marks the visual's transform as dirty.
2. Render thread: Picks up the dirty flag, recomputes the transform matrix, and redraws the affected region by re-applying the existing visual content with the new transform.

This means changing `ScaleX` every frame does **not** call your `Render` override — the compositor re-uses the cached drawing output and just runs it through a new matrix. This is why compositor-level transforms are fast.

### What the compositor does NOT do

- It does not run arbitrary C# code.
- It does not execute `DrawingContext` commands from scratch each frame (unless `InvalidateVisual()` is called).
- It does not know about layout, data binding, or `PropertyChanged` events.

---

## 2. `Visual` vs `Control`

```csharp
public class Particle : Visual
```

### What `Visual` provides

`Visual` is the lightest renderable node in Avalonia:
- `Bounds` — the element's layout rectangle.
- `RenderTransform` / `Opacity` / `Clip` — compositor-managed properties.
- `VisualParent` / `VisualChildren` — tree membership.
- `Render(DrawingContext)` — override this to draw.

### What `Visual` does NOT provide

- No `Measure` / `Arrange` — `Visual` is not `ILayoutable`. It has no layout pass. You must set `Bounds` manually or rely on a parent `Control` to position it.
- No `DataContext`, no `Binding`, no `Resources` — `Visual` is not `StyledElement`. You cannot data-bind to a `Visual`.
- No `Classes`, no `PseudoClasses`, no style system.
- No `OnApplyTemplate`, no `Template`.

### When to subclass `Visual`

Use `Visual` when you need hundreds or thousands of lightweight drawable nodes that do not need layout, binding, or styling. Each `Control` instance adds significant overhead (property dictionary, style cache, layout state). A `Visual` is roughly a `DrawingContext` snapshot with a transform and an opacity.

### The custom `Visual` must be parented

A `Visual` that is not attached to the visual tree via `VisualChildren.Add()` produces no output. The `ParticleSystem` control adds particles to its `VisualChildren` collection, which makes them part of the scene graph.

---

## 3. Why `AvaloniaLocator.Current.GetService<Compositor>()`

```csharp
_compositor = AvaloniaLocator.Current.GetService<Compositor>()!;
```

### `AvaloniaLocator` as a service locator

`AvaloniaLocator` is a simple service locator that is populated during startup (`AppBuilder.Start()`). It provides framework-level services:
- `Compositor` — the render-thread compositor instance.
- `IPlatformRenderInterface` — the Skia/Direct2D abstraction.
- `IFontManagerImpl` — font lookup.

In production code, avoid calling `AvaloniaLocator` directly in constructors. The compositor may not be resolved in unit tests without a headless platform. Instead, resolve it lazily or pass it as a dependency:

```csharp
private Compositor? _compositor;
private Compositor Compositor => _compositor ??=
    AvaloniaLocator.Current.GetService<Compositor>()
    ?? throw new InvalidOperationException("Compositor not available");
```

### When `GetService<Compositor>` returns null

- During unit testing without a headless platform.
- During design-time preview (the designer runs without a full compositor).
- Before `AppBuilder.Start()` completes.

Always null-check or use the lazy pattern to avoid `NullReferenceException` in these scenarios.

---

## 4. `CompositorAnimation`

```csharp
var compositorAnimation = new CompositorAnimation
{
    Duration = TimeSpan.FromSeconds(1),
    IterationCount = IterationCount.Infinite,
    AnimationMode = AnimationMode.Compositor
};
```

### Why `AnimationMode.Compositor` matters

The `AnimationMode` enum tells the framework which component drives the animation:

| Mode | Driven by | Properties animatable |
|---|---|---|
| `Compositor` | Render thread | `RenderTransform`, `Opacity`, `Clip` |
| `Style` | Style system | Any `StyledProperty` |
| `PropertyValue` | Property system | Any `StyledProperty` with registered animator |

When you set `AnimationMode.Compositor`, the animation keyframes are sent to the compositor thread, which interpolates them per frame **without involving the UI thread**. This is the only mode that can animate thousands of elements at 60fps without stuttering.

### Limitations of compositor animations

- Only `TransformOperations` can be animated (via `CompositorKeyFrame.Value`).
- No easing functions (the compositor uses linear interpolation).
- No event callbacks (no `Completed` or `Progress` events).
- No cancellation token support in the original API.

### The `CompositorKeyFrame`

```csharp
compositorAnimation.Children.Add(new CompositorKeyFrame
{
    Cue = new Cue(0.0),
    Value = TransformOperations.Parse("rotate(0deg)")
});
```

`CompositorKeyFrame.Value` is typed as `object?`, but the compositor only knows how to interpret `TransformOperations`. Passing any other type produces a runtime error in the compositor pipeline.

---

## 5. `RequestCompositionUpdate` — the frame callback

```csharp
Compositor?.RequestCompositionUpdate(OnCompositionUpdate);
```

### What this does

Registers a callback that the compositor invokes on the **UI thread** at the start of each composition frame (typically 60 times per second, or at the display's refresh rate). The callback receives `CompositionUpdateEventArgs` with:
- `Time` — a `TimeSpan` representing the current compositor time (not wall-clock; this can pause when the app is backgrounded).
- `Compositor` — the compositor instance.

### What you can do in the callback

- Update `Visual` coordinates (set `Position`, update `Bounds`).
- Call `InvalidateVisual()` on `Visual` subclasses.
- Read input state.
- **Do not** manipulate layout, add/remove children, or trigger binding evaluation.

### What happens if the callback is slow

The compositor synchronizes with the UI thread frame callback. If your `OnCompositionUpdate` takes longer than the frame interval (16ms at 60fps), the compositor skips frames. The animation appears to stutter. If you need heavy computation per particle, run it on a background thread and only update coordinates in the callback.

### Unregistering the callback

`RequestCompositionUpdate` returns an `IDisposable`. Dispose it when the control is detached:

```csharp
private IDisposable? _updateSubscription;

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnDetachedFromVisualTree(e);
    _updateSubscription?.Dispose();
    _updateSubscription = null;
}
```

Without disposal, the callback keeps running after the control is removed from the tree — wasted CPU cycles and potential ghost updates.

---

## 6. `RenderOpen()` — what it is and when to use it

```csharp
var drawingContext = (self as Visual)?.RenderOpen();
```

### What `RenderOpen()` does

`RenderOpen()` creates a `DrawingContext` that draws directly into the visual's cached render layer. This is **not** the same as overriding `Render`. Changes made via `RenderOpen()` persist until the next `InvalidateVisual()` clears and rebuilds the render cache.

### Why you almost never need it

- The render cache is volatile — the compositor may discard it at any time (window resize, DPI change, theme switch).
- Drawing via `RenderOpen()` while `Render` also draws produces double-drawing or overlapping artifacts.
- There is no synchronization between `RenderOpen()` calls and the compositor's render thread.

Use `RenderOpen()` only for advanced scenarios like:
- Drawing directly into a `Visual` that is not part of the layout tree.
- Rendering off-screen for texture caching.
- Interop with external rendering APIs.

For normal custom drawing, override `Render(DrawingContext)` and call `InvalidateVisual()` to trigger re-render.

---

## 7. `RenderTargetBitmap` — off-screen rendering

```csharp
var bitmap = new RenderTargetBitmap(new PixelSize(200, 200));
using (var ctx = bitmap.CreateDrawingContext())
{
    ctx.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 200, 200));
}
```

### What `RenderTargetBitmap` is

An off-screen render target that exists in CPU memory. You can draw to it using `DrawingContext` and then display it as an `IImage` source:

```xml
<Image Source="{Binding RenderedBitmap}" />
```

### Performance characteristics

- Every `CreateDrawingContext` → `Dispose` cycle renders into a software bitmap (Skia raster backend) or a GPU texture (if using Skia's GL backend).
- Reading back pixels (`bitmap.GetPixels()`) is expensive — causes a GPU→CPU sync point.
- Writing to a `RenderTargetBitmap` every frame for particle effects is slower than using the compositor path.

---

## 8. When to use compositor vs `Render` override

| Scenario | Approach |
|---|---|
| 1–10 elements with transforms | `RenderTransform` on `Control` — no compositor complexity needed |
| 10–100 elements, opacity/transform per frame | `Visual` subclass + `RequestCompositionUpdate` — compositor path |
| 100–10000 particles | `CompositorAnimation` or dedicated particle system — compositor path |
| Dynamic path drawing (signature, chart) | `Render` override with `InvalidateVisual` — compositor does not support procedural geometry updates per frame |
| Off-screen caching | `RenderTargetBitmap` |

The compositor path is not universally faster. The overhead of managing `Visual` children and frame callbacks only pays off above roughly 50 independently moving elements. Below that, `Render` overrides with `InvalidateVisual()` are simpler and equally performant.

---

## Cross-links

- [024 — Animation & Transitions](024-animation-transitions.md) — UI-thread animation alternatives
- [061 — Rendering & Interop Boundaries](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/61-rendering-and-interop-boundaries-opengl-vulkan-framebuffer.md) (plugin ref)
- [025E — Compositor & Custom Visuals (examples)](025-compositor-custom-visuals-examples.md)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — `Render(DrawingContext)` basics
- [Avalonia Docs: Compositor](https://docs.avaloniaui.net/docs/concepts/compositor)
