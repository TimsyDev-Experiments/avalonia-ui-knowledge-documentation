---
tier: advanced
topic: rendering
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 082V — Graphics & Drawing Reference (verbose companion)

**What this covers:** DrawingContext internals, brush transform details, geometry construction edge cases, text rendering with TextLayout, bitmap format options, and SkiaSharp custom draw operation patterns.

**Prerequisites:** 082 — Graphics & Drawing Reference core

---

## 1. DrawingContext push/pop internals

Each `Push*` method pushes a new state onto a stack. Dispose pops it:

```csharp
public override void Render(DrawingContext context)
{
    // State A (default)
    context.DrawRectangle(Brushes.Gray, null, new Rect(0, 0, 100, 100));

    using (context.PushClip(new Rect(10, 10, 80, 80)))  // Stack depth 1
    using (context.PushOpacity(0.5))                     // Stack depth 2
    {
        // Draws are clipped AND half-opaque
        context.DrawRectangle(Brushes.Red, null, new Rect(0, 0, 100, 100));
    }  // ← Pop opacity, back to depth 1

    // Draws are clipped but fully opaque
    context.DrawRectangle(Brushes.Blue, null, new Rect(20, 20, 60, 60));
}  // ← Pop clip, back to state A
```

The stack is limited only by available memory. Deep nesting may hurt performance.

### PushSetValue for custom per-draw state

```csharp
public static readonly StackingProperty<Color> TintProperty =
    AvaloniaProperty.RegisterStacking<MyControl, Color>("Tint");

using (context.PushSetValue(TintProperty, Colors.Red))
{
    // Custom drawing logic can read the property value
    var tint = context.GetValue(TintProperty);
}
```

---

## 2. Brush transforms

Brushes can be transformed independently of the control:

```csharp
var brush = new LinearGradientBrush
{
    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
    EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
    GradientStops =
    {
        new GradientStop(Colors.Blue, 0),
        new GradientStop(Colors.Red, 1)
    },
    Transform = new RotateTransform(45).Value  // Rotate the gradient
};
```

### RelativePoint and RelativeUnit

| Unit | Behavior |
|---|---|
| `RelativeUnit.Relative` | Coordinates are fractions of the element size (0–1) |
| `RelativeUnit.Absolute` | Coordinates are device-independent pixels |

---

## 3. Geometry construction edge cases

### ArcSegment parameters

```csharp
new ArcSegment
{
    Point = new Point(100, 150),          // End point
    Size = new Size(50, 50),              // Ellipse radii
    RotationAngle = 0,                    // Ellipse rotation in degrees
    IsLargeArc = false,                   // true = larger arc (>180°)
    SweepDirection = SweepDirection.Clockwise
};
```

The same start → end pair with the same radii can produce four different arcs based on `IsLargeArc` and `SweepDirection`.

### CombinedGeometry with non-overlapping shapes

When geometries don't overlap:
- `Union`: retains both shapes
- `Intersect`: produces empty geometry
- `Exclude`: returns the first geometry unchanged
- `Xor`: returns both shapes (same as Union for non-overlapping)

### FillRule behavior

| Rule | Behavior |
|---|---|
| `EvenOdd` | Alternates fill for overlapping regions (creates holes) |
| `NonZero` | Fills all enclosed regions based on winding direction |

```xml
<GeometryGroup FillRule="EvenOdd">
  <EllipseGeometry Center="50,50" RadiusX="50" RadiusY="50" />
  <EllipseGeometry Center="50,50" RadiusX="25" RadiusY="25" />
</GeometryGroup>
<!-- EvenOdd produces a donut; NonZero fills the inner circle -->
```

---

## 4. Text rendering with TextLayout

`TextLayout` offers more control than `FormattedText`:

```csharp
var layout = new TextLayout(
    "Multi-line text with wrapping and justification.",
    new Typeface("Segoe UI"),
    16,
    Brushes.Black,
    textAlignment: TextAlignment.Justify,
    maxWidth: 200,
    maxHeight: 300,
    lineHeight: 22,
    letterSpacing: 0.5);

// Access per-line metrics
foreach (var line in layout.TextLines)
{
    // line.Height, line.Width, line.Start, line.Baseline
}

// Draw
layout.Draw(context, new Point(10, 10));
```

`TextLayout` supports `TextAlignment.Justify` while `FormattedText` does not.

---

## 5. Bitmap formats and encoding

```csharp
var bitmap = new RenderTargetBitmap(
    new PixelSize(800, 600),
    new Vector(96, 96));        // DPI

bitmap.Render(myControl);

// Save as PNG (default)
bitmap.Save("output.png");

// Save with custom format via SkiaSharp
using var skBitmap = bitmap.GetSnapshot();
using var data = skBitmap.Encode(SKEncodedImageFormat.Jpeg, 85);
using var stream = File.OpenWrite("output.jpg");
data.SaveTo(stream);
```

### Bitmap from asset

```csharp
var uri = new Uri("avares://MyApp/Assets/photo.png");
var bitmap = new Bitmap(AssetLoader.Open(uri));
```

---

## 6. ICustomDrawOperation patterns

### Caching draw data across frames

```csharp
private class CachedChartOp : ICustomDrawOperation
{
    private readonly float[] _data;
    private readonly SKPaint _paint;

    public CachedChartOp(float[] data)
    {
        _data = data;
        _paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            Color = SKColors.DodgerBlue
        };
    }

    public Rect Bounds { get; set; }

    public void Render(ImmediateDrawingContext context)
    {
        var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (feature is null) return;
        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;

        using var path = new SKPath();
        path.MoveTo(0, (float)Bounds.Height);
        for (int i = 1; i < _data.Length; i++)
        {
            float x = (float)Bounds.Width * i / (_data.Length - 1);
            float y = (float)Bounds.Height * (1 - _data[i]);
            path.LineTo(x, y);
        }
        canvas.DrawPath(path, _paint);
    }

    public bool HitTest(Point p) => Bounds.Contains(p);
    public bool Equals(ICustomDrawOperation? other) =>
        other is CachedChartOp op && op._data == _data;
    public void Dispose() => _paint.Dispose();
}
```

### When to implement Equals

`Equals` lets the scene graph skip re-rendering when the draw operation hasn't changed. Return `false` to always re-render, or compare your data fields to enable caching.

---

## 7. BoxShadow performance and limitations

`BoxShadow` applies only to `Border` and `ContentPresenter`. For other controls, use `DropShadowEffect` (slower but applies everywhere).

```
Performance: BoxShadow > Effect (DropShadowEffect/BlurEffect)
```

`BoxShadow` with multiple layers is composited in a single pass. Combining more than ~4 shadows may impact performance.

---

## 8. Common pitfalls

| Problem | Cause | Fix |
|---|---|---|
| Render not called | Forgot `InvalidateVisual()` or `AffectsRender` | Register via `AffectsRender<T>(prop)` |
| Clip has no effect | `ClipToBounds` not set | Set `ClipToBounds="True"` or assign `Clip` geometry |
| Image not loaded | Wrong URI scheme | Use `avares://AssemblyName/Path/file.png` |
| Brush not updating | Mutable brush not recreated on property change | Create new brush or use binding |
| SkiaSharp draw not visible | `ICustomDrawOperation.Equals` always returns `true` | Return `false` or compare data properly |
| Text blurred | DPI mismatch | Use `TextLayout` for exact positioning |
| Gradient looks wrong | Coordinates use absolute by default | Use `RelativePoint` with `RelativeUnit.Relative` for 0%-100% behavior |
