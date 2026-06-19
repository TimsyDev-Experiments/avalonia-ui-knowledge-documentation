---
tier: advanced
topic: development
estimated: 30 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 085 — Performance & Profiling — Examples

**Prerequisites:** [085-core](085-performance-profiling.md)

---

## Example 1: Large list with virtualization check

```xml
<!-- CORRECT: ListBox in Grid row with * height — virtualization works -->
<Grid RowDefinitions="Auto,*">
  <TextBlock Grid.Row="0" Text="{Binding Count, StringFormat='{0} items'}" />
  <ListBox Grid.Row="1" ItemsSource="{Binding LogEntries}">
    <ListBox.ItemsPanel>
      <ItemsPanelTemplate>
        <VirtualizingStackPanel BufferFactor="0.5" />
      </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
  </ListBox>
</Grid>
```

```csharp
// ViewModel
public partial class LogViewModel : ObservableObject
{
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public async Task LoadAsync()
    {
        var items = await Task.Run(() => Enumerable.Range(1, 100_000)
            .Select(i => new LogEntry($"Event #{i}"))
            .ToList());

        LogEntries.Clear();
        foreach (var item in items)
            LogEntries.Add(item);
    }
}
```

---

## Example 2: Incremental loading for non-virtualizing panel

```csharp
private async Task LoadItemsIncrementally(IList<ItemViewModel> items, StackPanel container)
{
    const int batchSize = 50;

    // Load first batch immediately
    foreach (var item in items.Take(batchSize))
        container.Children.Add(CreateCard(item));

    // Load remaining batches with dispatcher yield
    for (int i = batchSize; i < items.Count; i += batchSize)
    {
        foreach (var item in items.Skip(i).Take(batchSize))
            container.Children.Add(CreateCard(item));

        await Dispatcher.UIThread.Yield(DispatcherPriority.Background);
    }
}

private Control CreateCard(ItemViewModel item)
{
    return new Border
    {
        Padding = new Thickness(12),
        Margin = new Thickness(0, 0, 0, 8),
        CornerRadius = new CornerRadius(8),
        Background = Brushes.White,
        Child = new TextBlock { Text = item.Title }
    };
}
```

---

## Example 3: Batch collection update

```csharp
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<MetricPoint> _points = new();

    public void RefreshData(List<MetricPoint> newPoints)
    {
        // Instead of clearing and adding one by one:
        Points = new ObservableCollection<MetricPoint>(newPoints);
        OnPropertyChanged(nameof(Points));
    }
}
```

---

## Example 4: Debounced search

```csharp
public partial class SearchViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private string _searchText = "";
    private readonly IDisposable _subscription;

    public SearchViewModel()
    {
        _subscription = this.WhenAnyValue(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(text => ApplyFilter(text));
    }

    private void ApplyFilter(string text)
    {
        // Expensive filter operation
    }

    public void Dispose() => _subscription.Dispose();
}
```

---

## Example 5: StreamGeometry icon vs PathGeometry

```csharp
public class StarIcon : Control
{
    public override void Render(DrawingContext context)
    {
        // Efficient: StreamGeometry
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
            double radius = Math.Min(Bounds.Width, Bounds.Height) / 2;
            double angle = -Math.PI / 2;

            ctx.BeginFigure(new Point(
                center.X + radius * Math.Cos(angle),
                center.Y + radius * Math.Sin(angle)), true);

            for (int i = 0; i < 5; i++)
            {
                angle += Math.PI * 4 / 5;
                ctx.LineTo(new Point(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle)));
            }
            ctx.EndFigure(true);
        }

        context.DrawGeometry(Foreground, null, geometry);
    }
}
```

---

## Example 6: Bitmap cache for complex header

```xml
<Border CornerRadius="8" BoxShadow="0 4 12 0 #30000000">
  <Border.CacheMode>
    <BitmapCache RenderAtScale="1"
                 SnapsToDevicePixels="True"
                 EnableClearType="True" />
  </Border.CacheMode>

  <Grid ColumnDefinitions="48,*" Padding="16"
        Background="{StaticResource SurfaceBrush}">
    <Ellipse Width="48" Height="48" Fill="{StaticResource AccentBrush}">
      <Ellipse.OpacityMask>
        <ImageBrush Source="/Assets/avatar.png" />
      </Ellipse.OpacityMask>
    </Ellipse>
    <StackPanel Grid.Column="1" VerticalAlignment="Center" Margin="12,0,0,0">
      <TextBlock Text="{Binding DisplayName}" FontWeight="SemiBold" />
      <TextBlock Text="{Binding Role}" Foreground="{StaticResource TextSecondaryBrush}" />
    </StackPanel>
  </Grid>
</Border>
```

---

## Example 7: Simplified TextBox theme for dense forms

```xml
<Application.Resources>
  <ResourceDictionary>
    <ControlTheme x:Key="DenseTextBox" TargetType="TextBox">
      <Setter Property="Template">
        <ControlTemplate>
          <Border Background="{TemplateBinding Background}"
                  BorderBrush="{TemplateBinding BorderBrush}"
                  BorderThickness="{TemplateBinding BorderThickness}">
            <TextPresenter Name="PART_TextPresenter"
                           Text="{TemplateBinding Text}"
                           CaretBrush="{TemplateBinding CaretBrush}"
                           SelectionBrush="{TemplateBinding SelectionBrush}"
                           SelectionForegroundBrush="{TemplateBinding SelectionForegroundBrush}" />
          </Border>
        </ControlTemplate>
      </Setter>
    </ControlTheme>
  </ResourceDictionary>
</Application.Resources>
```

```xml
<TextBox Theme="{StaticResource DenseTextBox}"
         Text="{Binding Value}"
         BorderBrush="#D1D5DB" BorderThickness="1"
         Background="White" />
```

---

## Example 8: Using DevTools profiler

```csharp
// Ensure DevTools is attached
public override void OnFrameworkInitializationCompleted()
{
#if DEBUG
    this.AttachDevTools();
#endif
    base.OnFrameworkInitializationCompleted();
}
```

Steps:
1. Press **F12** to open DevTools.
2. Navigate to **Profiler** tab.
3. Click **Record**.
4. Perform the slow interaction (e.g., open a large dialog, scroll a list).
5. Click **Record** again.
6. Inspect **Style Matching** tab — look for selectors with high attempts / low matches.
7. Inspect **Resource Lookup** tab — look for keys with high lookups / low success.
8. Inspect **Style Activators** tab — look for selectors with excessive evaluations.

Sample analysis: If `TextBlock.h1` shows 5,000 match attempts but only 50 matches, the selector is too broad — it's being tested against controls that aren't even `TextBlock`s. Add a type prefix to narrow: `TextBlock.h1` is already correct; the issue may be that many non-TextBlock controls inherit `TextElement` properties.

---

## Example 9: GPU cache size configuration

```csharp
public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .With(new SkiaOptions
        {
            MaxGpuResourceSizeBytes = 512 * 1024 * 1024 // 512 MB
        })
        .With(new Win32PlatformOptions
        {
            RenderingMode = new[] { Win32RenderingMode.Wgl }
        })
        .LogToTrace();
```

---

## Key Takeaways

- Constrain `ListBox` height via `Grid` rows or explicit `Height` — not `StackPanel`
- Batch collection updates to avoid per-item UI notification
- Debounce search input with `Throttle` to avoid filtering on every keystroke
- Use `StreamGeometry` for static shapes — less memory, faster to render
- Cache complex headers or repeated visuals with `BitmapCache`
- Simplify heavy control templates (`TextBox`, `ComboBox`) when full features aren't needed
- Profile with DevTools, focusing on Style Matching, Style Activators, and Resource Lookup tabs
- Increase `MaxGpuResourceSizeBytes` for image-heavy or visualization-heavy apps
