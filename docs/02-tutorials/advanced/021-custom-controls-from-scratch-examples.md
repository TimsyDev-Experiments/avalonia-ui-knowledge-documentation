---
tier: advanced
topic: custom controls
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 021-custom-controls-from-scratch.md
---

# 021E — Custom Controls from Scratch: Real-World Examples

**Applies to:** [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) | [021V — In-Depth Companion](021-custom-controls-from-scratch-verbose.md)

---

## Example 1: WaveformViewer

### Goal

A `Control` subclass that renders an audio waveform from a sample buffer. The control draws vertical bars representing audio amplitude at each sample point. It supports dynamic sample data updates, configurable bar width and spacing, and gradient coloring from a configurable start-to-end color.

### ViewModel

```csharp
// ViewModels/WaveformViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class WaveformViewModel : ObservableObject
{
    [ObservableProperty]
    private float[] _samples = new float[512];

    [ObservableProperty]
    private Color _waveformStartColor = Colors.Cyan;

    [ObservableProperty]
    private Color _waveformEndColor = Colors.Blue;

    public void LoadSamples(float[] newSamples)
    {
        Samples = newSamples;
    }
}
```

### XAML View

```xml
<!-- Views/WaveformView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:WaveformViewModel">
  <controls:WaveformViewer
      Samples="{Binding Samples}"
      StartColor="{Binding WaveformStartColor}"
      EndColor="{Binding WaveformEndColor}"
      BarWidth="4"
      BarSpacing="1" />
</UserControl>
```

### How It Works

1. `WaveformViewer` extends `Control`. It exposes `SamplesProperty` (`float[]`), `StartColorProperty`, `EndColorProperty`, `BarWidthProperty`, and `BarSpacingProperty` — all `StyledProperty` with `AffectsRender` registration in the static constructor.
2. `MeasureOverride` computes a desired height based on `Samples.Length` times `(BarWidth + BarSpacing)`, capped at `availableSize.Height`. The control is designed to fill horizontal space.
3. `Render(DrawingContext)` iterates the sample buffer. For each sample value (normalized -1.0 to 1.0), it draws a vertical bar. Bar height is proportional to `Math.Abs(sample) * Bounds.Height / 2`. Bar color is interpolated along the gradient from `StartColor` to `EndColor` based on the sample index.
4. `AffectsRender` ensures property changes trigger `InvalidateVisual()`.

```csharp
// Controls/WaveformViewer.cs
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace MyApp.Controls;

public class WaveformViewer : Control
{
    public static readonly StyledProperty<float[]> SamplesProperty =
        AvaloniaProperty.Register<WaveformViewer, float[]>(nameof(Samples), []);

    public static readonly StyledProperty<Color> StartColorProperty =
        AvaloniaProperty.Register<WaveformViewer, Color>(nameof(StartColor), Colors.Cyan);

    public static readonly StyledProperty<Color> EndColorProperty =
        AvaloniaProperty.Register<WaveformViewer, Color>(nameof(EndColor), Colors.Blue);

    public static readonly StyledProperty<double> BarWidthProperty =
        AvaloniaProperty.Register<WaveformViewer, double>(nameof(BarWidth), 4);

    public static readonly StyledProperty<double> BarSpacingProperty =
        AvaloniaProperty.Register<WaveformViewer, double>(nameof(BarSpacing), 1);

    public float[] Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public Color StartColor
    {
        get => GetValue(StartColorProperty);
        set => SetValue(StartColorProperty, value);
    }

    public Color EndColor
    {
        get => GetValue(EndColorProperty);
        set => SetValue(EndColorProperty, value);
    }

    public double BarWidth
    {
        get => GetValue(BarWidthProperty);
        set => SetValue(BarWidthProperty, Math.Max(1, value));
    }

    public double BarSpacing
    {
        get => GetValue(BarSpacingProperty);
        set => SetValue(BarSpacingProperty, Math.Max(0, value));
    }

    static WaveformViewer()
    {
        AffectsRender<WaveformViewer>(SamplesProperty, StartColorProperty,
            EndColorProperty, BarWidthProperty, BarSpacingProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var totalWidth = Samples.Length * (BarWidth + BarSpacing);
        return new Size(totalWidth, availableSize.Height);
    }

    public override void Render(DrawingContext context)
    {
        if (Samples is null || Samples.Length == 0)
        {
            context.DrawLine(new ImmutablePen(StartColor, 1),
                new Point(0, Bounds.Height / 2),
                new Point(Bounds.Width, Bounds.Height / 2));
            return;
        }

        var midY = Bounds.Height / 2;
        var maxBars = (int)(Bounds.Width / (BarWidth + BarSpacing));
        var count = Math.Min(Samples.Length, maxBars);

        for (int i = 0; i < count; i++)
        {
            var normalized = Math.Clamp(Samples[i], -1f, 1f);
            var barHeight = Math.Abs(normalized) * midY;
            var x = i * (BarWidth + BarSpacing);
            var y = midY - (normalized >= 0 ? barHeight : 0);
            var t = count > 1 ? (double)i / (count - 1) : 0.5;
            var color = Color.Lerp(StartColor, EndColor, t);

            context.FillRectangle(new ImmutableSolidColorBrush(color),
                new Rect(x, y, BarWidth, barHeight > 0 ? barHeight : 1));
        }
    }
}
```

---

### Design Decisions

- **`float[]` over `IList<float>`.** Arrays provide cache-friendly iteration in the render loop. The property is replaced wholesale (not mutated in-place), so array covariance is not an issue.
- **Gradient interpolation in `Render`.** Computed per frame using `Color.Lerp`. For real-time audio visualization (60fps), this avoids allocating `LinearGradientBrush` objects each frame. A cached gradient brush is a viable alternative if the start/end colors are stable.
- **Bar drawing over polygon rendering.** Simple bars are fast to draw via `context.FillRectangle`. A polygon waveform (filled shape following the sample contour) is visually smoother but requires `StreamGeometry` construction, which is slower per frame.

### Edge Cases

- **`Samples` is null or empty.** `Render` checks `Samples is null || Samples.Length == 0` and draws an empty area with a subtle baseline line.
- **`Samples` length changes between frames.** The measure pass uses the current buffer length. Changing the array triggers `InvalidateMeasure()` via a property-changed handler in the ViewModel.
- **`BarWidth` or `BarSpacing` is 0 or negative.** Clamp to 1 in the property setter to avoid division-by-zero or overlapping bars.
- **High-frequency sample updates.** `InvalidateVisual()` is throttled by the compositor (max once per frame). For 144Hz displays, the render loop keeps up with sample buffers up to ~2048 elements.

---

## Example 2: ColorGradientEditor

### Goal

An interactive gradient editor control. The user clicks to add color stops, drags stops to reposition them, and right-clicks to remove them. The control renders a live gradient preview bar and handles pointer input for stop manipulation. Exposes `Stops` as an `ObservableCollection<GradientStop>` and `SelectedStopIndex`.

### ViewModel

```csharp
// ViewModels/GradientEditorViewModel.cs
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class GradientEditorViewModel : ObservableObject
{
    public ObservableCollection<GradientStop> Stops { get; } =
    [
        new GradientStop(Colors.Red, 0.0),
        new GradientStop(Colors.Yellow, 0.5),
        new GradientStop(Colors.Green, 1.0),
    ];

    [ObservableProperty]
    private int _selectedStopIndex;

    [ObservableProperty]
    private Color _currentStopColor = Colors.Red;

    partial void OnSelectedStopIndexChanged(int value)
    {
        if (value >= 0 && value < Stops.Count)
            CurrentStopColor = Stops[value].Color;
    }
}
```

### XAML View

```xml
<!-- Views/GradientEditorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:GradientEditorViewModel">
  <DockPanel>
    <controls:GradientEditor
        Stops="{Binding Stops}"
        SelectedStopIndex="{Binding SelectedStopIndex, Mode=TwoWay}"
        Height="60"
        DockPanel.Dock="Top" />
    <Grid ColumnDefinitions="Auto,*" Margin="0,8,0,0">
      <TextBlock Text="Stop Color:" VerticalAlignment="Center" />
      <TextBox Text="{Binding CurrentStopColor}"
               Grid.Column="1" Margin="8,0,0,0" />
    </Grid>
  </DockPanel>
</UserControl>
```

### How It Works

1. `GradientEditor` extends `Control`. It holds an `ObservableCollection<GradientStop>` as a `StyledProperty`. `SelectedStopIndex` is a `StyledProperty<int>` with `defaultBindingMode: TwoWay`.
2. `Render(DrawingContext)` draws the gradient bar using `context.FillRectangle` with a `LinearGradientBrush` built from the current `Stops`. Below the bar, it draws small triangle markers for each stop, with the selected marker highlighted.
3. `OnPointerPressed` hits against the stop markers (by checking whether the pointer X coordinate is within a threshold of any stop's position). If a marker is hit, the stop is selected and dragging begins. If the click is on an empty region of the bar, a new stop is created at that position with the interpolated color.
4. `OnPointerMoved` during drag updates the stop's `Offset` (clamped to 0.0–1.0) and calls `InvalidateVisual()`. `OnPointerReleased` ends the drag.
5. Right-click on a stop removes it. Removal is blocked if the stop count would drop below 2 (a gradient needs at least two stops).

```csharp
// Controls/GradientEditor.cs
using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace MyApp.Controls;

public class GradientEditor : Control
{
    private const double MarkerHalfWidth = 4;
    private const double HitThreshold = 6;
    private const double MarkerHeight = 10;

    private int _dragIndex = -1;
    private AvaloniaList<GradientStop>? _stops;

    public static readonly StyledProperty<AvaloniaList<GradientStop>?> StopsProperty =
        AvaloniaProperty.Register<GradientEditor, AvaloniaList<GradientStop>?>(nameof(Stops));

    public static readonly StyledProperty<int> SelectedStopIndexProperty =
        AvaloniaProperty.Register<GradientEditor, int>(nameof(SelectedStopIndex), -1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public AvaloniaList<GradientStop>? Stops
    {
        get => GetValue(StopsProperty);
        set => SetValue(StopsProperty, value);
    }

    public int SelectedStopIndex
    {
        get => GetValue(SelectedStopIndexProperty);
        set => SetValue(SelectedStopIndexProperty, value);
    }

    static GradientEditor()
    {
        AffectsRender<GradientEditor>(StopsProperty, SelectedStopIndexProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StopsProperty)
        {
            if (change.OldValue is AvaloniaList<GradientStop> oldList)
            {
                oldList.CollectionChanged -= OnStopsChanged;
                foreach (var stop in oldList)
                    stop.PropertyChanged -= OnStopPropertyChanged;
            }
            if (change.NewValue is AvaloniaList<GradientStop> newList)
            {
                newList.CollectionChanged += OnStopsChanged;
                foreach (var stop in newList)
                    stop.PropertyChanged += OnStopPropertyChanged;
            }
            _stops = change.NewValue as AvaloniaList<GradientStop>;
            InvalidateVisual();
        }
    }

    private void OnStopsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnStopPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var stops = _stops;
        var validStops = stops is { Count: >= 2 };

        if (!validStops)
        {
            var fallback = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            };
            fallback.GradientStops.Add(new GradientStop(Colors.Black, 0));
            fallback.GradientStops.Add(new GradientStop(Colors.White, 1));
            context.FillRectangle(fallback, new Rect(0, 0, Bounds.Width, Bounds.Height - MarkerHeight));
            return;
        }

        var barRect = new Rect(0, 0, Bounds.Width, Bounds.Height - MarkerHeight);
        var brush = new ImmutableLinearGradientBrush(
            stops.Select(s => new ImmutableGradientStop(s.Offset, s.Color)),
            new RelativePoint(0, 0.5, RelativeUnit.Relative),
            new RelativePoint(1, 0.5, RelativeUnit.Relative));
        context.FillRectangle(brush, barRect);

        for (int i = 0; i < stops.Count; i++)
        {
            var stop = stops[i];
            var cx = stop.Offset * Bounds.Width;
            var ty = barRect.Bottom;

            var isSelected = i == SelectedStopIndex;
            var markerColor = isSelected ? Colors.White : Colors.Gray;
            var borderColor = isSelected ? Colors.Black : Colors.DarkGray;

            var marker = new StreamGeometry();
            using (var ctx = marker.Open())
            {
                ctx.BeginFigure(new Point(cx - MarkerHalfWidth, ty), true);
                ctx.LineTo(new Point(cx + MarkerHalfWidth, ty));
                ctx.LineTo(new Point(cx, ty + MarkerHeight));
                ctx.EndFigure(true);
            }

            context.DrawGeometry(
                new ImmutableSolidColorBrush(markerColor),
                new ImmutablePen(borderColor, 1),
                marker);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        var stops = _stops;
        if (stops is null) return;

        for (int i = 0; i < stops.Count; i++)
        {
            var cx = stops[i].Offset * Bounds.Width;
            if (Math.Abs(point.X - cx) <= HitThreshold)
            {
                SelectedStopIndex = i;
                _dragIndex = i;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }
        }

        if (point.Y < Bounds.Height - MarkerHeight)
        {
            var offset = Math.Clamp(point.X / Bounds.Width, 0, 1);
            var color = InterpolateColor(stops, offset);
            stops.Add(new GradientStop(color, offset));
            SelectedStopIndex = stops.Count - 1;
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_dragIndex < 0 || _stops is null || _dragIndex >= _stops.Count) return;

        var point = e.GetPosition(this);
        _stops[_dragIndex].Offset = Math.Clamp(point.X / Bounds.Width, 0, 1);
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_dragIndex >= 0)
        {
            _dragIndex = -1;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private static Color InterpolateColor(IList<GradientStop> stops, double offset)
    {
        if (stops.Count == 0) return Colors.Black;
        if (stops.Count == 1) return stops[0].Color;

        for (int i = 0; i < stops.Count - 1; i++)
        {
            var a = stops[i];
            var b = stops[i + 1];
            if (offset >= a.Offset && offset <= b.Offset)
            {
                var t = b.Offset - a.Offset > 0
                    ? (offset - a.Offset) / (b.Offset - a.Offset)
                    : 0;
                return Color.Lerp(a.Color, b.Color, t);
            }
        }
        return stops[^1].Color;
    }
}
```

---

### Design Decisions

- **`GradientStop` as a mutable model.** Avalonia's `GradientStop` implements `INotifyPropertyChanged`, so changing its `Offset` or `Color` automatically triggers the binding to refresh. The control subscribes to `CollectionChanged` and each stop's `PropertyChanged` to call `InvalidateVisual()`.
- **Hit-test threshold of 6px.** Stops are small (8px wide markers). A 6px tolerance around each marker position gives the user a reasonable click target without requiring pixel-perfect precision.
- **Minimum two stops.** Many gradient operations (linear interpolation) require at least two stops. The removal guard prevents invalid state.

### Edge Cases

- **`Stops` is null or has fewer than 2 entries.** The control renders a fallback black-to-white gradient. The add-stop handler still functions, allowing the user to build up from zero.
- **Stop offset violates ordering.** After dragging, `Stop[1].Offset` could become less than `Stop[0].Offset`. The control does not auto-sort; overlapping stops produce undefined gradient behavior. A production version should clamp drags to the adjacent stop boundaries.
- **Drag goes outside control bounds.** Clamp the offset to 0.0–1.0 on each move. If the pointer leaves the control, `Pointer.Capture` ensures the drag continues until release.
- **Color format in ViewModel.** `CurrentStopColor` is a `Color` struct. The `TextBox` in the view binds to `Color.ToString()` by default, which produces the hex format (`#AARRGGBB`). Use a value converter for custom color input formats.

## Testing

### Example 1: WaveformViewer

```csharp
[AvaloniaFact]
public void WaveformViewer_MeasureOverride_ReturnsCorrectWidth()
{
    var viewer = new WaveformViewer
    {
        Samples = new float[100],
        BarWidth = 4,
        BarSpacing = 1
    };
    viewer.Measure(new Size(double.PositiveInfinity, 100));
    viewer.DesiredSize.Width.Should().Be(100 * (4 + 1));
}

[AvaloniaFact]
public void WaveformViewer_NullSamples_DoesNotThrow()
{
    var viewer = new WaveformViewer { Samples = null! };
    viewer.Measure(new Size(200, 100));
    viewer.Arrange(new Rect(0, 0, 200, 100));
    Action render = () => viewer.Render(null!);
    render.Should().NotThrow();
}
```

### Example 2: GradientEditor

```csharp
[AvaloniaFact]
public void GradientEditor_AddStop_IncreasesStopCount()
{
    var vm = new GradientEditorViewModel();
    var editor = new GradientEditor
    {
        DataContext = vm,
        Stops = vm.Stops,
    };
    editor.Measure(new Size(300, 60));
    editor.Arrange(new Rect(0, 0, 300, 60));

    editor.Stops!.Add(new GradientStop(Colors.Blue, 0.75));
    vm.Stops.Count.Should().Be(4);
}
```

---

## What These Examples Demonstrate

| Aspect | WaveformViewer | GradientEditor |
|---|---|---|
| **Rendering** | Batch vertical bars per frame | Gradient fill + marker geometry |
| **Input handling** | None (display only) | Pointer press/move/release, right-click |
| **Input capture** | Not needed | `Pointer.Capture` for drag tracking |
| **Property change invalidation** | `AffectsRender` for all properties | `AffectsRender` + manual `InvalidateVisual` on collection changes |
| **Data binding** | One-way (`float[]` replaced wholesale) | Two-way (`SelectedStopIndex`), collection change notifications |
| **Measure behavior** | Computed from sample count + bar metrics | Fixed-height bar, fills available width |

---

## See Also

- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)
- [021V — Custom Controls from Scratch (verbose companion)](021-custom-controls-from-scratch-verbose.md)
- [023 — Custom Layout Panels](023-custom-layout-panels.md) — measure/arrange patterns shared with custom controls
- [020 — Custom Templated Controls](020-custom-templated-controls.md) — template-based alternative
- [Avalonia Docs: Custom Controls](https://docs.avaloniaui.net/docs/concepts/custom-controls)
