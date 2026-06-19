---
tier: intermediate
topic: data
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 061V — Collection Views (verbose companion)

**What this covers:** Deep dive into DataGridCollectionView internals, DynamicData pipeline operators, performance trade-offs, and view-model architecture for collection manipulation.

**Prerequisites:** 061 — Collection Views core

---

## 1. Why no ICollectionView?

WPF's `ICollectionView` was built into the framework because WPF was designed for data-heavy business apps in an era before reactive libraries. Avalonia deliberately omits it because:

- View-model-first composition is cleaner and testable
- Third-party libraries (DynamicData, Rx.NET) offer far richer reactive pipelines
- `DataGridCollectionView` exists for DataGrid-specific scenarios
- Avoids coupling UI concerns (sorting, grouping) into the data-binding layer

---

## 2. DataGridCollectionView internals

`DataGridCollectionView` wraps any `IEnumerable` and provides:

| Feature | API |
|---------|-----|
| Sorting | `SortDescriptions` collection |
| Grouping | `GroupDescriptions` collection |
| Filtering | `Filter` predicate property |
| Live shaping | `IsLiveSorting`, `IsLiveFiltering`, `IsLiveGrouping` |
| Deferred refresh | `DeferRefresh()` — batch multiple changes |
| Current item | `MoveCurrentTo`, `CurrentItem`, `IsCurrentBeforeFirst` |

### Live shaping

Enable live shaping so the view automatically re-sorts/filters/group when item properties change:

```csharp
GroupedProducts.IsLiveSorting = true;
GroupedProducts.IsLiveGrouping = true;
GroupedProducts.LiveSortingProperties.Add("Price");
GroupedProducts.LiveGroupingProperties.Add("Category");
```

This requires items to implement `INotifyPropertyChanged`. When `Price` changes on any item, the view re-sorts without needing a manual refresh.

### Deferred refresh

```csharp
using (GroupedProducts.DeferRefresh())
{
    GroupedProducts.GroupDescriptions.Clear();
    GroupedProducts.GroupDescriptions.Add(new DataGridPathGroupDescription("Department"));
    GroupedProducts.SortDescriptions.Add(new DataGridSortDescription("Name", ListSortDirection.Ascending));
}
```

The view only refreshes once when `DeferRefresh` is disposed, avoiding multiple re-evaluations.

---

## 3. DynamicData pipeline operators

DynamicData provides a rich operator set:

| Operator | Purpose |
|----------|---------|
| `Filter` | Include/exclude items by predicate |
| `Sort` | Maintain sorted order with `SortExpressionComparer` |
| `GroupOn` | Group by a key, returns `IGrouping` |
| `Transform` | Project each item (like LINQ `Select`) |
| `Bind` | Bind the result to an observable collection |
| `Subscribe` | Trigger side effects on change |
| `Throttle` | Debounce changes (useful for search inputs) |
| `ObserveOn` | Switch to dispatcher thread for UI updates |

### Pipeline composition

```csharp
_source.Connect()
    .Filter(filterPredicate)
    .Sort(SortExpressionComparer<Person>.Ascending(p => p.Name))
    .Throttle(TimeSpan.FromMilliseconds(200))
    .ObserveOn(AvaloniaSynchronizationContext.Current)
    .Bind(out _filtered)
    .Subscribe();
```

---

## 4. Performance considerations

| Collection size | Approach | Notes |
|-----------------|----------|-------|
| < 1,000 | Manual `ObservableCollection` rebuild | Simple, no external deps |
| 1,000–10,000 | `DynamicData` | Incremental updates only |
| 10,000+ | `DynamicData` + virtualization | Use `ListBox` or `DataGrid` with virtualizing panels |
| Grouped + large | `DataGridCollectionView` | Built-in virtualized grouping display |

### Avoiding full rebuilds

The manual approach (clear + re-add) triggers a full UI update. DynamicData only sends delta changes — adds, removes, and updates — which is significantly faster for large collections.

---

## 5. Grouped collection with DataGridCollectionView

```csharp
GroupedProducts = new DataGridCollectionView(products);
GroupedProducts.GroupDescriptions.Add(new DataGridPathGroupDescription("Category"));
GroupedProducts.GroupDescriptions.Add(new DataGridPathGroupDescription("SubCategory"));
```

### Custom group header via LoadingRowGroup

```csharp
private void OnLoadingRowGroup(object? sender, DataGridRowGroupHeaderEventArgs e)
{
    if (e.RowGroupHeader.DataContext is DataGridCollectionViewGroup group)
    {
        e.RowGroupHeader.PropertyValue = $"{group.Key} ({group.ItemCount})";
    }
}
```

### Expand/collapse programmatically

```csharp
if (viewModel.GroupedProducts.Groups is { } groups)
{
    foreach (var group in groups.OfType<DataGridCollectionViewGroup>())
        myDataGrid.CollapseRowGroup(group, collapseAllSubgroups: true);
}
```

---

## 6. Batch updates with ObservableCollection

For manual approaches, use `AddRange` from `System.Collections.Generic` or loop with `Clear` then `AddRange`:

```csharp
// No built-in AddRange in OC; use a helper
public static class ObservableCollectionExtensions
{
    public static void AddRange<T>(this ObservableCollection<T> oc, IEnumerable<T> items)
    {
        foreach (var item in items) oc.Add(item);
    }
}
```

For minimum UI updates, consider building a new collection and assigning it:

```csharp
[ObservableProperty]
private ObservableCollection<Person> _filteredPersons = new();

private void ApplyFilter()
{
    var filtered = string.IsNullOrWhiteSpace(SearchText)
        ? _allPersons
        : _allPersons.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    FilteredPersons = new ObservableCollection<Person>(filtered);
}
```

---

## 7. Filtering with debounce

```xml
<TextBox Text="{Binding SearchText, Delay=300}" />
```

The `Delay=300` on the binding defers source updates by 300ms after the user stops typing.

With DynamicData, use `Throttle`:

```csharp
this.WhenPropertyChanged(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Select(x => CreateFilter(x.Value))
    .Subscribe(predicate => _source.Connect().Filter(predicate).Bind(out _filtered).Subscribe());
```

---

## See Also

- [061 — Collection Views (core)](061-collection-views.md)
- [061E — Collection Views (examples)](061-collection-views-examples.md)
- [DataGrid How-To](https://docs.avaloniaui.net/docs/how-to/datagrid-how-to#grouping)
- [DynamicData Docs](https://dynamic-data.org/)
