---
tier: advanced
topic: virtualization
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 036-virtualization-large-lists.md
---

# Quiz — Virtualization & Large Lists

```quiz
Q: Which two conditions must be met for ItemsRepeater to virtualize its items?
A. It must be placed inside a ScrollViewer and use VirtualizingStackPanel as its layout (correct) || ItemsRepeater itself does not scroll; a ScrollViewer provides the scroll surface, and VirtualizingStackPanel enables the virtualization logic.
B. VirtualizationMode must be set to "Recycling" and the ItemsSource must be an ObservableCollection || Recycling mode is the default and recommended, but not strictly required for virtualization to activate.
C. The DataTemplate must use x:DataType and the parent must be a Window || x:DataType enables compiled bindings but does not affect virtualization.
D. EnableRowVirtualization must be true and the height must be explicitly set || EnableRowVirtualization is a DataGrid property, not applicable to ItemsRepeater.
Explanation: ItemsRepeater virtualizes only when it has a finite viewport (from ScrollViewer) and uses VirtualizingStackPanel for its layout.
```

```quiz
Q: What is the practical difference between VirtualizationMode="Standard" and VirtualizationMode="Recycling"?
A. Standard creates and destroys container elements on scroll; Recycling reuses existing containers (correct) || Recycling reduces allocation pressure by reusing container controls and only swapping data context.
B. Standard disables virtualization; Recycling enables it || Both modes enable virtualization; the difference is container lifecycle.
C. Standard uses CPU; Recycling uses GPU || Both modes are CPU-driven; the GPU is not involved in container management.
D. Standard works with ListBox; Recycling works with ItemsRepeater || Both modes work with any VirtualizingStackPanel; they are not control-specific.
Explanation: Recycling reuses the visual containers, replacing data contexts as items scroll into view. Standard creates and destroys containers each time, increasing GC pressure.
```

```quiz
Q: Which pattern correctly implements infinite scroll with incremental loading?
A. Handle ScrollViewer.ScrollChanged and test if Offset.Y + Viewport.Height >= Extent.Height - threshold (correct) || This common pattern fires a load action when the user nears the bottom of the scrollable content.
B. Bind a command to ListBox.ScrollIntoView || ScrollIntoView scrolls to a specific item; it does not detect proximity to the end.
C. Use ItemsRepeater's LayoutUpdated event and check the last visible index || ItemsRepeater does not expose a direct last-visible-index property without custom layout logic.
D. Set VirtualizingStackPanel.IncrementalLoadingThreshold attached property || Avalonia's VirtualizingStackPanel does not support an IncrementalLoadingThreshold attached property.
Explanation: Subscribe to ScrollChanged, compute the remaining scroll distance, and call the next-page loader when the threshold is crossed.
```

```quiz
Q: Why should you avoid storing mutable state in container elements within a virtualized ItemsRepeater?
A. Recycled containers retain their previous visual state, causing incorrect renderings when data context changes (correct) || Because containers are reused, any non-data-bound property (e.g., manually set Background) can leak from one item to another.
B. Mutable state causes the layout to remeasure on every scroll || Measured state is not the issue; the problem is stale state surviving reuse.
C. The Avalonia runtime throws an exception when it detects mutable state on a recycled container || No such exception exists.
D. ItemsRepeater does not support data binding on container properties || ItemsRepeater fully supports data binding; the advice is about manually set properties.
Explanation: VirtualizingStackPanel recycles containers. A manually set Background or other local value persists across items, producing visual bugs. Use data binding instead.
```

```quiz
Q: A DataGrid with 50,000 rows performs poorly when scrolled. What is the most likely cause?
A. EnableRowVirtualization is set to False or the rows are not virtualizing (correct) || DataGrid virtualizes by default, but if EnableRowVirtualization=False or a parent element prevents finite height, all rows are realized.
B. The ItemsSource is an Array instead of an ObservableCollection || Collection type does not affect virtualization performance.
C. VirtualizingPanel.VirtualizationMode is set to "Recycling" || Recycling mode is the recommended default; it improves performance, not degrades it.
D. RowHeight is not set || RowHeight affects layout calculations but does not prevent virtualization.
Explanation: DataGrid only virtualizes when EnableRowVirtualization=True (default) and the DataGrid has a constrained height. Check that virtualization is active.
```
