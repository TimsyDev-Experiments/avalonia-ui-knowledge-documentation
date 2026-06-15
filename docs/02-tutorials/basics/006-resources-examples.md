---
tier: basics
topic: resources
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 006-resources.md
---

# 006X — Resources: Real-World Examples

**What you'll build:** A theme-switchable dashboard with light/dark mode and a multi-file design-token system organized by concern — two scenarios that demonstrate `StaticResource` vs `DynamicResource` choices, merged dictionaries, and resource shadowing.

**Prerequisites:** [006 — Resources](006-resources.md). The [verbose companion](006-resources-verbose.md) covers the `IResourceDictionary` interface, lookup algorithm steps, and `DynamicResource` event subscription mechanics.

---

## Example 1: Theme-Switchable Dashboard

**Goal:** Build a dashboard that switches between light and dark themes at runtime, with user-toggleable accent colors. Resources are defined in theme dictionaries and referenced with `DynamicResource` so they update when the theme changes.

The dashboard has a header, a card grid, and a status bar — each using themed resources for backgrounds, text colors, and borders.

### Theme resource files

```xml
<!-- Assets/Themes/Light.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Color x:Key="PageBg">#ffffff</Color>
  <Color x:Key="CardBg">#f5f5f5</Color>
  <Color x:Key="HeaderBg">#e8e8e8</Color>
  <Color x:Key="TextPrimary">#1a1a1a</Color>
  <Color x:Key="TextSecondary">#6b7280</Color>
  <Color x:Key="BorderColor">#d1d5db</Color>
  <SolidColorBrush x:Key="PageBackgroundBrush" Color="{StaticResource PageBg}" />
  <SolidColorBrush x:Key="CardBackgroundBrush" Color="{StaticResource CardBg}" />
  <SolidColorBrush x:Key="HeaderBackgroundBrush" Color="{StaticResource HeaderBg}" />
  <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}" />
  <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}" />
  <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}" />
</ResourceDictionary>
```

```xml
<!-- Assets/Themes/Dark.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Color x:Key="PageBg">#1a1a2e</Color>
  <Color x:Key="CardBg">#16213e</Color>
  <Color x:Key="HeaderBg">#0f3460</Color>
  <Color x:Key="TextPrimary">#e0e0e0</Color>
  <Color x:Key="TextSecondary">#9ca3af</Color>
  <Color x:Key="BorderColor">#374151</Color>
  <SolidColorBrush x:Key="PageBackgroundBrush" Color="{StaticResource PageBg}" />
  <SolidColorBrush x:Key="CardBackgroundBrush" Color="{StaticResource CardBg}" />
  <SolidColorBrush x:Key="HeaderBackgroundBrush" Color="{StaticResource HeaderBg}" />
  <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}" />
  <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}" />
  <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}" />
</ResourceDictionary>
```

### App.axaml — swapping theme dictionaries at runtime

```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.App">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>

  <Application.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary x:Key="ThemeDictionary" />
      </ResourceDictionary.MergedDictionaries>

      <!-- App-level constants (StaticResource — never change) -->
      <x:Double x:Key="CardCornerRadius">8</x:Double>
      <x:Double x:Key="StandardPadding">16</x:Double>
      <Thickness x:Key="CardMargin">8</Thickness>
    </ResourceDictionary>
  </Application.Resources>
</Application>
```

### App.axaml.cs — theme loading and switching

```csharp
// App.axaml.cs
using Avalonia;
using Avalonia.Controls;

namespace MyApp;

public partial class App : Application
{
    private bool _isDarkTheme;

    public override void OnFrameworkInitializationCompleted()
    {
        LoadTheme(isDark: false);
        base.OnFrameworkInitializationCompleted();
    }

    public void ToggleTheme()
    {
        _isDarkTheme = !_isDarkTheme;
        LoadTheme(_isDarkTheme);
    }

    private void LoadTheme(bool isDark)
    {
        var themeName = isDark ? "Dark" : "Light";
        var themeDict = new ResourceDictionary();
        themeDict.MergedDictionaries.Add(
            (ResourceDictionary)Avalonia.AvaloniaLocator.Current.GetService(
                // In production, load from assembly resources
                // For this example, assume files are embedded
            ) ?? new ResourceDictionary());

        // Find the theme dictionary in Application.Resources
        if (Resources.MergedDictionaries.FirstOrDefault() is ResourceDictionary rootDict)
        {
            // Replace the placeholder dictionary with the theme dictionary
            rootDict.MergedDictionaries.Clear();
            rootDict.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri($"/Assets/Themes/{themeName}.axaml", UriKind.Relative)
                });
        }
    }
}
```

### ViewModel

```csharp
// ViewModels/DashboardViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _currentTheme = "Light";

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<DashboardCard> Cards { get; } = new()
    {
        new() { Title = "Revenue", Value = "$12,430", Change = "+12%" },
        new() { Title = "Users", Value = "1,892", Change = "+8%" },
        new() { Title = "Orders", Value = "342", Change = "-3%" },
        new() { Title = "Conversion", Value = "3.2%", Change = "+0.4%" },
    };

    [RelayCommand]
    private void ToggleTheme()
    {
        if (App.Current is App app)
        {
            app.ToggleTheme();
            CurrentTheme = CurrentTheme == "Light" ? "Dark" : "Light";
        }
    }
}

public class DashboardCard
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Change { get; set; } = string.Empty;
}
```

### View — using DynamicResource for theme-aware properties

```xml
<!-- Views/DashboardView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.ViewModels"
             x:Class="MyApp.Views.DashboardView"
             x:DataType="vm:DashboardViewModel">

  <UserControl.Resources>
    <!-- StaticResource for constant values — no need to re-evaluate -->
    <x:Double x:Key="CardWidth">200</x:Double>
  </UserControl.Resources>

  <DockPanel Background="{DynamicResource PageBackgroundBrush}"
             Margin="{StaticResource StandardPadding}">
    <!-- Header -->
    <Border DockPanel.Dock="Top"
            Background="{DynamicResource HeaderBackgroundBrush}"
            CornerRadius="{StaticResource CardCornerRadius}"
            Padding="{StaticResource StandardPadding}"
            Margin="0,0,0,16">
      <Grid ColumnDefinitions="*,Auto">
        <StackPanel>
          <TextBlock Text="Dashboard"
                     Foreground="{DynamicResource TextPrimaryBrush}"
                     FontSize="22" FontWeight="Bold" />
          <TextBlock Text="{Binding StatusText, Mode=OneWay}"
                     Foreground="{DynamicResource TextSecondaryBrush}"
                     FontSize="12" />
        </StackPanel>
        <Button Grid.Column="1"
                Content="{Binding CurrentTheme, StringFormat='Switch to {0} theme'}"
                Command="{Binding ToggleThemeCommand}" />
      </Grid>
    </Border>

    <!-- Card grid -->
    <ScrollViewer>
      <WrapPanel>
        <ItemsControl ItemsSource="{Binding Cards}">
          <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
              <WrapPanel />
            </ItemsPanelTemplate>
          </ItemsControl.ItemsPanel>

          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="models:DashboardCard">
              <Border Background="{DynamicResource CardBackgroundBrush}"
                      BorderBrush="{DynamicResource BorderBrush}"
                      BorderThickness="1"
                      CornerRadius="{StaticResource CardCornerRadius}"
                      Margin="{StaticResource CardMargin}"
                      Padding="{StaticResource StandardPadding}"
                      Width="{StaticResource CardWidth}">
                <StackPanel Gap="4">
                  <TextBlock Text="{Binding Title}"
                             Foreground="{DynamicResource TextSecondaryBrush}"
                             FontSize="11" />
                  <TextBlock Text="{Binding Value}"
                             Foreground="{DynamicResource TextPrimaryBrush}"
                             FontSize="24" FontWeight="Bold" />
                  <TextBlock Text="{Binding Change}"
                             FontSize="13"
                             Foreground="{DynamicResource TextSecondaryBrush}" />
                </StackPanel>
              </Border>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </WrapPanel>
    </ScrollViewer>
  </DockPanel>
</UserControl>
```

### How it works

1. Light and dark themes are defined in separate `.axaml` files with matching resource keys. Each file defines the same set of keys (`PageBackgroundBrush`, `TextPrimaryBrush`, etc.) with different color values.
2. At startup, `App.axaml.cs` loads the Light theme by setting the `Source` of a `ResourceDictionary` inside `Application.Resources.MergedDictionaries`. The placeholder dictionary is replaced at runtime.
3. When the user toggles the theme, `ToggleTheme()` in `App.axaml.cs` swaps the dictionary source. Because the view references resources with `{DynamicResource ...}` (not `{StaticResource ...}`), all bound properties automatically re-evaluate and pick up the new colors.
4. Constants like `CardCornerRadius`, `StandardPadding`, and `CardMargin` use `{StaticResource ...}` — they never change and should not incur the `DynamicResource` overhead.
5. The `CardWidth` is defined in `UserControl.Resources` with `StaticResource` — it scopes the resource to this view, shadowing any broader definition.

### Design decisions and edge cases

- **`DynamicResource` for theme brushes:** Every themed element uses `{DynamicResource ...}`. If this were `{StaticResource ...}`, the theme switch would have no visible effect — the old brush references would persist.
- **`StaticResource` for layout constants:** Padding, margin, and corner radius values rarely change with theme. Using `StaticResource` avoids the per-element `DynamicResource` binding cost. If a future dark theme needs different padding, change to `DynamicResource` only for those values.
- **Shadowing in `UserControl.Resources`:** The `CardWidth` resource at `UserControl.Resources` shadows any `CardWidth` defined at `Application.Resources`. This isolates the card layout to this view.
- **Theme switch performance:** Swapping the theme dictionary triggers `DynamicResource` re-evaluation for every reference. For complex pages with hundreds of `DynamicResource` bindings, this produces a visible flash. Mitigate by deferring the swap with a brief animation or by pre-loading both themes and toggling visibility.
- **Missing resource fallback:** If a theme dictionary is missing a key (e.g., `Dark.axaml` omits `PageBackgroundBrush`), the `DynamicResource` falls back to the next dictionary in the lookup chain (application resources, then theme default). The page renders with a mismatched background. Validate theme dictionaries at startup.

---

## Example 2: Multi-File Design Token System

**Goal:** Organize design tokens (colors, typography, spacing, shadows) into separate resource files by concern, then compose them in the application. This mimics a real design system where tokens are maintained by different teams.

Files are organized by token category, with a top-level `Tokens.xaml` that merges all categories. Components reference tokens by semantic names like `SurfaceDefault`, `TextHeading`, and `SpacingMedium`.

### Token file structure

```
Assets/
  Tokens/
    Colors.axaml          # raw color palette
    SemanticColors.axaml  # semantic aliases: SurfaceDefault, TextHeading
    Typography.axaml      # font sizes, weights, families
    Spacing.axaml         # spacing scale: xs, sm, md, lg, xl
    Shadows.axaml         # box shadow presets
    Tokens.axaml          # merges all of the above
```

### Individual token files

```xml
<!-- Assets/Tokens/Colors.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- Raw palette — never reference directly in views -->
  <Color x:Key="ColorNeutralWhite">#ffffff</Color>
  <Color x:Key="ColorNeutral50">#f9fafb</Color>
  <Color x:Key="ColorNeutral100">#f3f4f6</Color>
  <Color x:Key="ColorNeutral900">#111827</Color>
  <Color x:Key="ColorPrimary500">#6a33ff</Color>
  <Color x:Key="ColorPrimary700">#4a1ac0</Color>
  <Color x:Key="ColorError500">#dc2626</Color>
  <Color x:Key="ColorSuccess500">#16a34a</Color>
</ResourceDictionary>
```

```xml
<!-- Assets/Tokens/SemanticColors.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- Semantic aliases — bind views to these, not to raw colors -->
  <SolidColorBrush x:Key="SurfaceDefault" Color="{StaticResource ColorNeutralWhite}" />
  <SolidColorBrush x:Key="SurfaceRaised" Color="{StaticResource ColorNeutral50}" />
  <SolidColorBrush x:Key="TextHeading" Color="{StaticResource ColorNeutral900}" />
  <SolidColorBrush x:Key="TextBody" Color="{StaticResource ColorNeutral900}" />
  <SolidColorBrush x:Key="TextMuted" Color="{StaticResource ColorNeutral100}" />
  <SolidColorBrush x:Key="BrandAccent" Color="{StaticResource ColorPrimary500}" />
  <SolidColorBrush x:Key="BrandAccentHover" Color="{StaticResource ColorPrimary700}" />
  <SolidColorBrush x:Key="StatusError" Color="{StaticResource ColorError500}" />
  <SolidColorBrush x:Key="StatusSuccess" Color="{StaticResource ColorSuccess500}" />
</ResourceDictionary>
```

```xml
<!-- Assets/Tokens/Typography.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <x:Double x:Key="FontSizeXs">10</x:Double>
  <x:Double x:Key="FontSizeSm">12</x:Double>
  <x:Double x:Key="FontSizeMd">14</x:Double>
  <x:Double x:Key="FontSizeLg">18</x:Double>
  <x:Double x:Key="FontSizeXl">24</x:Double>
  <FontWeight x:Key="FontWeightRegular">400</FontWeight>
  <FontWeight x:Key="FontWeightSemiBold">600</FontWeight>
  <FontWeight x:Key="FontWeightBold">700</FontWeight>
</ResourceDictionary>
```

```xml
<!-- Assets/Tokens/Spacing.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <x:Double x:Key="SpacingXs">4</x:Double>
  <x:Double x:Key="SpacingSm">8</x:Double>
  <x:Double x:Key="SpacingMd">16</x:Double>
  <x:Double x:Key="SpacingLg">24</x:Double>
  <x:Double x:Key="SpacingXl">32</x:Double>
  <Thickness x:Key="PagePadding">24</Thickness>
</ResourceDictionary>
```

```xml
<!-- Assets/Tokens/Tokens.axaml — aggregator -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <ResourceDictionary.MergedDictionaries>
    <ResourceInclude Source="/Assets/Tokens/Colors.axaml" />
    <ResourceInclude Source="/Assets/Tokens/SemanticColors.axaml" />
    <ResourceInclude Source="/Assets/Tokens/Typography.axaml" />
    <ResourceInclude Source="/Assets/Tokens/Spacing.axaml" />
    <ResourceInclude Source="/Assets/Tokens/Shadows.axaml" />
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

### App.axaml — load the token system

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceInclude Source="/Assets/Tokens/Tokens.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

### ViewModel

```csharp
// ViewModels/ProfileViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    private string _userName = "Jane Doe";

    [ObservableProperty]
    private string _email = "jane@example.com";

    [ObservableProperty]
    private string _status = "Active";
}
```

### View — referencing semantic tokens

```xml
<!-- Views/ProfileView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.ProfileView"
             x:DataType="vm:ProfileViewModel">
  <Border Background="{StaticResource SurfaceDefault}"
          CornerRadius="{StaticResource SpacingSm}"
          Padding="{StaticResource PagePadding}"
          BorderBrush="{StaticResource SurfaceRaised}"
          BorderThickness="1"
          Margin="{StaticResource SpacingMd}">
    <StackPanel Gap="{StaticResource SpacingMd}">
      <TextBlock Text="{Binding UserName}"
                 Foreground="{StaticResource TextHeading}"
                 FontSize="{StaticResource FontSizeXl}"
                 FontWeight="{StaticResource FontWeightBold}" />
      <TextBlock Text="{Binding Email}"
                 Foreground="{StaticResource TextBody}"
                 FontSize="{StaticResource FontSizeMd}" />
      <TextBlock Text="{Binding Status, StringFormat='Status: {0}'}"
                 Foreground="{StaticResource StatusSuccess}"
                 FontSize="{StaticResource FontSizeSm}"
                 FontWeight="{StaticResource FontWeightSemiBold}" />
      <Border Background="{StaticResource BrandAccent}"
              CornerRadius="{StaticResource SpacingXs}"
              Padding="{StaticResource SpacingSm}">
        <TextBlock Text="Upgrade to Pro"
                   Foreground="{StaticResource SurfaceDefault}"
                   FontSize="{StaticResource FontSizeMd}" />
      </Border>
    </StackPanel>
  </Border>
</UserControl>
```

### How it works

1. Each token category is a separate `ResourceDictionary` file. The palette (`Colors.axaml`) defines raw color values. The semantic layer (`SemanticColors.axaml`) references palette colors with `{StaticResource}` and exposes meaningful names like `TextHeading` and `BrandAccent`.
2. `Tokens.axaml` merges all category files via `ResourceInclude`. The order matters: `Colors.axaml` must come before `SemanticColors.axaml` because the latter references keys defined in the former.
3. `App.axaml` loads only `Tokens.axaml` — it does not know about individual category files. This encapsulation means the view layer never imports token files directly.
4. The view references only semantic token names (`TextHeading`, `SurfaceDefault`, `SpacingMd`). If the raw color palette changes (e.g., `ColorPrimary500` changes from `#6a33ff` to `#5b21b6`), only `Colors.axaml` changes. The views are unaffected.
5. Tokens use `{StaticResource}` because the design system is compiled at app startup and never changes at runtime. If the app supports theme switching, change the semantic brush references to `{DynamicResource}`.

### Design decisions and edge cases

- **`StaticResource` for tokens:** The design token system is loaded once. `StaticResource` is correct for all token references — there is no runtime theme switching in this example. If theme switching is needed, the semantic colors layer becomes a theme dictionary and references change to `DynamicResource`.
- **Raw palette isolation:** Views must never reference `ColorNeutral900` or `ColorPrimary500` directly. The semantic layer is the public API. Enforce this convention through code review — the XAML compiler does not prevent direct palette access.
- **File loading order:** `Colors.axaml` is first in `Tokens.axaml` because `SemanticColors.axaml` depends on it. If the order is wrong, the `{StaticResource ColorPrimary500}` reference in `SemanticColors.axaml` fails at runtime with a `KeyNotFoundException`.
- **Scale consistency:** The spacing scale (`xs`=4, `sm`=8, `md`=16, `lg`=24, `xl`=32) follows a geometric progression (factor ~1.5–2). This is a common design-system pattern (similar to Material Design). Enforce the scale by never using raw numeric values in views.
- **Overriding tokens in specific views:** If a particular view needs a different `TextHeading` color, define a new resource with the same key in the view's `UserControl.Resources`. The local definition shadows the application-level token without modifying the source file.

---

## What These Examples Demonstrate

| Scenario | Resource technique | What to learn |
|---|---|---|
| Theme-switchable dashboard | `DynamicResource` + runtime dictionary swap | `DynamicResource` for theme-aware properties, `StaticResource` for constants, runtime resource replacement |
| Design token system | Merged dictionaries with `ResourceInclude` | Token organization by concern, semantic aliasing, palette isolation, load order |

The dashboard example focuses on *runtime resource switching* — changing resource values without rebuilding the UI. The token system example focuses on *organization and maintainability* — keeping resources modular, ordered, and semantically named so large teams can work on different token categories independently.

## See Also

- [006 — Resources](006-resources.md)
- [006V — Verbose Companion](006-resources-verbose.md)
- [003 — Basic Styling](003-basic-styling.md)
- [003V — Basic Styling (verbose companion)](003-basic-styling-verbose.md)
- [017 — Theme Switching](../intermediate/017-theme-switching.md)
- [Avalonia Docs: Resources](https://docs.avaloniaui.net/docs/styling/resources)
