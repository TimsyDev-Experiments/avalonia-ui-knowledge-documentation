---
tier: intermediate
topic: data display
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 040-datagrid-deep-dive.md
---

# 040V — DataGrid Deep Dive: An In-Depth Companion

**What you'll learn in this companion:** Not just how to write DataGrid markup, but how its column generation pipeline, editing lifecycle, grouping engine, virtualization strategy, and styling system work internally. Covers why certain patterns exist and when to reach for alternatives.

**Prerequisites:** [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md), [009 — Data Templates Basics](../basics/009-data-templates-basics.md)

**You should already have read:** [040 — DataGrid Deep Dive](040-datagrid-deep-dive.md) for the quick-start version. This file goes deeper on every section.

---

## 1. What DataGrid Actually Is

`DataGrid` is a high-level tabular control that manages its own editing lifecycle, selection state, column layout, scrolling, and virtualization. It is not a thin wrapper over a native list control — it is a full-featured grid with its own internal `DataGridCollectionView` for sorting, filtering, and grouping, and its own column types (`DataGridTextColumn`, `DataGridCheckBoxColumn`, `DataGridComboBoxColumn`, `DataGridTemplateColumn`) that each know how to generate editing and non-editing element trees.

The control is designed for spreadsheet-like interaction: click to select, double-click or F2 to edit, Tab to navigate cells, and built-in support for clipboard copy. This makes it fundamentally different from `ListBox` (selection-only) or `ItemsRepeater` (no selection, no built-in editing).

**When to use DataGrid vs alternatives:**

| Control | Best for |
|---------|----------|
| `DataGrid` | Tabular data with editing, sorting, grouping, frozen columns, clipboard copy |
| `ListBox` | Single-column selection lists with optional multi-select |
| `ItemsRepeater` | Custom layouts (Wrap, Stack, UniformGrid) with no built-in selection or editing |
| `TreeView` | Hierarchical data that needs expand/collapse |

---

## 2. Auto-Generated Columns: The Pipeline

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="True"
          x:DataType="vm:MainViewModel" />
```

When `AutoGenerateColumns` is `True`, the `DataGrid` inspects the **public properties** of the item type at runtime using reflection. For each property, it chooses a column type based on the property's type:

| Property type | Column type |
|---|---|
| `bool` / `bool?` | `DataGridCheckBoxColumn` |
| `enum` | `DataGridComboBoxColumn` (Avalonia 12) |
| `string`, `int`, `DateTime`, etc. | `DataGridTextColumn` |
| Everything else | `DataGridTextColumn` (Text = `.ToString()`) |

The `AutoGeneratingColumn` event fires once per column, before the column is added to the grid. This is your chance to:

- **Cancel** the column (hide a primary key or internal field)
- **Replace** the column with a different type (e.g., swap a `DataGridTextColumn` for a `DataGridTemplateColumn`)
- **Modify** the header, width, or display order
- **Change** the binding or formatting

```csharp
private void OnAutoGeneratingColumn(object? sender,
    DataGridAutoGeneratingColumnEventArgs e)
{
    if (e.PropertyName == "Id")
        e.Cancel = true;

    if (e.PropertyType == typeof(DateTime))
        e.Column.Header = $"{e.PropertyName} (Date)";
}
```

The event args expose `e.PropertyName`, `e.PropertyType`, and `e.Column` (the already-created column object). Changes to `e.Column.Header` are reflected in the UI automatically because the `DataGrid` adds the column after the event handler returns.

**Common mistake:** Trying to set `e.Column.Binding` from this event. The binding is already set based on the property name. If you need a custom binding path, cancel the auto-generated column and manually insert a replacement using `e.Column = new DataGridTextColumn { ... }` (not all properties on the column can be changed after creation).

---

## 3. Inline Cell Editing: The Lifecycle

```xml
<DataGrid ItemsSource="{Binding Items}"
          IsReadOnly="False"
          x:DataType="vm:MainViewModel">
  <DataGrid.Columns>
    <DataGridTextColumn Header="Title"
                        Binding="{CompiledBinding Title, Mode=TwoWay}"
                        IsReadOnly="False" />
    <DataGridCheckBoxColumn Header="Active"
                            Binding="{CompiledBinding IsActive, Mode=TwoWay}" />
    <DataGridComboBoxColumn Header="Status"
                            ItemsSource="{Binding $parent[DataGrid].DataContext.StatusOptions}"
                            SelectedItemBinding="{CompiledBinding Status, Mode=TwoWay}" />
  </DataGrid.Columns>
</DataGrid>
```

### The editing lifecycle, step by step

When a user triggers edit mode (F2, double-click, or typing):

1. **`BeginningEdit`** event fires. If `e.Cancel = true`, the cell stays in display mode.
2. The `DataGrid` column creates an **editing element** — a `TextBox` for text columns, a `CheckBox` for boolean columns, a `ComboBox` for combo columns. This replaces the display element in the visual tree.
3. The editing element gets focus. Its initial value comes from the binding.
4. User modifies the value.
5. User commits (Enter, Tab, click another cell) or cancels (Escape).
6. **`CellEditEnding`** event fires with `e.EditAction = Commit` or `Cancel`.
7. If committed, the binding writes the value to the source.
8. The display element replaces the editing element.

### Why TwoWay binding is required

`DataGridTextColumn.Binding` must be `TwoWay` for editing to work. If you use `OneWay` (the default), the edit commits UI-side but never propagates to the viewmodel. The column's default binding mode is `OneWay` — you must explicitly set `Mode=TwoWay`.

### ComboBoxColumn binding explained

```xml
<DataGridComboBoxColumn Header="Status"
                        ItemsSource="{Binding $parent[DataGrid].DataContext.StatusOptions}"
                        SelectedItemBinding="{CompiledBinding Status, Mode=TwoWay}" />
```

The `ItemsSource` uses a `$parent[DataGrid]` relative binding to reach the grid's `DataContext` (the `MainViewModel`), because the column itself is not part of the row's data context. The column's items are the available options from the parent viewmodel. The `SelectedItemBinding` points to the row item's `Status` property.

Note the property name: `SelectedItemBinding` (not `SelectedItem`). This is a property on `DataGridComboBoxColumn` that accepts a binding object, not a direct value.

### The role of IsReadOnly

Both the `DataGrid` and individual columns have an `IsReadOnly` property. The grid-level setting is the master switch. Column-level overrides apply per column. This lets you make most columns editable except a few:

```xml
<DataGrid IsReadOnly="False">
  <DataGrid.Columns>
    <DataGridTextColumn Binding="{CompiledBinding Id, Mode=TwoWay}"
                        IsReadOnly="True" />  <!-- ID is never editable -->
    <DataGridTextColumn Binding="{CompiledBinding Name, Mode=TwoWay}"
                        IsReadOnly="False" />
  </DataGrid.Columns>
</DataGrid>
```

### Edit events flow

```
User action (F2 / double-click / typing)
  → BeginningEdit (can cancel)
    → Editing element created and focused
      → User edits
        → CellEditEnding (check EditAction)
          → If Commit: binding writes, row validation runs
            → RowEditEnding (if row-level validation configured)
              → If Commit: row accepts changes
```

The `RowEditEnding` event is broader — it fires when an entire row's edits are being committed (e.g., moving to a different row). `CellEditEnding` fires per cell. Handle `CellEditEnding` for per-cell validation, and `RowEditEnding` for cross-field validation (e.g., "StartDate must be before EndDate").

---

## 4. Row Details Template: Expandable Content

```xml
<DataGrid ItemsSource="{Binding Orders}"
          x:DataType="vm:OrdersViewModel">
  <DataGrid.RowDetailsTemplate>
    <DataTemplate x:DataType="models:Order">
      <Border BorderBrush="LightGray" BorderThickness="1"
              CornerRadius="4" Padding="12" Margin="4">
        <StackPanel Spacing="4">
          <TextBlock Text="{Binding Customer, StringFormat='Customer: {0}'}"
                     FontWeight="Bold" />
          <ItemsControl ItemsSource="{Binding LineItems}">
            <ItemsControl.ItemTemplate>
              <DataTemplate x:DataType="models:LineItem">
                <TextBlock Text="{Binding Product, StringFormat='  - {0}'}" />
              </DataTemplate>
            </ItemsControl.ItemTemplate>
          </ItemsControl>
        </StackPanel>
      </Border>
    </DataTemplate>
  </DataGrid.RowDetailsTemplate>
</DataGrid>
```

Row details is an expandable section between the row content and the next row. It is perfect for master-detail patterns where each row has supplementary data that should not clutter the main grid cells.

### Visibility modes

| Mode | Behavior |
|------|----------|
| `Visible` | All detail sections are always visible |
| `VisibleWhenSelected` | Only the selected row(s) show details |
| `Collapsed` | No details visible (template still exists but not rendered) |

`VisibleWhenSelected` is the most common choice because it keeps the grid compact while still providing access to details.

### How it works internally

The `RowDetailsTemplate` is a `DataTemplate` applied to the same data item as the row. It has its own visual tree that sits in a `ContentPresenter` named `RowDetailsPresenter` inside the `DataGridRow` template. The presenter's visibility is toggled based on `RowDetailsVisibilityMode` and the row's selection state.

**Performance note:** Each visible detail section adds to the visual tree. For grids with hundreds of rows and `Visible` mode, this can significantly increase element count. Use `VisibleWhenSelected` or keep the template lightweight (avoid deeply nested panels) when dealing with large datasets.

---

## 5. Grouping with DataGridCollectionView

```csharp
public partial class GroupedViewModel : ObservableObject
{
    [ObservableProperty]
    private DataGridCollectionView? _groupedView;

    public GroupedViewModel()
    {
        var source = new List<Employee>
        {
            new() { Department = "Engineering", Name = "Alice" },
            new() { Department = "Engineering", Name = "Bob" },
            new() { Department = "Marketing", Name = "Carol" },
        };

        GroupedView = new DataGridCollectionView(source);
        GroupedView.GroupDescriptions.Add(
            new DataGridPathGroupDescription("Department"));
    }
}
```

### What is DataGridCollectionView?

`DataGridCollectionView` is Avalonia's equivalent of WPF's `CollectionView` or `ListCollectionView`. It wraps an `IEnumerable` source and provides:

- **Grouping** — Partition items by property values
- **Sorting** — Sort by one or more properties (via `SortDescriptions`)
- **Filtering** — Show a subset via a predicate (via `Filter` property)
- **Currency management** — Track current item and current position

The view sits between your data source and the grid. The grid binds to the view, not the raw collection. When you add group descriptions, the view restructures the data into `CollectionViewGroup` objects that the DataGrid renders as expandable group rows.

### Why not just LINQ GroupBy?

`DataGridCollectionView` groups without creating a new collection. LINQ `GroupBy` produces an `IGrouping<K, V>` which is a different type than your original items. With `DataGridCollectionView`, the grid still sees the original item types for columns, but renders group headers between groups. The grouping is a **view transformation**, not a data transformation.

### Group styling

```xml
<DataGrid.GroupStyle>
  <ControlTheme x:Key="DataGridGroup">
    <Setter Property="Padding" Value="4" />
  </ControlTheme>
</DataGrid.GroupStyle>
```

`GroupStyle` accepts a `ControlTheme` that is applied to the group row container (a `DataGridGroupRow`). By default, group rows show the group name and item count. You can customize the entire group row template with a full `ControlTheme`.

### Limitation: DataGridCollectionView is read-only

`DataGridCollectionView` does not support adding or removing items through the view. If your source collection changes, you must reset the view or call `Refresh()`. For dynamic collections that grow or shrink, consider replacing the entire `DataGridCollectionView` instance when the source changes.

```csharp
// When source changes:
GroupedView = new DataGridCollectionView(newSource);
// Or refresh:
GroupedView.Refresh();
```

---

## 6. Frozen Columns: How They Work

```xml
<DataGrid FrozenColumnCount="2">
  <DataGrid.Columns>
    <DataGridTextColumn Header="ID" Binding="{CompiledBinding Id}"
                        IsReadOnly="True" Width="50" />
    <DataGridTextColumn Header="Name" Binding="{CompiledBinding Name}"
                        Width="150" />
    <!-- Remaining columns scroll -->
    <DataGridTextColumn Header="Description"
                        Binding="{CompiledBinding Description}" />
    <DataGridTextColumn Header="Notes"
                        Binding="{CompiledBinding Notes}" />
  </DataGrid.Columns>
</DataGrid>
```

`FrozenColumnCount="N"` pins the first N columns so they remain visible during horizontal scroll. The frozen area is rendered in a separate `ScrollContentPresenter` layered on top of the scrollable area. The DataGrid synchronizes row heights between frozen and scrollable sections automatically.

**Constraints:**
- Only leftmost columns can be frozen (columns 0 to N-1)
- Reordering columns does not change which columns are frozen by index — `FrozenColumnCount` is an index count, not a named-column set
- Setting `FrozenColumnCount` too high (more than half the columns) can cause visual artifacts because the frozen area overlaps with the scrollable area
- Frozen columns should typically be narrow (ID, Name, checkbox) so the frozen area doesn't consume too much horizontal space

**Why frozen columns exist:** In wide tables, users lose context when scrolling horizontally. Keeping key identifier columns (ID, Name, row number) frozen maintains orientation.

---

## 7. Column Reordering: The User Experience

```xml
<DataGrid CanUserReorderColumns="True"
          CanUserResizeColumns="True" />
```

Column reordering is enabled by default. Users drag column headers left or right to reorder. The DataGrid fires `ColumnReordering` (before the move), `ColumnReordered` (after the move).

### Preventing reorder of specific columns

```csharp
private void OnColumnReordering(object? sender,
    DataGridColumnReorderingEventArgs e)
{
    if (e.Column.Header.ToString() == "ID")
        e.Cancel = true;
}
```

### Column display index vs collection index

Each column has a `DisplayIndex` (position in the UI) and a position in the `Columns` collection (logical index). Reordering changes `DisplayIndex` but not the collection index. When you iterate `Columns`, you get them in declaration order, not display order. Use `Columns.OrderBy(c => c.DisplayIndex)` for display order.

### What happens to frozen columns during reorder

If `FrozenColumnCount` is 2, columns at display indices 0 and 1 are frozen. If a user tries to drag a frozen column into the scrollable area (or vice versa), the DataGrid prevents it. Frozen columns can only be reordered among themselves; scrollable columns among themselves.

---

## 8. Cell and Header Styling: Selector Targeting

```xml
<DataGrid>
  <DataGrid.CellStyle>
    <Style Selector="DataGridCell">
      <Setter Property="Padding" Value="6,2" />
      <Setter Property="FontSize" Value="13" />
    </Style>
  </DataGrid.CellStyle>

  <DataGrid.ColumnHeaderStyle>
    <Style Selector="DataGridColumnHeader">
      <Setter Property="Background" Value="{StaticResource SystemAccentColor}" />
      <Setter Property="Foreground" Value="White" />
      <Setter Property="FontWeight" Value="SemiBold" />
      <Setter Property="Padding" Value="8,4" />
    </Style>
  </DataGrid.ColumnHeaderStyle>

  <DataGrid.RowStyle>
    <Style Selector="DataGridRow">
      <Setter Property="Background" Value="Transparent" />
    </Style>
  </DataGrid.RowStyle>
</DataGrid>
```

### Why three separate style properties?

`CellStyle`, `ColumnHeaderStyle`, and `RowStyle` are convenience properties that each accept a `Style` targeting a specific element type. They exist because the alternative — writing global styles and relying on selector specificity — is error-prone and harder to read.

Each property internally creates a style for its target element and adds it to the DataGrid's styles collection:

- `CellStyle` targets `DataGridCell` — the individual cell container
- `ColumnHeaderStyle` targets `DataGridColumnHeader` — the header cell for each column
- `RowStyle` targets `DataGridRow` — the entire row container

### Alternating rows

```xml
<DataGrid AlternatingRowBackground="#F5F5F5"
          RowBackground="White" />
```

`AlternatingRowBackground` applies to every other row (even-indexed rows get the alternating background; odd-indexed get `RowBackground`). This is implemented at the `DataGridRow` level: the DataGrid sets the `Background` property on each row during virtualization based on its index.

### Cell-level conditional styling via selectors

For conditional styling (e.g., highlight negative values), use standard style selectors with a class:

```xml
<DataGrid.CellStyle>
  <Style Selector="DataGridCell.negative">
    <Setter Property="Foreground" Value="Red" />
    <Setter Property="FontWeight" Value="Bold" />
  </Style>
</DataGrid.CellStyle>
```

Then apply the class from a converter or a behavior. However, a cleaner approach for cell-level conditional styling is to use a `DataGridTemplateColumn` with a control that has a value converter:

```xml
<DataGridTemplateColumn Header="Balance">
  <DataTemplate x:DataType="models:Account">
    <TextBlock Text="{Binding Balance, StringFormat='{0:C}'}"
               Foreground="{Binding Balance, Converter={StaticResource NegativeToRedConverter}}" />
  </DataTemplate>
</DataGridTemplateColumn>
```

---

## 9. Clipboard Copy: What Gets Copied

```xml
<DataGrid ClipboardCopyMode="ExcludeHeader" />
```

`ClipboardCopyMode` controls what goes to the clipboard when the user presses `Ctrl+C` (or `Cmd+C` on macOS):

| Mode | Columns | Rows | Content |
|------|---------|------|---------|
| `ExcludeHeader` | No headers | Selected rows only | Tab-separated cell values |
| `IncludeHeader` | Column headers as first row | Selected rows only | Header row + tab-separated values |
| `None` | — | — | Copy disabled |

The copy format is tab-separated text (TSV), which pastes correctly into Excel, Google Sheets, and text editors.

**What does NOT get copied:**
- Row details content
- CheckBox column values as booleans (copied as "True"/"False")
- ComboBox column selection text (copied as the selected item's `.ToString()`)
- Group headers
- Frozen column indicator

---

## 10. Virtualization: How DataGrid Manages Memory

```xml
<DataGrid ItemsSource="{Binding LargeSet}"
          EnableRowVirtualization="True"
          EnableColumnVirtualization="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          ScrollViewer.IsDeferredScrollingEnabled="True"
          MaxHeight="600" />
```

### Row virtualization

`EnableRowVirtualization="True"` (default: `True`) tells the DataGrid to create containers (`DataGridRow`) only for items that are currently visible or near-visible (within a scroll window). When the user scrolls, containers for scrolled-away items are released or recycled.

Without virtualization, a DataGrid with 100,000 items creates 100,000 `DataGridRow` instances, each containing its cell tree. Even if the rows are empty, the overhead of 100,000 `Control` instances in the visual tree causes severe performance degradation.

### Column virtualization

`EnableColumnVirtualization="True"` (default: `False`) applies the same concept to columns. In wide tables with 100+ columns, only visible columns and a small buffer on each side have their cell containers realized. This matters less for typical tables (5-20 columns) but is critical for tables with 50+ columns.

### Recycling mode

`VirtualizationMode="Recycling"` reuses row containers instead of destroying and recreating them. When a row scrolls out of view, its container is placed in a recycle queue. When a new row scrolls in, the recycled container is repurposed with the new data context.

The alternative (default) is `Standard` mode, which destroys containers that scroll out of view and creates new ones when needed. Recycling reduces GC pressure and improves scroll smoothness.

### Deferred scrolling

`IsDeferredScrollingEnabled="True"` skips rendering during fast scroll operations. While the user is actively dragging the scrollbar, only the scrollbar position updates — the content area shows a blank or frozen preview. When the user releases the scrollbar, the grid jumps to the final position and renders.

This is essential for smooth scrolling over large datasets because rendering every intermediate scroll position is wasteful when the user just wants to jump to a far section.

### MaxHeight requirement

DataGrid's virtualization only activates when the grid has a fixed height (or is constrained by a parent panel like `Grid` rows with `*` or `Auto`). If the grid is in an auto-sizing container without `MaxHeight`, it grows to fit all items, and virtualization is never triggered because every item is visible. Always set `MaxHeight` or place the grid in a constrained container.

---

## 11. DataGrid.Columns vs AutoGenerateColumns: Interaction

The two are not mutually exclusive. When `AutoGenerateColumns="True"` and you also define explicit columns in `<DataGrid.Columns>`, the explicit columns appear first (in declaration order), followed by the auto-generated columns. The `AutoGeneratingColumn` event fires for auto-generated columns only, not for explicit ones.

This pattern is useful when you want some columns fully controlled and others auto-generated:

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="True">
  <DataGrid.Columns>
    <!-- Explicit: full control -->
    <DataGridTemplateColumn Header="Actions">
      <DataTemplate x:DataType="models:Item">
        <Button Content="Edit" Command="{Binding $parent[DataGrid].DataContext.EditCommand}"
                CommandParameter="{Binding}" />
      </DataTemplate>
    </DataGridTemplateColumn>
  </DataGrid.Columns>
  <!-- Remaining columns auto-generated: Id, Name, CreatedDate, etc. -->
</DataGrid>
```

---

## 12. DataGridTemplateColumn: Custom Cell Content

When the built-in column types don't fit, use `DataGridTemplateColumn` with explicit display and editing templates:

```xml
<DataGridTemplateColumn Header="Rating">
  <DataTemplate x:DataType="models:Item">
    <!-- Display mode: read-only star representation -->
    <TextBlock Text="{Binding Rating, StringFormat='{0}/5'}" />
  </DataTemplate>
  <DataTemplate x:DataType="models:Item">
    <!-- Edit mode: slider -->
    <Slider Minimum="0" Maximum="5" Value="{Binding Rating, Mode=TwoWay}"
            TickFrequency="1" IsSnapToTickEnabled="True" />
  </DataTemplate>
</DataGridTemplateColumn>
```

Note: Two child `DataTemplate` elements — the first is display mode, the second is edit mode (Avalonia 12). If you provide only one, it is used for both modes.

---

## 13. Sorting

Sorting is built-in. Users click column headers to toggle ascending/descending/no sort. The sort arrow appears in the header. To disable per-column:

```xml
<DataGridTextColumn Header="Name" Binding="{CompiledBinding Name}"
                    CanUserSort="False" />
```

For programmatic sorting, use `DataGridCollectionView.SortDescriptions`:

```csharp
GroupedView.SortDescriptions.Add(
    new DataGridSortDescription("Name", ListSortDirection.Ascending));
```

Sorting works by property name (string), not lambda. Multiple `SortDescriptions` are applied in order (primary sort first, secondary next).

---

## Key Takeaways

- DataGrid is a self-managing tabular control with built-in editing lifecycle, clipboard, sorting, grouping, and virtualization — it is **not** just a styled ListBox
- Auto-generate for prototyping; explicit columns for production; the `AutoGeneratingColumn` event is your hook for column-level customization
- Editing requires `TwoWay` binding on columns and `IsReadOnly="False"` — the editing lifecycle fires events at the cell and row level for validation hooks
- `DataGridCollectionView` wraps your source data to add grouping, sorting, and filtering without modifying the original collection — it's a view transformation, not a data transformation
- Frozen columns pin leftmost columns by index count, not by name — the frozen area is a separate rendering layer
- Styling is split across `CellStyle`, `RowStyle`, and `ColumnHeaderStyle` for clarity, but standard theme-aware style selectors also work
- Virtualization requires a constrained height — without `MaxHeight` or a parent that limits size, virtualization never activates
- `VirtualizationMode="Recycling"` reduces GC churn for smooth scrolling over large datasets
- `DataGridTemplateColumn` gives you full control over cell appearance and editing UX

---

## See Also

- [040 — DataGrid Deep Dive (original quick-start)](040-datagrid-deep-dive.md)
- [040E — DataGrid Deep Dive (examples)](040-datagrid-deep-dive-examples.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)
- [036 — Virtualization and Large List Performance](../advanced/036-virtualization-large-lists.md)
- [043 — TreeView with Hierarchical Data](043-treeview-hierarchical-data.md)
- [Avalonia Docs: DataGrid](https://docs.avaloniaui.net/controls/datagrid)
- [Avalonia Docs: DataGridCollectionView](https://docs.avaloniaui.net/controls/datagrid/data-grid-collection-view)
