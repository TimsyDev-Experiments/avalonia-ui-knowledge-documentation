---
tier: intermediate
topic: controls
estimated: 18 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 066 — TabControl, Expander & SplitView

**What you'll learn:** How to build tabbed interfaces, collapsible sections, and collapsible side panes using `TabControl`, `Expander`, and `SplitView`.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md)

---

## 1. TabControl

A `TabControl` displays a tab strip and switches between content panes:

```xml
<TabControl>
  <TabItem Header="General">
    <TextBlock Text="General settings" Margin="8" />
  </TabItem>
  <TabItem Header="Advanced">
    <TextBlock Text="Advanced settings" Margin="8" />
  </TabItem>
</TabControl>
```

### Tab placement

```xml
<TabControl TabStripPlacement="Left">
  <TabItem Header="Page 1"><TextBlock Text="Content 1" Margin="8" /></TabItem>
  <TabItem Header="Page 2"><TextBlock Text="Content 2" Margin="8" /></TabItem>
</TabControl>
```

| Value | Description |
|-------|-------------|
| `Top` (default) | Tabs above content |
| `Bottom` | Tabs below content |
| `Left` | Tabs on the left side |
| `Right` | Tabs on the right side |

### Dynamic tabs from a collection

```xml
<TabControl ItemsSource="{Binding Tabs}"
            SelectedItem="{Binding SelectedTab}">
  <TabControl.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{Binding Header}" />
    </DataTemplate>
  </TabControl.ItemTemplate>
  <TabControl.ContentTemplate>
    <DataTemplate DataType="vm:TabItemViewModel">
      <views:TabContentView />
    </DataTemplate>
  </TabControl.ContentTemplate>
</TabControl>
```

```csharp
public partial class ViewModel : ObservableObject
{
    public ObservableCollection<TabItemViewModel> Tabs { get; } = new()
    {
        new("Settings", typeof(SettingsView)),
        new("Account", typeof(AccountView)),
    };

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;
}
```

### Responding to selection

```xml
<TabControl SelectedIndex="{Binding ActiveTabIndex}" />
```

---

## 2. Expander

Collapsible content sections:

```xml
<Expander Header="Advanced Settings">
  <StackPanel Spacing="8" Margin="8">
    <CheckBox Content="Enable diagnostics" />
    <CheckBox Content="Verbose logging" />
  </StackPanel>
</Expander>
```

### Initially expanded

```xml
<Expander Header="Details" IsExpanded="True">
  <TextBlock Text="Visible by default" TextWrapping="Wrap" />
</Expander>
```

### Expand direction

```xml
<Expander ExpandDirection="Up" Header="Expand Upward" VerticalAlignment="Bottom" />
<Expander ExpandDirection="Left" Header="Side Panel" />
```

| Direction | Behavior |
|-----------|----------|
| `Down` (default) | Content appears below header |
| `Up` | Content appears above header |
| `Left` | Content appears to the left |
| `Right` | Content appears to the right |

### Custom header

```xml
<Expander>
  <Expander.Header>
    <StackPanel Orientation="Horizontal" Spacing="8">
      <PathIcon Data="{StaticResource settings_regular}" />
      <TextBlock Text="Settings" />
    </StackPanel>
  </Expander.Header>
</Expander>
```

### Bind IsExpanded

```xml
<Expander IsExpanded="{Binding ShowAdvanced}" />
```

### Content transition

```xml
<Expander Header="Animated Section">
  <Expander.ContentTransition>
    <CrossFade Duration="0:0:0.2" />
  </Expander.ContentTransition>
</Expander>
```

---

## 3. SplitView

Side pane with main content area:

```xml
<SplitView IsPaneOpen="{Binding IsPaneOpen}"
           DisplayMode="CompactInline"
           CompactPaneLength="48"
           OpenPaneLength="200">
  <SplitView.Pane>
    <StackPanel>
      <Button Content="☰" Command="{Binding TogglePaneCommand}" />
      <ListBox ItemsSource="{Binding NavItems}" ... />
    </StackPanel>
  </SplitView.Pane>
  <SplitView.Content>
    <ContentControl Content="{Binding CurrentPage}" />
  </SplitView.Content>
</SplitView>
```

### Display modes

| Mode | Closed state | Open state |
|------|-------------|------------|
| `Inline` | Pane hidden | Pane pushes content |
| `Overlay` | Pane hidden | Pane overlays content |
| `CompactInline` | 48px strip visible | Pane pushes content |
| `CompactOverlay` | 48px strip visible | Pane overlays content |

### Pane placement

```xml
<SplitView PanePlacement="Right" IsPaneOpen="True"
           DisplayMode="Inline" OpenPaneLength="250" />
<SplitView PanePlacement="Top" DisplayMode="Inline" OpenPaneLength="150" />
```

### Toggle pane from ViewModel

```csharp
[ObservableProperty]
private bool _isPaneOpen = true;

[RelayCommand]
private void TogglePane() => IsPaneOpen = !IsPaneOpen;
```

---

## 4. TabStrip

A standalone tab header strip without content switching (useful with custom content switching logic):

```xml
<TabStrip ItemsSource="{Binding Sections}"
          SelectedItem="{Binding SelectedSection}" />
```

---

## 5. ToolTip

```xml
<Button Content="Save" ToolTip.Tip="Save the current file" />
```

| Attached Property | Description |
|-------------------|-------------|
| `ToolTip.Tip` | The tooltip content |
| `ToolTip.Placement` | `Top`, `Bottom`, `Left`, `Right`, `Pointer` |
| `ToolTip.ShowDelay` | Milliseconds before showing (default 400) |
| `ToolTip.HorizontalOffset` | Horizontal offset from placement point |
| `ToolTip.VerticalOffset` | Vertical offset from placement point |

```xml
<Button Content="Delete"
        ToolTip.Tip="Delete this item permanently"
        ToolTip.Placement="Bottom"
        ToolTip.ShowDelay="200">
</Button>
```

---

## Key Takeaways

- `TabControl` — `TabStripPlacement` for orientation; use `ItemsSource` + `ContentTemplate` for dynamic tabs
- `Expander` — `ExpandDirection` controls expansion axis; `ContentTransition` for animation
- `SplitView` — four `DisplayMode` values; `CompactInline` is ideal for nav sidebars
- `TabStrip` — header-only tab strip for custom content switching
- `ToolTip` — `ToolTip.Tip` attached property; configure `Placement` and `ShowDelay`

---

## See Also

- [066V — TabControl, Expander & SplitView (verbose)](066-tabcontrol-expander-splitview-verbose.md)
- [066E — TabControl, Expander & SplitView (examples)](066-tabcontrol-expander-splitview-examples.md)
- [Avalonia Docs: TabControl](https://docs.avaloniaui.net/controls/navigation/tabcontrol)
- [Avalonia Docs: Expander](https://docs.avaloniaui.net/controls/layout/containers/expander)
- [Avalonia Docs: SplitView](https://docs.avaloniaui.net/controls/layout/containers/splitview)
- [016 — Window & Dialog Management](016-window-dialog-management.md)
