---
tier: reference
topic: architecture
estimated: 10 min
researched: 2026-06-13
avalonia-version: 12.0.4
---

# Pattern 004 -- State Management

## Problem

Multiple ViewModels need to share and react to the same application state (e.g., current user, selected workspace, feature flags). Passing state through constructors or property chains creates tight coupling and makes state changes hard to trace.

## Solution

Three approaches, ordered by complexity:

---

## 1. Shared singleton service (simplest)

```csharp
public sealed class AppState
{
    public event Action<Workspace>? WorkspaceChanged;

    private Workspace _current = Workspace.Default;
    public Workspace Current
    {
        get => _current;
        set
        {
            _current = value;
            WorkspaceChanged?.Invoke(value);
        }
    }
}

// Register as singleton
builder.Services.AddSingleton<AppState>();
```

Subscribe in any ViewModel:

```csharp
public partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(AppState state)
    {
        state.WorkspaceChanged += workspace =>
        {
            // No dispatcher needed if AppState is always updated from UI thread
            CurrentWorkspace = workspace.Name;
        };
    }

    [ObservableProperty]
    private string _currentWorkspace = "";
}
```

**When to use:** Small apps, 2–3 ViewModels, mutable state is acceptable.

---

## 2. Flux / single-store pattern

Inspired by Redux: all state lives in one immutable store, ViewModels dispatch actions, and the store emits new state snapshots.

```csharp
// ─── State ──────────────────────────────────────────────────────────
public record AppState(
    IReadOnlyList<TodoItem> Items,
    string? SelectedFilter,
    bool IsLoading
);

// ─── Actions ────────────────────────────────────────────────────────
public abstract record Action;
public sealed record AddItemAction(string Title) : Action;
public sealed record ToggleItemAction(Guid Id) : Action;
public sealed record SetFilterAction(string? Filter) : Action;

// ─── Store ──────────────────────────────────────────────────────────
public sealed class Store<TState>
{
    private readonly object _lock = new();
    private TState _state;
    private readonly Func<TState, Action, TState> _reducer;
    private int _version;

    public event Action<TState>? StateChanged;

    public Store(TState initialState, Func<TState, Action, TState> reducer)
    {
        _state = initialState;
        _reducer = reducer;
    }

    public TState GetState() { lock (_lock) return _state; }

    public void Dispatch(Action action)
    {
        TState newState;
        lock (_lock)
        {
            newState = _reducer(_state, action);
            _state = newState;
            _version++;
        }
        StateChanged?.Invoke(newState);
    }
}
```

### Reducer

```csharp
public static AppState Reducer(AppState state, Action action) => action switch
{
    AddItemAction a     => state with
    {
        Items = state.Items.Append(new TodoItem(Guid.NewGuid(), a.Title)).ToList()
    },
    ToggleItemAction a  => state with
    {
        Items = state.Items.Select(i =>
            i.Id == a.Id ? i with { IsComplete = !i.IsComplete } : i).ToList()
    },
    SetFilterAction a   => state with { SelectedFilter = a.Filter },
    _                   => state
};
```

### ViewModel subscription

```csharp
public partial class TodoListViewModel : ObservableObject
{
    private readonly Store<AppState> _store;

    [ObservableProperty]
    private IReadOnlyList<TodoItem> _items = Array.Empty<TodoItem>();

    public TodoListViewModel(Store<AppState> store)
    {
        _store = store;
        _store.StateChanged += OnStateChanged;
        OnStateChanged(_store.GetState());
    }

    private void OnStateChanged(AppState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Items = state.SelectedFilter is null
                ? state.Items
                : state.Items.Where(i => !i.IsComplete).ToList();
        });
    }

    [RelayCommand]
    private void Toggle(Guid id) => _store.Dispatch(new ToggleItemAction(id));

    [RelayCommand]
    private void Add(string title) => _store.Dispatch(new AddItemAction(title));
}
```

**When to use:** Medium-to-large apps, undo/redo requirements, team needs a single source of truth.

---

## 3. IMessenger-based state sharing (MVVM-native)

```csharp
// In AppState service
public sealed class AppState
{
    private readonly IMessenger _messenger;

    public AppState(IMessenger messenger) => _messenger = messenger;

    public async Task SetWorkspaceAsync(Workspace workspace)
    {
        await Task.Delay(100); // simulate save
        _messenger.Send(new WorkspaceChangedMessage(workspace));
    }
}

// Message record
public sealed record WorkspaceChangedMessage(Workspace Workspace);

// ViewModel
public partial class ShellViewModel : ObservableObject, IRecipient<WorkspaceChangedMessage>
{
    [ObservableProperty]
    private string _workspaceName = "";

    public ShellViewModel(IMessenger messenger)
    {
        messenger.Register<WorkspaceChangedMessage>(this, (r, m) =>
        {
            ((ShellViewModel)r).WorkspaceName = m.Workspace.Name;
        });
    }

    void IRecipient<WorkspaceChangedMessage>.Receive(WorkspaceChangedMessage message)
    {
        // Not used — using lambda above
    }
}
```

**When to use:** Already using IMessenger for other cross-VM communication; decoupled but less traceable.

---

## Comparison

| Approach | Traceability | Boilerplate | Best for |
|---|---|---|---|
| Singleton event service | Low | Minimal | Small apps, quick MVPs |
| Flux / single-store | High (actions are logged) | Medium | Medium–large apps, undo/redo |
| IMessenger | Low (pub/sub is fire-and-forget) | Low | Decoupled components |

## Key takeaways

- Singleton service with events is the simplest start — refactor to Flux when state gets complex
- Flux store with `record` types and a single reducer makes state changes fully traceable
- Always dispatch state notifications to the UI thread when ViewModels observe them
- IMessenger works well but provides no built-in ordering or logging of state changes

---

## See Also

- [014 -- IMessenger Patterns](../02-tutorials/intermediate/014-imessenger-patterns.md)
- [032 -- Dependency Injection for MVVM](../02-tutorials/advanced/032-mvvm-di-wiring.md)
