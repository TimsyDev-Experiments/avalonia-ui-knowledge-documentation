---
tier: advanced
topic: theming
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 031-custom-theme-design-system.md
---

# 031V — Building a Complete Custom Theme and Design System: An In-Depth Companion

This companion explains the design decisions behind each layer of a custom theme system, how Avalonia resolves theme resources, and how to extend the pattern to more controls. Read it alongside [031 — Building a Complete Custom Theme and Design System](031-custom-theme-design-system.md).

---

## 1. Design Tokens — The Foundation

### What design tokens are and why they exist

Design tokens are the atomic values that define every visual property of your design system: colors, spacing, typography sizes, corner radii, shadows, durations. They exist to:

1. **Eliminate magic numbers** — no `"#6366F1"` scattered across 40 files. If the brand color changes, update one token.
2. **Enforce consistency** — spacing of 4, 8, 12, 16, 24, 32, 48 is a consistent scale. A developer cannot accidentally use 13px.
3. **Enable theme switching** — tokens are referenced with `DynamicResource` so they can be swapped per theme variant.

```xml
<Color x:Key="BrandPrimary">#6366F1</Color>
<x:Double x:Key="Space4">16</x:Double>
<CornerRadius x:Key="RadiusMd">8</CornerRadius>
```

### Why separate token files from theme variant files

`DesignTokens.axaml` defines the *palette* — the raw colors and numbers. `ThemeVariants.axaml` defines the *semantic tokens* — brushes like `TextPrimaryBrush` that use palette colors. This separation means:

- The palette file can be shared across multiple themes (light, dark, high-contrast).
- A dark variant can map `SurfaceBrush` to a different color without touching the brand palette.

### Token naming conventions

The names used (e.g., `Space4`, `FontSizeBase`, `RadiusMd`) follow a scale-based convention:

- **SpaceN** — spacing is a multiplier of 4px. `Space4 = 16px`. This matches the 4px grid used by Fluent and Material Design.
- **FontSizeXs/Sm/Base/Lg/Xl/2xl** — typical font size scale from 12px to 32px.
- **RadiusNone/Sm/Md/Lg/Full** — corner radius progression. `Full = 999` for pill shapes.

### Why use `Color` + `SolidColorBrush` separation

`DesignTokens.axaml` defines `Color` resources (value types, lightweight). `ThemeVariants.axaml` defines `SolidColorBrush` resources (reference types, participate in rendering). The pattern:

- **Colors** — for use in code-behind, converters, or as intermediate values.
- **Brushes** — for use in XAML styles and templates.

Brushes cannot be derived from other brush resources in a resource dictionary without custom markup extensions. Color-to-brush mapping happens explicitly in the theme variant file.

---

## 2. Theme Variants — How Light/Dark Switching Works

```xml
<ResourceDictionary.ThemeDictionaries>
  <ResourceDictionary x:Key="Light">
    <SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF" />
  </ResourceDictionary>
  <ResourceDictionary x:Key="Dark">
    <SolidColorBrush x:Key="SurfaceBrush" Color="#1A1A2E" />
  </ResourceDictionary>
</ResourceDictionary.ThemeDictionaries>
```

### The `ThemeDictionaries` mechanism

`ResourceDictionary.ThemeDictionaries` is a dictionary of dictionaries. Avalonia selects which inner dictionary to use based on the `RequestedThemeVariant`:

1. At startup, Avalonia checks `Application.RequestedThemeVariant`.
2. If set to `"Dark"`, it reads from the `Dark` key in all `ThemeDictionaries`.
3. If set to `"Light"`, it reads from the `Light` key.
4. If `"Default"`, it reads from the system theme (Windows light/dark setting, macOS appearance).

### Why `DynamicResource` references theme variant brushes

```xml
<Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
```

`DynamicResource` is required (not `StaticResource`) because:
- At resource resolution time, the active theme variant is already known.
- When the theme changes at runtime (user switches from light to dark), `DynamicResource` updates the resolved value. `StaticResource` would not.

### What happens during theme switching

1. The user triggers a theme change (e.g., `Application.RequestedThemeVariant = ThemeVariant.Dark`).
2. Avalonia invalidates all property values that use `DynamicResource` pointing to theme-variant resources.
3. Each affected property re-resolves the resource from the newly active dictionary.
4. The visual tree re-renders with the new values.

This is instant and does not require any per-control code.

---

## 3. ControlTheme — What It Replaces

### ControlTheme vs. Style

A `ControlTheme` is a specialized `Style` that replaces a control's **entire visual tree**. A regular `Style` only sets properties on the existing control.

```xml
<ControlTheme x:Key="{x:Type Button}" TargetType="Button">
```

- `{x:Type Button}` as `x:Key` — this theme applies to **all** `Button` controls by default (no explicit `Theme` assignment needed).
- `TargetType="Button"` — the theme is designed for `Button`. The selector `^` inside refers to `Button`.

### The template setter

```xml
<Setter Property="Template">
  <ControlTemplate>
    <Border Name="PART_Border"
            Background="{TemplateBinding Background}"
            ...>
      <ContentPresenter Name="PART_Content"
                        Content="{TemplateBinding Content}" />
    </Border>
  </ControlTemplate>
</Setter>
```

The `Template` setter defines the control's visual structure. Inside a `ControlTemplate`:
- `{TemplateBinding Property}` — binds to the control's property, not the template's data context. This is the templated parent binding mechanism.
- Named parts (e.g., `PART_Border`) let the control's logic interact with template elements (e.g., `Button` looks for `PART_Border` to apply visual states).

### Why it's called "lookless"

Avalonia buttons (like WPF) are "lookless" — the control's logic is separate from its visual representation. `ControlTheme` is how you change the look without changing the logic. The `Button` class handles click routing, command execution, and keyboard interaction — it does not care about the Border, ContentPresenter, or CornerRadius.

### BasedOn — creating theme variants

```xml
<ControlTheme x:Key="PrimaryButton" TargetType="Button"
              BasedOn="{StaticResource {x:Type Button}}">
  <Setter Property="Background" Value="{StaticResource BrandPrimary}" />
</ControlTheme>
```

`BasedOn` inherits the base theme's template and all setters. The variant theme:
- Can override any setter from the base theme.
- Cannot remove setters from the base theme (only override them).
- Inherits the base theme's template (unless it supplies its own `Template` setter).

### Implicit vs. explicit key

- **Implicit** (`x:Key="{x:Type Button}"`): Applied automatically to all `Button` controls.
- **Explicit** (`x:Key="PrimaryButton"`): Applied only when `Theme="{StaticResource PrimaryButton}"` is set on the control.

Both modes can coexist. Controls with no explicit `Theme` attribute pick up the implicit theme by type.

---

## 4. TextBox and Card Themes — Pattern Reuse

### TextBox — a text-input template

```xml
<ControlTemplate>
  <Border Name="PART_Border"
          Background="{TemplateBinding Background}"
          ...>
    <TextPresenter Name="PART_TextPresenter"
                   Text="{TemplateBinding Text}"
                   Watermark="{TemplateBinding Watermark}" />
  </Border>
</ControlTemplate>
```

The `TextBox` template uses:
- `PART_Border` — the border that changes appearance on focus (via the `:focus` style selector).
- `PART_TextPresenter` — renders the text, caret, and selection. `TextPresenter` is `TextBox`'s internal text renderer. You must include it for text input to work.
- `{TemplateBinding Watermark}` — the placeholder text.

### Focus style selector

```xml
<Style Selector="^:focus /template/ PART_Border">
  <Setter Property="BorderBrush" Value="{StaticResource BrandPrimary}" />
  <Setter Property="BorderThickness" Value="2" />
</Style>
```

The `/template/` axis in the selector targets elements *inside* the control template. Without `/template/`, the selector would look for a child `Border` in the logical tree (which doesn't exist — the Border is only in the visual/template tree).

### Card theme on Border — why `{x:Type Border}`

```xml
<ControlTheme x:Key="{x:Type Border}" TargetType="Border">
```

Styling `Border` with a `ControlTheme` is a trick: it makes every `Border` in your app pick up the card styling automatically. When you need a `Border` without card styling, set `Theme="{x:Null}"` or use a different element.

This works because `Border` is a full control type (`TemplatedControl` subclass), so it supports `ControlTheme`. Not all simple elements do — for instance, `ContentControl` supports themes, but `Panel` or `Decorator` subclasses may not.

---

## 5. Theme Entry Point — How Merged Dictionaries Work

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui">
  <ResourceDictionary.MergedDictionaries>
    <ResourceInclude Source="/Theme/DesignTokens.axaml" />
    <ResourceInclude Source="/Theme/ThemeVariants.axaml" />
    <ResourceInclude Source="/Theme/Controls/Button.axaml" />
    <ResourceInclude Source="/Theme/Controls/TextBox.axaml" />
    <ResourceInclude Source="/Theme/Controls/Card.axaml" />
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

### Merge order matters

Resources are resolved in reverse merge order (last merged dictionary checked first). If two files define the same key, the last one wins. The recommended order:

1. **DesignTokens.axaml** — base definitions, merged first.
2. **ThemeVariants.axaml** — builds on tokens, merged second.
3. **Control themes** — uses both tokens and variant brushes, merged after.

When a control theme references `{StaticResource BrandPrimary}`, it searches:
1. The control theme file itself.
2. `ThemeVariants.axaml` (merged later, checked earlier? No — merged later = checked first).
3. `DesignTokens.axaml`.

### Why `ResourceInclude` and not `StyleInclude`

- `ResourceInclude` merges `ResourceDictionary` files at runtime. The dictionary's content becomes part of the parent dictionary.
- `StyleInclude` merges files that contain `<Style>` elements (not `ControlTheme` or `ResourceDictionary`). Use `ResourceInclude` for all design system files.

### The source path format

```xml
<ResourceInclude Source="/Theme/DesignTokens.axaml" />
```

The `/` prefix means the path is relative to the **project root** (the `.csproj` directory). This is an `avares://` URI in disguise (`avares://DemoApp/Theme/DesignTokens.axaml`). Using the shorter `/Theme/...` form is idiomatic in Avalonia.

---

## 6. Applying the Theme — Replacing FluentTheme

```xml
<Application xmlns="https://github.com/avaloniaui"
             x:Class="DemoApp.App"
             RequestedThemeVariant="Default">

  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source="/Theme/Theme.axaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>
</Application>
```

### What happens when FluentTheme is removed

By removing `FluentTheme` (no `<Application.Styles><FluentTheme/></Application.Styles>`), you take full control:
- No built-in styles are applied to any control.
- Every control type you use **must** have a `ControlTheme` in your merged dictionaries, or it will have no visual template.
- Controls without a template still exist and function (they respond to input, raise events) but are invisible (zero-size because they have no visual tree).

### The "Default" theme variant

`RequestedThemeVariant="Default"` tells Avalonia to follow the system setting. The user's OS preference for light/dark mode determines which theme dictionary is active. Set to `"Light"` or `"Dark"` for override.

### Adding the theme in code-behind

If you need to switch themes at runtime from code:

```csharp
Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
```

This triggers the resource re-resolution described in section 2.

---

## 7. Using the Theme in Views — Static vs. Dynamic Resources

```xml
<StackPanel Spacing="{StaticResource Space4}">
  <Button Content="Default Button" />
  <Button Theme="{StaticResource PrimaryButton}" Content="Primary" />
  <Border Theme="{StaticResource ElevatedCard}">
    <TextBlock Text="Card content"
               Foreground="{DynamicResource TextPrimaryBrush}" />
  </Border>
</StackPanel>
```

### When to use `StaticResource` vs. `DynamicResource`

| Context | Use | Why |
|---|---|---|
| `Spacing`, `FontSize`, `CornerRadius` | `StaticResource` | These come from `DesignTokens.axaml`, which does not change at runtime. StaticResource resolves once and is faster. |
| `Foreground`, `Background`, `BorderBrush` | `DynamicResource` | These reference theme variant brushes that change on light/dark switch. DynamicResource follows the theme change. |
| `Theme` property | `StaticResource` | Theme assignments are typically static per view. Dynamic theme switching is handled by `RequestedThemeVariant`, not by changing `Theme` properties. |

### The `Spacing` property on StackPanel

`StackPanel.Spacing` is a convenience property that sets the gap between child elements. Using `{StaticResource Space4}` here ensures consistent 16px spacing across all panels in the app.

---

## Extending the Design System

### Adding a new control type

1. Create `Theme/Controls/ComboBox.axaml`.
2. Define `<ControlTheme x:Key="{x:Type ComboBox}" TargetType="ComboBox">`.
3. Add the template (copied and modified from the default FluentTheme or written from scratch).
4. Import in `Theme.axaml`: `<ResourceInclude Source="/Theme/Controls/ComboBox.axaml" />`.

### Adding a new variant (e.g., High Contrast)

1. Add a `<ResourceDictionary x:Key="HighContrast">` block in `ThemeVariants.axaml`.
2. Define high-contrast colors for all semantic brush keys.
3. Set `RequestedThemeVariant="HighContrast"` — note: this requires a custom `ThemeVariant` instance if you want a new variant beyond Light/Dark.
4. Custom theme variants must be registered: `ThemeVariant.Register("HighContrast")`.

### Component variant pattern

For components with multiple visual modes (e.g., `SmallButton`, `LargeButton`, `IconButton`), follow the `BasedOn` pattern:

```xml
<ControlTheme x:Key="IconButton" TargetType="Button"
              BasedOn="{StaticResource {x:Type Button}}">
  <Setter Property="Padding" Value="8" />
  <Setter Property="CornerRadius" Value="{StaticResource RadiusFull}" />
</ControlTheme>
```

---

## See Also

- [031 — Building a Complete Custom Theme (original)](031-custom-theme-design-system.md)
- [012 — Control Themes vs Styles](../intermediate/012-control-themes-vs-styles.md)
- [006 — Resources](../basics/006-resources.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
- [Avalonia Docs: Resources](https://docs.avaloniaui.net/docs/styling/resources)
- [031X — Building a Complete Custom Theme (examples)](031-custom-theme-design-system-examples.md)
