---
tier: intermediate
topic: layout
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 058-scrollviewer-scrollbar.md
---

# 058V — ScrollViewer & ScrollBar: An In-Depth Companion

**Why this exists:** The original tutorial covers the core `ScrollViewer` and `ScrollBar` APIs. This companion explores the scrolling engine internals, scroll anchoring, deferred scrolling performance, `ScrollBar` template parts, virtualizing scroll hosts, platform scrollbar behavior, and the WPF comparison.

**Cross-reference:** Original tutorial at [058-scrollviewer-scrollbar.md](058-scrollviewer-scrollbar.md).

---

## 1. Scrolling engine internals

`ScrollViewer` manages three key measurements:

```
Extent  = total size of the content (ScrollViewer.Extent)
Viewport = size of the visible area (ScrollViewer.Viewport)
Offset   = current scroll position  (ScrollViewer.Offset)
```

The scrollable range is `Extent - Viewport`. When `Offset` is 0, the top-left of the content is visible. When `Offset.Y == Extent.Height - Viewport.Height`, the bottom is reached.

### Layout cycle

1. `ScrollViewer` measures its child with infinite space in the scroll direction
2. Child returns its desired size (the `Extent`)
3. `ScrollViewer` sets its `Viewport` from its own available size
4. `Offset` is clamped to `Max(0, Extent - Viewport)`
5. Child is arranged at `-Offset` (shifted up/left by the scroll amount)

---

## 2. Scroll anchoring

Avalonia supports scroll anchoring: when content is added above the current viewport, the view position adjusts to keep the visible content stable.

`ScrollViewer.RegisterAnchorCandidate(Visual)` registers a child as a potential anchor:

```csharp
// The ScrollViewer automatically registers some children as anchors.
// You can manually call RegisterAnchorCandidate for custom scenarios.
```

Anchoring is particularly important in chat or log UIs where new content appears at the top.

---

## 3. IsDeferredScrollingEnabled

When `true`, dragging the scrollbar thumb does not update the content until the user releases the thumb:

```xml
<ScrollViewer IsDeferredScrollingEnabled="True">
  <ItemsControl ItemsSource="{Binding HeavyItems}" />
</ScrollViewer>
```

Useful when scrolling triggers expensive layout or rendering. The content jumps to the final position instead of updating continuously during drag.

---

## 4. Virtualizing scroll hosts

Controls like `ListBox`, `DataGrid`, and `ItemsRepeater` have built-in virtualization. They reuse item containers as the user scrolls, rather than creating elements for every item.

`ScrollViewer` itself does NOT virtualize — it scrolls whatever content is placed inside it. To get virtualization, use a virtualizing control:

| Control | Virtualization |
|---------|---------------|
| `ListBox` | Virtual by default |
| `DataGrid` | Virtual by default |
| `ItemsRepeater` | Virtual with `VirtualizingStackLayout` or `VerticalVirtualizingLayout` |
| `ItemsControl` | NOT virtual (wrap in `ScrollViewer` only for small sets) |
| `StackPanel` in `ScrollViewer` | NOT virtual |

### ScrollViewer as a virtualizing host

When a virtualizing panel is inside a `ScrollViewer`, the panel queries the `ScrollViewer` for the current `Viewport` and `Offset` to determine which items to realize. The `ScrollViewer` acts as the scroll host.

---

## 5. ScrollBar template parts

The `ScrollBar` consists of template parts:

| Part | Type | Description |
|------|------|-------------|
| `PART_Track` | `Track` | The track containing the thumb |
| `PART_Thumb` | `Thumb` | Draggable thumb |
| `PART_LineUpButton` / `PART_LineDownButton` | `RepeatButton` | Arrow buttons |
| `PART_PageUpButton` / `PART_PageDownButton` | `RepeatButton` | Track click areas |

You can restyle these parts to create custom scrollbar appearances:

```xml
<Style Selector="ScrollBar">
  <Setter Property="Template">
    <ControlTemplate>
      <Grid>
        <!-- Custom scrollbar look -->
      </Grid>
    </ControlTemplate>
  </Setter>
</Style>
```

---

## 6. ScrollViewer attached properties

Several `ScrollViewer` properties are available as attached properties, so you can set them on child controls (especially inside `ItemsControl` templates):

```xml
<ListBox ScrollViewer.IsScrollChainingEnabled="False"
         ScrollViewer.BringIntoViewOnFocusChange="False">
```

| Attached Property | Applies to |
|------------------|------------|
| `ScrollViewer.IsScrollChainingEnabled` | Any scrollable control |
| `ScrollViewer.BringIntoViewOnFocusChange` | Any scrollable control |
| `ScrollViewer.HorizontalScrollBarVisibility` | ItemsControl, ListBox, TextBox, etc. |
| `ScrollViewer.VerticalScrollBarVisibility` | ItemsControl, ListBox, TextBox, etc. |
| `ScrollViewer.IsDeferredScrollingEnabled` | ItemsControl, ListBox, etc. |

---

## 7. Scroll snap points

Scroll snap points make the `ScrollViewer` "snap" to content boundaries, useful for carousels:

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Disabled">
  <StackPanel Orientation="Horizontal" Spacing="16">
    <Border Width="300" Height="200" CornerRadius="8" />
    <Border Width="300" Height="200" CornerRadius="8" />
    <Border Width="300" Height="200" CornerRadius="8" />
  </StackPanel>
</ScrollViewer>
```

The `ScrollViewer` automatically snaps to the nearest element boundary when scrolling finishes. Snap behavior is controlled by the `ScrollSnapPointsType` and `ScrollSnapPointsAlignment` properties.

---

## 8. Sticky header layout pattern

Use a `Grid` to keep headers fixed while content scrolls:

```xml
<Grid RowDefinitions="Auto,*">
  <Border Grid.Row="0" Background="White" Padding="16"
          ZIndex="1" BoxShadow="0 2 4 0 #20000000">
    <TextBlock Text="Fixed Header" FontWeight="Bold" />
  </Border>
  <ScrollViewer Grid.Row="1">
    <StackPanel Spacing="8" Margin="16">
      <!-- Content -->
    </StackPanel>
  </ScrollViewer>
</Grid>
```

`ZIndex` on the header ensures it renders above content if they overlap.

---

## 9. Avalonia vs WPF ScrollViewer

| Concept | Avalonia | WPF |
|---------|----------|-----|
| ScrollViewer base | `ScrollViewer` control | `ScrollViewer` control |
| ScrollBarVisibility values | Same (`Auto`, `Visible`, `Hidden`, `Disabled`) | Same |
| ScrollChanged event | `ScrollChanged` event | `ScrollChanged` event |
| Offset type | `Vector` (struct) | `Point` (WPF uses `HorizontalOffset`/`VerticalOffset`) |
| Programmatic scroll | `Offset = new Vector(x, y)` | `ScrollToHorizontalOffset()` / `ScrollToVerticalOffset()` |
| Attached properties | `ScrollViewer.IsScrollChainingEnabled` | `ScrollViewer.IsScrollChainingEnabled` (similar) |
| Inertia | `IsScrollInertiaEnabled` (default `true`) | `IsScrollInertiaEnabled` (WPF 4.5+) |
| Deferred scrolling | `IsDeferredScrollingEnabled` | `IsDeferredScrollingEnabled` |
| Scroll anchoring | Built-in anchor candidates | `ScrollViewer.CanContentScroll` + virtualization |
| Snap points | `ScrollSnapPointsType` | Not built-in |
| Touch scrolling | Enabled by default | Requires `ScrollViewer.PanningMode` |

The largest practical difference: Avalonia's `Offset` is a `Vector`, while WPF splits it into `HorizontalOffset` and `VerticalOffset`. Avalonia's snap points and scroll anchoring are also more refined.

---

## 10. Platform scrollbar differences

| Platform | Visual |
|----------|--------|
| Windows | Classic overlay thumb or full bar (system setting) |
| macOS | Auto-hiding overlay thumb only |
| Linux | GTK-style overlay or themed bar (DE-dependent) |
| WASM | Browser-native scrollbar |

`AllowAutoHide` controls overlay behavior on platforms that support it.

---

## See Also

- [058 — ScrollViewer & ScrollBar (core tutorial)](058-scrollviewer-scrollbar.md)
- [058E — ScrollViewer & ScrollBar (examples)](058-scrollviewer-scrollbar-examples.md)
- [Avalonia Docs: ScrollViewer How-To](https://docs.avaloniaui.net/docs/how-to/scrollviewer-how-to)
