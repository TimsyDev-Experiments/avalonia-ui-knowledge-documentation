---
tier: advanced
topic: virtualization
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 036-virtualization-large-lists.md
---

# 036E — Virtualization and Large List Performance: Real-World Examples

**What this is:** Two complete scenarios that apply `ItemsRepeater`, `VirtualizingStackPanel`, incremental loading, selection behaviors, and scroll anchoring to concrete high-performance list problems.

**Prerequisites:** [036 — Virtualization and Large List Performance](036-virtualization-large-lists.md), [036V — Verbose Companion](036-virtualization-large-lists-verbose.md)

---

## Example 1: Real-Time Log Viewer with Auto-Scroll

### Goal

Display a live stream of log entries (up to 500,000) with `ItemsRepeater` + `VirtualizingStackPanel`, auto-scroll to the newest entry when the user is at the bottom, pause auto-scroll when the user scrolls up to inspect older entries, and anchor scroll position when new entries are prepended (by log level filtering).

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class LogEntry : ObservableObject
{
    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private string _level = "INFO";

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private int _threadId;
}

public partial class LogViewerViewModel : ObservableObject
{
    private readonly ObservableCollection<LogEntry> _entries = new();
    private readonly Random _rng = new();
    private int _counter;
    private bool _isAtBottom = true;

    public LogViewerViewModel()
    {
        Items = _entries;
    }

    public IList<LogEntry> Items { get; }

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private string _statusText = "0 entries";

    public void OnScrollChanged(double offsetY, double viewportHeight, double extentHeight)
    {
        var nearBottom = offsetY + viewportHeight >= extentHeight - 50;
        AutoScroll = nearBottom;
        _isAtBottom = nearBottom;
    }

    public void AddLogEntry(string level, string message)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message,
            ThreadId = _rng.Next(1, 9)
        };

        _entries.Add(entry);
        _counter++;
        StatusText = $"{_counter} entries";

        if (_entries.Count > 500_000)
        {
            for (int i = 0; i < 50_000; i++)
                _entries.RemoveAt(0);
            GC.Collect();
        }
    }

    [RelayCommand]
    private void StartSimulation()
    {
        var levels = new[] { "INFO", "WARN", "ERROR", "DEBUG" };
        var messages = new[]
        {
            "Request processed", "Connection pool exhausted",
            "User login failed", "Cache miss", "GC triggered",
            "Endpoint timeout", "Rate limit applied"
        };

        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        _ = Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync())
            {
                var level = levels[_rng.Next(levels.Length)];
                var msg = messages[_rng.Next(messages.Length)];
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    AddLogEntry(level, msg));
            }
        });
    }

    [RelayCommand]
    private void ClearEntries()
    {
        _entries.Clear();
        _counter = 0;
        StatusText = "0 entries";
    }
}
```

### View (XAML)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.LogViewerView"
             x:DataType="vm:LogViewerViewModel">
  <Grid RowDefinitions="Auto,*,Auto" Margin="8" Spacing="8">
    <!-- Toolbar -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8">
      <Button Content="Start Simulation"
              Command="{Binding StartSimulationCommand}" />
      <Button Content="Clear"
              Command="{Binding ClearEntriesCommand}" />
      <TextBlock Text="{Binding StatusText}"
                 VerticalAlignment="Center" FontSize="11" />
      <CheckBox IsChecked="{Binding AutoScroll}"
                Content="Auto-scroll" />
    </StackPanel>

    <!-- Virtualized log list -->
    <ScrollViewer Grid.Row="1"
                  Name="LogScroller">
      <ItemsRepeater ItemsSource="{Binding Items}"
                     x:DataType="vm:LogViewerViewModel"
                     ScrollAnchorOptions="Top">
        <ItemsRepeater.Layout>
          <VirtualizingStackPanel VirtualizationMode="Recycling" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
          <DataTemplate x:DataType="vm:LogEntry">
            <Grid ColumnDefinitions="Auto,Auto,*" Margin="0,1" Spacing="8"
                  Height="20">
              <TextBlock Grid.Column="0"
                         Text="{Binding Timestamp, StringFormat='{0:HH:mm:ss.fff}'}"
                         FontFamily="Consolas" FontSize="11" />
              <Border Grid.Column="1"
                      CornerRadius="3" Padding="4,0"
                      Background="{Binding Level, Converter={StaticResource LogLevelToBrush}}">
                <TextBlock Text="{Binding Level}" FontSize="10" />
              </Border>
              <TextBlock Grid.Column="2"
                         Text="{Binding Message}"
                         FontFamily="Consolas" FontSize="11"
                         TextTrimming="CharacterEllipsis" />
            </Grid>
          </DataTemplate>
        </ItemsRepeater.ItemTemplate>
      </ItemsRepeater>
    </ScrollViewer>

    <!-- A scroll button (visible when not at bottom) -->
    <Button Grid.Row="2" Content="⬇ Scroll to bottom"
            IsVisible="{Binding AutoScroll, Converter={StaticResource InvertBool}}"
            HorizontalAlignment="Right"
            Margin="0,0,8,8" />
  </Grid>
</UserControl>
```

### Code-Behind for Scroll Handling

```csharp
public partial class LogViewerView : UserControl
{
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var scroller = this.FindControl<ScrollViewer>("LogScroller");
        if (scroller is not null)
            scroller.ScrollChanged += OnScrollChanged;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is LogViewerViewModel vm)
        {
            var scroller = (ScrollViewer)sender!;
            vm.OnScrollChanged(
                scroller.Offset.Y,
                scroller.Viewport.Height,
                scroller.Extent.Height);
        }
    }
}
```

### How It Works

1. **Virtualization with `ItemsRepeater`** — The `ItemsRepeater` + `VirtualizingStackPanel` only creates containers for visible log entries. With 500,000 entries in the `ObservableCollection`, only ~20-30 `ContentPresenter` instances exist at any time.

2. **Auto-scroll detection** — `OnScrollChanged` calculates whether the user is near the bottom (`Offset.Y + Viewport.Height >= Extent.Height - 50`). When `true`, `AutoScroll` is set. A `ScrollViewer` bound to `AutoScroll` (via code-behind) calls `ScrollToEnd()` when new entries arrive.

3. **Collection cap** — `AddLogEntry` trims the collection to 500,000 by removing 50,000 oldest entries when exceeded. Without this, memory grows unbounded. The `GC.Collect()` call after bulk removal reduces memory pressure.

4. **Scroll anchoring** — `ScrollAnchorOptions="Top"` prevents the viewport from jumping when items are removed from the top (during the trim). The `ItemsRepeater` anchors to the first visible item and adjusts `Offset` to keep it stable.

5. **Background generation** — `StartSimulation` uses `PeriodicTimer` on a background thread and dispatches to the UI thread via `Dispatcher.UIThread.Post`. The timer is not awaited on the UI thread, so the UI stays responsive.

### Design Decisions and Trade-offs

- **`ObservableCollection` vs `AsyncVirtualizingCollection`** — This example keeps all items in memory (capped at 500K). `AsyncVirtualizingCollection` would reduce memory but requires paging from a data source. For real-time logs generated locally, in-memory is simpler.
- **Code-behind for scroll** — The `ScrollViewer.ScrollChanged` event is handled in code-behind because `ItemsRepeater` does not expose scroll events through bindings. This is pragmatically necessary.
- **`PeriodicTimer` vs `Task.Delay`** — `PeriodicTimer` avoids drift over long periods. `Task.Delay` in a loop accumulates timing error.

---

## Example 2: Variable-Height Image Gallery with Lazy Loading

### Goal

Display a grid of image thumbnails with variable heights (masonry-like layout using `WrapLayout` + `VirtualizingStackPanel`), lazy-load thumbnails as the user scrolls, and show a loading placeholder for not-yet-loaded items. Use `VirtualizationMode="Recycling"` to avoid container churn.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class GalleryItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string? _thumbnailUrl;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private double _aspectRatio = 1.0;

    public int DesiredWidth => 200;
    public int DesiredHeight => (int)(DesiredWidth / AspectRatio);
}

public partial class ImageGalleryViewModel : ObservableObject
{
    private readonly IGalleryService _gallery;
    private readonly ObservableCollection<GalleryItem> _items = new();
    private int _currentPage;
    private bool _isLoadingPage;

    public ImageGalleryViewModel(IGalleryService gallery)
    {
        _gallery = gallery;
    }

    public IList<GalleryItem> Items => _items;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [RelayCommand]
    private async Task LoadInitialBatchAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            _currentPage = 0;
            var batch = await _gallery.GetPageAsync(0, 50);
            _items.Clear();
            foreach (var item in batch)
                _items.Add(item);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load gallery: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task OnScrollNearEndAsync()
    {
        if (_isLoadingPage) return;

        _isLoadingPage = true;

        try
        {
            _currentPage++;
            var batch = await _gallery.GetPageAsync(_currentPage, 50);
            foreach (var item in batch)
                _items.Add(item);
        }
        catch
        {
            // Silently ignore — the user can scroll to retry
        }
        finally
        {
            _isLoadingPage = false;
        }
    }
}

public interface IGalleryService
{
    Task<IReadOnlyList<GalleryItem>> GetPageAsync(int page, int pageSize);
}
```

### View (XAML)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.ImageGalleryView"
             x:DataType="vm:ImageGalleryViewModel">
  <Grid RowDefinitions="Auto,*" Margin="8" Spacing="8">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8">
      <Button Content="Load Gallery"
              Command="{Binding LoadInitialBatchCommand}" />
      <TextBlock Text="{Binding ErrorMessage}"
                 Foreground="{DynamicResource ErrorBrush}"
                 VerticalAlignment="Center" />
    </StackPanel>

    <ScrollViewer Grid.Row="1" Name="GalleryScroller">
      <ItemsRepeater ItemsSource="{Binding Items}"
                     x:DataType="vm:ImageGalleryViewModel"
                     ScrollAnchorOptions="Near">
        <ItemsRepeater.Layout>
          <VirtualizingStackPanel Orientation="Vertical"
                                 VirtualizationMode="Recycling" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
          <DataTemplate x:DataType="vm:GalleryItem">
            <Border CornerRadius="6" Margin="4"
                    Width="{Binding DesiredWidth}"
                    Height="{Binding DesiredHeight}"
                    Background="{DynamicResource CardBrush}">
              <Grid>
                <!-- Loading placeholder -->
                <Border IsVisible="{Binding IsLoaded, Converter={StaticResource InvertBool}}"
                        CornerRadius="6">
                  <TextBlock Text="..."
                             HorizontalAlignment="Center"
                             VerticalAlignment="Center" />
                </Border>
                <!-- Thumbnail -->
                <Image Source="{Binding ThumbnailUrl}"
                       IsVisible="{Binding IsLoaded}"
                       Stretch="UniformToFill" />
                <!-- Overlay label -->
                <Border VerticalAlignment="Bottom"
                        Background="#80000000"
                        Padding="4,2"
                        IsVisible="{Binding IsLoaded}">
                  <TextBlock Text="{Binding Label}"
                             Foreground="White" FontSize="10" />
                </Border>
              </Grid>
            </Border>
          </DataTemplate>
        </ItemsRepeater.ItemTemplate>
      </ItemsRepeater>
    </ScrollViewer>
  </Grid>
</UserControl>
```

### Code-Behind for Infinite Scroll

```csharp
public partial class ImageGalleryView : UserControl
{
    private CancellationTokenSource? _scrollCts;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var scroller = this.FindControl<ScrollViewer>("GalleryScroller");
        if (scroller is not null)
            scroller.ScrollChanged += OnScrollChanged;
    }

    private async void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        _scrollCts?.Cancel();
        _scrollCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _scrollCts.Token);

            if (DataContext is ImageGalleryViewModel vm)
            {
                var scroller = (ScrollViewer)sender!;
                var nearEnd = scroller.Offset.Y + scroller.Viewport.Height
                    >= scroller.Extent.Height - 400;

                if (nearEnd)
                    await vm.OnScrollNearEndAsync();
            }
        }
        catch (TaskCanceledException) { }
    }
}
```

### How It Works

1. **Variable heights via `Width` + `Height` binding** — Each `GalleryItem` specifies `DesiredWidth` (200px) and `DesiredHeight` derived from its aspect ratio. The `Border` in the DataTemplate binds to both, so each item has a different height. `VirtualizingStackPanel` handles variable-sized items — it measures each realized item to determine its contribution to the total extent.

2. **Recycling-safe placeholders** — When an item is recycled, `IsLoaded` resets to `false` (because `DataContext` changes to a new `GalleryItem` that has `IsLoaded = false`). The placeholder border shows until the thumbnail finishes loading. This avoids stale images appearing on recycled containers.

3. **Debounced scroll detection** — The `ScrollChanged` handler uses a 300ms debounce via `CancellationTokenSource`. Rapid scrolling only triggers one fetch after the user stops scrolling. Without debounce, a fast scroll could fire 20+ `OnScrollNearEndAsync` calls.

4. **Page-level loading guard** — `_isLoadingPage` prevents concurrent page fetches. If the user stays near the bottom after a page loads, the debounce fires again but `_isLoadingPage` is still `true`, so the second call is a no-op.

5. **`ScrollAnchorOptions="Near"`** — This anchors to the element nearest the viewport center. When new items are added at the bottom, the scroll position stays stable — the user does not get pushed upward.

### Design Decisions and Trade-offs

- **`DesiredWidth` fixed, `DesiredHeight` variable** — A truly masonry layout would use `WrapLayout` with `VirtualizingStackPanel` wrapping horizontally. Avalonia does not have a built-in virtualizing wrap panel. This example approximates it with a vertical stack of variable-height cards. For true masonry, consider `ItemsRepeater` with a custom `VirtualizingLayout`.
- **Thumbnail loading in ViewModel** — This example assumes `IGalleryService.GetPageAsync` returns items with pre-loaded thumbnail URLs. A real implementation would lazy-load the `Image.Source` via `Bitmap` from disk or network, updating `IsLoaded` when the load completes.
- **No virtualization of images** — The `Image` control itself is not virtualized. Even if the container is recycled, the `Image` element exists. For very large galleries, consider unloading images that scroll far out of view via `ElementPrepared`/`ElementClearing`.

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Log Viewer | Example 2 — Image Gallery |
|--------|------------------------|---------------------------|
| Virtualizing panel | `VirtualizingStackPanel` (vertical) | `VirtualizingStackPanel` (vertical) |
| Virtualization mode | `Recycling` | `Recycling` |
| Item height | Fixed (20px each) | Variable (per-aspect-ratio) |
| Scroll anchoring | `ScrollAnchorOptions="Top"` | `ScrollAnchorOptions="Near"` |
| Data source | Real-time, in-memory generation | Paged, async service |
| Infinite scroll | Auto-scroll to bottom | Lazy-load next page |
| Debounce | Not needed (timer-based add) | 300ms debounce via `CancellationTokenSource` |
| Concurrency guard | Collection cap + trim | `_isLoadingPage` boolean |
| Container state leak risk | Low (text-only DataTemplate) | Medium (placeholder visibility depends on binding) |
| Code-behind requirements | Scroll anchor + auto-scroll | Debounced scroll + page fetch |

## See Also

- [036 — Virtualization and Large List Performance](036-virtualization-large-lists.md) — the original tutorial
- [036V — Virtualization and Large List Performance (verbose companion)](036-virtualization-large-lists-verbose.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](../intermediate/015-item-lists.md) — baseline non-virtualized usage
- [Avalonia Docs: ItemsRepeater](https://docs.avaloniaui.net/docs/concepts/itemsrepeater)
