---
tier: intermediate
topic: data display
estimated: 15 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 040 -- DataGrid Deep Dive: Editing, Grouping, Row Details, and Styling

**What you'll learn:** Advanced DataGrid patterns — inline editing, grouping, row details templates, column reordering, frozen columns, cell styling, and virtualization tuning.

**Prerequisites:** [015 -- Item Lists (ListBox, ItemsRepeater, DataGrid)](../intermediate/015-item-lists.md)

---

## 1. Auto-generated columns with compiled bindings

```xml
<DataGrid ItemsSource="{Binding Items}"
          AutoGenerateColumns="True"
          x:DataType="vm:MainViewModel" />
```

Override auto-generation in code-behind:

```csharp
private void OnAutoGeneratingColumn(object? sender,
    DataGridAutoGeneratingColumnEventArgs e)
{
    if (e.PropertyName == "Id")
        e.Cancel = true; // Hide primary key

    if (e.PropertyType == typeof(DateTime))
        e.Column.Header = $"{e.PropertyName} (Date)";
}
```

## 2. Inline cell editing workflow

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

### Keyboard editing flow

| Key | Action |
|-----|--------|
| `F2` | Enter edit mode on current cell |
| `Enter` | Commit edit and move down |
| `Escape` | Cancel edit |
| `Tab` | Commit and move right |
| `Shift+Tab` | Commit and move left |

### Handling edit events

```csharp
private void OnCellEditEnding(object? sender,
    DataGridCellEditEndingEventArgs e)
{
    if (e.EditAction == DataGridEditAction.Commit)
    {
        var cellValue = (e.EditingElement as TextBox)?.Text;
        // Custom validation or transformation
    }
}

private void OnBeginningEdit(object? sender,
    DataGridBeginningEditEventArgs e)
{
    // Prevent editing certain rows
    if (e.Row.DataContext is SomeItem item && item.IsLocked)
        e.Cancel = true;
}
```

## 3. Row details template

Show expandable detail sections below each row:

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

Row details visibility modes:

```xml
<DataGrid RowDetailsVisibilityMode="VisibleWhenSelected" />
<!-- Other options: Visible, Collapsed -->
```

## 4. Grouping with CollectionView

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

```xml
<DataGrid ItemsSource="{Binding GroupedView}"
          x:DataType="vm:GroupedViewModel">
  <DataGrid.GroupStyle>
    <ControlTheme x:Key="DataGridGroup">
      <Setter Property="Padding" Value="4" />
    </ControlTheme>
  </DataGrid.GroupStyle>
  <DataGrid.Columns>
    <DataGridTextColumn Header="Name"
                        Binding="{CompiledBinding Name}" />
    <DataGridTextColumn Header="Department"
                        Binding="{CompiledBinding Department}" />
  </DataGrid.Columns>
</DataGrid>
```

## 5. Frozen columns

Keep the first N columns visible during horizontal scroll:

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

## 6. Column reordering

```xml
<DataGrid CanUserReorderColumns="True"
          CanUserResizeColumns="True" />
```

Handle reorder events:

```csharp
private void OnColumnReordering(object? sender,
    DataGridColumnReorderingEventArgs e)
{
    // Prevent moving certain columns
    if (e.Column.Header.ToString() == "ID")
        e.Cancel = true;
}
```

## 7. Cell and header styling

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

Alternating rows:

```xml
<DataGrid AlternatingRowBackground="#F5F5F5"
          RowBackground="White" />
```

## 8. Clipboard and copy support

```xml
<DataGrid ClipboardCopyMode="ExcludeHeader" />
```

| Mode | What gets copied |
|------|-----------------|
| `ExcludeHeader` | Cell data only |
| `IncludeHeader` | Column headers + cell data |
| `None` | Copy disabled |

## 9. Virtualization tuning for large data sets

```xml
<DataGrid ItemsSource="{Binding LargeSet}"
          EnableRowVirtualization="True"
          EnableColumnVirtualization="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          ScrollViewer.IsDeferredScrollingEnabled="True"
          MaxHeight="600" />
```

| Setting | Effect |
|---------|--------|
| `EnableRowVirtualization` | Only realize visible and nearby rows |
| `EnableColumnVirtualization` | Only realize visible columns (many-column tables) |
| `VirtualizationMode="Recycling"` | Reuse container elements (reduces GC) |
| `IsDeferredScrollingEnabled` | Skip rendering during fast scroll |
| `MaxHeight` | Ensure virtualization activates (no auto-growing) |

## Key takeaways

- Use `AutoGenerateColumns="True"` for rapid prototyping, override with `AutoGeneratingColumn` event
- Inline editing is built-in with `TwoWay` bindings; handle `CellEditEnding` for validation
- `RowDetailsTemplate` shows expandable detail panes below each row
- `DataGridCollectionView` enables grouping with `GroupDescriptions`
- `FrozenColumnCount` pins N columns during horizontal scroll
- Styling is per-segment: `CellStyle`, `ColumnHeaderStyle`, `RowStyle`
- Enable both row and column virtualization for large data sets

---

## See Also

- [015 -- Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)
- [036 -- Virtualization and Large List Performance](../advanced/036-virtualization-large-lists.md)
- [Avalonia Docs: DataGrid](https://docs.avaloniaui.net/controls/datagrid)
