---
tier: advanced
topic: theming
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 031-custom-theme-design-system.md
---

# Quiz — Building a Complete Custom Theme and Design System

```quiz
Q: What is the recommended structure for defining colour and spacing values that are shared across all control themes?
A. Define them inline in each ControlTheme's Setter to keep files self-contained || Duplicating tokens across files causes maintenance drift — use a central DesignTokens.axaml instead.
B. Create a DesignTokens.axaml ResourceDictionary with Color, x:Double, and CornerRadius resources (correct) || Design tokens in a central file serve as the single source of truth referenced by all control themes.
C. Define them as C# constants in a static class and bind via x:Static || While possible, the Avalonia design-system convention uses XAML ResourceDictionary for token definitions.
D. Use SystemColors and let each control resolve platform defaults || Platform defaults cannot express a custom brand palette — define explicit design tokens.
Explanation: DesignTokens.axaml centralises brand colours, spacing scale, typography, and corner radii as reusable resources.
```

```quiz
Q: How do you provide light and dark colour variants for your brushes in a custom theme?
A. Create separate Light.axaml and Dark.axaml files and toggle between them in code-behind || This approach works but is less idiomatic than using theme dictionaries.
B. Use ThemeVariant resources with a ThemeDictionaries section inside a ResourceDictionary (correct) || ResourceDictionary.ThemeDictionaries with Light and Dark keys provides automatic variant switching via DynamicResource.
C. Define two SolidColorBrush resources with the same key — Avalonia picks the right one automatically || Resource keys must be unique within a dictionary — use ThemeDictionaries to scope by variant.
D. Override the control's Render method to choose colours based on RequestedThemeVariant || This mixes rendering logic with theme data — use XAML ResourceDictionary approach instead.
Explanation: ThemeDictionaries with Light and Dark sub-dictionaries let DynamicResource references switch automatically when the theme variant changes.
```

```quiz
Q: What is the purpose of the BasedOn attribute on a ControlTheme?
A. It makes the theme inherit setters from a base ControlTheme and override only what differs (correct) || BasedOn creates a derived theme that merges base setters, allowing variant themes (PrimaryButton, DangerButton) to share the template while overriding specific properties.
B. It applies the theme to all child controls in the visual tree || BasedOn does not affect scope — it is for inheritance between ControlTheme definitions.
C. It tells Avalonia to fall back to FluentTheme for unspecified properties || FluentTheme is a separate theme system — BasedOn references another ControlTheme in the same custom theme.
D. It binds the control's template to a base class template automatically || BasedOn inherits setters including the template, but the template itself is set via a Setter in the base theme.
Explanation: BasedOn="{StaticResource {x:Type Button}}" allows a named variant (e.g., PrimaryButton) to reuse the base Button template and override only colours.
```

```quiz
Q: What happens if you replace FluentTheme with your own theme but do not provide a ControlTheme for a control type used in your app?
A. The control renders with a default system theme automatically || There is no fallback — the control will have no default template and may render as invisible or cause errors.
B. Avalonia throws a compile-time warning but the control uses FluentTheme as fallback || There is no automatic fallback — every used control type must have a ControlTheme.
C. The control will have no default template and may not render correctly (correct) || When FluentTheme is removed entirely, every control type used must have its own ControlTheme defined, or it will lack a visual tree.
D. The control inherits a generic default template from the OS || Avalonia does not use OS templates — all templates are theme-provided.
Explanation: Replacing FluentTheme completely requires providing a ControlTheme for every used control type or those controls will lack a template.
```

```quiz
Q: How do you apply a named theme variant (e.g., DangerButton) to a Button in a view?
A. <Button Style="{StaticResource DangerButton}" /> || Named theme variants use the Theme property, not Style — Style is for selectors, Theme is for ControlTheme.
B. <Button Theme="{StaticResource DangerButton}" /> (correct) || The Theme property accepts a ControlTheme resource key, applying the variant's setters (background, foreground, border) on top of the base Button theme.
C. <Button Class="DangerButton" /> || Style classes work with CSS-like selectors, not with ControlTheme variant resolution.
D. <Button ControlTheme="DangerButton" /> || The correct property is Theme, not ControlTheme — Theme is the attached property for applying ControlTheme to an element.
Explanation: Theme="{StaticResource DangerButton}" applies the named ControlTheme variant to a specific Button instance.
```
