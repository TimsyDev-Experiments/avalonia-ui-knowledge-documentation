---
tier: intermediate
topic: theming
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 017 — Theme Switching (Light/Dark/System)

**What you'll learn:** Implement runtime light/dark theme switching, detect system theme preference, and persist the user's choice.

**Prerequisites:** [006 — Resources (Static & Dynamic)](docs/02-tutorials/basics/006-resources.md)

---

## 1. Theme variant resources

Define light and dark resources using theme variant dictionaries:

```xml
<!-- Assets/Themes/Light.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="WindowBackground" Color="#FAFAFA" />
  <SolidColorBrush x:Key="TextPrimary" Color="#1A1A2E" />
  <SolidColorBrush x:Key="Surface" Color="#FFFFFF" />
</ResourceDictionary>
```

```xml
<!-- Assets/Themes/Dark.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <SolidColorBrush x:Key="WindowBackground" Color="#1A1A2E" />
  <SolidColorBrush x:Key="TextPrimary" Color="#E0E0F0" />
  <SolidColorBrush x:Key="Surface" Color="#24243A" />
</ResourceDictionary>
```

---

## 2. Theme manager service

```csharp
// Services/IThemeService.cs
public enum ThemeVariant { Light, Dark, System }

public interface IThemeService
{
    ThemeVariant Current { get; }
    void SetTheme(ThemeVariant variant);
    event Action<ThemeVariant>? ThemeChanged;
}
```

```csharp
// Services/ThemeService.cs
using Avalonia.Styling;

public class ThemeService : IThemeService
{
    public ThemeVariant Current { get; private set; } = ThemeVariant.System;
    public event Action<ThemeVariant>? ThemeChanged;

    public void SetTheme(ThemeVariant variant)
    {
        Current = variant;

        var app = Application.Current;
        if (app is null) return;

        app.RequestedThemeVariant = variant switch
        {
            ThemeVariant.Light => ThemeVariant.Light,
            ThemeVariant.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default  // follows system
        };

        ThemeChanged?.Invoke(variant);
    }
}
```

---

## 3. Theme toggle ViewModel

```csharp
public partial class ThemeViewModel : ObservableObject
{
    private readonly IThemeService _theme;

    public ThemeViewModel(IThemeService theme)
    {
        _theme = theme;

        _theme.ThemeChanged += v =>
        {
            IsDarkMode = v == ThemeVariant.Dark;
            ThemeIcon = v == ThemeVariant.Dark ? "☀️" : "🌙";
        };
    }

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _themeIcon = "🌙";

    [RelayCommand]
    private void ToggleTheme()
    {
        _theme.SetTheme(IsDarkMode ? ThemeVariant.Light : ThemeVariant.Dark);
    }
}
```

---

## 4. UI with dynamic resources

```xml
<Window Background="{DynamicResource WindowBackground}"
        Foreground="{DynamicResource TextPrimary}">
  <Border Background="{DynamicResource Surface}"
          CornerRadius="8"
          Padding="16">
    <Button Command="{Binding ToggleThemeCommand}"
            Content="{Binding ThemeIcon}" />
  </Border>
</Window>
```

Resources must use `{DynamicResource}` — `{StaticResource}` won't update when the theme changes.

---

## 5. Detect system theme at startup

```csharp
// App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
    // Start with the system theme
    // Avalonia's ThemeVariant.Default follows the OS setting
    Application.Current!.RequestedThemeVariant = ThemeVariant.Default;

    base.OnFrameworkInitializationCompleted();
}
```

---

## 6. Persist the user's choice

```csharp
public void SetTheme(ThemeVariant variant)
{
    Current = variant;

    // Save to settings
    var settings = new AppSettings
    {
        Theme = variant.ToString()
    };
    File.WriteAllText("settings.json",
        JsonSerializer.Serialize(settings));
}

public void LoadTheme()
{
    if (File.Exists("settings.json"))
    {
        var settings = JsonSerializer
            .Deserialize<AppSettings>(File.ReadAllText("settings.json"));

        if (Enum.TryParse<ThemeVariant>(settings?.Theme, out var variant))
            SetTheme(variant);
    }
}
```

---

## Key Takeaways

- Use `RequestedThemeVariant` on `Application` for runtime switching
- `{DynamicResource}` for all theme-dependent values
- `ThemeVariant.Default` follows the OS light/dark setting
- Persist the user's choice to settings (JSON, preferences, etc.)
- Organize resources into light/dark dictionary files

---

## See Also

- [006 — Resources](docs/02-tutorials/basics/006-resources.md)
- [012 — Control Themes vs Styles](012-control-themes-vs-styles.md)
- [Avalonia Docs: Theme Variants](https://docs.avaloniaui.net/docs/styling/theme-variants)
