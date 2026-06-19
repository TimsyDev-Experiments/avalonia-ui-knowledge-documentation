---
tier: intermediate
topic: controls
avalonia-version: 12.0.4
quiz-format: multiple-choice
---

# 066Q — TabControl, Expander & SplitView (quiz)

## Q1. How do you position the tab strip on the left side of a TabControl?

- [ ] A. `TabStripPlacement="Left"`
- [ ] B. `TabOrientation="Vertical"`
- [ ] C. `Placement="Left"`
- [ ] D. `Orientation="Vertical"`

**Answer:** A. `TabStripPlacement` accepts `Top`, `Bottom`, `Left`, or `Right`.

---

## Q2. Which property enables lazy content loading in a TabControl?

- [ ] A. `VirtualizationMode="Recycling"`
- [ ] B. Using `ContentTemplate` with `DataTemplate` instead of static content
- [ ] C. `LazyLoad="True"`
- [ ] D. `DeferredLoading="True"`

**Answer:** B. When content is provided via `ContentTemplate`, the template creates a new view instance only when a tab is selected.

---

## Q3. Which Expander property controls the direction the content expands?

- [ ] A. `Direction`
- [ ] B. `ExpandDirection`
- [ ] C. `Orientation`
- [ ] D. `FlowDirection`

**Answer:** B. `ExpandDirection` accepts `Down`, `Up`, `Left`, or `Right`.

---

## Q4. Which event fires when an Expander content section begins to appear?

- [ ] A. `Expanded`
- [ ] B. `Expanding`
- [ ] C. `Opening`
- [ ] D. `ContentChanged`

**Answer:** B. `Expanding` fires at the start of the expand animation; `Collapsed` fires after collapse completes.

---

## Q5. Which SplitView DisplayMode shows a narrow 48px strip when closed?

- [ ] A. `Inline`
- [ ] B. `Overlay`
- [ ] C. `CompactInline`
- [ ] D. `CompactOverlay`

**Answer:** Both C and D are correct — `CompactInline` and `CompactOverlay` both show a closed strip. The question expects `CompactInline` (most common for nav sidebars).

---

## Q6. What does `CompactPaneLength` control in a SplitView?

- [ ] A. The open pane width
- [ ] B. The closed strip width in compact modes
- [ ] C. The animation duration
- [ ] D. The gap between pane and content

**Answer:** B. `CompactPaneLength` sets the pane width when closed in compact modes (default 48px).

---

## Q7. How do you attach a ToolTip to a Button?

- [ ] A. `Button.ToolTip="text"`
- [ ] B. `ToolTip.Tip="text"`
- [ ] C. `Button.ToolTipContent="text"`
- [ ] D. `ToolTipContent="text"`

**Answer:** B. `ToolTip.Tip` is the attached property that sets tooltip content.

---

## Q8. Which TabControl property binds to the current tab for tracking selection in the view model?

- [ ] A. `SelectedIndex` or `SelectedItem`
- [ ] B. `CurrentTab`
- [ ] C. `ActiveTab`
- [ ] D. `TabSelection`

**Answer:** A. `SelectedIndex` binds to an int; `SelectedItem` binds to the tab's data item.

---

## Q9. True or False: SplitView supports placing the pane on all four sides (top, bottom, left, right).

- [ ] A. True
- [ ] B. False

**Answer:** A. True. `PanePlacement` accepts `Left`, `Right`, `Top`, or `Bottom`.

---

## Q10. Which attached property controls how long before a ToolTip appears?

- [ ] A. `ToolTip.Duration`
- [ ] B. `ToolTip.ShowDelay`
- [ ] C. `ToolTip.VisibilityDelay`
- [ ] D. `ToolTip.InitialDelay`

**Answer:** B. `ToolTip.ShowDelay` sets the milliseconds before the tooltip shows (default 400ms).

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

- [066 — TabControl, Expander & SplitView (core)](066-tabcontrol-expander-splitview.md)
- [066V — TabControl, Expander & SplitView (verbose)](066-tabcontrol-expander-splitview-verbose.md)
- [066E — TabControl, Expander & SplitView (examples)](066-tabcontrol-expander-splitview-examples.md)
