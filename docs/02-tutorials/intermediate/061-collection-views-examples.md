---
tier: intermediate
topic: data
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 061E — Collection Views (examples)

## Example 1: Filterable person list

**ViewModel:**

```csharp
public partial class PersonListViewModel : ObservableObject
{
    private readonly List<Person> _all = new()
    {
        new("Alice", 30), new("Bob", 25),
        new("Charlie", 35), new("Diana", 28),
    };

    [ObservableProperty]
    private string _filter = "";

    public ObservableCollection<Person> Items { get; } = new();

    public PersonListViewModel() => ApplyFilter();

    partial void OnFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Items.Clear();
        var query = string.IsNullOrWhiteSpace(Filter)
            ? _all
            : _all.Where(p => p.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase));
        foreach (var p in query) Items.Add(p);
    }
}

public record Person(string Name, int Age);
```

**View:**

```xml
<StackPanel Spacing="8">
  <TextBox Text="{Binding Filter, Delay=300}" PlaceholderText="Search..." />
  <ListBox ItemsSource="{Binding Items}" DisplayMemberBinding="{Binding Name}" />
</StackPanel>
```

---

## Example 2: DataGrid with grouped products

**ViewModel:**

```csharp
public partial class ProductGridViewModel : ObservableObject
{
    public DataGridCollectionView GroupedView { get; }

    public ProductGridViewModel()
    {
        var products = new List<Product>
        {
            new("Widget", "Hardware", 9.99m),
            new("Gadget", "Hardware", 24.99m),
            new("App", "Software", 4.99m),
            new("Plugin", "Software", 14.50m),
            new("Service", "Services", 99.00m),
        };
        GroupedView = new DataGridCollectionView(products);
        GroupedView.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));
        GroupedView.SortDescriptions.Add(
            new DataGridSortDescription("Price", ListSortDirection.Ascending));
    }
}

public record Product(string Name, string Category, decimal Price);
```

**View:**

```xml
<DataGrid ItemsSource="{Binding GroupedView}" AutoGenerateColumns="False"
          LoadingRowGroup="OnLoadingRowGroup">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*" />
    <DataGridTextColumn Header="Price" Binding="{Binding Price, StringFormat='{}{0:C}'}" />
  </DataGrid.Columns>
</DataGrid>
```

```csharp
private void OnLoadingRowGroup(object? sender, DataGridRowGroupHeaderEventArgs e)
{
    if (e.RowGroupHeader.DataContext is DataGridCollectionViewGroup g)
        e.RowGroupHeader.PropertyValue = $"{g.Key} ({g.ItemCount} items)";
}
```

---

## Example 3: Multiple sort columns with toggles

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
  <RadioButton GroupName="Sort" Content="Name" IsChecked="{Binding SortByName}" />
  <RadioButton GroupName="Sort" Content="Age" IsChecked="{Binding SortByAge}" />
  <CheckBox Content="Descending" IsChecked="{Binding SortDesc}" />
</StackPanel>
<ListBox ItemsSource="{Binding Items}" />
```

```csharp
public partial class SortViewModel : ObservableObject
{
    private readonly List<Person> _all = Person.SampleData();
    public ObservableCollection<Person> Items { get; } = new();

    [ObservableProperty] private bool _sortByName = true;
    [ObservableProperty] private bool _sortByAge;
    [ObservableProperty] private bool _sortDesc;

    partial void OnSortByNameChanged(bool v) { if (v) ReSort(); }
    partial void OnSortByAgeChanged(bool v) { if (v) ReSort(); }
    partial void OnSortDescChanged(bool v) => ReSort();

    private void ReSort()
    {
        var sorted = (SortByName, SortDesc) switch
        {
            (true, false) => _all.OrderBy(p => p.Name),
            (true, true)  => _all.OrderByDescending(p => p.Name),
            (false, false) => _all.OrderBy(p => p.Age),
            (false, true)  => _all.OrderByDescending(p => p.Age),
        };
        Items.Clear();
        foreach (var p in sorted) Items.Add(p);
    }
}
```

---

## Example 4: DynamicData real-time filter + sort

```csharp
using DynamicData;
using DynamicData.Binding;

public partial class ReactiveListViewModel : ObservableObject, IDisposable
{
    private readonly SourceList<Person> _source = new();
    private readonly IDisposable _cleanup;

    [ObservableProperty]
    private string _searchText = "";

    public ReadOnlyObservableCollection<Person> Items { get; }

    public ReactiveListViewModel()
    {
        _source.AddRange(Person.SampleData());

        var filterPredicate = this.WhenPropertyChanged(x => x.SearchText)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Select(x => CreateFilter(x.Value));

        _cleanup = _source.Connect()
            .Filter(filterPredicate)
            .Sort(SortExpressionComparer<Person>.Ascending(p => p.Name))
            .ObserveOn(AvaloniaSynchronizationContext.Current)
            .Bind(out var items)
            .Subscribe();

        Items = items;
    }

    private static Func<Person, bool> CreateFilter(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? _ => true
            : p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || p.Age.ToString().Contains(text);

    public void Dispose() => _cleanup.Dispose();
}
```

---

## Example 5: Grouped list with header count

```csharp
public abstract class GroupItem { }

public class Header(string name, int count) : GroupItem
{
    public string Name => name;
    public int Count => count;
}

public class Entry(Person person) : GroupItem
{
    public Person Person => person;
}

public partial class GroupedListViewModel : ObservableObject
{
    private readonly List<Person> _all = Person.SampleData();
    public ObservableCollection<GroupItem> Items { get; } = new();

    [RelayCommand]
    private void GroupByAge()
    {
        Items.Clear();
        foreach (var g in _all.GroupBy(p => p.Age / 10 * 10).OrderBy(g => g.Key))
        {
            Items.Add(new Header($"{g.Key}s", g.Count()));
            foreach (var p in g.OrderBy(p => p.Name))
                Items.Add(new Entry(p));
        }
    }

    [RelayCommand]
    private void Ungroup()
    {
        Items.Clear();
        foreach (var p in _all.OrderBy(p => p.Name))
            Items.Add(new Entry(p));
    }
}
```

```xml
<StackPanel Spacing="8">
  <StackPanel Orientation="Horizontal" Spacing="8">
    <Button Content="Group by Decade" Command="{Binding GroupByAgeCommand}" />
    <Button Content="Ungroup" Command="{Binding UngroupCommand}" />
  </StackPanel>
  <ListBox ItemsSource="{Binding Items}">
    <ListBox.DataTemplates>
      <DataTemplate DataType="local:Header">
        <TextBlock Text="{Binding Name, StringFormat='{0} ({1} items)'}"
                   FontWeight="Bold" Margin="0,8,0,4" />
      </DataTemplate>
      <DataTemplate DataType="local:Entry">
        <TextBlock Text="{Binding Person.Name}" Margin="12,0,0,0" />
      </DataTemplate>
    </ListBox.DataTemplates>
  </ListBox>
</StackPanel>
```

---

## See Also

- [061 — Collection Views (core)](061-collection-views.md)
- [061V — Collection Views (verbose)](061-collection-views-verbose.md)
- [009 — Collections & ObservableCollection](../basics/009-collections-and-observablecollection.md)
- [015 — ItemsControl & ListBox](../basics/015-itemscontrol-listbox.md)
