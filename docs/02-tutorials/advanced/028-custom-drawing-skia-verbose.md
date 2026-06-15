---
tier: advanced
topic: rendering
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 028-custom-drawing-skia.md
---

# 028V — Custom Drawing with Skia: An In-Depth Companion

This companion explains the rendering pipeline, resource lifetimes, and performance trade-offs behind every technique in the original tutorial. Read it alongside [028 — Custom Drawing with Skia](028-custom-drawing-skia.md).

---

## 1. DrawingContext — What It Is and Why It Exists

`DrawingContext` is Avalonia's immediate-mode drawing API. It wraps the platform's rendering backend (Skia on most platforms, Direct2D on older Windows, Metal on macOS) behind a unified set of `Draw*` methods.

### The `Render(DrawingContext)` contract

```csharp
public override void Render(DrawingContext context)
{
    context.DrawRectangle(Background, null, new Rect(Bounds.Size));
    // ... drawing commands
}
```

Avalonia calls `Render()` whenever the control needs to repaint. Triggers include:
- First appearance (`OnInitialized`)
- Property changes declared with `AffectsRender<T>(property)`
- Manual call to `InvalidateVisual()`
- Parent layout changes that affect the control's bounds

Inside `Render()`, you must **not**:
- Store `DrawingContext` references beyond the method scope.
- Access the `DrawingContext` from another thread.
- Call `InvalidateVisual()` (would cause infinite re-render).

### Why `AffectsRender<T>(BackgroundProperty)`?

```csharp
static WaveformControl()
{
    AffectsRender<WaveformControl>(BackgroundProperty);
}
```

`AffectsRender` tells Avalonia's property system: "When `BackgroundProperty` changes, call `InvalidateVisual()`." Without it, changing `Background` would not trigger a redraw — the control would still show the old background. This is analogous to WPF's `FrameworkPropertyMetadata.AffectsRender`.

### Resource creation inside Render — what to avoid

The original tutorial creates brushes and pens inside `Render()`:

```csharp
var fillBrush = new SolidColorBrush(Colors.CornflowerBlue);
var pen = new Pen(new SolidColorBrush(Colors.White), 2);
```

**This is a memory anti-pattern for performance-sensitive code.** Each `Render()` call allocates new brush and pen objects, which the garbage collector must later reclaim. For controls that render every frame (animations, real-time data), cache these resources as fields or static members:

```csharp
private static readonly SolidColorBrush _fillBrush = new(Colors.CornflowerBlue);
private static readonly Pen _pen = new(new SolidColorBrush(Colors.White), 2);
```

The original keeps them inline for brevity, not for production use.

### `FormattedText` — how text rendering works

```csharp
var formattedText = new FormattedText(
    "Avalonia",
    Typeface.Default,
    24,
    TextAlignment.Center,
    TextWrapping.NoWrap,
    Bounds.Size);
context.DrawText(formattedText, new Point(centerX - 40, centerY - 12));
```

`FormattedText` is a layout-and-render object. It:
1. Measures the text with the specified font, size, and wrapping constraints.
2. Positions glyphs according to the font's shaping rules.
3. Renders to the `DrawingContext` at the given position.

Important: `FormattedText` allocates a lot of memory internally (glyph runs, line info). Cache it when the text content and size are stable.

---

## 2. Geometry and Paths — When and Why

```csharp
var geo = new StreamGeometry();
using (var ctx = geo.Open())
{
    ctx.BeginFigure(new Point(150, 10), true);  // filled
    ctx.LineTo(new Point(200, 80));
    ctx.LineTo(new Point(100, 80));
    ctx.EndFigure(true);  // close figure
}
```

### `StreamGeometry` vs. `PathGeometry`

| Type | Allocation | Mutability | Reuse |
|---|---|---|---|
| `StreamGeometry` | Lightweight (stream-based) | Immutable after open | Cache and reuse |
| `PathGeometry` | Heavier | Mutable | Modify segments |

Use `StreamGeometry` for static geometry that does not change. It's more memory-efficient because it stores path data as a compact binary stream rather than a collection of segment objects.

### The figure lifecycle

```csharp
ctx.BeginFigure(new Point(150, 10), true);  // isFilled = true
ctx.LineTo(new Point(200, 80));
ctx.LineTo(new Point(100, 80));
ctx.EndFigure(true);  // isClosed = true
```

- `BeginFigure(startPoint, isFilled)`: Opens a new figure. `isFilled` determines whether the interior is filled (relevant when you pass a fill brush to `DrawGeometry`).
- `LineTo(endPoint)`: Adds a straight segment.
- `EndFigure(isClosed)`: `isClosed = true` draws a line back to the figure's start point. `isClosed = false` leaves the path open.

For curved paths, use `BezierTo`, `QuadraticBezierTo`, or `ArcTo` instead of `LineTo`.

### `DrawGeometry` — fill + stroke in one call

```csharp
context.DrawGeometry(
    new SolidColorBrush(Colors.Orange),    // fill (null = no fill)
    new Pen(...),                           // stroke (null = no stroke)
    geo);                                   // geometry
```

Pass `null` for the fill to create a wireframe, or `null` for the pen to create a filled shape with no outline.

---

## 3. Linear and Radial Gradients — How the Coordinate System Works

### LinearGradientBrush

```csharp
var linear = new LinearGradientBrush
{
    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
    GradientStops = new GradientStops
    {
        new() { Color = Colors.Purple, Offset = 0 },
        new() { Color = Colors.HotPink, Offset = 1 }
    }
};
```

- `RelativePoint(0, 0, RelativeUnit.Relative)` — the gradient starts at the top-left of the filled area.
- `RelativePoint(1, 1, RelativeUnit.Relative)` — the gradient ends at the bottom-right.
- `RelativeUnit.Relative` means coordinates are fractions of the bounding box. Use `RelativeUnit.Absolute` for pixel-specific direction.

`GradientStop.Offset` ranges from 0 (start of gradient vector) to 1 (end of gradient vector). Intermediate stops are interpolated linearly in the sRGB color space.

### RadialGradientBrush

```csharp
var radial = new RadialGradientBrush
{
    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
    Radius = 0.5,
    GradientStops = ...
};
```

- `Center` is the focal point (where color at offset 0 appears).
- `Radius` is the gradient spread as a fraction of the bounding box's smallest dimension (when using `RelativeUnit.Relative`).
- Colors radiate outward from `Center` to the circle defined by `Center + Radius`.

### Gradient allocation warning

Like brushes and pens, gradient brushes are objects that should be cached as fields when reused across frames. Creating them per-render defeats the GPU caching Avalonia may perform internally.

---

## 4. SkiaSharp Interop — Pixel-Level Control

### The problem SkiaSharp solves

`DrawingContext.Draw*` methods cover shapes, text, and images but not pixel-level operations like:
- Per-pixel filters and effects (blur, emboss, color matrix)
- Custom compositing (multiply, screen, overlay)
- Direct bitmap manipulation
- Procedural texture generation

SkiaSharp gives you `SKCanvas`, which exposes `SetPixel`, `DrawBitmap` with custom shaders, `SKPaint` with image filters, and direct pixel buffer access.

### How to actually get `SKCanvas` from Avalonia

The original tutorial shows off-screen rendering via `SKSurface.Create(...)`, then converting back to an Avalonia `Bitmap`. There is a simpler approach for controls that want to draw directly on-screen with Skia:

Use `SKCanvas` inside a custom `DrawingContext`-compatible approach by installing the `Avalonia.Skia` package (it ships with Avalonia by default on Skia-backed platforms) and working at the `SKCanvas` level through Avalonia's `DrawOperation` system. However, the off-screen approach in the tutorial is the most portable and recommended pattern.

### Step-by-step of the off-screen approach

```csharp
using var surface = SKSurface.Create(new SKImageInfo(200, 200));
var canvas = surface.Canvas;

// Skia-native draw calls
canvas.Clear(SKColors.Black);
using var paint = new SKPaint
{
    Color = SKColors.Cyan,
    IsAntialias = true,
    StrokeWidth = 3,
    Style = SKPaintStyle.Stroke
};
canvas.DrawCircle(100, 100, 80, paint);

// Convert to Avalonia bitmap and draw
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
using var stream = data.AsStream();
var avaloniaBitmap = new Avalonia.Media.Imaging.Bitmap(stream);
context.DrawImage(avaloniaBitmap, new Rect(0, 0, 200, 200));
```

1. `SKSurface.Create(...)` — creates an off-screen Skia render target. This is a GPU-backed or CPU-backed surface depending on the platform.
2. `surface.Canvas` — the Skia `Canvas` object. All Skia draw calls go here.
3. `paint` — Skia's equivalent to Avalonia's `Pen` + brush combined. `Style = Stroke` means draw outline only; `Fill` draws interior.
4. `surface.Snapshot()` — captures the canvas content as an `SKImage`.
5. `Encode(Png, 100)` — encodes to PNG byte data (quality 100 = lossless for PNG).
6. `new Bitmap(stream)` — Avalonia's bitmap constructor accepts any seekable stream containing a supported image format.
7. `context.DrawImage(...)` — draws the Avalonia bitmap onto the control's visual surface.

### Performance cost of this round-trip

Each frame: allocate `SKSurface` → draw → encode PNG → decode back to `Bitmap`. This is expensive (CPU-bound encode/decode). **Do not do this per-frame.** Instead:
- Render once to `SKSurface`, keep the `Bitmap` cached.
- Re-render only when the Skia content changes.
- Or use `WriteableBitmap` for direct pixel updates without encode/decode.

### When SkiaSharp is the right choice

- Custom chart/graph rendering with antialiased lines and text
- Image processing with color matrix filters
- Procedural textures (noise, plasma, fractals)
- PDF or print-pipeline preview rendering

---

## 5. RenderTargetBitmap — Off-Screen Rendering

```csharp
var rtb = new RenderTargetBitmap(new PixelSize(400, 300));

using (var ctx = rtb.CreateDrawingContext())
{
    ctx.Clear(Colors.White);
    ctx.DrawEllipse(...);
}

rtb.Save("output.png");
myImage.Source = rtb;
```

### What `RenderTargetBitmap` is

`RenderTargetBitmap` is a `Bitmap` subclass that you can draw onto using a `DrawingContext` (same API as `Control.Render`). It serves two purposes:
1. **Off-screen rendering**: Generate an image without displaying it first.
2. **Caching**: Pre-render complex visuals and store them as a bitmap.

### Why `CreateDrawingContext()` and not `Render()`

`RenderTargetBitmap.CreateDrawingContext()` returns a `DrawingContext` that writes directly into the bitmap's pixel buffer. This is the same `DrawingContext` type used in `Control.Render()`, but it writes to memory instead of screen.

### Resolution independence

`RenderTargetBitmap` is pixel-based, not DPI-aware. If you need resolution-independent output, use `DrawingContext` on a control and let Avalonia handle the scaling. For print-quality output, create a high-DPI `RenderTargetBitmap` (e.g., `new PixelSize(1200, 900)` for 300 DPI at 4" × 3").

### Saving to file

`rtb.Save("output.png")` saves to the filesystem in PNG format. The extension determines the format. Supported formats: PNG, JPEG, BMP, GIF.

### Memory management

`RenderTargetBitmap` holds a pixel buffer in memory. Dispose it (`rtb.Dispose()`) when no longer needed, or let it be garbage collected. For animations that generate frames, reuse the same `RenderTargetBitmap` rather than creating new ones.

---

## 6. Performance Considerations — Deeper Analysis

### The cost of `InvalidateVisual()`

Each `InvalidateVisual()` call schedules a new render pass for the next frame. The render pass:
1. Walks the visual tree to find the dirty region.
2. Calls `Render(DrawingContext)` on each dirty control.
3. Composites the results onto the screen.

Calling `InvalidateVisual()` more than once per frame collapses into one render pass (Avalonia coalesces invalidations). But calling it at 60 FPS means 60 render passes per second, which will consume CPU/GPU time even if nothing changed visually.

### AffectsRender — the automatic invalidation

```csharp
AffectsRender<WaveformControl>(BackgroundProperty);
```

This registers `BackgroundProperty` with the invalidation system: when the property changes, `InvalidateVisual()` is called automatically. Without this, you must manually call `InvalidateVisual()` in the property change handler.

### Brush and pen caching strategy

```csharp
// Bad — allocates every frame
public override void Render(DrawingContext context)
{
    var brush = new SolidColorBrush(Colors.Blue);  // 72 bytes +
    context.DrawRectangle(brush, null, ...);
}

// Good — allocated once
private static readonly ISolidColorBrush _brush = new SolidColorBrush(Colors.Blue);

public override void Render(DrawingContext context)
{
    context.DrawRectangle(_brush, null, ...);
}
```

Why this matters: `SolidColorBrush` implements `ISolidColorBrush` which extends `IBrush`. The rendering backend may cache GPU resources for each brush instance. Recreating brushes defeats that cache.

### `RenderOptions.BitmapScalingMode`

```
<Image RenderOptions.BitmapScalingMode="LinearQuality" />
```

Controls how bitmaps are sampled when scaled. Options:
- `Unspecified` — platform default (usually linear)
- `LinearQuality` — bilinear filtering, good quality
- `HighQuality` — bicubic filtering, best quality, slower
- `LowQuality` — nearest-neighbor, fastest, pixelated
- `Fant` — Fant (a weighted-average) resampling

Only applies to `Image` control and `DrawingContext.DrawImage` calls.

---

## See Also

- [028 — Custom Drawing with Skia (original)](028-custom-drawing-skia.md)
- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)
- [059 — Colors, Brushes, and FormattedText (plugin ref)](../references/59-media-colors-brushes-and-formatted-text-practical-usage.md)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)
- [Avalonia Docs: Custom Drawing](https://docs.avaloniaui.net/docs/concepts/custom-drawing)
- [028X — Custom Drawing with Skia (examples)](028-custom-drawing-skia-examples.md)
