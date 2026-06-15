---
tier: advanced
topic: custom controls
estimated: 12 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 021 — Custom Controls from Scratch

**What you'll learn:** Build a custom control using `Render` override, handle pointer input, and implement measure/arrange logic without templates.

**Prerequisites:** [020 — Custom Templated Controls](020-custom-templated-controls.md)

---

## 1. The control

```csharp
// Controls/SignaturePad.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;

namespace MyApp.Controls;

public class SignaturePad : Control
{
    private readonly List<Point> _points = new();
    private bool _isDrawing;

    public SignaturePad()
    {
        ClipToBounds = true;
    }

    static SignaturePad()
    {
        AffectsRender<SignaturePad>(BackgroundProperty, StrokeColorProperty, StrokeThicknessProperty);
    }

    public static readonly StyledProperty<Color> StrokeColorProperty =
        AvaloniaProperty.Register<SignaturePad, Color>(nameof(StrokeColor), Colors.Black);

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<SignaturePad, double>(nameof(StrokeThickness), 3.0);

    public Color StrokeColor
    {
        get => GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
```

---

## 2. Pointer event handling

```csharp
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDrawing = true;
        _points.Clear();
        _points.Add(e.GetPosition(this));
        e.Pointer.Capture(this);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDrawing)
        {
            _points.Add(e.GetPosition(this));
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDrawing = false;
        e.Pointer.Capture(null);
    }
```

---

## 3. Custom rendering

```csharp
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // Background
        var background = Background;
        if (background is not null)
        {
            context.DrawRectangle(background, null, new Rect(Bounds.Size));
        }

        // Border
        var borderPen = new Pen(BorderBrush ?? Brushes.Gray, BorderThickness.Top);
        context.DrawRectangle(null, borderPen, new Rect(Bounds.Size));

        // Draw stroke
        if (_points.Count < 2) return;

        var stroke = new Pen(new SolidColorBrush(StrokeColor), StrokeThickness);

        for (int i = 1; i < _points.Count; i++)
        {
            context.DrawLine(stroke, _points[i - 1], _points[i]);
        }
    }
```

---

## 4. Measure and arrange

```csharp
    protected override Size MeasureOverride(Size availableSize)
    {
        // Default size if no constraints
        var desired = new Size(300, 150);

        if (availableSize.Width < desired.Width)
            desired = desired.WithWidth(availableSize.Width);

        if (availableSize.Height < desired.Height)
            desired = desired.WithHeight(availableSize.Height);

        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        return finalSize;
    }
```

---

## 5. Clear method

```csharp
    public void Clear()
    {
        _points.Clear();
        InvalidateVisual();
    }
}
```

---

## 6. Usage

```xml
<controls:SignaturePad StrokeColor="Blue"
                       StrokeThickness="4"
                       Background="White"
                       Width="400" Height="200" />
```

---

## Key Takeaways

- Extend `Control` directly for completely custom visuals (no template)
- Override `Render(DrawingContext)` for all drawing
- Override `MeasureOverride` / `ArrangeOverride` for custom layout
- Call `AffectsRender<T>()` in the static constructor to auto-render when properties change
- Call `InvalidateVisual()` to trigger a re-render
- Use `Pointer.Capture(this)` to track drags across element boundaries

---

## See Also

- [020 — Custom Templated Controls](020-custom-templated-controls.md)
- [023 — Custom Layout Panels](023-custom-layout-panels.md)
- [021E — Custom Controls from Scratch (examples)](021-custom-controls-from-scratch-examples.md)
- [021V — Custom Controls from Scratch (verbose companion)](021-custom-controls-from-scratch-verbose.md)
- [Avalonia Docs: Custom Controls](https://docs.avaloniaui.net/docs/concepts/custom-controls)
