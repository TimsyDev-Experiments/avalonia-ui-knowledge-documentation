---
tier: advanced
topic: layout
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 023-custom-layout-panels.md
---

# 023E — Custom Layout Panels: Real-World Examples

**Applies to:** [023 — Custom Layout Panels](023-custom-layout-panels.md) | [023V — In-Depth Companion](023-custom-layout-panels-verbose.md)

---

## Example 1: MasonryPanel

### Goal

A panel that arranges child elements in a masonry (Pinterest-style) layout. Items have varying heights but a fixed column width. The panel places each item in the column with the least total height. The number of columns is configurable or computed from the available width and a `ColumnWidth` property.

### ViewModel

```csharp
// ViewModels/GalleryViewModel.cs
using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class CardItemViewModel : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private Color _accentColor = Colors.Gray;
    [ObservableProperty] private double _cardHeight = 100;
}

public partial class GalleryViewModel : ObservableObject
{
    public ObservableCollection<CardItemViewModel> Cards { get; } = [];
}
```

### XAML View

```xml
<!-- Views/GalleryView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:GalleryViewModel">
  <ScrollViewer>
    <controls:MasonryPanel ColumnWidth="240"
                            HorizontalAlignment="Center">
      <ItemsControl ItemsSource="{Binding Cards}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <controls:MasonryPanel ColumnWidth="240" />
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:CardItemViewModel">
            <Border Background="{Binding AccentColor}"
                    CornerRadius="8" Margin="4"
                    Height="{Binding CardHeight}">
              <StackPanel Margin="12">
                <TextBlock Text="{Binding Title}"
                           FontWeight="Bold" />
                <TextBlock Text="{Binding Description}"
                           TextWrapping="Wrap" />
              </StackPanel>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </controls:MasonryPanel>
  </ScrollViewer>
</UserControl>
```

### How It Works

1. `MasonryPanel` extends `Panel`. It exposes `ColumnWidthProperty` (`double`, default 200) and `ColumnSpacingProperty` (`double`, default 8).
2. `MeasureOverride` computes the number of columns from `availableSize.Width`: `var columns = Math.Max(1, (int)((availableSize.Width + ColumnSpacing) / (ColumnWidth + ColumnSpacing)))`. Each child is measured with a constraint of `(ColumnWidth, availableSize.Height)`.
3. `ArrangeOverride` maintains an array of column heights. For each child, it finds the column index with the smallest height, arranges the child at that column's X position and current Y offset, and adds the child's height (plus spacing) to that column's accumulator.
4. `AffectsMeasure<MasonryPanel>(ColumnWidthProperty, ColumnSpacingProperty)` is registered in the static constructor.

```csharp
// Controls/MasonryPanel.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MyApp.Controls;

public class MasonryPanel : Panel
{
    public static readonly StyledProperty<double> ColumnWidthProperty =
        AvaloniaProperty.Register<MasonryPanel, double>(nameof(ColumnWidth), 200);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<MasonryPanel, double>(nameof(ColumnSpacing), 8);

    public double ColumnWidth
    {
        get => GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, Math.Max(50, value));
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, Math.Max(0, value));
    }

    static MasonryPanel()
    {
        AffectsMeasure<MasonryPanel>(ColumnWidthProperty, ColumnSpacingProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var columnWidth = ColumnWidth;
        var spacing = ColumnSpacing;
        var columns = Math.Max(1, (int)((availableSize.Width + spacing) / (columnWidth + spacing)));

        if (double.IsInfinity(availableSize.Width))
            columns = 1;

        var childConstraint = new Size(columnWidth, availableSize.Height);
        var columnHeights = new double[columns];

        foreach (var child in Children)
        {
            child.Measure(childConstraint);
            var col = Array.IndexOf(columnHeights, columnHeights.Min());
            columnHeights[col] += child.DesiredSize.Height + spacing;
        }

        var totalHeight = columnHeights.Length > 0 ? columnHeights.Max() : 0;
        return new Size(columns * (columnWidth + spacing) - spacing, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columnWidth = ColumnWidth;
        var spacing = ColumnSpacing;
        var columns = Math.Max(1, (int)((finalSize.Width + spacing) / (columnWidth + spacing)));

        if (double.IsInfinity(finalSize.Width))
            columns = 1;

        var columnHeights = new double[columns];

        foreach (var child in Children)
        {
            var col = Array.IndexOf(columnHeights, columnHeights.Min());
            var x = col * (columnWidth + spacing);
            var y = columnHeights[col];
            child.Arrange(new Rect(x, y, columnWidth, child.DesiredSize.Height));
            columnHeights[col] += child.DesiredSize.Height + spacing;
        }

        return finalSize;
    }
}
```

### Design Decisions

- **Fill-shortest-column algorithm.** This produces a balanced layout without sorting items by height. The first item always goes in column 0, the second in column 1, etc., which gives the user control over relative ordering while minimizing height differences.
- **`ColumnWidth` as a fixed value.** Variable column widths (each column adapts to its content) would require a different algorithm (like `MultiColumnPanel`). Fixed-width masonry is simpler and matches the common Pinterest/Unsplash layout.
- **No `RowSpacing` separate from `ColumnSpacing`.** A single `Spacing` property applies to both axes. If vertical and horizontal gaps need to differ, add a second property or defer to the consumer's margin on each child.

### Edge Cases

- **Fewer children than columns.** Unused columns get no items and contribute no height. The panel returns the height of the tallest column as its desired size.
- **Zero or negative `ColumnWidth`.** Clamp to a minimum of 50px in the property setter to avoid division by zero.
- **`availableSize.Width` is `Infinity`** (e.g., inside a horizontal `StackPanel`). The panel cannot compute columns from infinite width. Fall back to a single column or a hardcoded default like 800px. Better: document that `MasonryPanel` should be used inside a `ScrollViewer` or a `Grid` row with constrained width.
- **Child `Margin`.** MasonryPanel does **not** include child margin in column height tracking. Each child's final arrange size comes from `DesiredSize`, which already includes margin. The column accumulator adds `DesiredSize.Height + Spacing`.
- **Dynamic child height changes.** The panel's `InvalidateMeasure()` is not automatically triggered when a child's height changes (e.g., via binding). Wire `EffectiveViewportChanged` or have the ViewModel notify via an event that re-measures the panel.

---

## Example 2: AccordionPanel

### Goal

A vertical panel where items stack with a gap, but each item can be "expanded" to occupy more vertical space. Only one item is expanded at a time. Expanding an item pushes subsequent items down via a smooth layout transition. The panel exposes `ExpandedIndexProperty` and animates size changes.

### ViewModel

```csharp
// ViewModels/FaqViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class FaqItemViewModel : ObservableObject
{
    [ObservableProperty] private string _question = string.Empty;
    [ObservableProperty] private string _answer = string.Empty;
    [ObservableProperty] private bool _isExpanded;
}

public partial class FaqViewModel : ObservableObject
{
    public ObservableCollection<FaqItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private int _expandedIndex = -1;

    partial void OnExpandedIndexChanged(int value)
    {
        for (int i = 0; i < Items.Count; i++)
            Items[i].IsExpanded = i == value;
    }
}
```

### XAML View

```xml
<!-- Views/FaqView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:FaqViewModel">
  <ScrollViewer>
    <controls:AccordionPanel ExpandedIndex="{Binding ExpandedIndex, Mode=TwoWay}"
                              ItemSpacing="4">
      <ItemsControl ItemsSource="{Binding Items}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <controls:AccordionPanel ExpandedIndex="{Binding $parent[ItemsControl].DataContext.ExpandedIndex, Mode=TwoWay}"
                                      ItemSpacing="4" />
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate x:DataType="vm:FaqItemViewModel">
            <Border Background="#f0f0f0" CornerRadius="6"
                    Padding="12">
              <StackPanel>
                <TextBlock Text="{Binding Question}"
                           FontWeight="Bold"
                           PointerPressed="OnQuestionClicked" />
                <TextBlock Text="{Binding Answer}"
                           TextWrapping="Wrap"
                           IsVisible="{Binding IsExpanded}"
                           Margin="0,8,0,0" />
              </StackPanel>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </controls:AccordionPanel>
  </ScrollViewer>
</UserControl>
```

### How It Works

1. `AccordionPanel` extends `Panel`. It exposes `ExpandedIndexProperty` (`StyledProperty<int>`, `defaultBindingMode: TwoWay`, default -1) and `ItemSpacingProperty` (`StyledProperty<double>`, default 0).
2. `MeasureOverride` iterates children. For the expanded child, it passes `availableSize.Height` as the constraint (let it be as tall as it needs). For collapsed children, it constrains height to the collapsed size (e.g., the header-only height). The panel returns the sum of all child heights plus spacing.
3. `ArrangeOverride` positions children vertically. The expanded child gets its full desired height; collapsed children get their header-only height. The panel supports `RenderTransform` transitions by applying a `TranslateTransform` to children that shift position when the expanded index changes.
4. When `ExpandedIndex` changes, the panel notifies the layout system via `InvalidateMeasure()`. Children below the expanded item will be pushed down.

```csharp
// Controls/AccordionPanel.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace MyApp.Controls;

public class AccordionPanel : Panel
{
    private int _collapsedChildCount;

    public static readonly StyledProperty<int> ExpandedIndexProperty =
        AvaloniaProperty.Register<AccordionPanel, int>(nameof(ExpandedIndex), -1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> ItemSpacingProperty =
        AvaloniaProperty.Register<AccordionPanel, double>(nameof(ItemSpacing), 0);

    public int ExpandedIndex
    {
        get => GetValue(ExpandedIndexProperty);
        set => SetValue(ExpandedIndexProperty, value);
    }

    public double ItemSpacing
    {
        get => GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, Math.Max(0, value));
    }

    static AccordionPanel()
    {
        AffectsMeasure<AccordionPanel>(ExpandedIndexProperty, ItemSpacingProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var expanded = ExpandedIndex;
        var spacing = ItemSpacing;
        double totalHeight = 0;
        _collapsedChildCount = 0;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (i == expanded)
            {
                child.Measure(availableSize);
            }
            else
            {
                child.Measure(new Size(availableSize.Width, 0));
                _collapsedChildCount++;
            }

            totalHeight += child.DesiredSize.Height;
            if (i < Children.Count - 1)
                totalHeight += spacing;
        }

        return new Size(availableSize.Width, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var expanded = ExpandedIndex;
        var spacing = ItemSpacing;
        double y = 0;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childHeight = child.DesiredSize.Height;
            child.Arrange(new Rect(0, y, finalSize.Width, childHeight));
            y += childHeight + spacing;
        }

        return finalSize;
    }
}
```

### Design Decisions

- **`ExpandedIndex` on the panel, not per-item.** The panel tracks which item is expanded. This enforces the single-expansion constraint at the layout level. The ViewModel mirrors this via `OnExpandedIndexChanged` for per-item state (e.g., showing/hiding answer text).
- **Header-only size for collapsed items.** The panel measures each collapsed child only for its minimum (header) size. The consumer controls the collapsed appearance via `IsExpanded` bindings on inner elements; the panel controls the layout slot.
- **Smooth expansion.** The panel does not animate directly. Instead, it applies a `RenderTransform` transition on each child via the `Transitions` property. Consumers add `<DoubleTransition Property="RenderTransform.TranslateY" Duration="0:0:0.2" />` on child elements for automatic animation.

### Edge Cases

- **`ExpandedIndex` set to -1 (nothing expanded).** All children are measured and arranged at their header-only height. The panel returns the sum of header heights.
- **`ExpandedIndex` set to a value >= `Children.Count`.** Clamp to `Children.Count - 1` in the property setter. If `Children.Count` is 0, ignore the set.
- **Rapid expansion switching.** Each `ExpandedIndex` change triggers `InvalidateMeasure()`. The layout system processes these sequentially. No special debouncing is needed at the panel level, but the consumer should avoid setting the index faster than the layout pass completes.
- **Child removal during expanded state.** If the expanded child is removed from `Children`, set `ExpandedIndex` to 0 (or -1 if now empty) and re-measure.

---

## What These Examples Demonstrate

| Aspect | MasonryPanel | AccordionPanel |
|---|---|---|
| **Layout axis** | Horizontal (multiple columns, short-column fill) | Vertical (single column, expanded + collapsed) |
| **Algorithm** | Shortest-column greedy | Fixed-position with one variable-height slot |
| **Property that affects layout** | `ColumnWidth`, `ColumnSpacing` | `ExpandedIndex`, `ItemSpacing` |
| **Measure strategy** | Uniform child width, variable height per item | Two modes: full-height (expanded) vs header-only (collapsed) |
| **Smooth transitions** | Not applicable (layout is static once arranged) | Consumer adds `DoubleTransition` on children |
| **Dynamic child changes** | Height changes require external invalidation | `ExpandedIndex` change triggers `InvalidateMeasure` |

---

## See Also

- [023 — Custom Layout Panels](023-custom-layout-panels.md)
- [023V — Custom Layout Panels (verbose companion)](023-custom-layout-panels-verbose.md)
- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — Control base patterns used by Panel
- [024 — Animation & Transitions](024-animation-transitions.md) — `RenderTransform` transitions for smooth layout changes
- [030 — Layout Measure/Arrange & Custom Controls](../../references/30-layout-measure-arrange-and-custom-layout-controls.md) (plugin ref)
- [Avalonia Docs: Custom Layout](https://docs.avaloniaui.net/docs/layout/custom-layout)
