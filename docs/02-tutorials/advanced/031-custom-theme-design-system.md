---
tier: advanced
topic: theming
estimated: 45 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 031 -- Building a Complete Custom Theme and Design System

**What you'll learn:** How to create a full custom theme that replaces FluentTheme with your own design tokens, control themes, and component variants.

**Prerequisites:** [012 -- Control Themes vs Styles](/docs/02-tutorials/intermediate/012-control-themes-vs-styles.md), [006 -- Resources](/docs/02-tutorials/basics/006-resources.md)

---

## 1. Define design tokens

Create `Theme/DesignTokens.axaml` with your colour palette and spacing system:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- Brand palette -->
  <Color x:Key="BrandPrimary">#6366F1</Color>
  <Color x:Key="BrandSecondary">#8B5CF6</Color>
  <Color x:Key="BrandAccent">#06B6D4</Color>

  <!-- Neutral palette -->
  <Color x:Key="NeutralWhite">#FFFFFF</Color>
  <Color x:Key="Neutral50">#FAFAFA</Color>
  <Color x:Key="Neutral100">#F5F5F5</Color>
  <Color x:Key="Neutral200">#E5E5E5</Color>
  <Color x:Key="Neutral700">#404040</Color>
  <Color x:Key="Neutral900">#171717</Color>

  <!-- Semantic -->
  <Color x:Key="Success">#22C55E</Color>
  <Color x:Key="Warning">#F59E0B</Color>
  <Color x:Key="Danger">#EF4444</Color>
  <Color x:Key="Info">#3B82F6</Color>

  <!-- Spacing scale -->
  <x:Double x:Key="Space0">0</x:Double>
  <x:Double x:Key="Space1">4</x:Double>
  <x:Double x:Key="Space2">8</x:Double>
  <x:Double x:Key="Space3">12</x:Double>
  <x:Double x:Key="Space4">16</x:Double>
  <x:Double x:Key="Space5">24</x:Double>
  <x:Double x:Key="Space6">32</x:Double>
  <x:Double x:Key="Space7">48</x:Double>

  <!-- Typography scale -->
  <x:Double x:Key="FontSizeXs">12</x:Double>
  <x:Double x:Key="FontSizeSm">14</x:Double>
  <x:Double x:Key="FontSizeBase">16</x:Double>
  <x:Double x:Key="FontSizeLg">20</x:Double>
  <x:Double x:Key="FontSizeXl">24</x:Double>
  <x:Double x:Key="FontSize2xl">32</x:Double>

  <!-- Corner radii -->
  <CornerRadius x:Key="RadiusNone">0</CornerRadius>
  <CornerRadius x:Key="RadiusSm">4</CornerRadius>
  <CornerRadius x:Key="RadiusMd">8</CornerRadius>
  <CornerRadius x:Key="RadiusLg">12</CornerRadius>
  <CornerRadius x:Key="RadiusFull">999</CornerRadius>
</ResourceDictionary>
```

## 2. Create theme-aware colour scheme

Create `Theme/ThemeVariants.axaml` with light and dark variants:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
      <SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF" />
      <SolidColorBrush x:Key="SurfaceAltBrush" Color="#F5F5F5" />
      <SolidColorBrush x:Key="TextPrimaryBrush" Color="#171717" />
      <SolidColorBrush x:Key="TextSecondaryBrush" Color="#737373" />
      <SolidColorBrush x:Key="BorderBrush" Color="#E5E5E5" />
      <SolidColorBrush x:Key="OverlayBrush" Color="#80000000" />
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
      <SolidColorBrush x:Key="SurfaceBrush" Color="#1A1A2E" />
      <SolidColorBrush x:Key="SurfaceAltBrush" Color="#16213E" />
      <SolidColorBrush x:Key="TextPrimaryBrush" Color="#FAFAFA" />
      <SolidColorBrush x:Key="TextSecondaryBrush" Color="#A3A3A3" />
      <SolidColorBrush x:Key="BorderBrush" Color="#334155" />
      <SolidColorBrush x:Key="OverlayBrush" Color="#80000000" />
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

## 3. Create a Button control theme

Create `Theme/Controls/Button.axaml`:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ControlTheme x:Key="{x:Type Button}" TargetType="Button">
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    <Setter Property="Background" Value="{DynamicResource SurfaceAltBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}" />
    <Setter Property="Padding" Value="16,8" />
    <Setter Property="FontSize" Value="{StaticResource FontSizeSm}" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Cursor" Value="Hand" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
                Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="{TemplateBinding BorderThickness}"
                CornerRadius="{TemplateBinding CornerRadius}"
                BoxShadow="{TemplateBinding BoxShadow}"
                Padding="{TemplateBinding Padding}">
          <ContentPresenter Name="PART_Content"
                            Content="{TemplateBinding Content}"
                            HorizontalContentAlignment="Center"
                            VerticalContentAlignment="Center" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:pointerover /template/ PART_Border">
      <Setter Property="Background" Value="{StaticResource BrandPrimary}" />
      <Setter Property="BorderBrush" Value="{StaticResource BrandPrimary}" />
    </Style>
    <Style Selector="^:pressed /template/ PART_Border">
      <Setter Property="Background" Value="#4F46E5" />
    </Style>
    <Style Selector="^:disabled">
      <Setter Property="Opacity" Value="0.5" />
      <Setter Property="Cursor" Value="Arrow" />
    </Style>
  </ControlTheme>

  <!-- Primary variant -->
  <ControlTheme x:Key="PrimaryButton" TargetType="Button"
                BasedOn="{StaticResource {x:Type Button}}">
    <Setter Property="Background" Value="{StaticResource BrandPrimary}" />
    <Setter Property="Foreground" Value="{StaticResource NeutralWhite}" />
    <Setter Property="BorderBrush" Value="{StaticResource BrandPrimary}" />
  </ControlTheme>

  <!-- Danger variant -->
  <ControlTheme x:Key="DangerButton" TargetType="Button"
                BasedOn="{StaticResource {x:Type Button}}">
    <Setter Property="Background" Value="{StaticResource Danger}" />
    <Setter Property="Foreground" Value="{StaticResource NeutralWhite}" />
    <Setter Property="BorderBrush" Value="{StaticResource Danger}" />
  </ControlTheme>
</ResourceDictionary>
```

## 4. Create TextBox and Card themes

Create `Theme/Controls/TextBox.axaml`:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ControlTheme x:Key="{x:Type TextBox}" TargetType="TextBox">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="{StaticResource RadiusMd}" />
    <Setter Property="Padding" Value="12,8" />
    <Setter Property="FontSize" Value="{StaticResource FontSizeBase}" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
                Background="{TemplateBinding Background}"
                BorderBrush="{TemplateBinding BorderBrush}"
                BorderThickness="{TemplateBinding BorderThickness}"
                CornerRadius="{TemplateBinding CornerRadius}">
          <TextPresenter Name="PART_TextPresenter"
                         Text="{TemplateBinding Text}"
                         Watermark="{TemplateBinding Watermark}"
                         CaretBrush="{TemplateBinding CaretBrush}"
                         Padding="{TemplateBinding Padding}" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:focus /template/ PART_Border">
      <Setter Property="BorderBrush" Value="{StaticResource BrandPrimary}" />
      <Setter Property="BorderThickness" Value="2" />
    </Style>
    <Style Selector="^:disabled /template/ PART_Border">
      <Setter Property="Background" Value="{DynamicResource SurfaceAltBrush}" />
      <Setter Property="Opacity" Value="0.6" />
    </Style>
  </ControlTheme>
</ResourceDictionary>
```

Create `Theme/Controls/Card.axaml`:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ControlTheme x:Key="{x:Type Border}" TargetType="Border">
    <Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="{StaticResource RadiusLg}" />
    <Setter Property="Padding" Value="{StaticResource Space5}" />
    <Setter Property="BoxShadow" Value="0 2 8 0 #15000000" />
  </ControlTheme>

  <ControlTheme x:Key="ElevatedCard" TargetType="Border"
                BasedOn="{StaticResource {x:Type Border}}">
    <Setter Property="BoxShadow" Value="0 4 16 0 #20000000" />
  </ControlTheme>
</ResourceDictionary>
```

## 5. Create the main theme entry point

Create `Theme/Theme.axaml` that imports all components:

```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.MergedDictionaries>
    <ResourceInclude Source="/Theme/DesignTokens.axaml" />
    <ResourceInclude Source="/Theme/ThemeVariants.axaml" />
    <ResourceInclude Source="/Theme/Controls/Button.axaml" />
    <ResourceInclude Source="/Theme/Controls/TextBox.axaml" />
    <ResourceInclude Source="/Theme/Controls/Card.axaml" />
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

## 6. Apply the theme

In `App.axaml`, replace `FluentTheme` with your custom theme:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="DemoApp.App"
             RequestedThemeVariant="Default">

  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <!-- Merge the complete theme -->
        <ResourceInclude Source="/Theme/Theme.axaml" />
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>
</Application>
```

> **Note:** When replacing FluentTheme entirely, you must provide a `ControlTheme` for every control type your app uses, or controls will have no default template.

## 7. Use the theme in views

```xml
<StackPanel Spacing="{StaticResource Space4}">
  <!-- Default button picks up Button theme automatically -->
  <Button Content="Default Button" />

  <!-- Named theme variant -->
  <Button Theme="{StaticResource PrimaryButton}" Content="Primary" />
  <Button Theme="{StaticResource DangerButton}" Content="Delete" />

  <!-- Card with themed Border -->
  <Border Theme="{StaticResource ElevatedCard}">
    <TextBlock Text="Card content"
               Foreground="{DynamicResource TextPrimaryBrush}" />
  </Border>

  <!-- Themed TextBox -->
  <TextBox Text="{Binding Name}"
           PlaceholderText="Enter your name" />
</StackPanel>
```

## Design system checklist

| Component | File | Token dependencies |
|-----------|------|-------------------|
| Design tokens | `DesignTokens.axaml` | None (base definitions) |
| Theme variants | `ThemeVariants.axaml` | Design tokens |
| Button | `Controls/Button.axaml` | Design tokens + Theme variants |
| TextBox | `Controls/TextBox.axaml` | Design tokens + Theme variants |
| Card (Border) | `Controls/Card.axaml` | Design tokens + Theme variants |

## Key takeaways

- Design tokens are the single source of truth for colours, spacing, and typography
- `ControlTheme` replaces each control's visual tree with your own template
- Theme dictionaries provide light/dark variants referenced via `DynamicResource`
- Named theme variants (`BasedOn` a base theme) provide component modifiers
- Removing FluentTheme gives full control but requires themes for every control used
