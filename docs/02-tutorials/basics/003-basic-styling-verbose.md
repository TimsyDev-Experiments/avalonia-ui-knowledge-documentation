---
tier: basics
topic: styling
estimated: 25-30 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 003-basic-styling.md
---

# 003V — Basic Styling: An In-Depth Companion

**What you'll learn in this companion:** How Avalonia's style system differs from WPF and CSS, the selector engine's matching rules, specificity calculation, how pseudo-classes are toggled by the control, when `BasedOn` actually works, and the mechanical difference between a Style and a ControlTheme.

**Prerequisites:** [001 — Project Setup](001-project-setup.md)

**You should already have read:** [003 — Basic Styling](003-basic-styling.md) for the quick-start version. This file goes deeper on every section.

---

## 1. How Avalonia's Style Engine Differs from WPF

Avalonia styles are inspired by CSS, not by WPF's resource-keyed style system. The key differences:

- **WPF:** A `<Style>` has a `TargetType` and an optional `x:Key`. You apply it by setting `Style="{StaticResource MyKey}"` on each element. Styles are looked up by key.
- **Avalonia:** A `<Style>` has a `Selector` (like a CSS selector). Styles are applied automatically when the selector matches the element. You do not set `Style="..."` — the engine matches for you.

This means in Avalonia, you write a selector like `Button.primary` and every `<Button Classes="primary">` in scope gets that style automatically. No per-instance wiring. This is closer to how web CSS works: write a rule, and all matching elements pick it up.

### The Selector Grammar

Avalonia selectors follow a CSS-like grammar:

| Selector | Matches | CSS equivalent |
|---|---|---|
| `Button` | Any `Button` instance | `button` |
| `Button.primary` | `Button` with class `primary` | `button.primary` |
| `Button#myButton` | `Button` with `Name="myButton"` | `button#myButton` |
| `StackPanel > Button` | Direct child `Button` of `StackPanel` | `stackpanel > button` |
| `StackPanel Button` | Descendant `Button` inside `StackPanel` | `stackpanel button` |
| `Button:disabled` | Disabled `Button` | `button:disabled` |
| `Button /pointerover/` | Hovered `Button` | `button:hover` |
| `Button.class /pressed/` | Pressed `Button` with class | `button.class:active` |
| `Button:nth-child(2n+1)` | Odd-indexed `Button` siblings | `button:nth-child(odd)` |
| `Button.template` | Matches a `Button` inside a template | none (Avalonia-specific) |

The `Button.template` selector is unique to Avalonia: it matches only when the element is part of a control template (not in logical tree). This is how control themes target template parts without leaking to the logical tree.

---

## 2. How the Selector Engine Matches: Specificity and Cascade

When multiple styles match the same element, Avalonia resolves conflicts using **specificity** — exactly like CSS. The specificity is calculated as a three-part score:

1. **ID selectors** (`#name`): count = A
2. **Class selectors** (`.class`, `:pseudo`, `/pseudo/`): count = B
3. **Type selectors** (`Button`, `StackPanel`): count = C

The score is `A-B-C` (higher wins). Examples:

| Selector | A | B | C | Score |
|---|---|---|---|---|
| `Button` | 0 | 0 | 1 | 0-0-1 |
| `Button.primary` | 0 | 1 | 1 | 0-1-1 |
| `Button#saveBtn` | 1 | 0 | 1 | 1-0-1 |
| `Button.primary /pointerover/` | 0 | 2 | 1 | 0-2-1 |
| `StackPanel > Button.primary` | 0 | 1 | 2 | 0-1-2 |

A style with higher specificity overrides lower. If two styles have equal specificity, the one that appears **later** in the styles collection wins (source-order rule).

This is why order of `<Style>` elements in `Application.Styles` matters: a later style with the same selector overrides an earlier one.

---

## 3. Why Inline Styles Have the Highest Specificity (and When to Avoid Them)

```xml
<Button Content="Click"
        Background="Blue"
        Foreground="White"
        FontSize="16" />
```

Setting properties directly on an element is equivalent to inline CSS `style="..."`. In Avalonia's specificity calculation, local property values are in a separate layer above all styles. A local value always wins over any style setter, regardless of selector specificity.

This is useful for one-off overrides but harmful for consistency. If you have three buttons with inline `Background="Blue"` and later decide the color should be `#6a33ff`, you must edit three places. Worse, if a style tries to set `Background` via a selector, it has no effect — the inline value takes precedence.

**Rule:** Use inline values only for truly per-instance values (e.g., `Content` text). Use style setters for visual properties, even if you think it's a one-off.

---

## 4. Style Classes: Space-Separated, Not Comma-Separated

```xml
<Button Content="Delete" Classes="primary danger" />
```

`Classes="primary danger"` applies both the `primary` class and the `danger` class. Avalonia uses space-separated class names, like HTML. You cannot use commas.

The `Classes` property is an `IList<string>` that you can manipulate from code:

```csharp
myButton.Classes.Add("primary");
myButton.Classes.Remove("danger");
myButton.Classes.Contains("primary"); // true
```

`Classes` is also an `IAvaloniaList<string>` with change notification — adding or removing a class triggers a style re-match immediately.

---

## 5. Pseudo-Classes: How Controls Signal Their State

```xml
<Style Selector="Button.primary /pointerover/">
  <Setter Property="Background" Value="#5a2ae0" />
</Style>
```

Pseudo-classes are not CSS static strings — they are **set and cleared by the control itself** based on its internal state. The control's `PseudoClasses` collection (an `IAvaloniaList<string>`) is modified when state changes:

| Pseudo-class | Set by control | When |
|---|---|---|
| `:pointerover` | `Button`, `TextBlock`, any input control | Mouse pointer enters the element bounds |
| `:pressed` | `Button`, `ScrollBar` thumbs | Mouse button is down on the element |
| `:focus` | Any focusable control | Element has keyboard focus |
| `:focus-visible` | Any focusable control | Element has focus and would show a focus ring |
| `:disabled` | `InputElement` | `IsEnabled` is `false` |
| `:checked` | `CheckBox`, `RadioButton` | `IsChecked` is `true` |
| `:indeterminate` | `CheckBox` | `IsChecked` is `null` (three-state) |
| `:selected` | `TabItem`, `ListBoxItem` | Item is selected |
| `:dragging` | `Thumb`, `SplitView` | Element is being dragged |

You can also set pseudo-classes on custom controls:

```csharp
protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
{
    base.OnPropertyChanged(change);
    if (change.Property == IsActiveProperty)
        PseudoClasses.Set(":active", IsActive);
}
```

`PseudoClasses.Set(className, bool)` adds or removes the pseudo-class efficiently.

### Slash Syntax vs Colon Syntax for Pseudo-Classes

Avalonia supports two syntaxes for pseudo-classes:

- **Slash syntax:** `/pointerover/`, `/pressed/` — matches the raw pseudo-class name.
- **Colon syntax:** `:disabled` — shorthand for `:disabled`.

Why two syntaxes? Colon syntax is limited to a known set of pseudo-classes that the Avalonia selector parser recognizes. Slash syntax works with *any* pseudo-class, including custom ones your control defines. Use slash syntax for custom pseudo-classes; use colon syntax for standard ones (it's more readable).

---

## 6. Scoped vs Global: Why Placement Matters

```xml
<!-- Scoped: only applies in this Window -->
<Window.Resources>
  <Style Selector="Button.primary">
    <Setter Property="Background" Value="#6a33ff" />
  </Style>
</Window.Resources>

<!-- Global: applies in every window -->
<Application.Styles>
  <Style Selector="Button.primary">
    <Setter Property="Background" Value="#6a33ff" />
  </Style>
</Application.Styles>
```

The lookup chain for styles is:

```
Element.Resources → Parent.Resources → ... → Window.Resources → Application.Styles → Theme Styles
```

A style defined at `Application.Styles` is matched against all elements in all windows. A style defined in `Window.Resources` is matched only against elements in that window. This is how you scope styles: if you only want a special button style on a specific page, put it in that page's resources.

**Performance note:** Each `<Style>` with a selector is evaluated against each matching element. A style in `Application.Styles` is checked against every element in the app. A style in a specific `Window.Resources` is checked only against elements in that window's tree. For large apps, prefer scoped styles where possible to reduce selector evaluation cost.

---

## 7. Style Inheritance with `BasedOn`: How It Works (and Its Limitations)

```xml
<Style Selector="Button.primary">
  <Setter Property="CornerRadius" Value="8" />
</Style>

<Style Selector="Button.primary.danger"
       BasedOn="{StaticResource {Button.primary}}">
  <Setter Property="Background" Value="#dc2626" />
</Style>
```

`BasedOn` tells Avalonia: "before applying this style's setters, apply the setters from the referenced style." The referenced style is looked up by key, and in this case the key is the selector string `{Button.primary}` — Avalonia auto-creates a resource key from a `Style`'s selector when the style is registered.

**Limitations of `BasedOn`:**

1. **The referenced style must be in a resource dictionary.** `BasedOn` uses `StaticResource` lookup, so the parent style must be accessible via `{StaticResource SomeKey}`. If the parent style is not in any `Resources` dictionary, `BasedOn` will fail at XAML load time with a resource-not-found error.
2. **Only styles with known keys can be referenced.** The auto-generated key is `{SelectorString}` (e.g., `{Button.primary}`). If you move the parent style into a `ResourceDictionary.MergedDictionary`, the key is preserved.
3. **Circular references crash the app.** If `A` is `BasedOn` `B` and `B` is `BasedOn` `A`, the style resolution stack overflows.

In practice, `BasedOn` is rarely needed. The cascade from more-specific selectors (e.g., `Button.primary.danger` having higher specificity than `Button.primary`) already provides inheritance-like behavior for property overrides. `BasedOn` is useful when you want to reuse a *set of setter values* across unrelated selectors.

---

## 8. Styles vs Control Themes: Why Two Concepts Exist

```xml
<!-- Style: additive, overrides specific properties -->
<Style Selector="Button.primary">
  <Setter Property="Background" Value="#6a33ff" />
</Style>

<!-- ControlTheme: replaces the entire visual tree -->
<ControlTheme TargetType="Button">
  <Setter Property="Template">
    <ControlTemplate>
      <Border Background="{TemplateBinding Background}">
        <ContentPresenter />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

**A Style** modifies properties of an existing control. It does not change the control's visual structure — the `Button` still has its default `Chrome` border, `ContentPresenter`, and ripple effect. A style just overrides some of the property values that feed into that structure.

**A ControlTheme** replaces the entire visual tree (the `Template`). It defines what the control looks like from scratch. A `Button` with a custom `ControlTheme` might render as a circle, a gradient pill, or a flat rectangle with no chrome.

### Specificity Between Styles and ControlThemes

ControlThemes live in a separate layer of the styling system with higher priority than styles. A ControlTheme setter for `Background` wins over any non-local style setter for `Background`. This ensures that if a control theme defines the default look of a control, page-level styles (like `Button.primary`) can override specific properties, but the structural `Template` from the ControlTheme remains unless overridden by another ControlTheme.

### When to Use Which

- **Style:** 95% of your work. Coloring, sizing, spacing, font changes, border radius.
- **ControlTheme:** Complete visual overhaul. Creating a custom look that redefines the control's structure (e.g., a toggle switch that is not a standard CheckBox, a card that is not a standard Border).

---

## Common Mistakes

1. **Used comma-separated classes: `Classes="primary, danger"`.**
   Avalonia treats the comma as part of the class name. You get classes named `"primary,"` and `" danger"`. Use spaces only.

2. **Selector case sensitivity.** Selectors are case-insensitive for type names (`button` matches `Button`) but case-sensitive for class names. `Button.Primary` does not match `Classes="primary"`.

3. **Style order in `Application.Styles`.** Styles with equal specificity are resolved in source order. If `Button.primary` appears after `Button.primary.danger`, the `Background` from `Button.primary` overrides `Button.primary.danger`'s `Background` (because they have equal specificity for the `Background` property on an element that has both classes). Put more specific styles later.

4. **Changing `PseudoClasses` from code without calling `PseudoClasses.Set()`.** The `PseudoClasses` collection is an `IAvaloniaList<string>` — using `.Add()` and `.Remove()` works but does not have the same performance characteristics as `.Set()`. Use `PseudoClasses.Set(":name", isActive)` which only raises the change event if the state actually changed.

5. **Putting `<Style>` in `Window.Resources` instead of in `Application.Styles`.** `Window.Resources` holds resources (brushes, converters), not styles. While you can put `<Style>` there, the conventional and documented place for styles is `<Window.Styles>` (or `<Application.Styles>`). `Window.Resources` works, but confuses readers.

---

## See Also

- [003 — Basic Styling (original tutorial)](003-basic-styling.md)
- [003X — Basic Styling (examples)](003-basic-styling-examples.md)
- [006 — Resources (Static & Dynamic)](006-resources.md)
- [006V — Resources (verbose companion)](006-resources-verbose.md)
- [012 — Control Themes vs Styles](../intermediate/012-control-themes-vs-styles.md)
- [Avalonia Docs: Styles](https://docs.avaloniaui.net/docs/styling/styles)
