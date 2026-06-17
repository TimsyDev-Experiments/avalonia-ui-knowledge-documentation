---
tier: intermediate
topic: theme switching
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 017-theme-switching.md
---

# Quiz — Theme Switching (Light/Dark/System)

```quiz
Q: Which property on the Application class controls the active light or dark theme at runtime?
A. ThemeVariant || Incorrect — ThemeVariant is a class, not a property; it is used as a value type.
B. RequestedThemeVariant (correct) || Correct. Setting Application.Current.RequestedThemeVariant to ThemeVariant.Light, ThemeVariant.Dark, or ThemeVariant.Default switches the theme at runtime.
C. CurrentTheme || Incorrect — there is no CurrentTheme property on Application.
D. ActiveTheme || Incorrect — there is no ActiveTheme property on Application.
Explanation: RequestedThemeVariant on Application.Current is the single property that controls which theme variant is active. Set it to ThemeVariant.Default to follow the OS preference.
```

```quiz
Q: Why must theme-dependent resources use {DynamicResource} instead of {StaticResource}?
A. {StaticResource} does not support SolidColorBrush values || Incorrect — StaticResource works with any resource type.
B. {DynamicResource} re-evaluates when the application theme changes; {StaticResource} is evaluated once (correct) || Correct. DynamicResource listens for theme changes and updates the binding; StaticResource is resolved at load time and never updates.
C. {StaticResource} can only be used in styles, not in direct property values || Incorrect — StaticResource works in both.
D. {DynamicResource} is required for compiled bindings to work || Incorrect — DynamicResource and compiled bindings are independent features.
Explanation: StaticResource resolves the resource once at XAML load time. DynamicResource listens for changes and updates the value when RequestedThemeVariant changes, which is essential for runtime theme switching.
```

```quiz
Q: What does ThemeVariant.Default do when assigned to RequestedThemeVariant?
A. It forces the light theme regardless of OS settings || Incorrect — Default does not force light.
B. It follows the operating system's light/dark preference (correct) || Correct. ThemeVariant.Default causes Avalonia to detect and follow the OS-level light or dark mode setting.
C. It falls back to the SimpleTheme || Incorrect — ThemeVariant.Default affects the theme variant, not which theme pack (Fluent/Simple) is used.
D. It disables theme switching entirely || Incorrect — it enables automatic system-following behavior.
Explanation: ThemeVariant.Default is the system-aware option — Avalonia monitors the OS theme preference and switches automatically when the user changes it.
```

```quiz
Q: How should a developer persist the user's theme choice across application restarts?
A. Store the choice in a static variable || Incorrect — static variables are lost when the app restarts.
B. Serialize the theme variant to a JSON file and reload it on startup (correct) || Correct. The tutorial shows saving the theme as a JSON file (settings.json) and deserializing it during startup in the LoadTheme method.
C. Register the theme choice in the Windows Registry || Incorrect — while possible, the tutorial uses JSON file persistence as the recommended approach.
D. Use Application.Current.Properties to store the theme || Incorrect — Avalonia's Application does not have a Properties dictionary like WPF.
Explanation: The tutorial demonstrates serializing the ThemeVariant enum value to a settings.json file and reloading it at application startup via File.ReadAllText and JsonSerializer.Deserialize.
```

```quiz
Q: A developer writes separate Light.axaml and Dark.axaml ResourceDictionary files with the same resource keys but different values. How are these loaded into the application?
A. Both dictionaries are merged in App.axaml and the active variant is selected automatically based on the resource key || Incorrect — both can be merged, but the variant is selected by RequestedThemeVariant, not by resource key.
B. Theme variant dictionaries are merged into App.axaml and switched implicitly when RequestedThemeVariant changes (correct) || Correct. Resource dictionaries are merged into Application.Resources and Avalonia's theme variant system selects the appropriate values based on the active variant.
C. The developer must manually swap dictionary files in code when the theme changes || Incorrect — Avalonia handles the switching automatically when dictionaries are properly defined with theme variants.
D. Separate resource dictionaries are not needed; use Conditional resources instead || Incorrect — the tutorial uses separate dictionary files as the recommended organization pattern.
Explanation: Light and dark resource dictionaries are merged into App.axaml. When RequestedThemeVariant changes, Avalonia automatically resolves DynamicResource references to the values from the currently active variant dictionary.
```
