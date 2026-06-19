---
tier: intermediate
topic: focus
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 054-focus-management.md
---

# 054V — Focus Management: An In-Depth Companion

**Why this exists:** The original tutorial covers the core focus API. This companion explains focus scope internals, the `GettingFocus`/`LosingFocus` cancelable events, `IFocusManager` implementation details, arrow-key navigation in lists, focus visual styles, programmatic focus with `FindNextElement`, and how Avalonia's focus system differs from WPF's.

**Cross-reference:** Original tutorial at [054-focus-management.md](054-focus-management.md).

---

## 1. How focus works internally

Focus in Avalonia is managed by the `FocusManager` class, which implements `IFocusManager`. Each `TopLevel` (Window, etc.) has its own `FocusManager` instance.

### Focus scope chain

```
Window (TopLevel)
  └── FocusManager
       └── FocusScope (StackPanel)
            └── FocusScope (TabControl)
                 ├── TabItem (active)
                 │    └── TextBox ← focused element
                 └── TabItem (inactive)
```

The focus manager tracks:
- **Focused element** — the `IInputElement` that currently has keyboard focus
- **Focus scopes** — elements that bound tab-navigation regions

When the user presses Tab, the focus manager walks the visual tree from the focused element, respecting scope boundaries, to find the next `Focusable` element with `IsTabStop = true`.

### NavigationDirection values

```csharp
public enum NavigationDirection
{
    Next,      // Tab (forward)
    Previous,  // Shift+Tab (backward)
    First,     // Ctrl+Home
    Last,      // Ctrl+End
    Left,      // Arrow key
    Right,     // Arrow key
    Up,        // Arrow key
    Down,      // Arrow key
}
```

`Next` and `Previous` use tab order (visual tree + TabIndex). Directionals (Left/Right/Up/Down) use spatial navigation within the current scope.

---

## 2. GettingFocus and LosingFocus events

These preview events fire **before** focus changes, letting you cancel or redirect the operation.

### GettingFocus

```csharp
// Tunnel to intercept before focus moves
myPanel.AddHandler(
    InputElement.GettingFocusEvent,
    (s, e) =>
    {
        // e.NewFocus is the element about to receive focus
        // e.OldFocus is the element about to lose focus
        if (e.NewFocus is TextBox tb && tb.Tag?.ToString() == "restricted")
            e.Cancel = true;
    },
    RoutingStrategies.Tunnel);
```

`GettingFocusEventArgs` properties:

| Property | Type | Description |
|----------|------|-------------|
| `NewFocus` | `IInputElement?` | Element receiving focus |
| `OldFocus` | `IInputElement?` | Element losing focus |
| `Cancel` | `bool` | Set to `true` to prevent the focus change |
| `Direction` | `NavigationDirection` | How focus is moving |
| `Method` | `NavigationMethod` | Tab, arrow, pointer, or programmatic |

### LosingFocus

Same pattern but fires before the current element loses focus:

```csharp
myControl.AddHandler(
    InputElement.LosingFocusEvent,
    (s, e) =>
    {
        if (HasUnsavedChanges)
        {
            // Prompt user before losing focus
            e.Cancel = true;
        }
    },
    RoutingStrategies.Tunnel);
```

### Event order

```
1. LosingFocus (tunnel) — on element losing focus
2. GettingFocus (tunnel) — on element gaining focus
3. LostFocus (bubble)    — on element that lost focus
4. GotFocus (bubble)     — on element that gained focus
```

---

## 3. Auto-focus with OnLoaded

A common pattern is to focus a specific control when a view loads:

```csharp
protected override void OnLoaded(RoutedEventArgs e)
{
    base.OnLoaded(e);
    // Focus the first input field
    UserNameTextBox.Focus();
}
```

`Focus()` on a control that is not yet in the visual tree returns `false`. Always call `Focus()` after the control is loaded (in `OnLoaded` or later).

---

## 4. Arrow-key navigation in lists

`ListBox` and other selection controls handle arrow-key navigation internally. When a `ListBoxItem` has focus, Up/Down arrows move focus to the adjacent item.

### Custom keyboard navigation

For custom panels, handle `KeyDownEvent` and use `TryMoveFocus`:

```csharp
protected override void OnKeyDown(KeyEventArgs e)
{
    base.OnKeyDown(e);

    if (e.Key == Key.Enter)
    {
        var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
        focusManager?.TryMoveFocus(NavigationDirection.Next);
        e.Handled = true;
    }
}
```

### Focus visual styling

Use the `:focus` pseudo-class to style focused elements:

```xml
<Style Selector="TextBox:focus">
  <Setter Property="BorderBrush" Value="{DynamicResource AccentColor}" />
  <Setter Property="BorderThickness" Value="2" />
</Style>
```

Use the `:focus-within` pseudo-class to style a parent when any child is focused:

```xml
<Style Selector="Panel:focus-within">
  <Setter Property="Background" Value="#1AFFFFFF" />
</Style>
```

---

## 5. IFocusManager interface

The `IFocusManager` interface exposes the full focus API. Access it via `TopLevel.FocusManager`:

| Method | Description |
|--------|-------------|
| `GetFocusedElement()` | Current focused `IInputElement` or null |
| `Focus(control)` | Focus a specific control |
| `ClearFocus()` | Remove keyboard focus |
| `TryMoveFocus(direction)` | Move focus in a direction |
| `FindNextElement(direction)` | Query the next element without moving focus |
| `FindFirstFocusableElement()` | First focusable element in the scope |
| `FindLastFocusableElement()` | Last focusable element in the scope |

### FindNextElement options

```csharp
var options = new FindNextElementOptions
{
    // Exclude the currently focused element from the search
    ExcludeCurrent = true,
};

var next = focusManager.FindNextElement(
    NavigationDirection.Next, options);
```

---

## 6. Focus scopes in detail

A focus scope restricts Tab navigation to elements within that scope. When the user presses Tab on the last focusable element inside a scope, focus moves to the first focusable element of the **next** sibling scope — not the next element in flat tree order.

### Built-in focus scopes

| Control | Scope behavior |
|---------|---------------|
| `TabControl` | Tab stays within active tab's content |
| `GroupBox` | Tab cycles within the group |
| `ListBox` | Tab stays within the list items |
| `ScrollViewer` | Tab stays within scrollable content |

### Creating a custom focus scope

Set `Focusable` and handle the `IsTabStop` behavior:

```csharp
public class CustomScope : ContentControl
{
    // Marking as a focus scope requires the control to be Focusable
    // and handling tab navigation within the scope
    static CustomScope()
    {
        FocusableProperty.OverrideDefaultValue<CustomScope>(false);
    }
}
```

For a true custom focus scope, you would need to implement `IFocusScope`.

---

## 7. Listening for global focus changes

Subscribe to the static `GotFocusEvent.Raised` observable for app-wide focus tracking:

```csharp
InputElement.GotFocusEvent.Raised.Subscribe(args =>
{
    var (sender, e) = args;
    Console.WriteLine($"Focus moved to: {e.Source?.GetType().Name}");
});
```

This fires for every `GotFocus` event on any `InputElement` in any window.

---

## 8. Avalonia vs WPF focus

| Concept | Avalonia | WPF |
|---------|----------|-----|
| Focus method | `control.Focus()` | `control.Focus()` |
| Focus events | `GotFocusEvent`, `LostFocusEvent`, `GettingFocusEvent`, `LosingFocusEvent` | `GotFocus`, `LostFocus`, `PreviewGotKeyboardFocus`, `PreviewLostKeyboardFocus` |
| Preview events | Use `RoutingStrategies.Tunnel` with `AddHandler` | Separate `Preview*` routed events |
| Focus scopes | `FocusScope` via `IFocusScope` | `FocusManager.IsFocusScope` attached property |
| FocusManager | `TopLevel.FocusManager` | `Keyboard.FocusedElement` |
| Tab navigation | `IsTabStop`, `TabIndex` on `InputElement` | `IsTabStop`, `TabIndex` on `UIElement` |
| Navigation direction | `NavigationDirection` enum with spatial directions | `FocusNavigationDirection` enum with spatial directions |
| `:focus-within` pseudo-class | Supported natively | Requires custom implementation or `IsKeyboardFocusWithin` binding |

The main practical difference: Avalonia does not have separate `Preview*` CLR events for focus. Instead, the `GettingFocusEvent` / `LosingFocusEvent` are routed events that you subscribe to with `RoutingStrategies.Tunnel`.

---

## See Also

- [054 — Focus Management (core tutorial)](054-focus-management.md)
- [054E — Focus Management (examples)](054-focus-management-examples.md)
- [Avalonia Docs: Focus](https://docs.avaloniaui.net/docs/input-interaction/focus)
- [Avalonia Docs: FocusManager](https://docs.avaloniaui.net/docs/services/focus-manager)
