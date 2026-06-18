# Doc Plan — Phase 2 Topic Expansion

Extends the existing 217 docs with 24 new tutorial topics plus quick-refs and patterns.
Target: cover the major Avalonia 12 API surfaces not yet documented.

---

## Conventions

- **Effort** = core + verbose + examples + quiz (full 4-file treatment). Quick-refs are standalone.
- **Numbering**: new intermediate topics in the 051–075 range, advanced in 080–099.
- **Prerequisites** reference existing doc numbers so readers can chain sequentially.

---

## Phase 1 — Foundations

These fill gaps that affect comprehension of many existing docs. Start here.

### 1. 051 — Routed Events (Intermediate)

**Effort**: 16h · **Prerequisites**: 001, 002

| File | Est. |
|------|------|
| Core | 4h |
| Verbose | 6h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- Bubble / tunnel / direct routing strategies
- `RoutedEvent` registration (`RoutingStrategies`)
- `AddHandler` / `AddClassHandler`
- `handledEventsToo` pattern
- Custom `RoutedEventArgs` subclass
- Common Avalonia routed events table
- v12 changes (input event hierarchy)

### 2. 052 — Property System (Intermediate)

**Effort**: 18h · **Prerequisites**: 001, 007, 022

| File | Est. |
|------|------|
| Core | 5h |
| Verbose | 6h |
| Examples | 4h |
| Quiz | 3h |

**Core content**:
- `StyledProperty<T>` — inheritance, default, styling
- `DirectProperty<T>` — no inheritance, perf
- `AttachedProperty` — extending foreign objects
- `AvaloniaProperty.Register` vs `RegisterDirect` vs `RegisterAttached`
- Value precedence (local > style > default)
- Metadata — `PropertyMetadata`, `FrameworkPropertyMetadata`
- Property change callbacks (`OnPropertyChanged`)
- `AddOwner` pattern
- `Inherits` / `OverridesDefaultValue`

### 3. 053 — Threading & Dispatcher (Intermediate)

**Effort**: 14h · **Prerequisites**: 001

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- `Dispatcher.UIThread` / `DispatcherPriority`
- `InvokeAsync` / `Post`
- `AvaloniaSynchronizationContext`
- `DispatcherTimer` vs `System.Timers.Timer`
- Background thread safety rules
- v12: multiple-dispatcher support
- `MainThread` attribute

---

## Phase 2 — Input & Interaction

### 4. 054 — Focus Management (Intermediate)

**Effort**: 12h · **Prerequisites**: 001, 051

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `FocusManager` / `IFocusManager`
- `Focus()` / `IsFocused`
- Tab navigation (`IsTabStop`, `TabIndex`, `TabNavigation`)
- Arrow-key navigation in lists
- Focus scopes (`FocusScope`)
- `GotFocusEvent` / `LostFocusEvent` / v12 `FocusChangedEventArgs`
- Programmatic focus with `TopLevel.FocusManager`

### 5. 055 — Keyboard & Hotkeys (Intermediate)

**Effort**: 12h · **Prerequisites**: 002, 051

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `KeyBinding` / `KeyGesture`
- `HotKey` property on menu items, buttons
- `ICommand` + keyboard shortcut wiring
- Access keys / `AccessText` (v12)
- `OnKeyDown` / `OnTextInput` overrides
- Global hotkeys (platform limits)
- `Key` enum reference

### 6. 056 — Gestures (Intermediate)

**Effort**: 12h · **Prerequisites**: 001, 051

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `ScrollGesture`, `PinchGesture`, `PullGesture`
- Gesture recognizer setup
- `GestureRecognizer` base class
- Custom gesture recognizers
- `PointerPressed` / `PointerMoved` / `PointerReleased` combo
- v12: gestures moved to `InputElement`

---

## Phase 3 — Data & Binding

### 7. 057 — MultiBinding & PriorityBinding (Intermediate)

**Effort**: 12h · **Prerequisites**: 004, 011, 027

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `MultiBinding` syntax
- `IMultiValueConverter` / `FuncMultiValueConverter`
- v12: `IReadOnlyList<object?>` change
- `PriorityBinding` fallback chain
- Named `StringFormat` placeholders
- When to use vs composite bindings

### 8. 058 — Collection Views (Intermediate)

**Effort**: 14h · **Prerequisites**: 009, 015

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- `DataGridCollectionView` / `CollectionView`
- Sorting bound data in UI
- Filtering with predicates
- Grouping with header templates
- `IEditableCollectionView`
- `ICollectionViewLiveShaping` for real-time
- `DeferRefresh` batching

---

## Phase 4 — Platform Services

### 9. 059 — Clipboard & Launcher (Intermediate)

**Effort**: 10h · **Prerequisites**: 001

| File | Est. |
|------|------|
| Core | 2h |
| Verbose | 3h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `TopLevel.Clipboard` — `SetTextAsync` / `GetTextAsync`
- Clipboard data formats (HTML, RTF, files)
- `Launcher.LaunchUriAsync` / `LaunchFileAsync`
- Platform compatibility
- Secure clipboard handling

### 10. 060 — Storage Service (Intermediate)

**Effort**: 14h · **Prerequisites**: 034

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- `IStorageProvider` from `TopLevel`
- `OpenFilePickerAsync` / `SaveFilePickerAsync` / `OpenFolderPickerAsync`
- `FilePickerFileType` filters
- Storage bookmarks (persisting access)
- `StorageItem` — `GetParentAsync`, `GetItemsAsync`, `MoveAsync`
- Platform behavior matrix

---

## Phase 5 — Controls Deep Dives

### 11. 061 — TextBox & Text Input (Intermediate)

**Effort**: 14h · **Prerequisites**: 001

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- `TextBox` properties (`Text`, `Watermark`, `MaxLength`, `AcceptsReturn`, `TextWrapping`)
- Input validation and filtering
- Selection (selection start/length, selected text)
- Undo/redo
- `AutoComplete` / suggestion
- `OnTextInput` / IME support
- `PasswordBox` companion

### 12. 062 — Selection Controls: ComboBox, ListBox (Intermediate)

**Effort**: 14h · **Prerequisites**: 009, 015

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- `ComboBox` — `IsDropDownOpen`, `MaxDropDownHeight`, editable mode
- `ComboBox` virtualized dropdown
- `ListBox` — selection modes, multi-select
- `ListBoxItem` styling
- `AutoCompleteBox` — `FilterMode`, `ItemTemplate`
- Dropdown vs flyout vs popup

### 13. 063 — Range & Toggle Controls (Intermediate)

**Effort**: 12h · **Prerequisites**: 002, 003

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `Slider` — `Minimum`, `Maximum`, `TickFrequency`, `IsSnapToTickEnabled`
- `ProgressBar` — `IsIndeterminate`, `ShowProgressText`
- `NumericUpDown` — `Value`, `Increment`, `FormatString`, min/max validation
- `CheckBox` — `IsThreeState`, `IsChecked`
- `RadioButton` — grouped behavior
- `ToggleSwitch` — `OffContent`/`OnContent`, thumb styling
- Command wiring for all

### 14. 064 — TabControl, Expander, SplitView (Intermediate)

**Effort**: 14h · **Prerequisites**: 009

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 2h |

**Core content**:
- `TabControl` / `TabStrip` — `TabStripPlacement`, `TabItem` styling
- `TabControl` content reuse / virtualization
- `Expander` — `ExpandDirection`, `Header`, content animation
- `SplitView` — `DisplayMode` (Inline/Overlay/Compact), `PanePlacement`
- `SplitView` command bar / hamburger pattern
- `ToolTip` — `Placement`, `ShowDelay`, content
- `FlyoutBase` — `ShowAt`, `FlyoutPresenter` styling

---

## Phase 6 — Advanced Rendering & Layout

### 15. 080 — Layout System Deep Dive (Advanced)

**Effort**: 18h · **Prerequisites**: 023

| File | Est. |
|------|------|
| Core | 5h |
| Verbose | 6h |
| Examples | 4h |
| Quiz | 3h |

**Core content**:
- Measure pass: `MeasureOverride`, `Measure`, `DesiredSize`
- Arrange pass: `ArrangeOverride`, `Arrange`, `Bounds`, `BoundingBox`
- Layout rounding and pixel snapping
- `Layoutable` — `Width`, `Height`, `Margin`, `Padding`, `HorizontalAlignment`, `VerticalAlignment`
- `EffectiveViewportChanged` (v12)
- `LayoutManager` — `ExecuteLayoutPass`
- Invalidation — `InvalidateMeasure` vs `InvalidateArrange`
- Layout zones and overlay layer

### 16. 081 — Animation System Deep Dive (Advanced)

**Effort**: 18h · **Prerequisites**: 024

| File | Est. |
|------|------|
| Core | 5h |
| Verbose | 6h |
| Examples | 4h |
| Quiz | 3h |

**Core content**:
- `KeyFrame` animation — `KeyTime`, `KeySpline`, `KeyFrameType`
- Animation settings — `Duration`, `Delay`, `FillMode`, `IterationCount`, `PlaybackDirection`
- Control transitions — property, style, theme-level
- Page transitions — `CrossFade`, `SlideIn`, custom
- Composition animations — `CompositionCustomVisual`
- Easing functions — all types (`CubicBezier`, `Elastic`, `Bounce`, etc.)
- `PlaybackBehavior` — stop on invisible
- Animation performance considerations

### 17. 082 — Graphics & Drawing Reference (Advanced)

**Effort**: 20h · **Prerequisites**: 028

| File | Est. |
|------|------|
| Core | 5h |
| Verbose | 7h |
| Examples | 5h |
| Quiz | 3h |

**Core content**:
- Brushes — `SolidColorBrush`, `LinearGradientBrush`, `RadialGradientBrush`, `ImageBrush`, `VisualBrush`, `ImmutableBrush`
- Transforms — `RotateTransform`, `ScaleTransform`, `TranslateTransform`, `TransformGroup`, `RenderTransform` vs `LayoutTransform`
- Shapes — `Path`, `Geometry`, `RectangleGeometry`, `EllipseGeometry`, `CombinedGeometry`
- `DrawingContext` operations — `DrawLine`, `DrawRectangle`, `DrawEllipse`, `DrawGeometry`, `DrawText`, `DrawImage`
- Hit testing with geometries
- Clipping and masking (`OpacityMask`, `Clip`)
- Effects — `DropShadowEffect`, `BlurEffect`
- Bitmap blend modes, image interpolation
- Text rendering — `FormattedText`, `Typeface`, `TextLayout`

---

## Phase 7 — Platform & Performance

### 18. 083 — Container Queries (Intermediate)

**Effort**: 10h · **Prerequisites**: 003

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 3h |
| Examples | 3h |
| Quiz | 1h |

**Core content**:
- `ContainerQuery` syntax and selectors
- `@container` in styles
- Width/height-based responsive triggers
- Nesting container queries
- Container query units (`cqw`, `cqh`, `cqi`, `cqb`)

### 19. 084 — Typography & Custom Fonts (Intermediate)

**Effort**: 12h · **Prerequisites**: 003

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- `FontManager` — available fonts, fallback chains
- `FontFamily` URI schemes — `avares://`, `fonts://`
- Font weight / style / stretch reference
- `TextElement` attached properties
- Custom font loading at startup
- v12 font parser changes
- `GlyphRun` for advanced text layout

### 20. 085 — Performance Optimization (Advanced)

**Effort**: 16h · **Prerequisites**: 036, 080

| File | Est. |
|------|------|
| Core | 4h |
| Verbose | 5h |
| Examples | 4h |
| Quiz | 3h |

**Core content**:
- Reducing visual tree depth
- `RenderOptions` — `BitmapInterpolationMode`, `EdgeMode`
- Async binding with `AsyncConverter` to avoid UI thread blocking
- Virtualization — `ItemsRepeater` virtualization modes
- `CompositionCustomVisual` for GPU compute
- Layout pass debugging
- Profiling with DevTools

### 21. 086 — Platform Integration — Windows (Advanced)

**Effort**: 14h · **Prerequisites**: 037

| File | Est. |
|------|------|
| Core | 4h |
| Verbose | 5h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- Win32 window handle access (`TryGetPlatformHandle`)
- Mica / Acrylic backdrop
- Custom titlebar — `Window.ExtendClientAreaToTitleBar`
- Per-monitor DPI awareness
- Dark mode detection
- System tray (TrayIcon)
- `NativeControlHost` for WinForms interop
- UIA accessibility bridge

### 22. 087 — Platform Integration — macOS & Linux (Advanced)

**Effort**: 12h · **Prerequisites**: 037

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 4h |
| Examples | 3h |
| Quiz | 2h |

**Core content**:
- macOS — native menus, dock icon, URL scheme registration, NSView embedding
- Linux — X11/Wayland specifics, `NativeControlHost`, app indicators

---

## Phase 8 — Developer Experience

### 23. 088 — XAML Live Previewer (Intermediate)

**Effort**: 8h · **Prerequisites**: 001

| File | Est. |
|------|------|
| Core | 2h |
| Verbose | 3h |
| Examples | 2h |
| Quiz | 1h |

**Core content**:
- Enabling previewer in Rider / VS
- Design-time data context (`d:DataContext`)
- `d:DesignWidth` / `d:DesignHeight`
- Design-time resources and converters
- Previewer troubleshooting

### 24. 089 — Custom Flyout (Advanced)

**Effort**: 10h · **Prerequisites**: 051

| File | Est. |
|------|------|
| Core | 3h |
| Verbose | 3h |
| Examples | 3h |
| Quiz | 1h |

**Core content**:
- `FlyoutBase` / `PopupFlyoutBase`
- `CustomFlyout` with placement modes
- `FlyoutPresenter` styling
- Light-dismiss behavior
- Nested flyout patterns

---

## Quick-Ref Expansions

| Ref | Topic | Effort |
|-----|-------|--------|
| Q08 | Routed Events Quick Ref | 1h |
| Q09 | Property System Quick Ref | 1h |
| Q10 | Dispatcher & Timers Quick Ref | 1h |
| Q11 | Input Gestures Quick Ref | 1h |
| Q12 | Brushes & Colors Reference | 1h |
| Q13 | Font & Typography Reference | 1h |

## Pattern Expansions

| Ref | Topic | Effort | Prereqs |
|-----|-------|--------|---------|
| P008 | Focus & Keyboard Navigation | 2h | 054, 055 |
| P009 | Storage & File I/O Pipeline | 2h | 060 |

---

## Build Order Summary

| Phase | Topics | Total Effort | Rationale |
|-------|--------|-------------|-----------|
| 1 — Foundations | 051, 052, 053 | 48h | Unlocks understanding of many other docs |
| 2 — Input | 054, 055, 056 | 36h | Builds on routed events (051) |
| 3 — Data | 057, 058 | 26h | Extends existing binding coverage |
| 4 — Services | 059, 060 | 24h | Small, well-scoped topics |
| 5 — Controls | 061, 062, 063, 064 | 54h | Highest user-facing value |
| 6 — Rendering | 080, 081, 082 | 56h | Large but deepens existing 023/024/028 |
| 7 — Platform | 083, 084, 085, 086, 087 | 64h | Performance is high-need |
| 8 — Dev UX | 088, 089 | 18h | Lightweight wrap-up |
| Quick-refs | Q08–Q13 | 6h | Can be written incrementally |
| Patterns | P008–P009 | 4h | Final polish |

**Total estimated effort**: ~336 hours for all 24 full-topic treatments plus quick-refs and patterns.

---

## Appendices

### Overlapping / Merged Topics

Some gaps are narrow enough to fold into existing docs rather than creating new topics:

| Gap | Merge Into |
|-----|-----------|
| `Launcher` service | 059 — Clipboard & Launcher |
| `NumericUpDown`, `Slider` | 063 — Range & Toggle Controls |
| `CheckBox`, `RadioButton`, `ToggleSwitch` | 063 — Range & Toggle Controls |
| `ToolTip` | 064 — TabControl, Expander, SplitView |
| `AccessText` | 055 — Keyboard & Hotkeys |
| Perf (partial) | 036 — Virtualization |

### Deferred Topics

The following were identified in the gap audit but are too narrow or advanced for Phase 2:

- Appium UI testing (niche, tooling-dependent)
- Docker deployment (devops, not framework)
- Generic types in XAML (rare)
- Code-only UI deep dive (well-covered by existing patterns)
- Custom ItemsPanel (covered implicitly by 023)
- Unhandled exception handling (can fold into 053)
- Insets manager (mobile-only)
- AI tools / Pro licensing (commercial, not framework)

### Slotting New Quick-Refs

Current Q01–Q07 sequence continues with Q08–Q13. Files go under `01-quick-refs/`.

### Slotting New Patterns

Current P001–P007 sequence continues with P008–P009. Files go under `03-patterns/`.
