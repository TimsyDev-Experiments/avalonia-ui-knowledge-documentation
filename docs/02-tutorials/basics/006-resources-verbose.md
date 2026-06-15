---
tier: basics
topic: resources
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 006-resources.md
---

# 006V — Resources: An In-Depth Companion

**What you'll learn in this companion:** How the resource dictionary works internally, the lookup algorithm step by step, the difference between `StaticResource` and `DynamicResource` at the implementation level, how merged dictionaries resolve key conflicts, and how theme resources differ from application resources.

**Prerequisites:** [003 — Basic Styling](003-basic-styling.md)

**You should already have read:** [006 — Resources](006-resources.md) for the quick-start version. This file goes deeper on every section.

---

## 1. What a Resource Dictionary Actually Is

```xml
<Window.Resources>
  <SolidColorBrush x:Key="PrimaryBrush" Color="#6a33ff" />
</Window.Resources>
```

`Window.Resources` returns an `IResourceDictionary`. This is not a simple `Dictionary<string, object>` — it is an interface that supports hierarchical lookup:

```csharp
public interface IResourceDictionary : IDictionary<object, object?>
{
    void AddRange(IEnumerable<KeyValuePair<object, object?>> values);
    void RemoveRange(IEnumerable<object> keys);
    bool TryGetResource(object key, out object? value);
}
```

The `TryGetResource` method is the core of resource resolution. When the binding system calls `TryGetResource("PrimaryBrush", out value)`, the `ResourceDictionary` checks its own entries first. If not found, it delegates to its `Owner` (the parent `IResourceNode`) via `TryGetResource` on the owner. This recursive delegation is what creates the lookup chain.

### Resource Keys Are `object`, Not `string`

The `x:Key` attribute in XAML is always a string, but the dictionary key is typed as `object`. This means you can use non-string keys. The most important non-string key is the `Type` key used by `Styles`:

```xml
<Style Selector="Button.primary" x:Key="{Button.primary}">
  <Setter Property="Background" Value="#6a33ff" />
</Style>
```

Here, the key is the **selector string** itself (brace-syntax). `{StaticResource {Button.primary}}` uses this key to retrieve the style. The braces are literal characters in the key string — Avalonia's `ResourceDictionary` compares keys using `object.Equals()`, so the string `"{Button.primary}"` must match exactly.

---

## 2. `StaticResource`: One-Time Lookup at Load Time

```xml
<Button Background="{StaticResource PrimaryBrush}" />
```

When the XAML compiler encounters `{StaticResource PrimaryBrush}`, it emits code that:

1. At load time, calls `TryFindResource("PrimaryBrush")` on the element that owns the property (`Button`).
2. `TryFindResource` walks up the logical tree:
   - `Button.Resources` → `StackPanel.Resources` → `Window.Resources` → `Application.Resources` → `Theme.Resources`
3. If found, assigns the resource value directly to the `Background` property.
4. If not found, throws a `KeyNotFoundException` at runtime.

The lookup happens **once**, during the XAML loading phase (before the window is shown). The `Button` stores a direct reference to the `SolidColorBrush` object. If the brush is later modified (color changed), the button reflects the change because it holds a reference to the same mutable object. However, if the resource dictionary entry is **replaced** (e.g., `Resources["PrimaryBrush"] = new SolidColorBrush(Colors.Red)`), the button does **not** update — it still holds the old brush reference.

### Why StaticResource Is Faster

- No event subscription: `StaticResource` does not listen for resource-changed events.
- Single lookup: the resource reference is resolved once and stored directly.
- Inline IL: with compiled bindings, the resource reference can be inlined as a direct field access.

Use `StaticResource` for values that are defined once and never swapped at runtime: brand colors, fixed padding values, converter instances.

---

## 3. `DynamicResource`: Live Lookup with Change Tracking

```xml
<Button Background="{DynamicResource PrimaryBrush}" />
```

`DynamicResource` installs a `DynamicResourceBinding` — a specialized `IBinding` that:

1. At load time, registers a `ResourcesChanged` listener on the `Button`. This listener fires whenever any resource dictionary in the ancestor chain is modified.
2. When the listener fires, it re-runs `TryFindResource("PrimaryBrush")` and updates the `Background` property if the result differs from the current value.
3. The binding also stores the resource key and the target property — it can re-apply the lookup at any time.

The `DynamicResource` infrastructure imposes:

- **Memory:** Each `DynamicResource` reference allocates a `DynamicResourceBinding` object and a `ResourcesChanged` event subscription. For 500 `DynamicResource` references on a page, this is 500 small objects + 500 event handlers.
- **CPU:** Every resource dictionary modification (even on unrelated keys) triggers re-evaluation of all `DynamicResource` bindings that reference any key in that dictionary chain. If you frequently add/remove resources, you pay a scan cost.

### When DynamicResource Is Required

- **Theme switching:** When the user switches from Light to Dark theme, the theme dictionaries replace their resource values. `StaticResource` would still reference the old Light-theme brush. `DynamicResource` picks up the new value.
- **Runtime resource overrides:** If you allow the user to change accent colors in-app and store them in `Application.Resources`, `DynamicResource` propagates the change.
- **Third-party theme dictionaries:** If you reference `{DynamicResource SystemAccentColor}`, the lookup resolves to whichever theme dictionary is currently active.

### When Not to Use DynamicResource

- **Converter instances:** Converters do not change at runtime. `StaticResource` is always correct.
- **Immutable values:** `x:Double`, `x:Int32`, `CornerRadius` — these value types are copied on assignment. A `DynamicResource` re-assignment copies the new value, but changing them at runtime is rare.
- **Performance-critical lists:** Inside `DataTemplate` items, each element may reference the same `DynamicResource`, multiplying the bindings. Use `StaticResource` for shared brushes and colors.

---

## 4. Resource Lookup Order: Step by Step

```
Element.Resources → Parent.Resources → Window.Resources →
Application.Resources → Theme Resources (Fluent/Simple)
```

The lookup algorithm implemented in `StyledElement.TryFindResource(string key)`:

1. Check the element's own `Resources` dictionary. If found, return.
2. Check the element's logical parent's `Resources`. Repeat up the tree.
3. If the element is a `Window` or `UserControl`, check its `Resources`.
4. Check `Application.Current.Resources`.
5. Check each registered theme's resource dictionary (in order: `FluentTheme`, `SimpleTheme`, custom theme dictionaries).
6. If not found in any theme, return `AvaloniaProperty.UnsetValue`.

At step 2, "logical parent" is the `Parent` property of a `Control`, which follows the logical tree. This is **not** the same as the visual tree. A `ToolTip`'s logical parent is the element it targets, not the `Popup` root. This distinction matters for resource lookup in popups and flyouts: they can see the target element's resources.

### Shadowing Rules

A resource defined at a lower level (closer to the element) shadows a resource with the same key at a higher level:

```xml
<Application.Resources>
  <SolidColorBrush x:Key="PrimaryBrush" Color="Blue" />
</Application.Resources>

<Window.Resources>
  <SolidColorBrush x:Key="PrimaryBrush" Color="Red" />
</Window.Resources>

<!-- All buttons in this window see Red, not Blue -->
<Button Background="{StaticResource PrimaryBrush}" />
```

The `Button`'s lookup finds `PrimaryBrush` at `Window.Resources` first (step 3) and never reaches `Application.Resources` (step 4).

---

## 5. Merged Dictionaries: How They Resolve

```xml
<ResourceDictionary>
  <ResourceDictionary.MergedDictionaries>
    <ResourceInclude Source="/Assets/Styles/Colors.axaml" />
    <ResourceInclude Source="/Assets/Styles/Typography.axaml" />
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

`ResourceDictionary.MergedDictionaries` is an `IList<ResourceDictionary>`. When the parent dictionary performs a `TryGetResource`:

1. It checks its own entries first (keys defined directly inside the `<ResourceDictionary>` element).
2. Then it iterates through `MergedDictionaries` **in order** and calls `TryGetResource` on each.
3. The first match wins.

If `Colors.axaml` and `Typography.axaml` both define a key named `PrimaryColor`, the one from `Colors.axaml` wins because it appears first in `MergedDictionaries`. **Order matters.**

### ResourceInclude vs ResourceDictionary

- `<ResourceInclude Source="...">` is a markup extension that loads a XAML file at runtime and wraps it as a `ResourceDictionary`. The file at `Source` must have `<ResourceDictionary>` as its root element.
- You can also embed `<ResourceDictionary>` inline:

```xml
<ResourceDictionary.MergedDictionaries>
  <ResourceDictionary>
    <SolidColorBrush x:Key="InlineBrush" Color="Green" />
  </ResourceDictionary>
</ResourceDictionary.MergedDictionaries>
```

Inline embedded dictionaries behave identically to `ResourceInclude` — they are just other entries in the `MergedDictionaries` list.

---

## 6. Theme Resources: How FluentTheme Defines Its Palette

```xml
<Application.Styles>
  <FluentTheme />
</Application.Styles>
```

When you add `<FluentTheme />` to `Application.Styles`, it loads a set of built-in `ResourceDictionary` instances that define hundreds of resource keys: `SystemAccentColor`, `SystemAccentColorDark1`, `SystemControlBackgroundAltHighBrush`, etc.

These theme dictionaries are `ResourceDictionary` instances stored in the `Application.Styles` collection, not in `Application.Resources`. Yet theme resources are still discoverable via `TryFindResource` because the styles collection implements `IResourceProvider`. The lookup chain includes theme dictionaries after `Application.Resources`.

### Overriding Theme Resources

```xml
<Application.Styles>
  <FluentTheme />
  <Style Selector="Button.primary">
    <Setter Property="Background" Value="{DynamicResource SystemAccentColor}" />
  </Style>
</Application.Styles>
```

To override a theme resource globally, define a resource with the same key **after** the theme in `Application.Resources`:

```xml
<Application.Resources>
  <Color x:Key="SystemAccentColor">#6a33ff</Color>
</Application.Resources>
```

Because the lookup reaches `Application.Resources` before theme dictionaries, your override wins. However, the theme dictionary may bind to its own resources internally — overriding `SystemAccentColor` changes only the resources that reference it via `{DynamicResource SystemAccentColor}`, not the theme's internal style setters. Some theme internals use hardcoded colors. Overriding theme palette keys is best-effort, not guaranteed.

---

## 7. Primitive Type Elements as Resources

```xml
<x:Double x:Key="DefaultPadding">16</x:Double>
<CornerRadius x:Key="CardCorner">8</CornerRadius>
```

The `x:Double`, `x:Int32`, `x:String`, and `x:Boolean` elements are standard XAML primitives. When the XAML parser reads `<x:Double>16</x:Double>`, it creates a `System.Double` instance with value `16.0`. The resource dictionary stores a `double` (boxed to `object`).

When you use `{StaticResource DefaultPadding}` as a `Padding` value, the binding system converts the `double` to `Thickness` using Avalonia's type converter — `Padding` accepts a uniform `double` value and applies it to all four sides. This conversion happens at the property-assignment level and is not cached.

**Limitation:** You cannot store arbitrary value types this way. `x:Double` works only for `System.Double`. For `System.TimeSpan`, `System.Uri`, or your own structs, you need a custom markup extension or a `System.String` with runtime parsing.

---

## Common Mistakes

1. **Key collision between `MergedDictionaries` and direct entries.** A direct entry in the parent dictionary shadows all merged dictionaries. Put overrides in the parent dictionary, base definitions in merged dictionaries.
2. **Using `StaticResource` for theme-affected resources.** If the resource value comes from a theme dictionary (e.g., `SystemAccentColor`), use `DynamicResource`. `StaticResource` captures the current theme's value and never updates when the theme changes.
3. **Missing `x:Key` on a resource.** The XAML compiler requires `x:Key` on all items in a `ResourceDictionary`. Missing it produces a compile error.
4. **Putting `<Style>` in `Resources` without a key.** When you add `<Style Selector="...">` to a `Resources` dictionary, it must have a key (auto-generated or explicit). Selector-based styles typically go in `<Window.Styles>` or `<Application.Styles>`, not in `Resources`.
5. **Loading a `ResourceInclude` from a path that does not exist.** The `ResourceInclude` checks the path at runtime, not at compile time. A missing file produces a runtime exception with no build warning.

---

## See Also

- [006 — Resources (original tutorial)](006-resources.md)
- [006X — Resources (examples)](006-resources-examples.md)
- [003 — Basic Styling](003-basic-styling.md)
- [003V — Basic Styling (verbose companion)](003-basic-styling-verbose.md)
- [017 — Theme Switching](../intermediate/017-theme-switching.md)
- [Avalonia Docs: Resources](https://docs.avaloniaui.net/docs/styling/resources)
