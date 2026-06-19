---
tier: intermediate
topic: controls
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 064E — Selection Controls (examples)

## Example 1: ComboBox with enum values

```xml
<ComboBox ItemsSource="{Binding PriorityValues}"
          SelectedItem="{Binding SelectedPriority}" />
```

```csharp
public enum Priority { Low, Medium, High, Critical }

public partial class ViewModel : ObservableObject
{
    public Array PriorityValues => Enum.GetValues<Priority>();

    [ObservableProperty]
    private Priority _selectedPriority = Priority.Medium;
}
```

For display labels, wrap in a record:

```csharp
public record PriorityOption(Priority Value, string Label);

public Array PriorityOptions => Enum.GetValues<Priority>()
    .Select(p => new PriorityOption(p, p switch
    {
        Priority.Low => "Low Priority",
        Priority.Medium => "Medium Priority",
        Priority.High => "High Priority",
        Priority.Critical => "Critical Priority",
        _ => p.ToString()
    }))
    .ToArray();
```

---

## Example 2: Editable ComboBox with filtering

```xml
<ComboBox IsEditable="True"
          ItemsSource="{Binding AllCountries}"
          Text="{Binding CountryText}"
          TextSearch.TextBinding="{Binding Name}" />
```

```csharp
public partial class ViewModel : ObservableObject
{
    public ObservableCollection<Country> AllCountries { get; } = new()
    {
        new("USA"), new("Canada"), new("Mexico"),
        new("Brazil"), new("Argentina"), new("UK"),
        new("Germany"), new("France"), new("Japan")
    };

    [ObservableProperty]
    private string _countryText = "";
}

public record Country(string Name);
```

---

## Example 3: ListBox multi-select with ISlectionModel

```xml
<ListBox ItemsSource="{Binding Tasks}"
         Selection="{Binding Selection}"
         SelectionMode="Multiple,Toggle">
  <ListBox.ItemTemplate>
    <DataTemplate>
      <CheckBox Content="{Binding Title}" IsChecked="{Binding IsDone}"
                IsHitTestVisible="False" />
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>

<StackPanel Spacing="8">
  <TextBlock Text="{Binding Selection.Count, StringFormat='{0} selected'}" />
  <Button Content="Delete Selected" Command="{Binding DeleteSelectedCommand}" />
  <Button Content="Select All" Command="{Binding SelectAllCommand}" />
</StackPanel>
```

```csharp
public partial class ViewModel : ObservableObject
{
    public ObservableCollection<TaskItem> Tasks { get; } = new();

    [ObservableProperty]
    private ISelectionModel _selection = new SelectionModel<TaskItem>();

    [RelayCommand]
    private void DeleteSelected()
    {
        var toRemove = Selection.SelectedItems.Cast<TaskItem>().ToList();
        foreach (var item in toRemove)
            Tasks.Remove(item);
    }

    [RelayCommand]
    private void SelectAll()
    {
        Selection.BeginBatchUpdate();
        try
        {
            Selection.Clear();
            Selection.SelectRange(0, Tasks.Count - 1);
        }
        finally
        {
            Selection.EndBatchUpdate();
        }
    }
}

public partial class TaskItem : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private bool _isDone;
}
```

---

## Example 4: Static items in XAML

```xml
<ComboBox SelectedIndex="1">
  <ComboBoxItem>Small</ComboBoxItem>
  <ComboBoxItem IsSelected="True">Medium</ComboBoxItem>
  <ComboBoxItem>Large</ComboBoxItem>
  <ComboBoxItem>Extra Large</ComboBoxItem>
</ComboBox>
```

---

## Example 5: ComboBox with selected value binding

```xml
<ComboBox ItemsSource="{Binding Products}"
          SelectedValueBinding="{Binding Id}"
          SelectedValue="{Binding SelectedProductId}"
          DisplayMemberBinding="{Binding Name}" />
```

```csharp
public partial class ViewModel : ObservableObject
{
    public ObservableCollection<Product> Products { get; } = new()
    {
        new(1, "Laptop"), new(2, "Mouse"), new(3, "Keyboard")
    };

    [ObservableProperty]
    private int _selectedProductId;
}

public record Product(int Id, string Name);
```

---

## Example 6: ListBox drag-to-reorder (with GestureRecognizers)

```xml
<ListBox ItemsSource="{Binding Items}"
         SelectionMode="Single">
  <ListBox.GestureRecognizers>
    <DragRecognizer CanDrag="True" />
    <DropRecognizer AllowedEffects="Move" />
  </ListBox.GestureRecognizers>
</ListBox>
```

---

## Example 7: Dropdown-on-hover ComboBox

```csharp
comboBox.PointerEntered += (s, e) => comboBox.IsDropDownOpen = true;
comboBox.PointerExited += (s, e) => comboBox.IsDropDownOpen = false;
```

---

## See Also

- [064 — Selection Controls (core)](064-selection-controls.md)
- [064V — Selection Controls (verbose)](064-selection-controls-verbose.md)
- [064Q — Selection Controls (quiz)](064-selection-controls-quiz.md)
