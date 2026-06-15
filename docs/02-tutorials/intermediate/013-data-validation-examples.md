---
tier: intermediate
topic: validation
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 013-data-validation.md
---

# 013E — Data Validation: Real-World Examples

**What this is:** Two worked examples showing client-side and server-side validation with `ObservableValidator`. Read [013 — Data Validation](013-data-validation.md) and [013V — Verbose Companion](013-data-validation-verbose.md) first.

---

## Example 1: Registration Form with Cross-Field Validation

### Goal

Validate a user registration form where the password confirmation must match the password, and the form can only be submitted when all fields are valid.

### ViewModel

```csharp
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class RegistrationForm : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username: letters, digits, underscore only")]
    private string _username = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    private string _email = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    private string _password = string.Empty;

    [ObservableProperty]
    [Required(ErrorMessage = "Confirm your password")]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool _agreeToTerms;

    partial void OnUsernameChanged(string value) =>
        ValidateProperty(value, nameof(Username));

    partial void OnEmailChanged(string value) =>
        ValidateProperty(value, nameof(Email));

    partial void OnPasswordChanged(string value)
    {
        ValidateProperty(value, nameof(Password));
        // Re-validate confirm when password changes
        if (!string.IsNullOrEmpty(ConfirmPassword))
            ValidateProperty(ConfirmPassword, nameof(ConfirmPassword));
    }

    partial void OnConfirmPasswordChanged(string value)
    {
        ValidateProperty(value, nameof(ConfirmPassword));
        // Cross-field: check match against password
        if (value != Password)
        {
            SetPropertyErrors(nameof(ConfirmPassword), new[]
            {
                new ValidationResult("Passwords do not match"),
            });
        }
        else
        {
            ClearPropertyErrors(nameof(ConfirmPassword));
        }
    }

    private bool CanRegister() =>
        !HasErrors && AgreeToTerms && !IsSubmitting;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        ValidateAllProperties();
        if (HasErrors) return;

        IsSubmitting = true;
        try
        {
            // POST to API
            await Task.Delay(1000);
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
```

### XAML View

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:vm="using:MyApp.ViewModels"
            x:DataType="vm:RegistrationForm"
            Spacing="8" Margin="20" Width="380">

  <TextBlock Text="Create Account" FontSize="22" FontWeight="Bold" />

  <TextBox Text="{Binding Username, Mode=TwoWay}"
           Watermark="Username" />
  <TextBlock Text="{Binding (DataValidationErrors.Errors).Username}"
             Foreground="#dc3545" FontSize="12" />

  <TextBox Text="{Binding Email, Mode=TwoWay}"
           Watermark="Email" />
  <TextBlock Text="{Binding (DataValidationErrors.Errors).Email}"
             Foreground="#dc3545" FontSize="12" />

  <TextBox Text="{Binding Password, Mode=TwoWay}"
           Watermark="Password"
           PasswordChar="●" />
  <TextBlock Text="{Binding (DataValidationErrors.Errors).Password}"
             Foreground="#dc3545" FontSize="12" />

  <TextBox Text="{Binding ConfirmPassword, Mode=TwoWay}"
           Watermark="Confirm password"
           PasswordChar="●" />
  <TextBlock Text="{Binding (DataValidationErrors.Errors).ConfirmPassword}"
             Foreground="#dc3545" FontSize="12" />

  <CheckBox IsChecked="{Binding AgreeToTerms}">
    I agree to the terms of service
  </CheckBox>

  <Button Content="Register"
          Command="{Binding RegisterCommand}"
          HorizontalAlignment="Stretch"
          Margin="0,8,0,0" />
</StackPanel>
```

### How It Works

1. Each `TextBox` updates the ViewModel via two-way binding. The `On<Property>Changed` partial method calls `ValidateProperty` immediately — validation fires on every keystroke.
2. `OnPasswordChanged` also re-validates `ConfirmPassword` if it has a value, because changing the password may break the match.
3. `OnConfirmPasswordChanged` performs cross-field validation: if `value != Password`, it calls `SetPropertyErrors` to add a custom error. If they match, it clears errors.
4. `RegisterCommand` uses `[RelayCommand(CanExecute = nameof(CanRegister))]`. The button is disabled when `HasErrors` is true, `AgreeToTerms` is false, or the form is submitting.
5. `ValidateAllProperties()` in the command handler ensures fields the user never tabbed through also show errors.

### Design Decisions & Edge Cases

- **Cross-field validation via `SetPropertyErrors`:** Data annotations `[Compare]` attribute exists but ties the error to the wrong property in some frameworks. Manual `SetPropertyErrors` gives explicit control.
- **`NotifyCanExecuteChangedFor` on `AgreeToTerms`:** The checkbox toggles `CanRegister`, which re-evaluates the button's enabled state. Without this, the button stays disabled until another command-notifying event fires.
- **Edge case — user clears confirm field:** `OnConfirmPasswordChanged` fires with empty string. `[Required]` fails, and the match check runs (`"" != password`), producing both errors. To avoid double-messages, clear the cross-field error before running attribute validation.
- **Edge case — paste into password field:** `OnPasswordChanged` fires once with the full pasted value. Validation runs once, not per character. Performance is fine for a single invocation.

---

## Example 2: Debounced Server-Side Validation (Username Availability)

### Goal

Check username availability against the server as the user types, with a 400ms debounce to avoid flooding the API, and display the result (available, taken, or error) inline.

### ViewModel

```csharp
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class UsernameField : ObservableValidator
{
    private readonly IUserService _users;
    private CancellationTokenSource? _debounceCts;

    public UsernameField(IUserService users)
    {
        _users = users;
    }

    [ObservableProperty]
    [Required(ErrorMessage = "Username is required")]
    [MinLength(3, ErrorMessage = "At least 3 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Invalid characters")]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _availabilityMessage = string.Empty;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isAvailable;

    partial void OnUsernameChanged(string value)
    {
        ValidateProperty(value, nameof(Username));
        CheckAvailabilityAsync(value);
    }

    private async void CheckAvailabilityAsync(string username)
    {
        // Cancel previous debounce
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        // Skip server check if client-side validation failed
        if (GetErrors(nameof(Username)).Any())
        {
            AvailabilityMessage = string.Empty;
            IsAvailable = false;
            IsChecking = false;
            return;
        }

        IsChecking = true;
        AvailabilityMessage = "Checking...";

        try
        {
            await Task.Delay(400, token); // Debounce
            if (token.IsCancellationRequested) return;

            var available = await _users.CheckUsernameAsync(username, token);

            if (token.IsCancellationRequested) return;

            IsAvailable = available;
            AvailabilityMessage = available ? "✓ Available" : "✗ Taken";

            if (!available)
            {
                SetPropertyErrors(nameof(Username), new[]
                {
                    new ValidationResult("This username is already taken"),
                });
            }
            else
            {
                ClearPropertyErrors(nameof(Username));
            }
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled — expected
        }
        catch (Exception ex)
        {
            AvailabilityMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
        }
    }
}
```

### XAML View

```xml
<StackPanel xmlns="https://github.com/avaloniaui"
            xmlns:vm="using:MyApp.ViewModels"
            x:DataType="vm:UsernameField"
            Spacing="4" Margin="20" Width="300">

  <TextBox Text="{Binding Username, Mode=TwoWay}"
           Watermark="Choose a username" />

  <!-- Availability indicator -->
  <Grid ColumnDefinitions="Auto,*" Spacing="6">
    <ProgressBar IsVisible="{Binding IsChecking}"
                 IsIndeterminate="True"
                 Width="16" Height="16" />
    <TextBlock Grid.Column="1"
               Text="{Binding AvailabilityMessage}"
               Foreground="{Binding IsAvailable,
                 Converter={StaticResource BoolToGreenConverter}}"
               FontSize="12" />
  </Grid>

  <TextBlock Text="{Binding (DataValidationErrors.Errors).Username}"
             Foreground="#dc3545" FontSize="12" />
</StackPanel>
```

### How It Works

1. Every keystroke in the `TextBox` triggers `OnUsernameChanged`, which calls `CheckAvailabilityAsync`.
2. `CheckAvailabilityAsync` cancels any pending debounce (via `CancellationTokenSource`), then waits 400ms. If the user types again within 400ms, the previous check is cancelled and a new timer starts.
3. If client-side validation fails (too short, invalid chars), the server check is skipped — no point asking the server about an invalid username.
4. After the debounce, the method calls `_users.CheckUsernameAsync`. On success, it sets `IsAvailable` and either clears or adds a `ValidationResult` via `SetPropertyErrors`.
5. The UI shows a progress bar while checking, the status message in green (available) or red (taken), and the validation error text below.

### Design Decisions & Edge Cases

- **Debounce vs throttle:** Debounce (wait for a pause) is correct here — you want the final value after the user stops typing. Throttle (fire at most once per N ms) would still send intermediate values.
- **Why `CancellationTokenSource` per call:** Each debounce creates a new CTS. Cancelling the previous one prevents a stale response from overwriting a newer check. The `token.IsCancellationRequested` checks after `Task.Delay` and after the API call guard against continuation after cancellation.
- **Edge case — rapid typing:** If the user types "j", "jo", "joh", "john" in 300ms, only "john" triggers a server call. The first three attempts are cancelled.
- **Edge case — network failure:** The `catch (Exception ex)` sets the message to "Error: ..." and `IsAvailable` stays false. The user sees that the check failed but the form can still be submitted (server will re-validate).
- **Trade-off:** The ViewModel owns a `CancellationTokenSource`, which must be cancelled when the ViewModel is disposed. Implement `IDisposable` and cancel in `Dispose`.

---

## Comparison

| Aspect | Example 1 — Registration Form | Example 2 — Username Availability |
|---|---|---|
| **Validation scope** | Cross-field (password match) | Server-side (uniqueness) |
| **Validation timing** | Real-time per keystroke | Debounced (400ms) |
| **Error source** | Data annotations + manual `SetPropertyErrors` | Server response |
| **Async pattern** | None (synchronous validation) | CancellationToken + debounce |
| **UI feedback** | Per-field error text + disabled submit button | Availability status line + validation error |
| **When to use** | Forms with dependent fields | Fields that require server round-trip |
| **Key risk** | Cross-field race condition on rapid edits | Cancelled responses overwriting newer ones (handled by CTS) |

---

## See Also

- [013 — Data Validation (original)](013-data-validation.md)
- [013V — Data Validation (verbose companion)](013-data-validation-verbose.md)
- [007 — ObservableObject & ObservableProperty](../basics/007-observable-object-property.md)
- [Avalonia Docs: Data Validation](https://docs.avaloniaui.net/docs/data-binding/data-validation)
- [CommunityToolkit.Mvvm Docs: ObservableValidator](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablevalidator)
