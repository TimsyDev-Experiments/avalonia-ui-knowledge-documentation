---
tier: advanced
topic: rendering
estimated: 12 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 028 — Custom Drawing with Skia

**What you'll learn:** Use `DrawingContext` for vector rendering, SkiaSharp for pixel-level control, and `RenderTargetBitmap` for off-screen drawing.

**Prerequisites:** [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)

---

## 1. DrawingContext basics

Avalonia's `Render(DrawingContext)` method gives you a hardware-accelerated drawing surface:

```csharp
public class WaveformControl : Control
{
    static WaveformControl()
    {
        AffectsRender<WaveformControl>(BackgroundProperty);
    }

    public override void Render(DrawingContext context)
    {
        // Clear with background
        context.DrawRectangle(Background, null, new Rect(Bounds.Size));

        var centerX = Bounds.Width / 2;
        var centerY = Bounds.Height / 2;

        // Circle
        var fillBrush = new SolidColorBrush(Colors.CornflowerBlue);
        context.DrawEllipse(fillBrush, null, new Point(centerX, centerY), 50, 50);

        // Line
        var pen = new Pen(new SolidColorBrush(Colors.White), 2);
        context.DrawLine(pen, new Point(0, centerY), new Point(Bounds.Width, centerY));

        // Text
        var formattedText = new FormattedText(
            "Avalonia",
            Typeface.Default,
            24,
            TextAlignment.Center,
            TextWrapping.NoWrap,
            Bounds.Size);
        context.DrawText(formattedText, new Point(centerX - 40, centerY - 12));
    }
}
```

---

## 2. Geometry and paths

```csharp
public override void Render(DrawingContext context)
{
    // Rounded rectangle
    var rect = new Rect(10, 10, 100, 60);
    context.DrawRectangle(
        new SolidColorBrush(Colors.Green),
        new Pen(new SolidColorBrush(Colors.DarkGreen), 2),
        rect,
        8, 8);  // corner radius

    // Custom path
    var geo = new StreamGeometry();
    using (var ctx = geo.Open())
    {
        ctx.BeginFigure(new Point(150, 10), true);  // filled
        ctx.LineTo(new Point(200, 80));
        ctx.LineTo(new Point(100, 80));
        ctx.EndFigure(true);  // close figure
    }
    context.DrawGeometry(
        new SolidColorBrush(Colors.Orange),
        new Pen(new SolidColorBrush(Colors.DarkOrange), 2),
        geo);
}
```

---

## 3. Linear and radial gradients

```csharp
public override void Render(DrawingContext context)
{
    // Linear gradient
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
    context.DrawRectangle(linear, null, new Rect(0, 0, 200, 100));

    // Radial gradient
    var radial = new RadialGradientBrush
    {
        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        Radius = 0.5,
        GradientStops = new GradientStops
        {
            new() { Color = Colors.Yellow, Offset = 0 },
            new() { Color = Colors.Red, Offset = 1 }
        }
    };
    context.DrawEllipse(radial, null, new Point(300, 50), 40, 40);
}
```

---

## 4. SkiaSharp interop (pixel-level control)

For pixel-level rendering, access the Skia canvas:

```csharp
// Requires: dotnet add package SkiaSharp

public override void Render(DrawingContext context)
{
    // Custom drawing via SkiaSharp through the platform's SkCanvas
    // Use a custom Skia control or interop layer

    // Or render to a bitmap first, then draw it
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
}
```

---

## 5. RenderTargetBitmap (off-screen rendering)

```csharp
// Create an off-screen render target
var rtb = new RenderTargetBitmap(new PixelSize(400, 300));

using (var ctx = rtb.CreateDrawingContext())
{
    ctx.Clear(Colors.White);
    ctx.DrawEllipse(
        new SolidColorBrush(Colors.Red),
        null,
        new Point(200, 150),
        100, 100);
}

// Save to file
rtb.Save("output.png");

// Use as image source
myImage.Source = rtb;
```

---

## 6. Performance considerations

| Technique | Performance | Use when |
|---|---|---|
| `DrawingContext` in `Render()` | Fast, GPU-accelerated | Per-frame drawing in controls |
| `RenderTargetBitmap` | Moderate (one-time render) | Static pre-rendered content |
| SkiaSharp `SKCanvas` | Full control | Pixel-level effects, filters |
| `FormattedText` | Good | Text rendering in custom controls |

- Cache brushes and pens (don't create new ones per render)
- Use `AffectsRender<T>(prop)` to auto-invalidate
- Call `InvalidateVisual()` sparingly (at most once per frame)
- Consider `RenderOptions.BitmapScalingMode` for image quality vs speed

---

## Key Takeaways

- `DrawingContext` handles shapes, text, images, and transforms
- `StreamGeometry` for reusable custom paths
- SkiaSharp for pixel-level control (via `SKCanvas`)
- `RenderTargetBitmap` for off-screen or cached renders
- Cache drawing resources and minimize `InvalidateVisual()` calls

---

## See Also

- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)
- [059 — Colors, Brushes, and FormattedText](../references/59-media-colors-brushes-and-formatted-text-practical-usage.md)
- [Avalonia Docs: Custom Drawing](https://docs.avaloniaui.net/docs/concepts/custom-drawing)
- [028V — Custom Drawing with Skia (verbose companion)](028-custom-drawing-skia-verbose.md)
- [028X — Custom Drawing with Skia (examples)](028-custom-drawing-skia-examples.md)
