---
tier: advanced
topic: accessibility
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 008-focus-keyboard-navigation.md
---

# 008V — Focus & Keyboard Navigation: An In-Depth Companion

You should already have read: [008 — Focus & Keyboard Navigation](008-focus-keyboard-navigation.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Understanding the Avalonia Focus System

### FocusScope — The Fundamental Unit

A `FocusScope` is a logical boundary inside which tab and arrow navigation operates independently. Every `Window` and `UserControl` is implicitly a focus scope. Nested focus scopes create a tree:

```
Window (root focus scope)
├── ToolBar (focus scope — Once mode)
│   ├── Button "New"
│   ├── Button "Open"
│   └── Button "Save"
├── ContentPanel (focus scope — Local mode)
│   ├── TextBox "Name"
│   ├── TextBox "Email"
│   └── Button "Submit"
└── StatusBar (focus scope — Contained mode)
    ├── TextBlock (not focusable)
    └── HyperlinkButton
```

When the user presses Tab, focus moves through focusable elements within the current scope according to the scope's `TabNavigation` mode. When the end of a scope is reached, focus jumps to the next scope in the parent's tab order.

### KeyboardNavigation.TabNavigation Values — Detailed Behavior

| Value | Tab Behavior | Arrow Key Behavior | Use Case |
|---|---|---|---|
| `Local` (default) | Tab moves through children by `TabIndex` | Arrow keys move focus inside scope | Standard forms, property panels |
| `Cycle` | Tab wraps from last child to first | Arrow keys move focus inside scope | Toolbars, palette windows |
| `Once` | Tab enters scope and focuses the first child; subsequent tabs leave the scope | Arrow keys move inside scope after focus enters | Tree views, list boxes that want arrow-key navigation |
| `Contained` | Tab circulates within scope; focus never leaves | Arrow keys move inside scope | Modal sub-panels, editable lists, grid cells |

---

## 2. Programmatic Focus Management

### FocusNavigation Helper Class (Extended)

```csharp
public static class FocusNavigation
{
    public static void MoveFocusNext(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Next, from, false);

    public static void MoveFocusPrevious(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Previous, from, false);

    public static void MoveFocusUp(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Up, from, false);

    public static void MoveFocusDown(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Down, from, false);

    public static void MoveFocusLeft(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Left, from, false);

    public static void MoveFocusRight(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Right, from, false);

    public static void FocusFirst(Control scope)
    {
        var focusManager = FocusManager.GetFocusManager(scope);
        if (focusManager?.GetFocusScope(scope) is { } fs)
            focusManager.MoveFocus(NavigationDirection.First, fs, false);
    }

    public static void FocusLast(Control scope)
    {
        var focusManager = FocusManager.GetFocusManager(scope);
        if (focusManager?.GetFocusScope(scope) is { } fs)
            focusManager.MoveFocus(NavigationDirection.Last, fs, false);
    }

    public static bool TryFocus(Control control)
    {
        if (control?.Focusable == true)
        {
            control.Focus();
            return control.IsFocused;
        }
        return false;
    }
}
```

### Focus Events Lifecycle

Understanding when focus events fire is critical for correct behavior:

```csharp
// GotFocus — fires when the element receives focus
// LostFocus — fires when the element loses focus
// IsFocusedPropertyChanged — fires on either transition

public partial class FocusLogger : ObservableObject
{
    [ObservableProperty]
    private string _focusedElement = "";

    public FocusLogger(Control root)
    {
        root.AddHandler(InputElement.GotFocusEvent, (_, e) =>
        {
            var source = e.Source as Control;
            FocusedElement = $"Got focus: {source?.GetType().Name} '{source?.Name}'";

            // Recurse to find the logical parent chain
            var chain = new List<string>();
            var current = source;
            while (current is not null)
            {
                chain.Add($"{current.GetType().Name} '{current.Name}'");
                current = current.Parent as Control;
            }
            FocusChain = string.Join(" > ", chain);
        }, RoutingStrategies.Bubble);

        root.AddHandler(InputElement.LostFocusEvent, (_, e) =>
        {
            var source = e.Source as Control;
            FocusedElement = $"Lost focus: {source?.GetType().Name} '{source?.Name}'";
        }, RoutingStrategies.Bubble);
    }

    [ObservableProperty]
    private string _focusChain = "";
}
```

---

## 3. Tab Order Management — Deep Dive

### TabIndex Priority

Tab order follows these rules in order of precedence:

1. **Scope containment** — focus moves within the current scope first
2. **TabIndex value** — lower values receive focus first
3. **Visual tree order** — elements with the same TabIndex are ordered by their position in the visual tree
4. **Z-order** — if visual tree order is ambiguous, elements rendered later (higher Z) come first

```xml
<StackPanel KeyboardNavigation.TabNavigation="Local">
  <TextBox KeyboardNavigation.TabIndex="20" Name="Second" />
  <TextBox KeyboardNavigation.TabIndex="10" Name="First" />
  <!-- Despite being declared second, "First" receives tab focus first -->
  <!-- because TabIndex="10" < TabIndex="20" -->
</StackPanel>
```

### IsTabStop — Skip Elements in Tab Navigation

Set `IsTabStop="False"` to skip an element during tab traversal while keeping it focusable programmatically:

```xml
<!-- Search box — tab skips over it, but clicking or Shift+F6 still focuses it -->
<TextBox Name="SearchBox"
         KeyboardNavigation.IsTabStop="False"
         Watermark="Search (Ctrl+F)" />

<!-- The "Clear" button next to the search box is also skipped -->
<Button Content="Clear"
        KeyboardNavigation.IsTabStop="False"
        Command="{Binding ClearSearchCommand}" />
```

### Tab Navigation Mode on Containers

`KeyboardNavigation.TabNavigation` can be set on any `Control` that contains children. The mode applies to all direct focusable children:

```xml
<!-- Cycle mode — after last field, wraps to first -->
<StackPanel KeyboardNavigation.TabNavigation="Cycle">
  <TextBox Name="Field1" />
  <TextBox Name="Field2" />
  <TextBox Name="Field3" />
</StackPanel>

<!-- Contained mode — tab never leaves this group -->
<Border KeyboardNavigation.TabNavigation="Contained"
        BorderBrush="LightGray" BorderThickness="1" Padding="8">
  <StackPanel>
    <TextBlock Text="Search Filters" FontWeight="SemiBold" />
    <TextBox Name="FilterName" />
    <ComboBox Name="FilterCategory" />
    <Button Content="Apply Filter" />
  </StackPanel>
</Border>
```

---

## 4. Custom Arrow-Key Navigation

### Editable List with Full Keyboard Support

```csharp
public sealed partial class EditableListViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _items = new();

    [ObservableProperty]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private bool _isEditing;

    private readonly ListBox _listBox;

    public EditableListViewModel(ListBox listBox)
    {
        _listBox = listBox;
    }

    [RelayCommand]
    private void HandleListKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                if (SelectedIndex > 0)
                {
                    SelectedIndex--;
                    ScrollIntoView(SelectedIndex);
                }
                e.Handled = true;
                break;

            case Key.Down:
                if (SelectedIndex < Items.Count - 1)
                {
                    SelectedIndex++;
                    ScrollIntoView(SelectedIndex);
                }
                e.Handled = true;
                break;

            case Key.Home:
                SelectedIndex = 0;
                ScrollIntoView(0);
                e.Handled = true;
                break;

            case Key.End:
                SelectedIndex = Items.Count - 1;
                ScrollIntoView(SelectedIndex);
                e.Handled = true;
                break;

            case Key.Enter:
                if (SelectedIndex >= 0)
                    IsEditing = true;
                e.Handled = true;
                break;

            case Key.F2:
                if (SelectedIndex >= 0)
                    IsEditing = true;
                e.Handled = true;
                break;

            case Key.Escape:
                if (IsEditing)
                {
                    IsEditing = false;
                    e.Handled = true;
                }
                break;

            case Key.Delete:
                if (SelectedIndex >= 0 && !IsEditing)
                {
                    Items.RemoveAt(SelectedIndex);
                    SelectedIndex = Math.Min(SelectedIndex, Items.Count - 1);
                }
                e.Handled = true;
                break;

            case Key.Insert:
                var newItem = $"Item {Items.Count + 1}";
                Items.Insert(SelectedIndex + 1, newItem);
                SelectedIndex++;
                IsEditing = true;
                e.Handled = true;
                break;
        }
    }

    private void ScrollIntoView(int index)
    {
        if (_listBox?.ItemContainerGenerator.ContainerFromIndex(index) is Control container)
            container.BringIntoView();
    }
}
```

### Arrow-Key Navigation Between Focus Scopes

For panel-to-panel arrow navigation (e.g., splitting a window into quadrants), use directional navigation:

```csharp
public static class DirectionalFocus
{
    public static void Attach(Control from, Control? up, Control? down, Control? left, Control? right)
    {
        from.KeyDown += (_, e) =>
        {
            var target = e.Key switch
            {
                Key.Up => up,
                Key.Down => down,
                Key.Left => left,
                Key.Right => right,
                _ => null
            };

            if (target is not null)
            {
                target.Focus();
                e.Handled = true;
            }
        };
    }
}

// Usage
DirectionalFocus.Attach(panelA, up: null, down: panelC, left: null, right: panelB);
DirectionalFocus.Attach(panelB, up: null, down: panelD, left: panelA, right: null);
```

---

## 5. Global vs. Local Key Bindings

### Binding Priority and Routing

Key bindings are evaluated in the following order:

1. **Focused control's KeyBindings** — the specific element that has focus
2. **Ancestor chain** — parent controls up to the Window
3. **Window.KeyBindings** — the top-level window
4. **Application.KeyBindings** — application-wide hotkeys (always active)

If a handler sets `e.Handled = true`, the event stops propagating to higher levels.

```csharp
// Local — only active when TextBox has focus
<TextBox Text="{Binding SearchText}">
  <TextBox.KeyBindings>
    <KeyBinding Gesture="Enter" Command="{Binding SearchCommand}" />
    <KeyBinding Gesture="Escape" Command="{Binding ClearSearchCommand}" />
  </TextBox.KeyBindings>
</TextBox>

// Window-level — active for any focused element in the window, unless handled lower
<Window.KeyBindings>
  <KeyBinding Gesture="Ctrl+N" Command="{Binding NewDocumentCommand}" />
  <KeyBinding Gesture="Ctrl+O" Command="{Binding OpenDocumentCommand}" />
  <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
  <KeyBinding Gesture="Ctrl+Shift+S" Command="{Binding SaveAsCommand}" />
  <KeyBinding Gesture="Ctrl+Z" Command="{Binding UndoCommand}" />
  <KeyBinding Gesture="Ctrl+Y" Command="{Binding RedoCommand}" />
  <KeyBinding Gesture="Ctrl+A" Command="{Binding SelectAllCommand}" />
  <KeyBinding Gesture="Delete" Command="{Binding DeleteCommand}" />
  <KeyBinding Gesture="F5" Command="{Binding RunCommand}" />
</Window.KeyBindings>

// Application-level — always active, even when no window is focused
Application.Current.KeyBindings.Add(new KeyBinding
{
    Gesture = new KeyGesture(Key.D, KeyModifiers.Ctrl | KeyModifiers.Shift),
    Command = new RelayCommand(() => ShowDeveloperTools())
});
```

### Dynamic Key Binding Registration

For plugin systems or user-configurable shortcuts:

```csharp
public sealed class KeyBindingService
{
    private readonly Dictionary<string, KeyBinding> _bindings = new();

    public void Register(string name, KeyGesture gesture, ICommand command)
    {
        var binding = new KeyBinding
        {
            Gesture = gesture,
            Command = command
        };

        // Add to every open window
        foreach (var window in Application.Current?.Windows ?? Array.Empty<Window>())
        {
            window.KeyBindings.Add(binding);
        }

        // Also add to Application for non-window contexts
        Application.Current?.KeyBindings.Add(binding);

        _bindings[name] = binding;
    }

    public void Unregister(string name)
    {
        if (_bindings.TryGetValue(name, out var binding))
        {
            foreach (var window in Application.Current?.Windows ?? Array.Empty<Window>())
                window.KeyBindings.Remove(binding);
            Application.Current?.KeyBindings.Remove(binding);
            _bindings.Remove(name);
        }
    }

    public void Remap(string name, KeyGesture newGesture)
    {
        if (_bindings.TryGetValue(name, out var binding))
            binding.Gesture = newGesture;
    }
}
```

---

## 6. Roving Tab Stops

### Toolbar with Roving TabStop Pattern

In a toolbar, only the active tool button should be a tab stop. When the user navigates into the toolbar (via Tab), focus goes to the active tool. Arrow keys move between tools. Tab leaves the toolbar entirely.

```csharp
public sealed partial class ToolBarViewModel : ObservableObject
{
    private readonly List<ToolButton> _tools = new();

    [ObservableProperty]
    private int _activeToolIndex;

    public IReadOnlyList<ToolButton> Tools => _tools;

    public ToolBarViewModel()
    {
        _tools.Add(new ToolButton("Select", "select"));
        _tools.Add(new ToolButton("Move", "move"));
        _tools.Add(new ToolButton("Rotate", "rotate"));
        _tools.Add(new ToolButton("Scale", "scale"));
    }

    public void UpdateTabStops()
    {
        foreach (var tool in _tools)
            tool.IsTabStop = false;
        if (_activeToolIndex >= 0 && _activeToolIndex < _tools.Count)
            _tools[_activeToolIndex].IsTabStop = true;
    }

    [RelayCommand]
    private void ActivateTool(string toolId)
    {
        var index = _tools.FindIndex(t => t.Id == toolId);
        if (index >= 0)
        {
            _activeToolIndex = index;
            UpdateTabStops();
        }
    }

    [RelayCommand]
    private void HandleToolBarKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Left:
                ActivatePreviousTool();
                e.Handled = true;
                break;
            case Key.Right:
                ActivateNextTool();
                e.Handled = true;
                break;
        }
    }

    private void ActivateNextTool()
    {
        _activeToolIndex = (_activeToolIndex + 1) % _tools.Count;
        UpdateTabStops();
    }

    private void ActivatePreviousTool()
    {
        _activeToolIndex = (_activeToolIndex - 1 + _tools.Count) % _tools.Count;
        UpdateTabStops();
    }
}

public sealed class ToolButton : ObservableObject
{
    public string Name { get; }
    public string Id { get; }

    [ObservableProperty]
    private bool _isTabStop;

    [ObservableProperty]
    private bool _isActive;

    public ToolButton(string name, string id)
    {
        Name = name;
        Id = id;
    }
}
```

### XAML for Roving TabStop Toolbar

```xml
<ToolBar KeyboardNavigation.TabNavigation="Once"
         KeyDown="OnToolBarKeyDown">
  <ItemsControl Items="{Binding Tools}">
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <ToggleButton Content="{Binding Name}"
                      IsChecked="{Binding IsActive}"
                      KeyboardNavigation.IsTabStop="{Binding IsTabStop}"
                      Command="{Binding $parent[ItemsControl].DataContext.ActivateToolCommand}"
                      CommandParameter="{Binding Id}"
                      ToolTip.Tip="{Binding Name}" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</ToolBar>
```

---

## 7. FocusScope and Modal Dialogs

### Creating a Modal Focus Trap

For custom dialogs or popups that must trap focus:

```csharp
public sealed class FocusTrap : IDisposable
{
    private readonly Control _scope;
    private readonly Control _firstElement;
    private readonly Control _lastElement;

    public FocusTrap(Control scope, Control first, Control last)
    {
        _scope = scope;
        _firstElement = first;
        _lastElement = last;

        // Intercept Tab and Shift+Tab at the scope boundary
        scope.PreviewKeyDown += OnPreviewKeyDown;

        // Focus the first element
        _firstElement.Focus();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && e.Key == Key.Tab)
        {
            if (FocusManager.GetFocusManager(_scope)?.Current == _firstElement)
            {
                _lastElement.Focus();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Tab)
        {
            if (FocusManager.GetFocusManager(_scope)?.Current == _lastElement)
            {
                _firstElement.Focus();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            // Close the dialog
            CloseRequested?.Invoke();
            e.Handled = true;
        }
    }

    public event Action? CloseRequested;

    public void Dispose()
    {
        _scope.PreviewKeyDown -= OnPreviewKeyDown;
    }
}
```

---

## 8. Testing Keyboard Navigation

```csharp
[TestClass]
public sealed class FocusNavigationTests
{
    [TestMethod]
    public void MoveFocusNext_MovesToNextTabIndex()
    {
        var stack = new StackPanel { KeyboardNavigation.TabNavigation = KeyboardNavigationMode.Local };
        var first = new TextBox { KeyboardNavigation.TabIndex = 10 };
        var second = new TextBox { KeyboardNavigation.TabIndex = 20 };
        stack.Children.Add(first);
        stack.Children.Add(second);

        first.Focus();
        Assert.IsTrue(first.IsFocused);

        FocusNavigation.MoveFocusNext(first);
        Assert.IsTrue(second.IsFocused);
    }

    [TestMethod]
    public void FocusFirst_FocusesFirstElementInScope()
    {
        var window = new Window();
        var panel = new StackPanel();
        var first = new TextBox { KeyboardNavigation.TabIndex = 10 };
        var second = new TextBox { KeyboardNavigation.TabIndex = 20 };
        panel.Children.Add(first);
        panel.Children.Add(second);
        window.Content = panel;

        window.Show();
        FocusNavigation.FocusFirst(panel);
        Assert.IsTrue(first.IsFocused);
        window.Close();
    }
}
```

---

## Summary: Core vs. Verbose

| Concept | Core | Verbose |
|---|---|---|
| TabNavigation values | Basic table | Detailed behavior table with use-case guidance |
| FocusNavigation | 3 methods | Full set with `First`, `Last`, `TryFocus` |
| Tab order | Simple example | Priority rules, `IsTabStop`, `Cycle` wrap |
| Arrow-key handler | Single `switch` | Full keyboard model: Home/End/F2/Insert/Delete |
| Key bindings | Window + App level | Priority chain, dynamic registration, remapping |
| Roving tab stop | One-line example | Full ViewModel + XAML with update logic |
| Focus trap | — | `FocusTrap` implementation for modal dialogs |
| Testing | — | Unit tests with `[TestClass]` |
