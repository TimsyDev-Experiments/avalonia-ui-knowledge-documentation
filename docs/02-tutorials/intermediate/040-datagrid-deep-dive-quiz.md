---
tier: intermediate
topic: data display
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 040-datagrid-deep-dive.md
---

# Quiz — DataGrid Deep Dive

```quiz
Q: How can you prevent a specific column from appearing when AutoGenerateColumns is enabled?
A. Set the column's Visibility to Collapsed in XAML || Incorrect — auto-generated columns are not defined in XAML, so there is no element to set visibility on.
B. Handle the AutoGeneratingColumn event and set e.Cancel = true for the unwanted property (correct) || Correct — the AutoGeneratingColumn event fires per property; setting e.Cancel = true prevents that column from being generated.
C. Remove the property from the ViewModel || Incorrect — the ViewModel should model the data; removing properties to control display is poor practice and breaks other bindings.
D. Set AutoGenerateColumns="False" and manually define all columns except the unwanted one || Incorrect — while this works, it is overkill when the AutoGeneratingColumn event handles the same need more concisely.
Explanation: The AutoGeneratingColumn event provides per-property control: check e.PropertyName and set e.Cancel = true to exclude it.
```

```quiz
Q: Which key commits the current cell edit and moves the selection down by one row in DataGrid?
A. Tab || Incorrect — Tab commits and moves right, not down.
B. F2 || Incorrect — F2 enters edit mode; it does not commit.
C. Enter (correct) || Correct — Enter commits the current edit and moves the selection to the cell below.
D. Escape || Incorrect — Escape cancels the edit without committing.
Explanation: Enter commits the edit and advances to the next row; Tab advances to the next column.
```

```quiz
Q: What class enables DataGrid grouping with GroupDescriptions?
A. CollectionViewSource || Incorrect — CollectionViewSource is a WPF type; Avalonia's DataGrid uses its own grouping API.
B. DataGridGroupDescription || Incorrect — there is no standalone DataGridGroupDescription class; the grouping is configured on a collection view.
C. DataGridCollectionView (correct) || Correct — DataGridCollectionView wraps a source list and exposes GroupDescriptions where you add DataGridPathGroupDescription instances for grouping.
D. GroupedObservableCollection || Incorrect — no such type exists in Avalonia's DataGrid namespace.
Explanation: DataGridCollectionView provides the GroupDescriptions collection for defining grouping criteria on the DataGrid's ItemsSource.
```

```quiz
Q: Identify the bug in this DataGrid column definition:
    <DataGrid ItemsSource="{Binding Items}" x:DataType="vm:MainViewModel">
      <DataGrid.Columns>
        <DataGridTextColumn Header="Name"
                            Binding="{CompiledBinding Name, Mode=TwoWay}" />
        <DataGridTextColumn Header="Score"
                            Binding="{CompiledBinding Score}" IsReadOnly="False" />
      </DataGrid.Columns>
    </DataGrid>
A. The x:DataType should be on DataGridTextColumn, not DataGrid || Incorrect — x:DataType on the DataGrid is correct for compiled bindings in column bindings at this scope.
B. IsReadOnly="False" on a column without TwoWay binding will still prevent editing || Incorrect — IsReadOnly="False" allows editing, but without Mode=TwoWay the edited value will not propagate back to the source.
C. The Score column has IsReadOnly="False" but the binding is one-way (no Mode=TwoWay) (correct) || Correct — IsReadOnly="False" permits cell editing, but a one-way compiled binding will not write the edited value back to the ViewModel property.
D. The DataGrid should use AutoGenerateColumns="True" when defining Columns manually || Incorrect — AutoGenerateColumns must be False (default is already False) when defining columns manually; the bug is about binding direction.
Explanation: For inline editing to persist changes, each editable column binding must specify Mode=TwoWay. A one-way binding discards user edits on commit.
```

```quiz
Q: What effect does FrozenColumnCount="2" have on a DataGrid?
A. The first two columns remain visible when scrolling horizontally (correct) || Correct — frozen columns are pinned to the left edge and do not scroll out of view.
B. The first two rows are pinned at the top during vertical scroll || Incorrect — FrozenColumnCount affects columns, not rows.
C. The first two columns are hidden from view unless the user taps to reveal them || Incorrect — frozen columns are always visible; they are not hidden.
D. The DataGrid limits the total columns to two || Incorrect — FrozenColumnCount does not limit the number of columns; columns beyond the frozen count scroll normally.
Explanation: FrozenColumnCount pins the leftmost N columns so they stay visible while the remaining columns scroll horizontally.
```
