---
tier: advanced
topic: rendering
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 082 — Graphics & Drawing Reference

**What you'll learn:** The full Avalonia graphics stack — `DrawingContext` API, brush and geometry types, shape controls, effects, clipping, off-screen rendering, and SkiaSharp interop.

**Prerequisites:** [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)

---

## 1. DrawingContext API

`DrawingContext` is the central class for custom rendering in a control's `Render` override.

### Drawing operations

| Method | Description |
|---|---|
| `DrawRectangle(IBrush?, IPen?, Rect, rx, ry)` | Filled and/or stroked rectangle |
| `DrawEllipse(IBrush?, IPen?, Point, rx, ry)` | Ellipse at center with radii |
| `DrawLine(IPen, Point, Point)` | Straight line |
| `DrawGeometry(IBrush?, IPen?, Geometry)` | Any geometry object |
| `DrawText(FormattedText, Point)` | Formatted text at a position |
| `DrawImage(IImage, Rect src, Rect dest)` | Bitmap stretch from source rect to dest rect |
| `DrawGlyphRun(IBrush, GlyphRun)` | Pre-shaped text glyphs |
| `Custom(ICustomDrawOperation)` | Custom SkiaSharp drawing operation |

### State push operations

Each returns `IDisposable` — dispose to restore the previous state:

```csharp
using (context.PushClip(new RoundedRect(rect, 8)))
using (context.PushOpacity(0.5))
using (context.PushTransform(Matrix.CreateRotation(0.2)))
using (context.PushBrushRenderTransform(Matrix.CreateTranslation(10, 0)))
{
    // Nested draws use the pushed state
    context.DrawRectangle(Brushes.Blue, null, rect);
}
```

| Method | Effect |
|---|---|
| `PushClip(RoundedRect)` | Restrict drawing to a region |
| `PushClip(Geometry)` | Restrict drawing to a geometry outline |
| `PushOpacity(double)` | Apply uniform opacity to subsequent draws |
| `PushTransform(Matrix)` | Apply a transformation matrix |
| `PushBrushRenderTransform(Matrix)` | Transform brush coordinates only |
| `PushSetValue(StackingProperty, object?)` | Set a per-draw custom property |

---

## 2. Brush types

| Brush | Use case |
|---|---|
| `SolidColorBrush` | Single solid color (most common) |
| `LinearGradientBrush` | Gradient along a line |
| `RadialGradientBrush` | Gradient radiating from a center point |
| `ConicGradientBrush` | Gradient sweeping around a center point |
| `ImageBrush` | Tile or stretch a bitmap |
| `VisualBrush` | Render another visual element as a brush pattern |

### Color formats

```xml
<Border Background="SteelBlue" />
<Border Background="#4682B4" />
<Border Background="#804682B4" />        <!-- ARGB -->
<Border Background="rgb(70, 130, 180)" />
<Border Background="rgba(70,130,180,0.8)" />
<Border Background="hsl(207, 44%, 49%)" />
<Border Background="hsv(207, 61%, 71%)" />
```

```csharp
var brush = new SolidColorBrush(Colors.SteelBlue);
var brush2 = Brush.Parse("rgba(70, 130, 180, 0.8)");
```

### Gradient directions

```xml
<LinearGradientBrush StartPoint="0%,50%" EndPoint="100%,50%">  <!-- horizontal -->
<LinearGradientBrush StartPoint="50%,0%" EndPoint="50%,100%">  <!-- vertical -->
<LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%"> <!-- diagonal -->
```

`SpreadMethod` controls behavior beyond gradient bounds: `Pad` (default), `Reflect`, `Repeat`.

### ConicGradientBrush

```xml
<ConicGradientBrush Center="50%,50%" Angle="0">
  <GradientStop Color="#EF4444" Offset="0" />
  <GradientStop Color="#22C55E" Offset="0.5" />
  <GradientStop Color="#EF4444" Offset="1" />
</ConicGradientBrush>
```

### ImageBrush tiling

```xml
<ImageBrush Source="avares://MyApp/texture.png"
            Stretch="UniformToFill"
            TileMode="Tile"
            DestinationRect="0,0,32,32" />
```

`TileMode`: `None`, `Tile`, `FlipX`, `FlipY`, `FlipXY`.

### VisualBrush

```xml
<VisualBrush Stretch="Uniform" TileMode="None">
  <VisualBrush.Visual>
    <TextBlock Text="Avalonia" FontSize="14" Foreground="LightGray" />
  </VisualBrush.Visual>
</VisualBrush>
```

---

## 3. Geometry types

| Geometry | Description |
|---|---|
| `RectangleGeometry` | Bounding rectangle |
| `EllipseGeometry` | Ellipse defined by center and radii |
| `LineGeometry` | Single line segment |
| `PathGeometry` | Composed of figures and segments |
| `CombinedGeometry` | Set operation (Union, Intersect, Exclude, Xor) on two geometries |
| `GeometryGroup` | Groups multiple geometries with a `FillRule` |
| `StreamGeometry` | Lightweight immutable geometry (best for static shapes) |

### Segment types

| Segment | Description |
|---|---|
| `LineSegment` | Straight line to a point |
| `ArcSegment` | Elliptical arc (size, rotation, sweep direction) |
| `BezierSegment` | Cubic Bezier (two control points + endpoint) |
| `QuadraticBezierSegment` | Quadratic Bezier (one control point + endpoint) |
| `PolyLineSegment` | Series of connected lines |
| `PolyBezierSegment` | Series of connected cubic Bezier curves |

### CombinedGeometry modes

```xml
<CombinedGeometry GeometryCombineMode="Exclude">
  <!-- Union, Intersect, Exclude, Xor -->
</CombinedGeometry>
```

### Path mini-language (SVG style)

```
M x,y     Move to         L x,y     Line to
H x       Horizontal line  V y       Vertical line
C x1,y1 x2,y2 x,y          Cubic Bezier
Q x1,y1 x,y                 Quadratic Bezier
A rx,ry rot large sweep x,y Arc
S x2,y2 x,y                 Smooth cubic Bezier
T x,y                       Smooth quadratic Bezier
Z                           Close path
```

Uppercase = absolute; lowercase = relative.

```xml
<Path Data="M 50,0 L 100,100 L 0,100 Z" Fill="Gold" />
```

### StreamGeometry (code)

```csharp
var geometry = new StreamGeometry();
using (var ctx = geometry.Open())
{
    ctx.BeginFigure(new Point(10, 50), isFilled: true);
    ctx.LineTo(new Point(100, 50));
    ctx.ArcTo(new Point(100, 150), new Size(50, 50),
              rotationAngle: 0, isLargeArc: false,
              SweepDirection.Clockwise);
    ctx.LineTo(new Point(10, 150));
    ctx.EndFigure(isClosed: true);
}
```

### Geometry utility methods

| Method | Purpose |
|---|---|
| `FillContains(Point)` | Hit test inside the filled area |
| `StrokeContains(Pen, Point)` | Hit test on the stroke |
| `GetWidenedGeometry(Pen)` | Returns the stroke outline as a geometry |
| `GetFlattenedPathGeometry()` | Approximates curves as line segments |

---

## 4. Shape controls

| Shape | Description |
|---|---|
| `Rectangle` | Rounded rect via `RadiusX`/`RadiusY` |
| `Ellipse` | Circle when Width == Height |
| `Line` | Straight line from StartPoint to EndPoint |
| `Polyline` | Connected line segments (not closed) |
| `Polygon` | Closed shape from points |
| `Path` | Geometry-defined shape (most flexible) |

### Common shape properties

| Property | Values |
|---|---|
| `Fill` | Brush for the interior |
| `Stroke` | Brush for the outline |
| `StrokeThickness` | Outline width |
| `StrokeDashArray` | Dash pattern e.g. `5,3` |
| `StrokeDashOffset` | Dash offset |
| `StrokeLineCap` | `Flat`, `Round`, `Square` |
| `StrokeLineJoin` | `Miter`, `Bevel`, `Round` |
| `StrokeMiterLimit` | Ratio limit for miter-to-bevel switch |
| `Stretch` | `None`, `Fill`, `Uniform`, `UniformToFill` |

---

## 5. Effects

### BoxShadow (Border only)

```
offsetX offsetY blur spread color
```

```xml
<Border BoxShadow="inset 0 2 4 0 #40000000, 0 8 16 0 #10000000" />
```

### Effect properties (any Visual)

```xml
<Border.Effect>
  <BlurEffect Radius="10" />
</Border.Effect>
```

```xml
<TextBlock.Effect>
  <DropShadowEffect OffsetX="3" OffsetY="3" BlurRadius="5"
                    Color="Black" Opacity="0.5" />
</TextBlock.Effect>
```

```xml
<Border.Effect>
  <DropShadowDirectionEffect ShadowDepth="5" Direction="315"
                              BlurRadius="10" />
</Border.Effect>
```

---

## 6. Clipping and OpacityMask

```xml
<!-- Clip to bounds -->
<Border ClipToBounds="True" CornerRadius="50">
  <Image Source="avares://MyApp/photo.png" Stretch="UniformToFill" />
</Border>

<!-- Custom clip geometry -->
<Image.Clip>
  <EllipseGeometry Rect="0,0,200,200" />
</Image.Clip>

<!-- Per-pixel opacity mask -->
<Image.OpacityMask>
  <LinearGradientBrush StartPoint="0%,0%" EndPoint="0%,100%">
    <GradientStop Color="Black" Offset="0" />
    <GradientStop Color="Transparent" Offset="1" />
  </LinearGradientBrush>
</Image.OpacityMask>
```

Only the alpha channel of the mask is used. Black = fully visible, Transparent = hidden.

---

## 7. RenderTargetBitmap (off-screen rendering)

```csharp
var pixelSize = new PixelSize((int)control.Bounds.Width,
                               (int)control.Bounds.Height);
var renderTarget = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
renderTarget.Render(control);
renderTarget.Save("output.png");
```

Requires the control to be attached to a visual tree. Use the headless platform for server-side rendering.

---

## 8. ICustomDrawOperation (SkiaSharp interop)

```csharp
public override void Render(DrawingContext context)
{
    context.Custom(new MyDrawOp(new Rect(Bounds.Size)));
}

private class MyDrawOp : ICustomDrawOperation
{
    public Rect Bounds { get; }
    public MyDrawOp(Rect bounds) => Bounds = bounds;

    public void Render(ImmediateDrawingContext context)
    {
        var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (feature is null) return;
        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;
        // Full SkiaSharp API here
    }

    public bool HitTest(Point p) => Bounds.Contains(p);
    public bool Equals(ICustomDrawOperation? other) => false;
    public void Dispose() { }
}
```

Requires `Avalonia.Skia` and `SkiaSharp` packages.

---

## 9. Performance guidelines

| Practice | Why |
|---|---|
| Reuse `Pen`/`Brush` objects as fields | Avoids allocating GDI/Skia resources per frame |
| Use `AffectsRender` to auto-invalidate | Prevents unnecessary redraws |
| Prefer `BoxShadow` over `DropShadowEffect` on Border controls | Hardware-accelerated on Border |
| Use `ClipToBounds` over `OpacityMask` for simple rect clipping | Lower overhead |
| Use `StreamGeometry` for static icon paths | Lightweight, immutable, cacheable |
| Avoid allocations in `Render` | GC pressure causes frame drops |
| Use `ICustomDrawOperation` only when SkiaSharp control is needed | Bypasses scene graph caching |

---

## Key Takeaways

- `DrawingContext` provides `Draw*` and `Push*` methods for custom rendering in `Render` overrides
- Six brush types: `SolidColorBrush`, `LinearGradientBrush`, `RadialGradientBrush`, `ConicGradientBrush`, `ImageBrush`, `VisualBrush`
- Geometry types range from simple (`RectangleGeometry`) to complex (`PathGeometry`), with the SVG-style path mini-language for compact shape definitions
- Effects: `BoxShadow`, `BlurEffect`, `DropShadowEffect`, `DropShadowDirectionEffect`
- Clipping via `ClipToBounds`, `Clip` (geometry), and `OpacityMask` (brush alpha channel)
- `RenderTargetBitmap` captures control output to a bitmap; `ICustomDrawOperation` provides direct SkiaSharp canvas access

---

## See Also

- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md) — compositor rendering model
- [024 — Animation & Transitions](024-animation-transitions.md) — animating visual properties
- [028 — Custom Drawing with Skia](028-custom-drawing-skia.md) — deeper SkiaSharp patterns
- [Avalonia Docs: Drawing Graphics](https://docs.avaloniaui.net/docs/graphics-animation/drawing-graphics)
