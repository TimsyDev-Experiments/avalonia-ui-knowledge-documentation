---
tier: advanced
topic: devtools
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
packages: AvaloniaUI.DiagnosticsSupport 2.2.1
---

# 029 — Using Avalonia DevTools

**What you'll learn:** Install, configure, and use the Avalonia DevTools (F12) for live inspection, property editing, and performance profiling.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md)

---

## 1. Installation

```bash
dotnet add package AvaloniaUI.DiagnosticsSupport --version 2.2.1
```

In `Program.cs`:

```csharp
using AvaloniaUI.DiagnosticsSupport;

AppBuilder.Configure<App>()
    .UsePlatformDetect()
    .WithInterFont()
    .LogToTrace()
    .AttachDeveloperTools();  // ← replaces AttachDevTools()
```

> Requires an Avalonia Plus (or higher) subscription. Community license works for non-commercial use.

---

## 2. Opening DevTools

Press **F12** while your app is running, or call from code:

```csharp
// Programmatically (e.g., from a menu item)
var topLevel = TopLevel.GetTopLevel(myWindow);
DevTools.Open(topLevel);
```

---

## 3. Visual Tree Explorer

The main panel shows the logical/visual tree:

- **Select** any element by clicking it in the tree or in the app
- **Expand/collapse** subtree to inspect deep nesting
- **Search** by type name or `x:Name` (e.g., "Button" or "myButton")
- **Breadcrumb** at the top shows the selected element's ancestry

---

## 4. Property panel

Select a node to see all properties:

- **Styled Properties** — values by priority (animation, style, local, default)
- **Direct Properties** — current runtime values
- **Attached Properties** — including `Grid.Row`, `DockPanel.Dock`, etc.
- **Edit in place** — double-click any value to modify it live

The stack trace icon next to each property shows which style or source set it.

---

## 5. Styles panel

Shows all styles and setters applied to the selected element:

- **Matching selectors** — which styles currently apply
- **Setter values** — what each setter contributes
- **Override tracking** — see which style wins when multiple match
- **Toggle pseudo-classes** — manually activate `:pointerover`, `:pressed`, `:disabled`, `:focus` to test states

---

## 6. Layout explorer

- **Margin, Border, Padding** visualized as colored overlays
- **Measure size** vs **arranged size** comparison
- **Constraints** show the available / desired / final size
- **Invalidation tracking** — see when and why layout was triggered

---

## 7. Events monitor

- **Routed events** — track `PointerPressed`, `KeyDown`, `TextInput`, etc.
- **Gesture events** — tap, pinch, scroll, rotate
- **Filter** by event type or element
- **Timeline view** — see the order events fire

---

## 8. Performance profiler

- **Layout pass count** — identify excessive re-layouts
- **Render pass timing** — spot slow rendering
- **Frame rate** — real-time FPS counter
- **Memory** — basic allocation tracking

---

## 9. Screenshots

Capture the visual tree from DevTools:

```csharp
var topLevel = TopLevel.GetTopLevel(myWindow);
var stream = await DevTools.CaptureScreenshotAsync(topLevel);
```

Or use the DevTools UI:

1. Select an element in the tree
2. Right-click → **Screenshot**
3. Save as PNG

---

## 10. Accessibility tree

The Accessibility tab shows:

- **AutomationProperties.Name** and **HelpText** for each element
- **Focus order** (TabIndex)
- **Control type** announced to screen readers
- **Live region** settings
- **Tree structure** as assistive technology sees it

---

## Key Takeaways

- F12 opens DevTools at runtime — inspect, edit, profile live
- Property panel supports live editing of any value
- Styles panel shows which selectors match and what values they set
- Layout explorer visualizes margin/padding/content boxes
- Performance profiler catches layout and render bottlenecks
- Accessibility tree validates screen-reader readiness

---

## See Also

- [026 — Accessibility & Automation](026-accessibility-automation.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: DevTools](https://docs.avaloniaui.net/tools/developer-tools/installation)
- [029V — Using Avalonia DevTools (verbose companion)](029-avalonia-plus-devtools-verbose.md)
- [029X — Using Avalonia DevTools (examples)](029-avalonia-plus-devtools-examples.md)
