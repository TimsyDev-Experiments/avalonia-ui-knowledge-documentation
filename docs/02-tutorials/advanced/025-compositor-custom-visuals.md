---
tier: advanced
topic: rendering
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 025 — Compositor & Custom Visuals

**What you'll learn:** Use the compositor for GPU-accelerated drawing, create custom `Visual` nodes, and build compositor animations.

**Prerequisites:** [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)

---

## 1. When to use the compositor

The compositor runs on a separate render thread and handles:
- Hit testing
- Transform operations (scale, rotate, translate)
- Opacity
- Clip regions
- Render bounds

Use it when `Render(DrawingContext)` overhead becomes a bottleneck — typically with hundreds of frequently-updating visual elements.

---

## 2. Custom visual node

```csharp
// Controls/Particle.cs
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace MyApp.Controls;

public class Particle : Visual
{
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<Particle, Color>(nameof(Color), Colors.White);

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public Point Position { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double Radius { get; set; } = 4;

    public override void Render(DrawingContext context)
    {
        var brush = new SolidColorBrush(Color);
        context.DrawEllipse(brush, null, Position, Radius, Radius);
    }
}
```

---

## 3. Composing visuals into a scene

```csharp
// Controls/ParticleSystem.cs
public class ParticleSystem : Control
{
    private readonly List<Particle> _particles = new();
    private readonly Compositor _compositor;

    public ParticleSystem()
    {
        _compositor = AvaloniaLocator.Current
            .GetService<Compositor>()!;

        // Create and add particles
        for (int i = 0; i < 100; i++)
        {
            var p = new Particle
            {
                Position = new Point(Random.Shared.Next(400), Random.Shared.Next(400)),
                VelocityX = Random.Shared.NextDouble() * 2 - 1,
                VelocityY = Random.Shared.NextDouble() * 2 - 1,
                Radius = Random.Shared.Next(2, 6),
                Color = Colors.Cyan
            };
            _particles.Add(p);
            VisualChildren.Add(p);
        }
    }
```

---

## 4. Compositor animations (transform only)

```csharp
using Avalonia.Animation;
using Avalonia.Animation.Compositor;

// Create a compositor-only animation (no layout involvement)
var compositorAnimation = new CompositorAnimation
{
    Duration = TimeSpan.FromSeconds(1),
    IterationCount = IterationCount.Infinite,
    AnimationMode = AnimationMode.Compositor
};

// Apply to a visual's render transform
compositorAnimation.Children.Add(new CompositorKeyFrame
{
    Cue = new Cue(0.0),
    Value = TransformOperations.Parse("rotate(0deg)")
});

compositorAnimation.Children.Add(new CompositorKeyFrame
{
    Cue = new Cue(1.0),
    Value = TransformOperations.Parse("rotate(360deg)")
});

await compositorAnimation.RunAsync(myVisual);
```

Compositor animations run entirely on the render thread — zero impact on UI thread responsiveness.

---

## 5. Manual compositor updates (frame callback)

```csharp
// Subscribe to the compositor's frame tick
Compositor?.RequestCompositionUpdate(OnCompositionUpdate);

private void OnCompositionUpdate(Compositor compositor, CompositionUpdateEventArgs args)
{
    var now = args.Time.TotalSeconds;

    foreach (var particle in _particles)
    {
        particle.Position = particle.Position with
        {
            X = particle.Position.X + particle.VelocityX,
            Y = particle.Position.Y + particle.VelocityY
        };

        // Bounce off edges
        if (particle.Position.X < 0 || particle.Position.X > Bounds.Width)
            particle.VelocityX *= -1;

        if (particle.Position.Y < 0 || particle.Position.Y > Bounds.Height)
            particle.VelocityY *= -1;

        particle.InvalidateVisual();
    }
}
```

---

## 6. Custom drawing with IDrawingContext

For advanced scenarios where `Render` isn't enough:

```csharp
// Access the drawing context from a visual
var drawingContext = (self as Visual)?.RenderOpen();

// Or create a rendering off-screen via RenderTargetBitmap
var bitmap = new RenderTargetBitmap(new PixelSize(200, 200));
using (var ctx = bitmap.CreateDrawingContext())
{
    ctx.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 200, 200));
}
```

---

## Key Takeaways

- The compositor handles transforms, opacity, and clipping off the UI thread
- `CompositorAnimation` for GPU-accelerated transform animations
- Custom `Visual` subclasses for lightweight renderable nodes
- `RequestCompositionUpdate` for per-frame update callbacks
- Use compositor path when you need many (100+) independently moving elements

---

## See Also

- [024 — Animation & Transitions](024-animation-transitions.md)
- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md) — the compositor rendering model in depth
- [025E — Compositor & Custom Visuals (examples)](025-compositor-custom-visuals-examples.md)
- [025V — Compositor & Custom Visuals (verbose companion)](025-compositor-custom-visuals-verbose.md)
- [Avalonia Docs: Compositor](https://docs.avaloniaui.net/docs/concepts/compositor)
