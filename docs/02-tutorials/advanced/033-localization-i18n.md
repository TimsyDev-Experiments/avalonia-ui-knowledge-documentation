---
tier: advanced
topic: localization
estimated: 30 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 033 -- Localization and Internationalization

**What you'll learn:** How to localize an Avalonia application using ResX files, support runtime language switching, and handle right-to-left layouts.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md)

---

## 1. Create ResX resource files

Add a folder `Lang/` to your project. Create these files:

- `Lang/Resources.resx` (default -- English)
- `Lang/Resources.fr.resx` (French)
- `Lang/Resources.ja.resx` (Japanese)

Each file contains key-value pairs. For `Resources.resx`:

| Name | Value |
|------|-------|
| Greeting | Hello |
| WelcomeMessage | Welcome to {0} |

For `Resources.fr.resx`:

| Name | Value |
|------|-------|
| Greeting | Bonjour |
| WelcomeMessage | Bienvenue sur {0} |

## 2. Configure PublicResXFileCodeGenerator

In your `.csproj`, ensure the generator produces public types:

```xml
<ItemGroup>
  <EmbeddedResource Update="Lang\Resources.resx">
    <Generator>PublicResXFileCodeGenerator</Generator>
    <LastGenOutput>Resources.Designer.cs</LastGenOutput>
  </EmbeddedResource>
</ItemGroup>
```

Only the default `.resx` file should generate code. Satellite assemblies are produced for locale-specific `.resx` files automatically.

## 3. Set the culture in App.axaml.cs

```csharp
using System.Globalization;
using DemoApp.Lang;

public override void OnFrameworkInitializationCompleted()
{
    Resources.Culture = new CultureInfo("fr");
    // ...
}
```

## 4. Use localized text in XAML

```xml
<TextBlock Text="{x:Static lang:Resources.Greeting}" />
<TextBlock Text="{x:Static lang:Resources.WelcomeMessage}" />
```

`x:Static` resolves the static property once at load time.

## 5. Runtime language switching with INotifyPropertyChanged

For dynamic switching, create a wrapper service:

```csharp
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DemoApp.Lang;

namespace DemoApp.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public string this[string key] => Resources.ResourceManager.GetString(key)
        ?? key;

    public string Greeting => Resources.Greeting;
    public string WelcomeMessage => Resources.WelcomeMessage;

    public void SetCulture(string cultureCode)
    {
        Resources.Culture = new CultureInfo(cultureCode);
        PropertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(string.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

Register as a singleton in DI:

```csharp
services.AddSingleton<LocalizationService>();
```

Bind in XAML:

```xml
<TextBlock Text="{Binding Greeting, Source={x:Static services:LocalizationService.Instance}}" />
```

Or use the DI-aware approach:

```xml
<TextBlock Text="{Binding Localization.Greeting}" />
```

## 6. Right-to-left support

```xml
<Window FlowDirection="RightToLeft">
  <!-- All child controls mirror layout -->
</Window>
```

```csharp
var culture = new CultureInfo("ar-SA");
if (culture.TextInfo.IsRightToLeft)
{
    mainWindow.FlowDirection = FlowDirection.RightToLeft;
}
```

Controls that respect `FlowDirection`: `StackPanel` (horizontal reverse), `Grid` (column mirror), `DockPanel` (left/right swap), `TextBlock` (text alignment).

## 7. Culture-aware formatting

```xml
<TextBlock Text="{Binding Price, StringFormat='{}{0:C}'}" />
```

Set the formatting culture in startup:

```csharp
Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
```

## Key takeaways

- Use `PublicResXFileCodeGenerator` to make resource classes accessible from XAML
- `{x:Static}` resolves once at load time; use an `INotifyPropertyChanged` wrapper for runtime switching
- `FlowDirection="RightToLeft"` mirrors the layout for Arabic, Hebrew, Persian
- `StringFormat` in bindings follows `Thread.CurrentThread.CurrentCulture`
- Resource files produce satellite assemblies per locale automatically

## See Also

- [001 — Project Setup](../basics/001-project-setup.md)
- [032 — MVVM DI Wiring](032-mvvm-di-wiring.md)
- [033V — Localization and Internationalization (verbose companion)](033-localization-i18n-verbose.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Localization](https://docs.avaloniaui.net/docs/guides/implementation-guides/localization)
- [.NET Resource Manager docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/resources)
- [033X — Localization and Internationalization (examples)](033-localization-i18n-examples.md)
