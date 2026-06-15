---
tier: intermediate
topic: theming
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 017-theme-switching.md
---

# 017V — Theme Switching (Light/Dark/System): An In-Depth Companion

**Why this exists:** The original tutorial covers the mechanics of switching between light and dark themes. This companion explains *how `ThemeVariant` works internally*, *what `DynamicResource` does differently from `StaticResource`*, *how Avalonia detects the system theme*, and *how to structure theme resources for maintainability*.

**Cross-reference:** Original tutorial at [017-theme-switching.md](017-theme-switching.md).

---

## 1. ThemeVariant — what it is and how it resolves

```csharp
app.RequestedThemeVariant = ThemeVariant.Light;
```

`ThemeVariant` is a class, not an enum. It has three static instances:

- `ThemeVariant.Light` — forces light mode.
- `ThemeVariant.Dark` — forces dark mode.
- `ThemeVariant.Default` — follows the operating system setting.

**Why it is a class (not an enum):** The `ThemeVariant` class is extensible. You can create custom theme variants by subclassing or by using the constructor directly. This enables more than two themes — for example, "HighContrast", "Sepia", or "Dusk". The `ResourceDictionary` system uses the variant's equality comparison (`Equals`) to select which resources to apply.

**How theme resolution works:**

1. Each `ResourceDictionary` can specify a `ThemeVariant` attribute. If omitted, the dictionary applies to all variants.
2. When `RequestedThemeVariant` changes, Avalonia walks the resource tree and re-resolves every `{DynamicResource}` reference.
3. For a given resource key, Avalonia looks in the following order:
   - Dictionaries scoped to the current `ThemeVariant` on the current element.
   - Dictionaries scoped to the current `ThemeVariant` on parent elements.
   - Dictionaries scoped to the current `ThemeVariant` in `Application.Resources`.
   - Repeat for `ThemeVariant.Default` (fallback if no variant-specific resource found).
   - Repeat for the base (non-themed) dictionaries.

**What `ThemeVariant.Default` actually does:** It queries `PlatformThemeVariant` from `AvaloniaNative` / `Win32` / `X11` platform backend. The platform backend reads the OS preference: Windows reads the registry key for "Apps use light/dark mode", macOS reads `NSApplication.effectiveAppearance`, Linux reads the GTK or Freedesktop setting. The returned value is either `ThemeVariant.Light` or `ThemeVariant.Dark`.

---

## 2. Theme resource dictionaries — why separate files

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

**Why separate files:** Without separate files, you would have to put both light and dark values in the same dictionary with different keys (e.g., `LightWindowBackground`, `DarkWindowBackground`). That forces every binding to choose a key — you cannot simply `{DynamicResource WindowBackground}`.

**How they are loaded:**

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="/Assets/Themes/Light.axaml" />
      <ResourceDictionary Source="/Assets/Themes/Dark.axaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

**How the variant is assigned to each file:**

```xml
<!-- In App.axaml, when merging -->
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
```

When `RequestedThemeVariant` is `ThemeVariant.Light`, only the Light dictionary's resources are active for `WindowBackground`, `TextPrimary`, etc. When it changes to `Dark`, the Dark dictionary's resources take over. Resources with the same key from the non-active variant are ignored.

**What happens if a key is missing in the active variant:** Avalonia falls back to the `ThemeVariant.Default` dictionaries, then to un-themed dictionaries. If the key is still not found, the resource is "unresolved" — the binding gets `DynamicResource`'s fallback value (usually `null` or the default for the property type).

---

## 3. DynamicResource vs StaticResource — the difference

```xml
<Window Background="{DynamicResource WindowBackground}"
        Foreground="{DynamicResource TextPrimary}">
```

**StaticResource:** Resolved once at load time. The resource dictionary is searched, the value is applied, and the binding is removed. If the resource changes later (e.g., theme switches), the control does not update.

**DynamicResource:** Resolved at load time AND re-resolved when the resource system signals a change. When `RequestedThemeVariant` changes, Avalonia fires a notification through the resource system. Every `DynamicResource` binding re-evaluates: it looks up the key again in the now-active theme dictionary.

**Performance:** `DynamicResource` is more expensive than `StaticResource` because it must maintain a binding subscription and re-evaluate on every theme change. For properties that do not change with theme (e.g., `FontFamily`, `CornerRadius` when it is the same for both themes), use `StaticResource`.

**When DynamicResource does not update:**

- The resource is defined in a `ResourceDictionary` that was not merged with a `ThemeVariant` annotation. If both light and dark files are merged without a `ThemeVariant`, both are always active — the last-loaded dictionary wins at startup and never changes.
- The resource is an immutable type (e.g., `double`, `Color`) and the `SolidColorBrush` or `Color` was resolved to a concrete value that does not reference the dictionary. This happens when you use `StaticResource` or inline the color instead of a key.
- The property does not support `DynamicResource`. Most `AvaloniaProperty` bound properties do; custom properties may not unless they are `StyledProperty` (not `DirectProperty`).

---

## 4. Theme service — the IThemeService pattern

```csharp
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
            _ => ThemeVariant.Default
        };

        ThemeChanged?.Invoke(variant);
    }
}
```

**Why a service instead of setting `RequestedThemeVariant` directly:** A service provides:

- **Persistence** — it can save and load the user's choice.
- **Notification** — the `ThemeChanged` event lets other parts of the app react (e.g., update the toggle button icon).
- **Testability** — the ViewModel depends on an interface, not on `Application.Current` (which is null in unit tests).

**The `Application.Current` null check:** In unit tests, there is no `Application` instance. The service must not throw. The null check ensures `SetTheme` works in tests (it updates the `Current` property and fires the event, just skips the actual theme change).

---

## 5. The toggle ViewModel

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

**What happens when the user clicks the toggle button:**

1. `ToggleThemeCommand` fires.
2. `ToggleTheme` checks `IsDarkMode`: if `false`, it sets `ThemeVariant.Dark`; if `true`, `ThemeVariant.Light`.
3. `ThemeService.SetTheme` sets `Application.Current.RequestedThemeVariant`.
4. The `DynamicResource` bindings in every open window re-evaluate: `WindowBackground`, `TextPrimary`, `Surface` all update.
5. The `ThemeChanged` event fires. `ThemeViewModel`'s handler updates `IsDarkMode` and `ThemeIcon`.
6. The button's `Content` binding re-evaluates, showing the new icon.

**Edge case — initial state:** When the app starts with `ThemeVariant.Default` (system), `IsDarkMode` must reflect the actual OS theme. The `ThemeViewModel` should query the current variant:

```csharp
var currentVariant = Application.Current?.ActualThemeVariant;
IsDarkMode = currentVariant == ThemeVariant.Dark;
```

`ActualThemeVariant` returns the resolved variant (Light or Dark) even when `RequestedThemeVariant` is `Default`.

---

## 6. System theme detection

```csharp
public override void OnFrameworkInitializationCompleted()
{
    Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
    base.OnFrameworkInitializationCompleted();
}
```

**What `ThemeVariant.Default` does at startup:**

1. The platform backend queries the OS for the current theme preference.
2. On Windows 10/11: reads `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` (0 = dark, 1 = light).
3. On macOS: reads `NSApplication.effectiveAppearance` (Aqua = light, DarkAqua = dark).
4. On Linux: reads the GTK theme preference or the `XDG_CURRENT_DESKTOP` environment variable.
5. The result is mapped to `ThemeVariant.Light` or `ThemeVariant.Dark`.

**Runtime system theme changes:** If the user changes the OS theme while the app is running, Avalonia detects this and fires a theme change notification. If `RequestedThemeVariant` is `ThemeVariant.Default`, the app follows the OS change automatically. If a specific variant (Light/Dark) is set, the app ignores OS changes.

**How to listen for OS theme changes:**

```csharp
public override void OnFrameworkInitializationCompleted()
{
    Application.Current!.RequestedThemeVariant = ThemeVariant.Default;

    // Listen for OS-level changes (only when using Default)
    Application.Current!.ActualThemeVariantChanged += (_, _) =>
    {
        var newTheme = Application.Current!.ActualThemeVariant;
        ThemeChanged?.Invoke(newTheme == ThemeVariant.Dark ? ThemeVariant.Dark : ThemeVariant.Light);
    };

    base.OnFrameworkInitializationCompleted();
}
```

---

## 7. Persistence — save and load

```csharp
public void SetTheme(ThemeVariant variant)
{
    Current = variant;
    var settings = new AppSettings { Theme = variant.ToString() };
    File.WriteAllText("settings.json", JsonSerializer.Serialize(settings));
}
```

**Where to persist:** For a desktop app, options include:

- JSON file in `AppContext.BaseDirectory` or `Environment.SpecialFolder.LocalApplicationData`.
- `Microsoft.Extensions.Configuration` with a JSON provider.
- Windows Registry (Windows-only).

**When to load:** In the `ThemeService` constructor or in a `LoadTheme` method called during app startup, before the first window is shown. If you load after the window is created, there is a flash of the default theme before the persisted theme applies.

---

## 8. Beyond Light/Dark — multiple themes

The same `ThemeVariant` pattern extends to more than two themes:

```xml
<ResourceDictionary ThemeVariant="Sepia">
  <SolidColorBrush x:Key="WindowBackground" Color="#F5E6C8" />
</ResourceDictionary>
```

```csharp
// Custom variant
var sepia = new ThemeVariant("Sepia", null); // null base = no inheritance
Application.Current.RequestedThemeVariant = sepia;
```

Custom variants do not inherit from Light or Dark. They are independent. If you want "Dark with blue accent" to inherit Dark's base values and override only accent colors, pass the base variant:

```csharp
var darkBlue = new ThemeVariant("DarkBlue", ThemeVariant.Dark);
```

---

## Key Takeaways

- `ThemeVariant` is a class with three static instances: `Light`, `Dark`, and `Default` (OS-following).
- Separate `ResourceDictionary` files per variant, annotated with `<ResourceDictionary.ThemeVariant>`.
- Use `{DynamicResource}` for all theme-dependent values — `{StaticResource}` will not update at runtime.
- Wrap theme switching in an `IThemeService` interface for testability and persistence.
- `ActualThemeVariant` gives the resolved light/dark value even when `Default` is set.
- `ActualThemeVariantChanged` fires on OS theme changes when using `Default`.
- Validate restored window state (position/size) against `Screens.All` to handle disconnected monitors.

---

## See Also

- [017 — Theme Switching (original)](017-theme-switching.md)
- [006 — Resources (Static & Dynamic)](../basics/006-resources.md)
- [012 — Control Themes vs Styles](012-control-themes-vs-styles.md)
- [016 — Window & Dialog Management](016-window-dialog-management.md) (window state persistence complements theme persistence)
- [017E — Theme Switching (examples)](017-theme-switching-examples.md)
- [Avalonia Docs: Theme Variants](https://docs.avaloniaui.net/docs/styling/theme-variants)
