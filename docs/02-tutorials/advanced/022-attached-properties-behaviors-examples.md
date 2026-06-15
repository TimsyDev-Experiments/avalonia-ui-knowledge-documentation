---
tier: advanced
topic: extensibility
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 022-attached-properties-behaviors.md
---

# 022E — Attached Properties & Behaviors: Real-World Examples

**Applies to:** [022 — Attached Properties & Behaviors](022-attached-properties-behaviors.md) | [022V — In-Depth Companion](022-attached-properties-behaviors-verbose.md)

---

## Example 1: DragToReorderBehavior

### Goal

An attached behavior that enables drag-to-reorder on any `ItemsControl` or `ListBox`. The user drags an item vertically to a new position. During drag, a visual insertion indicator shows where the item will land. On drop, the behavior invokes a command with the old and new index.

### ViewModel

```csharp
// ViewModels/PlaylistViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
    public ObservableCollection<string> Songs { get; } =
    [
        "Song A",
        "Song B",
        "Song C",
        "Song D",
    ];

    [RelayCommand]
    private void ReorderItem((int OldIndex, int NewIndex) args)
    {
        Songs.Move(args.OldIndex, args.NewIndex);
    }
}
```

### XAML View

```xml
<!-- Views/PlaylistView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:attached="using:MyApp.Attached"
             x:DataType="vm:PlaylistViewModel">
  <ListBox ItemsSource="{Binding Songs}"
           attached:DragToReorderBehavior.IsEnabled="True"
           attached:DragToReorderBehavior.ReorderCommand="{Binding ReorderItemCommand}">
    <ListBox.ItemTemplate>
      <DataTemplate x:DataType="x:String">
        <TextBlock Text="{Binding}" Padding="8,4" />
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</UserControl>
```

### How It Works

1. `DragToReorderBehavior` defines two attached properties: `IsEnabledProperty` (`bool`) and `ReorderCommandProperty` (`ICommand?`).
2. The `IsEnabledProperty.Changed` handler (via `AddClassHandler<ItemsControl>`) subscribes to or unsubscribes from the control's `PointerPressed`, `PointerMoved`, and `PointerReleased` events.
3. On `PointerPressed`, the behavior records the item under the cursor using `e.GetPosition` and `ItemsControl.ContainerFromItem`. It clones the item's visual (a `Border` snapshot) and creates a floating overlay.
4. On `PointerMoved`, the behavior updates the overlay position and computes the target insertion index by iterating the item containers and comparing midpoints. It shows an insertion line (a thin `Rectangle` added to the control's `AdornerLayer`).
5. On `PointerReleased`, the behavior executes `ReorderCommand` with a tuple `(oldIndex, newIndex)`, removes the overlay and insertion line, and releases the pointer capture.

### Design Decisions

- **`AdornerLayer` for the insertion indicator.** The `AdornerLayer` renders above all other content without affecting layout. The behavior adds a `Rectangle` as an adorner and removes it on drop.
- **`Pointer.Capture` for reliable drag tracking.** Captured on the `ItemsControl` during `PointerPressed`. Ensures all subsequent pointer events reach the behavior even if the cursor leaves the control bounds.
- **Command over direct collection mutation.** The behavior does not touch the `ItemsSource` directly. Instead it invokes the command with positional data, leaving the ViewModel responsible for the actual `Move` operation. This preserves undo/redo opportunities and side-effect logging.

### Edge Cases

- **Drag starts but item is not under cursor.** Check if `ContainerFromItem` returns non-null. If null, cancel the drag (the user clicked on empty space).
- **Items change during drag.** The behavior caches the initial item count and container positions on `PointerPressed`. If the collection changes mid-drag (via background update), the computed indices may be stale. Disable background updates during drag or re-query containers each frame.
- **Drag to same position.** If `oldIndex == newIndex`, skip command execution entirely.
- **Scroll during drag.** The behavior does not auto-scroll. Production implementations should call `ScrollIntoView` on edge proximity and adjust the insertion index accordingly.
- **Multiple drag behaviors on the same control.** Guard via a static `HashSet<ItemsControl>` that prevents double-subscription. Log a warning if an attempt is made.

---

## Example 2: LongPressBehavior

### Goal

An attached behavior that detects a long press (touch or pointer held down for a configurable duration) on any `Control`. When the threshold is reached, the behavior executes a command and optionally shows a visual ripple effect.

### ViewModel

```csharp
// ViewModels/GalleryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isContextMenuOpen;

    [RelayCommand]
    private void ShowContextMenu(string itemId)
    {
        IsContextMenuOpen = true;
        // Show context menu positioned at the press location
    }
}
```

### XAML View

```xml
<!-- Views/GalleryView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:attached="using:MyApp.Attached"
             x:DataType="vm:GalleryViewModel">
  <ItemsControl ItemsSource="{Binding Items}">
    <ItemsControl.ItemTemplate>
      <DataTemplate x:DataType="vm:MediaItemViewModel">
        <Border Width="200" Height="200" Margin="4"
                Background="{Binding ThumbnailBg}">
          <attached:LongPressBehavior.Duration="500"
                      LongPressBehavior.Command="{Binding $parent[ItemsControl].DataContext.ShowContextMenuCommand}"
                      LongPressBehavior.CommandParameter="{Binding Id}" />
        </Border>
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</UserControl>
```

### How It Works

1. `LongPressBehavior` defines: `DurationProperty` (`int`, milliseconds, default 500), `CommandProperty` (`ICommand?`), and `CommandParameterProperty` (`object?`).
2. The `DurationProperty.Changed` handler subscribes to the control's `PointerPressed`, `PointerMoved`, `PointerReleased`, and `PointerCaptureLost` events.
3. On `PointerPressed`, the behavior starts a `DispatcherTimer` with the configured `Duration`. It stores the starting position.
4. On `PointerMoved` during the hold, the behavior checks if the pointer has moved more than a threshold (10px). If so, it cancels the timer (the user is scrolling or dragging, not long-pressing).
5. When the timer fires, the behavior sets `e.Handled = true` to prevent the click event from also firing, executes the command with the parameter, and optionally triggers a brief opacity pulse (visual feedback).
6. On `PointerReleased` before the timer fires, the behavior cancels the timer and does nothing — this was a normal tap.

### Design Decisions

- **`DispatcherTimer` over `Task.Delay`.** `DispatcherTimer` runs on the UI thread and can be cancelled synchronously. `Task.Delay` with cancellation would require async disposal and cannot be stopped as cleanly in pointer event handlers.
- **Movement threshold of 10px.** Prevents long-press from triggering during scroll or pan. The threshold matches common touch framework conventions (Android uses ~8px, iOS uses ~10px). Make it configurable via a `ThresholdProperty` for power users.
- **`PointerCaptureLost` subscription.** Handles edge cases where the pointer is captured by another element (e.g., a scroll viewer starts a scroll gesture). The behavior cancels the timer and resets state.

### Edge Cases

- **Control is removed from visual tree during hold.** The behavior subscribes to `DetachedFromVisualTree` and cancels the timer. Without this, the timer would fire on a detached control and attempt to execute a command on a stale data context.
- **Multiple fingers on a touch device.** Each `PointerPressed` creates a new timer. Track the pointer ID (`e.Pointer.Id`) and only respond to the first pointer. Ignore additional concurrent presses.
- **Rapid tap-and-hold after a scroll.** The `PointerReleased` from the scroll cancels any previous timer state. The next `PointerPressed` starts a fresh timer.
- **Command is null.** The behavior still shows the visual ripple effect but does not invoke a command. This allows the behavior to be used purely for visual feedback.

---

## What These Examples Demonstrate

| Aspect | DragToReorderBehavior | LongPressBehavior |
|---|---|---|
| **Trigger** | Drag gesture (press + move + release) | Duration-based hold (press + wait) |
| **Events subscribed** | `PointerPressed`, `PointerMoved`, `PointerReleased` | `PointerPressed`, `PointerMoved`, `PointerReleased`, `PointerCaptureLost` |
| **State management** | Tracks old index, overlay, insertion line | DispatcherTimer, start position, pointer ID |
| **Visual feedback** | Floating overlay + insertion line | Opacity pulse (ripple) |
| **Cancellation** | PointerCaptureLost or move outside bounds | Movement beyond threshold, pointer capture lost |
| **Output** | `ICommand` with `(int OldIndex, int NewIndex)` | `ICommand` with arbitrary parameter |

---

## See Also

- [022 — Attached Properties & Behaviors](022-attached-properties-behaviors.md)
- [022V — Attached Properties & Behaviors (verbose companion)](022-attached-properties-behaviors-verbose.md)
- [022 — Attached Properties & Behaviors](022-attached-properties-behaviors.md) — covers property system details inline
- [026 — Accessibility & Automation](026-accessibility-automation.md) — `AutomationProperties` as an attached-property family
- [Avalonia Docs: Attached Properties](https://docs.avaloniaui.net/docs/data-binding/attached-properties)
