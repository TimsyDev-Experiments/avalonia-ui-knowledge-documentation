---
tier: advanced
topic: rendering
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 028-custom-drawing-skia.md
---

# Quiz — Custom Drawing with Skia

```quiz
Q: Which method do you override to implement custom drawing in an Avalonia control?
A. OnDraw(DrawingContext) || No such override exists — the correct method is Render.
B. Paint(DrawingContext) || This is the WinForms/WPF pattern — Avalonia uses Render.
C. Render(DrawingContext) (correct) || Render is the standard override point for custom drawing in Avalonia controls.
D. Draw(DrawingContext) || No such override exists — Avalonia uses Render for per-frame drawing.
Explanation: Custom drawing in Avalonia is done by overriding the Render(DrawingContext) method on a Control subclass.
```

```quiz
Q: What is the recommended way to create a reusable geometric shape (e.g., a triangle) in a custom control's Render method?
A. DrawPolyline with a list of points || DrawPolyline exists but StreamGeometry is the standard approach for reusable complex paths.
B. StreamGeometry opened via Open() and drawn with DrawGeometry (correct) || StreamGeometry allows building a custom path with BeginFigure/LineTo/EndFigure, then drawing it with DrawGeometry.
C. Directly call DrawLine three times || This works but is not reusable and does not support filling or closing the shape cleanly.
D. Use a SkiaSharp SKPath and convert to a bitmap || Overkill for simple shapes — StreamGeometry is the idiomatic Avalonia approach.
Explanation: StreamGeometry.BeginFigure/LineTo/EndFigure defines a reusable path that can be drawn via DrawGeometry with fill and stroke.
```

```quiz
Q: How do you integrate SkiaSharp pixel-level drawing into an Avalonia custom control?
A. Override OnRender(SKCanvas) instead of Render(DrawingContext) || There is no such override — Avalonia controls always use Render(DrawingContext).
B. Render to an SKSurface, snapshot to an SKImage, encode to PNG, load as Bitmap, then draw via DrawingContext (correct) || This is the documented interop flow: SkiaSharp surface -> snapshot -> encode -> stream -> Bitmap -> DrawImage.
C. Cast DrawingContext to SKCanvas and draw directly || DrawingContext does not expose the underlying SKCanvas directly in the public API.
D. Set the control's RenderMode to Skia and use SKCanvas from a static accessor || There is no RenderMode property for this purpose.
Explanation: The pattern is to create an SKSurface, draw with Skia APIs, snapshot, encode to PNG, load as Avalonia Bitmap, then call context.DrawImage.
```

```quiz
Q: What is the primary use case for RenderTargetBitmap?
A. Per-frame animation in a custom control || RenderTargetBitmap is moderate performance and best for one-time renders, not per-frame.
B. Off-screen rendering of static or cached visual content (correct) || RenderTargetBitmap renders content once to a bitmap that can be saved or reused as an image source.
C. Replacing the DrawingContext in Render() || The bitmap's drawing context is separate from the control's Render pass — it is for off-screen work.
D. Real-time video frame processing || RenderTargetBitmap is not designed for real-time video — it is for static pre-rendered content.
Explanation: RenderTargetBitmap creates an off-screen render target for one-time or cached rendering, useful for pre-composing static content.
```

```quiz
Q: Which performance practice is most important when implementing a custom control's Render method?
A. Call InvalidateVisual() every frame to ensure smooth animation || Calling InvalidateVisual() too often can degrade performance — at most once per frame is the guideline.
B. Create new SolidColorBrush and Pen objects on each render call || Objects should be cached and reused to avoid allocation pressure during rendering.
C. Cache brushes and pens as fields and reuse them across render calls (correct) || Reusing drawing resources avoids GC pressure and improves rendering throughput.
D. Use SkiaSharp for all drawing operations || SkiaSharp has its place but DrawingContext is already GPU-accelerated — use the right tool per need.
Explanation: Creating brushes and pens per render causes unnecessary allocation — cache them as control fields and reuse them.
```
