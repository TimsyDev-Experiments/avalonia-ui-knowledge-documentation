---
tier: intermediate
topic: data validation
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 013-data-validation.md
---

# Quiz — Data Validation (ObservableValidator)

```quiz
Q: Which base class provides INotifyDataErrorInfo support and data annotation validation in CommunityToolkit.Mvvm?
A. ObservableObject || Incorrect — ObservableObject only implements INotifyPropertyChanged, not INotifyDataErrorInfo.
B. ObservableValidator (correct) || Correct. ObservableValidator extends ObservableObject with INotifyDataErrorInfo and ValidateProperty/ValidateAllProperties methods.
C. ObservableRecipient || Incorrect — ObservableRecipient adds IMessenger integration, not validation.
D. BindableBase || Incorrect — that is from Prism, not CommunityToolkit.Mvvm.
Explanation: ObservableValidator is the CommunityToolkit.Mvvm base class that adds data annotation validation and INotifyDataErrorInfo support.
```

```quiz
Q: In Avalonia 12, how does a developer display per-property validation errors next to a TextBox?
A. Bind the TextBox's ToolTip to (Validation.Errors).PropertyName || Incorrect — ToolTip is not the standard error display mechanism.
B. Use a TextBlock bound to (DataValidationErrors.Errors).PropertyName (correct) || Correct. DataValidationErrors.Errors attached property exposes per-property error messages.
C. Handle the TextBox.LostFocus event and show a message box || Incorrect — that is procedural and defeats MVVM binding.
D. Bind to the control's ValidationMessage property || Incorrect — there is no such property on TextBox.
Explanation: DataValidationErrors.Errors is the attached property that surfaces validation messages for each property when using ObservableValidator.
```

```quiz
Q: Which method should be called inside a partial OnPropertyChanged method to validate a single property in real time?
A. ValidateAllProperties() || Incorrect — that validates all properties at once and is too heavy for per-change validation.
B. SetError(propertyName, result) || Incorrect — SetError is a custom helper from the tutorial, not the built-in method.
C. ValidateProperty(value, propertyName) (correct) || Correct. ValidateProperty validates a single property and updates the errors collection immediately.
D. ClearPropertyErrors(propertyName) || Incorrect — that only clears errors, it does not perform validation.
Explanation: Call ValidateProperty(value, nameof(PropertyName)) in the generated partial OnPropertyChanged method to trigger per-property validation on each change.
```

```quiz
Q: What is the correct way to add a red border to a TextBox when it has validation errors?
A. <Style Selector="TextBox[IsValid=False]"><Setter Property="BorderBrush" Value="Red" /></Style> || Incorrect — there is no IsValid property; the pseudo-class :error is used instead.
B. <Style Selector="TextBox:error"><Setter Property="BorderBrush" Value="Red" /></Style> (correct) || Correct. The :error pseudo-class is automatically applied when the control has validation errors.
C. <Style Selector="TextBox.ErrorTemplate"><Setter Property="BorderBrush" Value="Red" /></Style> || Incorrect — ErrorTemplate is not a selector.
D. <Style Selector="TextBox.DataValidationErrors"><Setter Property="BorderBrush" Value="Red" /></Style> || Incorrect — DataValidationErrors is an attached property, not a pseudo-class.
Explanation: Avalonia applies the `:error` pseudo-class to controls bound to properties with validation errors, enabling clean style-based error indication.
```

```quiz
Q: Which Avalonia 12 change simplified the validation pipeline compared to v11?
A. Data annotations were removed entirely || Incorrect — data annotations still work with ObservableValidator.
B. The UpdateDataValidation override is no longer needed (correct) || Correct. In v12, data validation errors are reported automatically without overriding UpdateDataValidation.
C. ValidateProperty is now called automatically without code || Incorrect — the developer must still call ValidateProperty in OnPropertyChanged.
D. DataValidationErrors.Errors was replaced by Validation.Errors || Incorrect — DataValidationErrors.Errors is still the correct attached property.
Explanation: Avalonia 12 removed the data annotations binding plugin and automatically reports errors through INotifyDataErrorInfo — no UpdateDataValidation override required.
```
