---
tier: basics
topic: styling
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 003 — Basic Styling: Classes, Selectors, and Control Themes

**What you'll learn:** Apply styles using CSS-like selectors, create reusable style classes, and understand the difference between a style and a control theme.

**Prerequisites:** [001 — Project Setup](001-project-setup.md)

---

## 1. Inline style (quick but not reusable)

```xml
<Button Content="Click"
        Background="Blue"
        Foreground="White"
        FontSize="16" />
```

Works for one-off tweaks. Avoid for anything you'll reuse.

---

## 2. Named style (reusable in scope)

Add a style to a container or `Window.Resources`:

```xml
<Window.Resources>
  <Style Selector="Button.primary">
    <Setter Property="Background" Value="#6a33ff" />
    <Setter Property="Foreground" Value="White" />
    <Setter Property="FontSize" Value="16" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16,8" />
  </Style>
</Window.Resources>

<Button Content="Save"
        Classes="primary" />
```

The selector `Button.primary` matches any `<Button>` that has the style class `primary`.

---

## 3. Pseudo-classes for interactive states

```xml
<Style Selector="Button.primary /pointerover/">
  <Setter Property="Background" Value="#5a2ae0" />
</Style>

<Style Selector="Button.primary /pressed/">
  <Setter Property="Background" Value="#4a1ac0" />
</Style>

<Style Selector="Button.primary:disabled">
  <Setter Property="Opacity" Value="0.5" />
</Style>
```

Pseudo-classes are wrapped in `/slashes/` (or use `:disabled` for the disabled state).

---

## 4. Scoped styles in App.axaml (global)

Move reusable styles to `App.axaml` so they apply across all windows:

```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.App">
  <Application.Styles>
    <FluentTheme />  <!-- base theme -->

    <Style Selector="Button.primary">
      <Setter Property="Background" Value="#6a33ff" />
      <Setter Property="Foreground" Value="White" />
      <Setter Property="CornerRadius" Value="8" />
    </Style>
  </Application.Styles>
</Application>
```

---

## 5. Style inheritance with `BasedOn`

```xml
<Style Selector="Button.primary">
  <Setter Property="CornerRadius" Value="8" />
  <Setter Property="Padding" Value="16,8" />
</Style>

<Style Selector="Button.primary.danger"
       BasedOn="{StaticResource {Button.primary}}">
  <Setter Property="Background" Value="#dc2626" />
</Style>
```

Usage:

```xml
<Button Content="Delete" Classes="primary danger" />
```

---

## 6. Styles vs Control Themes

| | Style | Control Theme |
|---|---|---|
| Scope | Named, reusable via `Classes` | Replaces all default template visuals |
| Specificity | Lower (overridable) | Higher (overrides defaults) |
| Use for | Coloring, sizing, font changes | Complete visual overhaul of a control |
| Syntax | `<Style Selector="Button.primary">` | `<ControlTheme TargetType="Button">` |

Control themes are covered in [011 — Control Themes vs Styles](../intermediate/011-control-themes-vs-styles.md).

---

## Key Takeaways

- Style selectors follow CSS-like patterns: `Target.class`, `/pseudo/`, `:disabled`
- Use `Window.Resources` for page-local, `Application.Styles` for global
- Style classes are space-separated: `Classes="primary danger"`
- Prefer styles over inline properties for consistency

---

## See Also

- [011 — Control Themes vs Styles](../intermediate/011-control-themes-vs-styles.md)
- [006 — Resources (Static & Dynamic)](006-resources.md)
- [Avalonia Docs: Styles](https://docs.avaloniaui.net/docs/styling/styles)
