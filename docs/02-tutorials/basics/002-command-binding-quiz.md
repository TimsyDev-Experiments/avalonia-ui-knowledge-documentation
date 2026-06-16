---
tier: basics
topic: command binding
estimated: 5-8 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 002-command-binding.md
---

# Quiz — Command Binding

```quiz
Q: When you apply [RelayCommand] to a method in a partial class, what gets generated?
A. An event handler delegate || Source generators do not produce event handlers.
B. A backing field and a public IRelayCommand property that lazy-initializes a RelayCommand wrapping the method (correct) || The generator creates `_methodNameCommand` and `MethodNameCommand => _methodNameCommand ??= new RelayCommand(MethodName)`.
C. A static method on the class || Commands are instance-level, not static.
D. A Button.Click handler in code-behind || The ViewModel has no reference to the view.
Explanation: [RelayCommand] generates a private backing field and a public property of type IRelayCommand. The property getter lazy-initializes a RelayCommand instance that delegates to the original method. The view binds to this property.
```

```quiz
Q: How does the Button auto-disable itself during an async command?
A. The Button polls CanExecute on a timer || Buttons subscribe to CanExecuteChanged, not polling.
B. IAsyncRelayCommand returns false from CanExecute while IsRunning is true, and raises CanExecuteChanged when IsRunning changes (correct) || The AsyncRelayCommand overrides CanExecute to return false during execution and fires CanExecuteChanged on the UI thread when the task completes.
C. The Button inspects the Task.Status property || Buttons do not access the Task directly.
D. [RelayCommand] sets a bool IsBusy property on the ViewModel || The async command manages its own running state internally via IsRunning — no ViewModel property needed.
Explanation: IAsyncRelayCommand overrides CanExecute() to return false while IsRunning is true. It raises CanExecuteChanged when the task finishes, so bound controls re-evaluate their enabled state. No manual IsBusy flag needed.
```

```quiz
Q: In the wizard example from 002X, the GoNextCommand has CanExecute = nameof(CanGoNext). When the user updates the Name field in step 1, why does the "Next" button stay in its previous state?
A. CanExecute only runs on the button's Click event || CanExecute runs when CanExecuteChanged fires, not on click.
B. CanGoNext has no way to know that Name changed || The method is evaluated, but the button won't re-query unless NotifyCanExecuteChanged is called.
C. The source generator does not automatically emit NotifyCanExecuteChanged for property changes — you must add [NotifyCanExecuteChangedFor(nameof(GoNextCommand))] on the dependent [ObservableProperty] (correct) || The generator only calls NotifyCanExecuteChanged where explicitly directed.
D. The button binding is one-way || Binding mode does not affect CanExecute re-evaluation.
Explanation: Without [NotifyCanExecuteChangedFor(nameof(GoNextCommand))] on `[ObservableProperty] private string _name`, the generated setter does not call GoNextCommand.NotifyCanExecuteChanged(). The button stays in its previous enable state until something else triggers re-evaluation.
```

```quiz
Q: In the search-with-debounce example from 002X, what prevents an old search result from overwriting a newer one when the user types rapidly?
A. A lock() around the search method || Locking does not help with async timing — it prevents concurrent execution, not stale results.
B. Each keystroke creates a new CancellationTokenSource and cancels the previous one — the cancelled task's TaskCanceledException is caught, so only the latest search updates results (correct) || _debounceCts?.Cancel() aborts the previous delay; the catch block discards stale work.
C. Thread.Sleep(300) blocks the UI thread || Blocking the UI thread would freeze the app entirely.
D. Task.Run queues the search on a background thread || The issue is which result gets applied, not where the work runs.
Explanation: The CancellationTokenSource pattern ensures only the most recent keystroke's work completes. Each new search cancels the previous token; the cancelled task throws TaskCanceledException, which is caught silently. The subsequent search runs to completion and updates FilteredItems.
```

```quiz
Q: Why must the class containing [RelayCommand] be declared partial?
A. Partial is required for the source generator to add generated members to the same class — the generator produces a separate partial declaration file (correct) || Without partial, the authored file and the generated file would define separate classes with the same name.
B. ObservableObject requires partial || ObservableObject works without partial when no source generators are used.
C. [RelayCommand] injects IL at compile time || It generates C# source code, not IL.
D. The XAML compiler requires partial || XAML compiler requirements are separate from source generator requirements.
Explanation: Source generators emit a separate .cs file with an additional partial declaration of the same class. Without the partial keyword, the compiler sees two distinct classes with the same fully-qualified name, which is a compilation error.
```
