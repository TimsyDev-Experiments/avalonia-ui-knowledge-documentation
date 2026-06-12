---
tier: intermediate
topic: validation
estimated: 8 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 013 — Data Validation with ObservableValidator

**What you'll learn:** Add validation rules to ViewModels, display error messages in the UI, and use Avalonia 12's simplified validation pipeline.

**Prerequisites:** [007 — ObservableObject & ObservableProperty](../basics/007-observable-object-property.md)

---

## 1. The ViewModel

```csharp
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class RegistrationForm : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
    private string _name = string.Empty;

    [ObservableProperty]
    [EmailAddress(ErrorMessage = "Invalid email")]
    private string _email = string.Empty;

    partial void OnNameChanged(string value)
    {
        ValidateProperty(value, nameof(Name));
    }

    partial void OnEmailChanged(string value)
    {
        ValidateProperty(value, nameof(Email));
    }
}
```

`ObservableValidator` extends `ObservableObject` with `INotifyDataErrorInfo` support. Call `ValidateProperty` whenever a property changes.

---

## 2. Bulk validation

```csharp
[RelayCommand]
private void Submit()
{
    ValidateAllProperties();

    if (HasErrors)
    {
        // Show errors — the UI already displays them
        return;
    }

    // Proceed with submission
}
```

---

## 3. The XAML — error display

```xml
<StackPanel x:DataType="vm:RegistrationForm"
            Spacing="8" Margin="20">
  <TextBox Text="{Binding Name, Mode=TwoWay}"
           Watermark="Name" />
  <TextBlock Text="{Binding (DataValidationErrors.Errors).Name}"
             Foreground="Red"
             FontSize="12" />

  <TextBox Text="{Binding Email, Mode=TwoWay}"
           Watermark="Email" />
  <TextBlock Text="{Binding (DataValidationErrors.Errors).Email}"
             Foreground="Red"
             FontSize="12" />

  <Button Content="Submit"
          Command="{Binding SubmitCommand}" />
</StackPanel>
```

> Avalonia 12 removed the data annotations binding plugin. `DataValidationErrors` still works because it's built into the control, not the binding pipeline.

---

## 4. Server-side validation errors

```csharp
[ObservableProperty]
private string _email = string.Empty;

public async Task ValidateEmailAsync()
{
    // Clear previous errors for this property
    SetError(nameof(Email), null);

    var isTaken = await CheckEmailExistsAsync(Email);
    if (isTaken)
    {
        SetError(nameof(Email), new ValidationResult("Email is already registered"));
    }
}

private void SetError(string propertyName, ValidationResult? result)
{
    _ = result is not null
        ? SetPropertyErrors(propertyName, new[] { result })
        : ClearPropertyErrors(propertyName);
}
```

---

## 5. Custom validation style (red border)

```xml
<Style Selector="TextBox:error">
  <Setter Property="BorderBrush" Value="Red" />
  <Setter Property="BorderThickness" Value="2" />
</Style>
```

The `:error` pseudo-class is automatically applied when the control has validation errors.

---

## 6. Avalonia 12 change: no more UpdateDataValidation overrides

In v11, you needed to override `UpdateDataValidation` to report errors. In v12, it's automatic.

```diff
- protected override void UpdateDataValidation(...) { ... }
+ // Remove — happens automatically
```

---

## Key Takeaways

- `ObservableValidator` for data annotations, `INotifyDataErrorInfo` for custom errors
- Call `ValidateProperty()` in `On<Property>Changed` for real-time validation
- Use `DataValidationErrors.Errors` to display per-property errors
- The `:error` pseudo-class lets you style invalid controls
- v12 automatically reports data validation errors — no `UpdateDataValidation` override needed

---

## See Also

- [007 — ObservableObject & ObservableProperty](../basics/007-observable-object-property.md)
- [022 — Validation Pipeline](file:///C:/Users/tmher/source/development-plugin-for-avalonia/references/22-validation-pipeline-and-data-errors.md) (plugin ref)
- [Avalonia Docs: Data Validation](https://docs.avaloniaui.net/docs/data-binding/data-validation)
