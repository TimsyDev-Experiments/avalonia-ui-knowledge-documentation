---
tier: advanced
topic: tooling
estimated: 20 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 088 — XAML Previewer & Hot Reload — Examples

**Prerequisites:** [088-core](088-xaml-previewer-hot-reload.md)

---

## Example 1: Design-time data with a ViewModel

**MainWindow.axaml:**
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        x:Class="MyApp.Views.MainWindow"
        d:DataContext="{d:DesignInstance vm:MainViewModel, IsDesignTimeCreatable=True}">

  <StackPanel Spacing="8" Margin="16">
    <TextBlock Text="{Binding Title}" FontSize="24" FontWeight="Bold" />
    <ItemsControl Items="{Binding Products}">
      <ItemsControl.ItemTemplate>
        <DataTemplate>
          <Border BorderBrush="Gray" BorderThickness="1" CornerRadius="4" Padding="8" Margin="0,4">
            <StackPanel Orientation="Horizontal" Spacing="12">
              <TextBlock Text="{Binding Name}" FontWeight="SemiBold" />
              <TextBlock Text="{Binding Price, StringFormat='${0:F2}'}" />
            </StackPanel>
          </Border>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>
  </StackPanel>
</Window>
```

**ViewModels/MainViewModel.cs:**
```csharp
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Product Catalog";
    [ObservableProperty] private List<Product> _products = new()
    {
        new() { Name = "Widget A", Price = 19.99m },
        new() { Name = "Widget B", Price = 29.99m },
        new() { Name = "Widget C", Price = 39.99m }
    };
}

public record Product
{
    public string Name { get; init; } = "";
    public decimal Price { get; init; }
}
```

The previewer renders the product list immediately without running the app.

---

## Example 2: Sample data from a static class

**DesignData.cs:**
```csharp
[DesignTime]
public static class DashboardDesignData
{
    public static DashboardViewModel CreateSample() => new()
    {
        UserName = "Jane Doe",
        UnreadCount = 3,
        RecentOrders =
        [
            new() { Id = 1001, Status = "Shipped" },
            new() { Id = 1002, Status = "Processing" }
        ]
    };
}
```

**DashboardView.axaml:**
```xml
<UserControl xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:local="clr-namespace:MyApp.DesignData"
             d:DataContext="{d:DesignInstance Type=local:DashboardDesignData,
                             IsDesignTimeCreatable=True}">
  <StackPanel DataContext="{Binding CreateSample}">
    ...
  </StackPanel>
</UserControl>
```

---

## Example 3: MCP-based preview iteration loop

```text
Prompt to AI assistant:

"Open MainWindow.axaml and show me a screenshot of the current design.
Then change the sidebar background to #F5F5F5, the header font size to 28,
and add 16px padding around the content area. Show me another screenshot.
Repeat until the design matches the reference image."
```

The assistant:
1. Calls `attach-to-file` to connect to the previewer.
2. Calls `screenshot` to capture the current state.
3. Edits the XAML and saves.
4. Calls `screenshot` again to verify.

No builds needed between iterations.

---

## Example 4: Hot Reload — editing styles at runtime

Start the app with F5. While it runs, open `Styles.axaml` and change:

```xml
<!-- Before -->
<Style Selector="Button.base">
  <Setter Property="Background" Value="{StaticResource SystemAccentColor}" />
</Style>

<!-- After (save → immediate update) -->
<Style Selector="Button.base">
  <Setter Property="Background" Value="RoyalBlue" />
  <Setter Property="Foreground" Value="White" />
  <Setter Property="CornerRadius" Value="8" />
</Style>
```

All open windows reflect the new style without restarting.

---

## Example 5: Design-time converter preview

```xml
<Window xmlns:conv="clr-namespace:MyApp.Converters"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:sys="clr-namespace:System;assembly=mscorlib">

  <Window.Resources>
    <conv:StatusToColorConverter x:Key="StatusColor" />
    <sys:String x:Key="DesignStatus">Active</sys:String>
  </Window.Resources>

  <Border d:DataContext="{Binding Source={StaticResource DesignStatus}}"
          Background="{Binding ., Converter={StaticResource StatusColor}}">
    <TextBlock Text="{Binding .}" />
  </Border>
</Window>
```

The previewer shows the actual converted color, confirming the converter works before you run the app.
