---
tier: advanced
topic: theming
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 031-custom-theme-design-system.md
---

# 031X — Building a Complete Custom Theme and Design System: Real-World Examples

## Scenario 1: Dark-Mode Dashboard Design System with ToggleSwitch and Card Themes

### Goal

Build a cohesive dark-mode design system for a monitoring dashboard with custom `ToggleSwitch`, `Card`, and `Badge` control themes, supporting light/dark variant switching via a user-facing toggle.

### Design Tokens

```xml
<!-- Theme/DesignTokens.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- Dashboard-specific palette -->
  <Color x:Key="DashboardBg">#0F1117</Color>
  <Color x:Key="DashboardSurface">#1A1D27</Color>
  <Color x:Key="DashboardSurfaceAlt">#242837</Color>
  <Color x:Key="DashboardAccent">#00D4FF</Color>
  <Color x:Key="DashboardSuccess">#22C55E</Color>
  <Color x:Key="DashboardWarning">#F59E0B</Color>
  <Color x:Key="DashboardDanger">#EF4444</Color>

  <!-- Spacing (4px grid) -->
  <x:Double x:Key="Space1">4</x:Double>
  <x:Double x:Key="Space2">8</x:Double>
  <x:Double x:Key="Space3">12</x:Double>
  <x:Double x:Key="Space4">16</x:Double>
  <x:Double x:Key="Space5">24</x:Double>
  <x:Double x:Key="Space6">32</x:Double>

  <!-- Typography -->
  <x:Double x:Key="FontSizeXs">11</x:Double>
  <x:Double x:Key="FontSizeSm">13</x:Double>
  <x:Double x:Key="FontSizeBase">14</x:Double>
  <x:Double x:Key="FontSizeLg">18</x:Double>
  <x:Double x:Key="FontSizeXl">24</x:Double>
</ResourceDictionary>
```

### Custom ToggleSwitch Control Theme

```xml
<!-- Theme/Controls/ToggleSwitch.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ControlTheme x:Key="{x:Type ToggleSwitch}" TargetType="ToggleSwitch">
    <Setter Property="Width" Value="44" />
    <Setter Property="Height" Value="24" />
    <Setter Property="CornerRadius" Value="12" />

    <Setter Property="Template">
      <ControlTemplate>
        <Border Name="PART_Border"
                Width="{TemplateBinding Width}"
                Height="{TemplateBinding Height}"
                CornerRadius="{TemplateBinding CornerRadius}"
                Background="{DynamicResource SwitchTrackOffBrush}"
                BorderThickness="0">
          <Ellipse Name="PART_Thumb"
                   Width="20" Height="20"
                   Fill="{DynamicResource SwitchThumbOffBrush}"
                   HorizontalAlignment="Left"
                   Margin="2,0,0,0" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:checked /template/ PART_Border">
      <Setter Property="Background" Value="{DynamicResource SwitchTrackOnBrush}" />
    </Style>
    <Style Selector="^:checked /template/ PART_Thumb">
      <Setter Property="Fill" Value="{DynamicResource SwitchThumbOnBrush}" />
      <Setter Property="Margin" Value="22,0,0,0" />
    </Style>
    <Style Selector="^:pointerover /template/ PART_Border">
      <Setter Property="Opacity" Value="0.85" />
    </Style>
  </ControlTheme>
</ResourceDictionary>
```

### Theme Variants with Dashboard Colors

```xml
<!-- Theme/ThemeVariants.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Dark">
      <!-- Surfaces -->
      <SolidColorBrush x:Key="PageBgBrush" Color="#0F1117" />
      <SolidColorBrush x:Key="SurfaceBrush" Color="#1A1D27" />
      <SolidColorBrush x:Key="SurfaceAltBrush" Color="#242837" />
      <SolidColorBrush x:Key="BorderBrush" Color="#2A2E3A" />
      <!-- Text -->
      <SolidColorBrush x:Key="TextPrimaryBrush" Color="#E8EAED" />
      <SolidColorBrush x:Key="TextSecondaryBrush" Color="#9AA0A6" />
      <!-- Accent -->
      <SolidColorBrush x:Key="AccentBrush" Color="#00D4FF" />
      <SolidColorBrush x:Key="AccentHoverBrush" Color="#33DDFF" />
      <!-- Switch -->
      <SolidColorBrush x:Key="SwitchTrackOffBrush" Color="#3C4043" />
      <SolidColorBrush x:Key="SwitchThumbOffBrush" Color="#9AA0A6" />
      <SolidColorBrush x:Key="SwitchTrackOnBrush" Color="#00D4FF" />
      <SolidColorBrush x:Key="SwitchThumbOnBrush" Color="#FFFFFF" />
    </ResourceDictionary>
    <ResourceDictionary x:Key="Light">
      <SolidColorBrush x:Key="PageBgBrush" Color="#F8F9FA" />
      <SolidColorBrush x:Key="SurfaceBrush" Color="#FFFFFF" />
      <SolidColorBrush x:Key="SurfaceAltBrush" Color="#F1F3F4" />
      <SolidColorBrush x:Key="BorderBrush" Color="#DADCE0" />
      <SolidColorBrush x:Key="TextPrimaryBrush" Color="#202124" />
      <SolidColorBrush x:Key="TextSecondaryBrush" Color="#5F6368" />
      <SolidColorBrush x:Key="AccentBrush" Color="#1A73E8" />
      <SolidColorBrush x:Key="AccentHoverBrush" Color="#1557B0" />
      <SolidColorBrush x:Key="SwitchTrackOffBrush" Color="#DADCE0" />
      <SolidColorBrush x:Key="SwitchThumbOffBrush" Color="#5F6368" />
      <SolidColorBrush x:Key="SwitchTrackOnBrush" Color="#1A73E8" />
      <SolidColorBrush x:Key="SwitchThumbOnBrush" Color="#FFFFFF" />
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

### Dashboard ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace DashboardApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkMode = true;

    [ObservableProperty]
    private string _title = "System Overview";

    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var app = Avalonia.Application.Current;
        if (app != null)
            app.RequestedThemeVariant = IsDarkMode
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
    }
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DashboardApp.ViewModels"
             x:Class="DashboardApp.Views.DashboardView"
             x:DataType="vm:DashboardViewModel">

  <Grid RowDefinitions="Auto,*,Auto"
        Background="{DynamicResource PageBgBrush}">
    <!-- Header with theme toggle -->
    <Border Grid.Row="0" Padding="{StaticResource Space4}"
            Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource BorderBrush}"
            BorderThickness="0,0,0,1">
      <Grid ColumnDefinitions="*,Auto" Spacing="{StaticResource Space3}">
        <TextBlock Text="{Binding Title}"
                   FontSize="{StaticResource FontSizeXl}"
                   Foreground="{DynamicResource TextPrimaryBrush}" />
        <StackPanel Grid.Column="1" Orientation="Horizontal"
                    Spacing="{StaticResource Space2}">
          <TextBlock Text="Dark Mode"
                     VerticalAlignment="Center"
                     Foreground="{DynamicResource TextSecondaryBrush}" />
          <ToggleSwitch IsChecked="{Binding IsDarkMode}"
                        Command="{Binding ToggleThemeCommand}" />
        </StackPanel>
      </Grid>
    </Border>

    <!-- Cards grid -->
    <ScrollViewer Grid.Row="1" Padding="{StaticResource Space4}">
      <Grid ColumnDefinitions="*,*,*" RowDefinitions="Auto,Auto"
            Spacing="{StaticResource Space4}">
        <Border Theme="{StaticResource ElevatedCard}">
          <StackPanel Spacing="{StaticResource Space2}" Margin="{StaticResource Space4}">
            <TextBlock Text="CPU" Foreground="{DynamicResource TextSecondaryBrush}" />
            <TextBlock Text="45%" FontSize="{StaticResource FontSizeXl}"
                       Foreground="{DynamicResource AccentBrush}" />
          </StackPanel>
        </Border>
        <!-- Additional cards omitted for brevity -->
      </Grid>
    </ScrollViewer>
  </Grid>
</UserControl>
```

### How It Works

1. `ToggleSwitch` control theme defines a track (`Border`) and thumb (`Ellipse`). The `:checked` pseudo-class selector moves the thumb horizontally (`Margin="22,0,0,0"`) and changes both track and thumb colors.
2. Theme variant dictionaries define every brush using the same key names. The `ToggleTheme` command toggles `Application.RequestedThemeVariant`, which triggers `DynamicResource` re-resolution for all bound brushes.
3. Card themes on `Border` use `{x:Type Border}` as the implicit key — every `Border` without an explicit `Theme` attribute picks up the card styling. `ElevatedCard` adds a stronger shadow via `BasedOn`.
4. `DynamicResource` on all foreground/background brushes ensures instant visual update on theme switch without per-control code.

### Design Decisions & Edge Cases

- **ToggleSwitch thumb margin animation**: The original theme uses instant position change (`Margin` setter). For production, add a `Transition` on `Margin` property (or use `RenderTransform` with `TranslateTransform` animated) for a smooth sliding effect.
- **Unchecked state ghosting**: Without `:unchecked` pseudo-class, the default state applies. The initial thumb position (`HorizontalAlignment="Left"`) and track color (`SwitchTrackOffBrush`) define the off state.
- **Theme toggle persistence**: `IsDarkMode` is a ViewModel property but theme state typically lives in a settings service. Persist the user's choice in `ISettingsService` and load it at startup.

---

## Scenario 2: Form Design System with Input Variants and Validation States

### Goal

Create a form-oriented design system with `TextBox`, `ComboBox` variants (outlined, filled, underlined), validation state visual feedback (error, warning, success), and consistent label/helper-text composition.

### TextBox Variants

```xml
<!-- Theme/Controls/Forms.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Base outlined TextBox -->
  <ControlTheme x:Key="{x:Type TextBox}" TargetType="TextBox">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource BorderBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="6" />
    <Setter Property="Padding" Value="12,10" />
    <Setter Property="FontSize" Value="14" />
    <Setter Property="Watermark" Value=" " />

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
                         CaretBrush="{TemplateBinding Foreground}"
                         Padding="{TemplateBinding Padding}" />
        </Border>
      </ControlTemplate>
    </Setter>

    <Style Selector="^:focus /template/ PART_Border">
      <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}" />
      <Setter Property="BorderThickness" Value="2" />
    </Style>
    <Style Selector="^:disabled /template/ PART_Border">
      <Setter Property="Opacity" Value="0.5" />
      <Setter Property="Background" Value="{DynamicResource SurfaceAltBrush}" />
    </Style>
  </ControlTheme>

  <!-- Filled variant -->
  <ControlTheme x:Key="FilledTextBox" TargetType="TextBox"
                BasedOn="{StaticResource {x:Type TextBox}}">
    <Setter Property="Background" Value="{DynamicResource SurfaceAltBrush}" />
    <Setter Property="BorderBrush" Value="Transparent" />
    <Setter Property="CornerRadius" Value="6,6,0,0" />
    <Style Selector="^:focus /template/ PART_Border">
      <Setter Property="BorderBrush" Value="{DynamicResource AccentBrush}" />
      <Setter Property="BorderThickness" Value="0,0,0,2" />
      <Setter Property="Background" Value="{DynamicResource SurfaceBrush}" />
    </Style>
  </ControlTheme>

  <!-- Validation state selectors (applied via style classes) -->
  <Style Selector="TextBox.error /template/ PART_Border">
    <Setter Property="BorderBrush" Value="{DynamicResource ErrorBrush}" />
  </Style>
  <Style Selector="TextBox.warning /template/ PART_Border">
    <Setter Property="BorderBrush" Value="{DynamicResource WarningBrush}" />
  </Style>
  <Style Selector="TextBox.success /template/ PART_Border">
    <Setter Property="BorderBrush" Value="{DynamicResource SuccessBrush}" />
  </Style>
</ResourceDictionary>
```

### ViewModel with Validation

```csharp
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FormApp.ViewModels;

public partial class RegistrationFormViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(RegistrationFormViewModel), nameof(ValidateEmail))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _emailState = string.Empty;  // "error" | "warning" | "success" | ""

    [ObservableProperty]
    private string _emailHelperText = string.Empty;

    partial void OnEmailChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            EmailState = "error";
            EmailHelperText = "Email is required";
        }
        else if (!value.Contains('@'))
        {
            EmailState = "warning";
            EmailHelperText = "Missing '@' — email may be invalid";
        }
        else
        {
            EmailState = "success";
            EmailHelperText = "Valid email format";
        }
    }

    public static string ValidateEmail(string email, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email cannot be empty";
        return string.Empty;
    }

    [RelayCommand]
    private void Submit()
    {
        ValidateAllProperties();
        if (!HasErrors)
        {
            // Proceed with registration
        }
    }
}
```

### XAML View with Validation States

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:FormApp.ViewModels"
             x:Class="FormApp.Views.RegistrationFormView"
             x:DataType="vm:RegistrationFormViewModel">

  <StackPanel Spacing="{StaticResource Space4}"
              MaxWidth="400" Margin="{StaticResource Space5}">

    <!-- Email field with validation state -->
    <StackPanel Spacing="{StaticResource Space1}">
      <TextBlock Text="Email"
                 Foreground="{DynamicResource TextPrimaryBrush}"
                 FontSize="{StaticResource FontSizeSm}" />
      <TextBox Text="{Binding Email}"
               Theme="{StaticResource FilledTextBox}"
               Classes="{Binding EmailState}" />
      <TextBlock Text="{Binding EmailHelperText}"
                 Foreground="{DynamicResource TextSecondaryBrush}"
                 FontSize="{StaticResource FontSizeXs}" />
    </StackPanel>

    <!-- Password field -->
    <StackPanel Spacing="{StaticResource Space1}">
      <TextBlock Text="Password"
                 Foreground="{DynamicResource TextPrimaryBrush}"
                 FontSize="{StaticResource FontSizeSm}" />
      <TextBox Text="{Binding Password}"
               PasswordChar="•" />
    </StackPanel>

    <Button Content="Submit"
            Command="{Binding SubmitCommand}"
            HorizontalAlignment="Stretch" />
  </StackPanel>
</UserControl>
```

### How It Works

1. **TextBox variants**: `FilledTextBox` inherits from the base outlined `TextBox` via `BasedOn`. It overrides `Background`, `BorderBrush`, and the `:focus` selector to produce a bottom-border-only focus indicator common in Material-style forms.
2. **Validation state classes**: Style selectors `TextBox.error`, `TextBox.warning`, and `TextBox.success` target the `PART_Border` element inside the template. The ViewModel's `EmailState` property is bound to `Classes` (Avalonia binds comma-separated class strings to `Classes`).
3. **`ObservableValidator`**: The ViewModel extends `ObservableValidator` (CommunityToolkit.Mvvm) for built-in validation. `[NotifyDataErrorInfo]` on properties auto-generates validation error propagation.
4. **Helper text**: A `TextBlock` below each input shows contextual guidance. In an error state, use a converter to make this text `{DynamicResource ErrorBrush}` color instead of secondary.

### Design Decisions & Edge Cases

- **Classes binding**: Avalonia's `Classes` property accepts a space-separated string. `EmailState` values like `"error"` or `"warning"` are directly usable. For multiple classes, concatenate with spaces: `"error focused"`.
- **Empty state default**: When `EmailState` is `""`, no validation class selector matches, and the default `BorderBrush` from the theme applies (no visible validation indicator). This is the desired idle state.
- **`ObservableValidator` vs. manual validation**: `ObservableValidator` provides `ValidateAllProperties()` and `HasErrors` — no need to track error state per-property manually. The class-based visual state approach complements this by giving UI feedback without cluttering the ViewModel with `IsValidEmail` booleans.
- **Disabled state**: All input themes include `:disabled` handling. Controls inside a read-only form use `IsEnabled="False"` and get reduced opacity automatically.

### Comparison

| Aspect | Scenario 1: Dashboard | Scenario 2: Form System |
|---|---|---|
| Primary controls themed | ToggleSwitch, Border/Card | TextBox, ComboBox |
| Theme variants | Dark + Light via ThemeDictionaries | Dark + Light via ThemeDictionaries |
| State handling | `:checked` pseudo-class toggle | CSS-like validation classes on inputs |
| Component variants | ElevatedCard (BasedOn Border) | FilledTextBox (BasedOn TextBox) |
| ViewModel base | ObservableObject | ObservableValidator |
| Design focus | Data visualization, monitor | Data entry, validation feedback |
| Theme switching | User toggle (ToggleSwitch → theme variant) | Static (form always uses system theme) |

## See Also

- [031 — Building a Complete Custom Theme and Design System](031-custom-theme-design-system.md)
- [031V — Building a Complete Custom Theme (verbose companion)](031-custom-theme-design-system-verbose.md)
- [012 — Control Themes vs Styles](../intermediate/012-control-themes-vs-styles.md)
- [006 — Resources](../basics/006-resources.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
- [Avalonia Docs: Resources](https://docs.avaloniaui.net/docs/styling/resources)
