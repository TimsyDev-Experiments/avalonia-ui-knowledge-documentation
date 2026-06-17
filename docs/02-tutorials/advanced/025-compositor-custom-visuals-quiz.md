---
tier: advanced
topic: rendering
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 025-compositor-custom-visuals.md
---

# Quiz — Compositor & Custom Visuals

```quiz
Q: When should you move rendering from Render(DrawingContext) to the compositor?
A. Whenever you need to draw a shape with a gradient brush. || Gradients work fine in standard Render; the compositor is not needed for simple shapes.
B. When you have hundreds of frequently updating visual elements and Render becomes a throughput bottleneck. (correct) || The tutorial recommends the compositor "when Render(DrawingContext) overhead becomes a bottleneck — typically with hundreds of frequently-updating visual elements."
C. When you need text rendering with custom fonts. || Text is drawn through DrawingContext; the compositor does not provide a separate text API.
D. When the control needs to support data binding. || Data binding works at the AvaloniaObject level and is independent of the rendering path.
```

```quiz
Q: Which base class should a lightweight renderable node extend for compositor-based particle systems?
A. Control || Control includes layout (Measure/Arrange) and styled property overhead that a lightweight compositor node does not need.
B. Visual (correct) || Visual is the lightest base class with a Render override and no layout or template system, ideal for hundreds of independent particles.
C. TemplatedControl || TemplatedControl has template expansion and layout logic that adds unnecessary weight.
D. Decorator || Decorator wraps exactly one child and includes layout delegation; it is not designed for lightweight renderable nodes.
```

```quiz
Q: What is the key benefit of CompositorAnimation over a regular Animation?
A. CompositorAnimation can animate any property without keyframes. || CompositorAnimation still uses keyframes and is limited to render-thread-safe properties (transforms, opacity).
B. CompositorAnimation runs entirely on the render thread with zero UI thread impact. (correct) || The tutorial states: "Compositor animations run entirely on the render thread — zero impact on UI thread responsiveness."
C. CompositorAnimation supports longer maximum durations. || Duration limits are the same regardless of the animation execution path.
D. CompositorAnimation can be serialized to JSON. || Serialization is unrelated; standard Animation also supports serialization via XAML.
```

```quiz
Q: What does RequestCompositionUpdate do?
A. It forces an immediate synchronous re-render of the compositor scene. || The call schedules a callback for the next frame; it does not block.
B. It registers a per-frame callback for manual update logic such as particle position integration. (correct) || The callback receives CompositionUpdateEventArgs with timing data, allowing per-frame state updates like the bounce physics in the tutorial.
C. It increments the compositor's internal frame counter. || It schedules user code, not counter management.
D. It resizes the compositor's internal render target to match the control bounds. || Resizing happens automatically; RequestCompositionUpdate is for custom frame logic.
```

```quiz
Q: How do you resolve the Compositor service in Avalonia 12?
A. By injecting ICompositor into the constructor via DI. || There is no ICompositor interface; the compositor is a concrete class resolved through AvaloniaLocator.
B. Through AvaloniaLocator.Current.GetService<Compositor>(). (correct) || The tutorial shows: _compositor = AvaloniaLocator.Current.GetService<Compositor>()!;
C. By calling Application.Current.Compositor. || Application does not expose a Compositor property.
D. By referencing Compositor.Default static property. || Compositor has no Default static; the current compositor depends on the platform rendering subsystem.
```
