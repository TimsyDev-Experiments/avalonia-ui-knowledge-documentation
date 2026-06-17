---
tier: advanced
topic: background-services
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 044-background-services-and-progress.md
---

# Quiz — Background Services & Progress Reporting

```quiz
Q: Where does the ExecuteAsync method of a BackgroundService run?
A. On the UI thread by default || BackgroundService.ExecuteAsync runs on a thread-pool thread, not the UI thread.
B. On a background thread-pool thread — never touch UI directly (correct) || ExecuteAsync runs entirely on a non-UI thread; touching Avalonia controls or observable properties directly causes cross-thread exceptions.
C. On whichever SynchronizationContext was captured at service construction || There is no implicit SynchronizationContext capture — the service runs on a background thread.
D. On the main application thread before OnFrameworkInitializationCompleted || Background services start after the host is built and run on their own threads.
Explanation: BackgroundService.ExecuteAsync runs on a thread-pool (background) thread. The UI must never be accessed directly from this method.
```

```quiz
Q: Which approach automatically marshals progress callbacks to the UI thread without explicit Dispatcher.UIThread.Post?
A. IMessenger.Send from the background service || IMessenger.Send runs on the sender's thread — the receiver must dispatch manually.
B. IProgress<T> with a Progress<T> instance constructed on the UI thread (correct) || Progress<T> captures the SynchronizationContext at construction time (DispatcherSynchronizationContext in Avalonia) and invokes the callback on the UI thread automatically.
C. channel.Writer.WriteAsync from a background loop || Channel operations are thread-safe but do not provide any UI-thread marshaling.
D. PeriodicTimer with async callbacks || PeriodicTimer ticks on thread-pool threads and does not marshal to the UI.
Explanation: Progress<T> captures the current SynchronizationContext when constructed. When constructed on the UI thread, the callback always executes on the UI thread.
```

```quiz
Q: What is the advantage of PeriodicTimer over Task.Delay in a fixed-interval background loop?
A. PeriodicTimer runs on the UI thread || Both run on background threads; PeriodicTimer does not provide thread affinity.
B. PeriodicTimer does not drift and does not accumulate when the loop body takes longer than the interval (correct) || Task.Delay starts timing from the end of the previous iteration, causing drift. PeriodicTimer measures from the start and does not accumulate.
C. PeriodicTimer automatically cancels when the app exits || Both respect a CancellationToken; neither auto-cancels without one.
D. PeriodicTimer supports sub-millisecond intervals || Both support sub-millisecond intervals; the advantage is drift behavior, not precision.
Explanation: PeriodicTimer maintains a fixed interval by measuring from the start of each tick, preventing drift and accumulation that occurs with sequential Task.Delay calls.
```

```quiz
Q: When using IMessenger to communicate progress from a BackgroundService to a ViewModel, what must the receiver do?
A. Nothing — IMessenger automatically dispatches to the UI thread || IMessenger invokes the handler on the sender's thread, which is the background thread.
B. Call Dispatcher.UIThread.Post or Invoke before updating any observable properties (correct) || The message handler runs on the sender's background thread; observable property updates must be dispatched to the UI thread to avoid cross-thread exceptions.
C. Mark the ViewModel class with [ObservableObject] || The attribute enables source generators but does not affect threading.
D. Wrap the message handler in a lock statement || Locking does not change thread affinity and does not solve the cross-thread UI update issue.
Explanation: IMessenger handlers execute on the sender's thread. The receiver must explicitly dispatch property changes to the UI thread via Dispatcher.UIThread.Post.
```

```quiz
Q: Which registration method wires a queue-processing BackgroundService into the Avalonia DI container?
A. builder.Services.AddSingleton<QueueProcessor>() || Singleton registration creates the instance but does not start the background service lifecycle.
B. builder.Services.AddTransient<QueueProcessor>() || Transient services are created on demand and never have their ExecuteAsync started.
C. builder.Services.AddHostedService<QueueProcessor>() (correct) || AddHostedService registers the class as a singleton and calls IHostedService.StartAsync to begin execution.
D. builder.Services.AddScoped<QueueProcessor>() || Scoped services are tied to a scope and are not started automatically.
Explanation: AddHostedService<T> registers the service as a singleton and invokes its IHostedService.StartAsync method, which triggers the ExecuteAsync background loop.
```
