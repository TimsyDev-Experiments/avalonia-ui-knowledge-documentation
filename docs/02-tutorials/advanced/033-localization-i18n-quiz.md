---
tier: advanced
topic: localization
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 033-localization-i18n.md
---

# Quiz — Localization & i18n

```quiz
Q: Which MSBuild generator produces resource classes accessible from XAML via `{x:Static}`?
A. PublicResXFileCodeGenerator (correct) || Generates public static properties on the resource class, enabling XAML access with `{x:Static}`.
B. ResXFileCodeGenerator || Generates internal properties, invisible from XAML in other assemblies.
C. SingleFileGenerator || A generic generator that does not produce strongly typed resource accessors.
D. EmbeddedResourceGenerator || Not a valid MSBuild generator for .resx files.
Explanation: PublicResXFileCodeGenerator emits public static properties so `{x:Static lang:Resources.Key}` works in XAML.
```

```quiz
Q: Why does `{x:Static}` not support runtime language switching without a wrapper?
A. `{x:Static}` resolves the value once during XAML load and never updates (correct) || The binding is static; the target property is set once and does not listen for change notifications.
B. `{x:Static}` requires a converter to work with INotifyPropertyChanged || Converters do not enable re-evaluation; the issue is the static resolution model itself.
C. ResourceManager.GetString always returns the invariant culture value at runtime || GetString respects CultureInfo; the problem is the binding never re-queries it.
D. The XAML compiler inlines the string literal during build || x:Static emits a reflection-based access, not inlining.
Explanation: x:Static is a one-time fetch. For dynamic switching, wrap resources in an INotifyPropertyChanged service and bind to its properties.
```

```quiz
Q: Which layout controls respect `FlowDirection="RightToLeft"`?
A. StackPanel, Grid, DockPanel, TextBlock (correct) || These controls mirror their layout logic when FlowDirection is set on an ancestor.
B. ScrollViewer, Border, Canvas, Expander || Canvas uses absolute positioning; Border does not arrange children directionally.
C. ItemsRepeater, VirtualizingStackPanel, WrapPanel || ItemsRepeater itself does not handle flow direction; VirtualizingStackPanel only stacks.
D. ListBox, ComboBox, TreeView || These are selection controls that inherit FlowDirection but do not rearrange child layout.
Explanation: StackPanel reverses child order, Grid mirrors columns, DockPanel swaps left/right dock, TextBlock flips text alignment.
```

```quiz
Q: A developer sets `Resources.Culture = new CultureInfo("de-DE")` in OnFrameworkInitializationCompleted but currency formatting still shows the wrong symbol. What is the most likely cause?
A. The binding uses `StringFormat='{}{0:C}'` but Thread.CurrentThread.CurrentCulture was not set (correct) || StringFormat in bindings respects Thread.CurrentThread.CurrentCulture, not Resources.Culture.
B. The .resx file for de-DE was not compiled into a satellite assembly || Satellite assemblies affect resource lookups, not culture-aware formatting.
C. x:Static bindings are case-sensitive and the key name is wrong || A wrong key would show the key string, not a wrong currency symbol.
D. The XAML compiler cached the invariant culture at build time || StringFormat is evaluated at runtime, not baked at build.
Explanation: Set both Thread.CurrentThread.CurrentCulture and Thread.CurrentThread.CurrentUICulture at startup to control StringFormat in bindings.
```

```quiz
Q: Which approach correctly enables runtime language switching without restarting the application?
A. A service implementing INotifyPropertyChanged that resets Resources.Culture and raises PropertyChanged with empty string (correct) || Raising PropertyChanged with string.Empty notifies all bindings to re-read the resource properties.
B. Calling InitializeComponent again on the current window after changing culture || Re-initializing does not update existing bindings and risks duplicate event handlers.
C. Setting Resources.Culture and calling Application.Current.Styles.Clear() || Clearing styles has no effect on binding values.
D. Reloading the .resx file from disk using ResourceManager.GetString directly || Resources are compiled; reloading the file at runtime is not supported.
Explanation: Raising PropertyChanged with string.Empty (or null) signals every property to re-read, triggering fresh calls to the resource accessors.
```
