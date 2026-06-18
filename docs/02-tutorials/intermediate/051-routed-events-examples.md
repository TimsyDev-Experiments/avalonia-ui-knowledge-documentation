---
tier: intermediate
topic: events
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 051-routed-events.md
---

# 051E — Routed Events: Real-World Examples

**What this is:** Two worked examples showing routed events in real app scenarios. Read [051 — Routed Events](051-routed-events.md) and [051V — Verbose Companion](051-routed-events-verbose.md) first.

---

## Example 1: Canvas Drawing App with Pointer Capture

### Goal

Build a simple ink/drawing surface where the user can freehand draw by dragging the pointer. Uses pointer capture to track strokes even when the pointer moves fast outside the control bounds.

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="DrawingApp.Controls.InkCanvas">
  <Border BorderBrush="{StaticResource BorderBrush}"
          BorderThickness="1"
          Background="Transparent"
          Name="canvasBorder">
    <!-- Empty — drawing happens in code-behind -->
  </Border>
</UserControl>
```

### Code-behind

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections.Generic;

namespace DrawingApp.Controls;

public partial class InkCanvas : UserControl
{
    private readonly List<Point> _currentStroke = new();
    private bool _isDrawing;

    public InkCanvas()
    {
        InitializeComponent();

        // Subscribe to tunnel phase to reset state before children process
        canvasBorder.AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Tunnel);

        canvasBorder.AddHandler(
            InputElement.PointerMovedEvent,
            OnPointerMoved,
            RoutingStrategies.Bubble);

        canvasBorder.AddHandler(
            InputElement.PointerReleasedEvent,
            OnPointerReleased,
            RoutingStrategies.Bubble);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetPosition(canvasBorder);
        _currentStroke.Clear();
        _currentStroke.Add(point);
        _isDrawing = true;
        e.Pointer.Capture(canvasBorder);
        e.Handled = true;
        InvalidateVisual();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing) return;
        var point = e.GetPosition(canvasBorder);
        _currentStroke.Add(point);
        InvalidateVisual();
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDrawing) return;
        _isDrawing = false;
        e.Pointer.Capture(null);
        // Commit stroke to model (not shown)
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_currentStroke.Count < 2) return;

        var pen = new Pen(Brushes.Black, 2);
        for (int i = 1; i < _currentStroke.Count; i++)
        {
            context.DrawLine(pen, _currentStroke[i - 1], _currentStroke[i]);
        }
    }
}
```

**Key technique:** Pointer capture via `e.Pointer.Capture()` ensures the `PointerMoved` events route to the `InkCanvas` even when the pointer leaves its bounds during a fast drag. The `PointerCaptureLost` event fires if another element steals capture, which we could handle to cancel the stroke.

### What happens during routing

1. User presses pointer on `InkCanvas`
2. `PointerPressed` tunnels from `Window` → `InkCanvas`
3. `InkCanvas` captures the pointer and sets `Handled = true`
4. `PointerPressed` bubble phase fires but no other handler receives it (handled)
5. `PointerMoved` events route to `InkCanvas` (captured) regardless of pointer position
6. On release, capture is released and `PointerCaptureLost` fires

---

## Example 2: Custom Slider with Cancelable BeforeValueChange

### Goal

A numeric slider control that fires a `BeforeValueChanged` cancelable event. Parent views can reject the new value by setting `e.Cancel = true`.

### Custom event args

```csharp
using Avalonia.Interactivity;

namespace SliderApp.Controls;

public class BeforeValueChangedEventArgs : CancelRoutedEventArgs
{
    public BeforeValueChangedEventArgs(
        RoutedEvent routedEvent,
        object? source,
        double oldValue,
        double newValue)
        : base(routedEvent, source)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public double OldValue { get; }
    public double NewValue { get; }
}
```

### Custom slider control

```csharp
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SliderApp.Controls;

public class PreviewSlider : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<PreviewSlider, double>(
            nameof(Value), 0.0);

    public static readonly RoutedEvent<BeforeValueChangedEventArgs> BeforeValueChangedEvent =
        RoutedEvent.Register<PreviewSlider, BeforeValueChangedEventArgs>(
            nameof(BeforeValueChanged), RoutingStrategies.Bubble);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event EventHandler<BeforeValueChangedEventArgs>? BeforeValueChanged
    {
        add => AddHandler(BeforeValueChangedEvent, value);
        remove => RemoveHandler(BeforeValueChangedEvent, value);
    }

    private double _trackStart;
    private double _valueAtPress;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        e.Pointer.Capture(this);
        _trackStart = e.GetPosition(this).X;
        _valueAtPress = Value;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!e.Pointer.Captured!.Equals(this)) return;

        var delta = e.GetPosition(this).X - _trackStart;
        var proposed = Math.Clamp(_valueAtPress + delta / 100.0, 0.0, 1.0);

        // Raise cancelable event
        var args = new BeforeValueChangedEventArgs(
            BeforeValueChangedEvent, this, Value, proposed);
        RaiseEvent(args);

        if (!args.Cancel)
            Value = proposed;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        e.Pointer.Capture(null);
    }
}
```

### Consuming the cancelable event in a parent

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:SliderApp.Controls"
        x:Class="SliderApp.MainWindow">
  <StackPanel>
    <local:PreviewSlider Name="volumeSlider" Width="300" Height="30" />
  </StackPanel>
</Window>
```

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Handle the cancelable event bubbled from PreviewSlider
        volumeSlider.AddHandler(
            PreviewSlider.BeforeValueChangedEvent,
            OnSliderValueChanging);
    }

    private void OnSliderValueChanging(object? sender, BeforeValueChangedEventArgs e)
    {
        // Reject values above 0.9 (reserve headroom)
        if (e.NewValue > 0.9)
        {
            e.Cancel = true;
            StatusText.Text = "Value capped at 0.9";
        }
    }
}
```

**Key technique:** The `BeforeValueChangedEvent` uses `RoutingStrategies.Bubble` so the parent `Window` receives it. The cancelable pattern lets parent views enforce constraints without the slider needing to know about them.

---

## See Also

- [051 — Routed Events](051-routed-events.md)
- [051V — Routed Events (verbose companion)](051-routed-events-verbose.md)
- [051Q — Routed Events (quiz)](051-routed-events-quiz.md)
- [Avalonia Docs: Events Overview](https://docs.avaloniaui.net/docs/events)
