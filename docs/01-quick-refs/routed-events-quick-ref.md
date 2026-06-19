---
topic: events
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Routed Events Quick Ref

## Routing strategies

| Strategy | Direction | Example |
|---|---|---|
| `Bubble` | Child → parent (up the tree) | `ClickEvent`, `PointerPressedEvent` |
| `Tunnel` | Parent → child (down the tree) | `PreviewKeyDownEvent` |
| `Direct` | Only on the source element | `PropertyChangedEvent` |

## Registering a routed event

```csharp
public static readonly RoutedEvent<YourEventArgs> YourEvent =
    RoutedEvent.Register<YourClass, YourEventArgs>("YourEvent", RoutingStrategies.Bubble);
```

## Common built-in events

| Event | Type | Strategy |
|---|---|---|
| `Click` | `RoutedEventArgs` | Bubble |
| `PointerPressed` / `Released` | `PointerEventArgs` | Bubble |
| `PointerMoved` | `PointerEventArgs` | Bubble |
| `KeyDown` / `KeyUp` | `KeyEventArgs` | Direct |
| `TextInput` | `TextInputEventArgs` | Direct |
| `GotFocus` / `LostFocus` | `GotFocusEventArgs` / `RoutedEventArgs` | Direct |
| `SizeChanged` | `SizeChangedEventArgs` | Direct |
| `Loaded` / `Unloaded` | `RoutedEventArgs` | Direct |

## Adding handlers

```csharp
// Handled events too (v12)
control.AddHandler(Button.ClickEvent, OnClick, handledEventsToo: true);

// Class handler
static MyControl() =>
    MyControl.ClickEvent.AddClassHandler<MyControl>((c, e) => c.OnClick(e));
```

## Marking events as handled

```csharp
e.Handled = true;  // stops bubbling (unless handledEventsToo subscribers)
```
