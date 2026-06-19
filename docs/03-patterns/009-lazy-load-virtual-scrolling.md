---
tier: advanced
topic: performance
estimated: 15 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Pattern: Lazy Load / Virtual Scrolling

**What you'll learn:** Async loading of large datasets with virtualized UI, showing items as the user scrolls.

**Prerequisites:** [036 — Virtualization & Large List Performance](../02-tutorials/advanced/036-virtualization-large-lists.md), [061 — Collection Views](../02-tutorials/intermediate/061-collection-views.md)

---

## Problem

Displaying a list of 10,000+ items while loading them incrementally from a data source (API, database). Loading everything upfront blocks the UI and wastes memory. The list must remain responsive during scroll.

---

## Solution: AsyncLazyDataSource

```csharp
public class AsyncLazyDataSource<T> : ObservableCollection<T>
{
    private readonly Func<int, int, Task<IReadOnlyList<T>>> _pageLoader;
    private readonly int _pageSize;
    private int _currentPage;
    private bool _isLoading;
    private bool _hasMore = true;

    public AsyncLazyDataSource(
        Func<int, int, Task<IReadOnlyList<T>>> pageLoader,
        int pageSize = 50)
    {
        _pageLoader = pageLoader;
        _pageSize = pageSize;
    }

    public async Task LoadNextPageAsync()
    {
        if (_isLoading || !_hasMore) return;
        _isLoading = true;

        try
        {
            var items = await _pageLoader(_currentPage++, _pageSize);
            if (items.Count < _pageSize) _hasMore = false;
            foreach (var item in items) Add(item);
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task InitializeAsync()
    {
        Clear();
        _currentPage = 0;
        _hasMore = true;
        await LoadNextPageAsync();
    }
}
```

---

## ViewModel

```csharp
public partial class CatalogViewModel : ObservableObject
{
    private readonly AsyncLazyDataSource<Product> _products;

    [ObservableProperty] private bool _isLoading;

    public ICollectionView ProductsView { get; }

    public CatalogViewModel()
    {
        _products = new AsyncLazyDataSource<Product>(
            (page, size) => LoadProductsAsync(page, size), 50);
        ProductsView = CollectionViewSource.GetDefaultView(_products);
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        IsLoading = true;
        await _products.LoadNextPageAsync();
        IsLoading = false;
    }

    public async Task InitializeAsync()
    {
        await _products.InitializeAsync();
    }
}
```

---

## UI — infinite scroll with ScrollViewer

```xml
<ScrollViewer>
  <ItemsControl Items="{Binding ProductsView}">
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Border Padding="8" Margin="0,2" BorderBrush="LightGray" BorderThickness="0,0,0,1">
          <TextBlock Text="{Binding Name}" />
        </Border>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</ScrollViewer>
```

### Scroll-triggered loading (code-behind)

```csharp
public partial class CatalogView : UserControl
{
    public CatalogView() => InitializeComponent();

    protected override void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is CatalogViewModel vm && !vm.IsLoading
            && e.ViewportDelta.Y > 0
            && e.ScrollableSize.Y - e.Viewport.Y - e.Viewport.Height < 100)
        {
            vm.LoadMoreCommand.Execute(null);
        }
    }
}
```

---

## UI — with ListBox virtualization

```xml
<ListBox Items="{Binding ProductsView}"
         VirtualizationMode="Simple"
         ScrollViewer.IsScrollInertiaEnabled="True">
```

---

## Pagination via UI (page buttons)

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
  <Button Command="{Binding LoadMoreCommand}" Content="Load More"
          IsVisible="{Binding IsLoading, Converter={StaticResource InvertBool}}" />
  <ProgressBar IsVisible="{Binding IsLoading}" IsIndeterminate="True" Width="100" />
</StackPanel>
```

---

## Database-backed variant (SQLite)

```csharp
private async Task<IReadOnlyList<Product>> LoadFromDbAsync(int page, int size)
{
    await using var conn = new SqliteConnection("Data Source=products.db");
    await conn.OpenAsync();
    var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT * FROM Products ORDER BY Id LIMIT @size OFFSET @skip";
    cmd.Parameters.AddWithValue("@size", size);
    cmd.Parameters.AddWithValue("@skip", page * size);

    var results = new List<Product>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        results.Add(new Product { Id = reader.GetInt32(0), Name = reader.GetString(1) });
    return results;
}
```
