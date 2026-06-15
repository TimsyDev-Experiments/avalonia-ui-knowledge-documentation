---
tier: intermediate
topic: data display
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 043-treeview-hierarchical-data.md
---

# 043E — TreeView with Hierarchical Data: Real-World Examples

**What you'll learn:** Two complete scenarios — a project file explorer with lazy loading and search, and an organizational chart with drag support and expand-to-depth.

**Prerequisites:** [043 — TreeView with Hierarchical Data](043-treeview-hierarchical-data.md), [043V — TreeView with Hierarchical Data (verbose companion)](043-treeview-hierarchical-data-verbose.md)

---

## Example 1: Project File Explorer with Lazy Loading and Search

### Goal

Build a file-system tree that lazily loads directory contents on expand, supports search-as-you-type with debounced filtering, and distinguishes folders from files with different icons and context actions.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class FileSystemNode : ObservableObject
{
    private bool _childrenLoaded;

    public string Name { get; }
    public string FullPath { get; }
    public bool IsFolder { get; }
    public string IconKey => IsFolder ? "FolderIcon" : "FileIcon";

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ObservableCollection<FileSystemNode> _children = new();

    public FileSystemNode(string fullPath, bool isFolder)
    {
        FullPath = fullPath;
        Name = System.IO.Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(Name))
            Name = fullPath; // Drive root
        IsFolder = isFolder;

        if (isFolder)
        {
            // Placeholder to show expand chevron
            Children.Add(new FileSystemNode("", false) { Name = "Loading..." });
        }
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && IsFolder && !_childrenLoaded)
        {
            _childrenLoaded = true;
            _ = LoadChildrenAsync();
        }
    }

    private async Task LoadChildrenAsync()
    {
        try
        {
            var children = new List<FileSystemNode>();

            // Directories first
            foreach (var dir in System.IO.Directory.EnumerateDirectories(FullPath))
            {
                if (!HasHiddenAttribute(dir))
                    children.Add(new FileSystemNode(dir, true));
            }

            // Then files
            foreach (var file in System.IO.Directory.EnumerateFiles(FullPath))
            {
                if (!HasHiddenAttribute(file))
                    children.Add(new FileSystemNode(file, false));
            }

            // Sort: folders first by name, then files by name
            var sorted = children
                .OrderByDescending(n => n.IsFolder)
                .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Children.Clear();
                foreach (var node in sorted)
                    Children.Add(node);
            });
        }
        catch (UnauthorizedAccessException)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Children.Clear();
                Children.Add(new FileSystemNode("", false) { Name = "Access Denied" });
            });
        }
    }

    private static bool HasHiddenAttribute(string path)
    {
        try
        {
            return (System.IO.File.GetAttributes(path) & System.IO.FileAttributes.Hidden) != 0;
        }
        catch
        {
            return false;
        }
    }
}

public partial class FileExplorerViewModel : ObservableObject
{
    private List<FileSystemNode>? _allFiltered;

    [ObservableProperty]
    private ObservableCollection<FileSystemNode> _rootNodes = new();

    [ObservableProperty]
    private FileSystemNode? _selectedNode;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusText = "";

    private CancellationTokenSource? _searchCts;

    public FileExplorerViewModel()
    {
        // Start at drive roots
        foreach (var drive in System.IO.DriveInfo.GetDrives()
                     .Where(d => d.IsReady))
        {
            RootNodes.Add(new FileSystemNode(drive.RootDirectory.FullName, true));
        }
    }

    partial void OnSelectedNodeChanged(FileSystemNode? value)
    {
        if (value is not null)
        {
            StatusText = value.IsFolder
                ? $"Folder: {value.FullPath}"
                : $"File: {value.FullPath} ({GetFileSize(value.FullPath)})";
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            await Task.Delay(300, token);
            if (token.IsCancellationRequested) return;

            var filtered = await BuildFilteredTreeAsync(value, token);
            if (token.IsCancellationRequested) return;

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                RootNodes.Clear();
                foreach (var node in filtered)
                    RootNodes.Add(node);
            });
        }, token);
    }

    private async Task<List<FileSystemNode>> BuildFilteredTreeAsync(
        string search, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return await Task.Run(() =>
            {
                var roots = new List<FileSystemNode>();
                foreach (var drive in System.IO.DriveInfo.GetDrives()
                             .Where(d => d.IsReady))
                {
                    roots.Add(new FileSystemNode(drive.RootDirectory.FullName, true));
                }
                return roots;
            }, token);
        }

        // Walk a few levels deep to find matches
        var results = new List<FileSystemNode>();
        foreach (var drive in System.IO.DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            token.ThrowIfCancellationRequested();
            var matched = await SearchSubtreeAsync(
                drive.RootDirectory.FullName, search, 0, 3, token);
            if (matched is not null)
                results.Add(matched);
        }
        return results;
    }

    private async Task<FileSystemNode?> SearchSubtreeAsync(
        string path, string search, int depth, int maxDepth, CancellationToken token)
    {
        if (depth > maxDepth) return null;
        token.ThrowIfCancellationRequested();

        var name = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) name = path;
        var isFolder = System.IO.Directory.Exists(path);
        var matches = name.Contains(search, StringComparison.OrdinalIgnoreCase);

        var childNodes = new List<FileSystemNode>();
        if (isFolder && depth < maxDepth)
        {
            try
            {
                foreach (var entry in System.IO.Directory.EnumerateFileSystemEntries(path))
                {
                    token.ThrowIfCancellationRequested();
                    var child = await SearchSubtreeAsync(
                        entry, search, depth + 1, maxDepth, token);
                    if (child is not null)
                        childNodes.Add(child);
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        if (matches || childNodes.Count > 0)
        {
            var node = new FileSystemNode(path, isFolder);
            if (childNodes.Count > 0)
            {
                node.Children.Clear();
                foreach (var c in childNodes)
                    node.Children.Add(c);
            }
            else if (!matches)
            {
                return null;
            }
            // Ensure placeholder for expandability if not loaded
            if (isFolder && node.Children.Count == 0 && !matches)
                return null;
            return node;
        }

        return null;
    }

    private static string GetFileSize(string path)
    {
        try
        {
            var info = new System.IO.FileInfo(path);
            if (info.Length < 1024) return $"{info.Length} B";
            if (info.Length < 1024 * 1024) return $"{info.Length / 1024} KB";
            return $"{info.Length / (1024 * 1024):F1} MB";
        }
        catch { return "Unknown"; }
    }

    [RelayCommand]
    private void ExpandAll()
    {
        _ = ExpandAllAsync(RootNodes, 0, 2);
    }

    private async Task ExpandAllAsync(
        IEnumerable<FileSystemNode> nodes, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;

        foreach (var node in nodes)
        {
            if (node.IsFolder)
            {
                node.IsExpanded = true;
                // Small delay to let the UI breathe
                await Task.Delay(10);
                await ExpandAllAsync(node.Children, depth + 1, maxDepth);
            }
        }
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var node in RootNodes)
            CollapseNode(node);
    }

    private void CollapseNode(FileSystemNode node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children)
            CollapseNode(child);
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        xmlns:models="clr-namespace:MyApp.Models"
        x:DataType="vm:FileExplorerViewModel"
        Title="Project File Explorer" Width="600" Height="450">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <TextBox Watermark="Search files and folders..."
               Text="{Binding SearchText}"
               Width="250" />
      <Button Content="Expand to 2 Levels" Command="{Binding ExpandAllCommand}" />
      <Button Content="Collapse All" Command="{Binding CollapseAllCommand}" />
    </StackPanel>

    <TreeView ItemsSource="{Binding RootNodes}"
              SelectedItem="{Binding SelectedNode}"
              VirtualizingPanel.VirtualizationMode="Recycling"
              x:DataType="vm:FileExplorerViewModel">
      <TreeView.Styles>
        <Style Selector="TreeViewItem">
          <Setter Property="IsExpanded"
                  Value="{Binding IsExpanded, Mode=TwoWay}" />
        </Style>
        <Style Selector="TreeViewItem:empty /template/ ToggleButton#PART_ExpandCollapseChevron">
          <Setter Property="IsVisible" Value="False" />
        </Style>
        <Style Selector="TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter">
          <Setter Property="Background"
                  Value="{DynamicResource SystemAccentColor}" />
          <Setter Property="Foreground" Value="White" />
        </Style>
      </TreeView.Styles>
      <TreeView.DataTemplates>
        <TreeDataTemplate DataType="models:FileSystemNode"
                          ItemsSource="{Binding Children}"
                          x:DataType="models:FileSystemNode">
          <StackPanel Orientation="Horizontal" Spacing="6">
            <PathIcon Data="{StaticResource FolderIcon}"
                      IsVisible="{Binding IsFolder}" />
            <PathIcon Data="{StaticResource FileIcon}"
                      IsVisible="{Binding IsFolder, Converter={StaticResource BoolInvertConverter}}" />
            <TextBlock Text="{Binding Name}" />
          </StackPanel>
        </TreeDataTemplate>
      </TreeView.DataTemplates>
    </TreeView>

    <StatusBar DockPanel.Dock="Bottom">
      <TextBlock Text="{Binding StatusText}" />
    </StatusBar>
  </DockPanel>
</Window>
```

### How It Works

1. **Lazy loading with placeholder** — Each `FileSystemNode` created as a folder starts with a single placeholder child ("Loading..."). This makes `HasItems = true` so the expand chevron appears. When the user expands, `OnIsExpandedChanged` triggers `LoadChildrenAsync`, which clears the placeholder and adds real children.

2. **Async directory enumeration** — `LoadChildrenAsync` runs on a background thread via `Task.Run` (called from `_ = LoadChildrenAsync()` which starts the task). After gathering children, it marshals back to the UI thread with `Dispatcher.UIThread.InvokeAsync` to update the `ObservableCollection`.

3. **Debounced search** — `OnSearchTextChanged` cancels any previous search via `CancellationTokenSource`, waits 300ms, then builds a filtered tree. The filter walks up to 3 levels deep and includes any node whose name matches or has a matching descendant. Results replace `RootNodes` entirely.

4. **Expand to depth** — `ExpandAllAsync` recursively sets `IsExpanded = true` on nodes up to a maximum depth (2), with a 10ms delay between nodes to avoid blocking the UI thread. `CollapseAll` recursively sets `IsExpanded = false`.

5. **Status bar feedback** — `OnSelectedNodeChanged` updates `StatusText` with the selected item's path and (for files) its size. The `StatusBar` at the bottom displays this.

6. **Type-conditional icons** — The `TreeDataTemplate` shows either the `FolderIcon` or `FileIcon` based on the `IsFolder` property, using a `BoolInvertConverter` for the file icon visibility.

### Design Decisions and Trade-offs

- **Background thread for enumeration**: `Directory.EnumerateDirectories` is IO-bound and should not run on the UI thread. The async pattern keeps the UI responsive during expansion. The trade-off is complexity: cancellation tokens, thread marshaling, and error handling for `UnauthorizedAccessException`.
- **Placeholder pattern vs IsExpanded binding only**: Without the placeholder, `OnIsExpandedChanged` would never fire because the chevron would not appear. The placeholder adds a brief "Loading..." flash. For a smoother UX, replace the placeholder with a loading spinner inside the `TreeDataTemplate`.
- **Search depth limit at 3**: Walking the entire filesystem on every keystroke is impractical. The 3-level cap keeps search responsive. For deeper searches, add an explicit "Search all" button that runs without depth limit.
- **VirtualizationMode="Recycling"**: Reduces container creation overhead during expand/collapse. Without recycling, expanding a folder with 500 files creates 500 `TreeViewItem` instances; recycling reuses containers from collapsed nodes.

---

## Example 2: Organizational Chart with Drag-to-Reparent

### Goal

Display a company org chart where managers can drag employees between teams, track selection in a detail panel, and collapse/expand business units.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class EmployeeNode : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Email { get; set; } = "";
    public string AvatarInitials => Name.Length > 0
        ? string.Join("", Name.Split(' ').Take(2).Select(w => w[0]))
        : "?";

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private ObservableCollection<EmployeeNode> _children = new();

    public bool IsManager => Children.Count > 0;
}

public partial class OrgChartViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<EmployeeNode> _rootNodes = new();

    [ObservableProperty]
    private EmployeeNode? _selectedEmployee;

    [ObservableProperty]
    private string _detailText = "Select an employee";

    private int _nextId = 100;

    public OrgChartViewModel()
    {
        BuildOrg();
    }

    private void BuildOrg()
    {
        var ceo = new EmployeeNode
        {
            Id = 1,
            Name = "Sarah Chen",
            Title = "CEO",
            Email = "sarah@company.com",
            Children =
            {
                new EmployeeNode
                {
                    Id = 2, Name = "Alice Johnson", Title = "VP Engineering", Email = "alice@company.com",
                    Children =
                    {
                        new EmployeeNode { Id = 4, Name = "Bob Smith", Title = "Engineering Lead", Email = "bob@company.com" },
                        new EmployeeNode { Id = 5, Name = "Carol Lee", Title = "Senior Dev", Email = "carol@company.com" },
                    }
                },
                new EmployeeNode
                {
                    Id = 3, Name = "David Brown", Title = "VP Marketing", Email = "david@company.com",
                    Children =
                    {
                        new EmployeeNode { Id = 6, Name = "Eve Davis", Title = "Marketing Lead", Email = "eve@company.com" },
                        new EmployeeNode { Id = 7, Name = "Frank Wilson", Title = "Designer", Email = "frank@company.com" },
                    }
                },
            }
        };
        RootNodes.Add(ceo);
    }

    partial void OnSelectedEmployeeChanged(EmployeeNode? value)
    {
        DetailText = value is not null
            ? $"{value.Name}\n{value.Title}\n{value.Email}"
            : "Select an employee";
    }

    [RelayCommand]
    private void AddEmployee(EmployeeNode? manager)
    {
        if (manager is null) return;
        _nextId++;
        manager.Children.Add(new EmployeeNode
        {
            Id = _nextId,
            Name = "New Hire",
            Title = "Employee",
            Email = $"newhire{_nextId}@company.com",
        });
        manager.IsExpanded = true;
    }

    [RelayCommand]
    private void RemoveEmployee(EmployeeNode? employee)
    {
        if (employee is null) return;

        // Find and remove from parent
        foreach (var root in RootNodes)
        {
            if (RemoveFromParent(root, employee))
                break;
        }
    }

    private bool RemoveFromParent(EmployeeNode parent, EmployeeNode target)
    {
        if (parent.Children.Remove(target))
            return true;

        foreach (var child in parent.Children)
        {
            if (RemoveFromParent(child, target))
                return true;
        }
        return false;
    }

    [RelayCommand]
    private void ReparentEmployee(EmployeeNode? employee)
    {
        // This would be triggered by a drag-drop operation.
        // The actual drag-drop handling is done via Avalonia's DragDrop attached events.
        // This command is a stub for programmatic reparenting.
    }

    [RelayCommand]
    private void ExpandBusinessUnits()
    {
        foreach (var root in RootNodes)
            ExpandToLevel(root, 2);
    }

    private void ExpandToLevel(EmployeeNode node, int depth)
    {
        if (depth <= 0) return;
        node.IsExpanded = true;
        foreach (var child in node.Children)
            ExpandToLevel(child, depth - 1);
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var root in RootNodes)
            CollapseNode(root);
    }

    private void CollapseNode(EmployeeNode node)
    {
        node.IsExpanded = false;
        foreach (var child in node.Children)
            CollapseNode(child);
    }
}
```

### View

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        xmlns:models="clr-namespace:MyApp.Models"
        x:DataType="vm:OrgChartViewModel"
        Title="Organizational Chart" Width="700" Height="500">
  <DockPanel>
    <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="8" Margin="8">
      <Button Content="Expand BUs" Command="{Binding ExpandBusinessUnitsCommand}" />
      <Button Content="Collapse All" Command="{Binding CollapseAllCommand}" />
      <Button Content="Add Employee"
              Command="{Binding AddEmployeeCommand}"
              CommandParameter="{Binding SelectedEmployee}" />
      <Button Content="Remove Selected"
              Command="{Binding RemoveEmployeeCommand}"
              CommandParameter="{Binding SelectedEmployee}" />
    </StackPanel>

    <Grid ColumnDefinitions="2*, *" Margin="8">
      <!-- TreeView -->
      <TreeView ItemsSource="{Binding RootNodes}"
                SelectedItem="{Binding SelectedEmployee}"
                AllowDrop="True"
                VirtualizingPanel.VirtualizationMode="Recycling"
                x:DataType="vm:OrgChartViewModel">
        <TreeView.Styles>
          <Style Selector="TreeViewItem">
            <Setter Property="IsExpanded"
                    Value="{Binding IsExpanded, Mode=TwoWay}" />
          </Style>
          <Style Selector="TreeViewItem:empty /template/ ToggleButton#PART_ExpandCollapseChevron">
            <Setter Property="IsVisible" Value="False" />
          </Style>
          <Style Selector="TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter">
            <Setter Property="Background"
                    Value="{DynamicResource SystemAccentColor}" />
            <Setter Property="Foreground" Value="White" />
          </Style>
        </TreeView.Styles>
        <TreeView.DataTemplates>
          <TreeDataTemplate DataType="models:EmployeeNode"
                            ItemsSource="{Binding Children}"
                            x:DataType="models:EmployeeNode">
            <StackPanel Orientation="Horizontal" Spacing="8">
              <Border Width="28" Height="28" CornerRadius="14"
                      Background="{DynamicResource SystemAccentColor}"
                      VerticalAlignment="Center">
                <TextBlock Text="{Binding AvatarInitials}"
                           Foreground="White"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center"
                           FontSize="11" FontWeight="Bold" />
              </Border>
              <StackPanel VerticalAlignment="Center">
                <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
                <TextBlock Text="{Binding Title}" FontSize="11" Foreground="Gray" />
              </StackPanel>
            </StackPanel>
          </TreeDataTemplate>
        </TreeView.DataTemplates>
      </TreeView>

      <!-- Detail panel -->
      <Border Grid.Column="1" BorderBrush="LightGray"
              BorderThickness="1" CornerRadius="6" Padding="12"
              Margin="8,0,0,0">
        <StackPanel Spacing="8" VerticalAlignment="Top">
          <TextBlock Text="Employee Details"
                     FontSize="16" FontWeight="Bold" />
          <TextBlock Text="{Binding DetailText}"
                     TextWrapping="Wrap" />
          <Separator Margin="0,8" />

          <TextBlock Text="Name: " FontWeight="SemiBold" />
          <TextBlock Text="{Binding SelectedEmployee.Name}" />

          <TextBlock Text="Title: " FontWeight="SemiBold" />
          <TextBlock Text="{Binding SelectedEmployee.Title}" />

          <TextBlock Text="Email: " FontWeight="SemiBold" />
          <TextBlock Text="{Binding SelectedEmployee.Email}" />

          <TextBlock Text="Reports: " FontWeight="SemiBold" />
          <TextBlock Text="{Binding SelectedEmployee.Children.Count}"
                     x:DataType="models:EmployeeNode" />
        </StackPanel>
      </Border>
    </Grid>
  </DockPanel>
</Window>
```

### How It Works

1. **TreeDataTemplate for org hierarchy** — Each `EmployeeNode` has a `Children` collection for direct reports. The `TreeDataTemplate` binds `ItemsSource="{Binding Children}"` to create recursive tree levels. Manager nodes get the expand chevron; leaf nodes (no children) get `:empty` pseudo-class hiding the chevron.

2. **Selection with detail panel** — `SelectedItem="{Binding SelectedEmployee}"` is a `TwoWay` binding. When the user clicks any node, `OnSelectedEmployeeChanged` updates the `DetailText` property with a multi-line string of name, title, and email. The right-side detail panel shows structured data bound to `SelectedEmployee.*`.

3. **Add/Remove employee** — "Add Employee" adds a new `EmployeeNode` to the currently selected manager's `Children` and auto-expands the manager. "Remove Selected" recursively searches the tree to find and remove the selected node from its parent's `Children`.

4. **Expand to level** — `ExpandBusinessUnitsCommand` calls `ExpandToLevel` which sets `IsExpanded = true` on root and first-level children (depth 2). The `IsExpanded` style binding propagates to the UI automatically.

5. **Avatar circle** — Each node shows a colored circle with the employee's initials, generated from the `AvatarInitials` property (takes first character of first two name parts). The `Border` with `CornerRadius="14"` creates the circular clip.

6. **Drag-drop readiness** — `AllowDrop="True"` is set on the TreeView. A full drag-drop implementation would handle `DragDrop.DragStart`, `DragDrop.Drop`, and drop-position indicators. The `ReparentEmployeeCommand` is a stub for the drop handler.

### Design Decisions and Trade-offs

- **Recursive add/remove vs flat lookup** — `RemoveFromParent` walks the entire tree recursively. For a flat org (100-200 employees) this is fine. For larger orgs, maintain a `Dictionary<int, EmployeeNode>` lookup for O(1) access.
- **Detail panel via SelectedEmployee binding vs event** — Binding `SelectedEmployee` directly allows the detail panel to use sub-bindings (`SelectedEmployee.Name`, etc.). The trade-off: nullable sub-bindings require `x:DataType` on the detail panel's data context, or use `TargetNullValue`.
- **Single template for all nodes** — Unlike the file explorer (which could use separate templates for managers vs ICs), the org chart uses one `TreeDataTemplate` for all `EmployeeNode` items. The UI distinguishes managers from ICs only by the expand chevron presence. This is simpler but gives less visual differentiation.
- **Expanded state persistence** — The `IsExpanded` property is on each `EmployeeNode`. For large orgs, saving and restoring expanded state requires serializing the `IsExpanded` flag per node. This is straightforward to implement with JSON serialization.

---

## Comparison: What Each Example Demonstrates

| Aspect | Example 1: File Explorer | Example 2: Org Chart |
|--------|-------------------------|---------------------|
| **Data source** | Real filesystem (IO-bound) | In-memory model (flat) |
| **Node types** | Folders and files (single TreeDataTemplate) | Employees with managers and ICs (single TreeDataTemplate) |
| **Lazy loading** | Async with placeholder + cancellation | Not needed (full data loaded) |
| **Search** | Debounced, async, depth-limited | Not implemented |
| **Expand/collapse control** | Expand to depth N, Collapse all | Expand BUs (depth 2), Collapse all |
| **Selection** | Single, with status bar display | Single, with detail panel |
| **Drag/drop** | Not implemented | AllowDrop=True, command stub |
| **Add/Remove** | Not implemented | Add to manager, remove from parent |
| **Virtualization** | Recycling mode | Recycling mode |
| **Key edge case** | UnauthorizedAccessException for protected dirs | Recursive remove from tree |
| **Threading** | Background IO with UI marshaling | All on UI thread |

---

## See Also

- [043 — TreeView with Hierarchical Data](043-treeview-hierarchical-data.md)
- [043V — TreeView with Hierarchical Data (verbose companion)](043-treeview-hierarchical-data-verbose.md)
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](015-item-lists.md)
- [019 — Drag and Drop](019-drag-drop.md)
- [036 — Virtualization and Large List Performance](../advanced/036-virtualization-large-lists.md)
- [Avalonia Docs: TreeView](https://docs.avaloniaui.net/controls/treeview)
- [Avalonia Docs: TreeDataTemplate](https://docs.avaloniaui.net/controls/treeview/how-to-use-treeview-with-hierarchical-data)
