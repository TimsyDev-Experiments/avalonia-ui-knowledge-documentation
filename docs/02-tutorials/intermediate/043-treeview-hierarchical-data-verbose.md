---
tier: intermediate
topic: data display
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 043-treeview-hierarchical-data.md
---

# 043V — TreeView with Hierarchical Data: An In-Depth Companion

**What you'll learn in this companion:** How TreeView's recursive item generation works, why TreeDataTemplate exists as a separate type, how the expand/collapse system interacts with virtualization, and how to handle large hierarchical datasets without performance degradation.

**Prerequisites:** [009 — Data Templates Basics](../basics/009-data-templates-basics.md), [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)

**You should already have read:** [043 — TreeView with Hierarchical Data](043-treeview-hierarchical-data.md) for the quick-start version. This file goes deeper on every section.

---

## 1. What TreeView Actually Does

`TreeView` is an `ItemsControl` that renders its items as an indented, expandable/collapsible hierarchy. Each item is wrapped in a `TreeViewItem` container that provides:

- **Indentation** based on depth level
- **Expand/collapse toggle** (chevron arrow in the default template)
- **Selection** (single or multiple)
- **Virtualization** (built-in, unlike WPF TreeView)

The critical difference from a flat `ListBox` is that each `TreeViewItem` can contain another `ItemsControl` (also a `TreeViewItem` internally) that renders child items. This recursive structure is what makes TreeView work.

### How TreeDataTemplate enables recursion

```xml
<TreeView ItemsSource="{Binding RootNodes}">
  <TreeView.ItemTemplate>
    <TreeDataTemplate ItemsSource="{Binding Children}">
      <TextBlock Text="{Binding Name}" />
    </TreeDataTemplate>
  </TreeView.ItemTemplate>
</TreeView>
```

`TreeDataTemplate` extends `DataTemplate` by adding an `ItemsSource` property. When Avalonia's item generator creates a container for a data item using a `TreeDataTemplate`, it:

1. Creates a `TreeViewItem` as the container
2. Sets the `TreeViewItem.Header` to the control tree from the template's content (the `TextBlock`)
3. Binds `TreeViewItem.ItemsSource` to the `ItemsSource` binding path (`Children`)
4. For each child item in `Children`, repeats the process using the same `TreeDataTemplate`

This recursive application is the key: **the same template is applied at every level**. You get infinite nesting for free, as long as every node type has a `Children` collection.

---

## 2. TreeDataTemplate vs DataTemplate

A regular `DataTemplate` produces a control tree and stops. A `TreeDataTemplate` produces a control tree **and** tells the TreeView how to find child items.

| | `DataTemplate` | `TreeDataTemplate` |
|---|---|---|
| Used for | Leaf nodes | Branch nodes (nodes with children) |
| ItemsSource binding | Not available | Available — tells TreeView where children are |
| Container generated | `ContentPresenter` or `ListBoxItem` | `TreeViewItem` |
| Recursion | No | Yes — applied recursively to children |

You can mix both in `TreeView.DataTemplates`:

```xml
<TreeView ItemsSource="{Binding Items}">
  <TreeView.DataTemplates>
    <TreeDataTemplate DataType="models:FolderNode"
                      x:DataType="models:FolderNode"
                      ItemsSource="{Binding Children}">
      <StackPanel Orientation="Horizontal" Spacing="4">
        <PathIcon Data="{StaticResource FolderIcon}" />
        <TextBlock Text="{Binding Name}" />
      </StackPanel>
    </TreeDataTemplate>
    <DataTemplate DataType="models:FileNode"
                  x:DataType="models:FileNode">
      <StackPanel Orientation="Horizontal" Spacing="4">
        <PathIcon Data="{StaticResource FileIcon}" />
        <TextBlock Text="{Binding Name}" />
        <TextBlock Text="{Binding Size, StringFormat='{}{0:N0} KB'}"
                   Foreground="Gray" />
      </StackPanel>
    </DataTemplate>
  </TreeView.DataTemplates>
</TreeView>
```

In this example:
- `FolderNode` items get a `TreeDataTemplate` — they produce expandable `TreeViewItem` containers with children
- `FileNode` items get a `DataTemplate` — they produce leaf `TreeViewItem` containers with no expand arrow

If a `FolderNode` has no children, its `Children` collection is empty and the expand arrow is hidden (via the `:empty` pseudo-class on the toggle button).

**Important:** Template order matters here, just like with `DataTemplates` in general (see [009V — Data Templates Companion](../basics/009-data-templates-basics-verbose.md) for details). If a `FolderNode` also matches a `TreeDataTemplate` for a base class or interface declared before it, that template wins. Place more specific types first.

---

## 3. Selection: Single vs Multiple

```xml
<!-- Single selection -->
<TreeView ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedNode}" />

<!-- Multiple selection -->
<TreeView ItemsSource="{Binding Items}"
          SelectionMode="Multiple"
          SelectedItems="{Binding SelectedNodes}" />
```

### How SelectedItem binding works

`SelectedItem` is a direct property on `TreeView`. When the user clicks a `TreeViewItem` (or navigates to it with arrow keys), the TreeView sets `SelectedItem` to the item's data context. The binding is `TwoWay` by default — changing `SelectedNode` in the viewmodel programmatically also updates the TreeView selection.

### Multi-select behavior

With `SelectionMode="Multiple"`, users can:
- Click to select a single item (deselects others)
- Ctrl+Click to toggle selection of an item without affecting others
- Shift+Click to select a range (applies to flat item order, not visual tree order)

`SelectedItems` binds to an `IList` on the viewmodel. Changes to the list from the viewmodel side (adding/removing items) update the TreeView selection. The default `SelectedItems` binding mode is `OneWayToSource` — the TreeView writes to the list but does not read from it. For full two-way sync, you may need to handle `SelectionChanged`.

### Selection changed reaction

```csharp
[ObservableProperty]
private object? _selectedNode;

partial void OnSelectedNodeChanged(object? value)
{
    if (value is FolderNode folder)
        LoadContents(folder);
}
```

The `partial void On<PropertyName>Changed` method is generated by the `[ObservableProperty]` source generator. It fires every time `SelectedNode` changes, whether from UI interaction or programmatic assignment.

---

## 4. Lazy Loading: How the Placeholder Pattern Works

```csharp
public partial class LazyNode : ObservableObject
{
    private bool _loaded;

    public string Name { get; }
    public string Path { get; }

    public ObservableCollection<LazyNode> Children { get; } = new();

    public LazyNode(string name, string path, bool hasChildren = true)
    {
        Name = name;
        Path = path;
        if (hasChildren)
            Children.Add(new LazyNode("Loading...", "", false));
    }

    [ObservableProperty]
    private bool _isExpanded;

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && !_loaded)
        {
            _loaded = true;
            LoadChildren();
        }
    }

    private void LoadChildren()
    {
        Children.Clear();
        foreach (var dir in Directory.GetDirectories(Path))
            Children.Add(new LazyNode(Path.GetFileName(dir), dir));
    }
}
```

### Why the placeholder child exists

The `TreeViewItem` expand chevron is visible only when the item has children (`HasItems == true`). `HasItems` is determined by whether `ItemsSource` (or `Items`) has at least one item. If `Children` is empty, the chevron is hidden and the user cannot expand the node.

To show the chevron for a node that has children but hasn't loaded them yet, you add a **placeholder** item to `Children` during construction. This single placeholder item makes `HasItems = true`, which makes the chevron visible. When the user expands the node:

1. `IsExpanded` is set to `true`
2. `OnIsExpandedChanged` fires
3. `_loaded` is checked — if this is the first expansion, `LoadChildren()` runs
4. `LoadChildren()` clears the placeholder and adds real children
5. The UI updates to show the real children

### The IsExpanded binding

```xml
<TreeView ItemsSource="{Binding Roots}">
  <TreeView.Styles>
    <Style Selector="TreeViewItem">
      <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
    </Style>
  </TreeView.Styles>
</TreeView>
```

`IsExpanded` is a property on `TreeViewItem`, not on the data item. To connect the TreeViewItem's expansion state to your viewmodel, you use a style setter that binds `TreeViewItem.IsExpanded` to a property on the data item's `DataContext`.

Why a style setter and not a binding in the template? Because `IsExpanded` lives on the container (`TreeViewItem`), not on the content. The template only controls the content area. A style setter on `TreeViewItem` is the standard way to reach container properties.

**Binding direction matters:** The binding must be `TwoWay` because:
- The viewmodel writes to `IsExpanded` to programmatically expand/collapse (viewmodel → UI)
- The user clicks the chevron to expand/collapse (UI → viewmodel)

---

## 5. Async Lazy Loading

```csharp
partial void OnIsExpandedChanged(bool value)
{
    if (value && !_loaded)
    {
        _loaded = true;
        _ = LoadChildrenAsync();
    }
}

private async Task LoadChildrenAsync()
{
    var items = await _service.GetChildrenAsync(Id);
    Children.Clear();
    foreach (var item in items)
        Children.Add(new LazyNode(item));
}
```

### Why fire-and-forget is acceptable here

The `_ = LoadChildrenAsync()` pattern discards the returned `Task`. This is fine because:
- The method's only purpose is to populate `Children`
- Errors should be handled inside the method (wrap in try/catch)
- The UI thread is the context — `await` automatically returns to the calling synchronization context, which is the UI dispatcher
- There is no caller that needs to await completion

**Error handling requirement:** Without a try/catch, an exception in `LoadChildrenAsync` will be an unobserved Task exception. Always wrap the body:

```csharp
private async Task LoadChildrenAsync()
{
    try
    {
        var items = await _service.GetChildrenAsync(Id);
        Children.Clear();
        foreach (var item in items)
            Children.Add(new LazyNode(item));
    }
    catch (Exception ex)
    {
        // Log, show error indicator, or re-add placeholder with error message
        Children.Clear();
        Children.Add(new LazyNode($"Error: {ex.Message}", "", false));
    }
}
```

### UI thread safety

`ObservableCollection<T>` modifications must happen on the UI thread. The `await` in `LoadChildrenAsync` captures the current `SynchronizationContext` (UI dispatcher) before the await, and resumes on it after. This is the default behavior in Avalonia — no explicit `Dispatcher.UIThread.Post` or `InvokeAsync` needed.

---

## 6. Programmatic Expand/Collapse

```csharp
private void ExpandAll(IEnumerable<LazyNode> nodes)
{
    foreach (var node in nodes)
    {
        node.IsExpanded = true;
        ExpandAll(node.Children);
    }
}

private void CollapseAll(IEnumerable<LazyNode> nodes)
{
    foreach (var node in nodes)
    {
        node.IsExpanded = false;
        CollapseAll(node.Children);
    }
}
```

### How this works

This sets `IsExpanded` on each node's viewmodel property. The style binding (`TreeViewItem.IsExpanded="{Binding IsExpanded}"`) propagates the change to the UI:

1. `IsExpanded = true` on the viewmodel
2. The style binding updates `TreeViewItem.IsExpanded`
3. The `TreeViewItem` template shows the children
4. For each child, if `IsExpanded` is also true, its children appear
5. Recursion continues through the tree

### Performance concern

Calling `ExpandAll` on a tree with thousands of nodes sets `IsExpanded` on every node synchronously. Each property change triggers the `OnIsExpandedChanged` partial method, which may trigger lazy loading. For large trees, consider:
- Expanding only to a certain depth (e.g., `ExpandToDepth(nodes, 3)`)
- Using a `CancellationToken` in lazy loading to abort expansion
- Batch loading children before setting `IsExpanded`

---

## 7. Search and Filter: Recursive Tree Rebuild

```csharp
[ObservableProperty]
private string _searchText = "";

partial void OnSearchTextChanged(string value)
{
    FilteredItems.Clear();
    foreach (var root in _allItems)
    {
        var filtered = FilterNode(root, value);
        if (filtered is not null)
            FilteredItems.Add(filtered);
    }
}

private TreeNode? FilterNode(TreeNode node, string search)
{
    var matches = node.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
    var filteredChildren = node.Children
        .Select(c => FilterNode(c, search))
        .Where(c => c is not null)
        .ToList();

    if (matches || filteredChildren.Count > 0)
    {
        return new TreeNode
        {
            Name = node.Name,
            Children = new ObservableCollection<TreeNode>(filteredChildren!)
        };
    }
    return null;
}
```

### Why this rebuilds the tree instead of hiding nodes

`TreeView` does not have a built-in filter mechanism like `ListBox` (which can use `CollectionView.Filter`). To filter a tree, you must provide a filtered copy of the source tree. The `FilterNode` method:

1. Checks if the current node's name matches the search text
2. Recursively filters children
3. If the node matches OR any descendant matches, includes the node (with only the matching descendants)
4. If neither the node nor any descendant matches, returns null (excluded)

### Why new TreeNode instances

The method creates new `TreeNode` objects rather than modifying the originals. This is important because:
- The original tree remains unchanged (undo search by rebinding to `_allItems`)
- Removed nodes are naturally garbage-collected
- No side effects on shared references

### Performance for large trees

This filter rebuilds the entire filtered tree on every keystroke. For trees with thousands of nodes, debounce the search:

```csharp
private CancellationTokenSource? _searchCts;

partial void OnSearchTextChanged(string value)
{
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();
    var token = _searchCts.Token;

    _ = Task.Run(async () =>
    {
        await Task.Delay(300, token); // debounce 300ms
        if (token.IsCancellationRequested) return;

        var filtered = BuildFilteredTree(value);
        if (token.IsCancellationRequested) return;

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            FilteredItems.Clear();
            foreach (var item in filtered)
                FilteredItems.Add(item);
        });
    }, token);
}
```

---

## 8. Styling TreeViewItem Parts

```xml
<TreeView.Styles>
  <Style Selector="TreeViewItem:empty /template/ ToggleButton#PART_ExpandCollapseChevron">
    <Setter Property="IsVisible" Value="False" />
  </Style>

  <Style Selector="TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter">
    <Setter Property="Background" Value="{DynamicResource SystemAccentColor}" />
    <Setter Property="Foreground" Value="White" />
  </Style>

  <Style Selector="TreeViewItem">
    <Setter Property="Padding" Value="4,2" />
  </Style>
</TreeView.Styles>
```

### The template parts

`TreeViewItem`'s default control template has three key named parts:

| Part name | Type | Purpose |
|-----------|------|---------|
| `PART_ExpandCollapseChevron` | `ToggleButton` | The expand/collapse arrow |
| `PART_HeaderPresenter` | `ContentPresenter` | Displays the item's content (from template) |
| `PART_IndentPresenter` | `ContentPresenter` | Renders indentation guides/space |

### Why /template/ selector is needed

You cannot style `PART_ExpandCollapseChevron` directly because it is inside the control template. The `/template/` selector traverses into the template to reach it. Without it, the selector `TreeViewItem ToggleButton` would match any `ToggleButton` that is a descendant of `TreeViewItem`, not just the specific named part.

### The :empty pseudo-class

`:empty` applies when `TreeViewItem.HasItems` is `false` (no child items). When the node has no children, hiding the chevron removes the empty space where it would have been. This is the visual difference between a leaf node and a branch node.

### Selected item highlight

The `:selected` pseudo-class on `TreeViewItem` is set by the TreeView when the item is part of the current selection. Styling `PART_HeaderPresenter` on `:selected` gives the selected item a highlighted background that extends across the full width of the TreeView.

---

## 9. Expansion Events: Routed Event Pattern

```csharp
treeView.AddHandler(TreeViewItem.ExpandedEvent, (_, args) =>
{
    var item = (TreeViewItem)args.Source!;
    // Log or track expanded nodes
});

treeView.AddHandler(TreeViewItem.CollapsedEvent, (_, args) =>
{
    var item = (TreeViewItem)args.Source!;
});
```

### Why AddHandler instead of event subscription

`ExpandedEvent` and `CollapsedEvent` are **routed events**. They bubble up from the `TreeViewItem` where the expand/collapse happened to the `TreeView` ancestor. `AddHandler` on the `TreeView` catches events from any descendant `TreeViewItem`.

If you subscribed to the event on individual `TreeViewItem` instances, you would need to subscribe to each new item as it's created (or use `ItemContainerGenerator`). `AddHandler` avoids this by listening at the parent level.

### args.Source vs args.OriginalSource

- `args.Source` — the `TreeViewItem` that raised the event (the item that was expanded/collapsed)
- `args.OriginalSource` — in this case, same as `Source`

Use `args.Source` to access the specific item's `DataContext` to update the viewmodel or log the action.

---

## 10. Virtualization in TreeView

```xml
<TreeView ItemsSource="{Binding HugeData}"
          VirtualizingPanel.VirtualizationMode="Recycling" />
```

### How TreeView virtualization is different

Unlike `ListBox` virtualization (which only virtualizes flat items), `TreeView` must virtualize while maintaining the hierarchy. When a parent node is collapsed, its entire subtree is not rendered — the `TreeViewItem` containers for all descendants are released or recycled.

### Standard vs Recycling mode

| Mode | Behavior |
|------|----------|
| `Standard` (default) | Creates new `TreeViewItem` for each visible node. Destroys when scrolled out of view. |
| `Recycling` | Reuses `TreeViewItem` containers. When a node scrolls out of view, its container is recycled for an incoming node. |

Recycling is especially important for deep trees because each `TreeViewItem` carries the expand/collapse toggle, indentation logic, selection state, and its own `ItemsControl` for children. Creating and destroying these containers during scroll causes GC pressure and visible lag.

### When virtualization breaks

TreeView virtualization only works within a constrained height. If the TreeView is in an auto-sizing container (no `MaxHeight`, no `Height`, not in a `Grid` row with `*` height), virtualization is skipped because every item is "visible" (the panel measures all items).

To ensure virtualization:
```xml
<Grid>
  <Grid.RowDefinitions>
    <RowDefinition Height="*" />
  </Grid.RowDefinitions>
  <TreeView ... /> <!-- Grid row constrains height, virtualization activates -->
</Grid>
```

### Indentation and virtualization

TreeView manages indentation by reading the `TreeViewItem.Level` property, which tracks depth in the hierarchy. During virtualization, the level is preserved because it is set when the container is created, not when it is realized. Recycled containers have their `Level` reset to the new item's level.

---

## 11. Drag and Drop in TreeView

TreeView supports drag and drop for reordering nodes or moving nodes between parents. This is a common pattern for outline/managing tools.

```xml
<TreeView AllowDrop="True"
          DragDrop.DragSource="True">
```

Handling drag/drop in a TreeView requires careful management of the expanded/collapsed state, drop position (above, below, or as child), and visual feedback for where the item will land. See [019 — Drag and Drop](019-drag-drop.md) for the full drag/drop API.

---

## 12. Common TreeView Mistakes

### Mistake 1: Binding ItemsSource on TreeDataTemplate to the wrong collection

```xml
<TreeDataTemplate ItemsSource="{Binding Children}">
  <TextBlock Text="{Binding Name}" />
</TreeDataTemplate>
```

`ItemsSource` on `TreeDataTemplate` is a **binding path**, not a data source. It must point to a property on the item's DataContext that returns the children collection. If `Children` is null or missing, the TreeView reports the node as empty and no chevron appears.

### Mistake 2: Forgetting the placeholder for lazy loading

If `Children` is empty when the node is created, the chevron never appears. The user cannot expand the node to trigger loading. Always add a placeholder item to make `HasItems = true`.

### Mistake 3: Binding IsExpanded without TwoWay

```xml
<Setter Property="IsExpanded" Value="{Binding IsExpanded}" /> <!-- OneWay by default -->
```

Without `Mode=TwoWay`, clicking the chevron does not update the viewmodel's `IsExpanded`. The viewmodel remains out of sync.

### Mistake 4: Non-observable children collection

```csharp
public List<TreeNode> Children { get; set; } = new();
```

If `Children` is not `ObservableCollection<T>` (or at least `INotifyCollectionChanged`), adding or removing children after the initial binding does not update the TreeView. Use `ObservableCollection<TreeNode>`.

### Mistake 5: Mixing TreeDataTemplate and DataTemplate for leaf nodes

If you use `TreeDataTemplate` for a type that never has children (with an empty `Children` collection), the TreeView still creates the expand chevron (but hides it via `:empty`). This is slightly wasteful. Use `DataTemplate` for types that are always leaves.

---

## Key Takeaways

- `TreeDataTemplate` enables recursive template application by adding `ItemsSource` — it is the core of TreeView's hierarchical rendering
- Mix `TreeDataTemplate` (branch nodes) and `DataTemplate` (leaf nodes) in `TreeView.DataTemplates`, ordered by specificity
- Lazy loading uses a placeholder child to force the expand chevron visible, then replaces children on first expansion
- Bind `TreeViewItem.IsExpanded` via style setter with `Mode=TwoWay` for programmatic expand/collapse
- Search/filter rebuilds the visible tree from scratch — use debouncing and CancellationToken for large trees
- Style template parts via `/template/` selectors and pseudo-classes (`:empty`, `:selected`, `:pointerover`)
- Expansion events are routed — use `AddHandler` on the TreeView to catch events from any descendant TreeViewItem
- Virtualization requires a constrained height and benefits from `Recycling` mode for large trees
- `Children` must be `ObservableCollection<T>` for add/remove operations to update the UI

---

## See Also

- [043 — TreeView with Hierarchical Data (original quick-start)](043-treeview-hierarchical-data.md)
- [043E — TreeView with Hierarchical Data (examples)](043-treeview-hierarchical-data-examples.md)
- [009 — Data Templates Basics (verbose companion)](../basics/009-data-templates-basics-verbose.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)
- [036 — Virtualization and Large List Performance](../advanced/036-virtualization-large-lists.md)
- [019 — Drag and Drop](019-drag-drop.md)
- [Avalonia Docs: TreeView](https://docs.avaloniaui.net/controls/treeview)
- [Avalonia Docs: TreeDataTemplate](https://docs.avaloniaui.net/controls/treeview/how-to-use-treeview-with-hierarchical-data)
