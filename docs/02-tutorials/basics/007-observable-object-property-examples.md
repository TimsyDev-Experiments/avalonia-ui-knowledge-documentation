---
tier: basics
topic: mvvm
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 007-observable-object-property.md
---

# 007X — ObservableObject and ObservableProperty: Real-World Examples

**What you'll build:** A customer editor with cascading field dependencies and a multi-step calculator with rollback support — two scenarios that demonstrate change notification chaining, partial method hooks, and computed property patterns.

**Prerequisites:** [007 — ObservableObject & ObservableProperty](007-observable-object-property.md). The [verbose companion](007-observable-object-property-verbose.md) covers the generated code shape, `SetProperty` internals, and the `partial void` lifecycle.

---

## Example 1: Customer Editor with Cascading Dependencies

**Goal:** Build a customer profile editor where changing one field auto-updates computed fields, validates related fields, and enables/disables commands. All dependencies are explicit via `[NotifyPropertyChangedFor]` and partial methods.

When the user changes the country, the available regions update, the postal code format hint changes, and the "Save" command re-evaluates. Changing the first or last name recomputes the full name and display initial.

### ViewModel

```csharp
// ViewModels/CustomerViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class CustomerViewModel : ObservableValidator
{
    // --- Direct input fields ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    [NotifyPropertyChangedFor(nameof(Initials))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _firstName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    [NotifyPropertyChangedFor(nameof(Initials))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _lastName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RegionOptions))]
    [NotifyPropertyChangedFor(nameof(PostalCodeHint))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _selectedCountry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string? _selectedRegion;

    [ObservableProperty]
    private string _postalCode = string.Empty;

    // --- Computed read-only properties (no setter) ---

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Initials
    {
        get
        {
            var first = FirstName.Length > 0 ? FirstName[0].ToString().ToUpper() : string.Empty;
            var last = LastName.Length > 0 ? LastName[0].ToString().ToUpper() : string.Empty;
            return $"{first}{last}";
        }
    }

    public string PostalCodeHint => SelectedCountry switch
    {
        "US" => "ZIP code (5 digits)",
        "CA" => "Postal code (A#A #A#)",
        "UK" => "Postcode (e.g., SW1A 1AA)",
        _ => "Postal code",
    };

    // --- Data lookup tables ---

    private static readonly Dictionary<string, string[]> CountryRegions = new()
    {
        ["US"] = new[] { "Alabama", "Alaska", "Arizona", /* ... */ "Wyoming" },
        ["CA"] = new[] { "Alberta", "British Columbia", "Ontario", "Quebec" },
        ["UK"] = new[] { "England", "Scotland", "Wales", "Northern Ireland" },
    };

    private string[]? _regionOptions;
    public string[]? RegionOptions
    {
        get => _regionOptions;
        private set
        {
            if (SetProperty(ref _regionOptions, value))
            {
                SelectedRegion = null; // reset when country changes
            }
        }
    }

    // --- Save logic ---

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        System.Diagnostics.Debug.WriteLine(
            $"Saved: {FullName}, {Email}, {SelectedCountry}/{SelectedRegion}");
    }

    private bool CanSave() =>
        !string.IsNullOrWhiteSpace(FirstName) &&
        !string.IsNullOrWhiteSpace(LastName) &&
        Email.Contains('@') &&
        SelectedCountry is not null;

    // --- Partial method hooks ---

    partial void OnSelectedCountryChanged(string? value)
    {
        RegionOptions = value is not null && CountryRegions.ContainsKey(value)
            ? CountryRegions[value]
            : null;
    }

    partial void OnEmailChanged(string value)
    {
        // Clear the error when email becomes valid
        if (value.Contains('@'))
        {
            // ObservableValidator would clear the error via ValidateProperty
        }
    }
}
```

### View

```xml
<!-- Views/CustomerEditorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.CustomerEditorView"
             x:DataType="vm:CustomerViewModel">

  <StackPanel Spacing="12" Margin="24" MaxWidth="400">

    <!-- Avatar with initials -->
    <Border CornerRadius="28" Width="56" Height="56"
            Background="{DynamicResource BrandAccent}"
            HorizontalAlignment="Center">
      <TextBlock Text="{Binding Initials}"
                 Foreground="White" FontSize="22"
                 FontWeight="Bold"
                 HorizontalAlignment="Center"
                 VerticalAlignment="Center" />
    </Border>

    <TextBlock Text="{Binding FullName}"
               FontSize="18" FontWeight="SemiBold"
               HorizontalAlignment="Center" />

    <Separator />

    <!-- Name fields -->
    <Grid ColumnDefinitions="*,*" Gap="8">
      <TextBox Text="{Binding FirstName, Mode=TwoWay}"
               Watermark="First name" />
      <TextBox Grid.Column="1"
               Text="{Binding LastName, Mode=TwoWay}"
               Watermark="Last name" />
    </Grid>

    <!-- Email -->
    <TextBox Text="{Binding Email, Mode=TwoWay}"
             Watermark="Email address" />

    <!-- Country and Region -->
    <ComboBox ItemsSource="{Binding $this.CountryOptions}"
              SelectedItem="{Binding SelectedCountry, Mode=TwoWay}"
              Watermark="Select country" />

    <ComboBox ItemsSource="{Binding RegionOptions}"
              SelectedItem="{Binding SelectedRegion, Mode=TwoWay}"
              Watermark="Select region"
              IsEnabled="{Binding RegionOptions, Converter={StaticResource NotNullToBool}}" />

    <!-- Postal code with context-sensitive hint -->
    <TextBox Text="{Binding PostalCode, Mode=TwoWay}"
             Watermark="{Binding PostalCodeHint, Mode=OneWay}" />

    <Separator />

    <Button Content="Save Customer"
            Command="{Binding SaveCommand}"
            HorizontalAlignment="Right" />
  </StackPanel>
</UserControl>
```

### How it works

1. **Cascading updates via `[NotifyPropertyChangedFor]`:** `FirstName` and `LastName` both declare `[NotifyPropertyChangedFor(nameof(FullName))]` and `[NotifyPropertyChangedFor(nameof(Initials))]`. When either field changes, the generated setter raises `PropertyChanged` for the computed properties. The avatar initials and full name update immediately.
2. **`OnSelectedCountryChanged` partial method:** When the user picks a country, this hook runs automatically. It updates `RegionOptions` (which triggers the ComboBox to reload) and resets `SelectedRegion` to null. The `SetProperty` call in the `RegionOptions` setter ensures that if the new regions array is the same reference as the old one, no notification is raised.
3. **`PostalCodeHint` computed property:** This reads `SelectedCountry` directly in its getter. Because `OnSelectedCountryChanged` does not explicitly notify `PostalCodeHint`, the hint would not update — but the `[NotifyPropertyChangedFor(nameof(PostalCodeHint))]` on `SelectedCountry` fixes that.
4. **`CanSave` with multiple dependencies:** The `SaveCommand` has `CanExecute` that checks `FirstName`, `LastName`, `Email`, and `SelectedCountry`. Each of these properties includes `[NotifyCanExecuteChangedFor(nameof(SaveCommand))]` so the Save button enables/disables immediately as the user fills in fields.
5. **Region ComboBox enabled state:** The `IsEnabled` binding uses a `NotNullToBool` converter on `RegionOptions`. When the user clears the country selection, `RegionOptions` becomes null and the region ComboBox disables.

### Design decisions and edge cases

- **`[ObservableProperty]` vs hand-written property for `RegionOptions`:** `RegionOptions` is hand-written because its setter contains custom logic (resetting `SelectedRegion`). The `[ObservableProperty]` generator cannot inject this logic. The hand-written version is explicit about the side effect.
- **`CountryOptions` as a static list:** The example references `$this.CountryOptions` — a resource or static property on the control. In practice, move country options to the ViewModel or a service. The `$this` binding points to the `UserControl`, not the ViewModel.
- **No validation on `PostalCode`:** The `PostalCode` field has no `CanSave` dependency. The example omits it for brevity. In a real app, add validation based on `SelectedCountry` pattern.
- **Race condition in cascading resets:** When `SelectedCountry` changes, `RegionOptions` is set (which resets `SelectedRegion` to null). The property change notifications fire in this order: `SelectedCountry` → `RegionOptions` → `SelectedRegion`. The binding system sees the updated values in the correct sequence.

---

## Example 2: Multi-Operation Calculator with Rollback

**Goal:** Build a calculator that tracks an operation history and supports undo. Each operation modifies a shared state. When the user performs an operation, the previous state is saved for rollback. This demonstrates `INotifyPropertyChanging` for capturing old values and partial methods for operation logging.

### ViewModel

```csharp
// ViewModels/CalculatorViewModel.cs
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class CalculatorViewModel : ObservableObject
{
    [ObservableProperty]
    private double _currentValue = 0;

    [ObservableProperty]
    private double _inputValue = 0;

    [ObservableProperty]
    private string _expression = "0";

    public ObservableCollection<string> History { get; } = new();

    private readonly Stack<(double previousValue, string description)> _undoStack = new();

    // Track the previous value before it changes — uses INotifyPropertyChanging
    partial void OnCurrentValueChanging(double value)
    {
        // Capture the old value before the setter stores the new one
        var oldValue = CurrentValue;
        _pendingUndoEntry = (oldValue, $"{Expression} = {value}");
    }

    private (double, string)? _pendingUndoEntry;

    partial void OnCurrentValueChanged(double value)
    {
        if (_pendingUndoEntry is not null)
        {
            _undoStack.Push(_pendingUndoEntry.Value);
            History.Add(_pendingUndoEntry.Value.description);
            Expression = $"{value}";
            _pendingUndoEntry = null;
        }
    }

    [RelayCommand]
    private void Add()
    {
        CurrentValue += InputValue;
    }

    [RelayCommand]
    private void Subtract()
    {
        CurrentValue -= InputValue;
    }

    [RelayCommand]
    private void Multiply()
    {
        CurrentValue *= InputValue;
    }

    [RelayCommand]
    private void Divide()
    {
        if (InputValue == 0)
        {
            Expression = "Error: divide by zero";
            return;
        }
        CurrentValue /= InputValue;
    }

    [RelayCommand]
    private void Clear()
    {
        CurrentValue = 0;
        InputValue = 0;
        Expression = "0";
        History.Clear();
        _undoStack.Clear();
    }

    [RelayCommand]
    private void Undo()
    {
        if (_undoStack.TryPop(out var entry))
        {
            CurrentValue = entry.previousValue;
            History.RemoveAt(History.Count - 1);
            Expression = $"{CurrentValue}";
        }
    }

    [RelayCommand]
    private void PushDigit(string digit)
    {
        // Append digit to InputValue
        var current = InputValue.ToString("0");
        InputValue = double.Parse(current + digit);
    }
}
```

### View

```xml
<!-- Views/CalculatorView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:MyApp.ViewModels"
             x:Class="MyApp.Views.CalculatorView"
             x:DataType="vm:CalculatorViewModel">

  <Grid ColumnDefinitions="*,Auto" Margin="16" Gap="16">
    <!-- Calculator panel -->
    <StackPanel Spacing="8">
      <!-- Display -->
      <Border Background="#1a1a2e"
              CornerRadius="8"
              Padding="12"
              Margin="0,0,0,8">
        <StackPanel>
          <TextBlock Text="{Binding Expression}"
                     Foreground="#888"
                     FontSize="12" />
          <TextBlock Text="{Binding CurrentValue, StringFormat='{0:N2}'}"
                     Foreground="White"
                     FontSize="32"
                     FontWeight="Bold"
                     HorizontalAlignment="Right" />
        </StackPanel>
      </Border>

      <!-- Input row -->
      <Grid ColumnDefinitions="Auto,*,Auto" Gap="8">
        <Button Content="CE" Command="{Binding ClearCommand}" />
        <TextBox Grid.Column="1"
                 Text="{Binding InputValue, Mode=TwoWay}"
                 FontSize="18"
                 FontFamily="Consolas" />
        <Button Grid.Column="2"
                Content="Undo"
                Command="{Binding UndoCommand}" />
      </Grid>

      <!-- Operation buttons -->
      <Grid ColumnDefinitions="*,*,*,*" Gap="4">
        <Button Content="+" Command="{Binding AddCommand}" />
        <Button Grid.Column="1" Content="−" Command="{Binding SubtractCommand}" />
        <Button Grid.Column="2" Content="×" Command="{Binding MultiplyCommand}" />
        <Button Grid.Column="3" Content="÷" Command="{Binding DivideCommand}" />
      </Grid>

      <!-- Digit pad -->
      <WrapPanel Gap="4">
        <Button Content="1" Command="{Binding PushDigitCommand}" CommandParameter="1" Width="60" />
        <Button Content="2" Command="{Binding PushDigitCommand}" CommandParameter="2" Width="60" />
        <Button Content="3" Command="{Binding PushDigitCommand}" CommandParameter="3" Width="60" />
        <Button Content="4" Command="{Binding PushDigitCommand}" CommandParameter="4" Width="60" />
        <Button Content="5" Command="{Binding PushDigitCommand}" CommandParameter="5" Width="60" />
        <Button Content="6" Command="{Binding PushDigitCommand}" CommandParameter="6" Width="60" />
        <Button Content="7" Command="{Binding PushDigitCommand}" CommandParameter="7" Width="60" />
        <Button Content="8" Command="{Binding PushDigitCommand}" CommandParameter="8" Width="60" />
        <Button Content="9" Command="{Binding PushDigitCommand}" CommandParameter="9" Width="60" />
        <Button Content="0" Command="{Binding PushDigitCommand}" CommandParameter="0" Width="60" />
      </WrapPanel>
    </StackPanel>

    <!-- History panel -->
    <Border Grid.Column="1" BorderBrush="#ddd" BorderThickness="1"
            CornerRadius="8" Padding="8" MinWidth="180">
      <StackPanel>
        <TextBlock Text="History" FontWeight="Bold" Margin="0,0,0,8" />
        <ItemsControl ItemsSource="{Binding History}">
          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="x:String">
              <TextBlock Text="{Binding .}" FontSize="11" Margin="0,1" />
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
      </StackPanel>
    </Border>
  </Grid>
</UserControl>
```

### How it works

1. **`OnCurrentValueChanging` captures the old value:** The `partial void OnCurrentValueChanging(double value)` method is called by the generated setter *before* the backing field is updated. At this point, `CurrentValue` still holds the pre-operation value. The method saves it to `_pendingUndoEntry`.
2. **`OnCurrentValueChanged` commits the undo entry:** After the setter stores the new value, `OnCurrentValueChanged(double value)` runs. It pushes the pending entry onto `_undoStack` and adds a description to the history list.
3. **The `Undo` command pops the stack:** When the user clicks Undo, `UndoCommand` pops the most recent entry and restores `CurrentValue` to the previous value. Because `CurrentValue` is an `[ObservableProperty]`, setting it raises `OnCurrentValueChanging` and `OnCurrentValueChanged` again — but the undo handler removes the history entry, so the operation is not re-recorded.
4. **`INotifyPropertyChanging` vs `INotifyPropertyChanged`:** The `Changing` hook fires before the value changes. The `Changed` hook fires after. This pair is used to capture the before-state for the undo stack. If only `Changed` were used, the previous value would already be lost.
5. **The undo stack is a `Stack<(double, string)>`:** Each entry stores the previous `CurrentValue` and a description. The `Clear` command empties the stack.

### Design decisions and edge cases

- **`OnCurrentValueChanging` vs `OnCurrentValueChanged` choice:** The `Changing` hook is specifically for capturing state before mutation. If the undo information could be derived from the new value alone, `Changed` would suffice. Here, the old value is required, so both hooks are needed.
- **What happens if the same value is set?** The `[ObservableProperty]` setter checks `EqualityComparer<T>.Default.Equals` before writing. If `CurrentValue` is already 5 and the user adds 0, `OnCurrentValueChanging` is not called at all — the value did not change. This means no undo entry is created for a no-op operation, which is correct.
- **Undo after Clear:** The `Clear` command empties `_undoStack`. If the user then clicks Undo, nothing happens. The `UndoCommand` has no `CanExecute` guard in this example — in production, add `CanExecute = nameof(CanUndo)` that checks `_undoStack.Count > 0`.
- **`PushDigitCommand` with `string` parameter:** The `PushDigit` method takes a `string` — the digit character. The `[RelayCommand]` generates `IRelayCommand<string>`. The `CommandParameter` values `"1"`, `"2"`, etc. are passed as strings. Parsing inside the method ensures robustness.

---

## What These Examples Demonstrate

| Scenario | ObservableObject technique | What to learn |
|---|---|---|
| Customer editor | `[NotifyPropertyChangedFor]` chaining, `[NotifyCanExecuteChangedFor]`, partial methods | Cascading computed properties, command re-evaluation on field state, side effects in partial methods |
| Calculator with undo | `partial void On*Changing` + `partial void On*Changed` | Capturing old values via `INotifyPropertyChanging`, undo stacks, the Changing/Changed lifecycle pair |

The customer editor focuses on *forward* notification — when field A changes, B and C update automatically. The calculator focuses on *backward* tracking — capturing previous state to enable undo. Together they cover the full spectrum of `[ObservableProperty]` capabilities.

## See Also

- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [007V — Verbose Companion](007-observable-object-property-verbose.md)
- [002 — Command Binding](002-command-binding.md)
- [008 — RelayCommand in Depth](008-relay-command.md)
- [013 — Data Validation with ObservableValidator](../intermediate/013-data-validation.md)
- [CommunityToolkit.Mvvm Docs: ObservableProperty](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableproperty)
