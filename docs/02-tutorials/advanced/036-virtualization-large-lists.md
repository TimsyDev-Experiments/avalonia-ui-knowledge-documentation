---
tier: advanced
topic: virtualization
estimated: 35 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 036 -- Virtualization and Large List Performance

**What you'll learn:** How to efficiently display thousands of items using `ItemsRepeater`, `VirtualizingStackPanel`, and item recycling patterns in Avalonia.

**Prerequisites:** [009 -- Data Templates Basics](../basics/009-data-templates-basics.md), [015 -- Item Lists (ListBox, ItemsRepeater, DataGrid)](../intermediate/015-item-lists.md)

---

## 1. ItemsRepeater vs ListBox vs DataGrid

| Control | Virtualization | Selection | Editing | Use case |
|---------|---------------|-----------|---------|----------|
| `ItemsRepeater` | Full (with `VirtualizingStackPanel`) | None | None | High-performance, custom selection |
| `ListBox` | Built-in | Single/Multi | No | Standard item lists |
| `DataGrid` | Row-level | Row/Cell | Yes | Tabular data |
| `TreeView` | Virtualizing | Single | No | Hierarchical data |

## 2. Basic ItemsRepeater with virtualization

```xml
<ScrollViewer>
  <ItemsRepeater ItemsSource="{Binding Items}"
                 x:DataType="viewModels:MainViewModel">
    <ItemsRepeater.Layout>
      <VirtualizingStackPanel />
    </ItemsRepeater.Layout>
    <ItemsRepeater.ItemTemplate>
      <DataTemplate x:DataType="models:LogEntry">
        <Grid ColumnDefinitions="Auto,*" Margin="0,2">
          <TextBlock Grid.Column="0"
                     Text="{Binding Timestamp, StringFormat='{}{0:HH:mm:ss}'}"
                     FontFamily="Consolas" Margin="0,0,12,0" />
          <TextBlock Grid.Column="1" Text="{Binding Message}"
                     TextWrapping="NoWrap" />
        </Grid>
      </DataTemplate>
    </ItemsRepeater.ItemTemplate>
  </ItemsRepeater>
</ScrollViewer>
```

`ItemsRepeater` must be inside a `ScrollViewer` to virtualize.

## 3. ScrollViewer modes for virtualization

```xml
<!-- Enabled virtualization (default) -->
<ScrollViewer>
  <ItemsRepeater ... />
</ScrollViewer>

<!-- Disabled -- all items realized -->
<ScrollViewer AllowAutoHide="False">
  <VirtualizingStackPanel.IsVirtualizing="False">
    <ItemsRepeater ... />
  </VirtualizingStackPanel.IsVirtualizing>
</ScrollViewer>
```

## 4. VirtualizingStackPanel configuration

```xml
<VirtualizingStackPanel
    Orientation="Vertical"
    VirtualizationMode="Recycling" />
```

- `VirtualizationMode="Standard"` — create and destroy elements as they scroll in/out
- `VirtualizationMode="Recycling"` — reuse container elements (default, recommended)
- `Orientation="Horizontal"` — horizontal virtualized scroll

## 5. Optimize with async loading and pagination

```csharp
public partial class LogViewerViewModel : ObservableObject
{
    public AsyncVirtualizingCollection<LogEntry> Items { get; }

    public LogViewerViewModel()
    {
        Items = new AsyncVirtualizingCollection<LogEntry>(
            new LogEntryProvider(), pageSize: 100);
    }
}
```

Alternatively, build incremental loading with `IScrollAnchorProvider`:

```csharp
public partial class InfiniteFeedViewModel : ObservableObject
{
    private readonly ObservableCollection<FeedItem> _items = new();
    public IReadOnlyList<FeedItem> Items => _items;

    private int _page;

    public async Task LoadNextPageAsync()
    {
        _page++;
        var batch = await FetchPageAsync(_page);
        foreach (var item in batch)
            _items.Add(item);
    }
}
```

Wire to a `ScrollViewer.ScrollChanged` handler:

```csharp
private async void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
{
    var scroller = (ScrollViewer)sender!;
    if (scroller.Offset.Y + scroller.Viewport.Height
        >= scroller.Extent.Height - 200)
    {
        await _vm.LoadNextPageAsync();
    }
}
```

## 6. Converters and recycling-safe patterns

Never store state in containers that outlives a data item. Use `DataTemplate` bindings:

```xml
<DataTemplate x:DataType="models:FileItem">
  <Border Background="{Binding IsSelected, Converter={StaticResource BoolToBrush}}">
    <TextBlock Text="{Binding Name}" />
  </Border>
</DataTemplate>
```

For selection, attach behavior:

```csharp
public static class ItemsRepeaterSelection
{
    public static readonly AttachedProperty<bool> IsSelectedProperty
        = AvaloniaProperty.RegisterAttached<StyledElement, bool>(
            "IsSelected", typeof(ItemsRepeaterSelection));

    static ItemsRepeaterSelection()
    {
        IsSelectedProperty.Changed.AddClassHandler<Border>(OnIsSelectedChanged);
    }

    private static void OnIsSelectedChanged(Border border, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            border.Background = Brushes.LightBlue;
        else
            border.Background = Brushes.Transparent;
    }
}
```

## 7. DataGrid row virtualization

```xml
<DataGrid ItemsSource="{Binding LargeTable}"
          EnableRowVirtualization="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          RowHeight="24" />
```

`DataGrid` virtualizes by default. Set `EnableRowVirtualization="False"` only if all rows must be present (e.g., for measurement).

## Key takeaways

- `ItemsRepeater` + `VirtualizingStackPanel` is the fastest virtualizing list
- Always wrap `ItemsRepeater` in a `ScrollViewer` for virtualization to activate
- `VirtualizationMode="Recycling"` reuses containers (default, reduces GC pressure)
- Use incremental loading with `ScrollViewer.ScrollChanged` for infinite scroll
- Do not store mutable state in recycled containers; use data binding
- `DataGrid` virtualizes rows automatically; disable only when all rows must be realized
