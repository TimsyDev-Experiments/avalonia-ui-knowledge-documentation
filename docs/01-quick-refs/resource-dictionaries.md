---
topic: reference
estimated: 5 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Resource Dictionaries: Quick Reference

## Defining resources at different scopes

| Scope | XAML Element | Lifetime |
|-------|-------------|----------|
| Application-wide | `<Application.Resources>` | App start to exit |
| Window-level | `<Window.Resources>` | Window open to close |
| Control-level | `<UserControl.Resources>` (or any `Control`) | Control lifetime |
| Style-scoped | `<Style.Resources>` | Within that style only |

## Resource lookup order

1. Element's own `Resources`
2. Merged dictionaries at that level
3. Parent element's `Resources` (and merged)
4. Walk up logical tree
5. Style resources at each level
6. `Application.Resources` + merged dictionaries
7. Theme resources

First match wins.

## Merged dictionaries

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="/Resources/Colors.axaml" />
      <ResourceInclude Source="/Resources/Styles.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

| Element | Behavior |
|---------|----------|
| `ResourceInclude` | Creates separate dictionary scope |
| `MergeResourceInclude` | Merges inline into parent dictionary |

## Theme dictionaries

```xml
<ResourceDictionary>
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
      <SolidColorBrush x:Key="CardBackground" Color="White" />
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
      <SolidColorBrush x:Key="CardBackground" Color="#1E1E1E" />
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

Use `DynamicResource` to reference theme-variant resources.

## StaticResource vs DynamicResource

| Aspect | StaticResource | DynamicResource |
|--------|---------------|----------------|
| Resolution | Once at load | Monitors for changes |
| Error | Throws if missing | Silent fallback |
| Performance | Faster | Slightly slower |
| Use for | Converters, templates, constants | Theme colors, brushes, dynamic sizes |

## Accessing from code

```csharp
// Direct dictionary (no merged search)
var brush = (SolidColorBrush)this.Resources["PrimaryBrush"];

// Search merged at current level only
this.TryGetResource("PrimaryBrush", this.ActualThemeVariant, out var result);

// Walk full tree (most common)
this.TryFindResource("PrimaryBrush", this.ActualThemeVariant, out var found);

// Observable for runtime changes
myBorder.Bind(Border.BackgroundProperty,
    this.GetResourceObservable("PrimaryBrush"));
```

## Common resource types

| XAML Type | .NET Type | Usage |
|-----------|-----------|-------|
| `<SolidColorBrush>` | `SolidColorBrush` | Background, Foreground, BorderBrush |
| `<x:Double>` | `double` | FontSize, Spacing, Width/Height |
| `<Thickness>` | `Thickness` | Margin, Padding, BorderThickness |
| `<Color>` | `Color` | Used as component for brushes |
| `<FontFamily>` | `FontFamily` | Custom font references |
| `<x:String>` | `string` | Labels, titles, URIs |
| `<CornerRadius>` | `CornerRadius` | Border corner radii |
| `<BoxShadows>` | `BoxShadows` | Shadow effects |

## Common patterns

```xml
<!-- Converter as resource -->
<Application.Resources>
  <local:BoolToVisibilityConverter x:Key="BoolToVis" />
</Application.Resources>

<!-- Font family -->
<FontFamily x:Key="AppFont">avares://MyApp/Assets/Fonts#Inter</FontFamily>

<!-- Named spacing -->
<x:Double x:Key="SpaceSmall">4</x:Double>
<x:Double x:Key="SpaceMedium">12</x:Double>

<!-- Named color -->
<Color x:Key="BrandColor">#6366F1</Color>
```

> **Tip:** Always wrap `Application.Resources` in a `<ResourceDictionary>` element when using `MergedDictionaries`, or font resources may fail silently.
