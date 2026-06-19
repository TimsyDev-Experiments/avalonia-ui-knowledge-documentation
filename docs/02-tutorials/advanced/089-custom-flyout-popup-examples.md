---
tier: advanced
topic: controls
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 089 — Custom Flyout & Popup — Examples

**Prerequisites:** [089-core](089-custom-flyout-popup.md)

---

## Example 1: Confirmation dialog flyout

```csharp
public class ConfirmDeleteFlyout : FlyoutBase
{
    public event EventHandler? Confirmed;
    public event EventHandler? Cancelled;

    protected override Control CreatePresenter()
    {
        var confirmBtn = new Button { Content = "Delete", Background = Brushes.Red, Foreground = Brushes.White };
        var cancelBtn = new Button { Content = "Cancel" };

        confirmBtn.Click += (s, e) => { Confirmed?.Invoke(this, EventArgs.Empty); Hide(); };
        cancelBtn.Click += (s, e) => { Cancelled?.Invoke(this, EventArgs.Empty); Hide(); };

        return new FlyoutPresenter
        {
            Content = new StackPanel
            {
                Spacing = 8, Padding = 16,
                Children =
                {
                    new TextBlock { Text = "Delete this item permanently?", FontWeight = FontWeight.SemiBold },
                    new TextBlock { Text = "This action cannot be undone." },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = LayoutAlignment.Right,
                        Children = { cancelBtn, confirmBtn } }
                }
            }
        };
    }
}
```

Usage:

```csharp
var flyout = new ConfirmDeleteFlyout();
flyout.Confirmed += (s, e) => DeleteItemCommand.Execute(null);
flyout.ShowAt(deleteButton);
```

---

## Example 2: MVVM-bound search dropdown with Popup

```xml
<StackPanel>
  <TextBox x:Name="SearchBox" Text="{Binding SearchText}"
           Watermark="Search..." />
  <Popup IsOpen="{Binding IsSearchOpen}"
         PlacementTarget="{Binding #SearchBox}"
         Placement="Bottom"
         MinWidth="{Binding #SearchBox.Bounds.Width}"
         IsLightDismissEnabled="True">
    <Border BorderBrush="Gray" BorderThickness="1" Background="White">
      <ListBox Items="{Binding SearchResults}"
               SelectedItem="{Binding SelectedResult}">
        <ListBox.ItemTemplate>
          <DataTemplate>
            <TextBlock Text="{Binding DisplayName}" Padding="8,4" />
          </DataTemplate>
        </ListBox.ItemTemplate>
      </ListBox>
    </Border>
  </Popup>
</StackPanel>
```

```csharp
[ObservableProperty] private string _searchText = "";
[ObservableProperty] private bool _isSearchOpen;
[ObservableProperty] private SearchResult? _selectedResult;
[ObservableProperty] private List<SearchResult> _searchResults = [];

partial void OnSearchTextChanged(string value)
{
    SearchResults = PerformSearch(value).ToList();
    IsSearchOpen = SearchResults.Count > 0;
}
```

---

## Example 3: Color picker with placement control

```xml
<Button x:Name="ColorButton" Content="Pick Color" Click="OnPickColor" />
```

```csharp
private void OnPickColor(object? sender, RoutedEventArgs e)
{
    var popup = new Popup
    {
        PlacementTarget = ColorButton,
        Placement = PlacementMode.AnchorAndGravity,
        PlacementAnchor = PopupAnchor.TopRight,
        PlacementGravity = PopupGravity.BottomRight,
        IsLightDismissEnabled = true
    };

    var palette = new WrapPanel { Spacing = 4 };
    foreach (var color in new[] { Colors.Red, Colors.Green, Colors.Blue, Colors.Orange, Colors.Purple })
    {
        var swatch = new Border
        {
            Width = 24, Height = 24,
            Background = new SolidColorBrush(color),
            CornerRadius = new CornerRadius(4),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        swatch.PointerPressed += (s, e) =>
        {
            ColorButton.Background = new SolidColorBrush(color);
            popup.IsOpen = false;
        };
        palette.Children.Add(swatch);
    }

    popup.Child = new Border
    {
        Background = Brushes.White,
        Padding = new Thickness(8),
        CornerRadius = new CornerRadius(6),
        BoxShadow = "0 4 12 0 #60000000",
        Child = palette
    };

    popup.IsOpen = true;
}
```

---

## Example 4: Custom tooltip replacement via Popup

```xml
<TextBlock x:Name="HelpIcon" Text="? "
           PointerEntered="OnHelpPointerEntered"
           PointerExited="OnHelpPointerExited" />

<Popup x:Name="HelpPopup" Placement="Right" IsLightDismissEnabled="True">
  <Border Background="LightYellow" Padding="12" CornerRadius="6"
          BorderBrush="Goldenrod" BorderThickness="1" MaxWidth="250">
    <TextBlock Text="Click Save to persist all pending changes." TextWrapping="Wrap" />
  </Border>
</Popup>
```

```csharp
private void OnHelpPointerEntered(object? s, PointerEventArgs e)
    => HelpPopup.IsOpen = true;

private void OnHelpPointerExited(object? s, PointerEventArgs e)
    => HelpPopup.IsOpen = false;
```

---

## Example 5: Nested menu flyout

```csharp
public class NestedMenuFlyout : FlyoutBase
{
    protected override Control CreatePresenter()
    {
        var menu = new StackPanel { Spacing = 2 };
        foreach (var item in new[] { "Open", "Save", "Export" })
        {
            var btn = new Button { Content = item, Background = Brushes.Transparent,
                                   HorizontalAlignment = LayoutAlignment.Stretch,
                                   HorizontalContentAlignment = LayoutAlignment.Left };
            btn.Click += (s, e) => Hide();
            menu.Children.Add(btn);
        }
        return new FlyoutPresenter { Content = menu };
    }
}
```
