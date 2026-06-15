---
tier: intermediate
topic: data display
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 015-item-lists.md
---

# 015E — Item Lists: Real-World Examples

**What this is:** Two worked examples showing `DataGrid` with row details and `ItemsRepeater` in a dashboard layout. Read [015 — Item Lists](015-item-lists.md) and [015V — Verbose Companion](015-item-lists-verbose.md) first.

---

## Example 1: Order Management DataGrid with Row Details and Inline Editing

### Goal

Display a sortable order list in a `DataGrid`. Each row can be expanded to show line items (row details). The status column is an editable dropdown.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class OrderListViewModel : ObservableObject
{
    public ObservableCollection<OrderViewModel> Orders { get; } = new();

    [RelayCommand]
    private void LoadOrders()
    {
        Orders.Clear();
        Orders.Add(new OrderViewModel
        {
            Id = 1001,
            Customer = "Alice",
            Total = 245.00m,
            Status = "Pending",
            LineItems =
            {
                new LineItem("Widget A", 2, 50.00m),
                new LineItem("Widget B", 1, 145.00m),
            }
        });
        Orders.Add(new OrderViewModel
        {
            Id = 1002,
            Customer = "Bob",
            Total = 89.99m,
            Status = "Shipped",
            LineItems =
            {
                new LineItem("Gadget X", 1, 89.99m),
            }
        });
    }
}

public partial class OrderViewModel : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _customer = string.Empty;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private string _status = "Pending";

    public ObservableCollection<LineItem> LineItems { get; } = new();
}

public record LineItem(string Product, int Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:DataType="vm:OrderListViewModel">

  <Grid RowDefinitions="Auto,*" Margin="20">
    <Button Content="Load Orders"
            Command="{Binding LoadOrdersCommand}"
            Margin="0,0,0,8" />

    <DataGrid Grid.Row="1"
              ItemsSource="{Binding Orders}"
              AutoGenerateColumns="False"
              IsReadOnly="False"
              CanUserSortColumns="True"
              RowDetailsVisibility="VisibleWhenSelected">

      <DataGrid.Columns>
        <DataGridTextColumn Header="Order #"
                            Binding="{CompiledBinding Id}"
                            CanUserSort="True" />
        <DataGridTextColumn Header="Customer"
                            Binding="{CompiledBinding Customer}"
                            CanUserSort="True" />
        <DataGridTextColumn Header="Total"
                            Binding="{CompiledBinding Total, StringFormat='\{0:C\}'}"
                            CanUserSort="True"
                            IsReadOnly="True" />
        <DataGridComboBoxColumn Header="Status"
                                ItemsSource="{Binding $parent[DataGrid].DataContext.StatusOptions}"
                                SelectedItemBinding="{CompiledBinding Status}" />
      </DataGrid.Columns>

      <!-- Row details: line items -->
      <DataGrid.RowDetailsTemplate>
        <DataTemplate x:DataType="vm:OrderViewModel">
          <Border BorderBrush="#ccc" BorderThickness="1"
                  Padding="8" Margin="20,4">
            <Grid>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="Auto" />
              </Grid.ColumnDefinitions>
              <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
              </Grid.RowDefinitions>

              <TextBlock Text="Line Items"
                         FontWeight="SemiBold"
                         Grid.ColumnSpan="3" />

              <!-- Header row -->
              <TextBlock Grid.Row="1" Text="Product" FontWeight="Bold" />
              <TextBlock Grid.Row="1" Grid.Column="1" Text="Qty" FontWeight="Bold" Margin="12,0" />
              <TextBlock Grid.Row="1" Grid.Column="2" Text="Total" FontWeight="Bold" />

              <!-- Items - using ItemsControl because details are small -->
              <ItemsControl Grid.Row="2" Grid.ColumnSpan="3"
                            ItemsSource="{Binding LineItems}">
                <ItemsControl.ItemTemplate>
                  <DataTemplate x:DataType="models:LineItem">
                    <Grid ColumnDefinitions="*,Auto,Auto">
                      <TextBlock Text="{Binding Product}" />
                      <TextBlock Grid.Column="1" Text="{Binding Quantity}" Margin="12,0" />
                      <TextBlock Grid.Column="2"
                                 Text="{Binding LineTotal, StringFormat='\{0:C\}'}" />
                    </Grid>
                  </DataTemplate>
                </ItemsControl.ItemTemplate>
              </ItemsControl>
            </Grid>
          </Border>
        </DataTemplate>
      </DataGrid.RowDetailsTemplate>
    </DataGrid>
  </Grid>
</UserControl>
```

### How It Works

1. `DataGrid.ItemsSource` binds to `Orders`. Each column uses compiled bindings with `x:DataType` inherited from the `UserControl` root for the grid, and `DataTemplate x:DataType="vm:OrderViewModel"` for row details.
2. `DataGridComboBoxColumn` lets the user change the status inline. The `ItemsSource` uses `$parent[DataGrid].DataContext` to reach the parent ViewModel's `StatusOptions` collection.
3. `RowDetailsVisibility="VisibleWhenSelected"` — selecting a row expands its details section, showing the line items `ItemsControl`.
4. `CanUserSort="True"` on columns enables click-to-sort. The DataGrid re-sorts the `ObservableCollection` in-place.

### Design Decisions & Edge Cases

- **Why `ItemsControl` in row details instead of another `DataGrid`:** Row details are typically small (2–5 items). `ItemsControl` has no virtualization overhead and no selection chrome. A nested `DataGrid` would be visually confusing.
- **Edge case — sorting + inline editing:** When the user edits a status via the combo box, the sort order does not update automatically. The DataGrid re-sorts only when the column header is clicked. If auto-resort is needed, handle `CellEditEnding` and re-sort programmatically.
- **Edge case — row details with virtualized rows:** `DataGrid` virtualizes rows (creates containers only for visible rows). Row details are also virtualized. If the details are expensive to build, keep them lightweight — avoid complex nested lists.
- **Trade-off:** `DataGridComboBoxColumn` requires `SelectedItemBinding` with a string (not an enum). If `Status` were an enum, add a converter or use `DataGridTemplateColumn` with a real `ComboBox`.

---

## Example 2: Dashboard Tile Grid with ItemsRepeater

### Goal

Display a dashboard of metric tiles in a responsive grid layout, where each tile shows a label, value, and trend indicator. Tiles are virtualized and laid out in a uniform grid.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class MetricTile : ObservableObject
{
    [ObservableProperty]
    private string _label = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private double _changePercent;

    public bool IsPositiveChange => ChangePercent >= 0;
}

public partial class DashboardViewModel : ObservableObject
{
    public ObservableCollection<MetricTile> Tiles { get; } = new()
    {
        new() { Label = "Revenue", Value = "$48,290", ChangePercent = 12.5 },
        new() { Label = "Users", Value = "2,847", ChangePercent = 8.3 },
        new() { Label = "Orders", Value = "341", ChangePercent = -2.1 },
        new() { Label = "Conversion", Value = "3.2%", ChangePercent = 0.7 },
        new() { Label = "Churn", Value = "1.2%", ChangePercent = -0.3 },
        new() { Label = "Avg. Order", Value = "$142", ChangePercent = 4.1 },
        new() { Label = "Support Tickets", Value = "23", ChangePercent = -15.0 },
        new() { Label = "Page Load", Value = "1.2s", ChangePercent = 5.0 },
    };
}
```

### Value Converter

```csharp
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyApp.Converters;

public class ChangeToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d >= 0
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ChangeToGlyphConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d && d >= 0 ? "▲" : "▼";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:conv="using:MyApp.Converters"
             x:DataType="vm:DashboardViewModel">

  <UserControl.Resources>
    <conv:ChangeToColorConverter x:Key="ChangeToColor" />
    <conv:ChangeToGlyphConverter x:Key="ChangeToGlyph" />
  </UserControl.Resources>

  <ScrollViewer HorizontalScrollBarVisibility="Disabled"
                VerticalScrollBarVisibility="Auto">
    <ItemsRepeater ItemsSource="{Binding Tiles}"
                  x:DataType="vm:DashboardViewModel">
      <ItemsRepeater.ItemTemplate>
        <DataTemplate x:DataType="vm:MetricTile">
          <Border Background="{DynamicResource Surface}"
                  BorderBrush="#e0e0e0"
                  BorderThickness="1"
                  CornerRadius="8"
                  Padding="16"
                  Margin="4">
            <Grid RowDefinitions="Auto,Auto,Auto" Spacing="4">
              <TextBlock Text="{Binding Label}"
                         FontSize="12"
                         Foreground="#888" />
              <TextBlock Grid.Row="1"
                         Text="{Binding Value}"
                         FontSize="28"
                         FontWeight="Bold" />
              <StackPanel Grid.Row="2" Orientation="Horizontal" Spacing="4">
                <TextBlock Text="{Binding ChangePercent,
                           Converter={StaticResource ChangeToGlyph}}"
                           Foreground="{Binding ChangePercent,
                             Converter={StaticResource ChangeToColor}}"
                           FontSize="14" />
                <TextBlock Text="{Binding ChangePercent,
                           StringFormat='\{0:F1\}%'}"
                           Foreground="{Binding ChangePercent,
                             Converter={StaticResource ChangeToColor}}"
                           FontSize="14" />
              </StackPanel>
            </Grid>
          </Border>
        </DataTemplate>
      </ItemsRepeater.ItemTemplate>

      <ItemsRepeater.Layout>
        <UniformGridLayout Orientation="Horizontal"
                           MinItemWidth="200"
                           MinItemHeight="120" />
      </ItemsRepeater.Layout>
    </ItemsRepeater>
  </ScrollViewer>
</UserControl>
```

### How It Works

1. `ItemsRepeater` binds to `Tiles`. Each tile is rendered by the `DataTemplate` directly — no `ListBoxItem` wrapper, no selection.
2. `UniformGridLayout` arranges tiles in a grid. `MinItemWidth="200"` ensures each tile is at least 200px wide. The layout wraps to the next row when the available width is exceeded.
3. The `ScrollViewer` provides scrolling. Without it, `ItemsRepeater` would take infinite vertical space.
4. Two converters render the trend indicator — `ChangeToColorConverter` returns green/red, `ChangeToGlyphConverter` returns `▲`/`▼`.
5. The tile template uses `{DynamicResource Surface}` so the tile background follows the active theme.

### Design Decisions & Edge Cases

- **Why `ItemsRepeater` over `ListBox` or `WrapPanel`:** `ListBox` forces a stack layout and adds selection behavior that is not needed. A `WrapPanel` inside `ItemsControl` would not virtualize. `ItemsRepeater` with `UniformGridLayout` gives both grid layout and virtualization.
- **Why `UniformGridLayout` (equal-sized cells) instead of `StackLayout` (sequential stack):** Tiles look better in a grid. `UniformGridLayout` auto-wraps based on available width and makes all cells the same size.
- **Edge case — tile content overflows:** The tile `Border` has `CornerRadius="8"` and fixed `Padding`. If the value text is very long (e.g., "$1,234,567,890"), it overflows. Clip with `TextTrimming="CharacterEllipsis"` on the `TextBlock`.
- **Edge case — zero tiles:** The `ItemsRepeater` renders nothing; the `ScrollViewer` shows empty space. Add a `TextBlock` overlay when `Tiles.Count == 0`.
- **Performance:** With 8 tiles, virtualization is irrelevant. With 500 tiles, `UniformGridLayout` creates containers only for visible tiles, and recycling keeps memory flat. Measure with DevTools `Layout Explorer` to confirm.
- **Trade-off:** `ItemsRepeater` has no built-in selection, keyboard navigation, or accessibility. For a dashboard of read-only tiles this is fine. If tiles need to be focusable, add `TabNavigation` properties manually.

---

## Comparison

| Aspect | Example 1 — Order DataGrid | Example 2 — Dashboard Grid |
|---|---|---|
| **Control** | `DataGrid` | `ItemsRepeater` + `ScrollViewer` |
| **Layout** | Table (rows/columns) | Uniform grid (auto-wrap) |
| **Virtualization** | Row-level | Yes (via `UniformGridLayout`) |
| **Selection** | Yes (row selection, row details) | No |
| **Editing** | Inline (combo box for status) | Read-only |
| **Sorting** | Built-in (click column headers) | N/A |
| **When to use** | Tabular CRUD, order lists, spreadsheets | Dashboards, galleries, card UIs |
| **Key risk** | Nested virtualization in row details | No keyboard navigation by default |

---

## See Also

- [015 — Item Lists (original)](015-item-lists.md)
- [015V — Item Lists (verbose companion)](015-item-lists-verbose.md)
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
- [Avalonia Docs: DataGrid](https://docs.avaloniaui.net/controls/datagrid)
- [Avalonia Docs: ItemsRepeater](https://docs.avaloniaui.net/controls/itemsrepeater)
