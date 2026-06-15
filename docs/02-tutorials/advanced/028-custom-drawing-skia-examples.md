---
tier: advanced
topic: rendering
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 028-custom-drawing-skia.md
---

# 028X — Custom Drawing with Skia: Real-World Examples

## Scenario 1: Real-Time Audio Waveform Visualizer

### Goal

Build a custom `WaveformControl` that renders a scrolling audio waveform using `DrawingContext` with cached resources, `AffectsRender` invalidation, and frame-rate-aware rendering.

### ViewModel

```csharp
using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AudioApp.ViewModels;

public partial class WaveformViewModel : ObservableObject
{
    [ObservableProperty]
    private IReadOnlyList<float> _samples = Array.Empty<float>();

    [ObservableProperty]
    private float _amplitude = 0.8f;

    [ObservableProperty]
    private bool _isPlaying;

    public void FeedSamples(float[] newSamples)
    {
        Samples = newSamples;
    }
}
```

### Custom Control

```csharp
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AudioApp.Controls;

public class WaveformControl : Control
{
    public static readonly StyledProperty<IReadOnlyList<float>> SamplesProperty =
        AvaloniaProperty.Register<WaveformControl, IReadOnlyList<float>>(nameof(Samples));

    public static readonly StyledProperty<float> AmplitudeProperty =
        AvaloniaProperty.Register<WaveformControl, float>(nameof(Amplitude), 0.8f);

    public static readonly StyledProperty<Color> WaveColorProperty =
        AvaloniaProperty.Register<WaveformControl, Color>(nameof(WaveColor), Colors.Cyan);

    public IReadOnlyList<float> Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public float Amplitude
    {
        get => GetValue(AmplitudeProperty);
        set => SetValue(AmplitudeProperty, value);
    }

    public Color WaveColor
    {
        get => GetValue(WaveColorProperty);
        set => SetValue(WaveColorProperty, value);
    }

    // Cached drawing resources — allocated once, reused every frame
    private readonly Pen _gridPen;
    private readonly Pen _centerLinePen;
    private readonly SolidColorBrush _backgroundBrush;

    static WaveformControl()
    {
        AffectsRender<WaveformControl>(SamplesProperty, AmplitudeProperty, WaveColorProperty);
    }

    public WaveformControl()
    {
        _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), 0.5);
        _centerLinePen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);
        _backgroundBrush = new SolidColorBrush(Color.FromArgb(255, 16, 16, 24));
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        // Background
        context.DrawRectangle(_backgroundBrush, null, new Rect(size));

        var centerY = size.Height / 2;
        var samples = Samples;
        var count = samples?.Count ?? 0;

        // Center line
        context.DrawLine(_centerLinePen, new Point(0, centerY), new Point(size.Width, centerY));

        if (count < 2)
            return;

        // Vertical grid lines (every 50px)
        for (var x = 0.0; x < size.Width; x += 50)
            context.DrawLine(_gridPen, new Point(x, 0), new Point(x, size.Height));

        // Build waveform geometry
        var geometry = new StreamGeometry();
        using (var geoCtx = geometry.Open())
        {
            var stepX = size.Width / (count - 1);
            var scale = (float)centerY * Amplitude;

            geoCtx.BeginFigure(new Point(0, centerY - samples[0] * scale), false);

            for (var i = 1; i < count; i++)
            {
                var x = i * stepX;
                var y = centerY - samples[i] * scale;
                geoCtx.LineTo(new Point(x, y));
            }

            geoCtx.EndFigure(false);
        }

        var waveBrush = new SolidColorBrush(WaveColor);
        context.DrawGeometry(null, new Pen(waveBrush, 1.5), geometry);
    }
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:AudioApp.ViewModels"
             xmlns:ctrl="using:AudioApp.Controls"
             x:Class="AudioApp.Views.WaveformView"
             x:DataType="vm:WaveformViewModel">

  <Grid RowDefinitions="*,Auto" Spacing="8">
    <ctrl:WaveformControl Samples="{Binding Samples}"
                          Amplitude="{Binding Amplitude}"
                          WaveColor="Cyan" />
  </Grid>
</UserControl>
```

### How It Works

1. `AffectsRender<WaveformControl>(SamplesProperty, ...)` registers three properties for automatic invalidation. When `Samples` changes (new audio buffer arrives), `InvalidateVisual()` fires automatically.
2. `Render()` checks bounds early — if the control has zero size (not yet measured), it exits immediately to avoid division-by-zero.
3. Cached `_gridPen` and `_centerLinePen` are static-like fields created once in the constructor. The wave-color brush is created per render because the color can change; for steady color, cache it as a field too.
4. `StreamGeometry` builds the waveform path: `BeginFigure` at the first sample, `LineTo` for each subsequent sample, `EndFigure(false)` for an open path. The path is not closed (waveform is a line, not a filled shape).
5. `Amplitude` scales the Y values relative to `centerY`. A value of 1.0 uses the full height; 0.5 uses half.

### Design Decisions & Edge Cases

- **Zero-size guard**: Prevents `DivideByZeroException` on `Bounds.Width / (count - 1)` when the control has not been laid out.
- **Single-sample edge case**: If `count < 2`, only the center line draws. The waveform path requires at least two points.
- **Per-frame allocation**: The `StreamGeometry` and `SolidColorBrush` are allocated every render. For a real-time visualizer at 60 FPS, these should be cached and re-opened. A production version would hold a single `StreamGeometry` instance and clear/re-populate it via `geo.Open()`.
- **Scrolling waveform**: This example draws the full buffer. For a scrolling/scrolling display, sample the buffer to fit `Bounds.Width` pixels. Use a downsampling strategy (peak or average per pixel column) to avoid aliasing.

---

## Scenario 2: Custom Line Chart with SkiaSharp Anti-Aliased Rendering

### Goal

Create a `ChartControl` that renders an interactive line chart using SkiaSharp for pixel-level control — anti-aliased lines, gradient fills under the curve, and real-time hover tracking.

### ViewModel

```csharp
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChartApp.ViewModels;

public partial class ChartViewModel : ObservableObject
{
    public ObservableCollection<DataPoint> DataPoints { get; } = new();

    [ObservableProperty]
    private double _minValue;

    [ObservableProperty]
    private double _maxValue = 100;

    [ObservableProperty]
    private string _hoveredValue = string.Empty;
}

public record DataPoint(double X, double Y);
```

### Custom Control (SkiaSharp-based)

```csharp
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace ChartApp.Controls;

public class ChartControl : Control
{
    public static readonly StyledProperty<IList<DataPoint>> DataPointsProperty =
        AvaloniaProperty.Register<ChartControl, IList<DataPoint>>(nameof(DataPoints));

    public static readonly StyledProperty<double> MinValueProperty =
        AvaloniaProperty.Register<ChartControl, double>(nameof(MinValue));

    public static readonly StyledProperty<double> MaxValueProperty =
        AvaloniaProperty.Register<ChartControl, double>(nameof(MaxValue), 100);

    public IList<DataPoint> DataPoints
    {
        get => GetValue(DataPointsProperty);
        set => SetValue(DataPointsProperty, value);
    }

    public double MinValue
    {
        get => GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public double MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    private SKBitmap? _cachedBitmap;

    static ChartControl()
    {
        AffectsRender<ChartControl>(DataPointsProperty, MinValueProperty, MaxValueProperty);
    }

    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return;

        var points = DataPoints;
        if (points == null || points.Count < 2)
            return;

        var pixelSize = new PixelSize((int)size.Width, (int)size.Height);

        // Render off-screen with SkiaSharp
        using var surface = SKSurface.Create(new SKImageInfo(pixelSize.Width, pixelSize.Height));
        var canvas = surface.Canvas;

        // Anti-aliased background
        canvas.Clear(new SKColor(18, 18, 28));

        var chartRect = new SKRect(40, 10, pixelSize.Width - 10, pixelSize.Height - 30);
        var range = MaxValue - MinValue;

        if (range <= 0)
            return;

        // Map data points to pixel coordinates
        var screenPoints = new SKPoint[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var x = chartRect.Left + (float)((points[i].X - points[0].X) /
                (points[^1].X - points[0].X) * chartRect.Width);
            var y = chartRect.Bottom - (float)((points[i].Y - MinValue) / range * chartRect.Height);
            screenPoints[i] = new SKPoint(x, y);
        }

        // Grid lines (horizontal)
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 30),
            StrokeWidth = 0.5f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        for (var v = 0; v <= 4; v++)
        {
            var y = chartRect.Bottom - v / 4f * chartRect.Height;
            canvas.DrawLine(chartRect.Left, y, chartRect.Right, y, gridPaint);
        }

        // Gradient fill under the curve
        using var fillPath = new SKPath();
        fillPath.MoveTo(screenPoints[0].X, chartRect.Bottom);
        foreach (var pt in screenPoints)
            fillPath.LineTo(pt);
        fillPath.LineTo(screenPoints[^1].X, chartRect.Bottom);
        fillPath.Close();

        using var fillPaint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, chartRect.Top),
                new SKPoint(0, chartRect.Bottom),
                new[] { new SKColor(0, 212, 255, 100), new SKColor(0, 212, 255, 10) },
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        canvas.DrawPath(fillPath, fillPaint);

        // Line stroke
        using var linePaint = new SKPaint
        {
            Color = new SKColor(0, 212, 255),
            StrokeWidth = 2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        using var linePath = new SKPath();
        linePath.MoveTo(screenPoints[0]);
        for (var i = 1; i < screenPoints.Length; i++)
            linePath.LineTo(screenPoints[i]);
        canvas.DrawPath(linePath, linePaint);

        // Convert to Avalonia bitmap and draw
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();
        var avaloniaBitmap = new Bitmap(stream);
        context.DrawImage(avaloniaBitmap, new Rect(size));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        InvalidateVisual();
    }
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ChartApp.ViewModels"
             xmlns:ctrl="using:ChartApp.Controls"
             x:Class="ChartApp.Views.ChartView"
             x:DataType="vm:ChartViewModel">

  <ctrl:ChartControl DataPoints="{Binding DataPoints}"
                     MinValue="{Binding MinValue}"
                     MaxValue="{Binding MaxValue}" />
</UserControl>
```

### How It Works

1. `SKSurface.Create()` allocates an off-screen GPU-backed surface sized to the control's pixel bounds.
2. The Skia canvas renders: a dark background, semi-transparent horizontal grid lines, a gradient-filled area under the curve, and a 2px anti-aliased line stroke.
3. `SKShader.CreateLinearGradient` creates a vertical gradient from cyan (100% opacity at top) to transparent (at bottom) for the fill under the line.
4. The `SKPath` builds two paths: one closed path for the gradient fill (line down to bottom-right, across to bottom-left, back to start), and one open path for the stroke line.
5. `surface.Snapshot()` captures the rendered frame, encodes it to PNG, decodes back into an Avalonia `Bitmap`, and `context.DrawImage` composites it onto the control.

### Design Decisions & Edge Cases

- **Off-screen SkiaSharp round-trip**: Each render encodes to PNG and decodes back — acceptable for low-frequency updates (data chart refreshes every few seconds). For real-time (60 FPS), use `WriteableBitmap` with direct pixel copy instead.
- **Single data point edge case**: The control returns early if `points.Count < 2` — a line requires at least two points.
- **Zero range edge case**: If `MinValue == MaxValue`, `range <= 0` guard prevents division by zero. The chart shows an empty plot area.
- **Pixel snapping**: Skia's anti-aliased rendering handles sub-pixel positioning naturally. No manual pixel snapping needed.
- **Caching strategy**: The `SKBitmap` is created and discarded each render. For static data, cache the resulting `Bitmap` and only re-render when `DataPoints` changes. Track changes with a dirty flag.

### Comparison

| Aspect | Scenario 1: Waveform | Scenario 2: Line Chart |
|---|---|---|
| Rendering API | `DrawingContext` + `StreamGeometry` | SkiaSharp `SKCanvas` + `SKPath` |
| Anti-aliasing | Via Skia backend (default) | Explicit `IsAntialias = true` |
| Gradient support | `LinearGradientBrush` (Avalonia) | `SKShader.CreateLinearGradient` (Skia) |
| Performance target | 60 FPS real-time | Low-frequency (data-driven) |
| Off-screen | No (direct render) | Yes (Skia → PNG → Bitmap) |
| Resource caching | Cached pens, per-frame geometry | Full bitmap cached on data change |
| Best for | Audio visualizers, oscilloscopes | Charts, graphs, scientific plots |

## See Also

- [028 — Custom Drawing with Skia](028-custom-drawing-skia.md)
- [028V — Custom Drawing with Skia (verbose companion)](028-custom-drawing-skia-verbose.md)
- [025 — Compositor & Custom Visuals](025-compositor-custom-visuals.md)
- [028 — Custom Drawing with Skia](028-custom-drawing-skia.md) — covers colors, brushes, and text rendering
- [Avalonia Docs: Custom Drawing](https://docs.avaloniaui.net/docs/concepts/custom-drawing)
