---
tier: advanced
topic: localization
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 033-localization-i18n.md
---

# 033X — Localization and Internationalization: Real-World Examples

## Scenario 1: Multi-Lingual POS Application with Runtime Language Switch

### Goal

Build a point-of-sale application that supports English, French, and Japanese. The user switches language at runtime via a menu, and all UI text updates immediately without restart. Currency formatting respects the selected culture.

### ResX Resource Files

**`Lang/Resources.resx`** (neutral — English):

| Name | Value |
|------|-------|
| AppTitle | Point of Sale |
| NewSale | New Sale |
| TotalLabel | Total |
| TaxLabel | Tax ({0}%) |
| PaymentLabel | Payment |
| PrintReceipt | Print Receipt |
| CurrencyFormat | {0:C} |
| ConfirmCancel | Cancel this sale? |
| Yes | Yes |
| No | No |

**`Lang/Resources.fr.resx`** (French):

| Name | Value |
|------|-------|
| AppTitle | Point de Vente |
| NewSale | Nouvelle Vente |
| TotalLabel | Total |
| TaxLabel | Taxe ({0}%) |
| PaymentLabel | Paiement |
| PrintReceipt | Imprimer le Reçu |
| ConfirmCancel | Annuler cette vente ? |
| Yes | Oui |
| No | Non |

**`Lang/Resources.ja.resx`** (Japanese):

| Name | Value |
|------|-------|
| AppTitle | POS |
| NewSale | 新規販売 |
| TotalLabel | 合計 |
| TaxLabel | 税金 ({0}%) |
| PaymentLabel | 支払い |
| PrintReceipt | レシート印刷 |
| ConfirmCancel | この販売をキャンセルしますか？ |
| Yes | はい |
| No | いいえ |

### Localization Service

```csharp
using System.ComponentModel;
using System.Globalization;
using DemoApp.Lang;

namespace DemoApp.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public string this[string key] =>
        Resources.ResourceManager.GetString(key) ?? key;

    public string AppTitle => Resources.AppTitle;
    public string NewSale => Resources.NewSale;
    public string TotalLabel => Resources.TotalLabel;
    public string PaymentLabel => Resources.PaymentLabel;
    public string PrintReceipt => Resources.PrintReceipt;
    public string ConfirmCancel => Resources.ConfirmCancel;
    public string Yes => Resources.Yes;
    public string No => Resources.No;

    public string FormatTaxLabel(double rate) =>
        string.Format(Resources.TaxLabel, rate);

    public string FormatCurrency(decimal amount)
    {
        // Use the current thread culture for currency formatting
        return string.Format(Thread.CurrentThread.CurrentCulture,
            Resources.CurrencyFormat, amount);
    }

    public void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Resources.Culture = culture;

        // Notify all bound properties
        PropertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(string.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

### ViewModel

```csharp
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoApp.Services;

namespace DemoApp.ViewModels;

public partial class PosViewModel : ObservableObject
{
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private ObservableCollection<SaleItem> _items = new();

    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _tax;

    [ObservableProperty]
    private decimal _total;

    [ObservableProperty]
    private string _selectedLanguage = "en";

    public LocalizationService Localization => _localization;

    public PosViewModel(LocalizationService localization)
    {
        _localization = localization;
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        _localization.SetLanguage(value);
    }

    [RelayCommand]
    private void AddItem(string name)
    {
        Items.Add(new SaleItem { Name = name, Price = 9.99 });
        Recalculate();
    }

    private void Recalculate()
    {
        Subtotal = 0;
        foreach (var item in Items)
            Subtotal += item.Price;
        Tax = Subtotal * 0.1m;
        Total = Subtotal + Tax;
    }
}

public partial class SaleItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _price;
}
```

### XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.PosView"
             x:DataType="vm:PosViewModel">

  <Grid RowDefinitions="Auto,*,Auto,Auto" Spacing="{StaticResource Space4}"
        Margin="{StaticResource Space4}">

    <!-- Language switcher -->
    <ComboBox Grid.Row="0"
              SelectedValue="{Binding SelectedLanguage}"
              SelectedValuePath="Content"
              HorizontalAlignment="Right">
      <ComboBoxItem Content="en">English</ComboBoxItem>
      <ComboBoxItem Content="fr">Français</ComboBoxItem>
      <ComboBoxItem Content="ja">日本語</ComboBoxItem>
    </ComboBox>

    <!-- Sale items list -->
    <ListBox Grid.Row="1"
             ItemsSource="{Binding Items}"
             x:DataType="vm:PosViewModel">
      <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:SaleItem">
          <Grid ColumnDefinitions="*,Auto" Spacing="8">
            <TextBlock Text="{Binding Name}" />
            <TextBlock Grid.Column="1"
                       Text="{Binding Price, StringFormat='{}{0:C}'}" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>

    <!-- Totals via Localization properties -->
    <StackPanel Grid.Row="2" Spacing="{StaticResource Space2}">
      <Grid ColumnDefinitions="*,Auto">
        <TextBlock Text="{Binding Localization.TotalLabel}"
                   Foreground="{DynamicResource TextSecondaryBrush}" />
        <TextBlock Grid.Column="1"
                   Text="{Binding Total, StringFormat='{}{0:C}'}"
                   FontSize="{StaticResource FontSizeLg}"
                   FontWeight="Bold" />
      </Grid>
    </StackPanel>

    <!-- Action buttons -->
    <StackPanel Grid.Row="3" Orientation="Horizontal"
                Spacing="{StaticResource Space2}"
                HorizontalAlignment="Stretch">
      <Button Content="{Binding Localization.NewSale}"
              Command="{Binding AddItemCommand}"
              CommandParameter="Widget" />
      <Button Content="{Binding Localization.PrintReceipt}" />
    </StackPanel>
  </Grid>
</UserControl>
```

### How It Works

1. `LocalizationService.SetLanguage()` sets both `Thread.CurrentThread.CurrentCulture` (affects `StringFormat='{}{0:C}'` currency display) and `Resources.Culture` (affects resource string lookup). Raising `PropertyChangedEventArgs(string.Empty)` refreshes all bindings on the `Localization` property.
2. The ViewModel exposes `Localization` as a property returning the injected `LocalizationService`. XAML binds to `{Binding Localization.TotalLabel}` — a compiled binding that resolves at compile time via `x:DataType` and subscribes to `INotifyPropertyChanged` at runtime.
3. `SelectedLanguage` is bound to the `ComboBox`. The `partial void OnSelectedLanguageChanged` method calls `SetLanguage()`. Since the ComboBox reacts to user selection and the ViewModel is `ObservableObject`, the change notification propagates automatically.
4. Currency formatting: `{}{0:C}` uses `Thread.CurrentThread.CurrentCulture`. When switching from `en` (USD: `$10.00`) to `fr` (EUR: `10,00 €`), the format changes without any per-binding code.

### Design Decisions & Edge Cases

- **`string.Empty` PropertyChanged**: The service raises `PropertyChanged` with an empty string (meaning "all properties changed"). For a POS app with 50+ resource keys, this is simpler than raising individual events. For performance-critical scenarios with many bindings, raise per-property events.
- **Culture on background threads**: `SetLanguage` sets culture on the current thread (UI thread). Background threads (e.g., print spoolers) inherit the default culture. Set `CultureInfo.DefaultThreadCurrentCulture` and `DefaultThreadCurrentUICulture` for full coverage:

```csharp
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
```

- **Missing resource keys**: If a key is missing from a locale-specific `.resx`, the `ResourceManager` falls back to the neutral (English) resource. The service's indexer `this[key]` returns the key name itself as a last resort — a visible signal during development that a translation is missing.
- **`StringFormat` in DataTemplate**: Inside the `DataTemplate` for `SaleItem`, `x:DataType="vm:SaleItem"` provides compiled binding for `Price`. The `StringFormat` picks up `Thread.CurrentThread.CurrentCulture` automatically.

---

## Scenario 2: RTL-Aware Dashboard with Culture-Aware Number and Date Formatting

### Goal

Create a financial dashboard that renders correctly in Arabic (right-to-left layout), with Arabic currency formatting (SAR), Gregorian-to-Hijri date conversion display, and culture-aware number grouping.

### ViewModel

```csharp
using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DemoApp.Services;

namespace DashboardApp.ViewModels;

public partial class FinancialDashboardViewModel : ObservableObject
{
    private readonly LocalizationService _localization;

    [ObservableProperty]
    private string _selectedLocale = "en-US";

    [ObservableProperty]
    private decimal _revenue = 1_234_567.89m;

    [ObservableProperty]
    private decimal _expenses = 987_654.32m;

    [ObservableProperty]
    private decimal _profit;

    [ObservableProperty]
    private DateTime _reportDate = DateTime.Today;

    [ObservableProperty]
    private string _formattedReportDate = string.Empty;

    public decimal ProfitMargin =>
        Revenue > 0 ? (Revenue - Expenses) / Revenue * 100 : 0;

    public LocalizationService Localization => _localization;

    public FinancialDashboardViewModel(LocalizationService localization)
    {
        _localization = localization;
    }

    partial void OnSelectedLocaleChanged(string value)
    {
        _localization.SetLanguage(value);

        // Update date format for the new culture
        FormattedReportDate = ReportDate.ToString("D",
            Thread.CurrentThread.CurrentCulture);

        // Notify computed properties
        OnPropertyChanged(nameof(ProfitMargin));

        // Update FlowDirection for RTL support
        var culture = new CultureInfo(value);
        if (culture.TextInfo.IsRightToLeft)
        {
            Avalonia.Application.Current?.RequestedThemeVariant
                = Avalonia.Styling.ThemeVariant.Dark;
            // FlowDirection is set on the Window via a messenger
        }
    }

    partial void OnReportDateChanged(DateTime value)
    {
        FormattedReportDate = value.ToString("D",
            Thread.CurrentThread.CurrentCulture);
    }
}
```

### XAML View (with RTL support)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DashboardApp.ViewModels"
             x:Class="DashboardApp.Views.FinancialDashboardView"
             x:DataType="vm:FinancialDashboardViewModel">

  <!-- FlowDirection set programmatically on the parent Window -->
  <Grid RowDefinitions="Auto,Auto,Auto" Spacing="{StaticResource Space4}"
        Margin="{StaticResource Space4}">

    <!-- Locale selector -->
    <ComboBox Grid.Row="0"
              SelectedValue="{Binding SelectedLocale}"
              SelectedValuePath="Content"
              HorizontalAlignment="End">
      <ComboBoxItem Content="en-US">English (US)</ComboBoxItem>
      <ComboBoxItem Content="ar-SA">العربية (السعودية)</ComboBoxItem>
    </ComboBox>

    <!-- Report date — culture-aware format -->
    <TextBlock Grid.Row="1"
               Text="{Binding FormattedReportDate}"
               Foreground="{DynamicResource TextSecondaryBrush}"
               FontSize="{StaticResource FontSizeSm}" />

    <!-- Financial summary cards — RTL-safe layout -->
    <Grid Grid.Row="2" ColumnDefinitions="*,*,*"
          Spacing="{StaticResource Space3}">
      <Border CornerRadius="8" Padding="{StaticResource Space4}"
              Background="{DynamicResource SurfaceBrush}">
        <StackPanel Spacing="{StaticResource Space1}">
          <TextBlock Text="{Binding Localization.RevenueLabel}"
                     Foreground="{DynamicResource TextSecondaryBrush}" />
          <TextBlock Text="{Binding Revenue, StringFormat='{}{0:N}'}"
                     FontSize="{StaticResource FontSizeXl}"
                     FontWeight="Bold" />
        </StackPanel>
      </Border>

      <Border Grid.Column="1" CornerRadius="8"
              Padding="{StaticResource Space4}"
              Background="{DynamicResource SurfaceBrush}">
        <StackPanel Spacing="{StaticResource Space1}">
          <TextBlock Text="{Binding Localization.ExpensesLabel}"
                     Foreground="{DynamicResource TextSecondaryBrush}" />
          <TextBlock Text="{Binding Expenses, StringFormat='{}{0:N}'}"
                     FontSize="{StaticResource FontSizeXl}"
                     FontWeight="Bold" />
        </StackPanel>
      </Border>

      <Border Grid.Column="2" CornerRadius="8"
              Padding="{StaticResource Space4}"
              Background="{DynamicResource SurfaceBrush}">
        <StackPanel Spacing="{StaticResource Space1}">
          <TextBlock Text="{Binding Localization.ProfitLabel}"
                     Foreground="{DynamicResource TextSecondaryBrush}" />
          <TextBlock Text="{Binding ProfitMargin, StringFormat='{}{0:N2}%'}"
                     FontSize="{StaticResource FontSizeXl}"
                     FontWeight="Bold" />
        </StackPanel>
      </Border>
    </Grid>
  </Grid>
</UserControl>
```

### Window-level RTL handling

```csharp
using System.Globalization;
using Avalonia.Controls;
using DashboardApp.ViewModels;

namespace DashboardApp.Views;

public partial class FinancialDashboardWindow : Window
{
    public FinancialDashboardWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is FinancialDashboardViewModel vm)
        {
            SetFlowDirection(vm.SelectedLocale);
        }
    }

    public void SetFlowDirection(string locale)
    {
        var culture = new CultureInfo(locale);
        FlowDirection = culture.TextInfo.IsRightToLeft
            ? Avalonia.Media.FlowDirection.RightToLeft
            : Avalonia.Media.FlowDirection.LeftToRight;
    }
}
```

### How It Works

1. **RTL detection**: `CultureInfo("ar-SA").TextInfo.IsRightToLeft` returns `true`. The `SetFlowDirection` method in the Window code-behind sets `Window.FlowDirection` to `RightToLeft` for Arabic, which propagates to all child controls.
2. **Culture-aware number formatting**: `StringFormat='{}{0:N}'` uses `Thread.CurrentThread.CurrentCulture`. For `ar-SA`, numbers display with Arabic-Indic digits (١٬٢٣٤٬٥٦٧٫٨٩) and Arabic comma separators. For `en-US`, the same binding produces `1,234,567.89`.
3. **Date formatting**: `ReportDate.ToString("D", culture)` produces the long date pattern — `Monday, June 14, 2026` for `en-US` and formatted according to the Um Al-Qura calendar for `ar-SA` (the default calendar for Saudi Arabia).
4. **Grid layout with RTL**: `Grid.ColumnDefinitions` and `Grid.Column` indices flip automatically when `FlowDirection="RightToLeft"`. Column 2 becomes the rightmost column. No layout code changes needed.

### Design Decisions & Edge Cases

- **FlowDirection set after open**: `OnOpened` sets `FlowDirection` because changing it before the window is shown causes a jarring re-layout. The initial window is LTR; it flips before the user sees content.
- **RTL and custom drawing**: If the dashboard uses custom `DrawingContext` rendering (e.g., chart controls), `FlowDirection` does NOT auto-mirror drawing commands. The chart control must check `FlowDirection` and flip coordinates manually.
- **Mixed content**: Arabic text may contain English numbers or product names. `FlowDirection="RightToLeft"` aligns text correctly for Arabic but English text within the same `TextBlock` is still rendered in its natural direction via Unicode bidirectional algorithm support.
- **Number formatting vs. digit substitution**: `StringFormat='{}{0:N}'` produces Arabic-Indic digits only when the thread culture is Arabic. For Western digits in an Arabic context, set `culture.NumberFormat.NativeDigits = new[] { "0", "1", ... }` explicitly.
- **Locale persistence**: The selected locale should be saved to `ISettingsService` and restored at startup. `FinancialDashboardViewModel` loads the saved locale before any bindings evaluate.

### Comparison

| Aspect | Scenario 1: POS App | Scenario 2: Financial Dashboard |
|---|---|---|
| Locales | en, fr, ja (all LTR) | en-US (LTR), ar-SA (RTL) |
| Layout direction | Left-to-right only | RTL via FlowDirection on Window |
| Formatting concern | Currency (`C` format) | Number (`N`), percent, date (`D`) |
| RTL impact | None | Window child mirroring, Grid column flip |
| Date handling | Not displayed | Culture-aware `ToString("D")` |
| Key challenge | Runtime label refresh | RTL layout + digit/date conversion |
| Messenger usage | None | Theme variant coordination (implicit) |

## See Also

- [033 — Localization and Internationalization](033-localization-i18n.md)
- [033V — Localization and Internationalization (verbose companion)](033-localization-i18n-verbose.md)
- [001 — Project Setup](../basics/001-project-setup.md)
- [032 — MVVM DI Wiring](032-mvvm-di-wiring.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Localization](https://docs.avaloniaui.net/docs/guides/implementation-guides/localization)
- [.NET Resource Manager docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/resources)
