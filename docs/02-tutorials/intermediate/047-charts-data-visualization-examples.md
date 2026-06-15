---
tier: intermediate
topic: data display
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 047-charts-data-visualization.md
---

# 047X — Charts & Data Visualization: Real-World Examples

**What you'll build:** A real-time sensor dashboard that updates every second with live data, and an interactive sales report that switches between column and pie breakdown views.

**Prerequisites:** [047 — Charts & Data Visualization](047-charts-data-visualization.md). The [verbose companion](047-charts-data-visualization-verbose.md) covers rendering internals, axis mechanics, and performance with large datasets.

---

## Example 1: Real-Time Sensor Dashboard

**Goal:** Display three live sensor readings (temperature, humidity, pressure) as line charts that scroll new data every second, with auto-scaling Y axes.

### ViewModel

```csharp
// ViewModels/SensorDashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace MyApp.ViewModels;

public partial class SensorDashboardViewModel : ObservableObject, IDisposable
{
    private readonly Timer _timer;
    private readonly Random _rng = new();
    private int _index;

    public ObservableCollection<ObservablePoint> TemperaturePoints { get; } = new();
    public ObservableCollection<ObservablePoint> HumidityPoints { get; } = new();
    public ObservableCollection<ObservablePoint> PressurePoints { get; } = new();

    public ISeries[] TemperatureSeries { get; }
    public ISeries[] HumiditySeries { get; }
    public ISeries[] PressureSeries { get; }

    public Axis[] SharedXAxes { get; }
    public Axis[] TemperatureYAxes { get; }
    public Axis[] HumidityYAxes { get; }
    public Axis[] PressureYAxes { get; }

    [ObservableProperty]
    private bool _isRunning;

    public SensorDashboardViewModel()
    {
        // Seed initial data
        for (int i = 0; i < 20; i++)
        {
            TemperaturePoints.Add(new ObservablePoint(i, 22 + _rng.NextDouble() * 4));
            HumidityPoints.Add(new ObservablePoint(i, 45 + _rng.NextDouble() * 15));
            PressurePoints.Add(new ObservablePoint(i, 1013 + _rng.NextDouble() * 5 - 2.5));
        }
        _index = 20;

        TemperatureSeries = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = TemperaturePoints,
                Stroke = new SolidColorPaint(SKColors.Tomato) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Tomato.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Mapping = (p, _) => new(p.X, p.Y),
            }
        };

        HumiditySeries = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = HumidityPoints,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Mapping = (p, _) => new(p.X, p.Y),
            }
        };

        PressureSeries = new ISeries[]
        {
            new LineSeries<ObservablePoint>
            {
                Values = PressurePoints,
                Stroke = new SolidColorPaint(SKColors.LimeGreen) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.LimeGreen.WithAlpha(30)),
                GeometrySize = 0,
                LineSmoothness = 0.3,
                Mapping = (p, _) => new(p.X, p.Y),
            }
        };

        SharedXAxes = new Axis[]
        {
            new Axis
            {
                Labeler = value => value.ToString("N0"),
                MinLimit = 0,
            }
        };

        TemperatureYAxes = new Axis[] { new Axis { Name = "°C", MinLimit = 15, MaxLimit = 30 } };
        HumidityYAxes = new Axis[] { new Axis { Name = "%", MinLimit = 20, MaxLimit = 80 } };
        PressureYAxes = new Axis[] { new Axis { Name = "hPa", MinLimit = 1005, MaxLimit = 1025 } };

        _timer = new Timer(1000);
        _timer.Elapsed += (_, _) => AddSample();
    }

    [RelayCommand]
    private void Toggle()
    {
        if (IsRunning)
        {
            _timer.Stop();
            IsRunning = false;
        }
        else
        {
            _timer.Start();
            IsRunning = true;
        }
    }

    private void AddSample()
    {
        var x = _index++;
        TemperaturePoints.Add(new ObservablePoint(x, 22 + _rng.NextDouble() * 4));
        HumidityPoints.Add(new ObservablePoint(x, 45 + _rng.NextDouble() * 15));
        PressurePoints.Add(new ObservablePoint(x, 1013 + _rng.NextDouble() * 5 - 2.5));

        if (TemperaturePoints.Count > 60)
        {
            TemperaturePoints.RemoveAt(0);
            HumidityPoints.RemoveAt(0);
            PressurePoints.RemoveAt(0);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
```

### View

```xml
<!-- File: Views/SensorDashboardView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:lvc="using:LiveChartsCore.SkiaSharpView.Avalonia"
             x:Class="MyApp.Views.SensorDashboardView"
             x:DataType="vm:SensorDashboardViewModel">

  <DockPanel Margin="12">
    <Button DockPanel.Dock="Top"
            Content="{Binding IsRunning, Converter={StaticResource BoolToStartStop}}"
            Command="{Binding ToggleCommand}"
            Margin="0,0,0,8" />

    <ScrollViewer>
      <StackPanel Spacing="16">
        <lvc:CartesianChart Series="{Binding TemperatureSeries}"
                            XAxes="{Binding SharedXAxes}"
                            YAxes="{Binding TemperatureYAxes}"
                            Height="180"
                            TooltipPosition="Top" />

        <lvc:CartesianChart Series="{Binding HumiditySeries}"
                            XAxes="{Binding SharedXAxes}"
                            YAxes="{Binding HumidityYAxes}"
                            Height="180"
                            TooltipPosition="Top" />

        <lvc:CartesianChart Series="{Binding PressureSeries}"
                            XAxes="{Binding SharedXAxes}"
                            YAxes="{Binding PressureYAxes}"
                            Height="180"
                            TooltipPosition="Top" />
      </StackPanel>
    </ScrollViewer>
  </DockPanel>
</UserControl>
```

### How It Works

1. The ViewModel pre-seeds 20 data points on construction. `ObservableCollection<ObservablePoint>` notifies the chart of both structural changes (add/remove) and property changes on existing points.
2. A `System.Timers.Timer` fires every 1000ms. The callback `AddSample` runs on a thread-pool thread, but `ObservableCollection` changes must happen on the UI thread. The `LineSeries` listens to `INotifyCollectionChanged`, but the timer callback does not touch the dispatcher — this is actually a problem.
3. **Correction**: `ObservableCollection` raises `CollectionChanged` synchronously. If modified off the UI thread, the binding system throws. The timer callback should use `Dispatcher.UIThread.Post`:

```csharp
private void AddSample()
{
    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
        var x = _index++;
        TemperaturePoints.Add(new ObservablePoint(x, 22 + _rng.NextDouble() * 4));
        // ...
    });
}
```

Each chart has its own Y axis with fixed limits (`MinLimit`/`MaxLimit`) so the three scales are comparable over time. The X axis (`SharedXAxes`) is shared with `MinLimit = 0` to prevent negative indices.

4. The 60-point rolling window (`RemoveAt(0)` when count > 60) keeps the chart focused on the last 60 seconds of data.
5. `GeometrySize = 0` and `LineSmoothness = 0.3` keeps rendering fast — no geometry markers and minimal bezier smoothing.

### Key Points

- `ObservableCollection<ObservablePoint>` is the recommended live-data container. Each point individually notifies changes, and collection mutations signal structural changes.
- Fixed axis limits (`MinLimit`/`MaxLimit`) prevent the chart from rescaling on every new data point, which would be visually distracting.
- The rolling window (60 points) bounds memory and rendering cost. Without it, the chart would accumulate points for the entire session.
- The `BoolToStartStop` converter maps `true` → "Stop", `false` → "Start" for the toggle button text.

---

## Example 2: Interactive Sales Report with Drill-Down

**Goal:** Show a quarterly sales summary as a column chart. Clicking a column drills into a pie chart breaking down that quarter's sales by product category.

### ViewModel

```csharp
// ViewModels/SalesReportViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace MyApp.ViewModels;

public partial class SalesReportViewModel : ObservableObject
{
    [ObservableProperty]
    private object _currentChart;

    [ObservableProperty]
    private string _drillDownTitle = "Click a quarter to drill down";

    public ObservableCollection<ISeries> OverviewSeries { get; }
    public Axis[] OverviewXAxes { get; }
    public Axis[] OverviewYAxes { get; }

    // Drill-down state
    public ObservableCollection<ISeries> DetailSeries { get; }
    public Axis[] DetailYAxes { get; }

    public SalesReportViewModel()
    {
        // Overview: column chart
        OverviewSeries = new ObservableCollection<ISeries>
        {
            new ColumnSeries<double>
            {
                Values = new double[] { 42000, 38000, 51000, 47000 },
                Name = "Revenue",
                Fill = new SolidColorPaint(SKColors.SteelBlue),
                Stroke = null,
            }
        };

        OverviewXAxes = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "Q1", "Q2", "Q3", "Q4" },
                LabelsRotation = 0,
            }
        };

        OverviewYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Revenue ($)",
                Labeler = value => $"${value:N0}",
                MinLimit = 0,
            }
        };

        // Detail: pie chart (initial hidden)
        DetailSeries = new ObservableCollection<ISeries>();
        DetailYAxes = new Axis[] { new Axis { MinLimit = 0 } };

        CurrentChart = new ChartState { IsOverview = true };
    }

    [RelayCommand]
    private void SelectQuarter(int quarterIndex)
    {
        var breakdownData = quarterIndex switch
        {
            0 => new[] { 18000, 12000, 8000, 4000 },   // Q1
            1 => new[] { 15000, 11000, 7000, 5000 },   // Q2
            2 => new[] { 22000, 15000, 9000, 5000 },   // Q3
            3 => new[] { 19000, 14000, 8000, 6000 },   // Q4
            _ => throw new ArgumentOutOfRangeException(nameof(quarterIndex)),
        };

        var labels = new[] { "Hardware", "Software", "Services", "Licensing" };
        var colors = new[]
        {
            SKColors.DodgerBlue,
            SKColors.Orange,
            SKColors.LimeGreen,
            SKColors.Tomato,
        };

        DetailSeries.Clear();
        for (int i = 0; i < breakdownData.Length; i++)
        {
            DetailSeries.Add(new PieSeries<int>
            {
                Values = new[] { breakdownData[i] },
                Name = labels[i],
                Fill = new SolidColorPaint(colors[i]),
                HoverPushout = 8,
            });
        }

        DrillDownTitle = $"Q{quarterIndex + 1} Breakdown by Category";
        CurrentChart = new ChartState { IsOverview = false };
    }

    [RelayCommand]
    private void BackToOverview()
    {
        DrillDownTitle = "Click a quarter to drill down";
        CurrentChart = new ChartState { IsOverview = true };
    }

    public record ChartState
    {
        public bool IsOverview { get; init; }
    }
}
```

### View

```xml
<!-- File: Views/SalesReportView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:lvc="using:LiveChartsCore.SkiaSharpView.Avalonia"
             x:Class="MyApp.Views.SalesReportView"
             x:DataType="vm:SalesReportViewModel">

  <DockPanel Margin="16">
    <StackPanel DockPanel.Dock="Top" Spacing="6" Margin="0,0,0,12">
      <TextBlock Text="Sales Report" FontSize="20" FontWeight="Bold" />
      <TextBlock Text="{Binding DrillDownTitle}" FontSize="13" Foreground="Gray" />
      <Button Content="Back to Overview"
              Command="{Binding BackToOverviewCommand}"
              IsVisible="{Binding CurrentChart.IsOverview, Converter={StaticResource InvertBool}}" />
    </StackPanel>

    <!-- Overview: column chart -->
    <lvc:CartesianChart Series="{Binding OverviewSeries}"
                        XAxes="{Binding OverviewXAxes}"
                        YAxes="{Binding OverviewYAxes}"
                        Height="300"
                        IsVisible="{Binding CurrentChart.IsOverview}"
                        TooltipPosition="Top">
      <lvc:CartesianChart.Legend>
        <lvc:Legend TextPaint="{lvc:SolidColorPaint Gray}" />
      </lvc:CartesianChart.Legend>
    </lvc:CartesianChart>

    <!-- Detail: pie chart -->
    <lvc:PieChart Series="{Binding DetailSeries}"
                  Height="300"
                  IsVisible="{Binding CurrentChart.IsOverview, Converter={StaticResource InvertBool}}">
      <lvc:PieChart.Legend>
        <lvc:Legend TextPaint="{lvc:SolidColorPaint Gray}" />
      </lvc:PieChart.Legend>
    </lvc:PieChart>
  </DockPanel>
</UserControl>
```

### How It Works

1. The overview shows a `ColumnSeries` with four quarters. Each column's height represents total revenue.
2. `CurrentChart.IsOverview` is a boolean flag. When true, the column chart is visible and the pie chart is hidden. When false (after drill-down), the pie chart is visible and a "Back to Overview" button appears.
3. `SelectQuarter` is called by a chart interaction handler (chart click events are not shown — in practice you'd use `ChartPointPointerDown` event on the series). It replaces `DetailSeries` with four `PieSeries`, one per category.
4. Each `PieSeries` has exactly one value — this is the LiveCharts2 idiom: one `PieSeries` per slice. The `HoverPushout = 8` creates an exploded effect on hover.
5. `BackToOverview` toggles `CurrentChart.IsOverview` back to true, hiding the pie chart and showing the column chart.
6. The `InvertBool` converter (not shown) flips the `IsOverview` bool for the hidden-state bindings.

### Key Points

- The two-chart approach (column for overview, pie for detail) is simpler than trying to transform a single chart control between types. The `IsVisible` toggle switches between them.
- Each `PieSeries` is one slice. Setting `Values` to a single-element array is the correct pattern — do not put multiple values in one series.
- The `BackToOverview` command resets `DrillDownTitle` and toggles the visibility flag. The ViewModel state is clean enough that no "reset detail data" step is needed — the pie chart's series are replaced on each drill-down anyway.
- Edge case: if the user clicks the same quarter twice, `SelectQuarter` replaces `DetailSeries` with identical data. `ObservableCollection` fires `CollectionChanged` even for replacements, so the chart re-renders.
- Edge case: the column chart uses `SolidColorPaint` for fill and `null` for stroke. Without setting `Stroke = null`, the columns have a default thin stroke that may not match the design intent.

---

## What These Examples Demonstrate

| Scenario | Chart types | Key technique |
|---|---|---|
| Sensor dashboard | Line charts (3×) | Live data push, rolling window, fixed axis limits, per-second timer |
| Sales report | Column + Pie with drill-down | View switching, per-slice series, user-driven state toggling |

The sensor dashboard exercises real-time data flow and axis management — three charts staying synchronized on the same X axis. The sales report shows navigation between chart types driven by user interaction, with each quarter's data rendered as separated `PieSeries`.

## See Also

- [047 — Charts & Data Visualization](047-charts-data-visualization.md)
- [047V — Verbose Companion](047-charts-data-visualization-verbose.md)
- [LiveCharts2 Docs](https://livecharts.dev/)
- [015 — Item Lists](015-item-lists.md)
