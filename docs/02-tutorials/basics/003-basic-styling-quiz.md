---
tier: basics
topic: styling
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 003-basic-styling.md
---

```quiz
Q: What does the selector `Button.primary /pointerover/` match?
A. Any `<Button>` whose `Classes` string contains "pointerover" || CSS-class syntax applies to style classes; "pointerover" is not a class but a pseudo-class token, so this describes a different selector.
B. Any `<Button>` with style class "primary" while the pointer is hovering over it (correct) || The `/pointerover/` pseudo-class activates when the pointer enters the element's bounds; combined with `Button.primary` it targets the hover state of buttons that carry the `primary` class.
C. Any `<Button>` nested inside an element whose `Classes` include "primary pointerover" || Pseudo-classes are not inherited from ancestors; `/pointerover/` applies to the matched element itself, not its parent chain.
D. Any `<Button>` whose `x:Name` contains "primary" when the pointer is over it || The dot syntax in a selector separates the type from a style class (`Button.primary`), not a name; `x:Name` is not matched by CSS-style selectors.
Explanation: `Button.primary /pointerover/` matches `<Button>` elements having style class `primary` while the pointer is over them — a hover effect. Pseudo-classes use `/slashes/` (or `:` for some states like `:disabled`).
```

```quiz
Q: A view has `<Button Content="Delete" Classes="primary danger" />`. The style `Button.primary.danger` uses `BasedOn="{StaticResource {Button.primary}}"`. Which describes the resolved visual result?
A. Only the danger style setters apply; `BasedOn` pulls the primary style at resource-resolution time, then the danger setters override any conflicting properties. (correct) || `BasedOn` merges the base style before applying the derived style's own setters; properties set by both (e.g. `Background`) are won by the derived style.
B. Neither style applies because `BasedOn` references a style-key string, not a `Style` resource key, which raises a compile-time error in Avalonia 12. || `{StaticResource {Button.primary}}` uses the style's implicit key (its selector string); this is the documented pattern for `BasedOn`.
C. The button shows only the primary style; the `danger` class is ignored unless it appears first in the `Classes` string. || Class order in `Classes` does not affect selector matching or style priority; `Button.primary.danger` matches regardless of order.
D. Both styles apply independently, and any conflict causes a runtime fallback to the control theme default. || Conflicts are resolved by specificity and `BasedOn` inheritance, not by discarding both; no fallback to the theme occurs.
Explanation: `BasedOn` inherits all setters from the referenced style. Here `Button.primary.danger` inherits `CornerRadius` and `Padding` from `Button.primary`, then applies its own `Background="#dc2626"`. The result is the base primary look with a red background override.
```

```quiz
Q: Which scope ensures a named style is available to every window and dialog in an Avalonia 12 desktop application?
A. `Window.Resources` || Resources at the window level are scoped to that window and its children only; sibling or newly opened windows do not see them.
B. `Application.Styles` (correct) || Styles placed in the `<Application.Styles>` section of `App.axaml` are global; every `Window`, `Window` subclass, and popup in the process inherits the application-level style dictionary.
C. A `ResourceDictionary` merged into `Window.Resources` of each window individually || While functional, this duplicates the merge step per window and contradicts the single-source principle; `Application.Styles` handles it declaratively once.
D. The `App.xaml.cs` constructor, assigning `Style` instances to `Application.Current.Styles` || This works at runtime but bypasses the XAML-declared `Application.Styles` section, making the style invisible to XAML tooling and compiled-binding analyzers.
Explanation: `Application.Styles` (set in `App.axaml`) is the global scope. Any `<Style>` added there is visible to every `Window`, `UserControl`, and overlay in the application. Use `Window.Resources` only for page-local overrides.
```
