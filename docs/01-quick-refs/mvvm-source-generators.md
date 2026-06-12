---
topic: mvvm
estimated: 3 min read
researched: 2026-06-12
avalonia-version: 12.0.4
packages: CommunityToolkit.Mvvm 8.4.2
---

# CommunityToolkit.Mvvm Source Generators Reference

## Setup

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
```

ViewModel base classes: `ObservableObject`, `ObservableValidator`, `ObservableRecipient`.

## [ObservableProperty]

```csharp
[ObservableProperty]
private string _name;
// → generates: public string Name { get; set; } with INotifyPropertyChanged

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(FullName))]
private string _firstName;
// → also notifies FullName when FirstName changes

[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _email;
// → re-evaluates SaveCommand.CanExecute when Email changes

// Hook into setter:
partial void OnNameChanged(string value) { }
partial void OnNameChanging(string value) { }
```

## [RelayCommand]

| Method signature | Generated type | Behavior |
|---|---|---|
| `void Do()` | `IRelayCommand` | Always enabled |
| `void Do(T param)` | `IRelayCommand<T>` | Parameterized |
| `Task DoAsync()` | `IAsyncRelayCommand` | Auto-disables while running |
| `Task DoAsync(T param)` | `IAsyncRelayCommand<T>` | Parameterized + auto-disable |

```csharp
[RelayCommand]
private void Save() { }

[RelayCommand]
private async Task LoadAsync() { }

[RelayCommand(CanExecute = nameof(CanSave))]
private void SaveConditional() { }
private bool CanSave() => !HasErrors;

[RelayCommand]
private async Task FetchAsync(CancellationToken token) { }
// CancellationToken is injected by the source generator

[RelayCommand]
private async Task DownloadAsync(IProgress<double> progress) { }
// IProgress<T> is injected by the source generator
```

## ObservableValidator (validation)

```csharp
public partial class FormVm : ObservableValidator
{
    [ObservableProperty]
    [Required]
    [EmailAddress]
    [NotifyDataErrorInfo]
    private string _email;

    partial void OnEmailChanged(string value)
    {
        ValidateProperty(value, nameof(Email));
    }

    [RelayCommand]
    private void Submit()
    {
        ValidateAllProperties();
        if (HasErrors) return;
        // submit
    }
}
```

## ObservableRecipient (messenger)

```csharp
public partial class MyVm : ObservableRecipient
{
    // Messenger lifecycle managed automatically:
    // - Register in OnActivated()
    // - Unregister in OnDeactivated()
}
```

## IMessenger

```csharp
// Message class
public sealed class ItemSelectedMessage : ValueChangedMessage<string>
{
    public ItemSelectedMessage(string value) : base(value) { }
}

// Send
WeakReferenceMessenger.Default.Send(new ItemSelectedMessage(item));

// Receive (via IRecipient<T>)
public partial class MyVm : ObservableObject, IRecipient<ItemSelectedMessage>
{
    public void Receive(ItemSelectedMessage m) { }
}

// Request (blocking)
public sealed class ConfirmMessage : RequestMessage<bool> { }
var result = WeakReferenceMessenger.Default.Send<ConfirmMessage>();
```

## See Also

- Tutorials: [007](../02-tutorials/basics/007-observable-object-property.md), [008](../02-tutorials/basics/008-relay-command.md), [014](../02-tutorials/intermediate/014-imessenger-patterns.md)
- [CommunityToolkit.Mvvm Docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
