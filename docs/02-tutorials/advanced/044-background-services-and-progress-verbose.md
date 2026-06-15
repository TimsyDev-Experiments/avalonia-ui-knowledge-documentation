---
tier: advanced
topic: threading and async
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 044-background-services-and-progress.md
---

# 044V — Background Services and Progress Reporting: An In-Depth Companion

**What you'll learn in this companion:** Not just how to wire a BackgroundService, but how the .NET Generic Host model interacts with Avalonia's dispatcher, why each progress-reporting pattern exists, the threading semantics of `IMessenger` vs `IProgress<T>`, what `Channel<T>` gives you over a lock-based queue, and why `PeriodicTimer` beats `Task.Delay` in a loop.

**Prerequisites:** [008 — RelayCommand](../basics/008-relay-command.md), [032 — Dependency Injection for MVVM](032-mvvm-di-wiring.md)

**You should already have read:** [044 — Background Services and Progress Reporting](044-background-services-and-progress.md) for the quick-start version. This file goes deeper on every section.

---

## 1. The Hosted Service Pattern — What It Actually Is

### The .NET Generic Host contract

`BackgroundService` (from `Microsoft.Extensions.Hosting.Abstractions`) is an abstract class with a single method to override:

```csharp
protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
```

The host calls `ExecuteAsync` once when the service starts and treats the returned `Task` as the service's lifetime. When the task completes, the host considers the service stopped. When the host itself shuts down, it sets `stoppingToken` to cancelled and waits for the task to complete (with a configurable shutdown timeout, default 5 seconds).

Key detail: `ExecuteAsync` runs on a **thread-pool thread**, not the UI thread. That's by design — the entire purpose of a background service is to keep CPU or I/O work off the UI thread.

### What `AddHostedService<T>()` does

```csharp
builder.Services.AddHostedService<FileWatcherService>();
```

This registration does two things:

1. Registers `FileWatcherService` as a singleton in DI
2. Adds it to the host's internal list of hosted services

When the host starts, it creates an instance of `FileWatcherService` (resolving its constructor dependencies from DI) and calls `StartAsync` on it. `StartAsync` is implemented by `BackgroundService` to call `ExecuteAsync` — you should not call `StartAsync` directly. The host calls `StopAsync` on shutdown, which triggers the `stoppingToken`.

### Why you never `await` ExecuteAsync

The host calls `ExecuteAsync` but does not await it — it captures the returned task and tracks it. This means the host's startup sequence is not blocked by your service's first iteration. Your service begins running immediately, and the host's `StartAsync` returns quickly.

### When not to use BackgroundService

`BackgroundService` is the right tool when you need a **long-running, continuous** loop: polling a queue, watching a directory, refreshing data on a timer. It is the wrong tool for:

- **One-shot background work** triggered by a user action — use `Task.Run` or `AsyncRelayCommand` with `ConfigureAwait(false)` instead
- **Short operations** that complete in milliseconds — just run them on the UI thread
- **CPU-bound computation** that outlives the service — use a separate `Task` or parallel processing

---

## 2. The File Watcher — Why Each Line Exists

```csharp
public sealed class FileWatcherService : BackgroundService
{
    private readonly ILogger<FileWatcherService> _logger;
    private readonly string _watchPath;

    public FileWatcherService(ILogger<FileWatcherService> logger)
    {
        _logger = logger;
        _watchPath = Path.Combine(Environment.CurrentDirectory, "incoming");
    }
```

`ILogger<FileWatcherService>` is injected through DI. The `WatchPath` is constructed at startup, not hardcoded — this makes it testable (you can inject a different base path later) and ensures the path is resolved relative to the running app, not the build machine.

```csharp
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FileWatcherService started, watching {Path}", _watchPath);
        Directory.CreateDirectory(_watchPath);

        while (!stoppingToken.IsCancellationRequested)
        {
```

The `stoppingToken` is the cancellation token the host controls. When the application shuts down, this token is signalled. Every blocking call inside the loop **must** accept this token so the service can exit promptly.

`Directory.CreateDirectory` is idempotent — it does nothing if the directory already exists. Calling it redundantly on each iteration is harmless but wasteful; it belongs at the start, outside the loop.

```csharp
            var files = Directory.GetFiles(_watchPath, "*.csv");
            foreach (var file in files)
            {
                _logger.LogInformation("Processing {File}", file);
                await Task.Delay(500, stoppingToken);
                File.Move(file, Path.ChangeExtension(file, ".done"));
            }

            await Task.Delay(3000, stoppingToken);
```

`Directory.GetFiles` is a synchronous I/O call. In a background service this is acceptable because the code is already off the UI thread. Using `Directory.EnumerateFiles` would be slightly better for memory (streaming vs buffering), but for a small directory it makes no measurable difference.

The `Task.Delay(3000, stoppingToken)` prevents a tight spin loop when the directory is empty. Without it, `GetFiles` would run thousands of times per second, burning CPU.

**Common mistake:** Forgetting to pass `stoppingToken` to `Task.Delay`. Without it, the delay won't be interrupted when the app shuts down, delaying the exit by up to 3 seconds.

---

## 3. IMessenger — Threading Contract

### The problem: background thread, UI-bound properties

When a background service calls `_messenger.Send(new ProgressMessage(...))`, the `Send` method invokes all registered handlers **synchronously on the caller's thread**. If the handler updates an `[ObservableProperty]`, the property-changed notification fires on the background thread. The binding system picks up the change, but when it tries to update the UI control, it throws `AccessViolationException` or silently fails — Avalonia controls have thread affinity.

### The solution: explicit dispatch

The tutorial shows the dispatch in the registration lambda:

```csharp
messenger.Register<ProgressMessage>(this, (r, m) =>
{
    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
    {
        ((MainViewModel)r).ProgressPercent = m.Percent;
        ((MainViewModel)r).ProgressStatus = m.Status;
    });
});
```

`Dispatcher.UIThread.Post` queues an action on the UI thread's message loop. It is fire-and-forget — it returns immediately and the action runs asynchronously. Use `Post` when you don't need to wait for the action to complete. Use `Dispatcher.UIThread.Invoke` when you need a return value or need to await completion (but beware deadlocks if the background thread holds a lock the UI thread needs).

### Why Post over Invoke from a background service

`Invoke` blocks the calling thread until the UI thread processes the action. If the UI thread is currently waiting on something this background service is doing (a classic deadlock cycle), `Invoke` deadlocks. `Post` cannot deadlock because it doesn't wait. In a `BackgroundService.ExecuteAsync`, always prefer `Post`.

### Why the example implements IRecipient<T> but doesn't use it

The `IRecipient<T>` interface exists for the `WeakReferenceMessenger`'s automatic registration feature. With `IRecipient<ProgressMessage>`, you could call `messenger.Register<ProgressMessage>(this)` and the messenger would call `Receive` on the registered `IRecipient<ProgressMessage>` when a message arrives.

The tutorial explicitly registers a lambda instead:

```csharp
messenger.Register<ProgressMessage>(this, (r, m) => { ... });
```

This gives direct control over the dispatch logic. If you use `IRecipient<T>.Receive`, the dispatch still needs to happen there — but the lambda approach keeps the dispatch inline with the registration. Both are valid; the lambda is clearer when you only have one message type to handle.

---

## 4. IProgress<T> / Progress<T> — Why It Marshals Automatically

### The SynchronizationContext capture

```csharp
var progress = new Progress<(int, string)>(pair =>
{
    (Percent, Status) = pair;
});
```

`Progress<T>` captures `SynchronizationContext.Current` at construction time. In Avalonia, the UI thread's sync context is `DispatcherSynchronizationContext`, which posts delegates to the dispatcher queue.

When `progress.Report(...)` is called from any thread, `Progress<T>` invokes the callback via the captured sync context — which means the callback always runs on the UI thread.

### Why this works without explicit dispatcher code

The capture happens at construction, not at invocation. As long as you construct `Progress<T>` on the UI thread (which is the case when a ViewModel command runs on the UI thread), the marshalling is automatic.

### The constraint: construction thread matters

If you construct `Progress<T>` on a background thread, `SynchronizationContext.Current` is null (thread-pool threads have no sync context by default), and the callback runs on the thread-pool thread that called `Report`. Always construct `Progress<T>` on the UI thread.

### IProgress<T> as an interface

`IProgress<T>` is the interface, `Progress<T>` is the concrete implementation. Your service should depend on `IProgress<T>` (or a specific typed wrapper) rather than constructing `Progress<T>` internally. This lets the ViewModel own the construction (and thus the thread-affinity decision):

```csharp
// Service — accepts IProgress<T>
public async Task RunAsync(IProgress<(int, string)> progress, CancellationToken ct)

// ViewModel — owns the Progress<T> construction
var progress = new Progress<(int, string)>(...);  // UI-thread construction
await _service.RunAsync(progress, ct);
```

---

## 5. Cancellation — Why Two Tokens, and Why OperationCanceledException

### CancellationTokenSource chains

```csharp
_cts = new CancellationTokenSource();
await _service.RunAsync(Progress, _cts.Token);
```

When the user clicks Cancel, `_cts.Cancel()` signals only this operation's token. The `stoppingToken` from `BackgroundService` is separate — it applies to the service lifetime, not individual operations.

In a real app, you would typically combine them:

```csharp
// Pass both the operation token and the service shutdown token
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    _cts.Token, stoppingToken);
await _service.RunAsync(Progress, linkedCts.Token);
```

`CreateLinkedTokenSource` creates a token that is cancelled when **either** source is cancelled. This prevents the service from blocking shutdown if a long-running operation is in progress.

### The OperationCanceledException pattern

```csharp
catch (OperationCanceledException)
{
    Status = "Cancelled";
}
```

When a method receives a cancelled token, it should throw `OperationCanceledException`. Not all methods do this automatically — you often need to explicitly call `token.ThrowIfCancellationRequested()` after a blocking operation completes:

```csharp
await Task.Delay(30, ct);
ct.ThrowIfCancellationRequested();  // Check after delay
```

Without this check, cancellation is only noticed at the next blocking call. If your loop has long-running non-blocking work between cancellable points, the cancellation is delayed.

### CancellationToken.None — when to use it

`CancellationToken.None` is a special token that never cancels. Use it when the operation should not be interrupted. In the tutorial's `StartAsync`, the run completes in about 3 seconds (100 × 30ms), so the risk is low. For longer operations, always pass a real token.

---

## 6. PeriodicTimer — Why Task.Delay in a Loop Is Wrong

### The drift problem

```csharp
// Bad: drifts
while (!stoppingToken.IsCancellationRequested)
{
    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
    var data = await _api.FetchCurrentAsync(stoppingToken);
}

// Good: no drift
using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    var data = await _api.FetchCurrentAsync(stoppingToken);
}
```

`Task.Delay` starts counting **after the await completes**. If `FetchCurrentAsync` takes 30 seconds, the actual interval becomes 5 minutes 30 seconds. Over time, the drift compounds — the operation happens later and later.

`PeriodicTimer` measures the interval from the **start** of the previous tick. If `FetchCurrentAsync` takes 30 seconds, `WaitForNextTickAsync` still completes 5 minutes after the previous tick started. The schedule does not drift.

### The accumulated-overrun problem

If `FetchCurrentAsync` takes longer than the interval (say 6 minutes for a 5-minute timer), `PeriodicTimer` does not stack up pending ticks. It simply makes the next `WaitForNextTickAsync` return immediately. `Task.Delay` in a loop would also not stack (because the delay is after the work), but the drift means the interval is effectively `work time + delay time`.

### What PeriodicTimer does not do

`PeriodicTimer` does not handle overlapping execution. If your work takes longer than the interval, the next tick starts immediately. If you need **no overlapping execution**, use a SemaphoreSlim or track whether work is in progress:

```csharp
private bool _isRunning;

while (await timer.WaitForNextTickAsync(stoppingToken))
{
    if (_isRunning) continue;  // skip this tick
    _isRunning = true;
    try { await WorkAsync(stoppingToken); }
    finally { _isRunning = false; }
}
```

---

## 7. Channel<T> — Lock-Free Producer/Consumer

### Why Channel over ConcurrentQueue or lock-based queues

`System.Threading.Channels.Channel<T>` implements the **producer/consumer pattern** without explicit locks. Internally, it uses `Monitor` for bounded channels but with a more efficient signaling mechanism than a hand-written lock + `AutoResetEvent`:

- **Bounded** channels (as in the tutorial) have a maximum capacity. When full, the producer can wait (`Wait` mode), drop the newest item (`DropNewest`), or drop the oldest (`DropOldest`).
- **Unbounded** channels have no capacity limit — the producer never blocks.

### ChannelReader and ChannelWriter — separation of concerns

```csharp
public ChannelWriter<string> Writer => _channel.Writer;
public ChannelReader<string> Reader => _channel.Reader;
```

Exposing the writer and reader separately lets you give the writer to producer classes and the reader to consumer classes — each can only do their half of the contract. This prevents accidental reads from a producer or writes from a consumer.

### Backpressure with BoundedChannelFullMode.Wait

```csharp
new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait }
```

`Wait` means the producer's `WriteAsync` call will **asynchronously block** until space is available. This creates backpressure: if the consumer is slower than the producer, the producer slows down to match. Without backpressure, an unbounded channel would consume all available memory.

### The await foreach pattern

```csharp
await foreach (var item in _queue.Reader.ReadAllAsync(stoppingToken))
```

`ReadAllAsync` returns an `IAsyncEnumerable<T>`. Each iteration of the loop asynchronously waits for the next item — it does not poll. When the channel is empty, the iteration suspends until an item is written. When the channel is completed (Writer.Complete is called), the loop exits.

This is more efficient than a `while (true)` + `TryRead` loop because it uses zero CPU while waiting.

---

## 8. Dispatcher Considerations — Choosing the Right Pattern

### When each pattern breaks

| Pattern | Breaks when |
|---|---|
| `Progress<T>` | Constructed off the UI thread |
| `IMessenger` + Post | Receiver accesses UI outside the dispatched action |
| `Dispatcher.UIThread.Post` | Called during app shutdown (queue is drained) |
| `AsyncRelayCommand` | Command body blocks for seconds without ConfigureAwait(false) |

### The DispatcherSynchronizationContext lifetime

Avalonia installs `DispatcherSynchronizationContext` on the UI thread when the `Window` is created. If you construct objects inside a `Task.Run` or in a `BackgroundService` constructor (which runs on a thread-pool thread), `SynchronizationContext.Current` is null.

Rule: if you see `await` without `ConfigureAwait(true)` on the UI thread, the continuation resumes on the captured `SynchronizationContext` (which is the dispatcher). On a background thread, the continuation resumes on a thread-pool thread.

### When to use InvokeAsync vs Post

```csharp
// Post — fire and forget, no return value
Dispatcher.UIThread.Post(() => { ... });

// InvokeAsync — await completion, gets result
var result = await Dispatcher.UIThread.InvokeAsync(() => SomeMethod());
```

`InvokeAsync` returns a `Task<T>` that completes when the action has executed. Use it when the background service needs confirmation the UI update was processed. But beware: if the UI thread is blocked, `InvokeAsync` waits, while `Post` does not.

---

## 9. Registration Order and Lifetime

### AddHostedService<T> is singleton

`AddHostedService<T>` registers the service as a singleton. All dependencies injected into the constructor are resolved at service creation time. If the dependency is scoped, the service effectively promotes it to singleton — avoid injecting scoped DbContexts directly.

### Injecting scoped services into a BackgroundService

```csharp
// Correct way: inject IServiceScopeFactory
public class MyService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // use db...
    }
}
```

Each execution iteration creates a fresh scope, preventing stale data tracking and thread-safety issues with DbContext.

---

## 10. Testing Background Services

A background service's `ExecuteAsync` is just a method that returns a `Task`. You can test it by:

1. Constructing the service with mock dependencies
2. Calling `StartAsync` (which triggers `ExecuteAsync`)
3. Manipulating state to exercise the loop
4. Calling `StopAsync` to signal the cancellation token
5. Asserting that the service produced the expected side effects

The challenge is that `ExecuteAsync` is an infinite loop. For testability, extract the loop body into a virtual or injectable method:

```csharp
public class FileWatcherService : BackgroundService
{
    // Protected for test override
    protected virtual Task ProcessFilesAsync(CancellationToken ct)
    {
        // loop body
    }
}
```

---

## Key Takeaways

- `BackgroundService` is a singleton that runs `ExecuteAsync` on a thread-pool thread — never access UI controls from it
- `IMessenger.Send` is synchronous and runs on the caller's thread; the receiver must `Post` to the dispatcher before touching `[ObservableProperty]`
- `Progress<T>` captures `SynchronizationContext` at construction — construct on the UI thread
- `PeriodicTimer` does not drift; `Task.Delay` in a loop does
- `Channel<T>` provides lock-free producer/consumer with backpressure; prefer `await foreach` for consumption
- Always pass `stoppingToken` to every blocking call; use `CreateLinkedTokenSource` to merge operation and service cancellation
- Test `ExecuteAsync` by calling `StartAsync`/`StopAsync` with mock dependencies
- Use `IServiceScopeFactory` to inject scoped services into `BackgroundService`

---

## See Also

- [044 — Background Services and Progress Reporting (original)](044-background-services-and-progress.md)
- [044X — Background Services and Progress Reporting (examples)](044-background-services-and-progress-examples.md)
- [032 — Dependency Injection for MVVM](032-mvvm-di-wiring.md)
- [008 — RelayCommand](../basics/008-relay-command.md)
- [.NET docs: BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/background-service)
- [.NET docs: Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)
- [.NET docs: PeriodicTimer](https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer)
