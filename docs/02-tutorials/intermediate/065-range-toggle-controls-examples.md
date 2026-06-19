---
tier: intermediate
topic: controls
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 065E — Range & Toggle Controls (examples)

## Example 1: Volume slider with label

```xml
<StackPanel Spacing="8">
  <Slider Minimum="0" Maximum="10"
          Value="{Binding Volume}"
          TickFrequency="1" IsSnapToTickEnabled="True"
          SmallChange="1" LargeChange="2" />
  <TextBlock Text="{Binding Volume, StringFormat='Volume: {0}'}" />
</StackPanel>
```

```csharp
[ObservableProperty]
private int _volume = 5;
```

---

## Example 2: Download progress with indeterminate phase

```xml
<StackPanel Spacing="8">
  <ProgressBar IsIndeterminate="{Binding IsConnecting}"
               Value="{Binding DownloadProgress}"
               ShowProgressText="True"
               ProgressTextFormat="{}{1:F0}%" />
  <Button Content="Download" Command="{Binding DownloadCommand}"
          IsEnabled="{Binding IsNotDownloading}" />
</StackPanel>
```

```csharp
public partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private int _downloadProgress;

    [ObservableProperty]
    private bool _isNotDownloading = true;

    [RelayCommand]
    private async Task DownloadAsync()
    {
        IsConnecting = true;
        IsNotDownloading = false;
        await Task.Delay(2000);
        IsConnecting = false;

        for (int i = 0; i <= 100; i++)
        {
            DownloadProgress = i;
            await Task.Delay(30);
        }
        IsNotDownloading = true;
    }
}
```

---

## Example 3: NumericUpDown for product quantity

```xml
<StackPanel Spacing="8">
  <NumericUpDown Value="{Binding Quantity}" Minimum="1"
                 Maximum="99" Increment="1"
                 ShowButtonSpinner="True"
                 ButtonSpinnerLocation="Right" />
  <TextBlock Text="{Binding TotalPrice, StringFormat='Total: ${0:F2}'}" />
</StackPanel>
```

```csharp
public partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _quantity = 1;

    private decimal _unitPrice = 19.99m;

    public decimal TotalPrice => Quantity * _unitPrice;

    partial void OnQuantityChanged(decimal value) =>
        OnPropertyChanged(nameof(TotalPrice));
}
```

---

## Example 4: Three-state CheckBox select-all

```xml
<StackPanel Spacing="4">
  <CheckBox IsThreeState="True"
            IsChecked="{Binding SelectAllState}"
            Content="Select all" />
  <ItemsControl ItemsSource="{Binding Items}" Margin="24,0,0,0">
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <CheckBox IsChecked="{Binding IsSelected}"
                  Content="{Binding Name}" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</StackPanel>
```

```csharp
public partial class ViewModel : ObservableObject
{
    public ObservableCollection<SelectableItem> Items { get; } = new()
    {
        new("Email"), new("SMS"), new("Push")
    };

    [ObservableProperty]
    private bool? _selectAllState = false;

    partial void OnSelectAllStateChanged(bool? value)
    {
        if (value.HasValue)
            foreach (var item in Items)
                item.IsSelected = value.Value;
    }
}

public partial class SelectableItem : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isSelected;

    public SelectableItem(string name) => _name = name;
}
```

---

## Example 5: RadioButton bound to enum

```csharp
public enum ShippingMethod { Standard, Express, Overnight }

public partial class OrderViewModel : ObservableObject
{
    [ObservableProperty]
    private ShippingMethod _selectedShipping = ShippingMethod.Standard;
}
```

```xml
<StackPanel Spacing="4">
  <RadioButton Content="Standard (5-7 days)"
               IsChecked="{Binding SelectedShipping,
                   Converter={StaticResource EnumToBoolConverter},
                   ConverterParameter={x:Static vm:ShippingMethod.Standard}}" />
  <RadioButton Content="Express (2-3 days)"
               IsChecked="{Binding SelectedShipping,
                   Converter={StaticResource EnumToBoolConverter},
                   ConverterParameter={x:Static vm:ShippingMethod.Express}}" />
  <RadioButton Content="Overnight"
               IsChecked="{Binding SelectedShipping,
                   Converter={StaticResource EnumToBoolConverter},
                   ConverterParameter={x:Static vm:ShippingMethod.Overnight}}" />
</StackPanel>
```

---

## Example 6: ToggleSwitch settings panel

```xml
<StackPanel Spacing="12">
  <ToggleSwitch IsChecked="{Binding IsDarkMode}"
                OnContent="Dark Mode" OffContent="Light Mode" />
  <ToggleSwitch IsChecked="{Binding NotificationsEnabled}"
                OnContent="Notifications On" OffContent="Notifications Off" />
  <ToggleSwitch IsChecked="{Binding AutoUpdate}"
                OnContent="" OffContent="" />
</StackPanel>
```

```csharp
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _isDarkMode;
    [ObservableProperty] private bool _notificationsEnabled = true;
    [ObservableProperty] private bool _autoUpdate = true;
}
```

---

## Example 7: NumericUpDown with currency prefix

```xml
<NumericUpDown Value="{Binding Price}" Increment="0.01"
               FormatString="C2" Minimum="0">
  <NumericUpDown.InnerLeftContent>
    <TextBlock Text="$" VerticalAlignment="Center" Margin="4,0" />
  </NumericUpDown.InnerLeftContent>
</NumericUpDown>
```

---

## See Also

- [065 — Range & Toggle Controls (core)](065-range-toggle-controls.md)
- [065V — Range & Toggle Controls (verbose)](065-range-toggle-controls-verbose.md)
- [065Q — Range & Toggle Controls (quiz)](065-range-toggle-controls-quiz.md)
