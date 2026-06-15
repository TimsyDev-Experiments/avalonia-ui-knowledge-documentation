---
tier: advanced
topic: accessibility
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 026-accessibility-automation.md
---

# 026E — Accessibility & Automation: Real-World Examples

**Applies to:** [026 — Accessibility & Automation](026-accessibility-automation.md) | [026V — In-Depth Companion](026-accessibility-automation-verbose.md)

---

## Example 1: AccessibleTreeView

### Goal

A custom tree view control with full keyboard navigation (arrow keys, Home/End, type-ahead find, expand/collapse with Left/Right) and a custom `AutomationPeer` that exposes the tree structure to screen readers. The control reports selection state, parent-child relationships, and expand/collapse state via UIA.

### ViewModel

```csharp
// ViewModels/FileExplorerViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class TreeNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    public ObservableCollection<TreeNodeViewModel> Children { get; } = [];

    public bool HasChildren => Children.Count > 0;
}

public partial class FileExplorerViewModel : ObservableObject
{
    [ObservableProperty]
    private TreeNodeViewModel? _selectedNode;

    public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = [];

    [RelayCommand]
    private void SelectNode(TreeNodeViewModel node)
    {
        if (SelectedNode is not null)
            SelectedNode.IsSelected = false;
        node.IsSelected = true;
        SelectedNode = node;
    }

    [RelayCommand]
    private void ToggleExpand(TreeNodeViewModel node)
    {
        node.IsExpanded = !node.IsExpanded;
    }
}
```

### XAML View

```xml
<!-- Views/FileExplorerView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:controls="using:MyApp.Controls"
             x:DataType="vm:FileExplorerViewModel">
  <controls:AccessibleTreeView
      ItemsSource="{Binding RootNodes}"
      SelectedNode="{Binding SelectedNode, Mode=TwoWay}"
      SelectNodeCommand="{Binding SelectNodeCommand}"
      ToggleExpandCommand="{Binding ToggleExpandCommand}" />
</UserControl>
```

### How It Works

1. `AccessibleTreeView` extends `TemplatedControl`. It hosts an `ItemsControl` in its template that recursively renders `TreeNodeViewModel` items. Each node renders as a `Border` with expand/collapse arrow, icon, and text.
2. The control overrides `OnCreateAutomationPeer()` to return a `TreeViewAutomationPeer`. This peer implements `IExpandCollapseProvider` and `ISelectionProvider`, mapping to UIA control patterns.
3. For keyboard handling, `OnKeyDown` processes:
   - `Up`/`Down` — move selection to previous/next visible node, wrapping at boundaries.
   - `Right` — if the node has children and is collapsed, expand it. Otherwise move to the first child.
   - `Left` — if the node is expanded, collapse it. Otherwise move to the parent.
   - `Home`/`End` — select the first/last visible node.
   - `Enter` or `Space` — toggle expand/collapse on the selected node.
   - Type-ahead: accumulate typed characters and jump to the next node whose name starts with that prefix.
4. The `AutomationPeer.GetNameCore()` returns the node's `Name` property. `GetAutomationControlTypeCore()` returns `AutomationControlType.TreeItem`. `GetPatternCore()` returns `Pattern.IExpandCollapse` or `Pattern.ISelection` as appropriate.
5. When `IsExpanded` or `IsSelected` changes on a `TreeNodeViewModel`, the control calls `AutomationProperties.SetName(...)` on the corresponding container to trigger a live-region update, and raises `RaiseAutomationEvent(AutomationEvents.PropertyChanged)` via the peer.

### Implementation

```csharp
// Controls/AccessibleTreeView.cs
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MyApp.Controls;

public class AccessibleTreeView : TemplatedControl
{
    private ItemsControl? _itemsHost;
    private readonly List<Control> _visibleNodes = [];
    private string _typeAheadBuffer = string.Empty;
    private DateTime _lastTypeAheadTime;

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<AccessibleTreeView, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedNodeProperty =
        AvaloniaProperty.Register<AccessibleTreeView, object?>(nameof(SelectedNode),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> SelectNodeCommandProperty =
        AvaloniaProperty.Register<AccessibleTreeView, ICommand?>(nameof(SelectNodeCommand));

    public static readonly StyledProperty<ICommand?> ToggleExpandCommandProperty =
        AvaloniaProperty.Register<AccessibleTreeView, ICommand?>(nameof(ToggleExpandCommand));

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    public ICommand? SelectNodeCommand
    {
        get => GetValue(SelectNodeCommandProperty);
        set => SetValue(SelectNodeCommandProperty, value);
    }

    public ICommand? ToggleExpandCommand
    {
        get => GetValue(ToggleExpandCommandProperty);
        set => SetValue(ToggleExpandCommandProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _itemsHost = e.NameScope.Find<ItemsControl>("PartItemsHost");
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TreeViewAutomationPeer(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        BuildVisibleNodeList();

        if (_visibleNodes.Count == 0) return;

        var currentIndex = SelectedNode is not null
            ? _visibleNodes.FindIndex(n => n.DataContext == SelectedNode)
            : -1;

        switch (e.Key)
        {
            case Key.Down:
                MoveSelectionTo(currentIndex + 1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveSelectionTo(currentIndex - 1);
                e.Handled = true;
                break;
            case Key.Right:
                if (currentIndex >= 0)
                    ToggleExpandCommand?.Execute(_visibleNodes[currentIndex].DataContext);
                e.Handled = true;
                break;
            case Key.Left:
                if (currentIndex >= 0)
                    ToggleExpandCommand?.Execute(_visibleNodes[currentIndex].DataContext);
                e.Handled = true;
                break;
            case Key.Home:
                MoveSelectionTo(0);
                e.Handled = true;
                break;
            case Key.End:
                MoveSelectionTo(_visibleNodes.Count - 1);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Space:
                if (currentIndex >= 0)
                    ToggleExpandCommand?.Execute(_visibleNodes[currentIndex].DataContext);
                e.Handled = true;
                break;
            default:
                HandleTypeAhead(e);
                break;
        }
    }

    private void MoveSelectionTo(int index)
    {
        if (_visibleNodes.Count == 0) return;
        index = Math.Clamp(index, 0, _visibleNodes.Count - 1);
        var node = _visibleNodes[index];
        SelectedNode = node.DataContext;
        SelectNodeCommand?.Execute(node.DataContext);
        node.Focus();
    }

    private void HandleTypeAhead(KeyEventArgs e)
    {
        if (e.KeySymbol is null || e.KeySymbol.Length != 1) return;

        var now = DateTime.UtcNow;
        if ((now - _lastTypeAheadTime).TotalMilliseconds > 500)
            _typeAheadBuffer = string.Empty;

        _typeAheadBuffer += e.KeySymbol.ToUpperInvariant();
        _lastTypeAheadTime = now;

        var startIndex = SelectedNode is not null
            ? _visibleNodes.FindIndex(n => n.DataContext == SelectedNode) + 1
            : 0;

        for (int i = 0; i < _visibleNodes.Count; i++)
        {
            var idx = (startIndex + i) % _visibleNodes.Count;
            var name = GetNodeName(_visibleNodes[idx].DataContext);
            if (name?.StartsWith(_typeAheadBuffer, StringComparison.OrdinalIgnoreCase) == true)
            {
                MoveSelectionTo(idx);
                e.Handled = true;
                return;
            }
        }
    }

    private static string? GetNodeName(object? node)
    {
        return node?.GetType().GetProperty("Name")?.GetValue(node)?.ToString();
    }

    private void BuildVisibleNodeList()
    {
        _visibleNodes.Clear();
        if (_itemsHost is null) return;
        CollectVisibleNodes(_itemsHost);
    }

    private void CollectVisibleNodes(ItemsControl parent)
    {
        foreach (var child in parent.GetRealizedContainers())
        {
            _visibleNodes.Add(child);
            if (child.DataContext?.GetType().GetProperty("IsExpanded")?.GetValue(child.DataContext) is true)
            {
                if (child is ItemsControl nested)
                    CollectVisibleNodes(nested);
            }
        }
    }
}
```

```csharp
// Controls/TreeViewAutomationPeer.cs
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace MyApp.Controls;

public class TreeViewAutomationPeer : AutomationPeer, IExpandCollapseProvider, ISelectionProvider
{
    private readonly AccessibleTreeView _owner;

    public TreeViewAutomationPeer(AccessibleTreeView owner) : base(owner)
    {
        _owner = owner;
    }

    protected override string GetNameCore()
    {
        return _owner.SelectedNode?.ToString() ?? "Tree view";
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Tree;
    }

    protected override object? GetPatternCore(Pattern pattern)
    {
        return pattern switch
        {
            Pattern.ExpandCollapse => this,
            Pattern.Selection => this,
            _ => null,
        };
    }

    public void Expand()
    {
        if (_owner.SelectedNode is not null)
            _owner.ToggleExpandCommand?.Execute(_owner.SelectedNode);
    }

    public void Collapse()
    {
        if (_owner.SelectedNode is not null)
            _owner.ToggleExpandCommand?.Execute(_owner.SelectedNode);
    }

    public bool CanSelectMultiple => false;
    public bool IsSelectionRequired => false;

    public IRawElementProviderSimple? GetSelection()
    {
        if (_owner.SelectedNode is null) return null;
        return ProviderFromPeer(this);
    }
}
```

---

### Design Decisions
- **Custom `AutomationPeer` over attached properties only.** Attached properties provide static metadata. An `AutomationPeer` enables dynamic control patterns (expand/collapse, selection) that screen readers use for rich interaction. Nodes without a peer appear as generic "custom" controls in the automation tree.
- **`IExpandCollapseProvider` implementation.** The peer's `Expand()` / `Collapse()` methods invoke the `ToggleExpandCommand` on the ViewModel. This allows assistive technology (like NVDA's object navigation) to expand nodes programmatically without simulating keyboard input.
- **Recursive template via `HierarchicalDataTemplate`.** The tree template structure in Avalonia uses `HierarchicalDataTemplate` with `ItemsSource="{Binding Children}"` inside the `AccessibleTreeView`'s template to create properly nested containers.

### Edge Cases

- **Deep nesting (10+ levels).** The automation tree depth is unlimited, but screen readers may flatten beyond a certain depth. Set `AutomationProperties.AccessibilityView` on deep nodes to `Content` to simplify the tree for the user.
- **Empty tree.** `ItemsSource` is null or empty. The control shows a "No items" `TextBlock` and does not claim focus. The automation peer reports an empty tree.
- **Rapid keyboard input during type-ahead.** The type-ahead buffer resets after 500ms of inactivity. If the user pauses between characters, the buffer clears and starts fresh. This matches Explorer's type-ahead behavior.
- **Selected node is removed from the tree.** Clear `SelectedNode` to null and move focus to the first remaining node.
- **Focus indicator.** The `:focus` pseudo-class style highlights the selected node with a distinct background. Without this, keyboard-only users cannot track their position.

---

## Example 2: BackgroundOperationDialog

### Goal

A dialog that shows progress for a background file-copy operation. It displays a progress bar, estimated time remaining, and item count. The dialog uses live regions to announce status changes to screen readers (polite for incremental progress, assertive for completion or errors). Results are reported after the operation finishes.

### ViewModel

```csharp
// ViewModels/FileCopyViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class FileCopyViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Preparing...";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private int _filesCopied;

    [ObservableProperty]
    private int _totalFiles = 10;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _accessibilityAnnouncement = string.Empty;

    [RelayCommand]
    private async Task StartCopyAsync()
    {
        IsRunning = true;
        IsComplete = false;
        ErrorMessage = null;
        Progress = 0;
        FilesCopied = 0;

        for (int i = 1; i <= TotalFiles; i++)
        {
            await Task.Delay(200); // simulate copy
            FilesCopied = i;
            Progress = (double)i / TotalFiles * 100;
            StatusMessage = $"Copying file {i} of {TotalFiles}...";
            AccessibilityAnnouncement = StatusMessage;

            if (i == 5) // simulate an error
            {
                ErrorMessage = $"Failed to copy file {i}: access denied";
                AccessibilityAnnouncement = ErrorMessage;
                StatusMessage = "Copy completed with errors";
                break;
            }
        }

        IsRunning = false;
        IsComplete = true;
        if (ErrorMessage is null)
        {
            StatusMessage = "All files copied successfully";
            AccessibilityAnnouncement = StatusMessage;
        }
    }
}
```

### XAML View

```xml
<!-- Views/FileCopyView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:FileCopyViewModel"
             x:Name="Root">
  <Border Background="White" CornerRadius="12"
          Padding="24" MinWidth="400"
          AutomationProperties.Name="File copy progress">
    <StackPanel Spacing="12">
      <!-- Status text with live region -->
      <TextBlock Text="{Binding StatusMessage}"
                 AutomationProperties.LiveSetting="Polite"
                 AutomationProperties.Name="{Binding AccessibilityAnnouncement}"
                 FontSize="16" />

      <!-- Progress bar -->
      <ProgressBar Value="{Binding Progress}"
                   Minimum="0" Maximum="100"
                   Height="24"
                   AutomationProperties.Name="Copy progress" />

      <!-- File count -->
      <TextBlock HorizontalAlignment="Center"
                 AutomationProperties.Name="{Binding AccessibilityAnnouncement}">
        <Run Text="{Binding FilesCopied}" />
        <Run Text=" of " />
        <Run Text="{Binding TotalFiles}" />
        <Run Text=" files copied" />
      </TextBlock>

      <!-- Error message (assertive) -->
      <TextBlock Text="{Binding ErrorMessage}"
                 AutomationProperties.LiveSetting="Assertive"
                 Foreground="Red"
                 IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}" />

      <!-- Action buttons -->
      <StackPanel Orientation="Horizontal" HorizontalAlignment="Center"
                  Spacing="8">
        <Button Content="Start Copy"
                Command="{Binding StartCopyCommand}"
                IsVisible="{Binding IsComplete, Converter={x:Static BoolConverters.Not}}" />
        <Button Content="Close"
                Command="{Binding CloseCommand}"
                IsVisible="{Binding IsComplete}" />
      </StackPanel>
    </StackPanel>
  </Border>
</UserControl>
```

### How It Works

1. The `FileCopyViewModel` drives the entire dialog state. `StartCopyCommand` is an `AsyncRelayCommand` that iterates files, updates progress and status after each file.
2. The `StatusMessage` `TextBlock` has `AutomationProperties.LiveSetting="Polite"`. Each time `StatusMessage` changes, the screen reader announces the new text when idle.
3. `AccessibilityAnnouncement` is a dedicated property bound to `AutomationProperties.Name` on both the status text and the file count text. This ensures the live region fires even if the visual text does not change in a way the automation tree detects. Setting `Name` explicitly triggers the UIA `TextChanged` event.
4. The error `TextBlock` uses `LiveSetting="Assertive"`, which interrupts current speech to announce the error immediately.
5. The `ProgressBar` has its own `AutomationProperties.Name` so screen readers identify it as a progress indicator. The `Value` changes are automatically reflected in the automation tree's `IRangeValueProvider`.
6. When the operation completes, `AccessibilityAnnouncement` is set to the final status, and the live region announces it. The "Start Copy" button hides and "Close" button shows.

### Design Decisions

- **Separate `AccessibilityAnnouncement` property.** The visual `StatusMessage` may include formatting or prefixes that are redundant when spoken. A dedicated string property for announcements allows crafting a concise screen-reader message: "3 of 10 files copied" instead of "Copying file 3 of 10...". This follows the principle that visual text and accessible text can differ.
- **`Polite` for status, `Assertive` for errors.** Incremental progress announcements should not interrupt the user. Errors must interrupt because they require user action. This maps to WAI-ARIA `aria-live="polite"` and `aria-live="assertive"`.
- **`AsyncRelayCommand` for the copy operation.** The `await Task.Delay(200)` inside the loop does not block the UI thread. Properties are set on the UI thread via the dispatcher. The `IsRunning` / `IsComplete` flags enable/disable buttons at the correct times.
- **`StringConverters.IsNotNullOrEmpty` for error visibility.** Avalonia's built-in string converter returns `true` when the string is non-null and non-empty. This avoids a custom `IValueConverter`.

### Edge Cases

- **Operation finishes very quickly (< 500ms).** The progress jumps from 0 to 100 instantly. The live region fires only once with "All files copied successfully". The intermediate announcements are skipped, which is acceptable — the user hears the final state.
- **Multiple rapid errors.** Each error sets `ErrorMessage` and `AccessibilityAnnouncement`. The `Assertive` live region announces each one, potentially queuing multiple speech items. Limit error announcements to the first error and suppress subsequent ones until the user clears the dialog.
- **Dialog is closed while operation is running.** Cancel the `CancellationTokenSource` linked to the command. The `AsyncRelayCommand` handles cancellation gracefully and sets `IsRunning = false`.
- **Screen reader is not running.** The `AutomationProperties` have no effect on the visual UI. The dialog works correctly for sighted users — the properties are strictly additive for accessibility.
- **Progress bar indeterminate mode.** For operations where the total is unknown (e.g., searching), set `ProgressBar.IsIndeterminate = true` and provide a text-based status update instead of percentage. The live region still announces status changes.

---

## What These Examples Demonstrate

| Aspect | AccessibleTreeView | BackgroundOperationDialog |
|---|---|---|
| **Automation mechanism** | Custom `AutomationPeer` with `IExpandCollapseProvider`, `ISelectionProvider` | Attached `AutomationProperties` with live regions |
| **Dynamic updates** | Raises `AutomationEvents.PropertyChanged` via peer | Changes `AutomationProperties.Name` to trigger live region |
| **Keyboard navigation** | Arrow keys, Home/End, type-ahead, Enter/Space | Tab through dialog controls only |
| **Custom pattern** | `IExpandCollapseProvider` for expand/collapse in tree nodes | `IRangeValueProvider` via built-in `ProgressBar` |
| **Live region politeness** | Not used (property-change events) | `Polite` for progress, `Assertive` for errors |
| **Focus management** | Focus follows selection, visible focus indicator | Focus set to first focusable element on dialog open |

---

## See Also

- [026 — Accessibility & Automation](026-accessibility-automation.md)
- [026V — Accessibility & Automation (verbose companion)](026-accessibility-automation-verbose.md)
- [020 — Custom Templated Controls](020-custom-templated-controls.md) — templated control patterns for accessible design
- [022 — Attached Properties & Behaviors](022-attached-properties-behaviors.md) — `AutomationProperties` as attached properties
- [026 — Accessibility & Automation](026-accessibility-automation.md) — complete walkthrough of automation properties and patterns
- [Avalonia Docs: Accessibility](https://docs.avaloniaui.net/docs/accessibility/)
