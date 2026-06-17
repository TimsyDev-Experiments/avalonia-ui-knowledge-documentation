---
tier: intermediate
topic: item lists
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 015-item-lists.md
---

# Quiz — Item Lists (ListBox, ItemsRepeater, DataGrid)

```quiz
Q: Which control should you choose for a high-performance virtualized list with a custom layout (e.g., horizontal wrapping)?
A. ListBox || Incorrect — ListBox uses a Stack layout and does not support custom layouts.
B. DataGrid || Incorrect — DataGrid is optimized for tabular data with columns, not custom layouts.
C. ItemsRepeater (correct) || Correct. ItemsRepeater supports pluggable layouts (StackLayout, UniformGridLayout, custom ILayout) with virtualization.
D. ItemsControl || Incorrect — ItemsControl does not support virtualization.
Explanation: ItemsRepeater with a virtualizing layout (StackLayout or UniformGridLayout) provides high-performance, flexible layouts with virtualization.
```

```quiz
Q: What is the correct binding syntax for a Delete button inside a DataGridTemplateColumn to reach the parent DataGrid's DataContext?
A. {Binding DataContext.DeleteItemCommand} || Incorrect — this would bind to the current row's DataContext (the item), not the DataGrid's.
B. {Binding $parent[DataGrid].DataContext.DeleteItemCommand} (correct) || Correct. $parent[DataGrid] walks the visual tree up to the DataGrid to access its ViewModel DataContext.
C. {Binding RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type DataGrid}}, Path=DataContext.DeleteItemCommand} || Incorrect — Avalonia does not support this WPF-style RelativeSource syntax.
D. {Binding DeleteItemCommand} || Incorrect — this would look for DeleteItemCommand on the item model, not the parent ViewModel.
Explanation: In Avalonia, $parent[Type] is the compiled-binding-safe way to walk up the visual tree to access a parent control and its DataContext.
```

```quiz
Q: A developer needs a selectable list where users can pick multiple items. Which control and configuration is correct?
A. ListBox with SelectionMode="Multiple" (correct) || Correct. ListBox supports Single, Multiple, and Toggle selection modes.
B. ItemsRepeater with SelectionMode="Multiple" || Incorrect — ItemsRepeater does not have a SelectionMode property; it is a layout-only control.
C. DataGrid with SelectionMode="Multiple" || Incorrect — DataGrid supports row selection but is for tabular data, not general item selection.
D. ItemsControl with SelectionMode="Multiple" || Incorrect — ItemsControl has no selection support.
Explanation: ListBox is the go-to control for selectable item lists. SelectionMode="Multiple" allows selecting multiple items via Ctrl+click.
```

```quiz
Q: How does the UI automatically update when an item is added to an ObservableCollection<T>?
A. The ViewModel must call PropertyChanged for the Items property || Incorrect — ObservableCollection raises CollectionChanged automatically on add/remove/replace.
B. The control polls the collection on a timer || Incorrect — no polling is involved; updates are event-driven.
C. ObservableCollection raises CollectionChanged, and the control subscribes to it (correct) || Correct. ObservableCollection implements INotifyCollectionChanged, and item controls subscribe to this event to update the UI.
D. The developer must manually call Items.Refresh() || Incorrect — Refresh() is not needed; changes are automatically reflected.
Explanation: ObservableCollection<T> automatically raises the CollectionChanged event when items are added, removed, moved, or replaced, and Avalonia item controls listen to this event to stay in sync.
```

```quiz
Q: What is the recommended virtualization and layout behavior of each control? Match the control to its correct characteristic.
A. ListBox — virtualized, Stack layout (correct) || Correct. ListBox uses virtualizing stack panel by default.
B. ItemsControl — virtualized, flexible layout || Incorrect — ItemsControl does not virtualize and uses a simple stack.
C. ItemsRepeater — not virtualized, custom layout || Incorrect — ItemsRepeater supports virtualization with virtualizing layouts.
D. DataGrid — not virtualized, grid layout || Incorrect — DataGrid virtualizes rows.
Explanation: ListBox virtualizes with StackLayout; ItemsRepeater virtualizes with flexible layout; DataGrid virtualizes rows; ItemsControl does not virtualize at all.
```
