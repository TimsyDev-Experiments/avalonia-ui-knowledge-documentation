---
tier: advanced
topic: devtools
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 029-avalonia-plus-devtools.md
---

# 029V — Using Avalonia DevTools: An In-Depth Companion

This companion explains what each DevTools feature does under the hood, when to use each panel, and how to interpret the information they present. Read it alongside [029 — Using Avalonia DevTools](029-avalonia-plus-devtools.md).

---

## 1. Installation — Why `AvaloniaUI.DiagnosticsSupport`?

### The package change from `Avalonia.Diagnostics`

Avalonia 11.x shipped `Avalonia.Diagnostics` as a NuGet package. In Avalonia 12, it was replaced by `AvaloniaUI.DiagnosticsSupport` (available with an Avalonia Plus subscription or under the community license for non-commercial use).

### Why the change

`Avalonia.Diagnostics` was tightly coupled to the core rendering pipeline, which made it difficult to maintain across different rendering backends. `AvaloniaUI.DiagnosticsSupport` is a standalone package that communicates with the running app through a debug protocol, not by hooking into internal rendering APIs. This also makes it possible to debug apps running on platforms where attaching a debugger UI is impractical (e.g., embedded devices).

### The `AttachDeveloperTools()` call

```csharp
AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .AttachDeveloperTools();
```

`AttachDeveloperTools()` registers:
- The F12 key handler globally (regardless of which window has focus).
- The DevTools service in the application's service registry.
- The IPC endpoint that the DevTools UI communicates with.

It replaces the Avalonia 11 `AttachDevTools()` method. If you have old code using `AttachDevTools()`, update it — the old method no longer exists in Avalonia 12.

### Licensing note

The `AttachDeveloperTools()` call checks for a valid Avalonia Plus license at runtime. Under the community license, the DevTools run in a limited mode: the performance profiler and accessibility tree may be restricted. The property panel, visual tree explorer, and layout explorer work fully.

---

## 2. Opening DevTools — What Happens Under the Hood

### F12 key handler

When you press F12:
1. A global `KeyBinding` in the `AttachDeveloperTools()` setup intercepts the key.
2. The DevTools service finds the currently focused `TopLevel` (window, or the active `Window`).
3. It creates a new transparent overlay `Window` top-level that renders the DevTools panel.
4. This overlay window is modeless and movable — it does not block interaction with the main app.

### Programmatic opening

```csharp
var topLevel = TopLevel.GetTopLevel(myWindow);
DevTools.Open(topLevel);
```

`TopLevel.GetTopLevel(myWindow)` walks up the visual tree from `myWindow` to find the root `Window` (or other `TopLevel` such as `Popup` or `WindowBase`). This is the same method used by dialogs and other top-level services.

`DevTools.Open(topLevel)` opens the tools for that specific top-level. Each window gets its own DevTools instance — the tools are not shared across windows.

### When to open programmatically

- Adding a "Developer Tools" menu item for test builds.
- Opening DevTools on a specific secondary window.
- Automated testing scenarios where you need to inspect the tree.

---

## 3. Visual Tree Explorer — How the Tree Is Built

### Logical vs. Visual tree

The DevTools shows both. The difference:

- **Logical tree**: The tree as defined in XAML nesting. Children of a `StackPanel` are the elements declared inside it. The logical tree follows `ContentControl.Content`, `ItemsControl.Items`, `Decorator.Child`, etc.
- **Visual tree**: The tree of `Visual` nodes used for rendering. For a `Button`, the visual tree includes the `Button`'s internal `Border`, `ContentPresenter`, and the content's visual. The visual tree is what the compositor iterates for hit-testing and rendering.

DevTools defaults to the logical tree because it matches what developers wrote in XAML. Switch to the visual tree to debug layout or rendering issues (e.g., "why is this element positioned where it is?").

### The breadcrumb bar

The breadcrumb at the top shows the selected element's ancestry from root to selected. Each segment is clickable. This is useful for navigating deep trees (e.g., `Window → Grid → ScrollViewer → StackPanel → Button`).

### Search behavior

Search matches by:
- Control type name (e.g., "Button", "TextBlock").
- `x:Name` / `Name` property value.
- `AutomationProperties.AutomationId` value.

Partial matches are highlighted. Search is case-insensitive.

---

## 4. Property Panel — Understanding Property Value Priority

### Why "Styled Properties" have a priority stack

Avalonia's property system assigns values from multiple sources, each with a priority:

1. **Animation** (highest)
2. **Local value** (set directly on the element, e.g., `<Button Width="100" />`)
3. **Style trigger** (from a style selector like `Button:pointerover`)
4. **Style setter** (from a style selector without trigger)
5. **Theme style** (from `ControlTheme` or `FluentTheme`)
6. **Default value** (defined in `StyledProperty` metadata)
7. **Inherited** (from parent element)

The DevTools property panel shows which source is currently winning. The stack trace icon (a document with corner folded) next to each value shows the exact style or source that set it. Clicking it shows the file and line number of the style setter.

### Direct Properties vs. Styled Properties

- **Styled Properties** (`StyledProperty<T>`) — participate in the priority system above. Most layout and appearance properties are styled.
- **Direct Properties** (`DirectProperty<T>`) — simple property storage with no priority system. `TextBlock.Text` is a DirectProperty. They always use the local value.

The property panel groups both types, but Direct Properties have no priority stack — they show just the current value.

### Live editing

Double-click any value to edit it. The edit sets a **local value** (priority level 2). This means:
- The change persists until you restart the app.
- You can override animated values while the animation is running (the animation resumes when the value source changes back).

Live-editing is useful for experimenting with property values during design iteration before writing XAML.

---

## 5. Styles Panel — How Styles Are Resolved

### Matching selectors

The Styles panel lists every selector that matches the selected element, ordered by specificity (most specific first). A selector matches when the element satisfies all conditions:
- `Button` — type matches.
- `Button:pointerover` — type matches AND pseudo-class is active.
- `.large /template/ Border` — element has style class "large" AND has a templated child `Border`.

### Override tracking

When multiple selectors set the same property, the **most specific** selector wins (specificity computed per CSS-like rules). The panel marks the winning setter and dims overridden ones. Hover over a dimmed setter to see which style overrode it.

### Toggle pseudo-classes

The panel lets you manually activate pseudo-classes (`:pointerover`, `:pressed`, `:disabled`, `:focus`, `:checked`, etc.). This is useful for:
- Testing visual states without interacting with the control.
- Verifying that `:focus` styles appear in the correct order.
- Debugging theme interactions where multiple pseudo-classes are active simultaneously.

Internally, toggling a pseudo-class calls `PseudoClasses.Set(":pointerover", true)` on the element, which triggers style re-evaluation just like the real system event.

---

## 6. Layout Explorer — What Each Overlay Means

### Margin, Border, Padding visualization

The Layout Explorer paints colored rectangles over the selected element:
- **Margin** (transparent green) — space outside the border.
- **Border** (transparent blue) — the border area.
- **Padding** (transparent orange) — space between border and content.
- **Content** (transparent purple) — the element's inner content area.

These rectangles correspond to the `Layoutable` box model: `Margin → Border → Padding → Content`. The explorer also displays numeric values for each area.

### Measure vs. Arranged size

- **Desired size** — the size returned by `MeasureOverride`. The minimum size this element needs.
- **Final size** — the size set by `ArrangeOverride`. The parent may arrange the element larger or smaller than desired.
- **Constraints** — the `Size` passed to `MeasureOverride` (`availableSize`) and `ArrangeOverride` (`finalSize`).

When these differ significantly, it indicates a layout negotiation issue:
- Desired > Final: element is clipped (overflow).
- Final > Desired: element is stretched beyond its needs.

### Invalidation tracking

The Layout Explorer shows a log of every layout invalidation event: what property changed, which element triggered it, and how many layout passes resulted. Use this to find "layout storms" — cascading invalidations that cause 10+ layout passes per frame.

---

## 7. Events Monitor — What Routed Events Look Like

### Routed event propagation

Avalonia routes events through three phases:
1. **Tunneling** — from root → target element. Events end with "Preview" (e.g., `PreviewPointerPressed`).
2. **Target** — the event reaches the target element.
3. **Bubbling** — from target → root. Events without "Preview" prefix.

The Events Monitor shows each event as it passes through elements. You can see:
- Which element the event originated from.
- Which elements handled it (marked `Handled`).
- The route the event traveled.

### Filtering

Filter by event type (e.g., only `PointerPressed`) or by element (e.g., only events passing through `myButton`). The timeline view is chronological; scrolling back shows past events up to a buffer limit.

---

## 8. Performance Profiler — Interpreting the Numbers

### Layout pass count

A healthy UI updates layout 0–1 times per frame. A value of 3+ per frame indicates a layout storm. Common causes:
- Properties that trigger layout (Width, Height, Margin, Padding, Visibility) being set in a loop.
- `InvalidateMeasure()` called unnecessarily.
- Nested layout containers that invalidate each other.

### Render pass timing

Shows how long each `Render()` call took. If a single control takes >5ms, its `Render()` method is a bottleneck. Drill into the element to see its sub-render times.

### Frame rate

Real-time FPS counter. 60 FPS is the target. Below 30 FPS is perceptibly janky. Causes:
- Long `Render()` calls (custom drawing).
- Layout storms.
- Too many visuals in the tree (optimization: UI virtualization).
- Heavy effects (blur, drop-shadow on many elements).

### Memory tracking

Basic allocation count for UI-related objects (Visual, DrawingContext allocations). Does not replace a full memory profiler but catches obvious leaks (e.g., controls not being garbage collected after removal from tree).

---

## 9. Screenshots — What Gets Captured

```csharp
var topLevel = TopLevel.GetTopLevel(myWindow);
var stream = await DevTools.CaptureScreenshotAsync(topLevel);
```

- `CaptureScreenshotAsync` returns a `MemoryStream` containing a PNG of the entire window content.
- The capture happens on the render thread, so it includes everything visible (including animations mid-frame).
- The stream can be saved to disk, uploaded, or copied to clipboard.

The UI alternative (right-click → Screenshot in the tree panel) captures only the selected element, not the full window. It clips to the element's bounds.

---

## 10. Accessibility Tree — Why It Matters

### What the tree shows

The Accessibility tab reconstructs how your app's UI is exposed to assistive technologies (screen readers, switch controls, voice control) through the platform's automation API (UI Automation on Windows, NSAccessibility on macOS, AT-SPI on Linux).

Each node shows:
- **Control type** — the `AutomationPeer` type (e.g., `ButtonAutomationPeer`, `TextBlockAutomationPeer`).
- **Name** — the value of `AutomationProperties.Name` or derived from content.
- **HelpText** — `AutomationProperties.HelpText` if set.
- **Bounding rectangle** — pixel area of the element.
- **Is focusable** — whether keyboard focus can reach this element.
- **Has keyboard focus** — current focus state.

### Focus order validation

The tree shows `TabIndex` for each focusable element. Elements are ordered by their actual focus traversal order (not just TabIndex values). This helps catch:
- Elements that are skipped in tab order.
- Elements with duplicate TabIndex values (order falls back to declaration order).
- Non-focusable elements that should be focusable.

### Live region settings

`AutomationProperties.LiveSetting` determines how the screen reader announces changes:
- `Off` — no automatic announcement.
- `Polite` — announce when idle.
- `Assertive` — announce immediately.

The tree shows which live regions are set, which is critical for real-time updating content (e.g., stock tickers, chat messages).

---

## See Also

- [029 — Using Avalonia DevTools (original)](029-avalonia-plus-devtools.md)
- [026 — Accessibility & Automation](026-accessibility-automation.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: DevTools](https://docs.avaloniaui.net/tools/developer-tools/installation)
- [029X — Using Avalonia DevTools (examples)](029-avalonia-plus-devtools-examples.md)
