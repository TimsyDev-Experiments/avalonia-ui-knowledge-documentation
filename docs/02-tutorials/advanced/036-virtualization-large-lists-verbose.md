---
tier: advanced
topic: virtualization
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 036-virtualization-large-lists.md
---

# 036V — Virtualization and Large List Performance: An In-Depth Companion

**Why this exists.** The original tutorial covers how to use `ItemsRepeater` with virtualization. This companion explains how virtualization actually works at the layout level, why `ItemsRepeater` has no built-in selection, what `VirtualizationMode.Recycling` does to container instances, how `ScrollViewer` triggers virtualization, and why the `AsyncVirtualizingCollection` pattern exists.

**Read this alongside:** [036 — Virtualization and Large List Performance](036-virtualization-large-lists.md)

---

## 1. How virtualization works

Virtualization means the UI only creates visual elements for items that are currently visible (or slightly past the visible bounds as a buffer). As the user scrolls, items that leave the viewport are recycled or destroyed, and items entering the viewport are created or reused.

```text
Viewport:  [Item 4 | Item 5 | Item 6 | Item 7]  ← realized in the visual tree
Scrolled:  [Item 8 | Item 9 | Item 10 | Item 11] ← Item 4-7 destroyed/recycled
```

Without virtualization, a list of 100,000 items creates 100,000 `ContentPresenter` instances, each with its own visual subtree. With virtualization, only ~20 instances exist (viewport size + buffer).

**Avalonia's virtualization stack:**

1. `ScrollViewer` provides a scrollable viewport with an extent larger than the viewport.
2. `VirtualizingStackPanel` (or another virtualizing panel) measures only items within the viewport.
3. The panel reports its total extent height (items × item height) to the `ScrollViewer`.
4. When `ScrollViewer.Offset` changes, the panel re-realizes items at the new offset.

---

## 2. ItemsRepeater — why no built-in selection

`ItemsRepeater` is a "pure" virtualizing control — it renders items but adds no interaction logic. It has no `SelectedItem`, `SelectionMode`, or `ItemTemplate` selector. This is by design:

- **Not coupled to a view model pattern:** selection is a UX concept, not a rendering concept. `ItemsRepeater` only handles rendering.
- **Flexibility:** implement single-selection, multi-selection, drag-to-select, or no selection using the same control.
- **Performance:** selection state tracking has overhead. `ListBox` maintains a `Selection` collection, raises events, and applies visual state. If you do not need selection, `ItemsRepeater` avoids that cost.

You add selection via an attached behavior (as shown in the original), a custom wrapper control, or by binding a `IsSelected` property on your model:

```xml
<DataTemplate x:DataType="models:FileItem">
  <Border Background="{Binding IsSelected, Converter={StaticResource BoolToBrush}}">
    <TextBlock Text="{Binding Name}" />
  </Border>
</DataTemplate>
```

**Common mistake:** expecting `ItemsRepeater` to behave like `ListBox` and wondering why `SelectionChanged` does not exist.

---

## 3. `ScrollViewer` as the virtualization trigger

```xml
<ScrollViewer>
  <ItemsRepeater ItemsSource="{Binding Items}"
                 x:DataType="viewModels:MainViewModel">
    <ItemsRepeater.Layout>
      <VirtualizingStackPanel />
    </ItemsRepeater.Layout>
    ...
  </ItemsRepeater>
</ScrollViewer>
```

`ItemsRepeater` does not virtualize on its own — it delegates layout to the `ItemsRepeater.Layout` property. The layout panel (`VirtualizingStackPanel`) implements `IVirtualizingLayout` and communicates with the `ScrollViewer` through the `ScrollViewer`'s `IScrollAnchorProvider` and extent calculation.

The flow:

1. `ScrollViewer` asks its child (`ItemsRepeater`) for its desired size.
2. `ItemsRepeater` asks the layout (`VirtualizingStackPanel`) to estimate the total size.
3. `VirtualizingStackPanel` calculates: `itemCount × averageItemHeight` (or uses fixed `ItemSize`).
4. `ScrollViewer` sets its `Extent` to this value.
5. When user scrolls, `ScrollViewer` notifies the layout to realize items at the new offset.

Without a `ScrollViewer`, the `VirtualizingStackPanel` has no extent to scroll within — it acts like a regular `StackPanel` and realizes all items.

**Common mistake:** putting `VirtualizingStackPanel` outside a `ScrollViewer`, or putting `VirtualizingStackPanel` as the root panel instead of inside a `ScrollViewer`.

---

## 4. `VirtualizationMode` — Standard vs Recycling

```xml
<VirtualizingStackPanel
    Orientation="Vertical"
    VirtualizationMode="Recycling" />
```

- **`Standard`**: each item that enters the viewport gets a new container element created. When it leaves the viewport, the container is destroyed (or held for garbage collection). This creates allocation pressure and GC pauses on fast scroll.
- **`Recycling`** (default, recommended): containers are reused. When Item 5 scrolls out of the viewport, its `ContentPresenter` is placed in a pool. When Item 101 scrolls in, it gets the pooled presenter, and its `DataContext` is set to Item 101.

Recycling avoids per-item allocation and destruction, but introduces a subtle problem: **state leaks**. If you modify properties directly on the container (e.g., `border.Background = Brushes.Blue` in code), that state persists when the container is recycled for another item. Always use data binding for visual properties:

```xml
<DataTemplate x:DataType="models:FileItem">
  <Border Background="{Binding IsSelected, Converter={StaticResource BoolToBrush}}">
    <TextBlock Text="{Binding Name}" />
  </Border>
</DataTemplate>
```

If you must modify container state in code, subscribe to the `ItemsRepeater.ElementPrepared` and `ElementClearing` events to reset state:

```csharp
itemsRepeater.ElementPrepared += (_, args) =>
{
    // Set state for the newly prepared element
    var border = (Border)args.Element;
    border.Tag = args.Index;
};
itemsRepeater.ElementClearing += (_, args) =>
{
    // Reset state so recycled elements don't leak
    var border = (Border)args.Element;
    border.Tag = null;
};
```

---

## 5. The `AsyncVirtualizingCollection` pattern

```csharp
public partial class LogViewerViewModel : ObservableObject
{
    public AsyncVirtualizingCollection<LogEntry> Items { get; }

    public LogViewerViewModel()
    {
        Items = new AsyncVirtualizingCollection<LogEntry>(
            new LogEntryProvider(), pageSize: 100);
    }
}
```

`AsyncVirtualizingCollection<T>` is **not** part of Avalonia — it is a pattern from the Windows Community Toolkit or a custom implementation. It provides:

- **Fetch-as-you-scroll:** only the currently-visible pages are fetched.
- **Cache:** previously-fetched pages are cached in memory.
- **Cancelation:** in-flight fetches are cancelled when the user scrolls past the requested range.

A minimal implementation needs:

```csharp
public interface IIncrementalSource<T>
{
    Task<IEnumerable<T>> GetPageAsync(int pageIndex, int pageSize, CancellationToken ct);
}
```

The wrapping collection implements `IList` for the panel to index into, but only holds fetched pages in memory.

**Common mistake:** using `ObservableCollection<T>` with 100,000 items added at once. The collection stores all items in memory, defeating the purpose of UI virtualization.

---

## 6. Incremental loading via `ScrollViewer.ScrollChanged`

```csharp
private async void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
{
    var scroller = (ScrollViewer)sender!;
    if (scroller.Offset.Y + scroller.Viewport.Height
        >= scroller.Extent.Height - 200)
    {
        await _vm.LoadNextPageAsync();
    }
}
```

The threshold calculation: `Offset.Y + Viewport.Height` is the bottom of the visible area. `Extent.Height` is the total scrollable height. When the visible bottom is within 200 pixels of the total extent, fetch the next page.

The threshold (200px) prevents fetching when the user is far from the bottom. Adjust based on item height and page size — a larger threshold triggers earlier (smoother for fast scroll), a smaller threshold triggers later (fewer fetches).

**Important:** after `LoadNextPageAsync()` adds items, the `Extent.Height` increases. The handler fires again if the user stays near the bottom, potentially fetching multiple pages in quick succession. Debounce or throttle the handler:

```csharp
private CancellationTokenSource? _scrollCts;

private async void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
{
    _scrollCts?.Cancel();
    _scrollCts = new CancellationTokenSource();

    try
    {
        await Task.Delay(200, _scrollCts.Token);
        // Check threshold and load
    }
    catch (TaskCanceledException) { }
}
```

---

## 7. `IScrollAnchorProvider` — maintaining scroll position

When items are added at the top of the list (e.g., a chat feed), the scroll position jumps unless the scroll viewer anchors to a specific element. `IScrollAnchorProvider` is the contract between the `ScrollViewer` and the virtualizing layout:

- The layout registers an "anchor element" (usually the first visible item).
- When layout changes (items added/removed), the `ScrollViewer` adjusts `Offset` to keep the anchor element at the same visual position.

`ItemsRepeater` sets `ItemsRepeater.ScrollAnchorOptions` to configure anchoring behavior:

```xml
<ItemsRepeater ItemsSource="{Binding Messages}"
               ScrollAnchorOptions="Top">
```

Without anchoring, chat-style feeds jump to the bottom on each new message. With anchoring to the first visible item, the scroll position stays stable.

---

## 8. DataGrid virtualization

```xml
<DataGrid ItemsSource="{Binding LargeTable}"
          EnableRowVirtualization="True"
          VirtualizingPanel.VirtualizationMode="Recycling"
          RowHeight="24" />
```

`DataGrid` virtualizes rows (not cells — all cells in a visible row are realized). `EnableRowVirtualization="True"` is the default. The `VirtualizingPanel` and `RowHeight` give the DataGrid the information it needs to calculate the scroll extent.

Disable virtualization (`EnableRowVirtualization="False"`) only when you need every row present in the visual tree — for example, when measuring row heights, performing print layouts, or applying animations to every row. With 1000+ rows, disabling virtualization causes multi-second load times and high memory usage.

---

## 9. Common recycling bugs

| Symptom | Cause | Fix |
|---------|-------|-----|
| Checked CheckBox appears on wrong item | CheckBox.IsChecked bound to model? No — state leaked in recycled CheckBox | Bind `IsChecked` to a model property |
| Background color from row 0 appears on row 50 | Code sets `border.Background` directly | Use binding or reset in `ElementPrepared` |
| Scroll position jumps after prepend | No anchor set | Set `ScrollAnchorOptions` |
| Memory grows unbounded | Items not removed from source collection | Trim source or use `AsyncVirtualizingCollection` |
| Item appears with wrong data momentarily | Binding delay during recycling | Use `VirtualizationMode.Recycling` (it rebinds DataContext before render) |

---

## Key differences from the original

| Concept | Original says | Why it matters |
|---------|---------------|----------------|
| Disabling virtualization | Broken XAML syntax (lines 61-65) | Attached property syntax is `VirtualizingStackPanel.IsVirtualizing="False"` on the panel, not an element name |
| `AsyncVirtualizingCollection` | Presented as built-in | Not part of Avalonia — it is a custom/CommunityToolkit pattern |
| `ItemsRepeater` selection | Attached behavior shown | No built-in selection — design choice, not omission |
| Element recycling | Mentioned | Requires `ElementPrepared`/`ElementClearing` for stateful containers |

---

## See Also

- [036 — Virtualization and Large List Performance](036-virtualization-large-lists.md) — the original tutorial
- [036E — Virtualization and Large List Performance (examples)](036-virtualization-large-lists-examples.md)
- [015 — Item Lists (ListBox, ItemsRepeater, DataGrid)](../intermediate/015-item-lists.md) — baseline non-virtualized usage
- [009 — Data Templates Basics](../basics/009-data-templates-basics.md) — DataTemplate fundamentals
- [Avalonia Docs: ItemsRepeater](https://docs.avaloniaui.net/docs/concepts/itemsrepeater)
