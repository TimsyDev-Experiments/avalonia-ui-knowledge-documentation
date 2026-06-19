---
tier: advanced
topic: animation
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 081V — Animation System Deep Dive (verbose companion)

**What this covers:** Animation class internals, interpolation engine, global playlist management, attached property animation, and the event-driven AnimationTracker.

**Prerequisites:** 081 — Animation System Deep Dive core

---

## 1. Animation class internals

`Animation` implements `IDisposable`. Key properties and their default values:

```csharp
public class Animation : IDisposable
{
    public TimeSpan Duration { get; set; }
    public TimeSpan Delay { get; set; }
    public IterationCount IterationCount { get; set; } = IterationCount.Default; // 1
    public PlaybackDirection PlaybackDirection { get; set; } = PlaybackDirection.Normal;
    public FillMode FillMode { get; set; } = FillMode.None;
    public Easing Easing { get; set; } = new LinearEasing();
    public PlaybackBehavior PlaybackBehavior { get; set; } = PlaybackBehavior.Normal;
    public IList<KeyFrame> Children { get; }
}
```

`SpeedRatio` is not exposed in the current API. To slow an animation, extend `Duration`.

### Animation.RunAsync lifecycle

```
RunAsync(control, cancellationToken)
  → Validates setters against control type
  → If Delay > 0, waits (respecting cancellation)
  → For each iteration:
      → Resolves cue-to-cue segments from Children
      → Evaluates easing function per frame tick
      → Applies interpolated values via Setter
      → Handles FillMode on completion
  → Task completes (or never, if Infinite)
```

Each call to `RunAsync` creates a new animation run. To reuse an `Animation` instance, call `RunAsync` again — it clones internally.

---

## 2. Interpolation engine

Avalonia uses `PropertyMetadata.Interpolation` to determine how a property value is interpolated. Each `AvaloniaProperty` must have a registered interpolator for animation to work.

Built-in interpolators live in `Avalonia.Animation.Interpolators`:

```csharp
// Example: DoubleInterpolator
public class DoubleInterpolator : Interpolator<double>
{
    public override double Interpolate(double from, double to, double progress)
    {
        return from + (to - from) * progress;
    }
}
```

To make a custom property animatable, register an interpolator in metadata:

```csharp
public static readonly StyledProperty<double> MyCustomProperty =
    AvaloniaProperty.Register<MyControl, double>(
        "MyCustom",
        defaultValue: 0.0,
        inherits: false,
        BindingMode.Default,
        validate: null,
        enableDataValidation: false,
        interpolator: new DoubleInterpolator());
```

### What happens when multiple setters target the same property

```xml
<KeyFrame Cue="0%">
  <Setter Property="Opacity" Value="1" />
</KeyFrame>
<KeyFrame Cue="100%">
  <Setter Property="Opacity" Value="0" />
</KeyFrame>
```

At cue=0% opacity is 1, at cue=100% opacity is 0. At any point in between, the interpolator produces `1.0 - progress`. If two `KeyFrame` entries set the same property at the same cue, the last one wins.

---

## 3. Animation.GlobalPlaylist and multiple animations

When multiple animations target the same control concurrently, Avalonia resolves conflicts property-by-property. The last animation to touch a property wins.

For coordinating multiple sequenced animations:

```csharp
var fadeIn = (Animation)Resources["FadeIn"]!;
var pulse = (Animation)Resources["Pulse"]!;

await fadeIn.RunAsync(target, ct);
// fadeIn complete — start pulse
await pulse.RunAsync(target, ct);
```

Use `Task.WhenAll` for parallel animations targeting different properties:

```csharp
var fadeAnim = (Animation)Resources["FadeOut"]!;
var scaleAnim = (Animation)Resources["ScaleOut"]!;

await Task.WhenAll(
    fadeAnim.RunAsync(target, ct),
    scaleAnim.RunAsync(target, ct)
);
```

---

## 4. Animating attached properties

Use the attached property's fully qualified name in the `Setter`:

```xml
<Style Selector="Button">
  <Style.Animations>
    <Animation Duration="0:0:0.3">
      <KeyFrame Cue="100%">
        <Setter Property="Grid.Column" Value="1" />
      </KeyFrame>
    </Animation>
  </Style.Animations>
</Style>
```

In code:

```csharp
new KeyFrame
{
    Cue = new Cue(1.0),
    Setters =
    {
        new Setter
        {
            Property = Grid.ColumnProperty,
            Value = 1
        }
    }
};
```

Not all attached properties support animation — the property type must have a registered interpolator.

---

## 5. AnimationTracker

`AnimationTracker` provides event callbacks for animation lifecycle:

```csharp
var animation = new Animation { Duration = TimeSpan.FromSeconds(1) };
var tracker = new AnimationTracker(animation);

tracker.AnimationStarted += (_, _) => Debug.WriteLine("Started");
tracker.AnimationCompleted += (_, _) => Debug.WriteLine("Completed");
tracker.AnimationAborted += (_, _) => Debug.WriteLine("Aborted");

await tracker.RunAsync(target, ct);
```

Note: `AnimationTracker` is primarily used internally by the framework. For most scenarios, `await animation.RunAsync(...)` with proper `CancellationToken` management is sufficient.

---

## 6. Animation in DataTemplates

Animations defined inside a `DataTemplate` apply per-instance:

```xml
<DataTemplate DataType="viewmodels:ItemViewModel">
  <Border Background="Transparent">
    <Border.Styles>
      <Style Selector="Border">
        <Style.Animations>
          <Animation Duration="0:0:0.3" FillMode="Forward">
            <KeyFrame Cue="0%">
              <Setter Property="Opacity" Value="0" />
            </KeyFrame>
            <KeyFrame Cue="100%">
              <Setter Property="Opacity" Value="1" />
            </KeyFrame>
          </Animation>
        </Style.Animations>
      </Style>
    </Border.Styles>
  </Border>
</DataTemplate>
```

The animation runs once when each item is realized. Use `IterationCount="Infinite"` for persistent effects.

---

## 7. Animating TransformOperations via CSS shorthand

WPF-style `RotateTransform`, `ScaleTransform`, etc. cannot transition. Use the CSS shorthand string format for transitionable transforms:

```xml
<Border Background="Coral" Width="100" Height="100">
  <Border.Styles>
    <Style Selector="Border">
      <Setter Property="RenderTransform" Value="rotate(0)" />
    </Style>
    <Style Selector="Border:pointerover">
      <Setter Property="RenderTransform" Value="rotate(45deg)" />
    </Style>
  </Border.Styles>
  <Border.Transitions>
    <Transitions>
      <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.3" />
    </Transitions>
  </Border.Transitions>
</Border>
```

The `TransformOperationsTransition` supports: `translate`, `translateX`, `translateY`, `scale`, `scaleX`, `scaleY`, `skew`, `skewX`, `skewY`, `rotate`, `matrix`.

---

## 8. Animation locking and thread safety

`Animation.RunAsync` must be called from the UI thread. Composition animation calls (`StartAnimation`, `ImplicitAnimations`) must also be made on the UI thread even though they execute on the render thread.

```csharp
// ✓ Correct
await Dispatcher.UIThread.InvokeAsync(() =>
    animation.RunAsync(target, ct));

// ✓ Correct
Dispatcher.UIThread.Post(() =>
    visual.StartAnimation("Offset", anim));
```

---

## 9. Debugging animations

Use the **Animations** tab in Avalonia DevTools to inspect running animations:

- View active animations on the selected control
- See current playback state, duration, and elapsed time
- Pause or stop animations for debugging
- Inspect key frame values and current interpolated values

Enable animation debugging in code:

```csharp
#if DEBUG
    this.AttachDevTools();
#endif
```

---

## Common pitfalls

| Problem | Cause | Fix |
|---|---|---|
| Animation not running | Control not matching selector or `IsVisible` is false | Check selector path and visibility |
| Value snaps back after animation | `FillMode` is `None` | Set `FillMode="Forward"` |
| Cannot transition transform | Using WPF-style transform objects | Use CSS shorthand (`rotate(45deg)`) |
| Animation not appearing on hidden control | `PlaybackBehavior` defaults to `Normal` | Set `PlaybackBehavior="Always"` |
| `InvalidOperationException` | Property type has no interpolator | Use a supported property type or register one |
| Animation stutters | Animating layout-affecting properties | Switch to `RenderTransform` animation |
| Task never completes | `IterationCount="Infinite"` | Pass a `CancellationToken` with timeout |
