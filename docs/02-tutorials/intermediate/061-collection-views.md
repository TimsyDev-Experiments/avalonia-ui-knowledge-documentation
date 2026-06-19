---
tier: intermediate
topic: data
estimated: 14 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 061 — Collection Views

**What you'll learn:** How to filter, sort, and group collections in Avalonia using view-model patterns, DynamicData, and `DataGridCollectionView`.

**Prerequisites:** [009 — Collections & ObservableCollection](../basics/009-collections-and-observablecollection.md), [015 — ItemsControl & ListBox](../basics/015-itemscontrol-listbox.md)

---

## 1. The Avalonia approach

Unlike WPF, Avalonia does **not** have a built-in `ICollectionView`. Filtering, sorting, and grouping are done in the view model before binding.

### Manual filtering

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly List<Person> _allPersons;

    [ObservableProperty]
    private string _searchText = "";

    public ObservableCollection<Person> FilteredPersons { get; } = new();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredPersons.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allPersons
            : _allPersons.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        foreach (var p in filtered) FilteredPersons.Add(p);
    }
}
```

```xml
<TextBox Text="{Binding SearchText}" PlaceholderText="Search..." />
<ListBox ItemsSource="{Binding FilteredPersons}" />
```

---

## 2. Dynamic sorting

```csharp
[ObservableProperty]
private string _sortColumn = "Name";

[ObservableProperty]
private bool _sortDescending;

partial void OnSortColumnChanged(string value) => ApplySort();
partial void OnSortDescendingChanged(bool value) => ApplySort();

private void ApplySort()
{
    var sorted = (_sortColumn, _sortDescending) switch
    {
        ("Name", false) => _allPersons.OrderBy(p => p.Name),
        ("Name", true)  => _allPersons.OrderByDescending(p => p.Name),
        ("Age",  false) => _allPersons.OrderBy(p => p.Age),
        ("Age",  true)  => _allPersons.OrderByDescending(p => p.Age),
        _ => _allPersons.AsEnumerable()
    };
    FilteredPersons.Clear();
    foreach (var p in sorted) FilteredPersons.Add(p);
}
```

```xml
<ComboBox SelectedItem="{Binding SortColumn}">
  <ComboBoxItem Content="Name" />
  <ComboBoxItem Content="Age" />
</ComboBox>
<ToggleButton IsChecked="{Binding SortDescending}" Content="Desc" />
```

---

## 3. Grouping with flat lists

Avalonia has no built-in grouping. Create a flat list with header items:

```csharp
public abstract class ListItem { }
public class GroupHeader(string title) : ListItem
{
    public string Title => title;
}
public class ItemViewModel(Person person) : ListItem
{
    public Person Person => person;
}
```

```csharp
public ObservableCollection<ListItem> Items { get; } = new();

private void BuildGroups()
{
    Items.Clear();
    foreach (var group in _allPersons.GroupBy(p => p.Age / 10 * 10).OrderBy(g => g.Key))
    {
        Items.Add(new GroupHeader($"{group.Key}s"));
        foreach (var p in group.OrderBy(p => p.Name))
            Items.Add(new ItemViewModel(p));
    }
}
```

```xml
<ListBox ItemsSource="{Binding Items}">
  <ListBox.DataTemplates>
    <DataTemplate DataType="local:GroupHeader">
      <TextBlock Text="{Binding Title}" FontWeight="Bold" Margin="0,8,0,4" />
    </DataTemplate>
    <DataTemplate DataType="local:ItemViewModel">
      <TextBlock Text="{Binding Person.Name}" Margin="12,0,0,0" />
    </DataTemplate>
  </ListBox.DataTemplates>
</ListBox>
```

---

## 4. Reactive filtering with DynamicData

The [DynamicData](https://github.com/reactivemarbles/DynamicData) library provides reactive collection transformations:

```csharp
using DynamicData;
using DynamicData.Binding;

public partial class MainViewModel : ObservableObject
{
    private readonly SourceList<Person> _source = new();
    private readonly ReadOnlyObservableCollection<Person> _filtered;

    public ReadOnlyObservableCollection<Person> FilteredPersons => _filtered;

    [ObservableProperty]
    private string _searchText = "";

    partial void OnSearchTextChanged(string value)
    {
        _source.Edit(list =>
        {
            list.Clear();
            list.AddRange(_allPersons.Where(CreateFilter(value)));
        });
    }

    public MainViewModel()
    {
        _source.AddRange(_allPersons);

        _source.Connect()
            .Filter(this.WhenPropertyChanged(x => x.SearchText)
                .Select(x => CreateFilter(x.Value)))
            .Sort(SortExpressionComparer<Person>.Ascending(p => p.Name))
            .Bind(out _filtered)
            .Subscribe();
    }

    private static Func<Person, bool> CreateFilter(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? _ => true
            : p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
}
```

---

## 5. DataGridCollectionView (DataGrid grouping)

For `DataGrid`, use `DataGridCollectionView` for built-in sorting and grouping:

```csharp
using Avalonia.Collections;

public DataGridCollectionView GroupedProducts { get; }

public MainViewModel()
{
    var products = new List<Product>
    {
        new("Widget", "Hardware", 9.99m),
        new("App", "Software", 4.99m),
    };
    GroupedProducts = new DataGridCollectionView(products);
    GroupedProducts.GroupDescriptions.Add(
        new DataGridPathGroupDescription("Category"));
}
```

```xml
<DataGrid ItemsSource="{Binding GroupedProducts}" />
```

---

## 6. Best practices

| Scenario | Approach |
|----------|----------|
| Small collections, simple filter | Manual `ObservableCollection` rebuild |
| Large collections, reactive | `DynamicData` with `SourceList` |
| DataGrid with grouping | `DataGridCollectionView` |
| Read-only display | `ReadOnlyObservableCollection<T>` |
| Debounced search input | `Delay=300` on binding or `Throttle` in DynamicData |

---

## Key Takeaways

- Filter/sort/group in the view model, not in controls
- Manual rebuild works for small collections; use DynamicData for large ones
- `DataGridCollectionView` provides WPF-like grouping in `DataGrid`
- Use `ReadOnlyObservableCollection<T>` to prevent external mutation
- Debounce search inputs to avoid rebuilding on every keystroke

---

## See Also

- [061V — Collection Views (verbose)](061-collection-views-verbose.md)
- [061E — Collection Views (examples)](061-collection-views-examples.md)
- [Avalonia Docs: Collection Views](https://docs.avaloniaui.net/docs/data-binding/collection-views)
- [Avalonia Docs: DataGrid Grouping](https://docs.avaloniaui.net/docs/how-to/datagrid-how-to#grouping)
- [DynamicData on GitHub](https://github.com/reactivemarbles/DynamicData)
