---
tier: advanced
topic: layout
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 083 — Container Queries & Responsive Layout — Examples

**Prerequisites:** [083-core](083-container-queries-responsive-layout.md)

---

## Example 1: Sidebar-responsive app shell

**Goal:** A two-column layout that collapses the sidebar when the main container is narrow.

**App.axaml (Window-level styles):**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="clr-namespace:App.Views"
        x:Class="App.Views.MainWindow"
        Title="Responsive Shell"
        Width="1024" Height="768">
  <DockPanel>
    <!-- Header -->
    <Border DockPanel.Dock="Top" Height="48"
            Background="{StaticResource PrimaryBrush}"
            Padding="16">
      <TextBlock Text="My App" Foreground="White"
                 VerticalAlignment="Center" />
    </Border>

    <!-- Responsive content area -->
    <Border Container.Name="content-area"
            Container.Sizing="Width">
      <DockPanel>
        <!-- Sidebar: hidden when container <= 640 -->
        <Border DockPanel.Dock="Left" x:Name="sidebar"
                Width="250" Background="#F9FAFB">
          <ListBox ItemsSource="{Binding MenuItems}" />
        </Border>
        <!-- Main content -->
        <ContentControl Content="{Binding CurrentPage}" Padding="16" />
      </DockPanel>
    </Border>
  </DockPanel>

  <Window.Styles>
    <ContainerQuery Name="content-area" Query="max-width:640">
      <Style Selector="Border#sidebar">
        <Setter Property="IsVisible" Value="False" />
      </Style>
      <Style Selector="ContentControl">
        <Setter Property="Padding" Value="8" />
      </Style>
    </ContainerQuery>
  </Window.Styles>
</Window>
```

---

## Example 2: Adaptive card grid

**Goal:** A `UniformGrid` that switches columns based on the container's width using multiple breakpoints.

```xml
<ScrollViewer>
  <Border Padding="16"
          Container.Name="grid-area"
          Container.Sizing="Width">
    <Border.Styles>
      <ContainerQuery Name="grid-area" Query="max-width:400">
        <Style Selector="UniformGrid#card-grid">
          <Setter Property="Columns" Value="1" />
        </Style>
      </ContainerQuery>
      <ContainerQuery Name="grid-area" Query="min-width:400">
        <Style Selector="UniformGrid#card-grid">
          <Setter Property="Columns" Value="2" />
        </Style>
      </ContainerQuery>
      <ContainerQuery Name="grid-area" Query="min-width:800">
        <Style Selector="UniformGrid#card-grid">
          <Setter Property="Columns" Value="3" />
        </Style>
      </ContainerQuery>
    </Border.Styles>

    <UniformGrid x:Name="card-grid" Columns="3" Rows="*">
      <Border Classes="card" Background="White" BorderThickness="1"
              BorderBrush="#E5E7EB" CornerRadius="8" Padding="16">
        <StackPanel Spacing="8">
          <TextBlock Text="Card 1" FontSize="18" FontWeight="Bold" />
          <TextBlock Text="Description" TextWrapping="Wrap" />
        </StackPanel>
      </Border>
      <!-- Repeat for cards 2..n -->
    </UniformGrid>
  </Border>
</ScrollViewer>
```

---

## Example 3: Responsive toolbar with container queries

**Goal:** A toolbar that changes orientation and hides labels when narrow.

```xml
<Border Padding="8" Background="#F3F4F6"
        Container.Name="toolbar"
        Container.Sizing="Width">
  <Border.Styles>
    <ContainerQuery Name="toolbar" Query="max-width:400">
      <Style Selector="StackPanel#toolbar-panel">
        <Setter Property="Orientation" Value="Vertical" />
      </Style>
      <Style Selector="StackPanel#toolbar-panel TextBlock.label">
        <Setter Property="IsVisible" Value="False" />
      </Style>
    </ContainerQuery>
  </Border.Styles>

  <StackPanel x:Name="toolbar-panel"
              Orientation="Horizontal" Spacing="8">
    <Button Content="Save">
      <StackPanel Orientation="Horizontal" Spacing="4">
        <TextBlock Text="{StaticResource SaveIcon}" />
        <TextBlock Classes="label" Text="Save" />
      </StackPanel>
    </Button>
    <Button Content="Open">
      <StackPanel Orientation="Horizontal" Spacing="4">
        <TextBlock Text="{StaticResource OpenIcon}" />
        <TextBlock Classes="label" Text="Open" />
      </StackPanel>
    </Button>
    <Button Content="Delete">
      <StackPanel Orientation="Horizontal" Spacing="4">
        <TextBlock Text="{StaticResource DeleteIcon}" />
        <TextBlock Classes="label" Text="Delete" />
      </StackPanel>
    </Button>
  </StackPanel>
</Border>
```

---

## Example 4: ItemsRepeater with UniformGridLayout

**Goal:** A card list using `ItemsRepeater` and `UniformGridLayout` that adapts column count automatically — no manual breakpoints needed.

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:App.ViewModels"
             x:DataType="vm:CatalogViewModel">
  <ScrollViewer>
    <ItemsRepeater ItemsSource="{Binding Products}">
      <ItemsRepeater.Layout>
        <UniformGridLayout MinItemWidth="260"
                           MinItemHeight="320"
                           MinColumnSpacing="16"
                           MinRowSpacing="16" />
      </ItemsRepeater.Layout>
      <ItemsRepeater.ItemTemplate>
        <DataTemplate>
          <Border CornerRadius="8" Padding="16"
                  Background="White"
                  BorderThickness="1" BorderBrush="#E5E7EB">
            <StackPanel Spacing="8">
              <Border Height="160" Background="#E5E7EB"
                      CornerRadius="4" />
              <TextBlock Text="{Binding Name}"
                         FontSize="18" FontWeight="Bold" />
              <TextBlock Text="{Binding Price, StringFormat='C'}"
                         FontSize="16" Foreground="#059669" />
              <Button Content="Add to Cart" HorizontalAlignment="Stretch" />
            </StackPanel>
          </Border>
        </DataTemplate>
      </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
  </ScrollViewer>
</UserControl>
```

---

## Example 5: Window-size breakpoint view model

**Goal:** A view model that exposes `IsCompact`, `IsMedium`, and `IsWide` properties for programmatic layout branching.

```csharp
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty] private bool _isCompact;  // < 640
    [ObservableProperty] private bool _isMedium;   // 640–1023
    [ObservableProperty] private bool _isWide;     // >= 1024

    public void ApplyWindowWidth(double width)
    {
        IsWide = width >= 1024;
        IsMedium = width >= 640 && width < 1024;
        IsCompact = width < 640;
    }
}
```

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (DataContext is DashboardViewModel vm)
            vm.ApplyWindowWidth(e.NewSize.Width);
    }
}
```

```xml
<Window.Styles>
  <Style Selector="Grid.dashboard">
    <Setter Property="ColumnDefinitions" Value="280,*" />
  </Style>
  <Style Selector="Grid.dashboard.compact">
    <Setter Property="ColumnDefinitions" Value="*" />
  </Style>
</Window.Styles>

<Grid Classes="dashboard"
      IsVisible="{Binding IsWide, Converter={StaticResource BoolToVis}}">
  <Border Grid.Column="0">
    <ListBox ItemsSource="{Binding MenuItems}" />
  </Border>
  <ContentControl Grid.Column="1" Content="{Binding CurrentContent}" />
</Grid>

<Grid Classes="dashboard compact"
      IsVisible="{Binding IsCompact}">
  <ContentControl Content="{Binding CurrentContent}" />
</Grid>
```

---

## Example 6: OnFormFactor and OnPlatform in XAML

**Goal:** A toolbar that adapts once at startup for device and OS.

```xml
<StackPanel Orientation="Horizontal" Spacing="8">
  <TextBlock Text="File" />
  <TextBlock Text="Edit" />
  <TextBlock Text="View" />

  <!-- macOS: 12pt, Windows: 14pt, others fall through -->
  <TextBlock Text="Help"
             FontSize="{OnPlatform macOS=12, Windows=14, Default=13}" />

  <!-- Mobile: hide help; Desktop: show help -->
  <TextBlock Text="Help"
             IsVisible="{OnFormFactor Desktop=True,
                                      Mobile=False}" />
</StackPanel>
```

---

## Example 7: Nested container queries

**Goal:** An outer container sets padding, and an inner container changes font size independently.

```xml
<Border Padding="24"
        Container.Name="outer"
        Container.Sizing="Width">
  <Border.Styles>
    <ContainerQuery Name="outer" Query="min-width:500">
      <Style Selector="StackPanel#content">
        <Setter Property="Spacing" Value="16" />
      </Style>
    </ContainerQuery>
  </Border.Styles>

  <StackPanel x:Name="content" Spacing="8">
    <Border Container.Name="inner"
            Container.Sizing="Width">
      <Border.Styles>
        <ContainerQuery Name="inner" Query="min-width:300">
          <Style Selector="TextBlock.body">
            <Setter Property="FontSize" Value="18" />
          </Style>
        </ContainerQuery>
      </Border.Styles>
      <TextBlock Classes="body" Text="This text grows when inner container > 300px" />
    </Border>
    <TextBlock Classes="body" Text="This text is not affected by inner query" />
  </StackPanel>
</Border>
```

---

## Example 8: WrapPanel for tag overflow

```xml
<StackPanel Spacing="4">
  <TextBlock Text="Tags:" FontWeight="Bold" />
  <WrapPanel ItemSpacing="4" LineSpacing="4">
    <Button Classes="tag" Content="Avalonia" />
    <Button Classes="tag" Content="Responsive" />
    <Button Classes="tag" Content="Layout" />
    <Button Classes="tag" Content="Container Queries" />
    <Button Classes="tag" Content="XAML" />
    <Button Classes="tag" Content=".NET" />
  </WrapPanel>
</StackPanel>
```

---

## Example 9: ResponsiveLayout (CommunityToolkit)

```xml
xmlns:toolkit="using:Avalonia.CommunityToolkit.Controls"

<toolkit:ResponsiveLayout>
  <toolkit:ResponsiveLayout.SmallTemplate>
    <StackPanel Spacing="8">
      <TextBlock Text="Mobile-style layout" FontSize="14" />
      <ContentControl Content="{Binding SmallBody}" />
    </StackPanel>
  </toolkit:ResponsiveLayout.SmallTemplate>
  <toolkit:ResponsiveLayout.LargeTemplate>
    <Grid ColumnDefinitions="250,*" ColumnSpacing="16">
      <ContentControl Content="{Binding Sidebar}" />
      <ContentControl Content="{Binding Body}" />
    </Grid>
  </toolkit:ResponsiveLayout.LargeTemplate>
</toolkit:ResponsiveLayout>
```

---

## Key Takeaways

- Container queries handle any style-settable property — positioning, visibility, font size, padding
- `UniformGridLayout` with `MinItemWidth` offers automatic, code-free column adaptation
- Multiple named containers in the same tree are tracked independently
- Breakpoint view models provide programmatic branching for complex responsive logic
- `OnFormFactor` / `OnPlatform` handle startup-only adaptation for device type and OS
- `WrapPanel` and `WrapLayout` are simplest for flowing content that wraps naturally
