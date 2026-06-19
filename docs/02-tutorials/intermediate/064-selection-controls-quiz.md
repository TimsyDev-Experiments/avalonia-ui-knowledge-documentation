---
tier: intermediate
topic: controls
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 064Q — Selection Controls (quiz)

## Q1. Which property enables typing into a ComboBox?

- [ ] A. `IsTextSearchEnabled`
- [ ] B. `IsEditable`
- [ ] C. `AllowTextInput`
- [ ] D. `CanUserType`

**Answer:** B. `IsEditable="True"` enables text entry.

---

## Q2. How do you bind to just the ID of a selected item instead of the whole object?

- [ ] A. Use `SelectedValueBinding` and `SelectedValue`
- [ ] B. Use `SelectedItem.Id` in the binding path
- [ ] C. Cast `SelectedItem` in a converter
- [ ] D. Use `DisplayMemberBinding` with `SelectedIndex`

**Answer:** A. `SelectedValueBinding` extracts a property from the selected item and binds it to `SelectedValue`.

---

## Q3. What does `ISelectionModel` provide over `SelectedItems`?

- [ ] A. Better performance for large collections with index-based operations
- [ ] B. Support for `ObservableCollection`-only bindings
- [ ] C. Automatic sorting
- [ ] D. It is the same as `SelectedItems`

**Answer:** A. `ISelectionModel` uses index-based access, batch updates, and auto-adjusting indices — more efficient for large collections.

---

## Q4. Which ListBox selection mode requires Ctrl+click for multi-select?

- [ ] A. `Single`
- [ ] B. `Multiple`
- [ ] C. `Toggle`
- [ ] D. `Extended`

**Answer:** B. `Multiple` uses Ctrl+click. `Toggle` lets you tap/spacebar to toggle without modifiers.

---

## Q5. How do you display placeholder text in a ComboBox when nothing is selected?

- [ ] A. Set `Watermark` property
- [ ] B. Set `PlaceholderText` property
- [ ] C. Set `Hint` property
- [ ] D. Set `DefaultText` property

**Answer:** B. `PlaceholderText` shows a hint like "Select a category..." when no item is selected.

---

## Q6. How do you target ListBox items in a style?

- [ ] A. `Selector="ListBox"`
- [ ] B. `Selector="ListBoxItem"`
- [ ] C. `Selector="Item"`
- [ ] D. `Selector="ListBox > Item"`

**Answer:** B. Use `Style Selector="ListBoxItem"` inside `ListBox.Styles`.

---

## Q7. What property controls the maximum dropdown height in a ComboBox?

- [ ] A. `MaxHeight`
- [ ] B. `DropDownHeight`
- [ ] C. `MaxDropDownHeight`
- [ ] D. `PopupMaxHeight`

**Answer:** C. `MaxDropDownHeight` sets the maximum height of the dropdown list in pixels.

---

## Q8. True or False: ComboBox supports virtualization out of the box.

- [ ] A. True
- [ ] B. False

**Answer:** A. True. Both ComboBox and ListBox use `VirtualizingStackPanel` by default.

---

## Q9. Which event fires when the ComboBox dropdown closes?

- [ ] A. `Closed`
- [ ] B. `DropDownClosed`
- [ ] C. `PopupClosed`
- [ ] D. `SelectionChanged`

**Answer:** B. `DropDownClosed` fires after the dropdown closes.

---

## Q10. How do you make selection wrap around when reaching the last item in a ComboBox?

- [ ] A. Set `IsTextSearchEnabled="True"`
- [ ] B. Set `AutoScrollToSelectedItem="True"`
- [ ] C. Set `WrapSelection="True"`
- [ ] D. Set `KeyboardNavigation.DirectionalNavigation="Wrap"`

**Answer:** C. `WrapSelection="True"` wraps keyboard navigation from last to first item.

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

- [064 — Selection Controls (core)](064-selection-controls.md)
- [064V — Selection Controls (verbose)](064-selection-controls-verbose.md)
- [064E — Selection Controls (examples)](064-selection-controls-examples.md)
