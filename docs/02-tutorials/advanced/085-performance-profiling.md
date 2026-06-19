---
tier: advanced
topic: development
estimated: 30 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 085 — Performance & Profiling

**What you'll learn:** Diagnose and fix common performance issues in Avalonia applications — virtualization, layout optimization, rendering, bindings, collections, async patterns, and using the DevTools profiler.

**Prerequisites:** [036 — Virtualization & Large List Performance](036-virtualization-large-lists.md)

---

## 1. UI virtualization

Virtualization ensures only visible items are created and rendered.

| Control | Default panel | Virtualizes? |
|---|---|---|
| `ListBox` | `VirtualizingStackPanel` | Yes |
| `ItemsRepeater` | configurable | Yes (with `StackLayout` / `UniformGridLayout`) |
| `ItemsControl` | `StackPanel` | No |
| `DataGrid` | custom | Yes |
| `TreeDataGrid` (Pro) | custom | Yes |

### Ensuring virtualization

```xml
<!-- Correct: Grid row constrains height -->
<Grid RowDefinitions="*">
  <ListBox ItemsSource="{Binding LargeCollection}" />
</Grid>

<!-- Wrong: StackPanel gives infinite height, disables virtualization -->
<StackPanel>
  <ListBox ItemsSource="{Binding LargeCollection}" />
</StackPanel>
```

### BufferFactor

```xml
<ListBox ItemsSource="{Binding LargeCollection}">
  <ListBox.ItemsPanel>
    <ItemsPanelTemplate>
      <VirtualizingStackPanel BufferFactor="1" />
    </ItemsPanelTemplate>
  </ListBox.ItemsPanel>
</ListBox>
```

`BufferFactor="1"` keeps one extra viewport of realized items above and below the visible area — smoother scrolling at the cost of more memory.

---

## 2. Layout optimization

### Avoid deep nesting

```xml
<!-- Avoid -->
<StackPanel>
  <Border>
    <StackPanel>
      <Border><TextBlock Text="Hello" /></Border>
    </StackPanel>
  </Border>
</StackPanel>

<!-- Prefer -->
<StackPanel>
  <TextBlock Text="Hello" Margin="8" />
</StackPanel>
```

Flat layouts are faster because each nesting level adds measure and arrange passes.

### Use Grid instead of nested StackPanels

```xml
<Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto" RowSpacing="4">
  <TextBlock Grid.Row="0" Grid.Column="0" Text="Name:" />
  <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding Name}" />
  <TextBlock Grid.Row="1" Grid.Column="0" Text="Email:" />
  <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding Email}" />
</Grid>
```

---

## 3. Control template complexity

Complex controls like `TextBox` have deep visual trees. For high-density lists, consider:

### Swap TextBlock for TextBox

```csharp
var display = new TextBlock { Text = field.Value };
display.PointerPressed += (s, e) =>
{
    var editor = new TextBox { Text = field.Value };
    editor.LostFocus += (s2, e2) =>
    {
        field.Value = editor.Text;
        parent.Children.Remove(editor);
        parent.Children.Add(display);
    };
    parent.Children.Remove(display);
    parent.Children.Add(editor);
};
```

### Simplified control theme

```xml
<ControlTheme x:Key="LightTextBox" TargetType="TextBox">
  <Setter Property="Template">
    <ControlTemplate>
      <Border Background="{TemplateBinding Background}"
              BorderBrush="{TemplateBinding BorderBrush}"
              BorderThickness="{TemplateBinding BorderThickness}">
        <TextPresenter Name="PART_TextPresenter"
                       Text="{TemplateBinding Text}"
                       CaretBrush="{TemplateBinding CaretBrush}" />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

---

## 4. Rendering performance

| Technique | Benefit |
|---|---|
| `IsVisible="False"` | Removes from layout + rendering (better than `Opacity="0"`) |
| `IsHitTestVisible="False"` | Skips hit testing for non-interactive overlays |
| `ClipToBounds` | Only enable when content actually exceeds bounds |
| `BitmapCache` | Rasterizes complex visuals once |
| `StreamGeometry` | Lower memory than `PathGeometry` |
| Reduced image sizes | Prevents full-size decode for thumbnails |

### BitmapCache

```xml
<Border CornerRadius="8" BoxShadow="0 4 8 0 #40000000">
  <Border.CacheMode>
    <BitmapCache RenderAtScale="1" SnapsToDevicePixels="True"
                 EnableClearType="True" />
  </Border.CacheMode>
  <TextBlock Text="Cached complex content" />
</Border>
```

### GPU resource cache

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new SkiaOptions
    {
        MaxGpuResourceSizeBytes = 256 * 1024 * 1024 // 256 MB
    });
```

Increase from the default ~28 MB if your app works with large images or many cached visuals.

### Visual complexity

- Minimize `BoxShadow` effects (each adds an individual render pass)
- Avoid overlapping semi-transparent elements
- Use `Opacity` on a parent rather than on each child

---

## 5. Data binding

| Technique | Benefit |
|---|---|
| Compiled bindings | Zero runtime reflection (enabled by default in Avalonia 12) |
| `Mode=OneTime` | No change tracking for static values |
| Static values or resources | Avoid binding overhead for constants |
| Avoid `RelativeSource.FindAncestor` | Causes binding errors until template initialises |

```xml
<TextBlock Text="{Binding Version, Mode=OneTime}" />
<TextBlock Text="{StaticResource AppTitle}" />
```

---

## 6. Collections

```csharp
// Bad: triggers UI update per item
foreach (var item in newItems) Items.Add(item);

// Good: single collection replacement
Items = new ObservableCollection<Item>(newItems);
OnPropertyChanged(nameof(Items));
```

### Incremental loading

```csharp
private async Task LoadItemsIncrementally(IList<ItemViewModel> items, Panel container)
{
    const int batchSize = 50;
    for (int i = 0; i < items.Count; i += batchSize)
    {
        var batch = items.Skip(i).Take(batchSize);
        foreach (var item in batch)
            container.Children.Add(CreateControl(item));
        await Dispatcher.UIThread.Yield(DispatcherPriority.Background);
    }
}
```

For large reactive collections with frequent sorting/filtering, use [DynamicData](https://github.com/reactivemarbles/DynamicData).

---

## 7. Async and threading

```csharp
// Move heavy work off UI thread
var data = await Task.Run(() => LoadLargeDataSet());
Items = new ObservableCollection<Item>(data);

// Debounce search input
this.WhenAnyValue(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Subscribe(text => ApplyFilter(text));

// Deferred low-priority work
Dispatcher.UIThread.Post(() => UpdateStatistics(), DispatcherPriority.Background);
```

---

## 8. Profiling with DevTools

Press **F12** in debug builds to open DevTools.

### Frame timing

The **Performance** tab shows frame timing, layout counts, and render time. Look for frames exceeding 16ms (60 FPS target) or 33ms (30 FPS).

### Profiler tool

Click **Record**, interact with your app, click **Record** again. Results are shown in tabs:

| Profiler | What it measures |
|---|---|
| **Style Matching** | Selector evaluation during control creation. High attempts + few matches = overly broad selectors. |
| **Style Activators** | Runtime re-evaluation of conditional selectors (`:pointerover`, `:focus`, `:pressed`). |
| **Resource Lookup** | Resource resolution by key. High lookups + low success = missing/misspelled resource. |

### Metrics tool

Shows .NET metrics for process, GC, and Avalonia frame timing:

```csharp
static Meter s_meter = new Meter("MyApp");
static Counter<int> s_tasksResolved = s_meter.CreateCounter<int>("tasks.resolved");
```

### FPS overlay

```csharp
#if DEBUG
    this.AttachDevTools();
#endif
```

---

## 9. Region dirty rect clipping

Enables more accurate dirty-rect tracking. **Disabled by default** in Avalonia 12.1+ (CPU cost outweighs benefit on GPU-accelerated platforms). Enable for software-rendered targets like embedded Linux:

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .With(new CompositionOptions
    {
        UseRegionDirtyRectClipping = true
    });
```

---

## Key Takeaways

- **Virtualization** requires constrained height — `Grid` rows, `DockPanel`, or explicit `Height`
- **Flat layouts** are faster than deep nesting; `Grid` beats nested `StackPanel`s
- **Swap** heavy controls (`TextBox`) for lightweight ones (`TextBlock`) in dense lists; swap on interaction
- **BitmapCache** rasterises complex visuals; **StreamGeometry** uses less memory than PathGeometry
- **CompiledBindings** eliminate runtime reflection — they are on by default in Avalonia 12
- **Batch** list additions; use **incremental loading** for non-virtualizing containers
- **DevTools profiler** has three tabs: Style Matching, Style Activators, Resource Lookup
- **Gpu resource cache** default is ~28 MB — increase via `SkiaOptions.MaxGpuResourceSizeBytes` for image-heavy apps

---

## See Also

- [036 — Virtualization & Large List Performance](036-virtualization-large-lists.md)
- [038 — Headless Testing](038-headless-testing.md)
- [DevTools Profiler](https://docs.avaloniaui.net/tools/developer-tools/profiler-tool)
- [Metrics tool](https://docs.avaloniaui.net/tools/developer-tools/metrics-tool)
- [Troubleshooting: App performance issues](https://docs.avaloniaui.net/troubleshooting/app-performance-issues)
