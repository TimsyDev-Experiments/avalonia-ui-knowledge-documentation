---
topic: reference
estimated: 5 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Control Themes: Quick Reference

## What is a ControlTheme?

A `ControlTheme` defines the complete visual appearance (template + nested styles) for a control type. Unlike regular styles, `ControlTheme` lives in **Resources** (not `Styles` collections) and is looked up by type key.

## Basic structure

```xml
<ControlTheme x:Key="{x:Type Button}" TargetType="Button">
  <Setter Property="Background" Value="#6366F1" />
  <Setter Property="Foreground" Value="White" />
  <Setter Property="Template">
    <ControlTemplate>
      <Border Background="{TemplateBinding Background}"
              CornerRadius="6"
              Padding="{TemplateBinding Padding}">
        <ContentPresenter />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

## Key rules

- `x:Key="{x:Type ControlName}"` for implicit application to all controls of that type
- `^` selector inside nested `<Style>` refers to the templated control itself
- `ControlTheme` does **not** cascade (only one applies)
- `ControlTheme` lives in `Resources`, not `Styles`

## Nested pseudo-class styles

```xml
<ControlTheme x:Key="{x:Type Button}" TargetType="Button">
  <Setter Property="Background" Value="#6366F1" />
  <Setter Property="Foreground" Value="White" />
  <Setter Property="Template">
    <ControlTemplate>
      <Border Name="PART_Border"
              Background="{TemplateBinding Background}"
              CornerRadius="6" Padding="16,8">
        <ContentPresenter />
      </Border>
    </ControlTemplate>
  </Setter>

  <Style Selector="^:pointerover /template/ PART_Border">
    <Setter Property="Background" Value="#4F46E5" />
  </Style>
  <Style Selector="^:pressed /template/ PART_Border">
    <Setter Property="Background" Value="#4338CA" />
  </Style>
  <Style Selector="^:disabled">
    <Setter Property="Opacity" Value="0.5" />
  </Style>
</ControlTheme>
```

## Selector prefixes in ControlTheme

| Prefix | Meaning |
|--------|---------|
| `^` | The templated control itself |
| `^ /template/ PART_Name` | Named part inside the template |
| `^:pseudo` | Pseudo-class on the templated control |
| `^ /template/ PART_Name:inner-pseudo` | Pseudo-class on a named part |

> `^` is shorthand for the templated control. It is only valid inside a `ControlTheme`.

## Common pseudo-classes

| Pseudo-class | When active |
|-------------|-------------|
| `:pointerover` | Mouse/pointer hovers |
| `:pressed` | Mouse/pointer button held |
| `:disabled` | `IsEnabled == false` |
| `:focus` | Has keyboard focus |
| `:focus-visible` | Focused via keyboard |
| `:checked` | `IsChecked == true` |
| `:unchecked` | `IsChecked == false` |
| `:indeterminate` | `IsChecked == null` |
| `:selected` | `IsSelected == true` |
| `:expanded` | `IsExpanded == true` |

## TemplateBinding vs RelativeSource

```xml
<!-- OneWay only -->
<TextBlock Text="{TemplateBinding Title}" />

<!-- Two-way inside template -->
<TextBox Text="{Binding Title,
  RelativeSource={RelativeSource TemplatedParent},
  Mode=TwoWay}" />
```

## Applying a named theme

```xml
<Button Theme="{StaticResource PillButton}" Content="Click" />
```

Or via style:

```xml
<Style Selector="Button.pill">
  <Setter Property="Theme" Value="{StaticResource PillButton}" />
</Style>
```

## ControlTheme vs Style

| Aspect | Style | ControlTheme |
|--------|-------|-------------|
| Location | `Styles` collection | `Resources` dictionary |
| Key | Selector string | `{x:Type ControlName}` |
| Contains template | Optional | Yes (primary purpose) |
| Cascades | Yes | No |
| Pseudo-class selectors | Works on control itself | Uses `^` prefix |
| Use for | Visual property tweaks | Complete look + template |

## Minimal theme example

```xml
<ControlTheme x:Key="{x:Type TextBox}" TargetType="TextBox">
  <Setter Property="Template">
    <ControlTemplate>
      <Border BorderBrush="{TemplateBinding BorderBrush}"
              BorderThickness="{TemplateBinding BorderThickness}"
              Background="{TemplateBinding Background}">
        <TextPresenter Name="PART_TextPresenter"
                       Text="{TemplateBinding Text}"
                       CaretBrush="{TemplateBinding CaretBrush}" />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

See [Tutorial 012](../02-tutorials/intermediate/012-control-themes-vs-styles.md) for detailed walkthrough.
