---
tier: advanced
topic: rendering
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 082E — Graphics & Drawing Reference (examples)

## Example 1: Custom progress ring

```csharp
public class ProgressRing : Control
{
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Progress));

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    static ProgressRing()
    {
        AffectsRender<ProgressRing>(ProgressProperty);
    }

    public override void Render(DrawingContext context)
    {
        var rect = new Rect(Bounds.Size).Deflate(4);
        var startAngle = -90;
        var endAngle = startAngle + 360 * (Progress / 100);

        // Background track
        context.DrawEllipse(null, new Pen(Brushes.LightGray, 4), rect.Center,
                            rect.Width / 2, rect.Height / 2);

        // Progress arc using a PathGeometry
        var arc = CreateArc(rect.Center, rect.Width / 2,
                            startAngle, endAngle);
        context.DrawGeometry(null, new Pen(Brushes.DodgerBlue, 4), arc);
    }

    private static Geometry CreateArc(Point center, double radius,
                                       double start, double end)
    {
        var geo = new StreamGeometry();
        using var ctx = geo.Open();
        var startPt = PolarToCartesian(center, radius, start);
        var endPt = PolarToCartesian(center, radius, end);
        bool isLarge = Math.Abs(end - start) > 180;

        ctx.BeginFigure(startPt, isFilled: false);
        ctx.ArcTo(endPt, new Size(radius, radius), 0,
                  isLarge, SweepDirection.Clockwise);
        ctx.EndFigure(isClosed: false);
        return geo;
    }

    private static Point PolarToCartesian(Point center, double radius,
                                           double angleDeg)
    {
        double rad = angleDeg * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(rad),
                         center.Y + radius * Math.Sin(rad));
    }
}
```

```xml
<local:ProgressRing Progress="{Binding DownloadProgress}"
                    Width="80" Height="80" />
```

## Example 2: Rounded image with clip and shadow

```xml
<Border BoxShadow="0 4 8 0 #40000000" CornerRadius="12">
  <Border ClipToBounds="True" CornerRadius="12">
    <Image Source="avares://MyApp/Assets/photo.png"
           Stretch="UniformToFill" Width="200" Height="200" />
  </Border>
</Border>
```

## Example 3: VisualBrush reflection effect

```xml
<StackPanel>
  <TextBlock x:Name="SourceText" Text="Avalonia"
             FontSize="48" FontWeight="Bold" />

  <Rectangle Height="60" Margin="0,-10,0,0">
    <Rectangle.Fill>
      <VisualBrush Visual="{Binding #SourceText}" Stretch="None">
        <VisualBrush.Transform>
          <ScaleTransform ScaleY="-0.5" />
        </VisualBrush.Transform>
      </VisualBrush>
    </Rectangle.Fill>
    <Rectangle.OpacityMask>
      <LinearGradientBrush StartPoint="0%,0%" EndPoint="0%,100%">
        <GradientStop Color="Black" Offset="0" />
        <GradientStop Color="Transparent" Offset="1" />
      </LinearGradientBrush>
    </Rectangle.OpacityMask>
  </Rectangle>
</StackPanel>
```

## Example 4: Dashed border with multiple shadows

```xml
<Border Background="White" CornerRadius="8" Padding="20"
        BoxShadow="0 1 3 0 #20000000, 0 4 12 0 #10000000">
  <Border.BorderBrush>
    <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
      <GradientStop Color="#6366F1" Offset="0" />
      <GradientStop Color="#EC4899" Offset="1" />
    </LinearGradientBrush>
  </Border.BorderBrush>
  <Border.BorderThickness>2</Border.BorderThickness>
  <!-- Dashed border currently not supported on Border -->
</Border>
```

## Example 5: SkiaSharp real-time chart

```csharp
public class RealtimeChart : Control
{
    private readonly List<float> _data = new();
    private ICustomDrawOperation? _currentOp;

    public void AddPoint(float value)
    {
        _data.Add(value);
        if (_data.Count > 200) _data.RemoveAt(0);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        _currentOp = new ChartDrawOperation(_data.ToArray(),
                                             new Rect(Bounds.Size));
        context.Custom(_currentOp);
    }

    private class ChartDrawOperation : ICustomDrawOperation
    {
        private readonly float[] _data;
        public Rect Bounds { get; }

        public ChartDrawOperation(float[] data, Rect bounds)
        {
            _data = data;
            Bounds = bounds;
        }

        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (feature is null) return;
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;

            canvas.Clear(SKColors.White);

            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                Color = SKColors.DodgerBlue
            };
            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = new SKColor(30, 144, 255, 40)
            };

            var path = new SKPath();
            var w = (float)Bounds.Width;
            var h = (float)Bounds.Height;

            if (_data.Length < 2) return;
            path.MoveTo(0, h * (1 - _data[0]));

            for (int i = 1; i < _data.Length; i++)
            {
                float x = w * i / (_data.Length - 1);
                float y = h * (1 - _data[i]);
                path.LineTo(x, y);
            }

            // Fill area under curve
            path.LineTo(w, h);
            path.LineTo(0, h);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, linePaint);
        }

        public bool HitTest(Point p) => Bounds.Contains(p);
        public bool Equals(ICustomDrawOperation? other) => false;
        public void Dispose() { }
    }
}
```

## Example 6: OpacityMask radial vignette

```xml
<Image Source="avares://MyApp/photo.png" Width="300" Height="300"
       Stretch="UniformToFill">
  <Image.OpacityMask>
    <RadialGradientBrush Center="50%,50%" GradientOrigin="50%,50%"
                          RadiusX="50%" RadiusY="50%">
      <GradientStop Color="Black" Offset="0.4" />
      <GradientStop Color="Transparent" Offset="1" />
    </RadialGradientBrush>
  </Image.OpacityMask>
</Image>
```

## Example 7: SVG path icons as resources

```xml
<Application.Resources>
  <StreamGeometry x:Key="CheckIcon">M 4,8.5 L 8,12.5 L 16,4</StreamGeometry>
  <StreamGeometry x:Key="ArrowIcon">M 4,12 L 12,4 L 20,12</StreamGeometry>
</Application.Resources>
```

```xml
<Path Data="{StaticResource CheckIcon}"
      Stroke="Green" StrokeThickness="2"
      StrokeLineCap="Round" StrokeLineJoin="Round" />
```

## Example 8: Conic gradient color wheel

```xml
<Ellipse Width="200" Height="200">
  <Ellipse.Fill>
    <ConicGradientBrush Center="50%,50%" Angle="0">
      <GradientStop Color="#EF4444" Offset="0.0" />
      <GradientStop Color="#F59E0B" Offset="0.17" />
      <GradientStop Color="#22C55E" Offset="0.33" />
      <GradientStop Color="#3B82F6" Offset="0.5" />
      <GradientStop Color="#6366F1" Offset="0.67" />
      <GradientStop Color="#A855F7" Offset="0.83" />
      <GradientStop Color="#EF4444" Offset="1.0" />
    </ConicGradientBrush>
  </Ellipse.Fill>
</Ellipse>
```
