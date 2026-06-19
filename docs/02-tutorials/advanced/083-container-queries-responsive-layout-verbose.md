---
tier: advanced
topic: layout
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 083 — Container Queries & Responsive Layout — Verbose

**Prerequisites:** [083-core](083-container-queries-responsive-layout.md)

---

## 1. Container queries deep dive

### Container lifecycle

When a control declares `Container.Name` and `Container.Sizing`, the layout system:

1. Reserves the maximum available space for the tracked dimension(s) during measure.
2. On arrange, the container's final size is published to all `ContainerQuery` instances that share the same `Name` and are ancestors of the container.
3. If the query condition matches, the child styles are applied. If the condition no longer matches, styles are removed.
4. Because the container takes maximum available space, it does not grow or shrink due to its own query — this is by design to prevent cyclic layout.

### Container.Sizing in practice

```xml
<Panel>
  <Border Container.Name="area"
          Container.Sizing="WidthAndHeight"
          Background="{StaticResource SurfaceBrush}">
    <StackPanel Spacing="8">
      <TextBlock Text="This border is the container." />
      <ContentControl Content="{Binding CurrentContent}" />
    </StackPanel>
  </Border>
</Panel>
```

With `WidthAndHeight`, the `Border` will take the full width and height of the parent `Panel`. The tracked dimensions are the final arranged size.

### Query evaluation detail

Container queries are evaluated during the styling pass that follows the arrange pass. When a container resizes:

1. Arrange completes for the container.
2. The styling system receives a notification.
3. Each `ContainerQuery` with matching `Name` re-evaluates its `Query` against the container's current size.
4. Child `Style` elements are activated or deactivated accordingly.
5. Activated styles participate in normal style precedence — they can override base styles but can themselves be overridden by later selectors or explicit local values.

### Multiple containers, same name

```xml
<StackPanel Spacing="16">
  <Border Container.Name="flex-card"
          Container.Sizing="Width"
          Background="LightBlue">
    <Button Content="Card A" />
  </Border>
  <Border Container.Name="flex-card"
          Container.Sizing="Width"
          Background="LightGreen">
    <Button Content="Card B" />
  </Border>
</StackPanel>

<StackPanel.Styles>
  <ContainerQuery Name="flex-card" Query="max-width:300">
    <Style Selector="Button">
      <Setter Property="Width" Value="280" />
    </Style>
  </ContainerQuery>
</StackPanel.Styles>
```

Each `Border` with `Container.Name="flex-card"` is independently tracked. If one is 250px wide and the other is 400px, only the 250px border activates the query.

### Container queries in ControlTheme

```xml
<ControlTheme x:Key="{x:Type ContentControl}" TargetType="ContentControl">
  <Setter Property="Template">
    <ControlTemplate>
      <Border Name="root"
              Container.Name="theme-container"
              Container.Sizing="Width">
        <ContentPresenter Name="PART_ContentPresenter" />
      </Border>
    </ControlTemplate>
  </Setter>
  <ContainerQuery Name="theme-container" Query="max-width:400">
    <Style Selector="ContentPresenter">
      <Setter Property="Padding" Value="8" />
    </Style>
  </ContainerQuery>
  <ContainerQuery Name="theme-container" Query="min-width:400">
    <Style Selector="ContentPresenter">
      <Setter Property="Padding" Value="24" />
    </Style>
  </ContainerQuery>
</ControlTheme>
```

Restriction: the `ContainerQuery` is a direct child of `ControlTheme.Styles`, not of a `Setter` or nested `Style`.

---

## 2. ResponsiveLayout helper

Avalonia includes `ResponsiveLayout` via the toolkit — a `Panel` that uses column-based breakpoints defined in XAML.

```xml
<ResponsiveLayout>
  <ResponsiveLayout.SmallTemplate>
    <StackPanel Spacing="8">
      <ContentControl Content="{Binding SmallHeader}" />
      <ContentControl Content="{Binding SmallBody}" />
    </StackPanel>
  </ResponsiveLayout.SmallTemplate>
  <ResponsiveLayout.LargeTemplate>
    <Grid ColumnDefinitions="2*,3*" ColumnSpacing="16">
      <ContentControl Content="{Binding LargeSidebar}" />
      <ContentControl Content="{Binding LargeBody}" />
    </Grid>
  </ResponsiveLayout.LargeTemplate>
</ResponsiveLayout>
```

Breakpoint thresholds are configurable on `ResponsiveLayout`, adapting automatically as the panel resizes.

---

## 3. OnFormFactor details

`OnFormFactor` resolves at XAML load time. The `FormFactor` enum has four values:

| Value | Typical detection |
|---|---|
| `Desktop` | Windowed, keyboard/mouse primary |
| `Mobile` | Touch-primary, smaller screen |
| `TV` | D-pad navigation, 10-foot UI |
| `Default` | Fallback when nothing else matches |

```xml
<TextBlock Text="{OnFormFactor Desktop='Full UI',
                               Mobile='Touch UI'}" />
```

---

## 4. Reflow panels detail

### WrapPanel vs StackPanel

| Panel | Behaviour |
|---|---|
| `StackPanel Orientation="Horizontal"` | All children in one row; overflow hidden or scrollable |
| `WrapPanel Orientation="Horizontal"` | Children wrap to next line when width exceeded |

### ItemsRepeater layouts

| Layout | Behaviour |
|---|---|
| `StackLayout` | Single row/column; no wrap |
| `WrapLayout` | Like WrapPanel but for ItemsRepeater |
| `UniformGridLayout` | Fixed-size cells, column count adapts to width |

```xml
<ItemsRepeater.ItemsSource="{Binding Tags}">
  <ItemsRepeater.Layout>
    <WrapLayout Orientation="Horizontal" Spacing="6" />
  </ItemsRepeater.Layout>
</ItemsRepeater>
```

---

## 5. Nesting container queries

Container queries can be nested within the same visual tree:

```xml
<Panel Container.Name="outer" Container.Sizing="Width">
  <Panel.Styles>
    <ContainerQuery Name="outer" Query="min-width:600">
      <Style Selector="Panel#wrapper">
        <Setter Property="Padding" Value="32" />
      </Style>
    </ContainerQuery>
  </Panel.Styles>
  <Panel x:Name="wrapper">
    <Border Container.Name="inner" Container.Sizing="Width">
      <Border.Styles>
        <ContainerQuery Name="inner" Query="min-width:300">
          <Style Selector="TextBlock">
            <Setter Property="FontSize" Value="18" />
          </Style>
        </ContainerQuery>
      </Border.Styles>
    </Border>
  </Panel>
</Panel>
```

Both the outer and inner container queries are evaluated on each layout pass. The inner query only activates when the inner border exceeds 300px, regardless of the outer panel's width.

---

## 6. Performance considerations

- Container queries add a styling re-evaluation after each arrange pass that changes a container's size. For most apps the cost is negligible.
- Avoid placing container queries on deeply nested containers that resize every frame (e.g., inside an active animation).
- `Container.Sizing` with `WidthAndHeight` triggers a separate styling pass for each tracked dimension — prefer `Width` or `Height` if only one dimension matters.
- Multiple containers with the same name each produce an independent evaluation — but the evaluations are batched per styling pass.

---

## Key Takeaways

- Container queries are evaluated after arrange, applying styles conditionally based on container size
- `Container.Sizing` determines which dimension(s) are tracked — `WidthAndHeight` triggers two evaluations per pass
- `ControlTheme` can contain container queries as direct children of `Styles`, but not inside setters or nested styles
- `ResponsiveLayout` (toolkit) offers a code-based alternative in a single panel
- Nesting is supported; each container is independently tracked
- Prefer `Width` or `Height` over `WidthAndHeight` when only one dimension matters for performance
