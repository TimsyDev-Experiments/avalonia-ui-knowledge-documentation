---
tier: intermediate
topic: controls
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 066E — TabControl, Expander & SplitView (examples)

## Example 1: Settings dialog with TabControl

```xml
<TabControl TabStripPlacement="Left" Width="500" Height="300">
  <TabItem Header="General">
    <StackPanel Spacing="8" Margin="12">
      <CheckBox Content="Enable auto-save" />
      <CheckBox Content="Show line numbers" />
      <ComboBox ItemsSource="{Binding Themes}" SelectedItem="{Binding SelectedTheme}" />
    </StackPanel>
  </TabItem>
  <TabItem Header="Advanced">
    <StackPanel Spacing="8" Margin="12">
      <CheckBox Content="Hardware acceleration" />
      <NumericUpDown Value="{Binding MaxItems}" Minimum="1" Maximum="1000" />
    </StackPanel>
  </TabItem>
  <TabItem Header="About">
    <TextBlock Text="MyApp v1.0" Margin="12" />
  </TabItem>
</TabControl>
```

---

## Example 2: Dynamic tab pages with view models

```csharp
public partial class TabItemViewModel : ObservableObject
{
    [ObservableProperty] private string _header = "";
    [ObservableProperty] private string _content = "";

    public TabItemViewModel(string header, string content)
    {
        _header = header;
        _content = content;
    }
}

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<TabItemViewModel> Tabs { get; } = new()
    {
        new("Settings", "Configure your preferences."),
        new("Account", "Manage your account settings."),
        new("Security", "Set up two-factor authentication."),
    };

    [ObservableProperty]
    private TabItemViewModel? _selectedTab;
}
```

```xml
<TabControl ItemsSource="{Binding Tabs}"
            SelectedItem="{Binding SelectedTab}">
  <TabControl.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{Binding Header}" />
    </DataTemplate>
  </TabControl.ItemTemplate>
  <TabControl.ContentTemplate>
    <DataTemplate>
      <TextBlock Text="{Binding Content}" TextWrapping="Wrap" Margin="12" />
    </DataTemplate>
  </TabControl.ContentTemplate>
</TabControl>
```

---

## Example 3: Collapsible filter panel with Expander

```xml
<StackPanel Spacing="8">
  <Expander Header="Filters" IsExpanded="True">
    <StackPanel Spacing="8" Margin="8">
      <TextBox PlaceholderText="Search by name" />
      <Slider Minimum="0" Maximum="1000"
              Value="{Binding MaxPrice}"
              TickFrequency="100" IsSnapToTickEnabled="True" />
      <ComboBox ItemsSource="{Binding Categories}"
                SelectedItem="{Binding SelectedCategory}" />
    </StackPanel>
  </Expander>
  <Button Content="Apply Filters" Command="{Binding ApplyCommand}" />
</StackPanel>
```

---

## Example 4: Navigation sidebar with SplitView

```csharp
public partial class NavItem : ObservableObject
{
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string _icon = "";
}

public partial class NavViewModel : ObservableObject
{
    public ObservableCollection<NavItem> NavItems { get; } = new()
    {
        new() { Title = "Dashboard", Icon = "dashboard_regular" },
        new() { Title = "Orders", Icon = "cart_regular" },
        new() { Title = "Customers", Icon = "people_regular" },
        new() { Title = "Reports", Icon = "chart_regular" },
    };

    [ObservableProperty] private NavItem? _selectedNavItem;

    [ObservableProperty] private bool _isPaneOpen = true;

    [RelayCommand]
    private void TogglePane() => IsPaneOpen = !IsPaneOpen;
}
```

```xml
<SplitView IsPaneOpen="{Binding IsPaneOpen}"
           DisplayMode="CompactInline"
           CompactPaneLength="48"
           OpenPaneLength="220">
  <SplitView.Pane>
    <DockPanel>
      <Button Content="☰" Command="{Binding TogglePaneCommand}"
              DockPanel.Dock="Top" Height="48" />
      <ListBox ItemsSource="{Binding NavItems}"
               SelectedItem="{Binding SelectedNavItem}"
               Background="Transparent"
               BorderThickness="0">
        <ListBox.ItemTemplate>
          <DataTemplate>
            <StackPanel Orientation="Horizontal" Spacing="12" Height="40"
                        Margin="12,0">
              <PathIcon Data="{Binding Icon}" Width="16" />
              <TextBlock Text="{Binding Title}" VerticalAlignment="Center" />
            </StackPanel>
          </DataTemplate>
        </ListBox.ItemTemplate>
      </ListBox>
    </DockPanel>
  </SplitView.Pane>
  <SplitView.Content>
    <ContentControl Content="{Binding SelectedNavItem.Title}"
                    VerticalAlignment="Center" HorizontalAlignment="Center" />
  </SplitView.Content>
</SplitView>
```

---

## Example 5: ToolTip with rich content

```xml
<Button Content="Help">
  <Button.ToolTip>
    <ToolTip Tip="Click to open the help center"
             Placement="Bottom"
             ShowDelay="100">
      <ToolTip.Styles>
        <Style Selector="ToolTip">
          <Setter Property="Background" Value="{StaticResource SystemAccentColor}" />
          <Setter Property="Foreground" Value="White" />
        </Style>
      </ToolTip.Styles>
    </ToolTip>
  </Button.ToolTip>
</Button>
```

---

## Example 6: Flyout on a button

```xml
<Button Content="Learn more">
  <Button.Flyout>
    <Flyout Placement="Bottom" ShowMode="Transient">
      <StackPanel Spacing="8" Padding="16">
        <TextBlock Text="Did you know?" FontWeight="Bold" FontSize="16" />
        <TextBlock Text="Avalonia can run on Windows, macOS, Linux, iOS, Android, and in the browser."
                   TextWrapping="Wrap" MaxWidth="250" />
        <Button Content="Got it!" />
      </StackPanel>
    </Flyout>
  </Button.Flyout>
</Button>
```

---

## See Also

- [066 — TabControl, Expander & SplitView (core)](066-tabcontrol-expander-splitview.md)
- [066V — TabControl, Expander & SplitView (verbose)](066-tabcontrol-expander-splitview-verbose.md)
- [066Q — TabControl, Expander & SplitView (quiz)](066-tabcontrol-expander-splitview-quiz.md)
