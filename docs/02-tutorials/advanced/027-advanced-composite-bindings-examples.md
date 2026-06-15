---
tier: advanced
topic: bindings
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 027-advanced-composite-bindings.md
---

# 027X — Advanced Composite Bindings: Real-World Examples

## Scenario 1: Dashboard Card with MultiBinding + Indexer Binding

### Goal

Display a composed dashboard card that shows a user's display name (combined from first/last), their current permission role (looked up from a settings dictionary), and their online status — all with compiled bindings and no code-behind glue.

### ViewModel

```csharp
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DashboardApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName = "Jane";

    [ObservableProperty]
    private string _lastName = "Doe";

    [ObservableProperty]
    private string _permissionKey = "editor";

    [ObservableProperty]
    private bool _isOnline = true;

    public SettingsCollection Settings { get; } = new();

    public string StatusText => IsOnline ? "Online" : "Offline";
}

public class SettingsCollection
{
    private readonly Dictionary<string, string> _roles = new()
    {
        ["admin"] = "Administrator",
        ["editor"] = "Content Editor",
        ["viewer"] = "Read Only"
    };

    public string this[string key] =>
        _roles.TryGetValue(key, out var role) ? role : "Unknown";
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DashboardApp.ViewModels"
             xmlns:conv="using:DashboardApp.Converters"
             x:Class="DashboardApp.Views.DashboardCardView"
             x:DataType="vm:DashboardViewModel">

  <UserControl.Resources>
    <conv:FullNameConverter x:Key="FullNameConv" />
    <conv:BoolToColorConverter x:Key="StatusColorConv" />
  </UserControl.Resources>

  <Border Background="{DynamicResource SurfaceBrush}"
          CornerRadius="8" Padding="16"
          BoxShadow="0 2 8 0 #15000000">
    <Grid RowDefinitions="Auto,Auto,Auto" ColumnDefinitions="*,Auto"
          Spacing="{StaticResource Space3}">

      <!-- MultiBinding: FirstName + LastName → FullNameConverter -->
      <TextBlock Grid.Row="0" Grid.Column="0"
                 FontSize="{StaticResource FontSizeLg}"
                 FontWeight="SemiBold">
        <TextBlock.Text>
          <MultiBinding Converter="{StaticResource FullNameConv}"
                        x:DataType="vm:DashboardViewModel">
            <Binding Path="FirstName" />
            <Binding Path="LastName" />
          </MultiBinding>
        </TextBlock.Text>
      </TextBlock>

      <!-- Status indicator with BoolToColorConverter -->
      <Ellipse Grid.Row="0" Grid.Column="1"
               Width="12" Height="12"
               Fill="{Binding IsOnline,
                      Converter={StaticResource StatusColorConv}}" />
      <TextBlock Grid.Row="0" Grid.Column="2"
                 Text="{Binding StatusText}"
                 VerticalAlignment="Center" />

      <!-- Indexer binding: permissionKey → Settings["permissionKey"] -->
      <TextBlock Grid.Row="1" Grid.Column="0" Grid.ColumnSpan="2"
                 Text="{Binding Settings[PermissionKey]}"
                 Foreground="{DynamicResource TextSecondaryBrush}"
                 FontSize="{StaticResource FontSizeSm}" />
    </Grid>
  </Border>
</UserControl>
```

### Converters

```csharp
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DashboardApp.Converters;

public class FullNameConverter : IMultiValueConverter
{
    public object? Convert(IReadOnlyList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2
            && values[0] is string first
            && values[1] is string last
            && values[0] != Avalonia.Data.BindingOperations.DoNothing
            && values[1] != Avalonia.Data.BindingOperations.DoNothing)
        {
            return $"{first} {last}";
        }
        return null;
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
            return isOnline ? new SolidColorBrush(Colors.MediumSeaGreen)
                            : new SolidColorBrush(Colors.Gray);
        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

### How It Works

1. **MultiBinding** collects `FirstName` and `LastName` and sends them to `FullNameConverter`. The converter checks for `DoNothing` sentinels to avoid partial-update artifacts.
2. **Indexer binding** `{Binding Settings[PermissionKey]}` resolves `SettingsCollection.this[string]` at runtime. Compiled bindings validate `Settings` as a property of `DashboardViewModel` and `PermissionKey` as a string property; the indexer body runs on each access.
3. **`BoolToColorConverter`** translates `IsOnline` to a brush directly — no conditional in the ViewModel.
4. When `PermissionKey` changes, the indexer binding re-evaluates because `PermissionKey` raises `PropertyChanged`. The `SettingsCollection` indexer itself is not observable, so the key change triggers re-read.

### Design Decisions & Edge Cases

- **UnsetValue handling**: `FullNameConverter` checks `values[0] != DoNothing` to guard against partial binding failure (e.g., if `FirstName` property is removed from the ViewModel).
- **Indexer vs. computed property**: The role lookup stays in `SettingsCollection` (not a property on the ViewModel) because the role map is a reusable cross-cutting concern. If the role map changes at runtime, add `INotifyPropertyChanged` to the indexer.
- **Compiled binding on indexers**: Requires `x:DataType` on the root element (for `Settings`) and the index type must be statically known (`string` key, `string` return). The indexer parameter (`PermissionKey`) is a binding path, not a literal, so it updates reactively.

---

## Scenario 2: Priority Contact Resolver with Fallback Chain

### Goal

Display a contact's preferred communication handle with a fallback chain: phone number → email → username → a literal fallback. Use `PriorityBinding` so the first non-empty value wins.

### ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace ContactApp.ViewModels;

public partial class ContactViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _phoneNumber;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _userName;

    [ObservableProperty]
    private bool _isPriorityActive;
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:ContactApp.ViewModels"
             x:Class="ContactApp.Views.ContactCardView"
             x:DataType="vm:ContactViewModel">

  <Border CornerRadius="8" Padding="16"
          Background="{DynamicResource SurfaceBrush}">
    <Grid RowDefinitions="Auto,Auto" ColumnDefinitions="*,Auto"
          Spacing="8">

      <!-- PriorityBinding: phone → email → username → fallback -->
      <TextBlock Grid.Row="0" Grid.Column="0"
                 FontSize="{StaticResource FontSizeBase}"
                 FontWeight="SemiBold">
        <TextBlock.Text>
          <PriorityBinding x:DataType="vm:ContactViewModel">
            <Binding Path="PhoneNumber" />
            <Binding Path="Email" />
            <Binding Path="UserName" />
            <Binding Source="No contact info" />
          </PriorityBinding>
        </TextBlock.Text>
      </TextBlock>

      <!-- Toggle to simulate phone number appearing -->
      <Button Grid.Row="0" Grid.Column="1"
              Content="{Binding IsPriorityActive,
                       Converter={StaticResource BoolToToggleLabel}}"
              Command="{Binding TogglePriorityCommand}" />

      <!-- Secondary info: always shows email if available -->
      <TextBlock Grid.Row="1" Grid.Column="0"
                 Text="{Binding Email}"
                 Foreground="{DynamicResource TextSecondaryBrush}"
                 FontSize="{StaticResource FontSizeSm}" />
    </Grid>
  </Border>
</UserControl>
```

### How It Works

1. `PriorityBinding` evaluates child bindings in order. The first one that returns a non-null, non-`UnsetValue` result wins.
2. If `PhoneNumber` is `null`, the engine moves to `Email`. If `Email` is also `null`, it tries `UserName`. If all are null, the literal fallback `"No contact info"` via `<Binding Source="..." />` is used.
3. When `PhoneNumber` changes from null to a value (e.g., user toggles priority active), the `PriorityBinding` re-evaluates from the start — `PhoneNumber` now wins and the display updates.
4. The inline `BoolToToggleLabel` converter (not shown, assume registered in resources) maps `IsPriorityActive` to `"Add Phone"` / `"Remove Phone"` button text.

### Design Decisions & Edge Cases

- **PriorityBinding does not switch back dynamically**: If `PhoneNumber` is set and later cleared to null, the binding re-evaluates from position 1 (`Email`). It does NOT remember that `PhoneNumber` previously won. This is correct for a contact card — if the user removes their phone, show the next best option.
- **Literal fallback via `<Binding Source="..." />`**: The last entry has no `Path` — `Source` provides the literal. This is the only way to include a non-binding fallback in `PriorityBinding`. Avalonia 12 does not support `FallbackValue` on individual child bindings in a `PriorityBinding`.
- **Edge case — all properties null at startup**: The fallback literal displays immediately. No flicker from intermediate empty states.
- **Edge case — empty string vs. null**: `PriorityBinding` treats empty strings as "resolved" (they are non-null). If you need empty-string-as-fallback, use a converter that maps empty string to `UnsetValue` or `DoNothing`.

### Comparison

| Aspect | Scenario 1: Dashboard Card | Scenario 2: Contact Resolver |
|---|---|---|
| Binding technique | MultiBinding + Indexer | PriorityBinding + literal fallback |
| Converter needed | IMultiValueConverter (name join) + IValueConverter (bool→color) | Optional (button label converter) |
| Change propagation | Any child binding change re-runs converter | Only active binding forwards changes |
| Use case | Composing data from multiple sources | Fallback chain from highest to lowest priority |
| Fallback strategy | Null check inside converter | Declarative via PriorityBinding chain |
| Performance | Converter runs on any child change (multi-source) | Only the active binding is observed |

## See Also

- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [027V — Advanced Composite Bindings (verbose companion)](027-advanced-composite-bindings-verbose.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [004 — Value Converters](../basics/004-value-converters.md) — converter fundamentals extended by composite bindings
- [Avalonia Docs: Data Binding Syntax](https://docs.avaloniaui.net/docs/data-binding/data-binding-syntax)
