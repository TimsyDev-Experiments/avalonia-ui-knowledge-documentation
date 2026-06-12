---
tier: basics
topic: commands
estimated: 6 min
researched: 2026-06-11
avalonia-version: 12.0.4
---

# 008 — RelayCommand in Depth

**What you'll learn:** Sync vs async commands, CanExecute, cancellation, and command progress tracking.

**Prerequisites:** [002 — Command Binding](002-command-binding.md), [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)

---

## 1. Synchronous command

```csharp
[RelayCommand]
private void Save()
{
    // synchronous work
}
```

Generates: `IRelayCommand SaveCommand`. The button always enabled (unless `CanExecute` is specified).

---

## 2. Async command with auto-disabling

```csharp
[RelayCommand]
private async Task LoadDataAsync()
{
    await Task.Delay(2000);
}
```

Generates: `IAsyncRelayCommand LoadDataCommand`. The button disables while the task is running. Bind `IsRunning` to show a spinner:

```xml
<Button Content="{Binding LoadDataCommand.IsRunning, Converter={StaticResource BoolToRunningText}}"
        Command="{Binding LoadDataCommand}" />
```

Or display a progress indicator:

```xml
<ProgressBar IsIndeterminate="{Binding LoadDataCommand.IsRunning}"
             IsVisible="{Binding LoadDataCommand.IsRunning}" />
```

---

## 3. Command with CanExecute

```csharp
[ObservableProperty]
private string _name;

[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { /* ... */ }

private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
```

The button auto-disables when `CanSave()` returns `false`. The command re-evaluates `CanExecute` after any property change that might affect it — but for complex cases, call:

```csharp
SaveCommand.NotifyCanExecuteChanged();
```

---

## 4. Async with cancellation

```csharp
private CancellationTokenSource? _cts;

[RelayCommand]
private async Task LoadDataAsync(CancellationToken token)
{
    try
    {
        await Task.Delay(5000, token);
    }
    catch (OperationCanceledException) { /* handled */ }
}

[RelayCommand]
private void Cancel()
{
    _cts?.Cancel();
}
```

`CancellationToken` is injected automatically by the source generator when you add it as a method parameter.

---

## 5. Async command with progress reporting

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

`IProgress<T>` is injected automatically by the source generator.

---

## Generated command properties reference

| Method signature | Generated type | Behavior |
|---|---|---|
| `void Do()` | `IRelayCommand` | Always enabled |
| `bool CanDo()` + `void Do()` | `IRelayCommand` | Conditional |
| `Task DoAsync()` | `IAsyncRelayCommand` | Auto-disables, IsRunning |
| `Task DoAsync(T)` | `IAsyncRelayCommand<T>` | Parameter + auto-disable |

---

## Key Takeaways

- Async commands auto-disable while running — no manual `IsBusy` flags for simple cases
- `CancellationToken` and `IProgress<T>` are injected by the source generator
- Call `NotifyCanExecuteChanged()` after state changes that affect `CanExecute`
- Bind `IsRunning` for spinners or progress indicators

---

## See Also

- [002 — Command Binding](002-command-binding.md)
- [007 — ObservableObject & ObservableProperty](007-observable-object-property.md)
- [CommunityToolkit.Mvvm Docs: RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
