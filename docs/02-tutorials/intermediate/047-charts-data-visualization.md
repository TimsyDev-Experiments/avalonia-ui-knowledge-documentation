---
tier: intermediate
topic: data display
estimated: 15 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# 047 -- Charts & Data Visualization

**What you'll learn:** Add interactive charts to an Avalonia app using LiveCharts2, bind chart data to ViewModels, and customize axes, tooltips, and legends.

**Prerequisites:** [009 -- Data Templates Basics](../basics/009-data-templates-basics.md), [015 -- Item Lists](015-item-lists.md)

---

## 1. LiveCharts2 setup

```shell
dotnet add package LiveChartsCore.SkiaSharpView.Avalonia
dotnet add package LiveChartsCore.SkiaSharpView.WinUI  # not needed for desktop
```

The first package pulls in SkiaSharp as the rendering backend automatically.

Register the control in `App.axaml`:

```xml
<!-- App.axaml -->
<Application xmlns:lvc="using:LiveChartsCore.SkiaSharpView.Avalonia"
             ...>
```

## 2. Basic line chart

```xml
<lvc:CartesianChart Series="{Binding Series}"
                    XAxes="{Binding XAxes}"
                    YAxes="{Binding YAxes}" />
```

```csharp
public partial class DashboardViewModel : ObservableObject
{
    public ISeries[] Series { get; } =
    {
        new LineSeries<double>
        {
            Values = new double[] { 2, 5, 3, 8, 6, 9 },
            Name = "Revenue",
            Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 },
            Fill = null,
            GeometrySize = 8,
        }
    };

    public Axis[] XAxes { get; } =
    {
        new Axis { Name = "Month", Labels = new[] { "Jan","Feb","Mar","Apr","May","Jun" } }
    };

    public Axis[] YAxes { get; } =
    {
        new Axis { Name = "Amount ($)" }
    };
}
```

## 3. Column chart

```csharp
public ISeries[] ColumnSeries { get; } =
{
    new ColumnSeries<int>
    {
        Values = new[] { 12, 19, 8, 15 },
        Name = "Q1 Sales",
        Fill = new SolidColorPaint(SKColors.Orange),
    }
};
```

```xml
<lvc:CartesianChart Series="{Binding ColumnSeries}" />
```

## 4. Pie / donut chart

```csharp
public ISeries[] PieSeries { get; } =
{
    new PieSeries<int> { Values = new[] { 45 }, Name = "Windows",  HoverPushout = 10 },
    new PieSeries<int> { Values = new[] { 30 }, Name = "Linux",    HoverPushout = 10 },
    new PieSeries<int> { Values = new[] { 25 }, Name = "macOS",    HoverPushout = 10 },
};
```

```xml
<lvc:PieChart Series="{Binding PieSeries}" />
```

## 5. Observable data (live updating)

```csharp
public partial class LiveChartViewModel : ObservableObject
{
    public ObservableCollection<ObservablePoint> Points { get; } = new();

    public ISeries[] Series { get; }

    public LiveChartViewModel()
    {
        // Seed data
        var rng = new Random();
        for (int i = 0; i < 20; i++)
            Points.Add(new ObservablePoint(i, rng.NextDouble() * 100));

        Series = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = Points,
                Mapping = (point, index) => new(point.X, point.Y),
            }
        };
    }

    [RelayCommand]
    private void AddDataPoint()
    {
        var rng = new Random();
        Points.Add(new ObservablePoint(Points.Count, rng.NextDouble() * 100));
    }
}
```

`ObservablePoint` implements `INotifyPropertyChanged`, so the chart re-renders automatically when its properties change.

## 6. Axes customization

```csharp
public Axis[] XAxes { get; } =
{
    new Axis
    {
        Name = "Time",
        NameTextSize = 14,
        TextSize = 12,
        LabelsRotation = 45,
        MinLimit = 0,
        MaxLimit = 100,
        Labeler = value => value.ToString("F1"),
    }
};
```

## 7. Tooltips and legend

```xml
<lvc:CartesianChart Series="{Binding Series}"
                    TooltipPosition="Top"
                    TooltipBackgroundPaint="{lvc:SolidColorPaint #cc1e1e2e}"
                    TooltipTextPaint="{lvc:SolidColorPaint #cdd6f4}">
  <lvc:CartesianChart.Legend>
    <lvc:Legend TextPaint="{lvc:SolidColorPaint Gray}" />
  </lvc:CartesianChart.Legend>
</lvc:CartesianChart>
```

## 8. Multiple series on one chart

```csharp
public ISeries[] MultiSeries { get; } =
{
    new LineSeries<double>
    {
        Values = new double[] { 3, 7, 2, 9, 4 },
        Name = "Forecast",
        Stroke = new SolidColorPaint(SKColors.OrangeRed) { StrokeThickness = 2 },
        Fill = null,
        GeometrySize = 0,
    },
    new ColumnSeries<double>
    {
        Values = new double[] { 2, 6, 3, 8, 5 },
        Name = "Actual",
    }
};
```

## Key takeaways

- `LiveChartsCore.SkiaSharpView.Avalonia` is the only NuGet package needed
- Use `ObservablePoint` or implement `INotifyPropertyChanged` for live-updating charts
- `LineSeries`, `ColumnSeries`, and `PieSeries` cover most common chart types
- Axes support `Labels`, `MinLimit`/`MaxLimit`, rotation, and custom `Labeler` functions
- Tooltip and legend are fully customizable via paint properties
- Use `Mapping` lambdas to bind chart data from custom model types

---

## See Also

- [LiveCharts2 Docs](https://livecharts.dev/)
- [036 -- Virtualization & Large List Performance](../advanced/036-virtualization-large-lists.md)
- [015 -- Item Lists](015-item-lists.md)
- [047V -- Charts & Data Visualization (verbose companion)](047-charts-data-visualization-verbose.md)
- [047X -- Charts & Data Visualization (examples)](047-charts-data-visualization-examples.md)
