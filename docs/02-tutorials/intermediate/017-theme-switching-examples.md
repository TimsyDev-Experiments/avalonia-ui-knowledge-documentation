---
tier: intermediate
topic: theming
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 017-theme-switching.md
---

# 017E — Theme Switching: Real-World Examples

**What this is:** Two worked examples showing runtime theme management beyond basic light/dark toggling. Read [017 — Theme Switching](017-theme-switching.md) and [017V — Verbose Companion](017-theme-switching-verbose.md) first.

---

## Example 1: Custom Theme Variants (Sepia, High Contrast, Dark Blue)

### Goal

Offer four theme options — Light, Dark, Sepia, and High Contrast — each as a custom `ThemeVariant`. The user selects from a dropdown, and the switch is instant across all open windows.

### Theme Resource Dictionaries

```xml
<!-- Assets/Themes/Light.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="WindowBackground" Color="#FAFAFA" />
  <SolidColorBrush x:Key="CardBackground" Color="#FFFFFF" />
  <SolidColorBrush x:Key="TextPrimary" Color="#1A1A2E" />
  <SolidColorBrush x:Key="TextSecondary" Color="#666" />
  <SolidColorBrush x:Key="Accent" Color="#6a33ff" />
  <SolidColorBrush x:Key="Border" Color="#e0e0e0" />
</ResourceDictionary>
```

```xml
<!-- Assets/Themes/Dark.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="WindowBackground" Color="#1A1A2E" />
  <SolidColorBrush x:Key="CardBackground" Color="#24243A" />
  <SolidColorBrush x:Key="TextPrimary" Color="#E0E0F0" />
  <SolidColorBrush x:Key="TextSecondary" Color="#999" />
  <SolidColorBrush x:Key="Accent" Color="#8b6cff" />
  <SolidColorBrush x:Key="Border" Color="#333" />
</ResourceDictionary>
```

```xml
<!-- Assets/Themes/Sepia.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="WindowBackground" Color="#F5E6C8" />
  <SolidColorBrush x:Key="CardBackground" Color="#FAF0DC" />
  <SolidColorBrush x:Key="TextPrimary" Color="#3E2723" />
  <SolidColorBrush x:Key="TextSecondary" Color="#6D4C41" />
  <SolidColorBrush x:Key="Accent" Color="#8D6E63" />
  <SolidColorBrush x:Key="Border" Color="#D7CCC8" />
</ResourceDictionary>
```

```xml
<!-- Assets/Themes/HighContrast.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="WindowBackground" Color="#000000" />
  <SolidColorBrush x:Key="CardBackground" Color="#000000" />
  <SolidColorBrush x:Key="TextPrimary" Color="#FFFFFF" />
  <SolidColorBrush x:Key="TextSecondary" Color="#FFFF00" />
  <SolidColorBrush x:Key="Accent" Color="#00FFFF" />
  <SolidColorBrush x:Key="Border" Color="#FFFFFF" />
</ResourceDictionary>
```

### App.axaml — Merged Dictionaries with Theme Variants

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/Assets/Themes/Light.axaml">
          <ResourceDictionary.ThemeVariant>
            <ThemeVariant>Light</ThemeVariant>
          </ResourceDictionary.ThemeVariant>
        </ResourceDictionary>
        <ResourceDictionary Source="/Assets/Themes/Dark.axaml">
          <ResourceDictionary.ThemeVariant>
            <ThemeVariant>Dark</ThemeVariant>
          </ResourceDictionary.ThemeVariant>
        </ResourceDictionary>
        <ResourceDictionary Source="/Assets/Themes/Sepia.axaml">
          <ResourceDictionary.ThemeVariant>
            <ThemeVariant>Sepia</ThemeVariant>
          </ResourceDictionary.ThemeVariant>
        </ResourceDictionary>
        <ResourceDictionary Source="/Assets/Themes/HighContrast.axaml">
          <ResourceDictionary.ThemeVariant>
            <ThemeVariant>HighContrast</ThemeVariant>
          </ResourceDictionary.ThemeVariant>
        </ResourceDictionary>
      </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
  </Application.Resources>
</Application>
```

### ThemeService with Custom Variants

```csharp
using Avalonia.Styling;

namespace MyApp.Services;

public enum ThemeOption { Light, Dark, Sepia, HighContrast }

public interface IThemeService
{
    ThemeOption Current { get; }
    void SetTheme(ThemeOption option);
    event Action<ThemeOption>? ThemeChanged;
}

public class ThemeService : IThemeService
{
    private static readonly ThemeVariant SepiaVariant = new("Sepia", null);
    private static readonly ThemeVariant HighContrastVariant = new("HighContrast", null);

    public ThemeOption Current { get; private set; } = ThemeOption.Light;
    public event Action<ThemeOption>? ThemeChanged;

    public void SetTheme(ThemeOption option)
    {
        Current = option;

        var app = Application.Current;
        if (app is null) return;

        app.RequestedThemeVariant = option switch
        {
            ThemeOption.Light => ThemeVariant.Light,
            ThemeOption.Dark => ThemeVariant.Dark,
            ThemeOption.Sepia => SepiaVariant,
            ThemeOption.HighContrast => HighContrastVariant,
            _ => ThemeVariant.Default,
        };

        ThemeChanged?.Invoke(option);
    }
}
```

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ThemePickerViewModel : ObservableObject
{
    private readonly IThemeService _theme;

    public ThemePickerViewModel(IThemeService theme)
    {
        _theme = theme;
        _theme.ThemeChanged += v => CurrentTheme = v;
    }

    public Array ThemeOptions => Enum.GetValues<ThemeOption>();

    [ObservableProperty]
    private ThemeOption _currentTheme;

    partial void OnCurrentThemeChanged(ThemeOption value)
    {
        _theme.SetTheme(value);
    }
}
```

### XAML View

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:vm="using:MyApp.ViewModels"
            x:DataType="vm:ThemePickerViewModel"
            Spacing="12" Margin="20">

  <TextBlock Text="Appearance"
             FontSize="18" FontWeight="Bold" />

  <ComboBox ItemsSource="{Binding ThemeOptions}"
            SelectedItem="{Binding CurrentTheme}" />

  <!-- Preview card using theme resources -->
  <Border Background="{DynamicResource CardBackground}"
          BorderBrush="{DynamicResource Border}"
          BorderThickness="1"
          CornerRadius="8"
          Padding="16">
    <StackPanel Spacing="8">
      <TextBlock Text="Preview"
                 Foreground="{DynamicResource TextPrimary}"
                 FontSize="16" FontWeight="SemiBold" />
      <TextBlock Text="This card updates when the theme changes."
                 Foreground="{DynamicResource TextSecondary}" />
      <Border Background="{DynamicResource Accent}"
              CornerRadius="4"
              Padding="8,4">
        <TextBlock Text="Accent Button"
                   Foreground="White"
                   FontSize="12" />
      </Border>
    </StackPanel>
  </Border>
</StackPanel>
```

### How It Works

1. `ThemeVariant` is a class, not an enum. Custom variants are created with `new ThemeVariant("Sepia", null)`. The second parameter is the base variant (`null` means no inheritance).
2. Each `ResourceDictionary` in `App.axaml` is annotated with `<ResourceDictionary.ThemeVariant>`. Avalonia resolves `{DynamicResource WindowBackground}` against the active variant's dictionary.
3. The `ComboBox` binds to `CurrentTheme`. When the user selects a new option, `OnCurrentThemeChanged` calls `ThemeService.SetTheme`, which sets `Application.Current.RequestedThemeVariant`.
4. All `{DynamicResource}` bindings re-evaluate immediately. The preview card in the same view updates without any additional code.

### Design Decisions & Edge Cases

- **Why custom `ThemeVariant` instead of just swapping `ResourceDictionary.MergedDictionaries`:** Using `ThemeVariant` leverages Avalonia's built-in resource resolution. Swapping dictionaries manually would require removing/adding entries and notifying every `DynamicResource` binding.
- **Why `null` base variant for Sepia/HighContrast:** These themes are independent — they define every resource from scratch. If Sepia should inherit Light values for unspecified resources, pass `ThemeVariant.Light` as the base.
- **Edge case — resource missing in one variant:** If `HighContrast.axaml` is missing the `Accent` key, Avalonia falls back to un-themed dictionaries, then to `ThemeVariant.Default`, then to the default value for the property type. Validate all keys are present in every variant.
- **Edge case — variant not reloaded on second selection:** Selecting "Sepia" twice does nothing — the variant is already active. This is correct behavior.

---

## Example 2: Per-Window Theming (Main Window Light, Tools Dark)

### Goal

Apply different themes to different windows in the same application. The main document window uses Light, while tool windows (inspector, log viewer) use Dark — independent of each other.

### ThemeService — Per-Window Override

```csharp
using Avalonia.Styling;

namespace MyApp.Services;

public interface IPerWindowThemeService
{
    void SetWindowTheme(Window window, ThemeVariant variant);
    void ResetToDefault(Window window);
}

public class PerWindowThemeService : IPerWindowThemeService
{
    public void SetWindowTheme(Window window, ThemeVariant variant)
    {
        // Setting on the window overrides the Application-level theme for this window only
        window.RequestedThemeVariant = variant;
    }

    public void ResetToDefault(Window window)
    {
        window.RequestedThemeVariant = ThemeVariant.Default;
    }
}
```

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly IWindowManager _windows;
    private readonly IPerWindowThemeService _windowThemes;

    public ShellViewModel(IWindowManager windows, IPerWindowThemeService windowThemes)
    {
        _windows = windows;
        _windowThemes = windowThemes;
    }

    [RelayCommand]
    private void OpenDarkInspector()
    {
        _windows.Show<InspectorWindow>("inspector", () =>
        {
            var window = new InspectorWindow();
            _windowThemes.SetWindowTheme(window, ThemeVariant.Dark);
            return window;
        });
    }

    [RelayCommand]
    private void OpenDarkLogViewer()
    {
        _windows.Show<LogViewerWindow>("log", () =>
        {
            var window = new LogViewerWindow();
            _windowThemes.SetWindowTheme(window, ThemeVariant.Dark);
            return window;
        });
    }
}
```

### XAML — Tool Window (inherits dark via window RequestedThemeVariant)

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:InspectorViewModel"
        Title="Inspector" Width="320" Height="480">

  <!-- RequestedThemeVariant is set in code-behind to Dark -->
  <Grid RowDefinitions="Auto,*" Margin="12" Spacing="8">
    <TextBox Text="{Binding SearchText, Mode=TwoWay}"
             Watermark="Search properties..." />

    <ListBox Grid.Row="1"
             ItemsSource="{Binding Properties}">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:PropertyEntry">
          <Grid ColumnDefinitions="*,Auto">
            <TextBlock Text="{Binding Name}"
                       Foreground="{DynamicResource TextPrimary}" />
            <TextBlock Grid.Column="1"
                       Text="{Binding Value}"
                       Foreground="{DynamicResource TextSecondary}" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</Window>
```

### How It Works

1. `PerWindowThemeService.SetWindowTheme(window, ThemeVariant.Dark)` sets `window.RequestedThemeVariant = ThemeVariant.Dark`. This overrides the application-level `RequestedThemeVariant` for that specific window only.
2. The main window (not shown) uses `ThemeVariant.Light` at the application level. Tool windows explicitly set `Dark` at the window level.
3. `{DynamicResource}` references in the tool window resolve against the window's `ThemeVariant` (Dark), while the main window's references resolve against Light.
4. The `WindowManager` from [016 — Window & Dialog Management](016-window-dialog-management.md) creates each tool window with the correct theme applied before `Show()`.

### Design Decisions & Edge Cases

- **Why set theme on the window instead of on individual controls:** Setting `RequestedThemeVariant` on the window propagates to all child controls. Setting it individually on each control would be repetitive and error-prone.
- **Why a dedicated `IPerWindowThemeService` instead of a method on `IThemeService`:** Separating per-window from global theme management avoids confusion. The global service sets `Application.Current.RequestedThemeVariant`; the per-window service sets it on individual windows.
- **Edge case — new window created after theme change:** If the global theme switches to Sepia while a Dark tool window is open, the tool window stays Dark (its local override takes priority). New tool windows still get Dark because the factory explicitly sets it.
- **Edge case — window.RequestedThemeVariant reset to Default:** Setting `Default` on a window makes it follow the application-level theme. This is useful if you want a "reset to app theme" button.
- **Trade-off:** Per-window theming means the tool window's theme is fixed at creation time. If the user wants all windows to follow the global setting, omit the per-window override.

---

## Comparison

| Aspect | Example 1 — Custom Variants | Example 2 — Per-Window Themes |
|---|---|---|
| **Scope** | Application-wide | Per-window |
| **Variants** | Light, Dark, Sepia, High Contrast | Light (main), Dark (tools) |
| **Mechanism** | `Application.Current.RequestedThemeVariant` | `Window.RequestedThemeVariant` |
| **Resources** | 4 theme dictionaries with same keys, different values | Single pair of dictionaries, windows resolve against different active variants |
| **User control** | ComboBox selection | Hard-coded per window type |
| **When to use** | Accessibility, user preference, reading modes | MDI apps, design tools, IDEs with dark tool panels |
| **Key risk** | Missing resource in one variant | Window theme out of sync when global theme changes |

---

## See Also

- [017 — Theme Switching (original)](017-theme-switching.md)
- [017V — Theme Switching (verbose companion)](017-theme-switching-verbose.md)
- [006 — Resources (Static & Dynamic)](../basics/006-resources.md)
- [012 — Control Themes vs Styles](012-control-themes-vs-styles.md)
- [016 — Window & Dialog Management](016-window-dialog-management.md)
- [Avalonia Docs: Theme Variants](https://docs.avaloniaui.net/docs/styling/theme-variants)
