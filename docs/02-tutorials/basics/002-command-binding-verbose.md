---
tier: basics
topic: command binding
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 002-command-binding.md
---

# 002V — Command Binding: An In-Depth Companion

**What you'll learn in this companion:** How the `[RelayCommand]` source generator works internally, why `ICommand` exists as an interface, the mechanics of `CanExecute` re-evaluation, how async commands disable themselves, and the full type hierarchy of generated commands.

**Prerequisites:** [001 — Project Setup](001-project-setup.md)

**You should already have read:** [002 — Command Binding](002-command-binding.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Why `ICommand` Exists (and What It Actually Does)

The `ICommand` interface is defined in `System.Windows.Input` and is the standard .NET contract for UI actions:

```csharp
public interface ICommand
{
    event EventHandler? CanExecuteChanged;
    bool CanExecute(object? parameter);
    void Execute(object? parameter);
}
```

Avalonia's `Button` uses this interface to wire user clicks to application logic. When the user clicks a `Button`:

1. The button's pointer-up event handler calls `Command.CanExecute(CommandParameter)`.
2. If `CanExecute` returns `true`, it calls `Command.Execute(CommandParameter)`.
3. The button subscribes to `CanExecuteChanged`. When that event fires, it re-calls `CanExecute` and enables or disables itself accordingly.

Without `ICommand`, every Button would need a custom event handler in code-behind to call a method. With `ICommand`, the Button knows how to manage its own enabled state based on the command's logic. This is the Command pattern from Gang of Four, adapted for UI.

---

## 2. How `[RelayCommand]` Generates the Command

```csharp
[RelayCommand]
private void Greet()
{
    System.Diagnostics.Debug.WriteLine("Hello from Avalonia!");
}
```

The CommunityToolkit.Mvvm source generator sees the `[RelayCommand]` attribute on a method and generates:

```csharp
// Generated code (approximately)
private IRelayCommand? _greetCommand;
public IRelayCommand GreetCommand =>
    _greetCommand ??= new RelayCommand(Greet);
```

What the generator does step by step:

1. Strips the `Async` suffix if present (e.g., `LoadDataAsync` → `LoadDataCommand`).
2. Appends `Command` to the method name.
3. Creates a backing field (`_greetCommand`) of type `IRelayCommand`.
4. Creates a public property (`GreetCommand`) that lazy-initializes a `RelayCommand` instance, passing the original method as the `Action` delegate.
5. If the method has a `CanExecute` companion (via `[RelayCommand(CanExecute = nameof(CanGreet))]`), the generated `RelayCommand` also receives a `Func<bool>` for `CanExecute`.

The lazy initialization pattern (`??=`) is important: it means the `RelayCommand` object is not created until the ViewModel is bound to a view. If the ViewModel is used in a headless test, no command object is allocated.

### Why the Method Must Be `private`

The generator does not require `private` — it works with any accessibility (`public`, `internal`, `private`). However, convention is `private` because the generated command property becomes the public API. The original method is an implementation detail; external consumers call `GreetCommand.Execute()` or bind to `{Binding GreetCommand}`, not the raw method. Making it `private` communicates: "this is an implementation detail, use the command."

---

## 3. The `IRelayCommand` Type Hierarchy

CommunityToolkit.Mvvm defines these interfaces, in order of capability:

| Interface | Inherits | Purpose |
|---|---|---|
| `IRelayCommand` | `ICommand` | Basic synchronous command |
| `IRelayCommand<T>` | `IRelayCommand` | Typed parameter (e.g., `string`) |
| `IAsyncRelayCommand` | `IRelayCommand` | Async command with `IsRunning` and `ExecutionTask` |
| `IAsyncRelayCommand<T>` | `IAsyncRelayCommand` | Typed async command |

When you `[RelayCommand]` a `void` method, you get `IRelayCommand`. When you `[RelayCommand]` a `Task`-returning method, you get `IAsyncRelayCommand` — a superset that adds `IsRunning`, `ExecutionTask`, and `ExecutionCompleted` (an `INotifyPropertyChanged`-compatible event).

This hierarchy allows the binding system to know at compile time whether a command supports async progress tracking, which enables the `IsRunning` binding without runtime type checking.

---

## 4. Compiled Bindings: Why `x:DataType` Matters Here

```xml
<Button Content="Greet"
        Command="{Binding GreetCommand}" />
```

With `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`, this `{Binding GreetCommand}` is compiled at build time. The XAML compiler:

1. Looks at `x:DataType="vm:MainViewModel"` on the `<Window>`.
2. Resolves the type `MainViewModel` from the `vm` namespace (`using:MyApp.ViewModels`).
3. Checks for a public property named `GreetCommand` of type `IRelayCommand` (or assignable).
4. Generates IL equivalent to `((MainViewModel)DataContext).GreetCommand`.

If `GreetCommand` is missing (e.g., after renaming `Greet` to `Hello` without updating the XAML), the build fails with:

```
Error AVLN:0000: The property 'GreetCommand' was not found on type 'MainViewModel'.
```

Without compiled bindings, this same error is silent — the button's `Command` property stays `null`, and clicking it does nothing. The user sees a dead button with no console warning (unless `AvaloniaTrace` logging is enabled).

---

## 5. Commands with Parameters: How `CommandParameter` Flows

```csharp
[RelayCommand]
private void GreetUser(string name)
{
    System.Diagnostics.Debug.WriteLine($"Hello, {name}!");
}
```

The generated `GreetUserCommand` is typed as `IRelayCommand<string>`. The `string` type is inferred from the method parameter. The generated `RelayCommand<string>` does:

1. In `CanExecute(object? parameter)`: casts `parameter` to `string?`. If the cast fails and the parameter is not `null`, returns `false`.
2. In `Execute(object? parameter)`: casts `parameter` to `string?` and calls `GreetUser(name)`.

The XAML:

```xml
<Button Content="Greet"
        Command="{Binding GreetUserCommand}"
        CommandParameter="World" />
```

`CommandParameter="World"` sets the button's `CommandParameter` property to the string `"World"`. When clicked, the button reads `CommandParameter` and passes it to `GreetUserCommand.Execute("World")`.

### What Happens When Types Mismatch

If the `CommandParameter` is an `int` but the command expects a `string`, the `RelayCommand<string>` converts it using `Convert.ToString(parameter)` in its internal implementation. If conversion fails, `CanExecute` returns `false` and the click is ignored. This avoids runtime `InvalidCastException` crashes — the command silently fails closed.

---

## 6. Async Commands: How Auto-Disable Works

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    await Task.Delay(1000);
}
```

The generated `IAsyncRelayCommand` wraps the `Task`-returning method in an `AsyncRelayCommand` instance. This class:

1. Overrides `CanExecute()`: returns `false` while `IsRunning` is `true`.
2. Overrides `Execute()`: sets `IsRunning = true`, calls the async method, awaits the task, then sets `IsRunning = false`. If the task faults, `ExecutionTask` contains the exception; the command does not throw on the UI thread.
3. Raises `CanExecuteChanged` when `IsRunning` changes, so bound controls re-evaluate enabled state.

This means the button disables itself for the duration of the async operation — no `IsBusy` property, no manual `CommandManager.InvalidateRequerySuggested()`. The `IsRunning` property is also bindable:

```xml
<ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}" />
```

The `ProgressBar` binds to a command's property, not a ViewModel property. This is possible because `IAsyncRelayCommand` implements `INotifyPropertyChanged` — it's an observable command.

### Why Not Use `async void`?

`async void` methods cannot be awaited by the caller and crash the process if they throw. The `[RelayCommand]` generator requires `Task` or `Task<T>` for async commands. If you use `async void`, the generator treats it as synchronous (returns `IRelayCommand`, not `IAsyncRelayCommand`), and the button stays enabled while the async operation runs.

---

## 7. CanExecute: How the Button Knows When to Re-evaluate

```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* ... */ }

private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
```

The `CanExecute` parameter name is resolved by the source generator at compile time using `nameof()`. The generated `RelayCommand` stores a `Func<bool>` delegate to `CanSave`. Every time `CanExecuteChanged` fires, the UI re-calls `CanSave()` and updates the button state.

The key question: **when does `CanExecuteChanged` fire?**

- **After `Execute` completes** (for sync commands).
- **When `IsRunning` changes** (for async commands).
- **When you call `SaveCommand.NotifyCanExecuteChanged()` explicitly.**

The automatic re-evaluation in CommunityToolkit.Mvvm is **not** automatic for property changes. If `Name` changes, `CanSave` returns a different value, but the button does not know that unless you tell it. The standard pattern is:

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name;
```

The `[NotifyCanExecuteChangedFor]` attribute tells the generator to emit `SaveCommand.NotifyCanExecuteChanged()` in the `_name` setter after the value changes. This is the correct way to keep button state in sync with property state.

Without `[NotifyCanExecuteChangedFor]`, the user changes `Name` but the "Save" button stays disabled until some other event triggers re-evaluation.

---

## 8. Common Mistakes

1. **Method is not `partial` but class is.** The `[RelayCommand]` source generator requires the enclosing class to be `partial`. If it is `partial` but the method is `static`, the generator emits an error — commands cannot be static.
2. **Used `async void` thinking it would be async.** The generator treats `void` as synchronous. Always return `Task` for async commands.
3. **Forgot `[NotifyCanExecuteChangedFor]`.** The CanExecute method is only re-evaluated when `NotifyCanExecuteChanged()` is called. Without the attribute on the dependent property, the button stays in its previous state.
4. **Named the method `Execute` or `CanExecute`.** These clash with the generated `ICommand` interface members. Use different names.
5. **Set `x:DataType` but the command's return type doesn't match.** If you bind `Command="{Binding MyCommand}"` but `MyCommand` is a plain `ICommand` and the button expects `IRelayCommand`, compiled binding still works because `IRelayCommand` implements `ICommand`.

---

## See Also

- [002 — Command Binding (original tutorial)](002-command-binding.md)
- [002X — Command Binding (examples)](002-command-binding-examples.md)
- [008 — RelayCommand in Depth](008-relay-command.md)
- [008V — RelayCommand in Depth (verbose companion)](008-relay-command-verbose.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [007V — ObservableObject & ObservableProperty (verbose companion)](007-observable-object-property-verbose.md)
- [011 — Compiled Bindings in Depth](../intermediate/011-compiled-bindings.md)
- [CommunityToolkit.Mvvm Docs: RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
