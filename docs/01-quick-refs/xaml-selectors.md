---
topic: styling
estimated: 2 min read
researched: 2026-06-12
avalonia-version: 12.0.4
---

# XAML Style Selectors Cheat Sheet

## Basic Selectors

| Selector | Matches |
|---|---|
| `Button` | All Button controls |
| `Button.primary` | Buttons with `Classes="primary"` |
| `Button#myButton` | Button with `Name="myButton"` |
| `StackPanel > Button` | Direct child Button of StackPanel |
| `StackPanel Button` | Any descendant Button of StackPanel |
| `Button, TextBlock` | Button or TextBlock |

## Pseudo-classes

| Selector | When Active |
|---|---|
| `Button /pointerover/` | Mouse is over the element |
| `Button /pressed/` | Element is being pressed |
| `Button /focus/` | Element has keyboard focus |
| `Button /focus-visible/` | Focus was set by keyboard |
| `Button:disabled` | Element is disabled |
| `TextBox:error` | Element has data validation errors |
| `CheckBox:checked` | CheckBox is checked |

## Nesting in Control Themes

```xml
<ControlTheme TargetType="Button" x:Key="MyButton">
  <Setter Property="Template"> ... </Setter>
  <Style Selector="^/pointerover/">
    <Setter Property="Background" Value="Blue" />
  </Style>
</ControlTheme>
```

`^` refers to the control the theme is applied to.

## Specificity

Ordered by priority (highest wins):

1. `!important` suffix
2. `ControlTheme` (keyed or unkeyed)
3. `#Name` selector
4. `.class` selector
5. Type selector

## Common Patterns

```xml
<!-- Toggle on pointer hover -->
<Style Selector="Button:hover">
  <Setter Property="Opacity" Value="0.9" />
</Style>

<!-- Selected item in a ListBox -->
<Style Selector="ListBoxItem:selected /pointerover/">
  <Setter Property="Background" Value="{DynamicResource SystemAccentColor}" />
</Style>

<!-- First and last child -->
<Style Selector="Button:nth-child(1)">
  <Setter Property="CornerRadius" Value="8,0,0,8" />
</Style>
<Style Selector="Button:nth-last-child(1)">
  <Setter Property="CornerRadius" Value="0,8,8,0" />
</Style>
```
