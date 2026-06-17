---
tier: basics
topic: relay-command
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 008-relay-command.md
---

```quiz
Q: When a method returns Task and is decorated with [RelayCommand], what interface does the generated property expose?
A. IRelayCommand — the Task return is erased and the command runs synchronously. || The generator maps Task-returning methods to IAsyncRelayCommand, preserving the asynchronous semantics.
B. IAsyncRelayCommand — the command automatically disables itself while the Task is running and re-enables when it completes. || (correct) || The generator detects the Task return type and produces an IAsyncRelayCommand whose CanExecute returns false during execution, preventing re-entrant invocation.
C. IAsyncRelayCommand — but the developer must manually toggle a bool field to prevent re-entry. || The auto-disabling is built into IAsyncRelayCommand; no manual bool is needed.
D. IRelayCommand<Task> — the consumer must await the command's result manually. || The generated property type is IAsyncRelayCommand, not a generic wrapping of Task.
Explanation: [RelayCommand] inspects the method return type. A `Task`-returning method generates an `IAsyncRelayCommand` property. The infrastructure calls `Execute` which returns the Task and tracks its completion; `CanExecute` returns `false` while the Task is still running. This automatic disable-on-run behavior is a key reason to use [RelayCommand] over hand-rolled commands for async operations.

Q: A [RelayCommand] method with signature `async Task DoWork(CancellationToken token)` — where does the CancellationToken come from at runtime?
A. The developer must pass the CancellationToken explicitly when binding the command in XAML. || CancellationToken is injected automatically; it is not set from XAML.
B. The source generator synthesizes a CancellationTokenSource tied to the command's lifetime and injects the token each time Execute is called. || (correct) || The generator creates a per-command CancellationTokenSource that is cancelled if the command is re-executed before completion or when the owning ViewModel is disposed, and passes that token into the method.
C. The CancellationToken is always `CancellationToken.None` unless the ViewModel implements ICancellableCommand. || The token is meaningful and tied to a CancellationTokenSource managed by the generated infrastructure.
D. The CancellationToken parameter is ignored by the generator; the method receives a default token. || The generator explicitly supports the CancellationToken parameter pattern and supplies a linked token.
Explanation: The source generator recognizes a `CancellationToken` parameter in the method signature. It creates a `CancellationTokenSource` per command and passes the token on every invocation. If the command is executed again before the previous Task completes, the earlier CancellationTokenSource is cancelled, signaling the prior operation to stop. This pattern properly supports cancellation without manual wiring.

Q: A method `Task DoWork(IProgress<double> progress)` is decorated with [RelayCommand]. How is the IProgress<T> provided?
A. The developer resolves IProgress<double> from DI and stores it as a field; the generator ignores it. || The generator injects a fresh Progress<T> automatically; no DI registration is needed.
B. The source generator creates a Progress<double> instance and injects it each time the command executes. || (correct) || The generator detects the IProgress<T> parameter and creates a Progress<T> backed by the dispatcher, ensuring progress callbacks marshal to the UI thread.
C. The XAML binding supplies the IProgress<double> through a binding parameter. || IProgress<T> is injected by the generator; it is not a XAML-binding concern.
D. The method receives null unless the ViewModel has a property named Progress. || The generator always supplies a valid Progress<T> instance when the parameter is present.
Explanation: The generator recognizes `IProgress<T>` parameters and creates a `Progress<T>` instance that posts progress reports through the dispatcher to the UI thread. This lets async methods report progress (e.g., percentage complete, status text) directly through the parameter without any ViewModel-level plumbing. The pattern works with any T — `double`, `int`, `string`, or a custom type.

Q: Given methods `bool CanDo()` and `void Do()`, which [RelayCommand] usage produces a conditional IRelayCommand?
A. `[RelayCommand(CanExecute = nameof(CanDo))]` on `Do()` — the generator wires CanExecute to the CanDo method. || (correct) || The CanExecute parameter points to a bool-returning method that the generated command calls to determine enabled state. Changes must still be signaled via NotifyCanExecuteChanged().
B. `[RelayCommand(AllowConcurrentExecutions = false)]` on `Do()` — the command checks CanDo automatically. || AllowConcurrentExecutions controls re-entry, not the enable/disable condition.
C. The generator infers CanDo from the method name `CanDo` / `Do` convention without any attribute parameter. || The convention-based pairing is not automatic; you must specify `CanExecute = nameof(...)` explicitly.
D. `[RelayCommand]` on both `CanDo()` and `Do()` — the generator pairs them by return type. || Both methods would each produce their own command property; the attribute does not pair them implicitly.
Explanation: Setting `[RelayCommand(CanExecute = nameof(CanDo))]` on `Do()` causes the generated command to call `CanDo()` before each execution. When the condition changes — for example, after a field update — the ViewModel should call `DoCommand.NotifyCanExecuteChanged()` (the generated command property) to re-query `CanDo()`. Without this notification, the UI binding will not re-evaluate CanExecute.
```
