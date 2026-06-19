---
tier: intermediate
topic: controls
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 064V — Selection Controls (verbose companion)

**What this covers:** ISelectionModel internals, ComboBox dropdown events, custom containers, and performance tuning for large lists.

**Prerequisites:** 064 — Selection Controls core

---

## 1. ISelectionModel in depth

The `Selection` property on `ListBox` provides an `ISelectionModel` that is optimized for large collections. It avoids materializing the full selection set as objects.

```csharp
public interface ISelectionModel
{
    void Clear();
    void Select(int index);
    void SelectRange(int from, int to);
    void Deselect(int index);
    bool IsSelected(int index);
    int Count { get; }
    IEnumerable<int> SelectedIndices { get; }
    IEnumerable<object?> SelectedItems { get; }
    int AnchorIndex { get; set; }
    int RangeIndex { get; set; }
    event EventHandler<SelectionModelSelectionChangedEventArgs>? SelectionChanged;
}
```

### Batch updates

```csharp
// Suspend notifications for bulk operations
selection.BeginBatchUpdate();
try
{
    selection.Select(0);
    selection.SelectRange(2, 5);
}
finally
{
    selection.EndBatchUpdate();
}
```

### Source index mapping

The `ISelectionModel` works with indices. When the source collection changes (insert/remove), indices auto-adjust:

```csharp
Items.Insert(0, newItem);
// Existing selection indices shift automatically
```

---

## 2. ComboBox dropdown events

```csharp
comboBox.DropDownOpened += (s, e) =>
{
    // Populate items lazily, log analytics, etc.
};

comboBox.DropDownClosed += (s, e) =>
{
    // Commit changes, update UI state
};
```

### Programmatic control

```csharp
// Open/close the dropdown
comboBox.IsDropDownOpen = true;

// Check if open
bool isOpen = comboBox.IsDropDownOpen;
```

---

## 3. SelectionBoxItem and SelectionBoxItemTemplate

The `SelectionBoxItem` controls what the ComboBox displays in its closed state (as opposed to the dropdown items):

```xml
<ComboBox ItemsSource="{Binding Products}"
          SelectionBoxItemTemplate="{StaticResource CompactTemplate}">
  <ComboBox.ItemTemplate>
    <DataTemplate>
      <!-- Full template for dropdown -->
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

This is useful when the selected item presentation should be more compact than the dropdown entries.

---

## 4. Custom item containers

### ComboBoxItem

```xml
<ComboBox.Items>
  <ComboBoxItem IsEnabled="True">Option A</ComboBoxItem>
  <ComboBoxItem>Option B</ComboBoxItem>
  <ComboBoxItem IsEnabled="False">Option C (disabled)</ComboBoxItem>
</ComboBox.Items>
```

### ListBoxItem

```csharp
// Custom container class
public class CustomListBoxItem : ListBoxItem
{
    // Override OnPropertyChanged, add custom logic
}
```

Register with the ItemsControl:

```csharp
listBox.ItemContainerGenerator.ItemContainerType = typeof(CustomListBoxItem);
```

---

## 5. Performance: large lists

### Virtualization tips

- Both controls virtualize items by default using `VirtualizingStackPanel`
- Use `AsyncEnumerable` or paging for 10,000+ items
- Avoid complex `DataTemplate` nesting for the root items
- Set `ScrollViewer.VerticalScrollBarVisibility="Visible"` to avoid layout re-measure

### Deferred loading

```csharp
// Load on dropdown open
comboBox.DropDownOpened += async (s, e) =>
{
    if (comboBox.Items.Count == 0)
    {
        var items = await LoadItemsAsync();
        foreach (var item in items)
            comboBox.Items.Add(item);
    }
};
```

---

## 6. Text search in SelectingItemsControl

The `IsTextSearchEnabled` property (inherited from `SelectingItemsControl`) allows jumping to items by typing the first few characters:

```xml
<ListBox IsTextSearchEnabled="True"
         ItemsSource="{Binding Countries}" />
```

The control matches against `DisplayMemberBinding` or item `ToString()`.

---

## 7. WrapSelection and keyboard navigation

```xml
<ComboBox WrapSelection="True" />
```

When `WrapSelection` is `True`, pressing the down arrow on the last item wraps to the first, and vice-versa.

---

## 8. AutoScrollToSelectedItem

```xml
<ListBox AutoScrollToSelectedItem="True" />
```

Ensures the selected item is always visible in the viewport. Enabled by default.

---

## See Also

- [064 — Selection Controls (core)](064-selection-controls.md)
- [064E — Selection Controls (examples)](064-selection-controls-examples.md)
- [Avalonia API: ISelectionModel](https://docs.avaloniaui.net/api/avalonia/controls/selection/iselectionmodel)
- [Avalonia API: SelectionModel](https://docs.avaloniaui.net/api/avalonia/controls/selection/selectionmodel)
- [Avalonia API: SelectingItemsControl](https://docs.avaloniaui.net/api/avalonia/controls/primitives/selectingitemscontrol)
- [058 — ScrollViewer & ScrollBar](058-scrollviewer-scrollbar.md)
