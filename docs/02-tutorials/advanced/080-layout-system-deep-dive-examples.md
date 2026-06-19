---
tier: advanced
topic: layout
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 080E — Layout System Deep Dive (examples)

## Example 1: Uniform wrap panel

```csharp
public class UniformWrapPanel : Panel
{
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<UniformWrapPanel, double>(nameof(ItemWidth), 100);

    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    static UniformWrapPanel()
    {
        AffectsMeasure<UniformWrapPanel>(ItemWidthProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(ItemWidth, double.PositiveInfinity));
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double x = 0, y = 0, rowH = 0;
        foreach (var child in Children)
        {
            double childW = Math.Min(ItemWidth, finalSize.Width - x);
            child.Arrange(new Rect(x, y, childW, child.DesiredSize.Height));
            rowH = Math.Max(rowH, child.DesiredSize.Height);
            x += childW;
            if (x + ItemWidth > finalSize.Width)
            {
                x = 0;
                y += rowH;
                rowH = 0;
            }
        }
        return finalSize;
    }
}
```

```xml
<local:UniformWrapPanel ItemWidth="150">
  <Button Content="Item 1" />
  <Button Content="Item 2" />
  <Button Content="Item 3" />
</local:UniformWrapPanel>
```

---

## Example 2: EffectiveViewportChanged lazy loading

```csharp
public partial class LazyListBox : ListBox
{
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EffectiveViewportChanged += OnViewportChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        EffectiveViewportChanged -= OnViewportChanged;
    }

    private void OnViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
    {
        foreach (var container in GetRealizedContainers())
        {
            if (container.Bounds.Intersects(e.EffectiveViewport))
                LoadItemData(container);
        }
    }

    private void LoadItemData(Control container) { }
}
```

---

## Example 3: Forcing a layout pass

```csharp
// After adding/removing children, force immediate layout
myPanel.InvalidateMeasure();
myPanel.UpdateLayout(); // blocks until pass completes
```

---

## Example 4: Layout rounding off for animation

```csharp
public class SmoothPanel : Panel
{
    public SmoothPanel()
    {
        UseLayoutRounding = false;
    }
}
```

---

## Example 5: Measuring with infinite constraint

```csharp
public class AutoSizePanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double w = 0, h = 0;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            w = Math.Max(w, child.DesiredSize.Width);
            h = Math.Max(h, child.DesiredSize.Height);
        }
        return new Size(w, h);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
            child.Arrange(new Rect(new Point(), child.DesiredSize));
        return finalSize;
    }
}
```

---

## See Also

- [080 — Layout System Deep Dive (core)](080-layout-system-deep-dive.md)
- [080V — Layout System Deep Dive (verbose)](080-layout-system-deep-dive-verbose.md)
- [080Q — Layout System Deep Dive (quiz)](080-layout-system-deep-dive-quiz.md)
