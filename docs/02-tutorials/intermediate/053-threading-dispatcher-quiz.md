---
tier: intermediate
topic: threading
estimated: 5-8 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 053-threading-dispatcher.md
---

# Quiz — Threading & Dispatcher

```quiz
Q: What happens if you access a control property from a background thread in Avalonia?
A. The call succeeds but the UI update is silently deferred. || Avalonia does not defer; it throws immediately.
B. An InvalidOperationException is thrown with the message "Call from invalid thread." (correct) || Avalonia throws InvalidOperationException when a background thread attempts to access a control. This is the same behavior as WPF.
C. The application crashes with an AccessViolationException. || The exception is a managed InvalidOperationException, not an access violation.
D. Nothing — it works fine. || Avalonia enforces thread affinity. Background thread access is not allowed.
Explanation: Avalonia's single-threaded UI model throws InvalidOperationException when controls are accessed from non-UI threads.
```

```quiz
Q: You need to update a TextBlock from a background thread and do not need to wait for the update to complete. Which method should you use?
A. Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = "Done") || InvokeAsync is correct but it returns a Task you can await. For fire-and-forget, Post is simpler.
B. Dispatcher.UIThread.Post(() => StatusText.Text = "Done") (correct) || Post schedules the callback on the UI thread and returns immediately with no awaitable result — ideal for fire-and-forget UI updates.
C. Task.Run(() => StatusText.Text = "Done") || Task.Run runs on a thread-pool thread, not the UI thread — this would throw.
D. new DispatcherTimer(() => StatusText.Text = "Done") || A DispatcherTimer requires configuration (interval, start) and is not a single-shot dispatch mechanism.
Explanation: Post is the fire-and-forget dispatch method. It queues work on the UI thread and returns void immediately.
```

```quiz
Q: Which of the following code snippets correctly loads data on a background thread and updates the UI afterward?
A. var data = await Task.Run(() => LoadData()); Items = data; || If this started on the UI thread, the await resumes on the UI thread via SynchronizationContext. (correct)
B. var data = await Task.Run(() => LoadData()).ConfigureAwait(false); Items = data; || ConfigureAwait(false) bypasses the SynchronizationContext. The continuation runs on a thread-pool thread, so Items = data would throw.
C. Task.Run(() => { var data = LoadData(); Items = data; }); || Items is set from a thread-pool thread — throws InvalidOperationException.
D. var data = LoadData(); Items = data; || LoadData runs synchronously on the UI thread, blocking it.
Explanation: await preserves the SynchronizationContext by default, resuming on the UI thread. ConfigureAwait(false) would break this.
```

```quiz
Q: A background thread modifies an ObservableCollection that is bound to a ListBox. What happens?
A. The ListBox updates automatically. || ObservableCollection changes raise events on the modifying thread, which is not the UI thread.
B. The application throws an InvalidOperationException. || Not always — ObservableCollection does not enforce thread affinity itself.
C. Items may be silently dropped or only partially added. (correct) || ObservableCollection does not automatically marshal to the UI thread. When modified from a background thread, collection change notifications may be lost, resulting in silent data loss.
D. The application deadlocks. || No deadlock — notifications are simply lost or corrupted.
Explanation: Modifying an ObservableCollection from a background thread can cause silent data loss. Always dispatch collection modifications to the UI thread.
```

```quiz
Q: When should you use DispatcherTimer instead of System.Timers.Timer?
A. When you need the timer callback to run on the UI thread for direct UI updates. (correct) || DispatcherTimer's tick runs on the UI thread, making it the right choice for UI-bound periodic work like clock updates or animation progress.
B. When you need the timer to fire at precise microsecond intervals. || Both timers have ~15ms accuracy; neither is microsecond-precise.
C. When the timer interval is longer than 1 minute. || Either timer works for long intervals.
D. When you are running the timer in a headless test environment. || Headless tests may not run a dispatcher main loop; DispatcherTimer may not tick.
Explanation: DispatcherTimer integrates with the dispatcher main loop and fires on the UI thread. Use it whenever your periodic callback touches controls.
```

```quiz
Q: What does Dispatcher.Yield() do in an async method?
A. It pauses the method for exactly 1 second. || Yield does not pause for a fixed duration.
B. It queues the continuation on the dispatcher, allowing pending events to process before resuming. (correct) || Yield creates an awaitable that schedules the continuation on the current thread's dispatcher, letting the dispatcher process input, rendering, and other queued work before resuming.
C. It cancels the current task. || Yield does not cancel anything.
D. It throws if called from the UI thread. || Yield must be called from the dispatcher thread — it yields the dispatcher's processing to other work.
Explanation: Yield is the dispatcher equivalent of Task.Yield() — it interrupts the current method to let other queued work run, then resumes.
```

```quiz
Q: A library control needs to work correctly in both single-dispatcher (desktop) and multiple-dispatcher (v12+) environments. How should it dispatch work?
A. Always use Dispatcher.UIThread || UIThread assumes there is exactly one UI dispatcher — it may not be the right dispatcher for this control.
B. Use this.Dispatcher (the AvaloniaObject.Dispatcher property) (correct) || Each AvaloniaObject captures the dispatcher for the thread it was created on. Using this.Dispatcher ensures the work runs on the correct dispatcher regardless of the environment.
C. Use Dispatcher.CurrentDispatcher || CurrentDispatcher returns the dispatcher for the calling thread, which may not be the control's dispatcher.
D. Use TaskScheduler.FromCurrentSynchronizationContext() || This captures the current SynchronizationContext at the call site, not the control's dispatcher.
Explanation: AvaloniaObject.Dispatcher is the portable choice for library code. It captures the dispatcher at control-creation time, working correctly in single and multiple-dispatcher scenarios.
```
