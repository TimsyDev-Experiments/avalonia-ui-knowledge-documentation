---
tier: intermediate
topic: controls
estimated: 16 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 064 — Selection Controls: ComboBox & ListBox

**What you'll learn:** How to bind, template, and configure `ComboBox` and `ListBox` for single and multi-select scenarios.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md), [015 — Item Lists](015-item-lists.md)

---

## 1. ComboBox essentials

The `ComboBox` presents a selected item with a drop-down button. Bind to a collection and track the selection:

```xml
<ComboBox ItemsSource="{Binding Categories}"
          SelectedItem="{Binding SelectedCategory}"
          PlaceholderText="Select a category..." />
```

```csharp
public partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _selectedCategory;

    public ObservableCollection<string> Categories { get; } = new()
    {
        "Books", "Electronics", "Garden", "Music"
    };
}
```

| Property | Description |
|----------|-------------|
| `ItemsSource` | Collection of items |
| `SelectedItem` | Currently selected item |
| `SelectedIndex` | Zero-based index (-1 = nothing selected) |
| `SelectedValue` | Value extracted via `SelectedValueBinding` |
| `PlaceholderText` | Shown when nothing is selected |

### Item template

For complex objects, provide a `DataTemplate`:

```xml
<ComboBox ItemsSource="{Binding Products}"
          SelectedItem="{Binding SelectedProduct}">
  <ComboBox.ItemTemplate>
    <DataTemplate>
      <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBlock Text="{Binding Name}" FontWeight="Bold" />
        <TextBlock Text="{Binding Price, StringFormat='${0:F2}'}"
                   Foreground="{StaticResource SystemAccentColor}" />
      </StackPanel>
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

### Setting initial selection

```csharp
// In ViewModel constructor or load method
SelectedCategory = Categories[0];
```

---

## 2. Editable ComboBox

Set `IsEditable="True"` to allow typing:

```xml
<ComboBox IsEditable="True"
          ItemsSource="{Binding Cities}"
          Text="{Binding SearchText}"
          PlaceholderText="Type or select a city" />
```

For complex objects, tell the control which property to match against:

```xml
<ComboBox IsEditable="True" ItemsSource="{Binding People}"
          TextSearch.TextBinding="{Binding FullName}" />
```

The `Text` property holds the current text. `SelectedItem` is updated when the typed text matches an item.

---

## 3. SelectedValue / SelectedValueBinding

When you only need a single property of the selected item (e.g., an ID):

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

```xml
<ComboBox ItemsSource="{Binding Products}"
          SelectedValueBinding="{Binding Id}"
          SelectedValue="{Binding SelectedProductId}"
          DisplayMemberBinding="{Binding Name}" />
```

---

## 4. ListBox selection modes

`ListBox` supports multiple selection modes via `SelectionMode`:

```xml
<ListBox ItemsSource="{Binding Items}"
         SelectedItem="{Binding SelectedItem}" />
```

| Mode | Description |
|------|-------------|
| `Single` (default) | One item selected at a time |
| `Multiple` | Multiple items via Ctrl+click |
| `Toggle` | Tap/Space toggles selection |
| `AlwaysSelected` | Always at least one item selected |

Combine flags with comma:

```xml
<ListBox SelectionMode="Multiple,Toggle">
```

### Multi-select binding

Use `Selection` (an `ISelectionModel`) for efficient multi-select with large collections:

```xml
<ListBox ItemsSource="{Binding Items}"
         Selection="{Binding Selection}" />
```

```csharp
[ObservableProperty]
private ISelectionModel _selection = new SelectionModel<string>();

// Access selected items:
foreach (var item in Selection.SelectedItems) { }
```

### SelectedItems (small collections)

```xml
<ListBox ItemsSource="{Binding Tags}"
         SelectedItems="{Binding SelectedTags}" />
```

```csharp
[ObservableProperty]
private ObservableCollection<string> _selectedTags = new();
```

---

## 5. ListBox item styling

Style `ListBoxItem` elements through `ListBox.Styles`:

```xml
<ListBox ItemsSource="{Binding Animals}">
  <ListBox.Styles>
    <Style Selector="ListBoxItem">
      <Setter Property="Padding" Value="8,4" />
      <Setter Property="Margin" Value="2" />
    </Style>
  </ListBox.Styles>
  <ListBox.ItemTemplate>
    <DataTemplate>
      <Border BorderBrush="Blue" BorderThickness="1"
              CornerRadius="4" Padding="4">
        <TextBlock Text="{Binding}" />
      </Border>
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

---

## 6. ScrollViewer properties

Both controls have a built-in `ScrollViewer`. Control scrollbar visibility:

```xml
<ListBox ScrollViewer.HorizontalScrollBarVisibility="Disabled"
         ScrollViewer.VerticalScrollBarVisibility="Auto" />

<ComboBox MaxDropDownHeight="300" />
```

---

## 7. Virtualization

Both controls use `VirtualizingStackPanel` by default, which is efficient for large lists. To disable virtualization (e.g., for nested scrolling):

```xml
<ListBox>
  <ListBox.ItemsPanel>
    <ItemsPanelTemplate>
      <StackPanel />
    </ItemsPanelTemplate>
  </ListBox.ItemsPanel>
</ListBox>
```

---

## Key Takeaways

- Bind `ItemsSource` + `SelectedItem` for basic selection workflows
- Use `ItemTemplate` to control how items appear
- `IsEditable` with `TextSearch.TextBinding` enables type-to-select in ComboBox
- `SelectedValue`/`SelectedValueBinding` extract a single property from the selection
- ListBox supports `Single`, `Multiple`, `Toggle`, and `AlwaysSelected` modes
- `ISelectionModel` is the recommended multi-select API for large collections
- Style `ListBoxItem` via `ListBox.Styles` with `Selector="ListBoxItem"`

---

## See Also

- [064V — Selection Controls (verbose)](064-selection-controls-verbose.md)
- [064E — Selection Controls (examples)](064-selection-controls-examples.md)
- [Avalonia Docs: ComboBox](https://docs.avaloniaui.net/controls/input/selectors/combobox)
- [Avalonia Docs: ListBox](https://docs.avaloniaui.net/controls/data-display/collections/listbox)
- [Avalonia Docs: ComboBox API](https://docs.avaloniaui.net/api/avalonia/controls/combobox)
- [015 — Item Lists](015-item-lists.md)
- [061 — Collection Views](061-collection-views.md)
