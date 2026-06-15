---
tier: intermediate
topic: data display
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 047-charts-data-visualization.md
---

# 047V — Charts & Data Visualization: An In-Depth Companion

**What you'll learn in this companion:** Not just how to display a chart, but how LiveCharts2 renders visuals via SkiaSharp, the difference between `ISeries` implementations, why `ObservablePoint` exists, how axis limits interact with data, how tooltip positioning works, and when to use pie vs column vs line charts.

**Prerequisites:** [009 — Data Templates Basics](../basics/009-data-templates-basics.md), [015 — Item Lists](015-item-lists.md)

**You should already have read:** [047 — Charts & Data Visualization](047-charts-data-visualization.md) for the quick-start version. This file goes deeper on every section.

---

## 1. LiveCharts2 Rendering Pipeline — What Happens When You Bind Series

### The SkiaSharp dependency

LiveCharts2 does not use Avalonia's built-in controls for rendering chart visuals. Instead, it uses `SkiaSharp` — a cross-platform 2D graphics library — to draw directly to a bitmap surface. The `CartesianChart` and `PieChart` controls are lightweight containers that host a SkiaSharp canvas.

When you bind `Series`, the following happens:

1. The chart control creates an `SKCanvas` surface sized to its bounds
2. LiveCharts2's paint loop calls each series' `MeasuredDraw` method
3. Each series measures its data, calculates axes ranges, and draws lines/bars/pies using SkiaSharp APIs
4. The canvas is invalidated whenever a bound property changes (e.g., `Values` collection changes, or an `ObservablePoint` property changes)
5. The invalidation triggers `InvalidateVisual()` on the Avalonia control, scheduling a re-render

This means charts run at the compositor's frame rate — typically 60 FPS. The entire chart is a single visual, not a tree of Avalonia controls. You cannot style individual chart elements with Avalonia styles or templates.

### The NuGet package

```shell
dotnet add package LiveChartsCore.SkiaSharpView.Avalonia
```

This single package pulls in:
- `LiveChartsCore` — the core library (series types, axes, tooltips)
- `SkiaSharp` — rendering backend
- `LiveChartsCore.SkiaSharpView` — SkiaSharp-specific implementations (paints, geometries)
- `LiveChartsCore.SkiaSharpView.Avalonia` — Avalonia controls (`CartesianChart`, `PieChart`, etc.)

The WinUI package shown in the tutorial (`LiveChartsCore.SkiaSharpView.WinUI`) is a redundant inclusion — it is not needed for Avalonia desktop apps. Remove it if added by mistake; it imports WinUI-specific assemblies that serve no purpose on a desktop Avalonia project.

---

## 2. ISeries Contract — Why Arrays Not ObservableCollections in the Basics

```csharp
public ISeries[] Series { get; } =
{
    new LineSeries<double>
    {
        Values = new double[] { 2, 5, 3, 8, 6, 9 },
        ...
    }
};
```

`ISeries` is the interface for a single data series. The `Series` property on `CartesianChart` accepts `IEnumerable<ISeries>`. Using an array is fine when the series collection does not change at runtime — the chart reads the array once and subscribes to change notifications on each series' `Values` property.

If you need to add or remove series at runtime (e.g., toggle a line on/off), use `ObservableCollection<ISeries>`:

```csharp
public ObservableCollection<ISeries> DynamicSeries { get; } = new();
```

The chart observes `INotifyCollectionChanged` on the series collection. When you add or remove a series, the chart re-renders automatically.

### Why LineSeries<double> vs LineSeries<int>

LiveCharts2 series are generic: `LineSeries<T>`, `ColumnSeries<T>`, `PieSeries<T>`. The type parameter `T` is the type of individual data points. Use `double` for decimal data, `int` for integer data, `float` for memory-sensitive large datasets.

LiveCharts2 uses `T` for numeric operations like axis scaling. Using `int` means the chart treats values as integers — the Y axis shows integer ticks only. Using `double` shows fractional ticks.

---

## 3. LineSeries Properties — Why Each One Exists

```csharp
new LineSeries<double>
{
    Values = new double[] { 2, 5, 3, 8, 6, 9 },
    Name = "Revenue",
    Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 },
    Fill = null,
    GeometrySize = 8,
}
```

| Property | What it controls | Why you'd change it |
|---|---|---|
| `Name` | Series label in legend and tooltips | Set to a human-readable name |
| `Stroke` | The line itself — color, thickness, dash pattern | Match brand colors or differentiate series |
| `Fill` | The area fill under the line | Set to a semi-transparent color for area chart; `null` for line-only |
| `GeometrySize` | Size of the circle at each data point | `0` for a clean line without markers; larger values for emphasis |
| `GeometryStroke` | Stroke of the geometry circles | Set separately from Stroke if you want different line vs marker appearance |
| `GeometryFill` | Fill of the geometry circles | Match or contrast with the stroke |

### StrokeThickness interaction

The `StrokeThickness` setting on `SolidColorPaint` affects only the stroke thickness. If you also set `Fill` with a `SolidColorPaint`, its properties do not affect the stroke — they control the fill area. The `SolidColorPaint` is reused for both if you don't separate them.

---

## 4. Axis Mechanics — Why Limits, Labels, and Labelers

```csharp
new Axis
{
    Name = "Month",
    Labels = new[] { "Jan","Feb","Mar","Apr","May","Jun" }
}
```

`Axis.Labels` overrides numeric labels with string labels. The chart uses the index of each value as the label selector: value 0 → "Jan", value 1 → "Feb", etc. If you have 10 data points but only 6 labels, the last 4 data points use no label.

### MinLimit and MaxLimit

```csharp
MinLimit = 0,
MaxLimit = 100,
```

By default, LiveCharts2 auto-scales axes to fit the data with some padding. Setting `MinLimit` or `MaxLimit` fixes one or both bounds. Use this when:

- You want to compare multiple charts on the same scale (force all Y axes to 0-100)
- You want to prevent the chart from starting at a negative value
- You want to show a partial range (zoom into a section of data)

Setting only `MinLimit` allows auto-scaling to determine `MaxLimit`. Setting both pins the axis range.

### Labeler function

```csharp
Labeler = value => value.ToString("F1"),
```

The `Labeler` function formats numeric axis labels. The default uses `value.ToString("N0")`. Override for custom formatting — percentages (`"{value:F0}%"`), currency (`"${value:F2}"`), or engineering notation.

### LabelsRotation

```csharp
LabelsRotation = 45,
```

Rotates axis labels by the specified degrees. Useful when you have long category names (like "January 2024") that would overlap at 0 degrees.

---

## 5. Pie / Donut Series — Why Three Series, Not One

```csharp
new PieSeries<int> { Values = new[] { 45 }, Name = "Windows", HoverPushout = 10 },
new PieSeries<int> { Values = new[] { 30 }, Name = "Linux",   HoverPushout = 10 },
new PieSeries<int> { Values = new[] { 25 }, Name = "macOS",   HoverPushout = 10 },
```

This is the most common point of confusion. Each `PieSeries` represents **one slice** of the pie, not one series of multiple slices. The `Values` array on each contains exactly one value.

If you tried to put all values in one series:

```csharp
// Wrong — renders as one multi-segment ring
new PieSeries<int> { Values = new[] { 45, 30, 25 } }
```

This would create a single series with three data points — the same visual as three separate series, but without separate names for each segment.

Use the three-series approach when you need to:
- Set the name per slice (for legend labels)
- Customize colors per slice
- Add per-slice hover effects

Use the single-series approach when:
- You need a legend per data point name
- You want to bind data dynamically from a collection

The single-series pattern uses `PieSeries<ObservableValue>` and a `Mapping` function, or you can construct `ISeries` programmatically from a collection of model objects:

```csharp
public ISeries[] PieFromCollection { get; } =
{
    new PieSeries<SalesData>
    {
        Values = salesDataCollection,
        Mapping = (data, index) => data.Amount,
        Name = "Sales by Region",
    }
};
```

### HoverPushout

`HoverPushout = 10` moves the slice outward by 10 pixels when the mouse hovers over it. This creates the "exploded pie" visual. Set to 0 to disable.

---

## 6. Observable Data — The Mapping Lambda

```csharp
new LineSeries<ObservablePoint>
{
    Values = Points,
    Mapping = (point, index) => new(point.X, point.Y),
}
```

`ObservablePoint` is a LiveCharts2 type that implements `INotifyPropertyChanged`. When you modify `point.X` or `point.Y` on an existing point, the chart re-renders that point.

The `Mapping` lambda tells the chart how to extract coordinates from your data type. For `ObservablePoint`, the default mapping already works — you only need to pass `Mapping` when using custom model types:

```csharp
public class SalesRecord
{
    public DateTime Date { get; set; }
    public double Revenue { get; set; }
}

// Mapping from custom type
new LineSeries<SalesRecord>
{
    Values = salesData,
    Mapping = (record, index) => new(record.Date.Ticks, record.Revenue),
}
```

Without the mapping, LiveCharts2 would try to reflect on `SalesRecord` and fail to find `X` and `Y` properties.

### Why ObservableCollection and not List

If you used `List<ObservablePoint>` instead of `ObservableCollection<ObservablePoint>`, adding new points after initialization would not trigger a chart update. `ObservableCollection` notifies the chart when items are added, removed, or replaced. Individual point modifications still work with `ObservablePoint`'s `INotifyPropertyChanged`, but structural changes (add/remove) require the collection notification.

---

## 7. Tooltips and Legend — Why Paint Properties

```xml
<lvc:CartesianChart Series="{Binding Series}"
                    TooltipPosition="Top"
                    TooltipBackgroundPaint="{lvc:SolidColorPaint #cc1e1e2e}"
                    TooltipTextPaint="{lvc:SolidColorPaint #cdd6f4}">
```

Tooltips in LiveCharts2 are drawn on the SkiaSharp canvas, not as Avalonia popups. This means all styling uses `SolidColorPaint` (a LiveCharts2 type) rather than Avalonia brushes.

The `lvc:SolidColorPaint` markup extension creates a `SolidColorPaint` instance from a hex color string. The hex includes an alpha channel prefix (`#cc` = 80% opacity).

TooltipPosition options:
- `Top` — tooltip appears above the cursor
- `Bottom` — below
- `Left` / `Right` — to the side
- `Auto` — LiveCharts2 chooses based on available space
- `Center` — centered on the nearest data point
- `Hidden` — disable tooltip

### Legend

```xml
<lvc:CartesianChart.Legend>
    <lvc:Legend TextPaint="{lvc:SolidColorPaint Gray}" />
</lvc:CartesianChart.Legend>
```

The legend is an Avalonia control, not a SkiaSharp draw. You can style it with standard Avalonia properties in addition to the LiveCharts2 paint properties. For full control, set `LegendPosition` on the chart — options include `Hidden`, `Top`, `Bottom`, `Left`, `Right`.

---

## 8. Multiple Series — Mixed Types on One Chart

```csharp
public ISeries[] MultiSeries { get; } =
{
    new LineSeries<double> { ... Name = "Forecast", ... },
    new ColumnSeries<double> { ... Name = "Actual", ... }
};
```

LiveCharts2 supports mixing different series types on the same Cartesian chart. The chart shares the same X and Y axes across all series. This is useful for:

- Forecast vs actual (line + column)
- Budget vs spending (column + line)
- Target vs progress (line + area)

When mixing, ensure the value ranges are compatible. A `LineSeries` with values in the thousands and a `ColumnSeries` with values in the ones place on the same Y axis makes the column series invisible.

### Secondary axes

For incompatible ranges, use a secondary Y axis:

```csharp
new LineSeries<double>
{
    Values = ...,
    ScalesYAt = 1,  // Use the secondary axis (index 1)
}

new ColumnSeries<double>
{
    Values = ...,
    ScalesYAt = 0,  // Use the primary axis (index 0)
}
```

Then define both axes in `YAxes`:

```csharp
public Axis[] YAxes { get; } =
{
    new Axis { Name = "Primary (Left)" },
    new Axis { Name = "Secondary (Right)" },
};
```

---

## 9. Performance with Large Datasets

LiveCharts2 uses SkiaSharp for hardware-accelerated rendering, but large datasets (100,000+ points) still require careful handling:

- **Downsample before binding** — reduce the point count to a reasonable number (1000-5000 points is usually fine)
- **Use `float` instead of `double`** — reduces memory per point from 8 to 4 bytes
- **Avoid geometry markers** — set `GeometrySize = 0` for lines with many points
- **Use `LineSmoothness = 0`** — disable bezier smoothing (smooth curves are more expensive to calculate)
- **Prefer `ObservablePoint` over custom types** — reflection on custom model properties adds overhead per point during rendering

```csharp
new LineSeries<float>
{
    Values = downsampledValues,
    GeometrySize = 0,
    LineSmoothness = 0,
}
```

---

## Key Takeaways

- LiveCharts2 renders via SkiaSharp, not Avalonia controls — chart elements cannot be styled with Avalonia styles
- Use `ObservableCollection<ISeries>` for dynamic series addition/removal; arrays for static charts
- Each `PieSeries` is one slice — use one per category, not all data in one series
- `Mapping` lambdas connect custom model types to chart coordinates; `ObservablePoint` works out of the box
- Axis `MinLimit`/`MaxLimit` pin the scale; `Labeler` customizes tick formatting
- Tooltips and legends use `SolidColorPaint` (SkiaSharp) for styling, not Avalonia brushes
- Mixed chart types (line + column) require compatible axis ranges or secondary axes
- Large datasets benefit from downsampling, `float` types, and disabling geometry markers

---

## See Also

- [047 — Charts & Data Visualization (original)](047-charts-data-visualization.md)
- [047X — Charts & Data Visualization (examples)](047-charts-data-visualization-examples.md)
- [LiveCharts2 Docs](https://livecharts.dev/)
- [036 — Virtualization & Large List Performance](../advanced/036-virtualization-large-lists.md)
- [015 — Item Lists](015-item-lists.md)
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
