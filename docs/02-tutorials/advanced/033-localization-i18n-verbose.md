---
tier: advanced
topic: localization
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 033-localization-i18n.md
---

# 033V — Localization and Internationalization: An In-Depth Companion

This companion explains the ResX resource system, satellite assembly resolution, runtime language switching mechanics, and right-to-left layout considerations. Read it alongside [033 — Localization and Internationalization](033-localization-i18n.md).

---

## 1. ResX Resource Files — How the Resource Manager Works

### What ResX files are

`.resx` files are XML-based resource containers compiled into `.resources` binaries and either embedded in the main assembly or deployed as satellite assemblies. Each entry is a key-value pair, where the value is a string, image, icon, or any serializable object.

### The fallback chain

```
Resources.resx         (neutral/default — English)
Resources.fr.resx      (French — specific culture "fr")
Resources.fr-CA.resx   (French Canadian — specific culture "fr-CA")
Resources.ja.resx      (Japanese — specific culture "ja")
```

When `CurrentUICulture` is set to `fr-CA`:
1. `ResourceManager` looks in `Resources.fr-CA.resources` (satellite assembly for `fr-CA`).
2. If not found, falls back to `Resources.fr.resources` (parent culture `fr`).
3. If not found, falls back to `Resources.resources` (neutral culture, embedded in main assembly).
4. If the key does not exist in any fallback, the `ResourceManager` returns the key name as a string (or throws, depending on `ThrowOnMissingResource`).

### Why the default `.resx` file generates code and satellite assemblies do not

```xml
<EmbeddedResource Update="Lang\Resources.resx">
  <Generator>PublicResXFileCodeGenerator</Generator>
  <LastGenOutput>Resources.Designer.cs</LastGenOutput>
</EmbeddedResource>
```

- The **default** `.resx` (no culture code in the filename) undergoes `PublicResXFileCodeGenerator` to produce `Resources.Designer.cs` — a class with static properties for each key.
- **Locale-specific** `.resx` files (e.g., `Resources.fr.resx`, `Resources.ja.resx`) do NOT generate code. They are compiled into satellite assemblies (`DemoApp.resources.dll` in a `fr` subfolder, etc.).
- At runtime, `Resources.Greeting` accesses the neutral resource, and the `ResourceManager` resolves the locale-specific value from the satellite assembly automatically.

---

## 2. The `PublicResXFileCodeGenerator` — Why Public

The standard `ResXFileCodeGenerator` generates `internal` class members. `PublicResXFileCodeGenerator` generates `public` members. XAML's `{x:Static}` can only access `public` static properties. If the generated `Resources` class is `internal`, you get a compile-time accessibility error.

### What the generated code looks like

```csharp
// Resources.Designer.cs
namespace DemoApp.Lang {
    public class Resources {
        private static global::System.Resources.ResourceManager resourceMan;
        public static string Greeting => ResourceManager.GetString("Greeting") ?? "Greeting";
        // ...
    }
}
```

The `ResourceManager` instance is cached in a static field (`resourceMan`). Each property call goes through `ResourceManager.GetString(key)`, which performs the culture fallback lookup.

---

## 3. Setting the Culture — What Each Approach Does

### Static assignment at startup

```csharp
Resources.Culture = new CultureInfo("fr");
```

Setting `Resources.Culture` changes the culture for the *generated* `Resources` class only. The `Resources.Culture` property overrides `Thread.CurrentThread.CurrentUICulture` for this specific resource manager. All `Resources.Greeting` calls now return French strings.

### Thread.CurrentThread.CurrentUICulture

```csharp
Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");
```

- `CurrentCulture` — affects number, date, and currency formatting (`StringFormat='{}{0:C}'` uses this).
- `CurrentUICulture` — affects resource lookup (`ResourceManager.GetString` uses this if `Resources.Culture` is not explicitly set).

Setting both at startup ensures consistency: resource strings follow the UI culture, and formatted values follow the numeric culture.

### The problem with thread-level culture

`Thread.CurrentThread.CurrentUICulture` is per-thread. In a multi-threaded app, background threads may use a different culture if they do not inherit the main thread's culture. `Resources.Culture` is a static class-level override that applies to all threads accessing that resource manager. Use `Resources.Culture` for XAML-bound resources and `CurrentCulture` for formatting (but be aware of thread affinity).

---

## 4. Using Localized Text in XAML — `{x:Static}` Mechanics

```xml
<TextBlock Text="{x:Static lang:Resources.Greeting}" />
```

### What `{x:Static}` does

1. At XAML compile time, the compiler resolves `lang:Resources` to the `DemoApp.Lang.Resources` class.
2. It generates code equivalent to `Text = DemoApp.Lang.Resources.Greeting`.
3. The property is evaluated **once** when the XAML is loaded.
4. The binding does NOT subscribe to `INotifyPropertyChanged`. If `Resources.Culture` changes at runtime, the `TextBlock.Text` does NOT update.

### Why this limitation exists

`{x:Static}` is a markup extension that resolves a static value. It is not a binding — it has no change notification mechanism. For runtime language switching, you must use a binding-based approach (section 5).

---

## 5. Runtime Language Switching — The INotifyPropertyChanged Wrapper

```csharp
public class LocalizationService : INotifyPropertyChanged
{
    public string this[string key] => Resources.ResourceManager.GetString(key) ?? key;

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

### Why `PropertyChangedEventArgs(string.Empty)`

Passing `string.Empty` (or `null`) for the property name signals "all properties have changed." This is a WPF/Avalonia convention: the binding engine sees `PropertyChanged` with an empty name and re-reads *all* bound properties on the object. This is the simplest way to refresh all localized text without raising separate events for each resource key.

### The indexer pattern

```csharp
public string this[string key] => Resources.ResourceManager.GetString(key) ?? key;
```

An indexer lets you bind to arbitrary resource keys:

```xml
<TextBlock Text="{Binding Localization[WelcomeMessage]}" />
```

But `{Binding Localization[WelcomeMessage]}` requires `Localization` to be a property on the DataContext that returns an object with an indexer. The simpler per-property approach (`Greeting`, `WelcomeMessage`) is more commonly used because it works with compiled bindings (`x:DataType`).

### DI registration

```csharp
services.AddSingleton<LocalizationService>();
```

The `LocalizationService` is a singleton because:
- The culture state is application-wide.
- All ViewModels share the same instance.
- PropertyChanged notifications from one ViewModel's language switch reach all bindings.

### Why the tutorial's `{x:Static}` code is incomplete

The tutorial shows two binding approaches:

```xml
<TextBlock Text="{Binding Greeting, Source={x:Static services:LocalizationService.Instance}}" />
```

This assumes `LocalizationService` has a static `Instance` property. The code sample in section 5 does NOT define one — it shows DI registration instead. The working DI-aware form is:

```xml
<TextBlock Text="{Binding Localization.Greeting}" />
```

Where the ViewModel exposes a `Localization` property that returns the injected `LocalizationService`:

```csharp
public partial class MainViewModel(ILocalizationService localization) : ObservableObject
{
    public ILocalizationService Localization => localization;
}
```

---

## 6. Right-to-Left Support — FlowDirection Mechanics

```xml
<Window FlowDirection="RightToLeft">
```

### How `FlowDirection` works

`FlowDirection` is an attached/inherited property. When set on a `Window`, all child controls inherit it. Each control uses the direction to interpret its layout:

- **StackPanel** (`Orientation="Horizontal"`): Children are arranged from right to left.
- **Grid**: Column 0 is the rightmost column (columns are mirrored).
- **DockPanel**: `Dock="Left"` docks to the right edge in RTL; `Dock="Right"` docks to the left.
- **TextBlock**: Text alignment flips (left-aligned text becomes right-aligned). For pure RTL scripts (Arabic, Hebrew), the text itself is inherently RTL and `FlowDirection` affects alignment and cursor navigation.
- **ScrollViewer**: Scrollbar appears on the left side.

### Controls that do NOT flip with FlowDirection

- **Image** — content is not mirrored (a left-facing arrow stays left-facing).
- **Canvas** — uses absolute positioning; no auto-flip.
- **Custom rendering** — `DrawingContext` does not automatically mirror. If you use `DrawLine` or `DrawGeometry`, you must handle the mirror manually.

### Programmatic RTL detection

```csharp
var culture = new CultureInfo("ar-SA");
if (culture.TextInfo.IsRightToLeft)
{
    mainWindow.FlowDirection = FlowDirection.RightToLeft;
}
```

`CultureInfo.TextInfo.IsRightToLeft` returns `true` for all RTL cultures (Arabic, Hebrew, Persian, Urdu, etc.). This check should happen before the window is shown — changing `FlowDirection` after layout is visible can cause jarring re-layout.

### What does not change with FlowDirection

- **Input**: Text input direction depends on the keyboard layout, not `FlowDirection`. An Arabic keyboard still types Arabic right-to-left even if `FlowDirection="LeftToRight"`.
- **Number formatting**: `FlowDirection` does not affect `StringFormat`. Numbers still use `CurrentCulture` formatting.

---

## 7. Culture-Aware Formatting

```xml
<TextBlock Text="{Binding Price, StringFormat='{}{0:C}'}" />
```

### How `StringFormat` resolves the culture

The `StringFormat` binding uses the binding's `BindingCulture` property. By default, this is `Thread.CurrentThread.CurrentCulture`. When `CurrentCulture` is set to `de-DE`:

- `{0:C}` produces `1.234,56 €` (German format: comma as decimal separator, dot as thousands separator, EUR symbol).
- `{0:N}` produces `1.234,56`.
- `{0:D}` produces `Donnerstag, 14. Juni 2026`.

### Setting the formatting culture for bindings

Set `Thread.CurrentThread.CurrentCulture` before any bindings are evaluated:

```csharp
Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
```

This affects all bindings that use `StringFormat`. To override per-binding, set `Binding.Culture`.

### Common mistake: `StringFormat` with localized strings

```xml
<TextBlock Text="{Binding Price, StringFormat='Price: {0:C}'}" />
```

The prefix `"Price: "` is hardcoded in English. For localization, use `StringFormat` only for numbers/dates, and combine with localized label text from resources:

```xml
<StackPanel Orientation="Horizontal">
  <TextBlock Text="{x:Static lang:Resources.PriceLabel}" />
  <TextBlock Text="{Binding Price, StringFormat='{}{0:C}'}" />
</StackPanel>
```

---

## Building a Complete Localization Strategy

### File structure

```
/Lang/
  Resources.resx           (neutral — English)
  Resources.fr.resx        (French)
  Resources.ja.resx        (Japanese)
  Resources.ar-SA.resx     (Arabic — RTL)
/Services/
  LocalizationService.cs   (INotifyPropertyChanged wrapper)
/ViewModels/
  MainViewModel.cs         (injects ILocalizationService)
```

### Adding a new language

1. Copy `Resources.resx` to `Resources.xx-XX.resx`.
2. Translate all values.
3. Add a menu item or setting for the new language.
4. Call `localizationService.SetCulture("xx-XX")`.
5. If the language is RTL, set `FlowDirection = FlowDirection.RightToLeft`.

### Testing localization

- Set `Thread.CurrentThread.CurrentUICulture` in test setup to verify resource resolution.
- Use `CultureInfo.GetCultures(CultureTypes.AllCultures)` to enumerate all available cultures.
- Validate that no culture falls back to neutral for missing keys.

---

## See Also

- [033 — Localization and Internationalization (original)](033-localization-i18n.md)
- [001 — Project Setup](../basics/001-project-setup.md)
- [032 — MVVM DI Wiring](032-mvvm-di-wiring.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Localization](https://docs.avaloniaui.net/docs/guides/implementation-guides/localization)
- [.NET Resource Manager docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/resources)
- [033X — Localization and Internationalization (examples)](033-localization-i18n-examples.md)
