---
tier: intermediate
topic: validation
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 013-data-validation.md
---

# 013V — Data Validation: An In-Depth Companion

**Why this exists:** The original tutorial covers the mechanics of wiring `ObservableValidator` to the UI. This companion explains *how the validation pipeline works internally*, *why Avalonia 12 changed the data annotations plugin*, *what happens when validation fires*, and *how to avoid common pitfalls* in real-time, async, and server-side validation.

**Cross-reference:** Original tutorial at [013-data-validation.md](013-data-validation.md).

---

## 1. ObservableValidator — what it does and why it exists

`ObservableValidator` is a base class from CommunityToolkit.Mvvm that extends `ObservableObject` and implements `INotifyDataErrorInfo`. It provides:

- `ValidateProperty(value, propertyName)` — validates a single property against its `[Required]`, `[MinLength]`, `[EmailAddress]`, etc. attributes.
- `ValidateAllProperties()` — validates every property that has validation attributes.
- `HasErrors` — a bool you can check before submitting.
- `GetErrors(propertyName)` — returns `IEnumerable` of `ValidationResult` for a property.
- `SetPropertyErrors(propertyName, errors)` and `ClearPropertyErrors(propertyName)` — for manual (non-attribute) error reporting.

**Why `ObservableValidator` instead of manually implementing `INotifyDataErrorInfo`:** `INotifyDataErrorInfo` has two members — `HasErrors` (bool), `GetErrors(string)` (IEnumerable), and `ErrorsChanged` (event). Implementing them manually for every ViewModel is repetitive and error-prone. `ObservableValidator` handles the event plumbing, the error collection, and the binding to data annotations attributes. You only write the rules.

**The relationship with `[ObservableProperty]`:** The source generator produces a public property from a private field. When that property is set, it calls `SetProperty(ref _field, value)`. But it does **not** automatically call `ValidateProperty` — you must call it in the `partial void On<PropertyName>Changed(string value)` hook. The generator gives you the hook; you fill in the validation call.

---

## 2. Real-time validation — why partial OnChanged methods

```csharp
[ObservableProperty]
[Required(ErrorMessage = "Name is required")]
[MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
private string _name = string.Empty;

partial void OnNameChanged(string value)
{
    ValidateProperty(value, nameof(Name));
}
```

**What happens, step by step:**

1. The user types a character in the `TextBox` bound to `Name`.
2. `TextBox` updates the binding source (the `Name` property setter) — either immediately (`UpdateSourceTrigger=PropertyChanged`) or on lost focus (`UpdateSourceTrigger=LostFocus`).
3. The generated `set_Name` method calls `SetProperty(ref _name, value)`, which fires `PropertyChanged`.
4. The generated `partial void OnNameChanged(string value)` method body (user-written) runs `ValidateProperty(value, nameof(Name))`.
5. `ValidateProperty` clears old errors for `Name`, runs all `ValidationAttribute`s on the field, and collects `ValidationResult`s.
6. If any attributes fail, `ObservableValidator` adds the errors and fires `ErrorsChanged` for `Name`.
7. The `TextBox`'s `DataValidationErrors` attached property receives the `ErrorsChanged` event and updates its error state.
8. The `TextBox` toggles its `:error` pseudo-class (if errors exist) or removes it (if errors are cleared).
9. The `TextBlock` bound to `(DataValidationErrors.Errors).Name` updates to show the first error message.

**Performance consideration:** `ValidateProperty` calls `Validator.TryValidateProperty` internally, which uses reflection to find the attributes on the field. For a form with ~20 fields, each keystroke triggers one validation call — negligible. For lists with inline editing, debounce validation (e.g., wait 300ms after the last keystroke) to avoid validation churn.

---

## 3. The XAML binding — what DataValidationErrors.Errors actually is

```xml
<TextBlock Text="{Binding (DataValidationErrors.Errors).Name}"
           Foreground="Red" FontSize="12" />
```

`DataValidationErrors` is an attached property defined on `Control`. It listens for `INotifyDataErrorInfo.ErrorsChanged` on the control's `DataContext`. When errors change, it updates its own `Errors` collection. The attached property syntax `(DataValidationErrors.Errors)` tells the binding to look for the attached property `DataValidationErrors.Errors` on the current `DataContext` (which is `RegistrationForm`), then get the `.Name` key from the error dictionary.

**Why the attached property syntax:** `DataValidationErrors.Errors` returns a `IDictionary<string, IReadOnlyList<object>>` — a dictionary keyed by property name. `{Binding (DataValidationErrors.Errors).Name}` navigates into the dictionary and retrieves the errors for the `Name` property. The first error message is displayed when the `TextBlock` renders the collection (it calls `.ToString()` on the first item).

**Alternative — implicit error styling:** Instead of a `TextBlock` per field, you can rely on the `:error` pseudo-class:

```xml
<Style Selector="TextBox:error">
  <Setter Property="BorderBrush" Value="Red" />
</Style>
```

This does not show the error message text — only a visual indicator. For message text, you need the `TextBlock` approach or a custom error template.

---

## 4. Avalonia 12 change — data annotations binding plugin removed

In Avalonia 11, there was a "data annotations binding plugin" that intercepted property changes on `INotifyDataErrorInfo` ViewModels and automatically displayed errors. In Avalonia 12, this plugin was removed because the `DataValidationErrors` system was made part of the control itself, not the binding pipeline.

**What this means for your code:**

- `DataValidationErrors.Errors` still works — it is built into `Control`, not the binding system.
- You no longer need to call `UpdateDataValidation` in custom controls (that override was v11-only).
- The error display is triggered by `INotifyDataErrorInfo.ErrorsChanged` on the `DataContext`, not by binding pipeline hooks.

**If errors are not showing in v12:**

1. Confirm your ViewModel inherits `ObservableValidator`.
2. Confirm you call `ValidateProperty` in the `OnChanged` hook.
3. Confirm the `DataContext` implements `INotifyDataErrorInfo` (ObservableValidator does).
4. Check that `TextBox` has `Text="{Binding Name, Mode=TwoWay}"` — without `TwoWay`, the source is never updated, so validation never fires.

---

## 5. Server-side validation — SetError pattern

```csharp
[ObservableProperty]
private string _email = string.Empty;

public async Task ValidateEmailAsync()
{
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

**Why this is separate from the data annotations system:** Data annotations attributes like `[EmailAddress]` are synchronous and run on the UI thread. Server-side checks (email uniqueness, username availability, CAPTCHA) are async and require HTTP calls. You cannot express "go check the database" as an attribute.

**What `SetPropertyErrors` does:** It stores the `ValidationResult` in the internal error dictionary for `Email` and fires `ErrorsChanged`. The UI picks it up exactly as it would a failed `[Required]` attribute. This means server errors and client errors share the same display path.

**When to call server validation:** Do not call it on every keystroke. Debounce (300-500ms) or call only on submit/blur. `ValidateEmailAsync` is called from a command or a blur handler, not from `OnEmailChanged` unless debounced.

---

## 6. Submit-time validation — ValidateAllProperties

```csharp
[RelayCommand]
private void Submit()
{
    ValidateAllProperties();

    if (HasErrors) return;

    // Proceed with submission
}
```

**What `ValidateAllProperties` does:** It iterates every property that has `ValidationAttribute`s and runs `ValidateProperty` on each. This is useful because a user might not have tabbed through every field — required fields could still be empty with no error shown. `ValidateAllProperties` forces all errors to surface at once.

**Interaction with real-time validation:** Calling `ValidateAllProperties` does not clear errors that were already shown. It adds errors for any field that was never validated (because its `OnChanged` hook was never called). After `ValidateAllProperties`, `HasErrors` reflects the complete state.

---

## 7. Custom validation attributes

```csharp
public class NoSpecialCharactersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is string s && s.Any(c => !char.IsLetterOrDigit(c)))
        {
            return new ValidationResult("No special characters allowed");
        }
        return ValidationResult.Success;
    }
}
```

Apply it like any other attribute:

```csharp
[ObservableProperty]
[NoSpecialCharacters(ErrorMessage = "Username cannot contain special characters")]
private string _username = string.Empty;
```

---

## Key Takeaways

- `ObservableValidator` implements `INotifyDataErrorInfo` and integrates with `[ValidationAttribute]`s. It is the recommended base class for any ViewModel with user input.
- Call `ValidateProperty()` in `partial void On<Property>Changed()` for real-time, per-keystroke validation.
- Use `(DataValidationErrors.Errors).PropertyName` in XAML to display the first error message for a property.
- The `:error` pseudo-class is toggled automatically on controls with validation errors — style it with a red border.
- Server-side validation uses `SetPropertyErrors`/`ClearPropertyErrors` directly, not attributes.
- `ValidateAllProperties()` on submit catches fields the user never focused.
- Avalonia 12 removed the data annotations binding plugin but kept `DataValidationErrors` — errors are driven by `INotifyDataErrorInfo` on the DataContext, not by the binding pipeline.

---

## See Also

- [013 — Data Validation (original)](013-data-validation.md)
- [007 — ObservableObject & ObservableProperty](../basics/007-observable-object-property.md)
- [Avalonia Docs: Data Validation](https://docs.avaloniaui.net/docs/data-binding/data-validation)
- [013E — Data Validation (examples)](013-data-validation-examples.md)
- [CommunityToolkit.Mvvm Docs: ObservableValidator](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablevalidator)
