---
tier: advanced
topic: animation
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 024-animation-transitions.md
---

# 024V — Animation & Transitions: An In-Depth Companion

**Why this exists.** The original tutorial covers three animation APIs (style animations, transitions, programmatic keyframe animations) and the `RenderTransform` approach. This companion explains each API's threading model, when the compositor picks them up, how `FillMode` affects post-animation state, and the performance characteristics that should drive your choice between them.

**Read this alongside:** [024 — Animation & Transitions](024-animation-transitions.md)

---

## 1. The three animation APIs

Avalonia provides three distinct animation systems, each with different tradeoffs:

| API | Runs on | CPU/GPU | Property types | Use case |
|---|---|---|---|---|
| **Transitions** (`Transitions` collection) | UI thread (first frame), then compositor | GPU for render properties, CPU for layout | Properties listed in the transition table | Simple property change animations (button hover, color fade) |
| **Style animations** (`Style.Animations`) | Compositor / UI thread | GPU | Properties on the styled element | Trigger-based (pseudo-class) loops |
| **Programmatic keyframes** (`Animation` class) | UI thread | CPU | Any `AvaloniaProperty` | Complex multi-step sequences, delays, staggered starts |

### How to choose

- **Transitions** are the easiest and most performant for simple value interpolation (color, opacity, position). They require no code-behind.
- **Style animations** are the right choice for hover/press/focus effects driven by pseudo-classes.
- **Programmatic keyframes** are needed when the animation parameters (duration, target value) are computed at runtime.

---

## 2. How transitions work internally

```xml
<Border.Transitions>
  <Transitions>
    <BrushTransition Property="Background" Duration="0:0:0.3" />
  </Transitions>
</Border.Transitions>
```

### The subscription mechanism

When the `Transitions` collection is set on a control, each `Transition` instance subscribes to its `Property`'s change events. When `Background` changes (e.g., from binding or style setter), the transition:

1. Captures the old value (before the new value is applied).
2. Creates an internal animator that interpolates between old and new.
3. The animator produces intermediate values each frame and writes them via the property system.
4. After `Duration` elapses, the animator removes itself and the property settles at the final value.

### Why transitions stop when the control is hidden (Avalonia 12)

```xml
<!-- Animation.PlaybackBehavior="Always" to override -->
```

In v12, when a control is removed from the visual tree or has `IsVisible = false`, the compositor pauses all active transitions on that control's subtree. This reduces CPU usage because invisible animations produce no visible pixels. If your transition must complete regardless of visibility (e.g., an animation that plays during a fade-out), set `PlaybackBehavior="Always"`.

### The `FillMode` property

| `FillMode` | Behavior after the transition ends |
|---|---|
| `Forward` | Retains the final keyframe value |
| `Backward` | Retains the first keyframe value |
| `Both` | Retains both (applies first when waiting, last when done) |
| `None` | Returns to pre-animation value |

For transitions, `Forward` is the default — after the background fades to the new color, it stays there. If you used `None`, the background would snap back to the old value once the transition completes.

---

## 3. Style animations and pseudo-classes

```xml
<Style Selector="Button.primary /pressed/">
  <Style.Animations>
    <Animation Duration="0:0:0.15">
      <KeyFrame Cue="0%">
        <Setter Property="Background" Value="#6a33ff" />
      </KeyFrame>
      <KeyFrame Cue="100%">
        <Setter Property="Background" Value="#4a1ac0" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

### The pseudo-class lifecycle

When the pointer is pressed on the button:
1. The `:pressed` pseudo-class is added.
2. The style selector matches, and the animation starts.
3. The background interpolates from `#6a33ff` to `#4a1ac0` over 150ms.
4. When the pointer is released, `:pressed` is removed.
5. If the base style has no `:pressed` animation, the property snaps back to the base value (or transitions back if a transition is defined).

### Important: where the initial value comes from

The `0%` keyframe must match the base state's value. In this example, if the base `Background` is `#6a33ff` (from the `Button.primary` setter), the animation interpolates from that base to the pressed color. If the `0%` keyframe specified a different color, the animation would jump to that color on press.

### Animation playback direction

When a pseudo-class is added, the animation plays forward (`0% → 100%`). When the pseudo-class is removed, the animation plays in **reverse** (`100% → 0%`) by default. This means the button fades back to the original color when released, which is usually the desired behavior.

---

## 4. Programmatic `Animation` class

```csharp
var animation = new Animation
{
    Duration = TimeSpan.FromSeconds(0.3),
    FillMode = FillMode.Forward
};

animation.Children.Add(new KeyFrame
{
    Cue = new Cue(0.0),
    Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
});
```

### The `RunAsync` contract

```csharp
await animation.RunAsync(target, null);
```

`RunAsync` returns a `Task` that completes when the animation finishes (or is cancelled). The second parameter is an optional `CancellationToken`. If provided, canceling the token stops the animation immediately and the task completes.

### The animation is per-instance

Each `RunAsync` call creates a new animation controller that animates the specific `target` element. You can reuse the same `Animation` object for multiple targets — but concurrent `RunAsync` calls on the same target for the same property will compete, producing jitter.

### Why `IsVisible` is set after the animation

```csharp
await animation.RunAsync(target, null);
target.IsVisible = false;
```

The `await` ensures the opacity fade completes before the element is hidden. Without the await, `IsVisible` would be set immediately, the element would disappear, and the animation would run invisibly (or in v12, stop due to the hidden-control optimization).

---

## 5. `RenderTransform` vs `LayoutTransform`

```xml
<Button.RenderTransform>
  <ScaleTransform ScaleX="1" ScaleY="1" />
</Button.RenderTransform>
```

### `RenderTransform`

Applied to the visual after layout completes. The element's layout slot is computed **without** the transform. The transform scales/rotates/skews the rendered output. Neighboring elements are not affected.

- **Performance:** GPU-accelerated (compositor handles it). No re-layout on each frame.
- **Use for:** Hover scale effects, entrance animations, wobble effects.

### `LayoutTransform`

Applied before layout. The element's layout slot is computed **with** the transform applied. Neighboring elements shift to accommodate the scaled/rotated element.

- **Performance:** CPU-bound (triggers re-layout). Never use for per-frame animation.
- **Use for:** Permanent rotations (e.g., a rotated label in a form).

### `TransformOperations.Parse`

The original uses `TransformOperations.Parse("scale(1)")` in style animations. `TransformOperations` is a combined transform type that supports multiple operations:

```
scale(1.5) translate(10px, 20px) rotate(45deg)
```

This is more performant than setting `RenderTransform` to a `ScaleTransform` object because `TransformOperations` is a lightweight struct that the compositor can consume directly without creating a `Transform` object graph.

---

## 6. Available transition types and their internals

| Transition | What it interpolates | Performance notes |
|---|---|---|
| `DoubleTransition` | Continuous value between two doubles | Fast; no allocations per frame |
| `FloatTransition` | Same as Double, but 32-bit | Slightly faster on ARM |
| `BrushTransition` | Interpolates `SolidColorBrush` colors (no gradient interpolation) | Allocates new brush instances during animation; use sparingly |
| `ColorTransition` | RGB or scRGB interpolation | CPU-bound per frame |
| `TransformOperationsTransition` | Interpolates between two `TransformOperations` values | GPU-accelerated; prefer for transforms |
| `BoolTransition` | Cross-fade effect for visibility | Uses opacity internally, not instant |

### Why there is no `BrushTransition.EnableColorInterpolation`

`BrushTransition` only interpolates `SolidColorBrush`. If the start or end brush is a gradient, the transition snaps at the halfway point. There is no gradient-to-gradient interpolation in Avalonia 12.

---

## 7. Common pitfalls

### Pitfall 1: Animating `LayoutTransform` on a loop

Never use `LayoutTransform` in a per-frame animation (style animation or programmatic). It triggers `InvalidateMeasure()` on every frame, which re-runs layout on the entire subtree. At 60fps, this causes frame drops even on fast hardware.

### Pitfall 2: Forgetting `FillMode`

If an animation sets `Opacity` to `0.0` and `FillMode` is `None`, the element returns to full opacity immediately after the animation completes. The fade-out works but the element is immediately visible again. Always set `FillMode = Forward` for fade-out animations.

### Pitfall 3: Overlapping transitions and style animations

If a `BrushTransition` is defined on a `Border.Background` and a style animation also targets `Border.Background`, the two animation systems compete. The property value oscillates. Pick one system per property per element.

### Pitfall 4: `RunAsync` on a disposed element

If `target` is removed from the visual tree (e.g., navigating away from a page) while `RunAsync` is in flight, the animation either stops silently or throws `ObjectDisposedException`. Use `CancellationToken` linked to the page's disposal:

```csharp
var cts = new CancellationTokenSource();
Unloaded += (_, _) => cts.Cancel();
await animation.RunAsync(target, cts.Token);
```

---

## Cross-links

- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md) — for GPU-only compositor animations that bypass the UI thread entirely
- [024E — Animation & Transitions (examples)](024-animation-transitions-examples.md)
- [024 — Animation & Transitions](024-animation-transitions.md) — comprehensive coverage of animation primitives
- [Avalonia Docs: Animation](https://docs.avaloniaui.net/docs/animation/)
