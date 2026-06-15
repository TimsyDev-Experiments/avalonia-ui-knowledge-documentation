---
tier: advanced
topic: layout
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 023 — Custom Layout Panels

**What you'll learn:** Create a custom panel with `MeasureOverride` and `ArrangeOverride`, implement a wrap panel and a radial panel.

**Prerequisites:** [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)

---

## 1. The wrap panel

```csharp
// Controls/WrapPanel.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MyApp.Controls;

public class WrapPanel : Panel
{
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemWidth), double.NaN);

    public static readonly StyledProperty<double> ItemHeightProperty =
        AvaloniaProperty.Register<WrapPanel, double>(nameof(ItemHeight), double.NaN);

    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }
```

---

## 2. MeasureOverride

```csharp
    protected override Size MeasureOverride(Size availableSize)
    {
        var availableWidth = availableSize.Width;
        var childAvailable = new Size(
            double.IsNaN(ItemWidth) ? availableWidth : ItemWidth,
            double.IsNaN(ItemHeight) ? double.PositiveInfinity : ItemHeight);

        var totalWidth = 0.0;
        var totalHeight = 0.0;
        var rowHeight = 0.0;
        var rowWidth = 0.0;

        foreach (Control child in Children)
        {
            child.Measure(childAvailable);

            var childWidth = double.IsNaN(ItemWidth) ? child.DesiredSize.Width : ItemWidth;
            var childHeight = double.IsNaN(ItemHeight) ? child.DesiredSize.Height : ItemHeight;

            // Wrap to next row if needed
            if (rowWidth + childWidth > availableWidth)
            {
                totalHeight += rowHeight;
                rowWidth = 0;
                rowHeight = 0;
            }

            rowWidth += childWidth;
            rowHeight = Math.Max(rowHeight, childHeight);
            totalWidth = Math.Max(totalWidth, rowWidth);
        }

        totalHeight += rowHeight;

        return new Size(totalWidth, totalHeight);
    }
```

---

## 3. ArrangeOverride

```csharp
    protected override Size ArrangeOverride(Size finalSize)
    {
        var childHeight = double.IsNaN(ItemHeight)
            ? Children.Count > 0 ? Children[0].DesiredSize.Height : 0
            : ItemHeight;

        var x = 0.0;
        var y = 0.0;
        var rowHeight = 0.0;

        foreach (Control child in Children)
        {
            var childWidth = double.IsNaN(ItemWidth) ? child.DesiredSize.Width : ItemWidth;

            if (x + childWidth > finalSize.Width)
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
            }

            child.Arrange(new Rect(x, y, childWidth, childHeight));
            rowHeight = Math.Max(rowHeight, childHeight);
            x += childWidth;
        }

        return finalSize;
    }
}
```

---

## 4. Usage

```xml
<controls:WrapPanel ItemWidth="120" ItemHeight="80">
  <Border Background="Red" />
  <Border Background="Green" />
  <Border Background="Blue" />
  <!-- more children -->
</controls:WrapPanel>
```

---

## 5. Radial panel — measure

```csharp
// Controls/RadialPanel.cs
public class RadialPanel : Panel
{
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<RadialPanel, double>(nameof(Radius), 100);

    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var childSize = new Size(40, 40);

        foreach (Control child in Children)
            child.Measure(childSize);

        return new Size(Radius * 2 + 40, Radius * 2 + 40);
    }
```

---

## 6. Radial panel — arrange

```csharp
    protected override Size ArrangeOverride(Size finalSize)
    {
        var center = new Point(finalSize.Width / 2, finalSize.Height / 2);
        var count = Children.Count;

        if (count == 0) return finalSize;

        var angleStep = 2 * Math.PI / count;

        for (int i = 0; i < count; i++)
        {
            var angle = i * angleStep - Math.PI / 2; // start at top
            var x = center.X + Radius * Math.Cos(angle) - 20;
            var y = center.Y + Radius * Math.Sin(angle) - 20;

            Children[i].Arrange(new Rect(x, y, 40, 40));
        }

        return finalSize;
    }
}
```

---

## Layout system rules

| Override | Purpose | Called when |
|---|---|---|
| `MeasureOverride` | Determine how much space this element needs | Parent arranges, or `InvalidateMeasure()` |
| `ArrangeOverride` | Position children within the given space | After measure, or `InvalidateArrange()` |
| `AffectsMeasure<T>(prop)` | Auto-invalidate measure when a property changes | Static constructor |
| `AffectsArrange<T>(prop)` | Auto-invalidate arrange when a property changes | Static constructor |

---

## Key Takeaways

- `MeasureOverride` asks children how much space they want
- `ArrangeOverride` tells children where they go
- Use `double.IsNaN` for "auto-size" properties
- Register layout-affecting properties with `AffectsMeasure`/`AffectsArrange`
- Always call `child.Measure()` before `child.Arrange()`

---

## See Also

- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — measure/arrange patterns in depth
- [023E — Custom Layout Panels (examples)](023-custom-layout-panels-examples.md)
- [023V — Custom Layout Panels (verbose companion)](023-custom-layout-panels-verbose.md)
- [Avalonia Docs: Custom Layout](https://docs.avaloniaui.net/docs/layout/custom-layout)
