---
tier: basics
topic: resources
estimated: 6 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 006 — Resources: StaticResource and DynamicResource

**What you'll learn:** Define reusable brushes, colors, and values in XAML resource dictionaries, and understand when to use `StaticResource` vs `DynamicResource`.

**Prerequisites:** [003 — Basic Styling](003-basic-styling.md)

---

## 1. Defining resources

```xml
<Window.Resources>
  <SolidColorBrush x:Key="PrimaryBrush" Color="#6a33ff" />
  <SolidColorBrush x:Key="ErrorBrush" Color="#dc2626" />
  <x:Double x:Key="DefaultPadding">16</x:Double>
  <CornerRadius x:Key="CardCorner">8</CornerRadius>
</Window.Resources>
```

Primitive type elements (`x:Double`, `x:Int32`, `x:String`) let you store plain values as resources.

---

## 2. StaticResource (looked up once at load)

```xml
<Button Background="{StaticResource PrimaryBrush}"
        Padding="{StaticResource DefaultPadding}"
        Content="Save" />
```

`StaticResource` walks up the logical tree at XAML load time. If the key isn't found, you get a runtime exception.

---

## 3. DynamicResource (re-evaluated when the resource changes)

```xml
<Button Background="{DynamicResource PrimaryBrush}" />
```

Use `DynamicResource` when:
- The resource value can change at runtime (theme switches, user preferences)
- The resource is defined in a theme dictionary
- You're building theme-aware components

---

## 4. Resource lookup order

```
Element.Resources → Parent.Resources → Window.Resources →
Application.Resources → Theme Resources (Fluent/Simple)
```

Definitions lower in the chain override higher ones. A `PrimaryBrush` defined at `Window.Resources` overrides one at `Application.Resources`.

---

## 5. Merged Resource Dictionaries

```xml
<!-- App.axaml -->
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="/Assets/Styles/Colors.axaml" />
      <ResourceInclude Source="/Assets/Styles/Typography.axaml" />
    </ResourceDictionary.MergedDictionaries>

    <!-- App-level resources -->
    <SolidColorBrush x:Key="AppBackground" Color="#fafafa" />
  </ResourceDictionary>
</Application.Resources>
```

The source files are plain `ResourceDictionary` files:

```xml
<!-- Assets/Styles/Colors.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Color x:Key="Brand">#6a33ff</Color>
  <SolidColorBrush x:Key="BrandBrush" Color="{StaticResource Brand}" />
</ResourceDictionary>
```

---

## 6. Theme resources

Theme dictionaries like `FluentTheme` define resources such as `SystemAccentColor`. You can override them:

```xml
<Application.Styles>
  <FluentTheme />
  <Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource SystemAccentColor}" />
  </Style>
</Application.Styles>
```

---

## Key Takeaways

- `StaticResource` for stable values (better performance)
- `DynamicResource` for theme-aware or runtime-changing values
- Use `ResourceDictionary.MergedDictionaries` to organize large resource sets
- Primitive type elements (`x:Double`, `x:String`) work as resource values
- Theme resources are resolved via `DynamicResource`

---

## See Also

- [006V — Resources (verbose companion)](006-resources-verbose.md)
- [006X — Resources (examples)](006-resources-examples.md)
- [003 — Basic Styling](003-basic-styling.md)
- [017 — Theme Switching](../intermediate/017-theme-switching.md)
- [Avalonia Docs: Resources](https://docs.avaloniaui.net/docs/styling/resources)
