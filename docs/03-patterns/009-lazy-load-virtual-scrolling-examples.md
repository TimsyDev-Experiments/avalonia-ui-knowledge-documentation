---
tier: advanced
topic: performance
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 009-lazy-load-virtual-scrolling.md
---

# 009X — Lazy Load / Virtual Scrolling: Real-World Examples

## Example 1: Product Catalog with Infinite Scroll and Filtering

A product catalog that loads items from an API as the user scrolls, with real-time text search filtering.

### Data Source

```csharp
public sealed class ProductApiClient
{
    private static readonly string[] Categories = { "Electronics", "Books", "Home", "Clothing", "Sports" };
    private static readonly string[] Adjectives = { "Premium", "Basic", "Pro", "Ultra", "Eco", "Classic" };

    public async Task<IReadOnlyList<Product>> LoadProductsAsync(int page, int size, CancellationToken ct)
    {
        // Simulate network latency
        await Task.Delay(Random.Shared.Next(200, 600), ct);

        // Simulate occasional API failure
        if (Random.Shared.NextDouble() < 0.05)
            throw new HttpRequestException("Simulated network error");

        return Enumerable.Range(page * size, size)
            .Select(i => new Product
            {
                Id = i,
                Name = $"{Adjectives[Random.Shared.Next(Adjectives.Length)]} Product {i:D5}",
                Category = Categories[Random.Shared.Next(Categories.Length)],
                Price = Math.Round((decimal)(Random.Shared.NextDouble() * 500 + 1), 2),
                StockCount = Random.Shared.Next(0, 1000),
                Rating = Math.Round(Random.Shared.NextDouble() * 5, 1)
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
    public int StockCount { get; init; }
    public double Rating { get; init; }
    public bool InStock => StockCount > 0;
}
```

### ViewModel

```csharp
public sealed partial class ProductCatalogViewModel : ObservableObject
{
    private readonly AsyncLazyDataSource<Product> _products;
    private readonly ProductApiClient _api = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    public ICollectionView ProductsView { get; }
    public IEnumerable<string> Categories { get; } = new[] { "All", "Electronics", "Books", "Home", "Clothing", "Sports" };

    public ProductCatalogViewModel()
    {
        _products = new AsyncLazyDataSource<Product>(
            (page, size, ct) => _api.LoadProductsAsync(page, size, ct),
            pageSize: 30, prefetchPages: 1);

        ProductsView = CollectionViewSource.GetDefaultView(_products);
        ProductsView.Filter = FilterProduct;

        _products.OnPageLoaded += (start, count) =>
        {
            StatusText = $"{_products.TotalLoadedCount} products loaded";
        };

        _products.OnPageError += ex =>
        {
            StatusText = $"Error: {ex.Message}. Scroll to retry.";
        };
    }

    [RelayCommand]
    private async Task LoadMoreAsync(CancellationToken ct)
    {
        IsLoading = true;
        try { await _products.LoadNextPageAsync(ct); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        StatusText = "Refreshing...";
        await _products.RefreshAsync(ct);
        StatusText = $"{_products.TotalLoadedCount} products loaded";
    }

    partial void OnSearchTextChanged(string value)
    {
        ProductsView.Refresh();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ProductsView.Refresh();
    }

    private bool FilterProduct(object obj)
    {
        if (obj is not Product p) return false;

        if (!string.IsNullOrEmpty(SearchText) &&
            !p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            return false;

        if (SelectedCategory != "All" && p.Category != SelectedCategory)
            return false;

        return true;
    }
}
```

### View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="MyApp.Views.ProductCatalogView">
  <DockPanel>
    <!-- Toolbar -->
    <Border DockPanel.Dock="Top" Padding="8" Background="{StaticResource SystemChromeLowColor}">
      <WrapPanel Spacing="8">
        <TextBox Watermark="Search products..." Text="{Binding SearchText}" Width="200" />
        <ComboBox Items="{Binding Categories}" SelectedItem="{Binding SelectedCategory}" Width="130" />
        <Button Command="{Binding RefreshCommand}" Content="⟳ Refresh" />
        <TextBlock Text="{Binding StatusText}" VerticalAlignment="Center" />
      </WrapPanel>
    </Border>

    <!-- Virtualized list -->
    <ScrollViewer Name="ScrollViewer"
                  ScrollChanged="OnScrollChanged">
      <ItemsControl Items="{Binding ProductsView}"
                    VirtualizationMode="Simple">
        <ItemsControl.ItemTemplate>
          <DataTemplate DataType="vm:Product">
            <Border Padding="12,8" Margin="4,2"
                    BorderBrush="LightGray" BorderThickness="0,0,0,1">
              <Grid ColumnDefinitions="*,Auto,Auto">
                <StackPanel>
                  <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
                  <TextBlock Text="{Binding Category}" FontSize="11" Opacity="0.6" />
                  <TextBlock Text="{Binding Rating, StringFormat='★ {0:F1}'}" FontSize="11" />
                </StackPanel>
                <TextBlock Grid.Column="1" Text="{Binding Price, StringFormat='${0:N2}'}"
                           FontWeight="Bold" VerticalAlignment="Center" Margin="16,0" />
                <TextBlock Grid.Column="2"
                           Text="{Binding StockCount, StringFormat='Stock: {0}'}"
                           VerticalAlignment="Center"
                           Foreground="{Binding InStock, Converter={StaticResource BoolToGreenRed}}" />
              </Grid>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>

    <!-- Loading indicator -->
    <Border DockPanel.Dock="Bottom" IsVisible="{Binding IsLoading}" Padding="8">
      <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Spacing="8">
        <ProgressBar IsIndeterminate="True" Width="120" Height="16" />
        <TextBlock Text="Loading more products..." />
      </StackPanel>
    </Border>
  </DockPanel>
</UserControl>
```

### Code-Behind

```csharp
public partial class ProductCatalogView : UserControl
{
    public ProductCatalogView()
    {
        InitializeComponent();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not ProductCatalogViewModel vm || vm.IsLoading) return;
        if (sender is not ScrollViewer sv) return;

        double threshold = 300;
        double remaining = sv.ScrollableSize.Height - sv.Offset.Y - sv.Viewport.Height;

        if (remaining < threshold)
            vm.LoadMoreCommand.Execute(null);
    }
}
```

---

## Example 2: SQLite-Backed Log Viewer

A log viewer that loads entries from a SQLite database with date-range filtering and pagination.

### Data Layer

```csharp
public sealed class LogDatabase
{
    private readonly string _connectionString;

    public LogDatabase(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                Level TEXT NOT NULL,
                Source TEXT NOT NULL,
                Message TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    public async Task<IReadOnlyList<LogEntry>> LoadPageAsync(
        int page, int size, LogFilter? filter, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        var cmd = conn.CreateCommand();
        var where = new List<string>();

        if (filter?.Level is not null)
        {
            where.Add("Level = @level");
            cmd.Parameters.AddWithValue("@level", filter.Level);
        }
        if (filter?.FromDate is not null)
        {
            where.Add("Timestamp >= @from");
            cmd.Parameters.AddWithValue("@from", filter.FromDate.Value.ToString("O"));
        }
        if (filter?.ToDate is not null)
        {
            where.Add("Timestamp <= @to");
            cmd.Parameters.AddWithValue("@to", filter.ToDate.Value.ToString("O"));
        }
        if (!string.IsNullOrEmpty(filter?.SearchText))
        {
            where.Add("Message LIKE @search");
            cmd.Parameters.AddWithValue("@search", $"%{filter.SearchText}%");
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
        cmd.CommandText = $"SELECT Id, Timestamp, Level, Source, Message FROM Logs {whereClause} ORDER BY Id DESC LIMIT @size OFFSET @skip";
        cmd.Parameters.AddWithValue("@size", size);
        cmd.Parameters.AddWithValue("@skip", page * size);

        var results = new List<LogEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new LogEntry
            {
                Id = reader.GetInt32(0),
                Timestamp = DateTime.Parse(reader.GetString(1)),
                Level = reader.GetString(2),
                Source = reader.GetString(3),
                Message = reader.GetString(4)
            });
        }
        return results;
    }

    public async Task<int> GetTotalCountAsync(LogFilter? filter, CancellationToken ct)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Logs";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }
}

public sealed record LogEntry
{
    public int Id { get; init; }
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = "";
    public string Source { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record LogFilter
{
    public string? Level { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? SearchText { get; init; }
}
```

### ViewModel

```csharp
public sealed partial class LogViewerViewModel : ObservableObject
{
    private readonly LogDatabase _db;
    private readonly AsyncLazyDataSource<LogEntry> _logs;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _selectedLevel = "All";

    [ObservableProperty]
    private string _searchText = "";

    public ICollectionView LogsView { get; }

    public IEnumerable<string> Levels { get; } = new[] { "All", "INFO", "WARN", "ERROR", "DEBUG", "FATAL" };

    public LogViewerViewModel(LogDatabase db)
    {
        _db = db;
        _logs = new AsyncLazyDataSource<LogEntry>(
            (page, size, ct) => _db.LoadPageAsync(page, size, GetCurrentFilter(), ct),
            pageSize: 100);

        LogsView = CollectionViewSource.GetDefaultView(_logs);
    }

    [RelayCommand]
    private async Task LoadMoreAsync(CancellationToken ct)
    {
        IsLoading = true;
        try { await _logs.LoadNextPageAsync(ct); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        await _logs.RefreshAsync(ct);
        StatusText = $"{_logs.TotalLoadedCount} log entries";
    }

    private LogFilter GetCurrentFilter()
    {
        return new LogFilter
        {
            Level = SelectedLevel == "All" ? null : SelectedLevel,
            SearchText = string.IsNullOrEmpty(SearchText) ? null : SearchText
        };
    }
}
```

---

## Example 3: Paginated Search Results with Page Controls

A search results page that shows 25 results at a time with numbered page buttons and next/previous navigation.

### ViewModel

```csharp
public sealed partial class SearchViewModel : ObservableObject
{
    private readonly AsyncLazyDataSource<SearchResult> _results;
    private int _totalResults;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private bool _canGoPrevious;

    [ObservableProperty]
    private bool _canGoNext;

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private ObservableCollection<int> _pageNumbers = new();

    [ObservableProperty]
    private string _summaryText = "";

    public ICollectionView ResultsView { get; }

    private const int PageSize = 25;

    public SearchViewModel()
    {
        _results = new AsyncLazyDataSource<SearchResult>(
            (page, size, ct) => SearchApiAsync(page, size, ct),
            PageSize);

        ResultsView = CollectionViewSource.GetDefaultView(_results);
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        CurrentPage = 1;
        _totalResults = 0;
        await _results.RefreshAsync(ct);
        UpdatePagination();
    }

    [RelayCommand]
    private async Task GoToPageAsync(int page, CancellationToken ct)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
        await _results.RefreshAsync(ct);

        // Load pages until we reach the desired page
        while (_results.TotalLoadedCount < page * PageSize && _results.HasMore)
            await _results.LoadNextPageAsync(ct);

        UpdatePagination();
    }

    [RelayCommand]
    private async Task NextPageAsync(CancellationToken ct) =>
        await GoToPageAsync(CurrentPage + 1, ct);

    [RelayCommand]
    private async Task PreviousPageAsync(CancellationToken ct) =>
        await GoToPageAsync(CurrentPage - 1, ct);

    private void UpdatePagination()
    {
        CanGoPrevious = CurrentPage > 1;
        CanGoNext = CurrentPage < TotalPages;
        SummaryText = $"Page {CurrentPage} of {TotalPages} ({_totalResults} results)";

        // Build page number list (show max 10 pages)
        PageNumbers.Clear();
        int start = Math.Max(1, CurrentPage - 4);
        int end = Math.Min(TotalPages, start + 9);
        for (int i = start; i <= end; i++)
            PageNumbers.Add(i);
    }

    private async Task<IReadOnlyList<SearchResult>> SearchApiAsync(int page, int size, CancellationToken ct)
    {
        await Task.Delay(400, ct);
        _totalResults = 1000; // from API response header
        TotalPages = (int)Math.Ceiling((double)_totalResults / size);

        return Enumerable.Range(page * size, size)
            .Select(i => new SearchResult
            {
                Title = $"Result {i} for '{Query}'",
                Url = $"https://example.com/result/{i}",
                Snippet = $"This is the description for result number {i}."
            })
            .ToList();
    }
}

public sealed class SearchResult
{
    public string Title { get; init; } = "";
    public string Url { get; init; } = "";
    public string Snippet { get; init; } = "";
}
```

### View

```xml
<DockPanel>
  <!-- Search bar -->
  <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
    <TextBox Text="{Binding Query}" Watermark="Search..." Width="300"
             KeyDown="OnSearchKeyDown" />
    <Button Command="{Binding SearchCommand}" Content="Search" />
    <TextBlock Text="{Binding SummaryText}" VerticalAlignment="Center" />
  </StackPanel>

  <!-- Results -->
  <ScrollViewer>
    <ItemsControl Items="{Binding ResultsView}">
      <ItemsControl.ItemTemplate>
        <DataTemplate DataType="vm:SearchResult">
          <Border Padding="8" Margin="0,0,0,4" BorderBrush="LightGray" BorderThickness="0,0,0,1">
            <TextBlock Text="{Binding Title}" FontWeight="SemiBold" />
          </Border>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>
  </ScrollViewer>

  <!-- Pagination controls -->
  <Border DockPanel.Dock="Bottom" Padding="8" Background="{StaticResource SystemChromeLowColor}">
    <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Spacing="4">
      <Button Command="{Binding PreviousPageCommand}" IsEnabled="{Binding CanGoPrevious}"
              Content="◀" ToolTip.Tip="Previous page" />
      <ItemsControl Items="{Binding PageNumbers}">
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" Spacing="2" />
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate DataType="x:Int32">
            <Button Content="{Binding}"
                    Command="{Binding $parent[ItemsControl].DataContext.GoToPageCommand}"
                    CommandParameter="{Binding}"
                    MinWidth="30" Height="30" />
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
      <Button Command="{Binding NextPageCommand}" IsEnabled="{Binding CanGoNext}"
              Content="▶" ToolTip.Tip="Next page" />
    </StackPanel>
  </Border>
</DockPanel>
```

### Code-Behind

```csharp
public partial class SearchView : UserControl
{
    public SearchView() => InitializeComponent();

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is SearchViewModel vm)
        {
            vm.SearchCommand.Execute(null);
            e.Handled = true;
        }
    }
}
```
