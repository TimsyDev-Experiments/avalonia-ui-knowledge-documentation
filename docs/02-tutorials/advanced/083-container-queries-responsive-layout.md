---
tier: advanced
topic: layout
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 083 — Container Queries & Responsive Layout

**What you'll learn:** Build layouts that adapt to container size using container queries, `OnFormFactor`, `OnPlatform`, reflowing panels, and breakpoint view models.

**Prerequisites:** [080 — Layout System Deep Dive](080-layout-system-deep-dive.md)

---

## 1. Approaches at a glance

| Technique | Responds to | Resolves | Best for |
|---|---|---|---|
| Container queries | Size of an ancestor control | Live, as control resizes | Reusable components in panels of varying width |
| `OnFormFactor` | Device type (desktop, mobile) | Once, at startup | Platform-specific layout differences |
| `OnPlatform` | Operating system | Once, at startup | OS-specific values (fonts, sizes) |
| Reflowing panels | Available width | Live, as panel resizes | Card grids and flowing content |
| Breakpoint view models | Any measured value | Live, via property change | Complex multi-property transitions |

---

## 2. Container queries

Container queries activate styles when an ancestor reaches a specific size. Unlike window-level checks, a container-queried component adapts correctly whether in a full-width page, a narrow sidebar, or a dialog.

### Declaring a container

```xml
<Border Container.Name="main"
         Container.Sizing="Width">
  <!-- children -->
</Border>
```

`Container.Sizing` values:

| Value | Tracked |
|---|---|
| `Normal` | None (default) |
| `Width` | Width only |
| `Height` | Height only |
| `WidthAndHeight` | Both |

### Writing a container query

```xml
<Window.Styles>
  <ContainerQuery Name="main" Query="max-width:600">
    <Style Selector="StackPanel#sidebar">
      <Setter Property="IsVisible" Value="False" />
    </Style>
  </ContainerQuery>
</Window.Styles>
```

The `ContainerQuery` lives in `Styles` (or a `ControlTheme`) and activates its child styles when the query condition matches.

### Query syntax

| Operator | Meaning |
|---|---|
| `min-width:400` | Container width >= 400 |
| `max-width:400` | Container width <= 400 |
| `min-height:300` | Container height >= 300 |
| `max-height:300` | Container height <= 300 |
| `width:400` | Container width == 400 |
| `height:300` | Container height == 300 |

Combine conditions:

```xml
<!-- AND -->
<ContainerQuery Name="main" Query="min-width:400 and max-width:800" />

<!-- OR (comma) -->
<ContainerQuery Name="main" Query="max-width:300, min-height:600" />
```

### Breakpoint tiers

```xml
<Panel Container.Name="content" Container.Sizing="Width">
  <Panel.Styles>
    <ContainerQuery Name="content" Query="max-width:400">
      <Style Selector="UniformGrid#cards">
        <Setter Property="Columns" Value="1" />
      </Style>
    </ContainerQuery>
    <ContainerQuery Name="content" Query="min-width:400">
      <Style Selector="UniformGrid#cards">
        <Setter Property="Columns" Value="2" />
      </Style>
    </ContainerQuery>
    <ContainerQuery Name="content" Query="min-width:800">
      <Style Selector="UniformGrid#cards">
        <Setter Property="Columns" Value="3" />
      </Style>
    </ContainerQuery>
  </Panel.Styles>
  <UniformGrid x:Name="cards">
    <!-- card items -->
  </UniformGrid>
</Panel>
```

### Customising any property

Container queries work on any style-settable property — not just layout:

```xml
<ContainerQuery Name="content" Query="max-width:500">
  <Style Selector="TextBlock.heading">
    <Setter Property="FontSize" Value="18" />
  </Style>
  <Style Selector="StackPanel.toolbar">
    <Setter Property="Orientation" Value="Vertical" />
  </Style>
</ContainerQuery>
```

### Restrictions

- `ContainerQuery` cannot be nested inside a `Style` element — it must be a direct child of `Styles` or `ControlTheme`.
- Styles inside a `ContainerQuery` cannot affect the container itself or any ancestor (prevents cyclic resize loops).
- The container uses the maximum available size for its tracked dimension.

---

## 3. OnFormFactor and OnPlatform

### OnFormFactor

Resolves once at startup based on device type:

```xml
<Grid ColumnDefinitions="{OnFormFactor Desktop='250,*', Mobile='*'}">
  <Border Grid.Column="0"
          IsVisible="{OnFormFactor Desktop=True, Mobile=False}">
    <ListBox ItemsSource="{Binding MenuItems}" />
  </Border>
  <ContentControl Grid.Column="{OnFormFactor Desktop=1, Mobile=0}"
                  Content="{Binding CurrentPage}" />
</Grid>
```

Supported form factors: `Desktop`, `Mobile`, `TV`, `Default`.

### OnPlatform

Resolves once at startup based on operating system:

```xml
<TextBlock FontFamily="{OnPlatform macOS='San Francisco',
                                   Windows='Segoe UI',
                                   Default='Inter'}" />
```

Supported platforms: `Windows`, `macOS`, `Linux`, `iOS`, `Android`, `Browser`, `Default`.

---

## 4. Reflowing panels

### WrapPanel

Arranges children in a horizontal row, wrapping to the next line:

```xml
<WrapPanel ItemSpacing="8" LineSpacing="8">
  <Button Content="One" />
  <Button Content="Two" />
  <Button Content="Three" />
</WrapPanel>
```

### UniformGridLayout (with ItemsRepeater)

```xml
<ItemsRepeater ItemsSource="{Binding Cards}">
  <ItemsRepeater.Layout>
    <UniformGridLayout MinItemWidth="280"
                       MinItemHeight="200"
                       MinColumnSpacing="12"
                       MinRowSpacing="12" />
  </ItemsRepeater.Layout>
  <ItemsRepeater.ItemTemplate>
    <DataTemplate>
      <Border Padding="16" CornerRadius="8"
              Background="White"
              BorderBrush="#E5E7EB" BorderThickness="1">
        <TextBlock Text="{Binding Title}" />
      </Border>
    </DataTemplate>
  </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

`UniformGridLayout` calculates column count from available width and `MinItemWidth`.

---

## 5. Breakpoint view models

For responsive logic involving coordinated property changes or non-size conditions:

```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private bool _isCompact;
    [ObservableProperty] private bool _isWide;

    public void UpdateLayout(double windowWidth)
    {
        IsCompact = windowWidth < 640;
        IsWide = windowWidth >= 1024;
    }
}
```

```csharp
protected override void OnSizeChanged(SizeChangedEventArgs e)
{
    base.OnSizeChanged(e);
    if (DataContext is MainViewModel vm)
        vm.UpdateLayout(e.NewSize.Width);
}
```

```xml
<StackPanel IsVisible="{Binding IsCompact}" Spacing="8">
  <views:SidebarView />
  <views:ContentView />
</StackPanel>
<Grid IsVisible="{Binding !IsCompact}" ColumnDefinitions="280,*">
  <views:SidebarView Grid.Column="0" />
  <views:ContentView Grid.Column="1" />
</Grid>
```

---

## 6. Choosing the right panel

| Panel | Arrangement | Adaptive | Best for |
|---|---|---|---|
| `Grid` | Rows and columns | Yes | Forms, dashboards, general-purpose |
| `DockPanel` | Edges + fill | Yes | App shells (header/sidebar/content) |
| `StackPanel` | Single line | Partial | Toolbars, simple series |
| `WrapPanel` | Sequential with wrap | Yes | Tags, icon grids |
| `UniformGrid` | Equal cells | Yes | Keypads, tile galleries |
| `RelativePanel` | Relative to siblings | Yes | Adaptive repositioning |
| `Canvas` | Absolute coordinates | No | Diagrams, drawing surfaces |
| `Panel` | Layered | Yes | Overlays, stacking |

---

## Key Takeaways

- **Container queries** activate styles based on an ancestor control's size — reusable, live, XAML-only
- **OnFormFactor** / **OnPlatform** resolve once at startup for device or OS differences
- **WrapPanel** and **UniformGridLayout** reflow content automatically based on available width
- **Breakpoint view models** provide programmatic control for complex multi-property transitions
- Container queries live in `Styles` or `ControlTheme`, cannot affect the container itself, and use `Container.Name` / `Container.Sizing` to declare tracking targets

---

## See Also

- [080 — Layout System Deep Dive](080-layout-system-deep-dive.md) — measure/arrange internals
- [Choosing a layout panel](https://docs.avaloniaui.net/docs/layout/choosing-a-layout-panel)
- [Avalonia Docs: Responsive Layouts](https://docs.avaloniaui.net/docs/layout/responsive-layouts)
