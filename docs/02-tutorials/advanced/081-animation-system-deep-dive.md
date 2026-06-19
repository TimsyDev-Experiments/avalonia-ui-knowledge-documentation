---
tier: advanced
topic: animation
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 081 — Animation System Deep Dive

**What you'll learn:** The full animation architecture — key frame lifecycle, easing function internals, code-driven animation control, composition animations, page transitions, and performance optimization.

**Prerequisites:** [024 — Animation & Transitions](024-animation-transitions.md)

---

## 1. Animation architecture

Avalonia animations are built on three core types:

| Type | Role |
|---|---|
| `Animation` | Defines duration, iteration count, easing, fill mode, and contains key frames |
| `KeyFrame` | Captures property values at a cue point (time fraction) along the timeline |
| `Setter` | Sets a property value on a `KeyFrame` |

Animations attach to controls through one of **four layers**:

| Layer | Runs on | Trigger | Best for |
|---|---|---|---|
| `Style.Animations` | UI thread | Style selector match / pseudo-class | Declarative keyframe sequences |
| `Transitions` | UI thread | Property value change | Simple property-to-property smoothing |
| `Animation.RunAsync()` | UI thread | Code call | Programmatic control |
| Composition animations | Render thread | `StartAnimation()` / implicit | Performance-sensitive effects |

---

## 2. Animation settings reference

```xml
<Animation Duration="0:0:0.5"
            Delay="0:0:0.1"
            IterationCount="3"
            PlaybackDirection="Alternate"
            FillMode="Forward"
            Easing="CubicEaseOut"
            PlaybackBehavior="Always">
  <KeyFrame Cue="0%">
    <Setter Property="Opacity" Value="0" />
  </KeyFrame>
  <KeyFrame Cue="100%">
    <Setter Property="Opacity" Value="1" />
  </KeyFrame>
</Animation>
```

### FillMode

| Value | Behavior |
|---|---|
| `None` | Value resets when animation ends or during delay |
| `Forward` | Persists last interpolated value after animation ends |
| `Backward` | Applies first value during the delay period |
| `Both` | Combines Forward and Backward |

`FillMode.Forward` is essential for fade-out: without it the element snaps back to full opacity after the animation ends.

### PlaybackDirection

| Value | Sequence |
|---|---|
| `Normal` | 0% → 100% |
| `Reverse` | 100% → 0% |
| `Alternate` | 0→100, 100→0, 0→100, ... |
| `AlternateReverse` | 100→0, 0→100, 100→0, ... |

### IterationCount

Use an integer for a fixed repeat count or the string `Infinite` for unbounded looping:

```xml
<Animation IterationCount="Infinite" PlaybackDirection="Alternate">
```

### PlaybackBehavior

| Value | Behavior |
|---|---|
| `Normal` | Animation pauses when the control is hidden (v12 default) |
| `Always` | Always runs, even on hidden controls |

---

## 3. Key frames and cue resolution

Cues are expressed as percentages (0%–100%) or as `Cue` objects in code:

```csharp
new KeyFrame
{
    Cue = new Cue(0.0),   // start
    Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
};
new KeyFrame
{
    Cue = new Cue(0.5),   // midpoint
    Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.5 } }
};
new KeyFrame
{
    Cue = new Cue(1.0),   // end
    Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
};
```

Avalonia interpolates values between adjacent key frames using the animation's easing function. Non-animatable property types cause an `InvalidOperationException` at runtime.

---

## 4. Easing functions deep dive

### Built-in families

| Family | Variants | Character |
|---|---|---|
| `LinearEasing` | (single) | Constant speed |
| `SineEase` | In / Out / InOut | Gentle sine curve |
| `QuadraticEase` | In / Out / InOut | t² |
| `CubicEase` | In / Out / InOut | t³ |
| `QuarticEase` | In / Out / InOut | t⁴ |
| `QuinticEase` | In / Out / InOut | t⁵ |
| `ExponentialEase` | In / Out / InOut | Sharp acceleration |
| `CircularEase` | In / Out / InOut | Circular arc |
| `BackEase` | In / Out / InOut | Overshoots then settles |
| `BounceEase` | In / Out / InOut | Simulated bouncing |
| `ElasticEase` | In / Out / InOut | Spring oscillation |

### SplineEasing (custom cubic bezier)

```xml
<Animation.Easing>
  <SplineEasing X1="0.25" Y1="0.1" X2="0.25" Y2="1.0" />
</Animation.Easing>
```

Shorthand on `KeyFrame`:

```xml
<KeyFrame Cue="0%" KeySpline="0.25,0.1,0.25,1.0">
```

The four values map to CSS `cubic-bezier()`: `(x1, y1, x2, y2)`.

| Preset | KeySpline |
|---|---|
| `ease` | `0.25,0.1,0.25,1.0` |
| `ease-in` | `0.42,0,1.0,1.0` |
| `ease-out` | `0,0,0.58,1.0` |
| `ease-in-out` | `0.42,0,0.58,1.0` |

### SpringEasing (physics-based)

```xml
<Animation.Easing>
  <SpringEasing Mass="1" Stiffness="100" Damping="10" InitialVelocity="0" />
</Animation.Easing>
```

| Parameter | Effect of increasing |
|---|---|
| `Mass` | Slower, heavier motion |
| `Stiffness` | Snappier, faster oscillation |
| `Damping` | Less oscillation, settles faster |
| `InitialVelocity` | Stronger initial movement |

Low damping values (~3) produce bouncy motion; high damping values (~20) are overdamped.

### Custom easing function

```csharp
using Avalonia.Animation.Easings;

public class StepEasing : Easing
{
    public int Steps { get; set; } = 4;

    public override double Ease(double progress)
    {
        return Math.Floor(progress * Steps) / Steps;
    }
}
```

```xml
<Animation.Easing>
  <local:StepEasing Steps="8" />
</Animation.Easing>
```

---

## 5. Animatable property types

Avalonia can interpolate the following types in key frames and transitions:

| Type | Valid for |
|---|---|
| `double` | Opacity, Width, Height, rotation angles, `RenderTransform` values |
| `float` | Composition animation properties |
| `int` | Integer properties (integer transition only) |
| `Color` | Background, Border fill, text color |
| `IBrush` | Full brush interpolation (gradient + solid) |
| `Point` | Canvas.Left/Top, translate offsets |
| `Vector` | Scroll offsets, composition offsets |
| `Size` | Control size changes |
| `Thickness` | Padding, Margin, BorderThickness |
| `CornerRadius` | Border corner rounding |
| `BoxShadows` | Drop shadow transitions |
| `TransformOperations` | CSS-shorthand `RenderTransform` values |
| `bool` | Instant cross-fade style via `BoolTransition` |

Attempting to animate a non-registered type throws an `AnimationException`.

---

## 6. Code-driven animations

### From a XAML resource (recommended)

```xml
<Window.Resources>
  <Animation x:Key="FadeOut"
             x:SetterTargetType="Button"
             Duration="0:0:0.3"
             FillMode="Forward">
    <KeyFrame Cue="0%">
      <Setter Property="Opacity" Value="1" />
    </KeyFrame>
    <KeyFrame Cue="100%">
      <Setter Property="Opacity" Value="0" />
    </KeyFrame>
  </Animation>
</Window.Resources>
```

```csharp
var animation = (Animation)this.Resources["FadeOut"]!;
await animation.RunAsync(targetButton, CancellationToken.None);
targetButton.IsVisible = false;
```

### Purely from code

```csharp
var animation = new Animation
{
    Duration = TimeSpan.FromSeconds(0.3),
    FillMode = FillMode.Forward,
    Easing = new CubicEaseOut()
};

animation.Children.Add(new KeyFrame
{
    Cue = new Cue(0.0),
    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
});
animation.Children.Add(new KeyFrame
{
    Cue = new Cue(1.0),
    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
});

await animation.RunAsync(target, cancellationToken);
```

### Cancellation

`RunAsync` accepts a `CancellationToken`. On cancellation the animation stops at its current position. Infinite animations never complete unless cancelled.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await animation.RunAsync(target, cts.Token);
```

---

## 7. Composition animations (render thread)

Composition animations run on the render thread and animate `CompositionVisual` properties (`Offset`, `Opacity`, `Size`).

```csharp
var visual = ElementComposition.GetElementVisual(myControl);
var compositor = visual.Compositor;

var animation = compositor.CreateVector3KeyFrameAnimation();
animation.Duration = TimeSpan.FromMilliseconds(400);
animation.InsertKeyFrame(0f, new Vector3D(-200, 0, 0));
animation.InsertKeyFrame(1f, new Vector3D(0, 0, 0));

visual.StartAnimation("Offset", animation);
```

### Implicit animations

Trigger automatically when a mapped property changes:

```csharp
var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
offsetAnimation.Duration = TimeSpan.FromMilliseconds(300);
offsetAnimation.Target = "Offset";
offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue");

var implicitAnimations = compositor.CreateImplicitAnimationCollection();
implicitAnimations["Offset"] = offsetAnimation;
visual.ImplicitAnimations = implicitAnimations;
```

| Composition property | Key frame type | Code example |
|---|---|---|
| `Offset` | `Vector3KeyFrameAnimation` | `compositor.CreateVector3KeyFrameAnimation()` |
| `Opacity` | `ScalarKeyFrameAnimation` | `compositor.CreateScalarKeyFrameAnimation()` |
| `Size` | `Vector2KeyFrameAnimation` | `compositor.CreateVector2KeyFrameAnimation()` |

---

## 8. Page transitions

### Built-in types

```xml
<TransitioningContentControl PageTransition="{StaticResource SlideTransition}"
                              Content="{Binding CurrentPage}" />
```

```xml
<!-- CrossFade: subtle, non-directional -->
<CrossFade Duration="0:0:0.5" />

<!-- PageSlide: directional (horizontal by default) -->
<PageSlide Duration="0:0:0.5" Orientation="Vertical" />

<!-- CompositePageTransition: combine multiple -->
<CompositePageTransition>
  <CrossFade Duration="0:0:0.5" />
  <PageSlide Duration="0:0:0.5" Orientation="Horizontal" />
</CompositePageTransition>
```

### Custom IPageTransition

```csharp
public class CustomTransition : IPageTransition
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(0.3);

    public async Task Start(Visual? from, Visual? to, bool forward,
                            CancellationToken cancellationToken)
    {
        var scaleAnim = new Animation
        {
            Duration = Duration,
            FillMode = FillMode.Forward
        };
        scaleAnim.Children.Add(new KeyFrame
        {
            Cue = new Cue(0d),
            Setters = { new Setter(ScaleTransform.ScaleYProperty, 1d) }
        });
        scaleAnim.Children.Add(new KeyFrame
        {
            Cue = new Cue(1d),
            Setters = { new Setter(ScaleTransform.ScaleYProperty, 0d) }
        });

        if (from != null)
            _ = scaleAnim.RunAsync(from, cancellationToken);
        if (to != null)
        {
            to.IsVisible = true;
            _ = scaleAnim.RunAsync(to, cancellationToken);
        }

        await Task.Delay(Duration, cancellationToken);
    }
}
```

---

## 9. Performance guidelines

| Practice | Why |
|---|---|
| Animate `RenderTransform`, not `LayoutTransform` | Render transforms skip measure/arrange |
| Use CSS-shorthand `RenderTransform` values over WPF-style transform objects | Enables `TransformOperationsTransition` |
| Prefer composition animations for frequently updating properties | Render thread, zero UI thread impact |
| Avoid animating `Width`/`Height` every frame | Triggers full layout pass each tick |
| Use `BooleanTransition` for instant visibility toggles | Avoids unnecessary work |
| Set `PlaybackBehavior="Normal"` (default) on hidden controls | Stops ticking when offscreen |

---

## Key Takeaways

- Avalonia offers four animation layers: style keyframes, transitions, code-driven `Animation.RunAsync`, and composition animations
- `FillMode` controls post-animation state persistence — use `Forward` for fade-outs
- Easing functions range from `LinearEasing` to `SpringEasing` with custom cubic bezier support
- Composition animations run on the render thread via `ElementComposition.GetElementVisual()`
- Page transitions (`CrossFade`, `PageSlide`, `CompositePageTransition`) support custom `IPageTransition` implementations
- Animate `RenderTransform` for GPU-composited, layout-free animation

---

## See Also

- [024 — Animation & Transitions](024-animation-transitions.md) — basic animation patterns
- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md) — compositor rendering model
- [082 — Graphics & Drawing Reference](082-graphics-drawing-reference.md)
- [Avalonia Docs: Animations](https://docs.avaloniaui.net/docs/graphics-animation/animations)
