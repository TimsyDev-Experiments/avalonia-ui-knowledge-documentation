---
tier: intermediate
topic: styling
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 012-control-themes-vs-styles.md
---

# 012V — Control Themes vs Styles: An In-Depth Companion

**Why this exists:** The original tutorial draws the boundary between styles and control themes. This companion explains *why the distinction exists*, *what happens inside the property resolution system* when each applies, and *how to decide* which mechanism to use (and when neither is correct).

**Cross-reference:** Original tutorial at [012-control-themes-vs-styles.md](012-control-themes-vs-styles.md).

---

## 1. Why two mechanisms exist

Avalonia's rendering pipeline has two distinct layers that control what a control looks like:

1. **Template layer** — defines the visual tree (the `ControlTemplate`): which `Border`, `ContentPresenter`, `ScrollViewer`, etc. make up the control. This is like the blueprint.
2. **Property layer** — defines the values for each property of each part of that visual tree: the `Background` brush, the `CornerRadius`, the `FontSize`.

Styles operate on the property layer. Control themes operate on the template layer and can also set properties.

**Why not just use styles for everything?** A style can only set properties that already exist on a control. It cannot change the structure. If you want a `Button` that has an icon and a label stacked vertically (instead of the default single `ContentPresenter`), a style cannot do that — there is no "put two things here" property. You need a `ControlTheme` that replaces the `Template`.

**Why not just use control themes for everything?** Control themes are heavier. They replace the entire visual tree, which means the control loses its default behavior (like `PointerOver` background, focus visual, etc.) unless you re-implement it inside the theme. A style, by contrast, inherits all the default state animations from the default theme. You set one property (`Background="Red"`) and the existing `:pointerover` and `:pressed` pseudo-class styles in the theme still work.

---

## 2. Style resolution — how selectors match

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" Value="#6a33ff" />
</Style>
```

A style selector is a CSS-like pattern. Avalonia compiles it to a `Selector` tree. When the control is initialized (or when its style classes change), Avalonia walks all active styles in the `Styles` collection (application-level, window-level, control-level), tests each selector against the control, and collects matching setters.

**Selector matching order:** Styles are evaluated in specificity order, not document order. Avalonia calculates specificity using rules similar to CSS: pseudo-classes > type selectors > class selectors. When two styles set the same property, the one with higher specificity wins. If specificity is equal, the style that was added last wins.

**Why this matters:** If you write:

```xml
<Style Selector="Button.primary">
  <Setter Property="Background" Value="#6a33ff" />
</Style>
<Style Selector="Button">
  <Setter Property="Background" Value="Gray" />
</Style>
```

The `.primary` style will match only buttons with `Classes="primary"`. But if a button has both `Classes="primary"` and `Classes="secondary"`, only `.primary`'s background applies — unless `.secondary` has higher specificity.

**Key difference from WPF:** WPF's `Trigger`s fire in document order and stop at the first match. Avalonia's style system merges all matching selectors across all style collections, then resolves conflicts by specificity/last-wins.

---

## 3. Control theme resolution — how it differs

```xml
<ControlTheme TargetType="Button" x:Key="RoundedButton">
  <Setter Property="Template">
    <ControlTemplate TargetType="Button">
      <Border Background="{TemplateBinding Background}"
              CornerRadius="{TemplateBinding CornerRadius}">
        <ContentPresenter Content="{TemplateBinding Content}" />
      </Border>
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

Control themes are looked up differently than styles:

- **When you use `Theme="{StaticResource RoundedButton}"`:** Avalonia searches resources for a `ControlTheme` resource with that key. It applies that theme to the control's `Template` property (and any other property setters in the theme).
- **When you omit `x:Key`:** The theme becomes the implicit default theme for that `TargetType`. Every control of that type in scope uses this theme unless overridden by an explicit `Theme` attribute.

**Single-theme constraint:** A control can have only one active `ControlTheme` at a time. If you apply `Theme="{StaticResource A}"`, the default theme (Fluent or Simple) is replaced entirely. You cannot compose multiple control themes — they conflict on the `Template` property.

**What happens at load time:** When a `Button` is instantiated:

1. Avalonia checks if `Theme` is set explicitly. If yes, it loads that `ControlTheme`.
2. If not, it walks up the logical tree looking for an implicit `ControlTheme` for `Button` (a theme with no `x:Key`).
3. If none is found, it falls back to the default theme provided by `FluentTheme` or `SimpleTheme`.

---

## 4. Anatomy of a ControlTemplate — what TemplateBinding does

```xml
<ControlTemplate TargetType="Button">
  <Border Background="{TemplateBinding Background}"
          CornerRadius="{TemplateBinding CornerRadius}">
    <ContentPresenter Content="{TemplateBinding Content}" />
  </Border>
</ControlTemplate>
```

`{TemplateBinding}` is a lightweight compiled binding that binds to a property on the templated control (the `Button` itself). It is equivalent to:

```xml
<Border Background="{Binding Background, RelativeSource={RelativeSource TemplatedParent}}">
```

**Why `TemplateBinding` exists:** Without it, the `Border.Background` would be whatever the default `Border` background is (transparent), not the `Button.Background` set by the user. Every visual property that the user might customize must be piped through from the control to the template part.

**Common mistake:** Forgetting to propagate properties. If you create a control theme for `Button` and only pipe through `Background`, then `Foreground`, `FontSize`, `CornerRadius`, `Padding`, and all other styled properties will not work unless you add `TemplateBinding` for each one.

**What `ContentPresenter` does:** It is the part of the template that renders the `Content` property. If you omit it, the button's content (text, image, etc.) never appears. Every content-control template must include a `ContentPresenter` bound to `{TemplateBinding Content}`.

---

## 5. Nested styles inside control themes — the ^ selector

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

  <Style Selector="^/pointerover/">
    <Setter Property="Background" Value="#5a2ae0" />
  </Style>
</ControlTheme>
```

The `^` means "the control this theme is applied to." It is a shorthand that avoids repeating the type name. The selector `^/pointerover/` compiles to `Button/pseudo-class(pointerover)` at runtime.

**What this does:** When the user hovers over the button, the theme changes `Background` to `#5a2ae0`. But note: this setter targets `Background` on the **templated control** (the Button), not the `Border` inside the template. The `Border` only sees the change because it uses `{TemplateBinding Background}` — it reads the Button's `Background` property, which the style setter just changed.

**Design principle:** Keep pseudo-class state animations on the templated control's properties, not on template child names. This way the template is a pure function of the control's property values and does not need to know about state transitions.

**Why not use `VisualStateManager` like WPF?** Avalonia supports visual state groups (the `Template` can contain `VisualStateManager`), but the `^/pointerover/` syntax is simpler for single-property changes and preserves the pseudo-class system. Use `VisualStateManager` when multiple properties change simultaneously across multiple child elements.

---

## 6. Overriding default themes without replacing the template

```xml
<ControlTheme TargetType="Button">
  <!-- No x:Key = applies to ALL buttons -->
  <Setter Property="CornerRadius" Value="4" />
  <Setter Property="FontWeight" Value="SemiBold" />
</ControlTheme>
```

**What this does:** Every `Button` in scope gets `CornerRadius="4"` and `FontWeight="SemiBold"`. The Fluent theme's `ControlTemplate` still runs — the button still has its pointer-over highlight, pressed state, and ripple effect. You are only overriding two property values.

**When to do this vs. using a regular style:**

```xml
<Style Selector="Button">
  <Setter Property="CornerRadius" Value="4" />
</Style>
```

Both achieve the same visual result for `CornerRadius`. The difference is semantic and specificity:

- A `Style` with `Selector="Button"` has lower specificity than a property setter inside a `ControlTheme`.
- A `ControlTheme` with no `x:Key` is the *default theme* — it replaces the built-in default theme entirely (but still uses the Fluent template if you don't set `Template`).
- A `Style` can be conditional (based on classes, parents, etc.). A `ControlTheme` without `x:Key` applies unconditionally to all controls of that type.

**Rule of thumb:** Use a style for conditional, class-based, or scoped overrides. Use a keyless `ControlTheme` to set application-wide defaults for a control type.

---

## 7. When to use each — decision flow

```
Do you need to change the visual structure of the control?
  Yes → ControlTheme (with Template setter)
  No  → Style or keyless ControlTheme (property overrides only)

Do you need the override to apply to only some instances?
  Yes → Style (with class selector) or keyed ControlTheme (with Theme=)
  No  → keyless ControlTheme

Are you changing behavior (event handlers, bindings, animations)?
  Yes → ControlTemplate (inside ControlTheme)
  No  → Setter on existing property

Is the change temporary (theme toggle at runtime)?
  Yes → DynamicResource in a ControlTheme or Style
  No  → StaticResource
```

---

## 8. Scoping styles and themes

Styles and themes are scoped to the `Styles` collection they belong to:

- **Application.Styles** — global to every window
- **Window.Styles** — scoped to one window
- **Any Control.Styles** — scoped to that control and its children

Control themes in `Application.Styles` are global defaults. Control themes in `Window.Styles` override them for that window only.

**Merged dictionaries:** Use `StyleInclude` to load styles from separate files:

```xml
<Application.Styles>
  <FluentTheme />
  <StyleInclude Source="/Assets/Styles/Buttons.axaml" />
</Application.Styles>
```

---

## Key Takeaways

- **Styles** modify existing properties without touching the template. They use CSS-like selectors and can be class-based, conditional, or scoped.
- **Control themes** replace the entire visual tree. They can also set properties. A control has exactly one active theme.
- Use `TemplateBinding` inside `ControlTemplate` to propagate control properties to template parts.
- The `^` selector inside a `ControlTheme` refers to the templated control, enabling pseudo-class state changes.
- A keyless `ControlTheme` (no `x:Key`) becomes the default theme for that type, overriding Fluent/Simple defaults.
- Use `DynamicResource` for runtime-switchable values; use `StaticResource` otherwise.

---

## See Also

- [012 — Control Themes vs Styles (original)](012-control-themes-vs-styles.md)
- [003 — Basic Styling](../basics/003-basic-styling.md)
- [017 — Theme Switching](017-theme-switching.md)
- [012E — Control Themes vs Styles (examples)](012-control-themes-vs-styles-examples.md)
- [Avalonia Docs: Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
