---
tier: advanced
topic: devtools
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 029-avalonia-plus-devtools.md
---

# Quiz — Using Avalonia DevTools

```quiz
Q: Which NuGet package and API call are required to enable DevTools in Avalonia 12?
A. Avalonia.Diagnostics package with .AttachDevTools() || Avalonia.Diagnostics is deprecated — use AvaloniaUI.DiagnosticsSupport and .AttachDeveloperTools().
B. AvaloniaUI.DiagnosticsSupport package with .AttachDeveloperTools() (correct) || This is the correct package and API for Avalonia 12 with Avalonia Plus.
C. Avalonia.DevTools package with .EnableDeveloperTools() || No such package exists — the correct package is AvaloniaUI.DiagnosticsSupport.
D. No package needed — DevTools are built-in via .UsePlatformDetect() || DevTools require an explicit package reference and API call (AttachDeveloperTools).
Explanation: Avalonia 12 uses the AvaloniaUI.DiagnosticsSupport package and the AttachDeveloperTools() extension method (requires Avalonia Plus).
```

```quiz
Q: How do you open DevTools programmatically from a menu item click?
A. DevTools.Show(myWindow) || Show is not a method on DevTools — use DevTools.Open with a TopLevel reference.
B. DevTools.Open(TopLevel.GetTopLevel(myWindow)) (correct) || GetTopLevel retrieves the TopLevel instance for the window, then DevTools.Open opens the tools.
C. TopLevel.GetTopLevel(myWindow).OpenDevTools() || OpenDevTools is not a method on TopLevel — use the static DevTools.Open method.
D. Application.Current.OpenDevTools() || There is no static OpenDevTools on Application — use DevTools.Open with a TopLevel.
Explanation: The pattern is to get the TopLevel via TopLevel.GetTopLevel(window) and pass it to DevTools.Open().
```

```quiz
Q: Which DevTools panel allows you to see the priority stack of a StyledProperty (animation, style, local, default)?
A. Layout explorer || The layout explorer shows margin/padding/content boxes and measure/arrange sizes, not property values.
B. Events monitor || The events monitor tracks routed and gesture events — it does not show property values.
C. Property panel (correct) || The property panel displays Styled Properties with their priority stack, Direct Properties, and Attached Properties.
D. Styles panel || The styles panel shows matching selectors and setter values, but the full priority stack is in the Property panel.
Explanation: The Property panel shows each Styled Property's value broken down by priority (animation, style, local, default).
```

```quiz
Q: Which DevTools feature is useful for validating screen-reader readiness of your application?
A. Performance profiler || The profiler tracks layout passes, render timing, and frame rate — it does not address accessibility.
B. Layout explorer || The layout explorer helps debug sizing but not accessibility.
C. Events monitor || The events monitor shows routed events — accessibility tree structure is separate.
D. Accessibility tree (correct) || The Accessibility tab shows AutomationProperties.Name, HelpText, focus order, control types, and the tree as assistive technology sees it.
Explanation: The Accessibility tree tab validates automation properties, focus order, and screen-reader announcements.
```

```quiz
Q: What are the two ways to capture a screenshot in DevTools?
A. Press F12 and click Save, or call DevTools.CaptureScreenshotAsync(topLevel) (correct) || The UI allows right-click -> Screenshot on any element, and the code API is DevTools.CaptureScreenshotAsync.
B. Call Application.Current.CaptureScreenshot(), or use Snipping Tool || There is no static CaptureScreenshot on Application — use DevTools.CaptureScreenshotAsync.
C. Press PrintScreen, or use the DevTools Performance profiler || PrintScreen is OS-level — DevTools provides its own screenshot capability.
D. Call TopLevel.GetTopLevel(myWindow).Screenshot(), or press Ctrl+S || There is no Screenshot method on TopLevel — use DevTools.CaptureScreenshotAsync.
Explanation: DevTools supports screenshots via the right-click context menu in the tree and the DevTools.CaptureScreenshotAsync API.
```
