---
tier: advanced
topic: custom controls
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 020-custom-templated-controls.md
---

# 020V — Custom Templated Controls: An In-Depth Companion

**Why this exists.** The original tutorial walks through the mechanics of building a `RatingControl`. This companion explains why each piece exists, how the property system works under the hood, what conventions matter, and what goes wrong when you skip steps.

**Read this alongside:** [020 — Custom Templated Controls](020-custom-templated-controls.md)

---

## 1. Why `TemplatedControl`?

The three base-class tiers in Avalonia are:

| Base class | Use when |
|---|---|
| `Control` | You override `Render(DrawingContext)` and draw everything yourself. No template. See [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md). |
| `TemplatedControl` | You want a *lookless* control — the visual appearance comes from a `ControlTheme` that consumers can replace. This is the default for most reusable controls. |
| `ContentControl` / `ItemsControl` | You need content/model hosting with built-in template selection. These extend `TemplatedControl` and add `ContentPresenter` / `ItemsPresenter` wiring. |

`TemplatedControl` exists so that your control's *logic* (value bounds, increment/decrement, validation) lives in the C# class, while its *look* (borders, colors, animations) lives in a theme XAML file that users can override without touching your code. This is the "lookless control" pattern inherited from WPF's `TemplatedControl` but adapted to Avalonia's `ControlTheme` system (which replaced WPF's `DefaultStyleKey` approach).

### What `TemplatedControl` gives you for free

- `OnApplyTemplate()` — called when the template is instantiated. This is the hook for finding named template parts.
- `Template` property — the `ControlTemplate` that defines the visual tree. Set via a `ControlTheme` setter.
- `State management` — pseudo-classes (`:pointerover`, `:pressed`, `:disabled`) are applied automatically to the templated root.
- `Styling integration` — `ControlTheme` selectors match by `TargetType`, so themes can be swapped at the application level.

### Common mistake: extending `UserControl` for reusable controls

`UserControl` composes other controls into a fixed layout. It has no `Template` property, no `OnApplyTemplate`, and no `ControlTheme` support. Use it for pages/views in an MVVM app, not for library controls. If you want consumers to restyle the control, start with `TemplatedControl`.

---

## 2. Property system: `StyledProperty` vs `DirectProperty`

The original declares `ValueProperty` and `MaximumProperty` as `StyledProperty<int>`. Here is why that choice matters.

### StyledProperty

```csharp
public static readonly StyledProperty<int> ValueProperty =
    AvaloniaProperty.Register<RatingControl, int>(nameof(Value), 0);
```

**What it does:** Registers a property that participates in Avalonia's multi-layered value precedence system. The effective value is computed from (from highest to lowest):
1. Animation
2. Local value (`SetValue`)
3. Style trigger
4. Style setter
5. Default value (the third argument to `Register`)

This means a theme author can override the default via a style setter, and an animation can temporarily override the style. `StyledProperty` is the default choice for any property that might be set in a style or animated.

**Why `AvaloniaProperty.Register<RatingControl, int>` and not just `Register<int>`:** The generic parameter `RatingControl` tells the property system which class owns this property. This enables inheritance down the logical tree — a property registered on a parent class can be inherited by child elements if `Inherits = true` is passed.

### DirectProperty

The original uses `DirectProperty` for the command properties:

```csharp
public static readonly DirectProperty<RatingControl, ICommand?> IncrementCommandProperty =
    AvaloniaProperty.RegisterDirect<RatingControl, ICommand?>(
        nameof(IncrementCommand),
        o => o.IncrementCommand);
```

**What it does:** A `DirectProperty` bypasses the value precedence system entirely. It reads/writes directly to a backing field. There is no style or animation involvement. Use it when:
- The value is set only from code-behind or binding (never from styles)
- You need maximum get/set performance (no precedence computation)
- The property is backed by a CLR field that you want to manage yourself

`DirectProperty` is commonly used for `ICommand` properties on controls because commands are almost never styled — they are bound from the ViewModel.

### When to pick which

| Scenario | Property type |
|---|---|
| Styleable / animatable | `StyledProperty` |
| Read-only (only has a getter in the public API) | `DirectProperty` with `RegisterDirect` and no setter |
| Performance-critical, set frequently per frame | `DirectProperty` |
| Attached property | `RegisterAttached` (always returns `AttachedProperty<T>`) |

### The `AffectsRender` pattern

The original does not include it, but any `StyledProperty` that changes the visual output should be registered with `AffectsRender<TRatingControl>()` in the static constructor:

```csharp
static RatingControl()
{
    AffectsRender<RatingControl>(ValueProperty, MaximumProperty);
}
```

Without this, changing `Value` does not call `InvalidateVisual()`, so the cached visual remains unchanged. The same principle applies to `AffectsMeasure` and `AffectsArrange` for layout-affecting properties.

---

## 3. `OnApplyTemplate` and named parts

```csharp
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);

    if (e.NameScope.Get<Button>("PartIncrement") is { } increment)
    {
        // Wire up logic
    }
}
```

### What `TemplateAppliedEventArgs` contains

The `e.NameScope` is an `INameScope` containing all elements in the instantiated template that have an `x:Name` or `Name` attribute. The `Get<T>(string)` method looks up the name and casts to `T`. If the name is missing or the type is wrong, `Get<T>` throws — use `Find<T>` instead if you want a null-returning variant.

### Why the `Part` prefix convention

Avalonia follows a convention (inherited from WPF) where template-part names start with "Part". The original `TemplatedControl` class in Avalonia uses names like `PART_ContentPresenter` (note the underscore prefix). For your own controls, you can use any naming convention, but prefixing with "Part" signals to maintainers that these names are part of the control's public API contract.

### Contract documentation

Every named part should be documented with XML comments:

```csharp
/// <summary>
/// The button that increments the rating value.
/// Template part name: "PartIncrement"
/// Required: false
/// </summary>
```

Mark parts as required or optional. If a part is required and missing in `OnApplyTemplate`, consider logging a warning or falling back to a default implementation.

### Unsubscription in `OnApplyTemplate`

`OnApplyTemplate` can be called multiple times during a control's lifetime (theme changes, reload). Always unsubscribe from old template events before subscribing to new ones:

```csharp
protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
{
    base.OnApplyTemplate(e);

    if (_incrementButton is not null)
        _incrementButton.Click -= OnIncrementClick;

    _incrementButton = e.NameScope.Find<Button>("PartIncrement");
    if (_incrementButton is not null)
        _incrementButton.Click += OnIncrementClick;
}
```

Without this, every theme switch adds another event handler subscription, causing memory leaks and duplicate invocations.

---

## 4. The `ControlTheme` and why it replaced WPF's `DefaultStyleKey`

```xml
<ControlTheme TargetType="local:RatingControl">
  <Setter Property="Template">
    <ControlTemplate TargetType="local:RatingControl">
      ...
    </ControlTemplate>
  </Setter>
</ControlTheme>
```

### What `ControlTheme` is

`ControlTheme` is a special kind of style that:
- Matches by `TargetType` (subtypes do NOT inherit — a `ControlTheme` for `Button` does not apply to `ToggleButton`)
- Is looked up by the `Theme` property of the control (not by type in a global resource dictionary)
- Can be explicitly overridden per-instance by setting `Theme={StaticResource ...}` on the control

This is different from WPF's `DefaultStyleKey` system, where the framework automatically looked for a `Style` with matching `TargetType` in the global `Resources`. Avalonia's approach is more explicit and avoids the "magic" lookup that caused styling surprises in WPF.

### How the lookup works

1. The control's `Theme` property is evaluated (default `null`)
2. If `Theme` is null, the framework walks up the application resources looking for a `ControlTheme` with matching `TargetType`
3. If found, that theme is applied; if not found, the control renders with no template (invisible)

This means you must **merge the theme dictionary** into `Application.Resources` (as shown in section 3 of the original) for the theme to be discovered.

### `ControlTemplate` vs `Template`

`ControlTemplate` (the class) defines the visual tree. The `<Setter Property="Template">` sets the `Template` property of the `TemplatedControl`. The `ControlTemplate` must declare its own `TargetType` because it uses that type to resolve `TemplateBinding` and `$parent` references.

### `TemplateBinding` vs `{Binding RelativeSource={RelativeSource TemplatedParent}}`

`TemplateBinding` is a compiled-binding shorthand equivalent to:

```xml
{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Value}
```

It is compiled (requires `x:DataType` context) and is the preferred way to bind to the templated parent's properties. `TemplateBinding` only works inside a `ControlTemplate` with a matching `TargetType`.

### `$parent` syntax

The original uses:

```xml
Command="{TemplateBinding $parent[local:RatingControl].DecrementCommand}"
```

The `$parent` syntax is an Avalonia extension in compiled bindings. `$parent[TypeName]` walks up the visual tree and finds the nearest ancestor of that type. This is necessary when you need to bind to a property on the templated parent that is not available via `TemplateBinding` (such as nested bindings inside a `DataTemplate` within the template).

---

## 5. Merging the theme dictionary

```xml
<ResourceDictionary>
  <ResourceDictionary.MergedDictionaries>
    <ResourceInclude Source="/Themes/RatingControl.axaml" />
  </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

### Why this is necessary

The `ControlTheme` for `RatingControl` is defined in a separate `RatingControl.axaml` file. Avalonia does not automatically scan assemblies for themes. You must explicitly include the resource dictionary in your `Application.Resources` so the framework can find it during theme lookup.

### The `Source` URI format

- `/Themes/RatingControl.axaml` — relative to the project root (works for both desktop and browser builds)
- `avares://MyApp/Themes/RatingControl.axaml` — absolute avares URI (use when the theme is in a different assembly)

### Theme dictionary as an asset

In your `.csproj`, the theme file must be included as an `EmbeddedResource` (Avalonia automatically treats `.axaml` files as such):

```xml
<ItemGroup>
  <AvaloniaResource Include="Themes\**" />
</ItemGroup>
```

The default Avalonia project template already includes this glob for `**/*.axaml`, so you usually don't need to add it manually.

---

## 6. Adding commands — the `RelayCommand` pattern

```csharp
private ICommand? _incrementCommand;

public ICommand? IncrementCommand =>
    _incrementCommand ??= new RelayCommand(Increment);

private void Increment()
{
    if (Value < Maximum)
        Value++;
}
```

### Why lazy-initialize

The `_incrementCommand` backing field is lazily created in the getter. This avoids allocating `RelayCommand` instances for every control instance if the command property is never accessed. Since `DirectProperty` getters are called frequently (especially during template application), the lazy pattern keeps construction cost low.

### Accepting `ICommand?` as the property type

Using `ICommand?` (nullable) allows binding to fail gracefully. If a consumer does not provide a command binding, the property returns its default value — a functional `RelayCommand` that delegates to the private `Increment` method. This is a **default-behavior pattern**: the control works out of the box without any binding.

### An alternative: exposing methods as commands via the ViewModel

If your control is used exclusively with MVVM, you might skip `RelayCommand` on the control and instead have the template bind directly to a ViewModel command:

```xml
<Button Command="{Binding $parent[local:RatingControl].DataContext.IncrementCommand}" />
```

But this couples the template to the ViewModel contract. The approach in the original — exposing commands on the control itself — is more reusable because the control encapsulates its own behavior.

---

## 7. Usage: bindings vs events

```xml
<controls:RatingControl Value="{Binding Rating}"
                        Maximum="5"
                        ValueChanged="OnRatingChanged" />
```

### Two-way binding for value properties

`Value` should typically use a two-way binding so that the control can update the ViewModel when the user clicks increment/decrement:

```csharp
public static readonly StyledProperty<int> ValueProperty =
    AvaloniaProperty.Register<RatingControl, int>(nameof(Value), 0, defaultBindingMode: BindingMode.TwoWay);
```

Set `defaultBindingMode` to `TwoWay` in the property registration so consumers can write:

```xml
<controls:RatingControl Value="{Binding Rating}" />
```

without specifying `Mode=TwoWay`.

### When to use `ValueChanged` event instead

The `ValueChanged` event (which you must define as a `RoutedEvent`) is useful for code-behind scenarios or when you need to respond to changes synchronously. ViewModel bindings should use the two-way property binding instead.

---

## 8. Testing a templated control

```csharp
[Fact]
public void RatingControl_Increment_UpdatesValue()
{
    var target = new RatingControl { Maximum = 5, Value = 2 };
    target.IncrementCommand.Execute(null);
    Assert.Equal(3, target.Value);
}

[Fact]
public void RatingControl_Theme_Applied()
{
    var target = new RatingControl();
    // In a headless test, apply the theme manually
    target.ApplyTemplate();
    Assert.NotNull(target.GetTemplateChildren());
}
```

See [038 — Headless Testing](../advanced/038-headless-testing.md) for setting up the test host.

---

## Cross-links

- [021 — Custom Controls from Scratch](021-custom-controls-from-scratch.md) — when you need Render-override controls instead of templates
- [020E — Custom Templated Controls (examples)](020-custom-templated-controls-examples.md)
- [016 — Property System & Attached Properties](../../02-tutorials/advanced/022-attached-properties-behaviors.md) — deeper dive into property value precedence
- [Avalonia Docs: Templated Controls](https://docs.avaloniaui.net/docs/concepts/templated-controls)
