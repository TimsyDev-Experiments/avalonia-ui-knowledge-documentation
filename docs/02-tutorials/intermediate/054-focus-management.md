---
tier: intermediate
topic: focus
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 054 — Focus Management

**What you'll learn:** How keyboard focus works in Avalonia — focusing controls programmatically, tab navigation, focus scopes, and responding to focus changes with routed events.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md), [051 — Routed Events](051-routed-events.md)

---

## 1. The Focus method

Call `Focus()` on any `InputElement` to give it keyboard focus:

```csharp
myButton.Focus();
```

Returns `false` if the control is not visible or `Focusable` is `false`.

### Focusable property

Controls are focusable by default (`Focusable="True"`). To exclude a control from focus:

```xml
<Button Content="Read-only" Focusable="False" />
```

---

## 2. FocusManager

Access the global focus manager from a `TopLevel` (e.g., `Window`):

```csharp
var focusManager = TopLevel.GetTopLevel(myControl)?.FocusManager;

// Get the currently focused element
IInputElement? focused = focusManager?.GetFocusedElement();

// Clear focus
focusManager?.ClearFocus();

// Move focus programmatically
focusManager?.TryMoveFocus(NavigationDirection.Next);
```

`TryMoveFocus` supports `Next`, `Previous`, `Up`, `Down`, `Left`, `Right` directions.

---

## 3. Tab navigation

Controls are navigated in visual-tree order by default. Override with `TabIndex`:

```xml
<StackPanel>
  <TextBox TabIndex="2" Watermark="Second" />
  <TextBox TabIndex="1" Watermark="First" />
  <TextBox TabIndex="3" Watermark="Third" />
</StackPanel>
```

### IsTabStop

Exclude an individual control from tab navigation without affecting its ability to receive focus programmatically:

```xml
<TextBox IsTabStop="False" />
```

The control can still be focused via code or mouse click, but Tab skips over it.

---

## 4. Focus scopes

A focus scope creates a tab-navigation boundary. When focus reaches the last element in a scope, pressing Tab moves to the first element of the next scope rather than the next element in tree order.

```xml
<StackPanel>
  <!-- Scope 1 -->
  <TabControl Focusable="False">
    <TabItem Header="A">
      <TextBox Name="a1" />
      <TextBox Name="a2" />
    </TabItem>
  </TabControl>

  <!-- Scope 2 -->
  <GroupBox>
    <TextBox Name="b1" />
  </GroupBox>
</StackPanel>
```

Common controls that act as focus scopes: `TabControl`, `GroupBox`, `ListBox`, `ScrollViewer`. You can mark any element as a focus scope by setting the `Focusable` property and handling its behavior.

---

## 5. Focus events

| Event | EventArgs | Fires |
|-------|-----------|-------|
| `GotFocusEvent` | `GotFocusEventArgs` | After the element receives focus |
| `LostFocusEvent` | `LostFocusEventArgs` | After the element loses focus |
| `GettingFocusEvent` | `GettingFocusEventArgs` | Before focus changes (cancelable) |
| `LosingFocusEvent` | `LosingFocusEventArgs` | Before focus is lost (cancelable) |

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);

    if (change.Property == InputElement.IsFocusedProperty)
    {
        if (change.GetNewValue<bool>())
            OnGotFocus();
        else
            OnLostFocus();
    }
}
```

### GotFocus / LostFocus as routed events

```csharp
// Handle on any parent
myPanel.AddHandler(InputElement.GotFocusEvent, (s, e) =>
{
    var focused = e.Source as Control;
    StatusBar.Text = $"Focused: {focused?.Name}";
});
```

### GettingFocus — cancel focus change

The preview event lets you intercept and cancel:

```csharp
myControl.AddHandler(InputElement.GettingFocusEvent, (s, e) =>
{
    if (e.Source is TextBox && !IsEditable)
        e.Cancel = true;  // prevent focus
}, RoutingStrategies.Tunnel);
```

---

## 6. IsKeyboardFocusWithin

Checks whether keyboard focus is anywhere within the element or its subtree:

```csharp
if (myPanel.IsKeyboardFocusWithin)
{
    // Some child of myPanel has focus
}
```

Useful for styling parent elements when a child is focused.

---

## 7. Programmatic focus with TopLevel.FocusManager

```csharp
// From any control, get the TopLevel and its FocusManager
var topLevel = TopLevel.GetTopLevel(this);
var manager = topLevel?.FocusManager;

// Find next focusable element
var next = manager?.FindNextElement(NavigationDirection.Next);

// Focus it
if (next is not null)
    manager?.Focus(next);
```

---

## Key Takeaways

- `control.Focus()` to give focus; check `Focusable` and `IsVisible`
- `FocusManager` for global queries (`GetFocusedElement`, `TryMoveFocus`)
- `TabIndex` and `IsTabStop` control tab order
- Focus scopes create tab-navigation boundaries (TabControl, GroupBox, etc.)
- `GotFocusEvent` / `LostFocusEvent` for reactive focus tracking
- `GettingFocusEvent` / `LosingFocusEvent` for canceling focus changes

---

## See Also

- [054V — Focus Management (verbose companion)](054-focus-management-verbose.md)
- [054E — Focus Management (examples)](054-focus-management-examples.md)
- [Avalonia Docs: Focus](https://docs.avaloniaui.net/docs/input-interaction/focus)
- [Avalonia Docs: FocusManager](https://docs.avaloniaui.net/docs/services/focus-manager)
- [Avalonia Docs: InputElement API](https://docs.avaloniaui.net/api/avalonia/input/inputelement)
