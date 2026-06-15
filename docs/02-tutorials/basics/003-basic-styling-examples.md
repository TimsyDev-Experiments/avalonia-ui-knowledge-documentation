---
tier: basics
topic: styling
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 003-basic-styling.md
---

# 003X — Basic Styling: Real-World Examples

**What you'll build:** A themed card grid with interactive hover states and a validation-aware form that styles inputs based on their error state — two scenarios that combine selectors, pseudo-classes, and style classes in practical layouts.

**Prerequisites:** [003 — Basic Styling](003-basic-styling.md). The [verbose companion](003-basic-styling-verbose.md) covers selector specificity, the cascade order, and pseudo-class mechanics in depth.

---

## Example 1: Interactive Card Grid with Hover and Selection States

**Goal:** Display a grid of product cards that change appearance on hover and when selected, using style classes and pseudo-class selectors — no code-behind.

Cards are a common UI pattern. Each card needs a resting state, a hover state (elevation, border highlight), and a selected state (check mark overlay, accent border). Styling these via selectors keeps the ViewModel clean — it only tracks the selected item.

### ViewModel

```csharp
// ViewModels/CatalogViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyApp.Models;

namespace MyApp.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    [ObservableProperty]
    private Product? _selectedProduct;

    public ObservableCollection<Product> Products { get; } = new()
    {
        new() { Name = "Wireless Mouse", Price = 29.99m, Category = "Electronics" },
        new() { Name = "Mechanical Keyboard", Price = 89.99m, Category = "Electronics" },
        new() { Name = "USB-C Hub", Price = 34.99m, Category = "Accessories" },
        new() { Name = "Desk Lamp", Price = 49.99m, Category = "Furniture" },
    };

    [RelayCommand]
    private void SelectProduct(Product product)
    {
        SelectedProduct = product;
    }
}
```

### Model

```csharp
// Models/Product.cs
namespace MyApp.Models;

public class Product
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}
```

### View with styles

```xml
<!-- Views/CatalogView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             xmlns:models="using:MyApp.Models"
             x:Class="MyApp.Views.CatalogView"
             x:DataType="vm:CatalogViewModel">

  <UserControl.Styles>
    <!-- Card base style -->
    <Style Selector="Border.card">
      <Setter Property="Background" Value="{DynamicResource CardBackground}" />
      <Setter Property="BorderBrush" Value="Transparent" />
      <Setter Property="BorderThickness" Value="2" />
      <Setter Property="CornerRadius" Value="8" />
      <Setter Property="Padding" Value="12" />
      <Setter Property="Margin" Value="4" />
      <Setter Property="MinWidth" Value="180" />
    </Style>

    <!-- Card hover state -->
    <Style Selector="Border.card /pointerover/">
      <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentColor}" />
      <Setter Property="BoxShadow" Value="0 2px 8px rgba(0,0,0,0.15)" />
    </Style>

    <!-- Card selected state -->
    <Style Selector="Border.card.selected">
      <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentColor}" />
      <Setter Property="Background" Value="{DynamicResource SystemAccentColorDark1}" />
    </Style>

    <!-- Category label style -->
    <Style Selector="TextBlock.category">
      <Setter Property="FontSize" Value="11" />
      <Setter Property="Foreground" Value="Gray" />
      <Setter Property="Margin" Value="0,4,0,0" />
    </Style>

    <!-- Price style -->
    <Style Selector="TextBlock.price">
      <Setter Property="FontSize" Value="16" />
      <Setter Property="FontWeight" Value="Bold" />
      <Setter Property="Foreground" Value="{DynamicResource SystemAccentColor}" />
    </Style>
  </UserControl.Styles>

  <ScrollViewer>
    <ItemsControl ItemsSource="{Binding Products}">
      <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
          <WrapPanel />
        </ItemsPanelTemplate>
      </ItemsControl.ItemsPanel>

      <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="models:Product">
          <Border Classes="card"
                  PointerPressed="OnCardPressed">
            <StackPanel Gap="2">
              <TextBlock Text="{Binding Name}"
                         FontWeight="SemiBold" FontSize="14" />
              <TextBlock Text="{Binding Category}"
                         Classes="category" />
              <TextBlock Text="{Binding Price, StringFormat='{0:C}'}"
                         Classes="price" />
            </StackPanel>
          </Border>
        </DataTemplate>
      </ItemsControl.ItemTemplate>
    </ItemsControl>
  </ScrollViewer>
</UserControl>
```

```csharp
// Views/CatalogView.axaml.cs
using MyApp.Models;

namespace MyApp.Views;

public partial class CatalogView : UserControl
{
    public CatalogView()
    {
        InitializeComponent();
    }

    private void OnCardPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Border { DataContext: Product product } border)
        {
            var vm = DataContext as ViewModels.CatalogViewModel;
            vm?.SelectProductCommand.Execute(product);

            // Toggle 'selected' class on all cards
            var parent = border.Parent as StackPanel;
            if (parent?.Parent is ItemsControl itemsControl)
            {
                foreach (var child in itemsControl.GetRealizedContainers())
                {
                    if (child is ContentPresenter { Child: Border b })
                    {
                        b.Classes.Remove("selected");
                    }
                }
            }
            border.Classes.Add("selected");
        }
    }
}
```

### How it works

1. Each product card is a `Border` with `Classes="card"`. The `Border.card` selector applies base dimensions, background, and corner radius.
2. When the mouse hovers over a card, the `/pointerover/` pseudo-class is activated by Avalonia. The `Border.card /pointerover/` selector changes the border color and adds a box shadow.
3. When a card is pressed, the code-behind adds `Classes="selected"` to the pressed `Border` and removes it from all others. The `Border.card.selected` selector changes the background and border.
4. The `price` and `category` style classes are applied via `Classes="..."` on individual `TextBlock` elements, separating visual concerns from the data template structure.
5. All colors reference `{DynamicResource ...}` — the card theme adapts when the user switches between light and dark modes.

### Design decisions and edge cases

- **Code-behind for class toggling:** The styling is pure XAML, but adding/removing the `selected` class requires imperative code because selection is a container-scoped concept (only one card selected at a time). A `ValueConverter` + `IsSelected` property on the model is a pure-XAML alternative but couples the model to UI state.
- **`DynamicResource` for theme colors:** The `CardBackground` resource must be defined in `App.axaml` or theme dictionaries. If it is missing, the `DynamicResource` silently resolves to `null` — the card gets a transparent background. Define fallback values.
- **PointerPressed vs Command:** The `PointerPressed` event handler calls the ViewModel command *and* manages visual state. An alternative is to bind `Border.Command` (if using `Button` instead of `Border`), but `Button` has its own chrome that conflicts with the card appearance. A custom `CardButton` control would be the cleanest long-term solution.

---

## Example 2: Validation-Aware Form Styling

**Goal:** Build a registration form where input fields show a red border and error message when validation fails, using pseudo-classes to react to validation state without code-behind.

Avalonia controls do not have a built-in `:invalid` pseudo-class. The ViewModel exposes validation state via properties, and the view uses style triggers based on those properties. This example shows the pattern with `ObservableValidator`.

### ViewModel using ObservableValidator

```csharp
// ViewModels/RegistrationViewModel.cs
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class RegistrationViewModel : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [NotifyDataErrorInfo]
    private string _username = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [NotifyDataErrorInfo]
    private string _email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [NotifyDataErrorInfo]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _hasErrors;

    partial void OnHasErrorsChanged(bool value)
    {
        RegisterCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private void Register()
    {
        // Submit logic
    }

    private bool CanRegister() => !HasErrors
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Email)
        && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand]
    private void ValidateAll()
    {
        ValidateAllProperties();
        HasErrors = HasErrors;
    }
}
```

### View with validation styling

```xml
<!-- Views/RegistrationView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.RegistrationView"
             x:DataType="vm:RegistrationViewModel">

  <UserControl.Styles>
    <!-- Input field base -->
    <Style Selector="TextBox.input">
      <Setter Property="BorderThickness" Value="1" />
      <Setter Property="BorderBrush" Value="#ccc" />
      <Setter Property="CornerRadius" Value="4" />
      <Setter Property="Padding" Value="8,4" />
    </Style>

    <!-- Input field focus state -->
    <Style Selector="TextBox.input:focus">
      <Setter Property="BorderBrush" Value="{DynamicResource SystemAccentColor}" />
    </Style>

    <!-- Input field error state -->
    <Style Selector="TextBox.input.error">
      <Setter Property="BorderBrush" Value="#dc2626" />
      <Setter Property="Background" Value="#fef2f2" />
    </Style>

    <!-- Input field error + focus -->
    <Style Selector="TextBox.input.error:focus">
      <Setter Property="BorderBrush" Value="#dc2626" />
      <Setter Property="BoxShadow" Value="0 0 0 2px rgba(220,38,38,0.25)" />
    </Style>

    <!-- Error message text -->
    <Style Selector="TextBlock.error-text">
      <Setter Property="Foreground" Value="#dc2626" />
      <Setter Property="FontSize" Value="11" />
      <Setter Property="Margin" Value="0,2,0,0" />
    </Style>

    <!-- Disabled button style -->
    <Style Selector="Button:disabled">
      <Setter Property="Opacity" Value="0.5" />
    </Style>
  </UserControl.Styles>

  <StackPanel Spacing="12" Margin="24" MaxWidth="360">

    <!-- Username -->
    <StackPanel Spacing="2">
      <TextBlock Text="Username" />
      <TextBox Text="{Binding Username, Mode=TwoWay}"
               Classes="input"
               Name="usernameInput" />
      <TextBlock Text="{Binding (vm:RegistrationViewModel.Username)[0].ErrorMessage}"
                 Classes="error-text"
                 IsVisible="{Binding (vm:RegistrationViewModel.Username).HasErrors}" />
    </StackPanel>

    <!-- Email -->
    <StackPanel Spacing="2">
      <TextBlock Text="Email" />
      <TextBox Text="{Binding Email, Mode=TwoWay}"
               Classes="input" />
    </StackPanel>

    <!-- Password -->
    <StackPanel Spacing="2">
      <TextBlock Text="Password" />
      <TextBox Text="{Binding Password, Mode=TwoWay}"
               PasswordChar="*"
               Classes="input" />
    </StackPanel>

    <Button Content="Register"
            Command="{Binding RegisterCommand}" />

    <TextBlock Text="{Binding Username.Count, StringFormat='{0} error(s)'}"
               IsVisible="{Binding Username.HasErrors}"
               Classes="error-text" />
  </StackPanel>
</UserControl>
```

In code-behind, toggle the `error` class when validation state changes:

```csharp
// Views/RegistrationView.axaml.cs
using System.ComponentModel;

namespace MyApp.Views;

public partial class RegistrationView : UserControl
{
    public RegistrationView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ViewModels.RegistrationViewModel vm)
        {
            vm.ErrorsChanged += OnErrorsChanged;
        }
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        var input = this.FindControl<TextBox>($"{e.PropertyName}Input");
        if (input is null) return;

        var vm = (ViewModels.RegistrationViewModel)DataContext;
        var hasErrors = vm.GetErrors(e.PropertyName).Cast<object>().Any();
        if (hasErrors)
            input.Classes.Add("error");
        else
            input.Classes.Remove("error");
    }
}
```

### How it works

1. `RegistrationViewModel` extends `ObservableValidator` — validation attributes are processed by CommunityToolkit.Mvvm's source generator, which generates `ValidateProperty()` calls in the property setters.
2. Each `TextBox` has `Classes="input"`. The `.input` selector provides base appearance.
3. When validation fails, the code-behind adds `Classes="error"` to the relevant `TextBox`. The `.input.error` selector changes the border to red and the background to a light red tint.
4. The `:focus` pseudo-class interacts cleanly with `.error` — a focused, errored input shows a red glow (`:focus` + `.error`), while a focused, valid input shows the accent color.
5. The `ObservableValidator` exposes errors through `GetErrors(propertyName)`, which the `ErrorsChanged` event listens to. Each property's `[NotifyDataErrorInfo]` attribute ensures the event fires on validation changes.
6. The error message `TextBlock` uses the `(vm:RegistrationViewModel.Username)[0].ErrorMessage` binding path — this accesses the first `ValidationResult` from the indexer that `ObservableValidator` provides.

### Design decisions and edge cases

- **Code-behind for class toggling:** Avalonia does not have a `:invalid` pseudo-class. The pattern of listening to `ErrorsChanged` and toggling `Classes` is simple and works across all controls. A `Behavior<T>` could encapsulate this and keep the code-behind empty.
- **`ObservableValidator` vs manual validation:** `ObservableValidator` integrates with CommunityToolkit.Mvvm's source generators. The `[NotifyDataErrorInfo]` attribute is required on each property — without it, the `ErrorsChanged` event is not raised for that property.
- **Error message display:** The `[0].ErrorMessage` binding assumes there is at least one error. The `IsVisible` guard with `HasErrors` prevents the error text from showing when there are no errors. A `MultiBinding` with a fallback is more robust.
- **`CanExecute` interaction:** The `RegisterCommand` has `CanExecute = nameof(CanRegister)`, which returns `false` when `HasErrors` is true. The `OnHasErrorsChanged` partial method calls `RegisterCommand.NotifyCanExecuteChanged()` to keep the button in sync.

---

## What These Examples Demonstrate

| Scenario | Styling technique | What to learn |
|---|---|---|
| Card grid | Style classes + pseudo-class selectors (`/pointerover/`, `.selected`) | Interactive states without triggers, class-based theming, `DynamicResource` for theme-aware colors |
| Validation form | `ObservableValidator` + imperative class toggling | Bridging ViewModel validation to visual state, multiple selector combinations (`.error:focus`) |

The card grid example uses only selector-based styling — the ViewModel is unaware of visual state. The validation form bridges ViewModel validation state to visual appearance, demonstrating how styles interact with data-driven state.

## See Also

- [003 — Basic Styling](003-basic-styling.md)
- [003V — Verbose Companion](003-basic-styling-verbose.md)
- [006 — Resources (Static & Dynamic)](006-resources.md)
- [012 — Control Themes vs Styles](../intermediate/012-control-themes-vs-styles.md)
- [013 — Data Validation with ObservableValidator](../intermediate/013-data-validation.md)
- [Avalonia Docs: Styles](https://docs.avaloniaui.net/docs/styling/styles)
