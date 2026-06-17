---
tier: intermediate
topic: data display
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 043-treeview-hierarchical-data.md
---

# Quiz — TreeView with Hierarchical Data

```quiz
Q: What is the key difference between TreeDataTemplate and DataTemplate in a TreeView?
A. TreeDataTemplate uses a different base class and does not support compiled bindings || Incorrect — both templates support compiled bindings with x:DataType.
B. TreeDataTemplate has an ItemsSource property that tells the TreeView where to find child items for recursive rendering (correct) || Correct — ItemsSource on TreeDataTemplate defines the child collection; DataTemplate has no ItemsSource and is used for leaf nodes.
C. TreeDataTemplate can only be used with a single node type || Incorrect — TreeDataTemplate supports multiple node types via DataType matching, just like DataTemplate.
D. TreeDataTemplate requires IsExpanded binding to function || Incorrect — IsExpanded binding is optional; TreeDataTemplate works without it, though expand/collapse still toggles.
Explanation: The ItemsSource property on TreeDataTemplate enables recursive template application for nested child items; DataTemplate lacks this property and renders a flat item.
```

```quiz
Q: How does the lazy-loading pattern ensure the expand arrow appears before children are fetched?
A. Set TreeViewItem.IsExpanded to true in the style || Incorrect — IsExpanded controls the expanded state, not whether the expand arrow is visible.
B. Add a placeholder child to the Children collection so the TreeView renders a chevron (correct) || Correct — the TreeView shows an expand arrow when a node has at least one child; a placeholder "Loading..." item triggers the arrow before real data arrives.
C. Set a special LazyLoadingMode property on TreeDataTemplate || Incorrect — there is no LazyLoadingMode property on TreeDataTemplate.
D. Override the TreeViewItem template to always show the chevron || Incorrect — forcing the chevron to always show would display an expand arrow even for empty leaf nodes.
Explanation: The TreeView only renders the expand chevron when Children.Count > 0; a placeholder child triggers the arrow, and the real children replace the placeholder on first expand.
```

```quiz
Q: How is the TreeViewItem.IsExpanded property bound to a ViewModel property for expand/collapse tracking?
A. Directly on the TreeDataTemplate's Bindings collection || Incorrect — TreeDataTemplate does not expose an IsExpanded binding.
B. Via a Style selector targeting TreeViewItem with a Setter for IsExpanded (correct) || Correct — a Style with Selector="TreeViewItem" and a Setter binding IsExpanded to the ViewModel property enables TwoWay expand/collapse synchronization.
C. Through the TreeView.Resources collection with a DataTrigger || Incorrect — DataTrigger does not exist in Avalonia; styles with selectors are the mechanism.
D. By handling the ExpandedEvent and setting the property in code-behind || Incorrect — while this works, it is not the idiomatic pattern; a style binding is cleaner and maintains MVVM separation.
Explanation: A Style setter on TreeViewItem binds IsExpanded to the node's ViewModel property, keeping expand state in the ViewModel layer.
```

```quiz
Q: Identify the bug in this TreeView node filter logic:
    private TreeNode? FilterNode(TreeNode node, string search)
    {
        var matches = node.Name.Contains(search);
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
A. The filteredChildren list is rebuilt as a new list but the original Children references are lost || Incorrect — the code creates new TreeNode instances; losing references to the originals is expected for a filtered view.
B. StringComparison.OrdinalIgnoreCase is not specified for the Contains call, making the search case-sensitive (correct) || Correct — without StringComparison.OrdinalIgnoreCase, Contains uses the current culture's default comparison, which is case-sensitive by default; this can cause unexpected filtering results.
C. The null-forgiving operator on filteredChildren! may throw if filteredChildren is empty || Incorrect — null-forgiving (!) is a compile-time hint; filteredChildren is a non-null List<TreeNode?> and ObservableCollection will accept it even if empty.
D. The method returns null instead of an empty TreeNode || Incorrect — returning null for non-matching nodes is correct; nulls are filtered out by the Where clause in the caller.
Explanation: When performing case-insensitive text search, StringComparison.OrdinalIgnoreCase should be passed to Contains to avoid unexpected case-sensitive matching.
```

```quiz
Q: How does TreeView handle rendering different node types (e.g., folders and files) in the same tree?
A. By using a single TreeDataTemplate with a converter that checks the type || Incorrect — while a converter could work, the idiomatic approach uses multiple templates with DataType matching.
B. By defining multiple TreeDataTemplate and DataTemplate entries in TreeView.DataTemplates with DataType attributes (correct) || Correct — TreeView.DataTemplates can contain multiple templates; branching nodes use TreeDataTemplate with ItemsSource, leaf nodes use DataTemplate, and Avalonia matches by DataType.
C. By handling the TreeView.ItemTemplateSelector event || Incorrect — there is no ItemTemplateSelector event; Avalonia uses DataType matching on templates in the DataTemplates collection.
D. By setting different ItemTemplate properties based on the node depth || Incorrect — TreeView does not have depth-based template selection built in.
Explanation: Avalonia's template selection matches by DataType, allowing different templates (TreeDataTemplate for folders, DataTemplate for files) in the same TreeView.
```
