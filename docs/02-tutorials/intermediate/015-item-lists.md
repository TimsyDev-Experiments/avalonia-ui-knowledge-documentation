---
tier: intermediate
topic: data display
estimated: 10 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 015 — Item Lists: ListBox, ItemsRepeater, DataGrid

**What you'll learn:** Choose the right item control for your data, configure selection, enable sorting, and use virtualization.

**Prerequisites:** [009 — Data Templates Basics](../basics/009-data-templates-basics.md)

---

## 1. ListBox — selection, multi-select, search

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

`SelectionMode` options: `Single` (default), `Multiple`, `Toggle`.

Enable text search on a `ListBox`:

```xml
<ListBox TextSearchEnabled="True">
```

---

## 2. ItemsRepeater — virtualized, custom layout

```xml
<ItemsRepeater ItemsSource="{Binding Items}"
               x:DataType="vm:MainViewModel">
  <ItemsRepeater.ItemTemplate>
    <DataTemplate x:DataType="models:TodoItem">
      <Border Background="{Binding IsDone, Converter={StaticResource DoneToColor}}"
              Padding="12"
              Margin="0,2">
        <TextBlock Text="{Binding Title}" />
      </Border>
    </DataTemplate>
  </ItemsRepeater.ItemTemplate>

  <ItemsRepeater.Layout>
    <StackLayout Spacing="4" />
  </ItemsRepeater.Layout>
</ItemsRepeater>
```

`ItemsRepeater` supports different layouts:
- `StackLayout` — vertical/horizontal stacking (virtualized)
- `UniformGridLayout` — grid with equal-sized cells
- Custom `ILayout` implementations

> ItemsRepeater requires `Avalonia.Controls.ItemsRepeater` namespace and is available in Avalonia 12.

---

## 3. DataGrid — tabular data with columns

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

The `$parent[DataGrid]` binding walks up to the `DataGrid` to access its `DataContext` (the parent ViewModel).

---

## 4. DataGrid sorting

```xml
<DataGridTextColumn Header="Title"
                    Binding="{CompiledBinding Title}"
                    CanUserSort="True"
                    SortDirection="Ascending" />
```

Sorting is built into `DataGrid`. Enable per-column with `CanUserSort`.

---

## 5. Virtualization comparison

| Control | Virtualization | Layout | Use case |
|---|---|---|---|
| `ItemsControl` | No | Stack | Small lists (<100 items) |
| `ListBox` | Yes (default) | Stack | Selectable lists |
| `ItemsRepeater` | Yes (with virtual layouts) | Flexible | High-performance, custom layout |
| `DataGrid` | Yes (rows) | Grid | Tabular data with columns |

---

## 6. Working with ObservableCollection

```csharp
public ObservableCollection<TodoItem> Items { get; } = new();

// Add
Items.Add(new TodoItem { Title = "New item" });

// Remove
Items.Remove(item);

// Clear
Items.Clear();

// Replace (raises CollectionChanged)
Items[0] = new TodoItem { Title = "Replaced" };
```

`ObservableCollection<T>` notifies the UI of adds, removes, moves, and replaces automatically.

---

## Key Takeaways

- `ListBox` for selectable lists, `DataGrid` for tabular data
- `ItemsRepeater` for virtualized custom-layout lists
- `$parent[Type]` binding to access the parent control's DataContext
- `ObservableCollection<T>` drives automatic UI updates

---

## See Also

- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
- [015V — Item Lists (verbose companion)](015-item-lists-verbose.md)
- [015E — Item Lists (examples)](015-item-lists-examples.md)
- [Avalonia Docs: DataGrid](https://docs.avaloniaui.net/controls/datagrid)
