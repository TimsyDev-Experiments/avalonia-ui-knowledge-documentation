---
tier: intermediate
topic: layout
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 058 — ScrollViewer & ScrollBar

**What you'll learn:** How to use `ScrollViewer` for scrollable content, control scrollbar visibility, scroll programmatically, handle nested scrolling, and use the standalone `ScrollBar` primitive.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md), [056 — Input Events](056-input-events.md)

---

## 1. ScrollViewer basics

Wrap content that may exceed the available space:

```xml
<ScrollViewer>
  <StackPanel Spacing="8">
    <TextBlock Text="Item 1" />
    <TextBlock Text="Item 2" />
    <!-- ... -->
  </StackPanel>
</ScrollViewer>
```

`ScrollViewer` shows scrollbars automatically when content overflows.

**Important:** A `ScrollViewer` cannot be inside a panel that offers infinite space in the scroll direction (e.g., a `StackPanel`). The scroll direction's parent must have constrained size — use a `Grid`, `DockPanel`, or a fixed `Height`/`MaxHeight`.

---

## 2. ScrollBarVisibility

Each axis has a `ScrollBarVisibility` value:

| Value | Behavior |
|-------|----------|
| `Auto` | Show only when content overflows (vertical default) |
| `Visible` | Always show, even if content fits |
| `Hidden` | Hide bar but allow scroll via touch/wheel/keyboard |
| `Disabled` | Disable scrolling entirely (horizontal default) |

```xml
<ScrollViewer VerticalScrollBarVisibility="Auto"
              HorizontalScrollBarVisibility="Disabled">
  <TextBlock Text="{Binding LongText}" TextWrapping="Wrap" />
</ScrollViewer>
```

---

## 3. Key properties

| Property | Type | Description |
|----------|------|-------------|
| `Offset` | `Vector` | Current scroll position (X, Y) |
| `Extent` | `Size` | Total size of scrollable content |
| `Viewport` | `Size` | Size of visible area |
| `ScrollBarMaximum` | `double` | `Extent - Viewport` (max scroll distance) |
| `AllowAutoHide` | `bool` | Auto-hide bars when inactive (default `true`) |
| `IsScrollChainingEnabled` | `bool` | Chain scroll to parent when reaching limit |
| `IsScrollInertiaEnabled` | `bool` | Enable scroll inertia (default `true`) |
| `BringIntoViewOnFocusChange` | `bool` | Auto-scroll focused child into view (default `true`) |
| `IsDeferredScrollingEnabled` | `bool` | Update only on thumb release (performance) |

---

## 4. Programmatic scrolling

### Set offset directly

```csharp
scrollViewer.Offset = new Vector(0, 500); // Scroll to Y = 500
```

### Scroll to top/bottom

```csharp
// Top
scrollViewer.Offset = new Vector(scrollViewer.Offset.X, 0);

// Bottom
scrollViewer.Offset = new Vector(
    scrollViewer.Offset.X,
    scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
```

### Bring a child into view

```csharp
targetControl.BringIntoView();

// With a specific rectangle
targetControl.BringIntoView(
    new Rect(0, 0, targetControl.Bounds.Width, targetControl.Bounds.Height));
```

`BringIntoView` works with virtualized panels — the item is materialized and then scrolled to.

### Scroll methods

```csharp
scrollViewer.LineDown();  // One line down
scrollViewer.PageDown();  // One viewport down
scrollViewer.ScrollToEnd();   // Bottom
scrollViewer.ScrollToHome();  // Top
```

---

## 5. ScrollChanged event

React to scroll position changes:

```csharp
scrollViewer.ScrollChanged += (s, e) =>
{
    double bottomThreshold = sv.Extent.Height - sv.Viewport.Height - 1;
    if (sv.Offset.Y >= bottomThreshold)
    {
        LoadMoreItems();
    }
};
```

Observe the offset reactively:

```csharp
scrollViewer.GetObservable(ScrollViewer.OffsetProperty)
    .Subscribe(offset => UpdateStatus(offset.Y));
```

---

## 6. Nested ScrollViewers

When nesting scrollable content, disable the inner scroll direction that the outer already handles:

```xml
<ScrollViewer>
  <StackPanel Spacing="16">
    <ScrollViewer HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Disabled">
      <StackPanel Orientation="Horizontal" Spacing="8">
        <Border Width="200" Height="150" Background="Red" />
        <Border Width="200" Height="150" Background="Blue" />
      </StackPanel>
    </ScrollViewer>
  </StackPanel>
</ScrollViewer>
```

Control scroll chaining:

```xml
<ListBox ScrollViewer.IsScrollChainingEnabled="False"
         Height="200" ItemsSource="{Binding Items}" />
```

---

## 7. Standalone ScrollBar

`ScrollBar` is a primitive control for custom scrolling when `ScrollViewer` is not appropriate.

```xml
<ScrollBar Orientation="Vertical"
           Minimum="0" Maximum="100"
           Value="{Binding ScrollValue}"
           ViewportSize="20"
           SmallChange="1" LargeChange="10" />
```

| Property | Description |
|----------|-------------|
| `Minimum` / `Maximum` | Value range (default 0–100) |
| `Value` | Current position |
| `ViewportSize` | Thumb size (ratio of visible to total) |
| `SmallChange` | Arrow key step |
| `LargeChange` | Track click step |

---

## Key Takeaways

- Wrap overflow content in `ScrollViewer` — constrained parent required
- `ScrollBarVisibility`: `Auto`, `Visible`, `Hidden`, `Disabled`
- Programmatic scroll: `Offset`, `BringIntoView`, `ScrollToEnd`/`ScrollToHome`
- `ScrollChanged` event for infinite loading and position tracking
- Nesting: disable inner direction matching outer; use `IsScrollChainingEnabled`
- Standalone `ScrollBar` for custom scroll UI

---

## See Also

- [058V — ScrollViewer & ScrollBar (verbose companion)](058-scrollviewer-scrollbar-verbose.md)
- [058E — ScrollViewer & ScrollBar (examples)](058-scrollviewer-scrollbar-examples.md)
- [Avalonia Docs: ScrollViewer How-To](https://docs.avaloniaui.net/docs/how-to/scrollviewer-how-to)
- [Avalonia Docs: ScrollViewer API](https://docs.avaloniaui.net/api/avalonia/controls/scrollviewer)
