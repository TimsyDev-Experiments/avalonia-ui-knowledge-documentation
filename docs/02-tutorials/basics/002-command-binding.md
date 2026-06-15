---
tier: basics
topic: command binding
estimated: 5 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 002 — Command Binding with CommunityToolkit.Mvvm

**What you'll learn:** Wire a Button to a C# command using `[RelayCommand]`, handle parameters, and manage execution state.

**Prerequisites:** [001 — Project Setup](001-project-setup.md)

---

## 1. Create a ViewModel

```csharp
// ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [RelayCommand]
    private void Greet()
    {
        System.Diagnostics.Debug.WriteLine("Hello from Avalonia!");
    }
}
```

`[RelayCommand]` generates a `GreetCommand` property of type `IRelayCommand` that the view can bind to.

---

## 2. Bind the button in XAML

```xml
<!-- Views/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:MyApp.ViewModels"
        x:Class="MyApp.Views.MainWindow"
        x:DataType="vm:MainViewModel"
        Title="MyApp">
  <StackPanel Spacing="8" Margin="20">
    <Button Content="Greet"
            Command="{Binding GreetCommand}" />
  </StackPanel>
</Window>
```

`x:DataType` enables compiled binding — if `GreetCommand` doesn't exist on `MainViewModel`, the build fails.

---

## 3. Wire the ViewModel in code-behind

```csharp
// Views/MainWindow.axaml.cs
using MyApp.ViewModels;

namespace MyApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
```

---

## 4. Commands with parameters

```csharp
[RelayCommand]
private void GreetUser(string name)
{
    System.Diagnostics.Debug.WriteLine($"Hello, {name}!");
}
```

Generated: `GreetUserCommand` of type `IRelayCommand<string>`.

```xml
<Button Content="Greet"
        Command="{Binding GreetUserCommand}"
        CommandParameter="World" />
```

---

## 5. Async commands

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    await Task.Delay(1000);  // simulate network call
}
```

Generated: `LoadDataCommand` of type `IAsyncRelayCommand`. The button auto-disables while the task runs — no manual `IsBusy` flag needed. Bind `IsRunning` for a spinner:

```xml
<ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}" />
```

---

## 6. Commands with CanExecute

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* ... */ }

private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
```

The button auto-disables when `CanSave()` returns `false`. Call `SaveCommand.NotifyCanExecuteChanged()` to re-evaluate.

---

## Key Takeaways

- `[RelayCommand]` on a method generates `<MethodName>Command` automatically
- No manual `ICommand` implementation needed
- Use `CanExecute` for conditional enable/disable
- Async commands (returning `Task`) auto-manage IsRunning state
- `x:DataType` is required for compiled bindings — always set it

---

## See Also

- [002V — Command Binding (verbose companion)](002-command-binding-verbose.md)
- [002X — Command Binding (examples)](002-command-binding-examples.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [008 — RelayCommand in Depth](008-relay-command.md)
- [CommunityToolkit.Mvvm Docs: RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
