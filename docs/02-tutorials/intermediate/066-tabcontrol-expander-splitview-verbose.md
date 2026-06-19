---
tier: intermediate
topic: controls
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 066V — TabControl, Expander & SplitView (verbose companion)

**What this covers:** TabControl content reuse/lazy loading, TabItem styling, Expander events and nested hierarchies, SplitView pane animation, and FlyoutBase.

**Prerequisites:** 066 — TabControl, Expander & SplitView core

---

## 1. TabControl content lifecycle

By default, `TabControl` creates content for all tabs at startup. For lazy loading:

```xml
<TabControl ItemsSource="{Binding Tabs}"
            SelectedItem="{Binding SelectedTab}">
  <TabControl.ContentTemplate>
    <DataTemplate DataType="vm:TabItemViewModel">
      <views:TabContentView />
    </DataTemplate>
  </TabControl.ContentTemplate>
</TabControl>
```

Each time the selected tab changes, the `ContentTemplate` creates a new view instance. Only the active tab's content exists in the visual tree — reducing memory for complex tabs.

### TabItem styling

```xml
<TabControl>
  <TabControl.Styles>
    <Style Selector="TabItem">
      <Setter Property="Padding" Value="12,6" />
      <Setter Property="FontWeight" Value="SemiBold" />
    </Style>
    <Style Selector="TabItem:selected">
      <Setter Property="Background" Value="{StaticResource SystemAccentColor}" />
    </Style>
  </TabControl.Styles>
</TabControl>
```

### Content reuse with x:Shared

By default `DataTemplate` creates new instances each time. To reuse a single instance across tab switches, set the containing view as a cached resource.

---

## 2. TabStrip standalone

`TabStrip` provides the tab header visual without content switching:

```xml
<TabStrip ItemsSource="{Binding Categories}"
          SelectedItem="{Binding SelectedCategory}">
  <TabStrip.ItemTemplate>
    <DataTemplate>
      <StackPanel Orientation="Horizontal" Spacing="6">
        <PathIcon Data="{Binding Icon}" />
        <TextBlock Text="{Binding Name}" />
      </StackPanel>
    </DataTemplate>
  </TabStrip.ItemTemplate>
</TabStrip>
```

Combine with `Carousel` or `ContentControl` for custom content-switching logic.

---

## 3. Expander events

```csharp
expander.Expanding += (s, e) =>
{
    // Content is about to appear — load data lazily
};

expander.Collapsed += (s, e) =>
{
    // Content is hidden — release resources
};
```

The `Expanding` event fires at the start of the animation; `Collapsed` fires after the animation completes.

### Nested expanders

```xml
<Expander Header="General" IsExpanded="True">
  <StackPanel Spacing="8">
    <CheckBox Content="Dark mode" />
    <Expander Header="Advanced">
      <StackPanel Spacing="8">
        <CheckBox Content="Hardware acceleration" />
        <CheckBox Content="Verbose logging" />
      </StackPanel>
    </Expander>
  </StackPanel>
</Expander>
```

Keep hierarchies to 2 levels to avoid overwhelming users.

### ContentTransition options

```xml
<Expander Header="Transitions">
  <Expander.ContentTransition>
    <PageSlide Duration="0:0:0.25" Orientation="Vertical" />
  </Expander.ContentTransition>
</Expander>
```

Built-in transitions: `CrossFade`, `PageSlide`.

---

## 4. SplitView compact pane layout

In `CompactInline` or `CompactOverlay` mode, the closed pane shows a 48px strip (customizable via `CompactPaneLength`). This is ideal for icon-only nav bars:

```xml
<SplitView IsPaneOpen="{Binding IsPaneOpen}"
           DisplayMode="CompactInline"
           CompactPaneLength="56"
           OpenPaneLength="220">
  <SplitView.Pane>
    <StackPanel>
      <Button Content="☰" Command="{Binding TogglePaneCommand}"
              Width="56" Height="48" />
      <ListBox ItemsSource="{Binding NavItems}"
               SelectedItem="{Binding SelectedNavItem}">
        <ListBox.ItemTemplate>
          <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="12" Height="40">
              <PathIcon Data="{Binding Icon}" Width="20" />
              <TextBlock Text="{Binding Title}"
                         Opacity="{Binding #root.IsPaneOpen,
                             Converter={StaticResource BoolToDoubleConverter}}" />
            </StackPanel>
          </DataTemplate>
        </ListBox.ItemTemplate>
      </ListBox>
    </StackPanel>
  </SplitView.Pane>
</SplitView>
```

The label opacity can be animated to fade in/out as the pane opens/closes.

### Pane animation

The pane open/close animation is built into the theme. To customize, override the template's `SplitViewPane` visual states (`Closed`, `Open`, `CompactClosed`, `CompactOpen`).

---

## 5. FlyoutBase

`FlyoutBase` is the base class for `Flyout` and `MenuFlyout`. Attach a flyout to any control:

```xml
<Button Content="More info">
  <Button.Flyout>
    <Flyout>
      <StackPanel Spacing="8" Padding="12">
        <TextBlock Text="Details" FontWeight="Bold" />
        <TextBlock Text="This is additional information." TextWrapping="Wrap" />
      </StackPanel>
    </Flyout>
  </Button.Flyout>
</Button>
```

### Flyout placement

```xml
<Flyout Placement="Bottom" ShowMode="Standard">
```

| ShowMode | Behavior |
|----------|----------|
| `Standard` | Opens on click |
| `Transient` | Opens on click, closes on focus loss |
| `TransientWithDismissOnPointerMoveAway` | Like Transient, but also closes on pointer leave |

### Programmatic show

```csharp
flyout.ShowAt(button);
flyout.ShowAt(button, new PopupPositioning.PopupPositionerParameters());
```

---

## See Also

- [066 — TabControl, Expander & SplitView (core)](066-tabcontrol-expander-splitview.md)
- [066E — TabControl, Expander & SplitView (examples)](066-tabcontrol-expander-splitview-examples.md)
- [Avalonia API: TabControl](https://docs.avaloniaui.net/api/avalonia/controls/tabcontrol)
- [Avalonia API: Expander](https://docs.avaloniaui.net/api/avalonia/controls/expander)
- [Avalonia API: SplitView](https://docs.avaloniaui.net/api/avalonia/controls/splitview)
- [Avalonia API: FlyoutBase](https://docs.avaloniaui.net/api/avalonia/controls/flyout)
