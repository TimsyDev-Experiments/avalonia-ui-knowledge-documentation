---
tier: intermediate
topic: data display
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 040-datagrid-deep-dive.md
---

# 040E — DataGrid Deep Dive: Real-World Examples

**What you'll learn:** Two complete, production-inspired DataGrid scenarios covering editing with validation, row details, grouping, frozen columns, and column reordering.

**Prerequisites:** [040 — DataGrid Deep Dive](040-datagrid-deep-dive.md), [040V — DataGrid Deep Dive (verbose companion)](040-datagrid-deep-dive-verbose.md)

---

## Example 1: Order Management Dashboard

### Goal

Build an order-tracking grid where users can edit line items inline, see expandable order summaries, and receive per-cell validation feedback. The grid must handle real-time total recalculation and prevent editing of shipped orders.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class OrderLineItem : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _product = "";

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _unitPrice;

    public decimal Total => Quantity * UnitPrice;

    partial void OnQuantityChanged(int value)
    {
        if (value < 0) Quantity = 0;
        OnPropertyChanged(nameof(Total));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        if (value < 0) UnitPrice = 0;
        OnPropertyChanged(nameof(Total));
    }
}

public partial class SalesOrder : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _customer = "";

    [ObservableProperty]
    private string _status = "Pending"; // Pending, Shipped, Cancelled

    [ObservableProperty]
    private ObservableCollection<OrderLineItem> _lineItems = new();

    public decimal GrandTotal
    {
        get
        {
            decimal total = 0;
            foreach (var item in LineItems)
                total += item.Total;
            return total;
        }
    }

    public bool IsShipped => Status == "Shipped";

    public SalesOrder()
    {
        // Recalculate grand total whenever any line item changes
        LineItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(GrandTotal));
    }
}

public partial class OrderManagementViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SalesOrder> _orders = new();

    [ObservableProperty]
    private SalesOrder? _selectedOrder;

    [ObservableProperty]
    private string _validationMessage = "";

    private int _nextOrderId = 1;

    public OrderManagementViewModel()
    {
        var order = new SalesOrder
        {
            Id = _nextOrderId++,
            Customer = "Acme Corp",
            Status = "Pending",
            LineItems =
            {
                new OrderLineItem { Id = 1, Product = "Widget A", Quantity = 10, UnitPrice = 5.99m },
                new OrderLineItem { Id = 2, Product = "Widget B", Quantity = 3, UnitPrice = 12.50m },
            }
        };
        Orders.Add(order);
    }

    [RelayCommand]
    private void AddOrder()
    {
        Orders.Add(new SalesOrder
        {
            Id = _nextOrderId++,
            Customer = "New Customer",
            Status = "Pending"
        });
    }

    [RelayCommand]
    private void ShipOrder(SalesOrder? order)
    {
        if (order is null) return;
        order.Status = "Shipped";
    }

    public bool CanEditCell(SalesOrder order, string propertyName)
    {
        if (order.IsShipped)
        {
            ValidationMessage = "Cannot edit shipped orders.";
            return false;
        }
        return true;
    }

    public bool ValidateLineItem(OrderLineItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Product))
        {
            ValidationMessage = "Product name is required.";
            return false;
        }
        if (item.Quantity <= 0)
        {
            ValidationMessage = "Quantity must be greater than zero.";
            return false;
        }
        if (item.UnitPrice < 0)
        {
            ValidationMessage = "Unit price cannot be negative.";
            return false;
        }
        ValidationMessage = "";
        return true;
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        xmlns:models="clr-namespace:MyApp.Models"
        x:DataType="vm:OrderManagementViewModel"
        Title="Order Management" Width="900" Height="600">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <Button Content="Add Order" Command="{Binding AddOrderCommand}" />
      <Button Content="Ship Selected"
              Command="{Binding ShipOrderCommand}"
              CommandParameter="{Binding SelectedOrder}" />
      <TextBlock Text="{Binding ValidationMessage}"
                 Foreground="Red" VerticalAlignment="Center" />
    </StackPanel>

    <DataGrid ItemsSource="{Binding Orders}"
              SelectedItem="{Binding SelectedOrder}"
              AutoGenerateColumns="False"
              CanUserReorderColumns="True"
              CanUserResizeColumns="True"
              FrozenColumnCount="2"
              RowDetailsVisibilityMode="VisibleWhenSelected"
              x:DataType="vm:OrderManagementViewModel">
      <DataGrid.Columns>
        <DataGridTextColumn Header="ID"
                            Binding="{CompiledBinding Id}"
                            IsReadOnly="True" Width="50" />
        <DataGridTextColumn Header="Customer"
                            Binding="{CompiledBinding Customer, Mode=TwoWay}"
                            IsReadOnly="False" Width="150" />
        <DataGridTextColumn Header="Status"
                            Binding="{CompiledBinding Status}"
                            IsReadOnly="True" Width="100" />
        <DataGridTextColumn Header="Grand Total"
                            Binding="{CompiledBinding GrandTotal, StringFormat='{0:C}'}"
                            IsReadOnly="True" Width="100" />
      </DataGrid.Columns>

      <DataGrid.RowDetailsTemplate>
        <DataTemplate x:DataType="models:SalesOrder">
          <Border BorderBrush="LightGray" BorderThickness="1"
                  CornerRadius="4" Padding="8" Margin="4">
            <StackPanel Spacing="4">
              <TextBlock Text="Line Items" FontWeight="Bold" />
              <DataGrid ItemsSource="{Binding LineItems}"
                        AutoGenerateColumns="False"
                        IsReadOnly="{Binding IsShipped}"
                        x:DataType="models:SalesOrder">
                <DataGrid.Columns>
                  <DataGridTextColumn Header="Product"
                                      Binding="{CompiledBinding Product, Mode=TwoWay}"
                                      Width="*" />
                  <DataGridTextColumn Header="Qty"
                                      Binding="{CompiledBinding Quantity, Mode=TwoWay}"
                                      Width="60" />
                  <DataGridTextColumn Header="Unit Price"
                                      Binding="{CompiledBinding UnitPrice, Mode=TwoWay, StringFormat='{0:C}'}"
                                      Width="80" />
                  <DataGridTextColumn Header="Total"
                                      Binding="{CompiledBinding Total, StringFormat='{0:C}'}"
                                      IsReadOnly="True" Width="80" />
                </DataGrid.Columns>
              </DataGrid>
            </StackPanel>
          </Border>
        </DataTemplate>
      </DataGrid.RowDetailsTemplate>
    </DataGrid>
  </DockPanel>
</Window>
```

### How It Works

1. **Inline editing with conditional locks** — The outer `DataGrid` binds customer names with `Mode=TwoWay`. The nested details grid uses `IsReadOnly="{Binding IsShipped}"` — shipped orders get all cells locked. The `SelectedItem` binding on the outer grid enables the "Ship Selected" command.

2. **Row details for master-detail** — `RowDetailsVisibilityMode="VisibleWhenSelected"` shows the line-item sub-grid only for the selected order. The nested `DataGrid` inside `RowDetailsTemplate` binds to `LineItems` on the `SalesOrder` model. Nested DataGrids have full editing support including keyboard navigation.

3. **Grand total recalculation** — `SalesOrder.GrandTotal` iterates `LineItems` and sums each item's `Total`. The `CollectionChanged` handler on `LineItems` raises `PropertyChanged(nameof(GrandTotal))` when items are added or removed. Individual line item property changes raise their own `Total` recalculation through the `OnQuantityChanged` and `OnUnitPriceChanged` partial methods.

4. **Validation** — The ViewModel exposes `CanEditCell` and `ValidateLineItem` which are intended to be called from code-behind edit events. A real implementation would wire `CellEditEnding` to call these. The `ValidationMessage` property drives a red `TextBlock` above the grid.

5. **Frozen columns** — `FrozenColumnCount="2"` pins ID and Customer columns during horizontal scroll. The status and grand total columns scroll normally.

### Design Decisions and Trade-offs

- **Nested DataGrid vs flat data with row details**: A nested DataGrid in row details gives full column sorting and editing in the detail section. The cost is heavier visual tree per expanded row. For order line items (typically 1-20 items), this is fine.
- **IsShipped as a computed property**: Using `IsReadOnly="{Binding IsShipped}"` leverages the existing binding system instead of handling `BeginningEdit` events. The trade-off is that the entire sub-grid becomes read-only, not individual cells.
- **Grand total as a computed property**: Recalculating on every change is simple and correct for small line-item sets. For large sets (100+ items), consider caching or batched recalculation.

---

## Example 2: Employee Directory with Grouping and Column Reordering

### Goal

Present an employee list grouped by department, with frozen identifier columns and user-configurable column order. Include a search-as-you-type filter that narrows results across multiple fields.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Department { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}

public partial class EmployeeDirectoryViewModel : ObservableObject
{
    private List<Employee> _allEmployees = new();

    [ObservableProperty]
    private DataGridCollectionView? _groupedView;

    [ObservableProperty]
    private string _searchText = "";

    public EmployeeDirectoryViewModel()
    {
        LoadEmployees();
    }

    private void LoadEmployees()
    {
        _allEmployees = new List<Employee>
        {
            new() { Id = 1, FirstName = "Alice", LastName = "Johnson", Department = "Engineering", JobTitle = "Senior Dev", Email = "alice@example.com", HireDate = new(2020, 3, 1), IsActive = true },
            new() { Id = 2, FirstName = "Bob", LastName = "Smith", Department = "Engineering", JobTitle = "Junior Dev", Email = "bob@example.com", HireDate = new(2023, 6, 15), IsActive = true },
            new() { Id = 3, FirstName = "Carol", LastName = "Lee", Department = "Marketing", JobTitle = "Marketing Lead", Email = "carol@example.com", HireDate = new(2021, 1, 10), IsActive = true },
            new() { Id = 4, FirstName = "David", LastName = "Brown", Department = "Marketing", JobTitle = "Designer", Email = "david@example.com", HireDate = new(2022, 9, 5), IsActive = false },
            new() { Id = 5, FirstName = "Eve", LastName = "Davis", Department = "Sales", JobTitle = "Account Manager", Email = "eve@example.com", HireDate = new(2019, 11, 20), IsActive = true },
            new() { Id = 6, FirstName = "Frank", LastName = "Wilson", Department = "Engineering", JobTitle = "DevOps", Email = "frank@example.com", HireDate = new(2021, 7, 8), IsActive = true },
        };
        RebuildView();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (GroupedView is null) return;

        if (string.IsNullOrWhiteSpace(value))
        {
            GroupedView.Filter = null;
        }
        else
        {
            var search = value.Trim();
            GroupedView.Filter = item =>
            {
                if (item is not Employee emp) return false;
                return emp.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || emp.Department.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || emp.JobTitle.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || emp.Email.Contains(search, StringComparison.OrdinalIgnoreCase);
            };
        }
        GroupedView.Refresh();
    }

    [RelayCommand]
    private void GroupByDepartment()
    {
        RebuildView();
    }

    [RelayCommand]
    private void ClearGrouping()
    {
        GroupedView = new DataGridCollectionView(_allEmployees);
    }

    private void RebuildView()
    {
        GroupedView = new DataGridCollectionView(_allEmployees);
        GroupedView.GroupDescriptions.Add(
            new DataGridPathGroupDescription("Department"));
        GroupedView.SortDescriptions.Add(
            new DataGridSortDescription("LastName", ListSortDirection.Ascending));
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        xmlns:models="clr-namespace:MyApp.Models"
        x:DataType="vm:EmployeeDirectoryViewModel"
        Title="Employee Directory" Width="850" Height="550">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <TextBox Watermark="Search employees..."
               Text="{Binding SearchText}"
               Width="250" />
      <Button Content="Group by Dept" Command="{Binding GroupByDepartmentCommand}" />
      <Button Content="Clear Grouping" Command="{Binding ClearGroupingCommand}" />
    </StackPanel>

    <DataGrid ItemsSource="{Binding GroupedView}"
              AutoGenerateColumns="False"
              CanUserReorderColumns="True"
              CanUserResizeColumns="True"
              CanUserSortColumns="True"
              FrozenColumnCount="1"
              AlternatingRowBackground="#F5F5F5"
              RowBackground="White"
              x:DataType="vm:EmployeeDirectoryViewModel">
      <DataGrid.GroupStyle>
        <ControlTheme x:Key="DataGridGroup">
          <Setter Property="Foreground" Value="{DynamicResource SystemAccentColor}" />
          <Setter Property="FontWeight" Value="SemiBold" />
          <Setter Property="Padding" Value="8,4" />
        </ControlTheme>
      </DataGrid.GroupStyle>
      <DataGrid.Columns>
        <DataGridTextColumn Header="ID"
                            Binding="{CompiledBinding Id}"
                            IsReadOnly="True" Width="50" />
        <DataGridTextColumn Header="First Name"
                            Binding="{CompiledBinding FirstName}"
                            Width="100" />
        <DataGridTextColumn Header="Last Name"
                            Binding="{CompiledBinding LastName}"
                            Width="120" />
        <DataGridTextColumn Header="Department"
                            Binding="{CompiledBinding Department}"
                            Width="120" />
        <DataGridTextColumn Header="Job Title"
                            Binding="{CompiledBinding JobTitle}"
                            Width="150" />
        <DataGridTextColumn Header="Email"
                            Binding="{CompiledBinding Email}"
                            Width="180" />
        <DataGridTextColumn Header="Hire Date"
                            Binding="{CompiledBinding HireDate, StringFormat='{0:yyyy-MM-dd}'}"
                            Width="100" />
        <DataGridCheckBoxColumn Header="Active"
                                Binding="{CompiledBinding IsActive}"
                                Width="60" />
      </DataGrid.Columns>
    </DataGrid>
  </DockPanel>
</Window>
```

### How It Works

1. **Grouping via DataGridCollectionView** — The constructor creates a `DataGridCollectionView` from the employee list and adds a `DataGridPathGroupDescription("Department")`. The grid renders group headers with department names. Each group row shows the department name and item count.

2. **Search-as-you-type filter** — `OnSearchTextChanged` sets `GroupedView.Filter` to a predicate that checks `FullName`, `Department`, `JobTitle`, and `Email` against the search text. Calling `GroupedView.Refresh()` reapplies the filter. When search is cleared, `Filter` is set to `null` to show all items.

3. **Sorting** — A `SortDescriptions` entry on `LastName` (ascending) is added in `RebuildView`. Users can also click column headers to change sort order because `CanUserSortColumns="True"` (default).

4. **Frozen columns** — `FrozenColumnCount="1"` pins the ID column. Users can reorder remaining columns via drag-and-drop, but ID always stays leftmost.

5. **Alternating row colors** — `AlternatingRowBackground="#F5F5F5"` with `RowBackground="White"` improves readability across grouped rows.

6. **Group style** — `GroupStyle` applies a `ControlTheme` to group rows. The theme here just sets foreground color and font weight on the default group row template.

### Design Decisions and Trade-offs

- **DataGridCollectionView.Filter vs rebuilding the tree**: The filter approach avoids creating a new collection or copying data. It simply hides non-matching rows. Group headers remain visible if any child matches. The trade-off: filter predicates run on the UI thread, so very large datasets (50k+ items) should debounce keystrokes.
- **FrozenColumnCount vs column reordering**: Frozen columns are index-based, so user reordering does not change which columns are frozen. If you need user-configurable frozen columns, add a context menu or toggle button that rebuilds the column list.
- **DataGridCollectionView limitation**: Adding/removing employees requires replacing the view or calling `Refresh()`. For live-updating employee lists, consider wrapping the collection in an `ObservableCollection` and recreating the view after changes.

---

## Comparison: What Each Example Demonstrates

| Aspect | Example 1: Orders | Example 2: Employees |
|--------|-------------------|---------------------|
| **Primary DataGrid feature** | Inline editing with validation | Grouping with CollectionView |
| **Row details** | Nested DataGrid for line items | Not used |
| **Frozen columns** | 2 columns (ID, Customer) | 1 column (ID) |
| **Column reordering** | Enabled | Enabled |
| **Editing** | TwoWay bindings, conditional IsReadOnly | Read-only display |
| **Filter/Search** | Not implemented | DataGridCollectionView.Filter predicate |
| **Sorting** | Default column-header sorting | Programmatic + column-header sorting |
| **Alternating rows** | Not used | Used |
| **Grouping** | Not used | Grouped by Department |
| **Validation** | ViewModel methods + property clamping | Not needed (read-only) |
| **Data source** | ObservableCollection of SalesOrder | List of Employee wrapped in DataGridCollectionView |
| **Key edge case** | Shipped orders locked, negative values clamped | Group headers with mixed filter results |

---

## See Also

- [040 — DataGrid Deep Dive](040-datagrid-deep-dive.md)
- [040V — DataGrid Deep Dive (verbose companion)](040-datagrid-deep-dive-verbose.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)
- [013 — Data Validation](013-data-validation.md)
- [036 — Virtualization and Large List Performance](../advanced/036-virtualization-large-lists.md)
- [Avalonia Docs: DataGrid](https://docs.avaloniaui.net/controls/datagrid)
- [Avalonia Docs: DataGridCollectionView](https://docs.avaloniaui.net/controls/datagrid/data-grid-collection-view)
