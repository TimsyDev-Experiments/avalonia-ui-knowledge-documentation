---
tier: intermediate
topic: data display
estimated: 15 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 043 -- TreeView with Hierarchical Data

**What you'll learn:** Bind hierarchical data to a TreeView using TreeDataTemplate, handle lazy loading on expand, search and filter, and style tree nodes.

**Prerequisites:** [009 -- Data Templates Basics](../basics/009-data-templates-basics.md)

---

## 1. Basic hierarchical binding

```csharp
public class TreeNode
{
    public string Name { get; set; } = "";
    public ObservableCollection<TreeNode> Children { get; set; } = new();
}
```

```xml
<TreeView ItemsSource="{Binding RootNodes}">
  <TreeView.ItemTemplate>
    <TreeDataTemplate ItemsSource="{Binding Children}">
      <TextBlock Text="{Binding Name}" />
    </TreeDataTemplate>
  </TreeView.ItemTemplate>
</TreeView>
```

`TreeDataTemplate` is the key difference from `DataTemplate` — its `ItemsSource` tells the TreeView where to find child items. The same template is applied recursively at each level.

## 2. Multiple node types

```xml
<TreeView ItemsSource="{Binding Items}">
  <TreeView.DataTemplates>
    <TreeDataTemplate DataType="models:FolderNode"
                      ItemsSource="{Binding Children}">
      <StackPanel Orientation="Horizontal" Spacing="4">
        <PathIcon Data="{StaticResource FolderIcon}" />
        <TextBlock Text="{Binding Name}" />
      </StackPanel>
    </TreeDataTemplate>
    <DataTemplate DataType="models:FileNode">
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

Leaf nodes use `DataTemplate` (no `ItemsSource`). Branching nodes use `TreeDataTemplate`. Avalonia matches templates by type, including interfaces and derived types.

## 3. Selection

```xml
<!-- Single selection -->
<TreeView ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedNode}" />

<!-- Multiple selection -->
<TreeView ItemsSource="{Binding Items}"
          SelectionMode="Multiple"
          SelectedItems="{Binding SelectedNodes}" />
```

```csharp
[ObservableProperty]
private object? _selectedNode;

partial void OnSelectedNodeChanged(object? value)
{
    if (value is FolderNode folder)
        LoadContents(folder);
}
```

## 4. Lazy loading (load on expand)

Start with a placeholder child so the expand arrow appears, then load real children when expanded:

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

Bind `IsExpanded` on the TreeViewItem container:

```xml
<TreeView ItemsSource="{Binding Roots}">
  <TreeView.Styles>
    <Style Selector="TreeViewItem">
      <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}" />
    </Style>
  </TreeView.Styles>
  <TreeView.ItemTemplate>
    <TreeDataTemplate ItemsSource="{Binding Children}">
      <TextBlock Text="{Binding Name}" />
    </TreeDataTemplate>
  </TreeView.ItemTemplate>
</TreeView>
```

## 5. Async lazy loading

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

The `await` returns to the UI thread automatically, so updating `Children` is safe.

## 6. Programmatic expand / collapse

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

## 7. Search and filter

Rebuild the visible tree from source when the search text changes:

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

## 8. Styling TreeViewItem

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

## 9. Expansion events

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

## 10. Virtualization

TreeView virtualizes items by default. For large trees:

```xml
<TreeView ItemsSource="{Binding HugeData}"
          VirtualizingPanel.VirtualizationMode="Recycling" />
```

## Key takeaways

- Use `TreeDataTemplate` for nodes with children, `DataTemplate` for leaf nodes
- Multiple node types use `DataType` matching; order matters (specific first)
- Lazy loading places a placeholder child, then replaces on `IsExpanded` change
- Bind `IsExpanded` on `TreeViewItem` via style selector for the expand/collapse pattern
- Search/filter rebuilds the visible tree by filtering recursively
- Style `TreeViewItem` parts via `#PART_ExpandCollapseChevron` and `#PART_HeaderPresenter`
- Virtualization is on by default; enable recycling for very large trees

---

## See Also

- [043V — TreeView with Hierarchical Data (verbose companion)](043-treeview-hierarchical-data-verbose.md)
- [043E — TreeView with Hierarchical Data (examples)](043-treeview-hierarchical-data-examples.md)
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)
- [036 — Virtualization and Large List Performance](../advanced/036-virtualization-large-lists.md)
- [Avalonia Docs: TreeView](https://docs.avaloniaui.net/controls/treeview)
