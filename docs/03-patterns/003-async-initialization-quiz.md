---
title: Quiz
topic: 003-async-initialization
type: quiz
---

# Quiz: Async Initialization Patterns

```quiz
Q: What is the primary danger of using fire-and-forget (`_ = SomeMethodAsync()`) for startup initialization in Avalonia?
A. The method runs synchronously instead of asynchronously || Fire-and-forget still runs the task asynchronously on the thread pool
B. Unobserved exceptions from the task can crash the process silently (correct) || Fire-and-forget tasks that throw exceptions on the synchronization context are unobserved — the exception may crash the application or be silently swallowed depending on the runtime version
C. The UI thread blocks until the method completes || Fire-and-forget does not block — that is the problem, the task runs independently
D. The method cannot use `await` || Fire-and-forget methods can use `await` internally

Explanation: Fire-and-forget tasks (`_ = Task`) have no exception handling. If the task throws an exception, it goes unobserved. In .NET Core and later, unobserved task exceptions may still terminate the process depending on the `TaskScheduler.UnobservedTaskException` configuration. Always add explicit error handling or use structured patterns like `IAsyncInitialization`.
```

```quiz
Q: In a lazy-loading scenario, why should you use a `SemaphoreSlim` guard instead of a simple `if (_loaded) return;` check?
A. `SemaphoreSlim` is faster than a boolean check || A boolean check is faster — SemaphoreSlim adds overhead for thread safety
B. A boolean check can race when the user clicks the navigation button multiple times rapidly (correct) || Multiple clicks can create concurrent `async` calls that all pass the `if (_loaded)` check before any of them set `_loaded = true`, causing duplicate loading and potential data corruption
C. `SemaphoreSlim` provides better memory locality || Memory locality is not relevant here
D. A boolean check may be optimized away by the JIT compiler || The JIT does not remove boolean checks in this pattern

Explanation: When a user clicks rapidly, multiple async calls can pass the `if (_loaded)` check simultaneously because `_loaded` has not been set yet. A `SemaphoreSlim` ensures only one call proceeds. The double-check pattern — check `_loaded`, acquire the lock, check `_loaded` again — is the standard solution.
```

```quiz
Q: What is the purpose of batching items in memory before adding them to an `ObservableCollection<T>` during background data streaming?
A. To reduce the number of allocations || Batching creates a temporary buffer, which adds allocations
B. To avoid overwhelming the UI thread with too many individual collection-change notifications (correct) || Each `Add` call raises a `NotifyCollectionChanged` event that triggers UI re-layout. Adding items one at a time in a tight loop floods the dispatcher and makes the UI unresponsive even though the async method does not block
C. To comply with Avalonia's data-binding requirements || Avalonia does not require batching for data binding
D. To reduce memory usage || Batching increases memory usage due to the temporary buffer

Explanation: `ObservableCollection.Add` raises a collection-changed event on every call. Adding thousands of items individually generates thousands of layout passes. Batching items and adding them in groups (50-200) significantly reduces the number of UI updates while still keeping the UI responsive.
```

```quiz
Q: Which pattern is most appropriate when your application needs to show a branded window with progress for 3-5 seconds at startup, then transition to the main UI?
A. Lazy loading with per-feature spinners || Lazy loading shows the main window immediately — the user would see an empty shell before anything loads
B. Background data prep with `IAsyncEnumerable` || Background prep is for continuous streaming, not bounded startup work
C. Splash screen with incremental progress reporting (correct) || A splash screen provides branded feedback during the startup window and transitions to the main UI when initialization completes
D. `IAsyncInitialization` with fire-and-forget || Fire-and-forget would not block the window from appearing but provides no progress feedback or completion coordination

Explanation: A splash screen is purpose-built for this scenario. It gives the user immediate visual feedback (branding + progress) while the application loads. The splash closes and the main window opens when initialization completes. This is the standard pattern for startup sequences that take several seconds.
```

```quiz
Q: What does the `IAsyncInitialization` interface provide that a simple Task-returning method does not?
A. Automatic cancellation support || The interface does not include cancellation — that is handled separately via `CancellationToken`
B. A standardised contract that can be composed, awaited, and tracked by the composition root (correct) || `IAsyncInitialization` exposes the initialization Task as a property, which lets the composition root await it, compose multiple initializations, and track completion status without knowing the concrete ViewModel type
C. Automatic progress reporting || Progress reporting requires `IProgress<T>` — the interface only exposes the Task
D. Built-in exception handling || The interface does not catch exceptions — the initializer must handle them

Explanation: `IAsyncInitialization` standardises how asynchronous initialization is exposed across ViewModels. The composition root can treat all ViewModels uniformly — await them, compose them, report combined status — without knowing their concrete types. The pattern separates the *fact* of initialization from the *implementation* of initialization.
```
