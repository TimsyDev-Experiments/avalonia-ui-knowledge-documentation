---
tier: intermediate
topic: styling
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 012 — Control Themes vs Styles

**What you'll learn:** The difference between styles and control themes, when to use each, and how to create a custom control theme.

**Prerequisites:** [003 — Basic Styling](../basics/003-basic-styling.md)

---

## 1. The distinction

| | Style | Control Theme |
|---|---|---|
| Applies to | Controls matching a selector | A specific control type |
| Key | Named (optional) | `TargetType` |
| Override | Values set by the control's default theme | Replaces the entire default theme |
| Syntax | `<Style Selector="Button.primary">` | `<ControlTheme TargetType="Button">` |
| Use for | Coloring, spacing, font | Complete visual redesign |

---

## 2. A style (colors, shape, fonts)

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" Value="#6a33ff" />
  <Setter Property="Foreground" Value="White" />
  <Setter Property="CornerRadius" Value="8" />
</Style>
```

Applied via `Classes="primary"`. Does not change the button's structure — just its appearance.

---

## 3. A control theme (full template replacement)

```xml
<ControlTheme TargetType="Button" x:Key="RoundedButton">
  <Setter Property="CornerRadius" Value="20" />
  <Setter Property="Padding" Value="24,12" />
  <Setter Property="Template">
    <ControlTemplate TargetType="Button">
      <Border Background="{TemplateBinding Background}"
              CornerRadius="{TemplateBinding CornerRadius}"
              Padding="{TemplateBinding Padding}">
        <ContentPresenter Content="{TemplateBinding Content}" />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

Applied via `Theme="{StaticResource RoundedButton}"`:

```xml
<Button Content="Rounded"
        Theme="{StaticResource RoundedButton}" />
```

---

## 4. Default control themes (Fluent, Simple)

Avalonia ships two default theme packs:

- **FluentTheme** — modern Microsoft Fluent design, full control templates
- **SimpleTheme** — lightweight, flat, cross-platform consistent

```xml
<!-- App.axaml -->
<Application.Styles>
  <FluentTheme />
</Application.Styles>
```

---

## 5. Overriding specific properties of a default theme

```xml
<ControlTheme TargetType="Button">
  <!-- No x:Key = applies to ALL buttons -->
  <Setter Property="CornerRadius" Value="4" />
  <Setter Property="FontWeight" Value="SemiBold" />
</ControlTheme>
```

This overrides the default Fluent button appearance for every button in scope, without rewriting the full template.

---

## 6. Control theme with visual states

```xml
<ControlTheme TargetType="Button" x:Key="FancyButton">
  <Setter Property="Template">
    <ControlTemplate TargetType="Button">
      <Border Name="RootBorder"
              Background="{TemplateBinding Background}"
              CornerRadius="8">
        <ContentPresenter Content="{TemplateBinding Content}" />
      </Border>
    </ControlTemplate>
  </Setter>

  <!-- Pointer over -->
  <Style Selector="^/pointerover/">
    <Setter Property="Background" Value="#5a2ae0" />
  </Style>

  <!-- Pressed -->
  <Style Selector="^/pressed/">
    <Setter Property="Background" Value="#4a1ac0" />
  </Style>
</ControlTheme>
```

The `^` in the selector refers to the control the theme is applied to.

---

## Key Takeaways

- **Styles** modify properties of existing templates (colors, sizes)
- **Control themes** replace the entire visual structure (templates)
- Use `Theme="{StaticResource ...}"` to apply a control theme
- Omit `x:Key` on a control theme to override the default template for a type
- Nest style selectors inside `ControlTheme` with `^` for pseudo-class states

---

## See Also

- [003 — Basic Styling](../basics/003-basic-styling.md)
- [012V — Control Themes vs Styles (verbose companion)](012-control-themes-vs-styles-verbose.md)
- [017 — Theme Switching](017-theme-switching.md)
- [012E — Control Themes vs Styles (examples)](012-control-themes-vs-styles-examples.md)
- [Avalonia Docs: Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
