---
tier: basics
topic: commands
estimated: 25-30 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 008-relay-command.md
---

# 008V — RelayCommand in Depth: An In-Depth Companion

**What you'll learn in this companion:** The internal implementation of `RelayCommand` and `AsyncRelayCommand`, how the source generator infers command type from method signature, the `CancellationToken` injection mechanism, how `IProgress<T>` is wired to the async command, the full lifecycle of `CanExecute` re-evaluation, and when to use manual `ICommand` implementations.

**Prerequisites:** [002 — Command Binding](002-command-binding.md), [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)

**You should already have read:** [008 — RelayCommand in Depth](008-relay-command.md) for the quick-start version. This file goes deeper on every section.

---

## 1. How the Source Generator Classifies Your Method

The CommunityToolkit.Mvvm source generator uses a multi-step classification algorithm on every method marked `[RelayCommand]`:

1. **Is the method `async`?** If it returns `Task` or `Task<T>`, it is async. If it returns `void`, it is sync.
2. **Does the method have parameters?** Zero parameters → non-generic command. One parameter → generic command (`RelayCommand<T>` / `AsyncRelayCommand<T>`).
3. **Is there a `CanExecute` companion?** `[RelayCommand(CanExecute = nameof(CanSave))]` → the generated command wraps both `Execute` and `CanExecute` delegates. The `CanExecute` method must return `bool` and take the same parameter type (if any).
4. **Are there injectable parameters?** `CancellationToken` and `IProgress<T>` are detected by type and removed from the command's parameter list — they are injected by the command infrastructure, not passed via `CommandParameter`.

The classification table:

| Method signature | Generated type | Generic? | Async? | CanExecute? |
|---|---|---|---|---|
| `void Do()` | `IRelayCommand` | No | No | Optional |
| `void Do(T)` | `IRelayCommand<T>` | Yes | No | Optional |
| `Task DoAsync()` | `IAsyncRelayCommand` | No | Yes | Optional |
| `Task DoAsync(T)` | `IAsyncRelayCommand<T>` | Yes | Yes | Optional |
| `Task DoAsync(CancellationToken)` | `IAsyncRelayCommand` | No | Yes | No CanExecute (injected token) |
| `Task DoAsync(IProgress<double>)` | `IAsyncRelayCommand` | No | Yes | No CanExecute (injected progress) |

---

## 2. The Generated `RelayCommand` Implementation

For a synchronous parameterless command:

```csharp
[RelayCommand]
private void Save() { /* ... */ }
```

The generator produces approximately:

```csharp
private IRelayCommand? _saveCommand;
public IRelayCommand SaveCommand =>
    _saveCommand ??= new RelayCommand(Save);
```

### What `RelayCommand` Does Internally

`RelayCommand` (from `CommunityToolkit.Mvvm.Input`) is a sealed class implementing `IRelayCommand`. Its simplified structure:

```csharp
public sealed class RelayCommand : IRelayCommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    bool ICommand.CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    void ICommand.Execute(object? parameter)
    {
        _execute();
    }

    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

Key details:

- **`canExecute` defaults to `null`.** If not provided, `CanExecute` always returns `true`.
- **`NotifyCanExecuteChanged()` is public.** You call it externally to trigger UI re-evaluation. The `CanExecuteChanged` event is the standard `ICommand` event that the `Button` subscribes to.
- **The `parameter` argument in `ICommand.CanExecute`/`Execute` is ignored** for non-generic commands. The `RelayCommand` passes the button's `CommandParameter` to `Execute` as `object?`, but the method is `void` with no parameters, so the parameter is discarded.

### Why `RelayCommand<T>` Exists

For a command with a typed parameter:

```csharp
[RelayCommand]
private void GreetUser(string name) { /* ... */ }
```

The generated `RelayCommand<string>` casts the `CommandParameter` from `object?` to `string?`:

```csharp
void ICommand.Execute(object? parameter)
{
    _execute((string?)parameter!);
}
```

The cast uses the null-forgiving operator (`!`) because the `Action<string>` delegate expects a non-nullable `string`. If `parameter` is `null` and `T` is a non-nullable reference type, you get a `NullReferenceException` at runtime. To handle nullable, use `string?` as the method parameter type:

```csharp
[RelayCommand]
private void GreetUser(string? name) { /* name can be null */ }
```

The generated `RelayCommand<string?>` uses `Action<string?>`, and the cast is `(string?)parameter` (safe for null).

---

## 3. AsyncRelayCommand: Full Auto-Disable Mechanics

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    await Task.Delay(2000);
}
```

The generated command wraps the task in an `AsyncRelayCommand`, which inherits from `RelayCommand` and adds:

```csharp
public sealed class AsyncRelayCommand : IAsyncRelayCommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private int _isRunning; // 0 = false, 1 = true (using int for Interlocked)

    public bool IsRunning => _isRunning != 0;
    public Task? ExecutionTask { get; private set; }

    public event EventHandler? CanExecuteChanged;

    bool ICommand.CanExecute(object? parameter)
    {
        if (_isRunning != 0) return false;  // auto-disable while running
        return _canExecute?.Invoke() ?? true;
    }

    async void ICommand.Execute(object? parameter)
    {
        if (Interlocked.Exchange(ref _isRunning, 1) != 0)
            return; // already running, ignore

        try
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(IsRunning));

            ExecutionTask = _execute();
            await ExecutionTask;
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(nameof(IsRunning));
        }
    }
}
```

The `Interlocked.Exchange` is important: it prevents race conditions if `Execute` is called rapidly (e.g., double-click). Only one execution runs at a time. The `CanExecute` check plus the `Interlocked` guard means even if `CanExecute` returns `false` slowly, a second `Execute` call is ignored.

### IsRunning Binding

`IAsyncRelayCommand` implements `INotifyPropertyChanged` directly (not through `ObservableObject`). The `IsRunning` property raises `PropertyChanged` when it changes. You bind to it as:

```xml
<ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}" />
```

This binds to a property on the **command object**, not on the ViewModel. The binding path is `LoadDataCommand.IsRunning` — Avalonia walks `DataContext`, finds `LoadDataCommand`, then accesses `.IsRunning`. With compiled bindings, this requires `x:DataType` to include the command property:

```xml
<Window x:DataType="vm:MainViewModel">
  <ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}" />
</Window>
```

The compiler checks that `MainViewModel` has a property `LoadDataCommand` of type `IAsyncRelayCommand` (or `IRelayCommand`), and that type has a property `IsRunning`.

---

## 4. CancellationToken Injection: How the Async Command Receives It

```csharp
private CancellationTokenSource? _cts;

[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    await Task.Delay(5000, token);
}

[RelayCommand]
private void Cancel()
{
    _cts?.Cancel();
}
```

The source generator detects `CancellationToken` as a method parameter and **removes it** from the command's external parameter list. The generated command:

1. Creates a `CancellationTokenSource` internally (or reuses an existing one).
2. Passes `token` (from `CancellationTokenSource.Token`) to the method.
3. When the command's execution completes (success, fault, or cancellation), it disposes the token source.

The `Cancel` command calls `_cts?.Cancel()`. However, `_cts` is managed inside the async command — you do not own it directly. The correct pattern is:

```csharp
[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
    // Use _cts.Token for linked cancellation
    await Task.Delay(5000, _cts.Token);
}
```

Or, if you want multiple commands to share the same cancellation source, store it as a field and pass the token to the command:

```csharp
private CancellationTokenSource? _cts;

[RelayCommand]
private async Task LoadDataAsync()
{
    _cts = new CancellationTokenSource();
    try
    {
        await Task.Delay(5000, _cts.Token);
    }
    catch (OperationCanceledException) { }
}

[RelayCommand]
private void Cancel()
{
    _cts?.Cancel();
}
```

Here, `LoadDataAsync` does **not** have `CancellationToken` in its signature — it creates the source itself. The `Cancel` command cancels it. This is simpler and gives you full control over the token lifecycle.

---

## 5. IProgress<T> Injection

```csharp
[RelayCommand]
private async Task DownloadAsync(IProgress<double> progress)
{
    for (int i = 0; i <= 100; i++)
    {
        progress.Report(i / 100.0);
        await Task.Delay(50);
    }
}
```

The source generator detects `IProgress<T>` and wraps it with a `Progress<T>` instance that marshals the callback to the captured `SynchronizationContext` (typically the UI thread). The generated code:

```csharp
private async void Execute(object? parameter)
{
    var progress = new Progress<double>(OnProgressReported);
    await DownloadAsync(progress);
}
```

Where `OnProgressReported` is an internal callback that raises `PropertyChanged` for an auto-generated `Progress` property (untyped `double`). You bind the progress as:

```xml
<ProgressBar Value="{Binding DownloadCommand.Progress}" />
```

The `Progress` property on `IAsyncRelayCommand` is a `double` [0.0, 1.0] that updates as `progress.Report()` is called.

---

## 6. CanExecute Re-Evaluation with `[NotifyCanExecuteChangedFor]`

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name;

[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* ... */ }

private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
```

Without `[NotifyCanExecuteChangedFor]`, changing `_name` sets `Name`, raises `PropertyChanged(nameof(Name))`, but never calls `SaveCommand.NotifyCanExecuteChanged()`. The button remains in its previous enable/disable state.

The `[NotifyCanExecuteChangedFor]` attribute on `_name` generates:

```csharp
set
{
    if (!EqualityComparer<string>.Default.Equals(_name, value))
    {
        OnPropertyChanging(nameof(Name));
        _name = value;
        OnPropertyChanged(nameof(Name));
        SaveCommand.NotifyCanExecuteChanged(); // injected
    }
}
```

This ensures that every time `Name` changes, the command re-evaluates `CanSave()` and the button updates its enabled state.

### Complex CanExecute Dependencies

If `CanSave()` checks multiple properties, you need `[NotifyCanExecuteChangedFor]` on each one:

```csharp
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _name;

[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SaveCommand))]
private string _email;

[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { }

private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);
```

For computed properties that affect `CanExecute`, see the pattern in [002V — Command Binding](002-command-binding-verbose.md#7-canexecute-how-the-button-knows-when-to-re-evaluate).

---

## 7. When to Write a Manual `ICommand` Instead

`[RelayCommand]` covers 95% of cases. The remaining 5% where you need a manual `ICommand`:

1. **Commands that need to be static or singleton.** `[RelayCommand]` generates instance commands. If you need a command that is not tied to a ViewModel instance (e.g., a `SettingsCommand` shared across multiple windows), implement a custom `ICommand` as a singleton.

2. **Commands that wrap service calls with retry logic.** `AsyncRelayCommand` does not support retry. A custom command can implement `Execute` with a retry loop.

3. **Commands that need to defer execution.** A custom command might queue `Execute` calls and process them on a background thread, while `[RelayCommand]` executes immediately on the calling thread.

4. **Commands with complex `CanExecute` that depends on external events.** `[RelayCommand]` re-evaluates `CanExecute` only when `NotifyCanExecuteChanged()` is called. If your command's enabled state depends on timer ticks, network status, or other system events, a custom `ICommand` can subscribe to those events and raise `CanExecuteChanged` automatically.

---

## Common Mistakes

1. **Using `[RelayCommand]` on a `static` method.** The generator emits an error. Commands must be instance methods.
2. **Naming collision with `Execute` or `CanExecute`.** If you name your method `Execute`, the generated property is `ExecuteCommand`, which is fine, but the method name clashes with `ICommand.Execute` conceptually. Use descriptive names.
3. **Not handling `OperationCanceledException` in async commands with `CancellationToken`.** The async command does not catch `OperationCanceledException` — it propagates to `ExecutionTask`. If you don't handle it in the method or on the task, the exception goes unobserved. Always catch it inside the command method.
4. **Binding to `{Binding SaveCommand.IsRunning}` without `x:DataType` set.** Compiled binding fails because it cannot resolve `IsRunning` on `MainViewModel`. Set `x:DataType` on the ancestor element.
5. **Calling `NotifyCanExecuteChanged()` in a tight loop.** Each call evaluates `CanExecute` and updates all bound controls. For rapid changes (e.g., slider drag), batch the updates.

---

## See Also

- [008 — RelayCommand in Depth (original tutorial)](008-relay-command.md)
- [008X — RelayCommand in Depth (examples)](008-relay-command-examples.md)
- [002 — Command Binding](002-command-binding.md)
- [002V — Command Binding (verbose companion)](002-command-binding-verbose.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [007V — ObservableObject & ObservableProperty (verbose companion)](007-observable-object-property-verbose.md)
- [CommunityToolkit.Mvvm Docs: RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
