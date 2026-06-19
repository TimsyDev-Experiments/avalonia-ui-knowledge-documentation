---
tier: advanced
topic: performance
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 009-lazy-load-virtual-scrolling.md
---

# 009V — Lazy Load / Virtual Scrolling: An In-Depth Companion

You should already have read: [009 — Lazy Load / Virtual Scrolling](009-lazy-load-virtual-scrolling.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Why Lazy Loading Matters

Displaying 100,000+ items in a list is a common requirement in data-heavy applications: log viewers, file browsers, database explorers, analytics dashboards, and asset managers. Three problems arise when loading everything upfront:

| Problem | Consequence |
|---|---|
| **Memory** | 100,000 items × 200 bytes each = 20 MB of raw data, plus UI overhead per container |
| **Initial load time** | Querying all rows from SQLite or API can take 10+ seconds |
| **UI responsiveness** | Rendering 100,000 containers at once freezes the UI thread for minutes |

Lazy loading solves all three by:
- Loading only the visible page of items
- Virtualizing the UI containers so only on-screen items have a visual representation
- Keeping the data source paginated, with out-of-view items stored as cheap records

---

## 2. AsyncLazyDataSource — Complete Implementation

```csharp
public sealed class AsyncLazyDataSource<T> : ObservableCollection<T>
{
    private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<T>>> _pageLoader;
    private readonly int _pageSize;
    private readonly int _prefetchPages;
    private int _nextPageIndex;
    private bool _isLoading;
    private bool _hasMore = true;
    private int _totalLoadedCount;

    public AsyncLazyDataSource(
        Func<int, int, CancellationToken, Task<IReadOnlyList<T>>> pageLoader,
        int pageSize = 50,
        int prefetchPages = 2)
    {
        _pageLoader = pageLoader;
        _pageSize = pageSize;
        _prefetchPages = prefetchPages;
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsLoading)));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsNotLoading)));
            }
        }
    }

    public bool IsNotLoading => !_isLoading;
    public bool HasMore => _hasMore;
    public int TotalLoadedCount => _totalLoadedCount;

    public async Task LoadNextPageAsync(CancellationToken ct = default)
    {
        if (_isLoading || !_hasMore) return;
        IsLoading = true;

        try
        {
            var items = await _pageLoader(_nextPageIndex, _pageSize, ct);
            _nextPageIndex++;

            if (items.Count < _pageSize)
            {
                _hasMore = false;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasMore)));
            }

            int startIndex = Count;
            foreach (var item in items)
                Add(item);

            _totalLoadedCount = Count;
            OnPageLoaded?.Invoke(startIndex, items.Count);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected — do nothing
        }
        catch (Exception ex)
        {
            OnPageError?.Invoke(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task PrefetchNextPagesAsync(CancellationToken ct = default)
    {
        for (int i = 0; i < _prefetchPages && _hasMore && !ct.IsCancellationRequested; i++)
            await LoadNextPageAsync(ct);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Clear();
        _nextPageIndex = 0;
        _hasMore = true;
        _totalLoadedCount = 0;
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasMore)));
        await LoadNextPageAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await InitializeAsync(ct);
    }

    public event Action<int, int>? OnPageLoaded;   // startIndex, count
    public event Action<Exception>? OnPageError;
}
```

### Key Design Decisions

- **CancellationToken** is passed through to the page loader, allowing the UI to cancel in-flight requests when the user scrolls rapidly
- **Prefetch** loads the next 2 pages ahead of time, making scroll feel instant
- **`OnPageLoaded`** event allows the view to know exactly which indices were added (useful for keeping scroll position)
- **`OnPageError`** event allows the ViewModel to show error UI without knowing the data source details

---

## 3. ViewModel with Filtering, Sorting, and Status

```csharp
public sealed partial class CatalogViewModel : ObservableObject
{
    private readonly AsyncLazyDataSource<Product> _products;
    private readonly SourceCache<Product, int> _filterCache;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasMore;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    public ICollectionView ProductsView { get; }

    public IEnumerable<string> Categories { get; }

    public CatalogViewModel()
    {
        _products = new AsyncLazyDataSource<Product>(
            (page, size, ct) => LoadProductsAsync(page, size, ct), 50, prefetchPages: 2);

        ProductsView = CollectionViewSource.GetDefaultView(_products);

        // Wire up filtering
        ProductsView.Filter = FilterProduct;

        // Sync status
        _products.OnPageLoaded += (start, count) =>
        {
            TotalItems = _products.TotalLoadedCount;
            StatusText = $"{TotalItems} items loaded";
        };

        _products.OnPageError += ex =>
        {
            StatusText = $"Error: {ex.Message}";
        };
    }

    [RelayCommand]
    private async Task LoadMoreAsync(CancellationToken ct)
    {
        IsLoading = true;
        await _products.LoadNextPageAsync(ct);
        IsLoading = false;
        HasMore = _products.HasMore;
        StatusText = _products.HasMore
            ? $"{_products.TotalLoadedCount} items loaded — scroll for more"
            : $"All {_products.TotalLoadedCount} items loaded";
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        StatusText = "Refreshing...";
        await _products.RefreshAsync(ct);
        StatusText = $"{_products.TotalLoadedCount} items loaded";
        HasMore = _products.HasMore;
    }

    partial void OnSearchTextChanged(string value)
    {
        ProductsView.Refresh();
        StatusText = string.IsNullOrEmpty(value)
            ? $"{_products.TotalLoadedCount} items"
            : $"Filtered: {ProductsView.Count} of {_products.TotalLoadedCount}";
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ProductsView.Refresh();
    }

    private bool FilterProduct(object obj)
    {
        if (obj is not Product product) return false;

        if (!string.IsNullOrEmpty(SearchText) &&
            !product.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            return false;

        if (SelectedCategory != "All" && product.Category != SelectedCategory)
            return false;

        return true;
    }

    private async Task<IReadOnlyList<Product>> LoadProductsAsync(int page, int size, CancellationToken ct)
    {
        // Simulated API call
        await Task.Delay(300, ct);

        return Enumerable.Range(page * size, size)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Product {i:D5}",
                Category = i % 3 == 0 ? "Electronics" : i % 3 == 1 ? "Books" : "Home",
                Price = Random.Shared.Next(1, 1000)
            })
            .ToList();
    }
}

public sealed class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Price { get; init; }
}
```

---

## 4. Infinite Scroll — Complete UI and Code-Behind

### XAML View with Scroll Detector

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:MyApp.ViewModels"
             x:Class="MyApp.Views.CatalogView"
             x:DataType="vm:CatalogViewModel">
  <DockPanel>
    <!-- Toolbar -->
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <TextBox Watermark="Search..." Text="{Binding SearchText}" Width="200" />
      <ComboBox Items="{Binding Categories}" SelectedItem="{Binding SelectedCategory}" Width="120" />
      <Button Command="{Binding RefreshCommand}" Content="Refresh" />
      <TextBlock Text="{Binding StatusText}" VerticalAlignment="Center" />
    </StackPanel>

    <!-- Virtualized list with infinite scroll -->
    <ScrollViewer Name="ScrollViewer"
                  ScrollChanged="OnScrollChanged"
                  AllowAutoHide="False">
      <ItemsControl Items="{Binding ProductsView}">
        <ItemsControl.ItemTemplate>
          <DataTemplate DataType="vm:Product">
            <Border Padding="12,8" Margin="0,0,0,1"
                    BorderBrush="{Binding Category, Converter={StaticResource CategoryToColor}}"
                    BorderThickness="4,0,0,0">
              <Grid ColumnDefinitions="*,Auto">
                <StackPanel>
                  <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
                  <TextBlock Text="{Binding Category}" FontSize="11" Opacity="0.6" />
                </StackPanel>
                <TextBlock Grid.Column="1" Text="{Binding Price, StringFormat='${0:N2}'}"
                           FontWeight="Bold" VerticalAlignment="Center" />
              </Grid>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

      <!-- Loading indicator at bottom -->
      <Border Name="LoadingIndicator" IsVisible="{Binding IsLoading}"
              Padding="12" Background="Transparent">
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Spacing="8">
          <ProgressBar IsIndeterminate="True" Width="100" Height="16" />
          <TextBlock Text="Loading more items..." />
        </StackPanel>
      </Border>
    </ScrollViewer>
  </DockPanel>
</UserControl>
```

### Scroll Detection Code-Behind

```csharp
public partial class CatalogView : UserControl
{
    private CancellationTokenSource? _scrollCts;

    public CatalogView()
    {
        InitializeComponent();

        // Also handle programmatic loading when near the bottom
        if (DataContext is CatalogViewModel vm && vm.ProductsView.Count == 0)
            vm.LoadMoreCommand.Execute(null);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is CatalogViewModel vm && vm.ProductsView.Count == 0 && IsLoaded)
            vm.LoadMoreCommand.Execute(null);
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not CatalogViewModel vm) return;
        if (vm.IsLoading) return;

        var scrollViewer = (ScrollViewer)sender!;

        // Trigger when within 200px of the bottom
        double triggerThreshold = 200;
        double remaining = scrollViewer.ScrollableSize.Height
            - scrollViewer.Offset.Y
            - scrollViewer.Viewport.Height;

        if (remaining < triggerThreshold && vm.ProductsView.Count > 0)
        {
            // Cancel any previous delayed load
            _scrollCts?.Cancel();
            _scrollCts = new CancellationTokenSource();

            // Debounce: wait 100ms before loading next page
            Task.Delay(100, _scrollCts.Token)
                .ContinueWith(_ =>
                {
                    if (!_scrollCts.Token.IsCancellationRequested)
                        vm.LoadMoreCommand.Execute(null);
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
```

---

## 5. ListBox Virtualization Modes

Avalonia supports two virtualization modes for `ListBox` and `ItemsControl`:

| Mode | Description | Memory | When to Use |
|---|---|---|---|
| `Simple` | Realizes only visible items; recycles containers as user scrolls | Low | Large lists (10,000+), uniform items |
| `None` | Realizes all items immediately | High | Small lists (< 200 items), non-uniform items, animations |

```xml
<!-- Simple virtualization with recycling -->
<ListBox Items="{Binding ProductsView}"
         VirtualizationMode="Simple"
         ScrollViewer.IsScrollInertiaEnabled="True"
         ScrollViewer.HorizontalScrollBarVisibility="Disabled">

  <ListBox.ItemTemplate>
    <DataTemplate DataType="vm:Product">
      <TextBlock Text="{Binding Name}" Padding="8" />
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

### When to Avoid Virtualization

- **Animated item transitions** — `VirtualizationMode.None` is required because recycled containers break ongoing animations
- **Items with different heights** — virtualization assumes uniform item size; non-uniform items cause layout jumps
- **Expandable/collapsible items** — a `TreeView` equivalent with expand/collapse should use its own virtualization strategy

---

## 6. Pagination via UI (Page Buttons)

For scenarios where infinite scroll is inappropriate (audit logs, invoices, analytical reports):

```csharp
public sealed partial class PagedCatalogViewModel : ObservableObject
{
    private readonly AsyncLazyDataSource<Product> _products;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _canGoPrevious;

    [ObservableProperty]
    private bool _canGoNext;

    public ICollectionView ProductsView { get; }

    public PagedCatalogViewModel()
    {
        _products = new AsyncLazyDataSource<Product>(
            (page, size, ct) => LoadProductsAsync(page, size, ct), pageSize: 25);

        ProductsView = CollectionViewSource.GetDefaultView(_products);
    }

    [RelayCommand]
    private async Task GoToPageAsync(int page, CancellationToken ct)
    {
        if (page < 1) return;
        CurrentPage = page;
        IsLoading = true;

        // Reinitialize the data source for the new page
        await _products.InitializeAsync(ct);

        // Load remaining pages until we reach the desired page
        while (_products.TotalLoadedCount < page * 25 && _products.HasMore)
            await _products.LoadNextPageAsync(ct);

        CanGoPrevious = CurrentPage > 1;
        CanGoNext = _products.HasMore;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task NextPageAsync(CancellationToken ct) =>
        await GoToPageAsync(CurrentPage + 1, ct);

    [RelayCommand]
    private async Task PreviousPageAsync(CancellationToken ct) =>
        await GoToPageAsync(CurrentPage - 1, ct);

    private async Task<IReadOnlyList<Product>> LoadProductsAsync(int page, int size, CancellationToken ct)
    {
        await Task.Delay(200, ct);
        return Enumerable.Range(page * size, size)
            .Select(i => new Product { Id = i, Name = $"Product {i:D5}" })
            .ToList();
    }
}
```

### Page Button UI

```xml
<StackPanel Orientation="Horizontal" Spacing="4" HorizontalAlignment="Center">
  <Button Command="{Binding PreviousPageCommand}" IsEnabled="{Binding CanGoPrevious}"
          Content="◀ Previous" />

  <!-- Page number buttons -->
  <ItemsControl Items="{Binding PageNumbers}">
    <ItemsControl.ItemsPanel>
      <ItemsPanelTemplate>
        <StackPanel Orientation="Horizontal" Spacing="2" />
      </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Button Content="{Binding}"
                Command="{Binding $parent[ItemsControl].DataContext.GoToPageCommand}"
                CommandParameter="{Binding}"
                MinWidth="32" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>

  <Button Command="{Binding NextPageCommand}" IsEnabled="{Binding CanGoNext}"
          Content="Next ▶" />
</StackPanel>
```

---

## 7. SQLite-Backed Data Source — Full Implementation

```csharp
public sealed class SqliteProductDataSource
{
    private readonly string _connectionString;

    public SqliteProductDataSource(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL,
                Price REAL NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<Product>> LoadPageAsync(int page, int size, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Category, Price FROM Products ORDER BY Id LIMIT @size OFFSET @skip";
        cmd.Parameters.AddWithValue("@size", size);
        cmd.Parameters.AddWithValue("@skip", page * size);

        var results = new List<Product>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Price = reader.GetDecimal(3)
            });
        }
        return results;
    }

    public async Task<int> GetTotalCountAsync(CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Products";
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    public async Task SeedAsync(int count, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var categories = new[] { "Electronics", "Books", "Home", "Clothing", "Sports" };
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Products (Name, Category, Price) VALUES (@name, @cat, @price)";

        var nameParam = cmd.Parameters.Add("@name", SqliteType.Text);
        var catParam = cmd.Parameters.Add("@cat", SqliteType.Text);
        var priceParam = cmd.Parameters.Add("@price", SqliteType.Real);

        for (int i = 0; i < count; i++)
        {
            nameParam.Value = $"Product {i:D6}";
            catParam.Value = categories[i % categories.Length];
            priceParam.Value = Random.Shared.Next(100, 10000) / 100m;
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}

// Usage in ViewModel
public CatalogViewModel()
{
    var dataSource = new SqliteProductDataSource("products.db");
    _products = new AsyncLazyDataSource<Product>(
        (page, size, ct) => dataSource.LoadPageAsync(page, size, ct),
        pageSize: 100);
}
```

---

## 8. Error Handling and Retry

```csharp
public sealed class ResilientAsyncLazyDataSource<T> : ObservableCollection<T>
{
    private readonly AsyncLazyDataSource<T> _inner;
    private readonly int _maxRetries = 3;

    public ResilientAsyncLazyDataSource(
        Func<int, int, CancellationToken, Task<IReadOnlyList<T>>> pageLoader,
        int pageSize = 50,
        int maxRetries = 3)
    {
        _inner = new AsyncLazyDataSource<T>(pageLoader, pageSize);
        _maxRetries = maxRetries;
        _inner.OnPageError += OnPageError;
    }

    public new async Task LoadNextPageAsync(CancellationToken ct)
    {
        int attempts = 0;
        while (attempts < _maxRetries)
        {
            try
            {
                await _inner.LoadNextPageAsync(ct);
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                attempts++;
                if (attempts >= _maxRetries) throw;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)), ct);
            }
        }
    }

    private void OnPageError(Exception ex)
    {
        // Log and optionally notify user
    }
}
```

---

## 9. Testing Lazy Loading

```csharp
[TestClass]
public sealed class AsyncLazyDataSourceTests
{
    [TestMethod]
    public async Task LoadNextPageAsync_AddsItems()
    {
        var source = new AsyncLazyDataSource<int>(
            (page, size, ct) => Task.FromResult<IReadOnlyList<int>>(
                Enumerable.Range(page * size, size).ToList()),
            pageSize: 10);

        await source.InitializeAsync();
        Assert.AreEqual(10, source.Count);
        Assert.AreEqual(0, source[0]);
        Assert.AreEqual(9, source[9]);
    }

    [TestMethod]
    public async Task LoadNextPageAsync_AppendsItems()
    {
        var source = new AsyncLazyDataSource<int>(
            (page, size, ct) => Task.FromResult<IReadOnlyList<int>>(
                Enumerable.Range(page * size, size).ToList()),
            pageSize: 10);

        await source.InitializeAsync();
        await source.LoadNextPageAsync();
        Assert.AreEqual(20, source.Count);
        Assert.AreEqual(10, source[10]);
    }

    [TestMethod]
    public async Task LoadNextPageAsync_StopsWhenNoMoreData()
    {
        int callCount = 0;
        var source = new AsyncLazyDataSource<int>(
            (page, size, ct) =>
            {
                callCount++;
                if (page >= 2)
                    return Task.FromResult<IReadOnlyList<int>>(Array.Empty<int>());
                return Task.FromResult<IReadOnlyList<int>>(
                    Enumerable.Range(page * size, size).ToList());
            },
            pageSize: 10);

        await source.InitializeAsync();
        await source.LoadNextPageAsync();
        await source.LoadNextPageAsync();
        await source.LoadNextPageAsync(); // should be no-op

        Assert.AreEqual(20, source.Count);
        Assert.AreEqual(3, callCount); // Initialize + 2 successful loads
    }

    [TestMethod]
    public async Task InitializeAsync_ClearsExisting()
    {
        var source = new AsyncLazyDataSource<int>(
            (page, size, ct) => Task.FromResult<IReadOnlyList<int>>(
                Enumerable.Range(0, size).ToList()),
            pageSize: 10);

        await source.InitializeAsync();
        Assert.AreEqual(10, source.Count);
        await source.InitializeAsync();
        Assert.AreEqual(10, source.Count); // fresh load
    }
}
```

---

## Summary: Core vs. Verbose

| Concept | Core | Verbose |
|---|---|---|
| DataSource | Basic `AsyncLazyDataSource` | CancellationToken, prefetch, events, error handling |
| ViewModel | Simple `CatalogViewModel` | Filtering, sorting, search, status text, refresh |
| Infinite scroll | Basic ScrollViewer | Debounced scroll detection, loading indicator, inertia |
| ListBox virtual | One-line attribute | Mode comparison table, when to avoid |
| Pagination UI | Simple buttons | Full page-number UI, `CanGoPrevious`/`CanGoNext` |
| SQLite | Snippet | Full `SqliteProductDataSource` with `SeedAsync` |
| Error handling | None | `ResilientAsyncLazyDataSource` with retry + backoff |
| Testing | None | 4 unit tests with `[TestClass]` |
