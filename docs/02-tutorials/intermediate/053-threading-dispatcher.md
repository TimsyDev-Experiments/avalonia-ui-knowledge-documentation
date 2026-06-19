---
tier: intermediate
topic: threading
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 053 — Threading & Dispatcher

**What you'll learn:** How Avalonia's single-threaded UI model works, how to marshal work to the UI thread with `Dispatcher`, and when to use timers, `async`/`await`, and background threads.

**Prerequisites:** [001 — Project Setup](../basics/001-project-setup.md)

---

## 1. The UI thread rule

Avalonia uses a single-threaded UI model. All control creation, property reads/writes, layout, and rendering must happen on the UI thread. Accessing a control from a background thread throws `InvalidOperationException` ("Call from invalid thread").

```csharp
// WRONG — throws on background thread
await Task.Run(() => StatusText.Text = "Done");

// CORRECT — marshal to UI thread first
var result = await Task.Run(() => ComputeExpensiveResult());
StatusText.Text = result;  // back on UI thread after await
```

---

## 2. Dispatcher.UIThread

`Dispatcher.UIThread` provides access to the UI thread's dispatcher from anywhere in your code.

### Post (fire-and-forget)

Schedule a callback that runs on the UI thread. Returns immediately — no result to await.

```csharp
Dispatcher.UIThread.Post(() =>
{
    StatusText.Text = "Processing complete";
});
```

### InvokeAsync (await result)

Schedule a callback and return a `Task<T>` that completes when the callback finishes.

```csharp
var text = await Dispatcher.UIThread.InvokeAsync(() => SearchBox.Text);
```

### CheckAccess / VerifyAccess

Test whether you are already on the UI thread:

```csharp
if (Dispatcher.UIThread.CheckAccess())
{
    UpdateDirectly();     // already on UI thread
}
else
{
    Dispatcher.UIThread.Post(() => UpdateDirectly());
}

// Throws if not on UI thread:
Dispatcher.UIThread.VerifyAccess();
```

---

## 3. DispatcherPriority

Controls when a dispatched work item runs relative to other queued items.

| Priority | Description |
|----------|-------------|
| `Send` | Before other async operations |
| `Normal` | Normal priority |
| `Render` | Same priority as rendering |
| `Input` | Same priority as input |
| `Background` | After non-idle operations |
| `ApplicationIdle` | When the app is idle |
| `SystemIdle` | When the system is idle |

```csharp
Dispatcher.UIThread.Post(() => UpdateStatus(), DispatcherPriority.Background);
```

Default priority for `Post` and `InvokeAsync` is `Normal`/`Default`.

---

## 4. AvaloniaSynchronizationContext

When you `await` a `Task` in an async method that started on the UI thread, execution resumes on the UI thread automatically. Avalonia installs an `AvaloniaSynchronizationContext` that captures the UI thread context.

```csharp
private async void OnLoadClick(object? sender, RoutedEventArgs e)
{
    LoadButton.IsEnabled = false;

    // This runs on a thread-pool thread:
    var data = await Task.Run(() => LoadLargeDataSet());

    // This runs back on the UI thread (thanks to SynchronizationContext):
    Items = new ObservableCollection<Item>(data);
    LoadButton.IsEnabled = true;
}
```

Do NOT use `ConfigureAwait(false)` in code that needs to resume on the UI thread — it bypasses the synchronization context and the continuation would run on a thread-pool thread.

---

## 5. DispatcherTimer

For periodic UI updates. The callback runs on the UI thread.

```csharp
var timer = new DispatcherTimer
{
    Interval = TimeSpan.FromSeconds(1)
};

timer.Tick += (sender, e) =>
{
    ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
};

timer.Start();
// timer.Stop() when done
```

`DispatcherTimer` runs on the UI thread. `System.Timers.Timer` runs on a thread-pool thread and requires dispatching to touch UI.

| Timer type | Tick runs on | Use for |
|------------|-------------|---------|
| `DispatcherTimer` | UI thread | UI updates, animations |
| `System.Timers.Timer` | Thread pool | Background work, periodic data sync |

---

## 6. Background thread safety rules

- **DO** load data, serialize/deserialize, or compute on background threads
- **DO** marshal results to the UI thread before touching controls
- **DO NOT** read or write control properties from background threads
- **DO NOT** modify `ObservableCollection` from a background thread (silent data loss)
- **DO NOT** block the UI thread with `.Result` or `.Wait()` on async operations (deadlock risk)

```csharp
// Safe pattern: background load → UI update
var data = await Task.Run(() => LoadItems());
Items = new ObservableCollection<Item>(data);

// Safe pattern: incremental UI updates
foreach (var item in loadedItems)
{
    await Dispatcher.UIThread.InvokeAsync(() => Items.Add(item));
}
```

---

## 7. v12: multiple-dispatcher support

Avalonia 12 supports multiple dispatchers in advanced scenarios (e.g., rendering on a secondary thread, hosting multiple windows on separate threads). In most applications, `Dispatcher.UIThread` is the only dispatcher you need.

Library authors can use `AvaloniaObject.Dispatcher` (captured per-instance at creation time) instead of assuming `Dispatcher.UIThread`:

```csharp
myControl.Dispatcher.Post(() => myControl.IsVisible = false);
```

`Dispatcher.FromThread(Thread.CurrentThread)` returns the dispatcher for a given thread without creating one.

---

## 8. `Dispatcher.Yield` and `Resume`

Pause the current async method to let the dispatcher process pending work:

```csharp
private async Task ProcessItemsAsync(IList<Item> items)
{
    foreach (var item in items)
    {
        ProcessItem(item);
        await Dispatcher.Yield();  // let input/rendering process
    }
}
```

`Resume` works on a specific dispatcher instance:

```csharp
await myControl.Dispatcher.Resume(DispatcherPriority.Background);
```

---

## Key Takeaways

- All UI access must happen on the UI thread
- `Dispatcher.UIThread.Post` for fire-and-forget; `InvokeAsync` for awaiting results
- `AvaloniaSynchronizationContext` resumes `await` continuations on the UI thread automatically
- Use `DispatcherTimer` for UI-bound periodic work; `System.Timers.Timer` for background work
- Never block the UI thread with `.Result` or `.Wait()`
- v12 supports multiple dispatchers; library authors should prefer `AvaloniaObject.Dispatcher`

---

## See Also

- [053V — Threading & Dispatcher (verbose companion)](053-threading-dispatcher-verbose.md)
- [053E — Threading & Dispatcher (examples)](053-threading-dispatcher-examples.md)
- [Avalonia Docs: Threading Model](https://docs.avaloniaui.net/docs/app-development/threading)
- [Avalonia API: Dispatcher](https://docs.avaloniaui.net/api/avalonia/threading/dispatcher)
