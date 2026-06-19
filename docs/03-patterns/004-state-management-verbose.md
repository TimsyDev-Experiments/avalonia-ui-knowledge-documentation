---
tier: reference
topic: architecture
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 004-state-management.md
---

# 004V — State Management: An In-Depth Companion

You should already have read: [004 — State Management](004-state-management.md) for the quick-start version. This file goes deeper on every section.

## Prerequisites

- Familiarity with DI registration (`AddSingleton`, `AddTransient`, `AddScoped`)
- Understanding of `record` types and `with` expressions
- Basic `IMessenger` usage (see [Pattern 014](../02-tutorials/intermediate/014-imessenger-patterns.md))

---

## 1. Shared Singleton Service — Deep Dive

The singleton event service is the most straightforward approach, but its simplicity hides several design decisions worth examining.

### Thread safety

The `AppState` class in the core file is not thread-safe. If a background operation (e.g., a file load on a thread-pool thread) writes to `Current`, the event fires on that background thread, and subscribers must marshal back to the UI thread:

```csharp
public sealed class AppState
{
    private readonly object _lock = new();
    private Workspace _current = Workspace.Default;
    private int _version;

    public event Action<Workspace>? WorkspaceChanged;

    public Workspace Current
    {
        get { lock (_lock) return _current; }
        set
        {
            Workspace snapshot;
            lock (_lock)
            {
                _current = value;
                _version++;
                snapshot = _current;
            }
            WorkspaceChanged?.Invoke(snapshot);
        }
    }

    public (Workspace Current, int Version) Snapshot()
    {
        lock (_lock) return (_current, _version);
    }
}
```

Even with the lock, event subscribers still need to dispatch to the UI thread:

```csharp
public ShellViewModel(AppState state)
{
    state.WorkspaceChanged += workspace =>
    {
        Dispatcher.UIThread.Post(() =>
        {
            CurrentWorkspace = workspace.Name;
        });
    };
}
```

### Multiple state properties

As the app grows, the singleton accumulates more properties and events. A common refactor is to group related state into nested objects:

```csharp
public sealed class AppState
{
    public WorkspaceState Workspace { get; } = new();
    public UserState User { get; } = new();
    public FeatureFlags Features { get; } = new();
}

public sealed class WorkspaceState
{
    private Workspace _current = Workspace.Default;
    public event Action<Workspace>? Changed;

    public Workspace Current
    {
        get => _current;
        set { _current = value; Changed?.Invoke(value); }
    }
}
```

Each ViewModel subscribes only to the sub-state it needs.

### Unsubscription and lifecycle

Singleton state lives for the lifetime of the application. ViewModels that subscribe to singleton events must unsubscribe when they are no longer active, or they will leak:

```csharp
public partial class ShellViewModel : ObservableObject, IDisposable
{
    private readonly AppState _state;

    public ShellViewModel(AppState state)
    {
        _state = state;
        state.WorkspaceChanged += OnWorkspaceChanged;
    }

    private void OnWorkspaceChanged(Workspace w)
    {
        Dispatcher.UIThread.Post(() => CurrentWorkspace = w.Name);
    }

    public void Dispose()
    {
        _state.WorkspaceChanged -= OnWorkspaceChanged;
    }
}
```

If you are using `ActivatorUtilities` or a DI container that calls `Dispose` on transient ViewModels, wire the disposal. If ViewModels are never disposed (singleton-scoped VMs), the leak may not matter, but be intentional about the choice.

---

## 2. Flux / Single-Store Pattern — Deep Dive

### Why `record` types matter

Records give you value equality and `with` expressions for free. In the Flux pattern, every state change produces a new object, so `with` is the idiomatic way to derive new state:

```csharp
var newState = state with { Items = updatedItems };
```

Value equality means the store can detect "no-op" dispatches and skip notification:

```csharp
public void Dispatch(Action action)
{
    TState newState;
    lock (_lock)
    {
        newState = _reducer(_state, action);
        if (EqualityComparer<TState>.Default.Equals(_state, newState))
            return; // no change, skip notification
        _state = newState;
        _version++;
    }
    StateChanged?.Invoke(newState);
}
```

### Middleware pipeline

A pure store can be extended with middleware (logging, undo, analytics) by wrapping the `Dispatch` method:

```csharp
public sealed class Store<TState>
{
    private readonly List<Func<Func<Action, TState>, Func<Action, TState>>> _middlewares = new();

    public Store<TState> Use(Func<Func<Action, TState>, Func<Action, TState>> middleware)
    {
        _middlewares.Add(middleware);
        return this;
    }

    public void Dispatch(Action action)
    {
        Func<Action, TState> pipeline = InnerDispatch;
        for (int i = _middlewares.Count - 1; i >= 0; i--)
            pipeline = _middlewares[i](pipeline);
        pipeline(action);
    }

    private TState InnerDispatch(Action action)
    {
        TState newState;
        lock (_lock)
        {
            newState = _reducer(_state, action);
            _state = newState;
            _version++;
        }
        StateChanged?.Invoke(newState);
        return newState;
    }
}
```

Usage:

```csharp
var store = new Store<AppState>(initialState, Reducer)
    .Use(LoggingMiddleware)
    .Use(UndoRedoMiddleware);
```

### Undo/redo

Because every action produces a new state snapshot, undo is a history walk:

```csharp
public sealed class UndoRedoMiddleware
{
    private readonly Stack<AppState> _undo = new();
    private readonly Stack<AppState> _redo = new();
    private const int MaxHistory = 50;

    public AppState Dispatch(Func<Action, AppState> next, Action action)
    {
        if (action is UndoAction)
        {
            if (_undo.Count == 0) return next(action);
            _redo.Push(next(action)); // push current before undoing
            var restored = _undo.Pop();
            return restored;
        }

        if (action is RedoAction)
        {
            if (_redo.Count == 0) return next(action);
            _undo.Push(next(action));
            return _redo.Pop();
        }

        var state = next(action);
        _undo.Push(state);
        _redo.Clear();
        return state;
    }
}
```

### Async actions

The core `Dispatch` is synchronous. For async operations (HTTP calls, disk I/O), dispatch a "request" action, then dispatch a "result" action on completion:

```csharp
public sealed record LoadItemsRequestAction : Action;
public sealed record LoadItemsResultAction(IReadOnlyList<TodoItem> Items) : Action;
public sealed record LoadItemsFailedAction(string Error) : Action;

// In ViewModel
[RelayCommand]
private async Task LoadItemsAsync()
{
    _store.Dispatch(new LoadItemsRequestAction());
    try
    {
        var items = await _service.GetAllAsync();
        _store.Dispatch(new LoadItemsResultAction(items));
    }
    catch (Exception ex)
    {
        _store.Dispatch(new LoadItemsFailedAction(ex.Message));
    }
}
```

The reducer handles each phase:

```csharp
public static AppState Reducer(AppState state, Action action) => action switch
{
    LoadItemsRequestAction  => state with { IsLoading = true, Error = null },
    LoadItemsResultAction a => state with { IsLoading = false, Items = a.Items },
    LoadItemsFailedAction a => state with { IsLoading = false, Error = a.Error },
    _                       => state
};
```

---

## 3. IMessenger-Based State — Deep Dive

### Registration styles

The core file shows lambda-based registration. The `IRecipient<T>` interface is the alternative:

```csharp
public partial class ShellViewModel : ObservableObject, IRecipient<WorkspaceChangedMessage>
{
    public ShellViewModel(IMessenger messenger)
    {
        messenger.Register(this);
    }

    public void Receive(WorkspaceChangedMessage message)
    {
        Dispatcher.UIThread.Post(() => WorkspaceName = message.Workspace.Name);
    }
}
```

The `IRecipient<T>` approach is cleaner when the handler logic is non-trivial and benefits from method extraction.

### Strong vs. WeakReferenceMessenger

The default `WeakReferenceMessenger` uses weak references to subscribers. This means:
- Subscribers can be garbage-collected without explicit unregistration
- But unregistered recipients may still receive messages if they are still alive

`StrongReferenceMessenger` keeps strong references — subscribers must explicitly `Unregister` or they will leak.

```csharp
// In App.axaml.cs or composition root
IMessenger messenger = new StrongReferenceMessenger();

// Or keep the default
IMessenger messenger = WeakReferenceMessenger.Default;
```

For long-lived ViewModels (e.g., shell, main window), either works. For transient views (popups, dialogs), prefer `WeakReferenceMessenger` or explicitly unregister:

```csharp
public sealed class EditDialogViewModel : IRecipient<ItemSavedMessage>, IDisposable
{
    private readonly IMessenger _messenger;

    public EditDialogViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        messenger.Register(this);
    }

    public void Receive(ItemSavedMessage message) { /* ... */ }

    public void Dispose() => _messenger.Unregister(this);
}
```

### Request messages (two-way)

Messages aren't limited to one-way notification. A request message carries a return value:

```csharp
public sealed class RequestCurrentUserMessage : RequestMessage<User>
{
}

// Sender
var user = messenger.Send(new RequestCurrentUserMessage());

// Receiver (must be registered)
public void Receive(RequestCurrentUserMessage message)
{
    message.Reply(_currentUser);
}
```

This is useful when a child ViewModel needs data from a parent without tight coupling.

### Token-based filtering

Messages can be filtered by token to limit which subscribers receive them:

```csharp
// Define tokens
public static class MessageChannels
{
    public const string WorkspaceA = "WorkspaceA";
    public const string WorkspaceB = "WorkspaceB";
}

// Send to a specific channel
messenger.Send(new WorkspaceChangedMessage(ws), MessageChannels.WorkspaceA);

// Register on a specific channel
messenger.Register<WorkspaceChangedMessage>(this, MessageChannels.WorkspaceA, (r, m) =>
{
    ((ShellViewModel)r).WorkspaceName = m.Workspace.Name;
});
```

This is valuable when the same message type is used in multiple contexts.

---

## 4. Comparison Deep Dive

| Dimension | Singleton Event Service | Flux/Store | IMessenger |
|---|---|---|---|
| **State location** | Centralized in one or few singletons | Single `TState` record | Distributed (senders push messages) |
| **Change propagation** | .NET events | Store dispatches `StateChanged` | Message bus pub/sub |
| **State immutability** | Optional (mutating setters) | Required (record with expressions) | Not enforced |
| **Debugging** | Hard (which subscriber caused a change?) | Easy (every action is an object) | Medium (no central log of messages) |
| **Undo/redo** | Manual implementation | Built-in via history stack | Very difficult |
| **Threading model** | Must lock; events on writer thread | Lock in Dispatch; event on writer thread | Message sent on sender's thread |
| **DI registration** | `AddSingleton` | `AddSingleton<Store<AppState>>` | `AddSingleton<IMessenger>` |
| **Boilerplate per action** | One event, one property setter | One record + one reducer arm | One message record + one handler |

### When to evolve from singleton to Flux

Signs that the singleton approach is straining:

1. You need undo/redo
2. Multiple properties change together and subscribers must react to the batch atomically
3. You want to log or replay state changes (time-travel debugging)
4. The event list in `AppState` exceeds 5–7 events
5. You need middleware (e.g., persist state to disk on every change)

---

## 5. Hybrid Approaches

### Singleton state with Flux-style notifications

Use a singleton for state but dispatch action records instead of raw events:

```csharp
public sealed class AppState
{
    private readonly Subject<IAction> _actions = new();

    public IObservable<IAction> Actions => _actions;

    public Workspace Current { get; private set; } = Workspace.Default;

    public void Dispatch(IAction action)
    {
        Apply(action);
        _actions.OnNext(action);
    }

    private void Apply(IAction action)
    {
        if (action is SetWorkspaceAction a)
            Current = a.Workspace;
    }
}
```

ViewModels subscribe to the observable stream:

```csharp
public ShellViewModel(AppState state)
{
    state.Actions
        .OfType<SetWorkspaceAction>()
        .Subscribe(action => Dispatcher.UIThread.Post(() =>
            CurrentWorkspace = action.Workspace.Name));
}
```

This gives you traceability (every action is an object) without a full immutable store.

### ReactiveUI-style `SourceCache`

If you are using ReactiveUI, `SourceCache<TKey, T>` provides an observable collection with transactional updates:

```csharp
public sealed class TodoStore
{
    private readonly SourceCache<TodoItem, Guid> _items =
        new(x => x.Id);

    public IObservable<IChangeSet<TodoItem, Guid>> Connect() =>
        _items.Connect();

    public void Add(TodoItem item) => _items.AddOrUpdate(item);
    public void Remove(Guid id) => _items.Remove(id);
    public IObservableCache<TodoItem, Guid> Cache => _items;
}
```

This is a middle ground between singleton state and full Flux.

---

## Key Takeaways (Expanded)

- **Start simple** — a singleton event service is fine for apps with fewer than 5 ViewModels
- **Records are your friend** — the `with` expression makes immutable updates ergonomic in the Flux pattern
- **Thread awareness** — state mutations often happen on background threads; always marshal to `Dispatcher.UIThread` before updating bound properties
- **IMessenger is not a state container** — it's a notification bus. Store the actual state somewhere (singleton, store, database) and use messages to announce changes
- **Middleware unlocks power** — logging, undo, and persistence can all be composed as middleware around a core store
- **Know when to evolve** — the moment you need undo or time-travel debugging, adopt Flux
