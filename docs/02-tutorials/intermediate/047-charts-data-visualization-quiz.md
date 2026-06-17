---
tier: intermediate
topic: data display
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 047-charts-data-visualization.md
---

# Quiz — Charts & Data Visualization

```quiz
Q: Which NuGet package is required to add LiveCharts2 charting to an Avalonia desktop application?
A. LiveChartsCore.SkiaSharpView.Avalonia (correct) || Correct — this single package pulls in SkiaSharp as the rendering backend and provides all chart controls (CartesianChart, PieChart, etc.).
B. LiveChartsCore.SkiaSharpView.WinUI || Incorrect — this package is for WinUI applications; it is not needed for Avalonia desktop apps.
C. LiveCharts2.Avalonia || Incorrect — there is no package named LiveCharts2.Avalonia; the correct name includes "SkiaSharpView".
D. SkiaSharp.Views.Avalonia || Incorrect — this package provides SkiaSharp canvas support but does not include the LiveCharts2 charting controls.
Explanation: LiveChartsCore.SkiaSharpView.Avalonia is the single dependency needed; it includes SkiaSharp automatically.
```

```quiz
Q: What makes a chart series update automatically when the underlying data changes at runtime?
A. Using arrays like `double[]` for Values || Incorrect — plain arrays do not notify the chart of changes; the chart captures the values once and never re-reads them.
B. Calling Chart.InvalidateVisual() after each data change || Incorrect — calling InvalidateVisual is not necessary when using observable types; LiveCharts2 reacts to collection and property changes automatically.
C. Using ObservableCollection<T> with INotifyPropertyChanged items, such as ObservablePoint (correct) || Correct — ObservableCollection fires collection change notifications, and ObservablePoint implements INotifyPropertyChanged so the chart re-renders when values or positions change.
D. Setting the Series property to a new array after every change || Incorrect — replacing the entire Series array triggers a full chart rebuild, which is wasteful compared to using observable collections.
Explanation: LiveCharts2 observes INotifyCollectionChanged on the Values collection and INotifyPropertyChanged on individual data points for live updates without manual refresh.
```

```quiz
Q: How do you display two different series types (e.g., a line and a column) on the same CartesianChart?
A. Nest both series inside a single ISeries[] array assigned to the Series property (correct) || Correct — CartesianChart.Series accepts an array of ISeries; you can mix LineSeries<double>, ColumnSeries<int>, and other types in the same array.
B. Stack two CartesianChart controls on top of each other with transparent backgrounds || Incorrect — overlapping controls is fragile and unnecessary; a single chart supports multiple series.
C. Use a CombinedSeries wrapper type || Incorrect — there is no CombinedSeries wrapper; you simply add both series to the Series array.
D. Set the Series property to an aggregate ObservableCollection that merges both types || Incorrect — the Series property expects ISeries[], not a single merged collection.
Explanation: CartesianChart renders all series in the Series array on the same axes, so mixing LineSeries and ColumnSeries in one array produces an overlaid chart.
```

```quiz
Q: Identify the bug in this pie chart series definition:
    public ISeries[] PieSeries { get; } =
    {
        new PieSeries<int> { Values = new[] { 45 }, Name = "Windows" },
        new PieSeries<int> { Values = new[] { 30 }, Name = "Linux" },
        new PieSeries<int> { Values = new[] { 25 }, Name = "macOS" },
    };
A. PieSeries<int> should be PieSeries<double> because percentages have decimals || Incorrect — int values are valid for PieSeries; LiveCharts2 handles both integer and floating-point values.
B. The PieChart control does not accept ISeries[] directly || Incorrect — PieChart has a Series property that accepts ISeries[] just like CartesianChart.
C. The code is correct and will produce a three-segment pie chart (correct) || Correct — each PieSeries with a single value creates a segment; the chart automatically proportions them by value.
D. The HoverPushout property must be set to enable tooltips on segments || Incorrect — HoverPushout is optional and controls the "pop-out" on hover, not tooltips.
Explanation: Each PieSeries with a single-value array represents one pie segment; the chart calculates proportions automatically from the values.
```

```quiz
Q: What does the Mapping lambda in LineSeries<ObservablePoint> do?
A. It transforms the data into a different coordinate system before rendering || Incorrect — Mapping maps the data model's properties to X and Y coordinates for the chart, not a coordinate system transform.
B. It maps each data point's X and Y properties to the chart's axis values (correct) || Correct — Mapping = (point, index) => new(point.X, point.Y) tells LiveCharts2 which properties on the custom type correspond to the X and Y axes.
C. It defines the color mapping for the series stroke || Incorrect — color is set via Stroke and Fill properties, not Mapping.
D. It maps series names to legend entries || Incorrect — legend names are set via the Name property on the series, not Mapping.
Explanation: Mapping is required when using custom data types (like ObservablePoint) so LiveCharts2 knows which properties to use for the X and Y coordinates.
```
