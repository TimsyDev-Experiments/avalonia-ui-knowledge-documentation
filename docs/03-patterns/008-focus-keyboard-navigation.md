---
tier: advanced
topic: accessibility
estimated: 12 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Pattern: Focus & Keyboard Navigation

**What you'll learn:** A composable pattern for managing focus scope, tab order, arrow-key navigation, and keyboard shortcut routing in complex Avalonia views.

**Prerequisites:** [054 — Focus Management](../02-tutorials/intermediate/054-focus-management.md), [055 — Keyboard & Hotkeys](../02-tutorials/intermediate/055-keyboard-hotkeys.md)

---

## Problem

A complex view (data form, property editor, multi-pane IDE) needs predictable tab order, arrow-key navigation within lists, roving tab stops, and global key bindings that coexist with focused-element bindings.

---

## Solution: FocusScope + KeyboardNavigation

### Tab order

```xml
<StackPanel KeyboardNavigation.TabNavigation="Cycle">
  <TextBox Name="FirstName" KeyboardNavigation.TabIndex="10" />
  <TextBox Name="LastName" KeyboardNavigation.TabIndex="20" />
  <DatePicker KeyboardNavigation.TabIndex="30" />
  <Button Content="Submit" KeyboardNavigation.TabIndex="40" />
</StackPanel>
```

| TabNavigation value | Behavior |
|---|---|
| `Local` (default) | Tab moves through children with TabIndex |
| `Cycle` | After last child, wraps to first |
| `Once` | Tab enters scope, arrow keys navigate inside |
| `Contained` | Tab stays inside scope, does not leave |

### Programmatic focus with FocusScope

```csharp
public static class FocusNavigation
{
    public static void MoveFocusNext(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Next, from, false);

    public static void MoveFocusPrevious(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Previous, from, false);

    public static void MoveFocusDown(Control from) =>
        FocusManager.GetFocusManager(from)?.MoveFocus(
            NavigationDirection.Down, from, false);
}
```

### Custom arrow-key handler for editable list

```csharp
public partial class FocusableListViewModel
{
    [RelayCommand]
    private void HandleKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                MoveSelection(-1);
                e.Handled = true;
                break;
            case Key.Down:
                MoveSelection(1);
                e.Handled = true;
                break;
            case Key.Enter:
                ActivateSelected();
                e.Handled = true;
                break;
        }
    }
}
```

```xml
<ListBox KeyDown="OnListKeyDown"
         KeyboardNavigation.TabNavigation="Contained" />
```

### Global vs local key bindings

```xml
<!-- Window-level (overrides focused control if no handler claims it) -->
<Window.KeyBindings>
  <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveCommand}" />
  <KeyBinding Gesture="Ctrl+Shift+S" Command="{Binding SaveAsCommand}" />
</Window.KeyBindings>
```

Application-level hotkeys (always available):

```csharp
// In App.axaml.cs or Program.cs
var binding = new KeyBinding
{
    Gesture = new KeyGesture(Key.D, KeyModifiers.Ctrl | KeyModifiers.Shift),
    Command = new RelayCommand(() => ShowDeveloperTools())
};
Application.Current.KeyBindings.Add(binding);
```

### Roving tab stop in toolbars

```csharp
// Only the active tool button is focusable; others skip tab
KeyboardNavigation.SetIsTabStop(button, isActive);
```

---

## Benefits

- Tab order is explicit and auditable.
- Focus scope containment prevents tab-from-leaving-lists.
- Arrow-key navigation coexists with mouse use.
- Global hotkeys don't interfere with per-control bindings.
