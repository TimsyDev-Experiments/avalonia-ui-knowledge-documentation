---
tier: intermediate
topic: interactions
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 019-drag-drop.md
---

# 019E — Drag & Drop: Real-World Examples

**What this is:** Two worked examples of drag-and-drop in Avalonia 12 using `DataTransfer` and gesture recognizers. Read [019 — Drag & Drop](019-drag-drop.md) and [019V — Verbose Companion](019-drag-drop-verbose.md) first.

---

## Example 1: Reorderable ListBox with Drag-and-Drop Rearrangement

### Goal

Allow the user to reorder items in a `ListBox` by dragging them up or down within the list. The `ObservableCollection` is updated to reflect the new order, and a visual indicator shows the drop position.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class TaskItem : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private int _priority;
}

public partial class TaskListViewModel : ObservableObject
{
    public ObservableCollection<TaskItem> Tasks { get; } = new()
    {
        new() { Title = "Design wireframes", Priority = 1 },
        new() { Title = "Implement login", Priority = 2 },
        new() { Title = "Write unit tests", Priority = 3 },
        new() { Title = "Review PR", Priority = 4 },
        new() { Title = "Deploy to staging", Priority = 5 },
    };

    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        Tasks.Move(fromIndex, toIndex);

        // Re-assign priorities based on new order
        for (var i = 0; i < Tasks.Count; i++)
            Tasks[i].Priority = i + 1;
    }
}
```

### Code-Behind — Drag and Drop Handlers

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using MyApp.ViewModels;

namespace MyApp.Views;

public partial class TaskListView : UserControl
{
    private const string TaskItemFormat = "MyApp.TaskItem";

    public TaskListView()
    {
        InitializeComponent();
    }

    private void OnDragStarting(object? sender, DragStartingEventArgs e)
    {
        if (sender is Control c && c.DataContext is TaskItem item)
        {
            var data = new DataTransfer();
            var dataItem = new DataTransferItem();

            // Store the item index for lookup during drop
            dataItem.Set(DataFormat.Text, item.Title);
            dataItem.Set(new DataFormat(TaskItemFormat), item);
            data.Add(dataItem);

            e.Data = data;
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(new DataFormat(TaskItemFormat))
            ? DragEffects.Move
            : DragEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is ListBox listBox
            && listBox.DataContext is TaskListViewModel vm
            && e.DataTransfer.Contains(new DataFormat(TaskItemFormat)))
        {
            // Determine drop index from the item under the cursor
            var dropPosition = e.GetPosition(listBox);
            var dropIndex = GetDropIndex(listBox, dropPosition);

            // Find the source item
            var draggedItem = e.DataTransfer
                .Get(new DataFormat(TaskItemFormat)) as TaskItem;

            if (draggedItem is null) return;

            var fromIndex = vm.Tasks.IndexOf(draggedItem);
            vm.MoveItem(fromIndex, dropIndex);
        }
    }

    private static int GetDropIndex(ListBox listBox, Point dropPosition)
    {
        var items = listBox.ItemCount;
        for (var i = 0; i < items; i++)
        {
            var container = listBox.ContainerFromIndex(i);
            if (container is null) continue;

            var bounds = container.Bounds;
            var midY = bounds.Y + bounds.Height / 2;

            if (dropPosition.Y < midY)
                return i;
        }
        return items;
    }
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:TaskListViewModel"
             x:Class="MyApp.Views.TaskListView">

  <Grid RowDefinitions="Auto,*" Margin="20">
    <TextBlock Text="Drag tasks to reorder"
               FontSize="14" Foreground="#888" Margin="0,0,0,8" />

    <ListBox Grid.Row="1"
             ItemsSource="{Binding Tasks}"
             AllowDrop="True"
             SelectionMode="Single">

      <ListBox.Gestures>
        <DropGestureRecognizer DragOver="OnDragOver"
                                Drop="OnDrop" />
      </ListBox.Gestures>

      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:TaskItem">
          <Grid ColumnDefinitions="Auto,*,Auto" Spacing="8" Padding="4">
            <DragGestureRecognizer DragStarting="OnDragStarting"
                                    CanDrag="True" />

            <CheckBox Grid.Column="0"
                      IsChecked="{Binding IsComplete}" />
            <TextBlock Grid.Column="1"
                       Text="{Binding Title}"
                       TextDecorations="{Binding IsComplete,
                         Converter={StaticResource StrikeThroughConverter}}" />
            <TextBlock Grid.Column="2"
                       Text="{Binding Priority, StringFormat='#{0}'}"
                       Foreground="#888" FontSize="12" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</UserControl>
```

### How It Works

1. Each item in the `ListBox` has a `DragGestureRecognizer` in its template. When the user clicks and drags a task, `OnDragStarting` fires. It stores the `TaskItem` object using a custom `DataFormat` (`"MyApp.TaskItem"`) plus the item title as text.
2. As the pointer moves over the `ListBox`, `OnDragOver` checks for the custom format. If present, `DragEffects.Move` is set, showing the move cursor.
3. On drop, `OnDrop` calculates the target index from `e.GetPosition(listBox)` by iterating containers and comparing against their vertical midpoints.
4. `TaskListViewModel.MoveItem` calls `ObservableCollection.Move`, which fires `CollectionChanged` with a `Move` action. The `ListBox` animates the item to its new position.
5. Priorities are reassigned after the move.

### Design Decisions & Edge Cases

- **Why custom `DataFormat` instead of just `DataFormat.Text`:** A custom format carries the actual `TaskItem` object reference. Text-only would require looking up the item by title, which is fragile (duplicate titles). The custom format works within the same process.
- **Why `GetDropIndex` iterates containers instead of using a simple index from the mouse position:** The mouse may land on a gap between items. Iterating containers and comparing against midpoints gives a stable drop position regardless of item height variation.
- **Edge case — drag to same position:** `MoveItem` checks `fromIndex == toIndex` and returns early. No unnecessary `CollectionChanged` event.
- **Edge case — empty list:** `DragOver` never fires because there is no drop target. The `ListBox` has zero items and zero area. If drop-on-empty-list should add items, set `AllowDrop` on a parent panel instead.
- **Edge case — item under cursor is not the dragged item (scrolling):** If the user drags past the visible area, the `ListBox` auto-scrolls. `ContainerFromIndex` may return `null` for virtualized items outside the viewport. The `GetDropIndex` method skips null containers and returns the last valid index.

---

## Example 2: Drag from DataGrid to External File System

### Goal

Export rows from a `DataGrid` by dragging selected items to Windows Explorer, macOS Finder, or a file upload dialog. The dragged data includes the file content as a CSV string and a `.csv` file path.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;

namespace MyApp.ViewModels;

public partial class ExportRow : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private int _quantity;
}

public partial class ProductListViewModel : ObservableObject
{
    public ObservableCollection<ExportRow> Products { get; } = new()
    {
        new() { Name = "Widget A", Category = "Tools", Price = 12.99m, Quantity = 100 },
        new() { Name = "Gadget B", Category = "Electronics", Price = 49.99m, Quantity = 50 },
        new() { Name = "Doohickey C", Category = "Tools", Price = 8.49m, Quantity = 200 },
    };

    public string GenerateCsv(IEnumerable<ExportRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Name,Category,Price,Quantity");
        foreach (var r in rows)
            sb.AppendLine($"{EscapeCsv(r.Name)},{EscapeCsv(r.Category)},{r.Price},{r.Quantity}");
        return sb.ToString();
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
```

### Code-Behind — Drag from DataGrid

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using MyApp.ViewModels;
using System.Linq;

namespace MyApp.Views;

public partial class ProductListView : UserControl
{
    public ProductListView()
    {
        InitializeComponent();
    }

    private void OnDragStarting(object? sender, DragStartingEventArgs e)
    {
        // Get selected rows from the DataGrid
        var dataGrid = this.FindControl<DataGrid>("ProductGrid");
        if (dataGrid?.DataContext is not ProductListViewModel vm)
            return;

        var selectedRows = dataGrid.SelectedItems
            .OfType<ExportRow>()
            .ToList();

        if (selectedRows.Count == 0) return;

        // Generate CSV content
        var csv = vm.GenerateCsv(selectedRows);
        var fileName = $"products_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var tempPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), fileName);

        // Write to temp file
        System.IO.File.WriteAllText(tempPath, csv);

        var data = new DataTransfer();
        var textItem = new DataTransferItem();
        textItem.Set(DataFormat.Text, csv);
        data.Add(textItem);

        var fileItem = new DataTransferItem();
        fileItem.Set(DataFormat.Files, new[] { tempPath });
        data.Add(fileItem);

        e.Data = data;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Accept if the target understands files or text
        e.DragEffects = (e.DataTransfer.Contains(DataFormat.Files)
                      || e.DataTransfer.Contains(DataFormat.Text))
            ? DragEffects.Copy
            : DragEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        // Handle drop back into the grid (re-import)
        if (e.DataTransfer.Contains(DataFormat.Files))
        {
            var files = await e.DataTransfer.TryGetFilesAsync();
            if (files is not null)
            {
                foreach (var file in files)
                {
                    // Process dropped CSV files
                }
            }
        }
    }
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:ProductListViewModel"
             x:Class="MyApp.Views.ProductListView">

  <Grid RowDefinitions="Auto,*" Margin="20">
    <TextBlock Text="Drag rows to export as CSV"
               FontSize="14" Foreground="#888" Margin="0,0,0,8" />

    <DataGrid Grid.Row="1"
              Name="ProductGrid"
              ItemsSource="{Binding Products}"
              AutoGenerateColumns="False"
              IsReadOnly="True"
              SelectionMode="Multiple"
              AllowDrop="True">

      <DataGrid.Gestures>
        <DropGestureRecognizer DragOver="OnDragOver"
                                Drop="OnDrop" />
      </DataGrid.Gestures>

      <DataGrid.Columns>
        <DataGridTextColumn Header="Name"
                            Binding="{CompiledBinding Name}" />
        <DataGridTextColumn Header="Category"
                            Binding="{CompiledBinding Category}" />
        <DataGridTextColumn Header="Price"
                            Binding="{CompiledBinding Price, StringFormat='\{0:C\}'}" />
        <DataGridTextColumn Header="Qty"
                            Binding="{CompiledBinding Quantity}" />
      </DataGrid.Columns>

      <DataGrid.RowHeaderTemplate>
        <DataTemplate x:DataType="vm:ExportRow">
          <DragGestureRecognizer DragStarting="OnDragStarting"
                                  CanDrag="True" />
        </DataTemplate>
      </DataGrid.RowHeaderTemplate>
    </DataGrid>
  </Grid>
</UserControl>
```

### How It Works

1. The `DragGestureRecognizer` is placed in the row header template. When the user clicks and drags a row header (or the row itself), `OnDragStarting` fires.
2. `OnDragStarting` gets selected items from `DataGrid.SelectedItems`, generates CSV content in memory, writes a temporary `.csv` file, and populates the `DataTransfer` with both `DataFormat.Text` (CSV content) and `DataFormat.Files` (the temp file path).
3. The user drops onto Explorer/Finder. The OS reads the `Files` format and copies/moves the temp file to the drop location.
4. The `DataGrid` also has a `DropGestureRecognizer` for re-import scenarios. `OnDragOver` accepts files and text.

### Design Decisions & Edge Cases

- **Why both `Text` and `Files` formats:** Some targets prefer file paths (Explorer), others accept pasted text (email, chat). Providing both maximizes compatibility. The `Files` format is what Explorer needs for a file copy operation.
- **Why write a temp file instead of streaming:** The `DataFormat.Files` API expects file paths on disk. For large exports (10,000+ rows), write to a temp file and let the OS handle the copy. Clean up temp files on app exit.
- **Edge case — no selection:** `SelectedItems` may be empty. `OnDragStarting` checks and returns early if nothing is selected.
- **Edge case — drag multiple rows:** `DataGrid.SelectedItems` returns all selected `ExportRow` objects. The CSV includes all of them. The temp file name includes a timestamp to avoid collisions.
- **Edge case — drop on same DataGrid:** The `Drop` handler on the grid can re-import CSV files. If the user drops the exported file back onto the grid, parse and merge the rows. Use `TryGetFilesAsync` (which is async) to read the file list.
- **Trade-off:** The `DragGestureRecognizer` in the row header template means the user must drag from the row header area (the small square at the left of each row). To allow dragging from anywhere in the row, place the recognizer in the `RowDetailsTemplate` or use a `DataGridTemplateColumn` with the recognizer in the cell.

---

## Comparison

| Aspect | Example 1 — Reorderable List | Example 2 — Export to File System |
|---|---|---|
| **Source control** | `ListBox` | `DataGrid` |
| **Target** | Same `ListBox` (reorder) | External app (Explorer/Finder) |
| **Data format** | Custom `DataFormat` (object reference) | `DataFormat.Text` + `DataFormat.Files` |
| **Drag effect** | `DragEffects.Move` | `DragEffects.Copy` |
| **Async operations** | None | `TryGetFilesAsync` on drop |
| **When to use** | Task lists, playlists, priority sorting | Data export, attachment dragging, file management |
| **Key risk** | Custom format unused cross-process | Temp file cleanup on crash |

---

## See Also

- [019 — Drag & Drop (original)](019-drag-drop.md)
- [019V — Drag & Drop (verbose companion)](019-drag-drop-verbose.md)
- [015 — Item Lists](015-item-lists.md) (DataGrid, ListBox)
- [Avalonia Docs: Drag & Drop](https://docs.avaloniaui.net/docs/input/drag-and-drop)
