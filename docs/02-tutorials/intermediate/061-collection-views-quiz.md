---
tier: intermediate
topic: data
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 061Q — Collection Views (quiz)

## Q1. Does Avalonia have a built-in `ICollectionView` like WPF?

- [ ] A. Yes, it's in `Avalonia.Collections`
- [ ] B. No — filtering, sorting, and grouping are done in the view model
- [ ] C. Yes, called `CollectionView`
- [ ] D. No, but `DataGridCollectionView` exists for DataGrid scenarios

**Answer:** B. Avalonia deliberately omits `ICollectionView`. The recommended approach is view-model-level logic, optionally with DynamicData.

---

## Q2. Which class provides built-in grouping support for DataGrid?

- [ ] A. `CollectionView`
- [ ] B. `DataGridCollectionView`
- [ ] C. `GroupedCollection`
- [ ] D. `ObservableCollection`

**Answer:** B. `DataGridCollectionView` wraps a collection and supports `GroupDescriptions` and `SortDescriptions` for the DataGrid control.

---

## Q3. How do you add grouping to a DataGridCollectionView?

- [ ] A. `GroupedView.GroupDescriptions.Add(new DataGridPathGroupDescription("Property"))`
- [ ] B. `GroupedView.GroupBy("Property")`
- [ ] C. `GroupedView.GroupColumn = "Property"`
- [ ] D. Set `DataGrid.GroupBy` property

**Answer:** A. Use `GroupDescriptions.Add(new DataGridPathGroupDescription("..."))` on the collection view.

---

## Q4. What is the main benefit of DynamicData over manual ObservableCollection rebuilds?

- [ ] A. DynamicData is simpler to write
- [ ] B. DynamicData sends incremental delta updates, avoiding full UI rebuilds
- [ ] C. DynamicData requires no NuGet package
- [ ] D. DynamicData works without INotifyPropertyChanged

**Answer:** B. DynamicData processes adds, removes, and updates incrementally, which is faster for large collections.

---

## Q5. True or False: You can use `DataGridCollectionView` with non-DataGrid controls like ListBox.

- [ ] A. True — it implements IEnumerable so any ItemsControl can bind to it
- [ ] B. False — it only works with DataGrid

**Answer:** A. `DataGridCollectionView` implements `IEnumerable`, so it can be bound to a `ListBox` or any `ItemsControl`, but grouping headers only render in `DataGrid`.

---

## Q6. Which method defers a DataGridCollectionView refresh until multiple changes are complete?

- [ ] A. `DeferRefresh()`
- [ ] B. `BatchUpdate()`
- [ ] C. `SuspendRefresh()`
- [ ] D. `BeginInit()`

**Answer:** A. `DeferRefresh()` batching prevents multiple re-evaluations when making several changes to grouping/sorting.

---

## Q7. How do you enable live shaping so DataGridCollectionView re-sorts when item properties change?

- [ ] A. `IsLiveSorting = true` + add to `LiveSortingProperties`
- [ ] B. `AutoSort = true`
- [ ] C. `LiveUpdate = true`
- [ ] D. `ReactiveSort = true`

**Answer:** A. Set `IsLiveSorting = true` and add property names to `LiveSortingProperties`. Items must implement `INotifyPropertyChanged`.

---

## Q8. What should you expose instead of `ObservableCollection<T>` to prevent external mutation?

- [ ] A. `IEnumerable<T>`
- [ ] B. `ReadOnlyObservableCollection<T>`
- [ ] C. `IList<T>`
- [ ] D. `ICollection<T>`

**Answer:** B. `ReadOnlyObservableCollection<T>` wraps an `ObservableCollection` but prevents callers from adding/removing items.

---

## Q9. Which DynamicData operator switches back to the UI thread?

- [ ] A. `OnUIThread()`
- [ ] B. `ObserveOn(AvaloniaSynchronizationContext.Current)`
- [ ] C. `InvokeOn(AvaloniaSynchronizationContext.Current)`
- [ ] D. `SwitchTo(Dispatcher)`

**Answer:** B. `ObserveOn` with the Avalonia synchronization context ensures UI-bound observers run on the dispatcher thread.

---

## Q10. What binding parameter debounces a TextBox search input?

- [ ] A. `Throttle="300"`
- [ ] B. `Delay=300`
- [ ] C. `Debounce="300"`
- [ ] D. `UpdateSourceTrigger=Debounced`

**Answer:** B. `Delay=300` on the binding waits 300ms after the last keystroke before updating the source property.

---

## Scoring

| Score | Interpretation |
|-------|---------------|
| 10/10 | Expert |
| 8-9 | Strong understanding |
| 6-7 | Getting there |
| <6 | Review the core tutorial |

---

## See Also

- [061 — Collection Views (core)](061-collection-views.md)
- [061V — Collection Views (verbose)](061-collection-views-verbose.md)
- [061E — Collection Views (examples)](061-collection-views-examples.md)
