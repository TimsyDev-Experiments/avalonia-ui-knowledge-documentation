---
tier: intermediate
topic: styling
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 012-control-themes-vs-styles.md
---

# 012E — Control Themes vs Styles: Real-World Examples

**What this is:** Two worked examples demonstrating when to use a `ControlTheme` (full template replacement) versus a `Style` (property overrides). Read [012 — Control Themes vs Styles](012-control-themes-vs-styles.md) and [012V — Verbose Companion](012-control-themes-vs-styles-verbose.md) first.

---

## Example 1: Custom Toggle Switch with Full Template Replacement

### Goal

Build a themed toggle switch control that replaces the default `ToggleSwitch` template entirely — custom track shape, animated thumb, and brand colors — using a keyed `ControlTheme`.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _notificationsEnabled;

    [ObservableProperty]
    private bool _darkMode;

    [ObservableProperty]
    private bool _autoSave;
}
```

### XAML — ControlTheme Definition (Themes/BrandToggle.axaml)

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <ControlTheme x:Key="BrandToggle" TargetType="ToggleSwitch">
    <Setter Property="Width" Value="48" />
    <Setter Property="Height" Value="26" />

    <Setter Property="Template">
      <ControlTemplate TargetType="ToggleSwitch">
        <Border Name="TrackBorder"
                Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="1"
                CornerRadius="13">
          <Border Name="Thumb"
                  Width="22" Height="22"
                  HorizontalAlignment="Left"
                  Margin="1,0,0,0"
                  Background="White"
                  CornerRadius="11" />
        </Border>
      </ControlTemplate>
    </Setter>

    <!-- Checked state: move thumb right, color track -->
    <Style Selector="^/checked/">
      <Setter Property="Background" Value="#6a33ff" />
    </Style>
    <Style Selector="^/checked/ /template/Thumb">
      <Setter Property="Margin" Value="24,0,0,0" />
    </Style>

    <!-- Pointer over: lighter track -->
    <Style Selector="^/pointerover/">
      <Setter Property="Background" Value="#DDD" />
    </Style>
    <Style Selector="^/checked/:pointerover">
      <Setter Property="Background" Value="#7a44ff" />
    </Style>

    <!-- Disabled: muted -->
    <Style Selector="^:disabled">
      <Setter Property="Opacity" Value="0.4" />
    </Style>
  </ControlTheme>
</ResourceDictionary>
```

### XAML View

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:vm="using:MyApp.ViewModels"
            x:DataType="vm:SettingsViewModel"
            Spacing="16" Margin="20">

  <TextBlock Text="App Settings" FontSize="20" FontWeight="Bold" />

  <Grid ColumnDefinitions="*,Auto">
    <TextBlock Text="Enable Notifications" VerticalAlignment="Center" />
    <ToggleSwitch Grid.Column="1"
                  IsChecked="{Binding NotificationsEnabled}"
                  Theme="{StaticResource BrandToggle}" />
  </Grid>

  <Grid ColumnDefinitions="*,Auto">
    <TextBlock Text="Dark Mode" VerticalAlignment="Center" />
    <ToggleSwitch Grid.Column="1"
                  IsChecked="{Binding DarkMode}"
                  Theme="{StaticResource BrandToggle}" />
  </Grid>

  <Grid ColumnDefinitions="*,Auto">
    <TextBlock Text="Auto-Save" VerticalAlignment="Center" />
    <ToggleSwitch Grid.Column="1"
                  IsChecked="{Binding AutoSave}"
                  Theme="{StaticResource BrandToggle}" />
  </Grid>
</StackPanel>
```

### How It Works

1. The `ControlTheme` with `x:Key="BrandToggle"` defines a completely new visual tree for `ToggleSwitch` — a rounded `Border` (track) containing a smaller `Border` (thumb).
2. `^/checked/` sets the track `Background` to the brand color and repositions the thumb via margin on the `/template/Thumb` target.
3. `^/pointerover/` provides hover feedback. Combined selectors (`^/checked/:pointerover`) handle hover-on-checked.
4. Each `ToggleSwitch` in the view applies the theme via `Theme="{StaticResource BrandToggle}"`.

### Design Decisions & Trade-offs

- **ControlTheme vs Style:** A `Style` cannot change the toggle's visual structure (track + thumb). Only a `ControlTheme` with a `Template` setter can replace the default `ToggleSwitch` template.
- **`/template/` selector** targets a named child inside the template. This is the way to style template parts from a nested style within the theme.
- **Edge case — accessibility:** Replacing the template removes default accessibility patterns. Add `AutomationProperties` to the template parts if screen-reader support is needed.
- **Trade-off:** A custom theme is more code to maintain. If only colors change (not shape or animation), use a `Style` with `:checked` selector instead.

---

## Example 2: Brand Override Using Keyless ControlTheme + Variant Styles

### Goal

Apply a consistent brand appearance (colors, border radius, typography) to all `Button` and `TextBox` controls application-wide, with class-based variants (`Primary`, `Danger`, `Ghost`) — without replacing the default Fluent template.

### Style Definitions (App.axaml)

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Application.Styles>
    <FluentTheme />

    <!-- Keyless ControlTheme: overrides default button appearance without touching template -->
    <ControlTheme TargetType="Button">
      <Setter Property="CornerRadius" Value="6" />
      <Setter Property="FontWeight" Value="SemiBold" />
      <Setter Property="Padding" Value="16,8" />
      <Setter Property="Background" Value="#6a33ff" />
      <Setter Property="Foreground" Value="White" />
      <Setter Property="BorderThickness" Value="0" />
    </ControlTheme>

    <!-- Class-based variant styles -->
    <Style Selector="Button.danger">
      <Setter Property="Background" Value="#dc3545" />
    </Style>
    <Style Selector="Button.ghost">
      <Setter Property="Background" Value="Transparent" />
      <Setter Property="Foreground" Value="#6a33ff" />
      <Setter Property="BorderThickness" Value="1" />
      <Setter Property="BorderBrush" Value="#6a33ff" />
    </Style>
    <Style Selector="Button.ghost:pointerover">
      <Setter Property="Background" Value="#f0ebff" />
    </Style>

    <!-- Keyless ControlTheme: override TextBox border -->
    <ControlTheme TargetType="TextBox">
      <Setter Property="BorderBrush" Value="#ccc" />
      <Setter Property="BorderThickness" Value="1" />
      <Setter Property="CornerRadius" Value="6" />
    </ControlTheme>
    <Style Selector="TextBox:focus">
      <Setter Property="BorderBrush" Value="#6a33ff" />
      <Setter Property="BorderThickness" Value="2" />
    </Style>
  </Application.Styles>
</Application>
```

### XAML View

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:vm="using:MyApp.ViewModels"
            x:DataType="vm:SettingsViewModel"
            Spacing="12" Margin="20">

  <TextBox Text="{Binding Email}"
           Watermark="Email address" />

  <StackPanel Orientation="Horizontal" Spacing="8">
    <Button Content="Save"
            Command="{Binding SaveCommand}" />
    <Button Content="Discard"
            Classes="danger"
            Command="{Binding DiscardCommand}" />
    <Button Content="Cancel"
            Classes="ghost"
            Command="{Binding CancelCommand}" />
  </StackPanel>
</StackPanel>
```

### How It Works

1. The keyless `ControlTheme TargetType="Button"` applies to every `Button` in the application. It sets `CornerRadius`, `FontWeight`, `Padding`, and default colors. The Fluent template is not replaced — only these property values override the Fluent defaults.
2. Class-based styles add variants: `.danger` changes the background to red, `.ghost` makes it transparent with an outline.
3. The `.ghost:pointerover` style provides hover feedback. Because the `ControlTheme` already set `CornerRadius="6"`, the ghost button inherits the rounded corners.
4. A separate keyless `ControlTheme` for `TextBox` sets the default border appearance. The `:focus` style changes the border to the brand color when focused.

### Design Decisions & Trade-offs

- **Keyless ControlTheme vs Style with Selector="Button":** A keyless `ControlTheme` has higher specificity than a classless `Style`. It also signals intent: "this is the default appearance for all Button controls." A `Style` is better for conditional (class-based) overrides.
- **Why not replace the Fluent template:** The Fluent `Button` template includes pressed animation, ripple effect, focus visual, and disabled state. Replacing it would require reimplementing all of those. Property overrides preserve them.
- **Edge case — conflicting overrides:** If the Fluent theme updates in a future Avalonia version, property overrides continue to work. A custom `Template` would need to be updated manually.
- **Trade-off:** Setting `Background` on the `ControlTheme` makes it the default for all buttons. If a third-party control also styles `Button`, specificity rules apply. Use `!important` sparingly (Avalonia supports it via `Setter` priority).

---

## Comparison

| Aspect | Example 1 — Custom Toggle Theme | Example 2 — Brand Override |
|---|---|---|
| **Approach** | Full template replacement (`Template` setter) | Property-only override (keyless `ControlTheme`) |
| **Keyed?** | Yes (`x:Key="BrandToggle"`) | No (applies to all instances) |
| **Visual structure changed?** | Yes — new track, thumb, layout | No — Fluent template preserved |
| **Defaults preserved?** | No — must reimplement states | Yes — hover, pressed, ripple, focus |
| **Application scope** | Per-instance via `Theme=` | Global |
| **When to use** | Non-standard controls, custom widgets | Branding an existing Fluent app |
| **Maintenance cost** | Higher (template + states + animations) | Lower (just property values) |

---

## See Also

- [012 — Control Themes vs Styles (original)](012-control-themes-vs-styles.md)
- [012V — Control Themes vs Styles (verbose companion)](012-control-themes-vs-styles-verbose.md)
- [003 — Basic Styling](../basics/003-basic-styling.md)
- [017 — Theme Switching](017-theme-switching.md)
- [Avalonia Docs: Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
