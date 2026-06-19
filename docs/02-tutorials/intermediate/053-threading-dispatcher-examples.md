---
tier: intermediate
topic: threading
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 053-threading-dispatcher.md
---

# 053E — Threading & Dispatcher: Real-World Examples

**What this is:** Two worked examples showing dispatcher usage in real app scenarios. Read [053 — Threading & Dispatcher](053-threading-dispatcher.md) and [053V — Verbose Companion](053-threading-dispatcher-verbose.md) first.

---

## Example 1: Image Batch Processor with Progress

### Goal

Load and process a large batch of images on background threads, reporting progress back to the UI without blocking the window.

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="BatchProcessor.Views.ProcessingView">
  <Grid RowDefinitions="Auto,*,Auto,Auto" Margin="16" Spacing="8">
    <TextBlock Text="Image Batch Processor" FontSize="18" />

    <ListBox Name="ImageList" Grid.Row="1">
      <ListBox.ItemTemplate>
        <DataTemplate>
          <TextBlock Text="{Binding FileName}" />
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>

    <ProgressBar Name="ProgressBar" Grid.Row="2" Minimum="0" Maximum="100" />
    <TextBlock Name="StatusText" Grid.Row="3" />
  </Grid>
</UserControl>
```

### ViewModel

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BatchProcessor.ViewModels;

public partial class ImageItem : ObservableObject
{
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _status = "Pending";
}

public partial class ProcessingViewModel : ObservableObject
{
    public ObservableCollection<ImageItem> Items { get; } = new();

    [ObservableProperty]
    private int _progress;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [RelayCommand]
    private async Task ProcessAllAsync()
    {
        StatusMessage = "Processing...";

        var progress = new Progress<(int completed, string fileName)>(update =>
        {
            // This callback runs on the UI thread via SynchronizationContext
            Progress = (int)((double)update.completed / Items.Count * 100);
            StatusMessage = $"Processed {update.completed} of {Items.Count}";

            var item = Items.FirstOrDefault(i => i.FileName == update.fileName);
            if (item is not null)
                item.Status = "Done";
        });

        await Task.Run(() => ProcessOnBackground(progress));

        StatusMessage = $"Complete — processed {Items.Count} images";
    }

    private void ProcessOnBackground(IProgress<(int, string)> progress)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            // Simulate image processing
            Task.Delay(100).Wait();

            // Report progress — IProgress posts back to captured
            // SynchronizationContext (UI thread)
            progress.Report((i + 1, Items[i].FileName));
        }
    }

    public void LoadFiles(string[] files)
    {
        Items.Clear();
        foreach (var file in files)
        {
            Items.Add(new ImageItem { FileName = System.IO.Path.GetFileName(file) });
        }
    }
}
```

### Key points

- `IProgress<T>` captures the `SynchronizationContext` at creation time and invokes the callback on the UI thread
- Heavy processing runs on `Task.Run` — the UI stays responsive
- The `ProgressBar` and `ListBox` are updated via the automatic UI-thread continuation
- No explicit `Dispatcher.UIThread.Post` needed — `IProgress` handles the marshaling

---

## Example 2: Real-Time Data Feed with DispatcherTimer

### Goal

Display a live data feed (simulated stock ticks) that updates a chart every second. The feed runs on a `DispatcherTimer` for smooth UI updates, with a `System.Timers.Timer` fallback for background data aggregation.

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="LiveFeed.Views.TickerView">
  <Grid RowDefinitions="Auto,Auto,*" Margin="16" Spacing="8">
    <TextBlock Name="TickerSymbol" FontSize="14" Foreground="Gray" />
    <TextBlock Name="CurrentPrice" FontSize="32" FontWeight="Bold"
               Grid.Row="1" />
    <ListBox Name="RecentTicks" Grid.Row="2" />
  </Grid>
</UserControl>
```

### Code-behind with DispatcherTimer

```csharp
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace LiveFeed.Views;

public partial class TickerView : UserControl
{
    private readonly ObservableCollection<TickItem> _ticks = new();
    private readonly DispatcherTimer _uiTimer;
    private readonly System.Timers.Timer _aggregationTimer;
    private readonly Random _rng = new();
    private double _lastPrice = 100.0;
    private double _totalChange;

    public TickerView()
    {
        InitializeComponent();
        RecentTicks.Items = _ticks;

        // ── UI timer: update display every second ──────────────
        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _uiTimer.Tick += (s, e) =>
        {
            // Runs on UI thread — safe to update controls
            var latest = _ticks.LastOrDefault();
            if (latest is not null)
            {
                CurrentPrice.Text = latest.Price.ToString("F2");
                CurrentPrice.Foreground = latest.Change >= 0
                    ? Avalonia.Media.Brushes.LimeGreen
                    : Avalonia.Media.Brushes.Red;
            }
        };

        // ── Background aggregation timer ───────────────────────
        _aggregationTimer = new System.Timers.Timer(5000);
        _aggregationTimer.Elapsed += async (s, e) =>
        {
            // Runs on thread-pool thread
            var avg = _ticks.Any()
                ? _ticks.Average(t => t.Price)
                : 0.0;

            // Must dispatch to update controls
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TickerSymbol.Text = $"AVG (5s): {avg:F2}";
            });
        };
    }

    public void Start(string symbol)
    {
        TickerSymbol.Text = symbol;
        _uiTimer.Start();
        _aggregationTimer.Start();

        // Generate initial ticks
        for (int i = 0; i < 10; i++)
            GenerateTick();

        // Generate new ticks on a background thread every 200ms
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(200);

                // Must dispatch collection modification
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    GenerateTick();
                    if (_ticks.Count > 100)
                        _ticks.RemoveAt(0);
                });
            }
        });
    }

    public void Stop()
    {
        _uiTimer.Stop();
        _aggregationTimer.Stop();
    }

    private void GenerateTick()
    {
        var change = (_rng.NextDouble() - 0.5) * 2.0;
        _lastPrice += change;
        _totalChange += change;

        _ticks.Add(new TickItem
        {
            Price = _lastPrice,
            Change = change,
            Time = DateTime.Now
        });
    }
}

public class TickItem
{
    public double Price { get; set; }
    public double Change { get; set; }
    public DateTime Time { get; set; }
}
```

### Key points

- `DispatcherTimer` updates the UI text and color on the UI thread every second — no manual dispatching needed
- `System.Timers.Timer` aggregates data on a background thread every 5 seconds, then dispatches to update the control
- Background tick generation uses `Dispatcher.UIThread.InvokeAsync` to safely add items to the bound `ObservableCollection`
- Two timers with different responsibilities: one for UI rhythm, one for background work

### Timer comparison in action

```
Time     DispatcherTimer (1s)              System.Timers.Timer (5s)
───      ─────────────────────              ───────────────────────
0.2s     —                                  —
0.4s     —                                  —
0.6s     —                                  —
0.8s     —                                  —
1.0s     Updates CurrentPrice display       —
1.2s     —                                  —
...
5.0s     —                                  Computes average, dispatches to UI
```

---

## See Also

- [053 — Threading & Dispatcher (core tutorial)](053-threading-dispatcher.md)
- [053V — Threading & Dispatcher (verbose companion)](053-threading-dispatcher-verbose.md)
- [053Q — Threading & Dispatcher (quiz)](053-threading-dispatcher-quiz.md)
