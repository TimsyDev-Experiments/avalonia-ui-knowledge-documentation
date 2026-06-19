---
tier: advanced
topic: layout
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 083 — Container Queries & Responsive Layout — Quiz

**Prerequisites:** [083-core](083-container-queries-responsive-layout.md)

---

### Q1: What attached properties declare a control as a container for container queries?

<details>
<summary>Answer</summary>

`Container.Name` (sets the query name) and `Container.Sizing` (sets the tracked dimensions).

</details>

---

### Q2: Which `Container.Sizing` values are valid, and what do they each track?

<details>
<summary>Answer</summary>

- `Normal` — none (default)
- `Width` — width only
- `Height` — height only
- `WidthAndHeight` — both

</details>

---

### Q3: Where in XAML can a `ContainerQuery` be placed? (Choose all that apply.)

1. Inside a `Style` element
2. As a direct child of `Styles`
3. As a direct child of `ControlTheme.Styles`
4. Inside a `Setter`

<details>
<summary>Answer</summary>

2 and 3. Container queries are direct children of `Styles` or `ControlTheme.Styles`. They cannot be nested in a `Style` element or inside a `Setter`.

</details>

---

### Q4: What happens if a `ContainerQuery`'s styles attempt to change a property on the container itself?

<details>
<summary>Answer</summary>

The styles are ignored. Container queries cannot modify the container or any ancestor — this prevents cyclic layout loops.

</details>

---

### Q5: Write a container query that activates when the container's width is between 400 and 800 (inclusive).

<details>
<summary>Answer</summary>

```xml
<ContainerQuery Name="myContainer"
                Query="min-width:400 and max-width:800">
  <Style Selector="...">
    <Setter Property="..." Value="..." />
  </Style>
</ContainerQuery>
```

The `and` keyword requires both conditions to be true.

</details>

---

### Q6: True or False: Multiple containers in the same visual tree can use the same `Container.Name`, but each is tracked independently.

<details>
<summary>Answer</summary>

True. The name is not unique — every control with a matching `Container.Name` and `Container.Sizing` is independently evaluated against any `ContainerQuery` with that name in its ancestor chain.

</details>

---

### Q7: What is the difference between `OnFormFactor` and `OnPlatform`?

<details>
<summary>Answer</summary>

`OnFormFactor` resolves based on device type (Desktop, Mobile, TV, Default). `OnPlatform` resolves based on operating system (Windows, macOS, Linux, iOS, Android, Browser, Default). Both resolve once at XAML load time.

</details>

---

### Q8: Which layout automatically calculates column count from available width and a minimum item width?

<details>
<summary>Answer</summary>

`UniformGridLayout` (used with `ItemsRepeater`). It adapts the number of columns based on the container's width and the `MinItemWidth`/`MinColumnSpacing` properties.

</details>

---

### Q9: Describe one performance consideration when using container queries.

<details>
<summary>Answer</summary>

Accepted answers include:

- `WidthAndHeight` triggers styling re-evaluation for both dimensions — prefer `Width` or `Height` when possible.
- Avoid container queries on containers that resize every frame (e.g., inside active animations).
- Evaluation is batched per styling pass, so multiple containers with the same name share a pass.

</details>

---

### Q10: Write XAML that shows a `WrapPanel` displaying tag buttons.

<details>
<summary>Answer</summary>

```xml
<WrapPanel ItemSpacing="4" LineSpacing="4">
  <Button Content="Avalonia" />
  <Button Content="Responsive" />
  <Button Content="Layout" />
  <Button Content="Container Queries" />
</WrapPanel>
```

</details>

---

### Q11: Your `UniformGrid` card grid has a single static `Columns="3"` value but cards look cramped when the panel is narrow. What are two solutions?

<details>
<summary>Answer</summary>

1. Use container queries with multiple breakpoints (e.g., `max-width:400` → `Columns="1"`, `min-width:400` → `Columns="2"`, `min-width:800` → `Columns="3"`).
2. Replace `UniformGrid` with `ItemsRepeater` + `UniformGridLayout` with `MinItemWidth` set to the desired minimum card width — columns adapt automatically.

</details>

---

### Q12: Write a breakpoint view model property and the `Window.OnSizeChanged` wiring to expose an `IsCompact` flag.

<details>
<summary>Answer</summary>

```csharp
// ViewModel
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCompact;
}

// Window
protected override void OnSizeChanged(SizeChangedEventArgs e)
{
    base.OnSizeChanged(e);
    if (DataContext is MainViewModel vm)
        vm.IsCompact = e.NewSize.Width < 640;
}
```

```xml
<StackPanel IsVisible="{Binding IsCompact}">
  <!-- compact layout -->
</StackPanel>
```

</details>
