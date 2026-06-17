---
tier: intermediate
topic: control themes vs styles
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 012-control-themes-vs-styles.md
---

# Quiz — Control Themes vs Styles

```quiz
Q: Which of the following correctly describes the difference between a Style and a ControlTheme in Avalonia?
A. A Style replaces the control's template; a ControlTheme sets individual property values (correct) || Incorrect — this is backwards.
B. A Style sets individual property values; a ControlTheme replaces the control's template || Correct. Styles modify properties on existing templates; ControlThemes define the full template structure.
C. Both are interchangeable and use the same syntax || Incorrect — they use different elements (Style vs ControlTheme) and selectors.
D. A Style can only be used with primitive types; a ControlTheme only with composite controls || Incorrect — either can be used with any control type.
Explanation: Styles use a Selector and modify specific properties. ControlThemes use TargetType (no selector) and define the visual tree via Template.
```

```quiz
Q: How do you apply a named control theme to a Button?
A. <Button Style="{StaticResource RoundedButton}" /> || Incorrect — Style references a Style, not a ControlTheme.
B. <Button Theme="{StaticResource RoundedButton}" /> (correct) || Correct. The Theme property applies a ControlTheme to a control instance.
C. <Button Class="RoundedButton" /> || Incorrect — Classes are used with Style selectors, not ControlThemes.
D. <Button Template="{StaticResource RoundedButton}" /> || Incorrect — Template is a property of the ControlTheme, not applied directly.
Explanation: ControlThemes are referenced via the `Theme` attached property, not through selectors or the Style property.
```

```quiz
Q: What does the `^` symbol represent inside a ControlTheme selector?
A. The root visual of the template || Incorrect — the root visual is defined inside ControlTemplate.
B. The control the theme is being applied to (correct) || Correct. `^` is a shorthand for the target control within a ControlTheme.
C. The Application instance || Incorrect — `^` is scoped to the control theme, not the application.
D. The parent control in the visual tree || Incorrect — `^` refers to the themed control itself, not its parent.
Explanation: Inside a ControlTheme, `^` in nested styles refers to the control instance the theme is applied to, enabling pseudo-class selectors like `^/pointerover/`.
```

```quiz
Q: A developer writes a ControlTheme with no x:Key. What happens?
A. It applies to every control of that TargetType in scope (correct) || Correct. An unkeyed ControlTheme overrides the default template for the type.
B. It is ignored — every ControlTheme requires an x:Key || Incorrect — x:Key is optional; omitting it makes the theme the default for that type.
C. It throws a compile-time error || Incorrect — this is valid XAML and will compile.
D. It applies only if the control has no existing theme || Incorrect — it replaces the existing default theme.
Explanation: A ControlTheme without x:Key becomes the implicit default theme for the TargetType, overriding the theme provided by FluentTheme or SimpleTheme.
```

```quiz
Q: Which resource marker must be used when referencing theme-dependent brushes inside a ControlTheme?
A. {StaticResource ...} || Incorrect — StaticResource is evaluated once and does not react to theme changes.
B. {DynamicResource ...} || Correct wait — actually the tutorial doesn't specify this for ControlThemes specifically. Let me reconsider.
C. {TemplateBinding ...} (correct) || Correct. TemplateBinding binds to the templated parent's properties, which is the standard pattern inside ControlTemplate.
D. {Binding ...} || Incorrect — TemplateBinding is preferred inside control templates for performance and correctness.
Explanation: Inside a ControlTemplate within a ControlTheme, TemplateBinding binds to the target control's properties, allowing the theme to respect instance-level property values set on the control.
```

```quiz
Q: What is the correct way to style a Button's background when pressed using a ControlTheme?
A. <Style Selector="Button:pressed"><Setter Property="Background" Value="Red" /></Style> || Incorrect — inside a ControlTheme, use `^` not the full type name in nested selectors.
B. <Style Selector="^/pressed/"><Setter Property="Background" Value="Red" /></Style> (correct) || Correct. The `^` refers to the themed control and nested styles use the pseudo-class syntax `/pressed/`.
C. <VisualState Name="Pressed"><Setter Property="Background" Value="Red" /></VisualState> || Incorrect — Avalonia uses pseudo-class selectors, not VisualState, inside ControlThemes.
D. <ControlTemplate.Triggers><Trigger Property="IsPressed" Value="True">...</Trigger></ControlTemplate.Triggers> || Incorrect — Avalonia does not use WPF-style triggers.
Explanation: Pseudo-class states inside a ControlTheme are authored as nested `<Style>` elements with the `^/pressed/` selector pattern.
```
