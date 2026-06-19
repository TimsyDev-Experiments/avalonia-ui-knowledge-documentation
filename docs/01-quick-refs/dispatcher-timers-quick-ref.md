---
topic: threading
estimated: 3 min read
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Dispatcher & Timers Quick Ref

## Dispatcher

```csharp
// Post work to UI thread
Dispatcher.UIThread.Post(() => UpdateUI(), DispatcherPriority.Background);

// Invoke and await result
var result = await Dispatcher.UIThread.InvokeAsync(() => ComputeSomething());

// Fire-and-forget
Dispatcher.UIThread.Post(() => DoWork());

// Check thread
if (!Dispatcher.UIThread.CheckAccess())
    // marshalling needed
```

## DispatcherPriority

| Priority | Use |
|---|---|
| `Sleep` | Only wake on I/O or timer |
| `Input` | Pointer / key events |
| `Normal` | Default bound/trigger work |
| `Render` | Layout / draw updates |
| `Background` | Low-priority background tasks |
| `ApplicationIdle` | After all UI work |
| `SystemIdle` | Last, after everything |

## DispatcherTimer

```csharp
var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal,
    (s, e) => PollStatus());
timer.Start();
// timer.Stop() to cancel
```

## System.Timers.Timer (background)

```csharp
var timer = new System.Timer.Timer(1000);
timer.Elapsed += async (s, e) =>
{
    var data = await FetchDataAsync();
    // Marshal back
    Dispatcher.UIThread.Post(() => UpdateUI(data));
};
timer.AutoReset = true;
timer.Start();
```

## AvaloniaSynchronizationContext

Automatically installed on the UI thread. Use `await` normally — continuations marshal back:

```csharp
await Task.Run(() => Compute());
UpdateUI();  // back on UI thread
```

## v12 multiple dispatcher support

Each `TopLevel` (window) can have its own dispatcher in multi-window scenarios. Use `TopLevel.Dispatcher` for window-specific dispatch.
