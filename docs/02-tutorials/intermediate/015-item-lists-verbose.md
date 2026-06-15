---
tier: intermediate
topic: data display
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 015-item-lists.md
---

# 015V — Item Lists: An In-Depth Companion

**Why this exists:** The original tutorial surveys `ListBox`, `ItemsRepeater`, and `DataGrid`. This companion explains *how virtualization works internally*, *what each control does during measure and arrange*, *how to choose between them* for different data shapes, and *what happens inside `ObservableCollection` when you add/remove items*.

**Cross-reference:** Original tutorial at [015-item-lists.md](015-item-lists.md).

---

## 1. How item controls work — the common architecture

Every Avalonia item control (`ItemsControl`, `ListBox`, `ItemsRepeater`, `DataGrid`) follows the same basic pattern:

1. **ItemsSource** — provides the data (must implement `IEnumerable`; for dynamic updates, `INotifyCollectionChanged`).
2. **ItemTemplate** — a `DataTemplate` that produces a `Control` for each item.
3. **Container generation** — for each item, the control creates a container (e.g., `ListBoxItem` for `ListBox`, `DataGridRow` for `DataGrid`), applies the template, and sets `DataContext` to the item.
4. **Layout** — the containers are measured and arranged according to the panel's layout logic.

**The distinction between data items and containers:** An `ObservableCollection<TodoItem>` has 1,000 items. A `ListBox` does not create 1,000 `ListBoxItem` containers — it creates only the visible ones (plus a few extra for buffering). This is virtualization. The container is recycled as the user scrolls: when an item scrolls out of view, its container is repopulated with the next incoming item.

---

## 2. ListBox — selection and virtualization

```xml
<ListBox ItemsSource="{Binding Items}"
         SelectedItem="{Binding SelectedItem}"
         SelectionMode="Single">
  <ListBox.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <TextBlock Text="{Binding Title}" />
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

### Selection modes

| Mode | Behavior |
|---|---|
| `Single` | One item selected at a time. Clicking a different item deselects the previous. |
| `Multiple` | Multiple items can be selected by clicking each one independently. Clicking a selected item deselects it. |
| `Toggle` | Clicking an item toggles it on/off without affecting other items. Unlike `Multiple`, `Toggle` allows zero or more selections without modifier keys. |

**What `SelectedItem` binding does:** When the user clicks an item, `ListBox` sets its `SelectedItem` to the clicked item's data item (the `TodoItem` instance). The binding updates the ViewModel property. Conversely, if the ViewModel sets `SelectedItem` to a new `TodoItem` that is in `Items`, the `ListBox` scrolls to and highlights that item.

**What `SelectedItems` binding does (for Multiple/Toggle):** `SelectedItems` is a collection of selected data items. It does **not** support two-way binding in Avalonia — use `SelectionChanged` event or bind the `SelectedIndex`/`SelectedItem` for single-select scenarios.

### Text search

```xml
<ListBox TextSearchEnabled="True">
```

This enables keyboard search: when the user types characters, the `ListBox` navigates to the first item whose `ToString()` starts with the typed prefix. This only works if items are simple strings or have a meaningful `ToString()`. For complex items, implement `ITextSearchProvider` on the `ItemsSource`.

### Virtualization in ListBox

`ListBox` uses `VirtualizingStackPanel` by default. It creates containers only for visible items plus a "generation" buffer (default: 2 pages). As the user scrolls, containers are recycled:

- An item scrolls out of the viewport → its container enters the recycle queue.
- A new item scrolls into the viewport → a container is dequeued, its `DataContext` is updated to the new data item, and it re-templates.

**Why this matters for performance:** With 100,000 items and no virtualization, the `ListBox` would create 100,000 `ListBoxItem` containers — each with its own visual tree, bindings, and layout pass. Virtualization reduces the container count to ~30-50. The cost of recycling is the `DataContext` reassignment and possible re-templating.

**When virtualization breaks:**

- The `ListBox` is inside a `ScrollViewer`. The `ScrollViewer` gives infinite space to its child, so the `ListBox` thinks all items are visible and creates containers for all of them. Fix: remove the outer `ScrollViewer` (the `ListBox` has its own scrolling) or set `ScrollViewer.HorizontalScrollBarVisibility="Disabled"` and `VirtualizingStackPanel.IsVirtualized="True"`.
- You use `ItemsControl` instead of `ListBox`. `ItemsControl` has no virtualization by default.

---

## 3. ItemsRepeater — flexible layout with virtualization

```xml
<ItemsRepeater ItemsSource="{Binding Items}"
               x:DataType="vm:MainViewModel">
  <ItemsRepeater.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <Border Background="{Binding IsDone, Converter={StaticResource DoneToColor}}"
              Padding="12" Margin="0,2">
        <TextBlock Text="{Binding Title}" />
      </Border>
    </DataTemplate>
  </ItemsRepeater.ItemTemplate>

  <ItemsRepeater.Layout>
    <StackLayout Spacing="4" />
  </ItemsRepeater.Layout>
</ItemsRepeater>
```

### What ItemsRepeater does differently

`ItemsRepeater` is a low-level, high-performance item control. Unlike `ListBox`, it does not provide:

- Selection
- Item containers (no `ListBoxItem` wrapper — the template output is the direct child)
- Scrolling (must be placed inside a `ScrollViewer`)
- Keyboard navigation

**What it provides:**

- Virtualization via `StackLayout`, `UniformGridLayout`, or custom `ILayout` implementations.
- Full control over layout (stack, grid, flow, custom).
- No container overhead — the template result is added directly to the visual tree.

### Layout types

| Layout | Behavior | Virtualized |
|---|---|---|
| `StackLayout` | Stacks items vertically or horizontally | Yes |
| `UniformGridLayout` | Grid with equal-sized cells, auto-wrapping | Yes |
| `FlowLayout` (proposed) | Line-wrapping like a WrapPanel | Planned |
| Custom `ILayout` | Implement `Measure` and `Arrange` for your own layout algorithm | Up to you |

**When to use ItemsRepeater over ListBox:**

- You need a non-stack layout (grid, flow, or custom).
- You need maximum performance and do not need selection.
- You are building a custom list control and want full control over layout and recycling.

**When to use ListBox over ItemsRepeater:**

- You need selection (single or multiple).
- You need keyboard navigation (arrow keys, home, end).
- You need text search.
- You need the item container pattern (`ListBoxItem` with its own pseudo-classes, context menu, etc.).

### Real-time UI updates with converters

```csharp
public class DoneToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Colors.LightGreen)
            : new SolidColorBrush(Colors.LightGray);
    }
}
```

**Why this matters for ItemsRepeater:** Because there is no `ListBoxItem` wrapper, you must put all visual logic — colors, margins, etc. — directly in the `DataTemplate` or use converters. You cannot use `ListBoxItem` styles because there is no `ListBoxItem`.

---

## 4. DataGrid — tabular data

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="False"
          IsReadOnly="False"
          x:DataType="vm:MainViewModel">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Title"
                        Binding="{CompiledBinding Title}" />
    <DataGridCheckBoxColumn Header="Done"
                            Binding="{CompiledBinding IsDone}" />
    <DataGridTemplateColumn Header="Actions">
      <DataGridTemplateColumn.CellTemplate>
        <DataTemplate x:DataType="models:TodoItem">
          <Button Content="Delete"
                  Command="{Binding $parent[DataGrid].DataContext.DeleteItemCommand}"
                  CommandParameter="{Binding}" />
        </DataTemplate>
      </DataGridTemplateColumn.CellTemplate>
    </DataGridTemplateColumn>
  </DataGrid.Columns>
</DataGrid>
```

### Column types

| Column | Binds to | Editable |
|---|---|---|
| `DataGridTextColumn` | Text | Yes (two-way by default) |
| `DataGridCheckBoxColumn` | Bool | Yes |
| `DataGridComboBoxColumn` | Enum/selection | Yes |
| `DataGridTemplateColumn` | Any (custom template) | Yes (if template includes editors) |

### The $parent binding pattern

```xml
Command="{Binding $parent[DataGrid].DataContext.DeleteItemCommand}"
```

**What this does:** The `DataTemplate`'s `x:DataType` is `models:TodoItem`, so `{Binding}` resolves against the item. But `DeleteItemCommand` is on the parent ViewModel (`MainViewModel`), not on `TodoItem`. The `$parent[DataGrid]` compiled binding walks up the logical tree until it finds a `DataGrid`, then accesses its `DataContext` (which is `MainViewModel`), then accesses `DeleteItemCommand`.

**Why not use `RelativeSource`:** Avalonia's `$parent[Type]` syntax is cleaner than the WPF-style `RelativeSource={RelativeSource AncestorType={x:Type DataGrid}}`. It is a compiled binding, so it is type-safe when `x:DataType` is set on the DataGrid.

### Sorting

```xml
<DataGridTextColumn Header="Title"
                    Binding="{CompiledBinding Title}"
                    CanUserSort="True"
                    SortDirection="Ascending" />
```

**How sorting works:** `DataGrid` wraps `ItemsSource` in a `DataGridSortDescriptionCollection`. When the user clicks a column header, the DataGrid creates a `SortDescription` (property name, direction) and re-sorts the view. This works only if `ItemsSource` is a `List<T>` or `ObservableCollection<T>` — the DataGrid reorders the collection in-place. It does **not** sort an `IEnumerable<T>` or a database query.

**Multi-column sorting:** Hold Shift while clicking additional column headers.

---

## 5. ItemsControl — the non-virtualized fallback

```xml
<ItemsControl ItemsSource="{Binding Items}">
  <ItemsControl.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <TextBlock Text="{Binding Title}" />
    </DataTemplate>
  </ItemsControl.ItemTemplate>
</ItemsControl>
```

`ItemsControl` creates a container for every item and does not virtualize. Use it only for small lists (under 100 items) where the total visual tree is small. For everything else, use `ListBox`, `ItemsRepeater`, or `DataGrid`.

**When ItemsControl is useful:** When you need to wrap items in a non-standard panel (e.g., `WrapPanel`) and selection is not needed. You can set `ItemsControl.ItemsPanel` to any `Panel`.

---

## 6. ObservableCollection — how it drives UI updates

```csharp
public ObservableCollection<TodoItem> Items { get; } = new();

Items.Add(new TodoItem { Title = "New item" });
Items.Remove(item);
Items[0] = new TodoItem { Title = "Replaced" };
```

### What happens when you call Add

1. `ObservableCollection` calls `InsertItem`, which adds the item to the internal `List<T>`.
2. It fires `PropertyChanged` for `Count` and `Item[]` (the indexer).
3. It fires `CollectionChanged` with `NotifyCollectionChangedAction.Add`, including the new item and its index.
4. The item control (e.g., `ListBox`) receives `CollectionChanged` and:
   - Creates a new container for the added item.
   - Measures and arranges the new container.
   - Inserts it at the correct position in the panel.

### What happens when you replace by index

```csharp
Items[0] = new TodoItem { ... };
```

1. `ObservableCollection` calls `SetItem`, replacing the item at index 0.
2. It fires `CollectionChanged` with `NotifyCollectionChangedAction.Replace`, including the old and new items.
3. The item control updates the existing container's `DataContext` to the new item. The container is not recreated — only its bindings re-evaluate.

### What does NOT trigger updates

- Modifying a property on an **item** (e.g., `Items[0].Title = "New"`). The collection fires no event. The item must implement `INotifyPropertyChanged` (via `ObservableObject`) for the UI to see the change.
- Reassigning `Items = new ObservableCollection<T>()` to a new instance. The control sees a new collection, but if `ItemsSource` is a binding, the control only re-evaluates if the ViewModel fires `PropertyChanged` for the `Items` property.

### Performance with large collections

- `Add` is O(1). `Insert` at index 0 is O(n). `Remove` is O(n) for non-last items.
- For bulk operations (add 1000 items), suppress collection notifications:

```csharp
Items.Clear();
// AddRange on List<T>, then:
foreach (var item in newItems)
    Items.Add(item);
```

Better: use `Items.AddRange` (available in some ports) or an `ObservableCollection` subclass that suspends notifications:

```csharp
public class BatchObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    public void AddRange(IEnumerable<T> items)
    {
        _suppressNotifications = true;
        foreach (var item in items) Items.Add(item);
        _suppressNotifications = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
```

---

## Key Takeaways

- `ListBox` for selectable, scrollable lists with virtualization. Use `SelectedItem` binding for single selection.
- `ItemsRepeater` for high-performance, custom-layout lists without selection overhead. Requires a `ScrollViewer`.
- `DataGrid` for tabular data with sortable columns. Use `$parent[DataGrid].DataContext` to reach parent VM commands.
- `ItemsControl` only for small non-virtualized lists (<100 items).
- `ObservableCollection<T>` drives UI updates through `CollectionChanged`. Item property changes require `INotifyPropertyChanged` on the item class.
- Virtualization breaks when the list is inside a nested `ScrollViewer`.

---

## See Also

- [015 — Item Lists (original)](015-item-lists.md)
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
- [Avalonia Docs: DataGrid](https://docs.avaloniaui.net/controls/datagrid)
- [015E — Item Lists (examples)](015-item-lists-examples.md)
- [Avalonia Docs: ListBox](https://docs.avaloniaui.net/controls/listbox)
