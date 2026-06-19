---
tier: reference
topic: architecture
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 004-state-management.md
---

# 004X — State Management: Real-World Examples

## Example 1: User Session with Singleton Event Service

A desktop application needs to track the currently logged-in user and react when the user logs out.

### State service

```csharp
public sealed class SessionState
{
    public event Action<User?>? UserChanged;

    private User? _currentUser;
    public User? CurrentUser
    {
        get => _currentUser;
        set
        {
            _currentUser = value;
            UserChanged?.Invoke(value);
        }
    }

    public bool IsLoggedIn => _currentUser is not null;
}

// Registration
builder.Services.AddSingleton<SessionState>();
```

### ShellViewModel (header bar)

```csharp
public partial class ShellViewModel : ObservableObject, IDisposable
{
    private readonly SessionState _session;

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private bool _isLoggedIn;

    public ShellViewModel(SessionState session)
    {
        _session = session;
        session.UserChanged += OnUserChanged;
        OnUserChanged(session.CurrentUser);
    }

    private void OnUserChanged(User? user)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DisplayName = user?.FullName ?? "Guest";
            IsLoggedIn = user is not null;
        });
    }

    [RelayCommand]
    private void Logout()
    {
        _session.CurrentUser = null;
    }

    public void Dispose()
    {
        _session.UserChanged -= OnUserChanged;
    }
}
```

### DashboardViewModel (content area)

```csharp
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly SessionState _session;
    private readonly ITodoService _todoService;

    [ObservableProperty]
    private IReadOnlyList<TodoItem> _items = Array.Empty<TodoItem>();

    public DashboardViewModel(SessionState session, ITodoService todoService)
    {
        _session = session;
        _todoService = todoService;
        session.UserChanged += OnUserChanged;
    }

    private async void OnUserChanged(User? user)
    {
        if (user is null)
        {
            Items = Array.Empty<TodoItem>();
            return;
        }
        Items = await _todoService.GetForUserAsync(user.Id);
    }

    public void Dispose()
    {
        _session.UserChanged -= OnUserChanged;
    }
}
```

### Key points

- Two independent ViewModels react to the same state change
- Both unsubscribe on `Dispose` to prevent leaks
- The `async void` handler is acceptable here because the event fires from the UI thread

---

## Example 2: Flux Store for a Todo Application with Undo

A full-featured todo list using the Flux pattern.

### State and actions

```csharp
public sealed record TodoItem(Guid Id, string Title, bool IsComplete, DateTime CreatedAt);

public sealed record AppState(
    IReadOnlyList<TodoItem> Items,
    string? Filter,
    bool IsLoading,
    string? Error
);

public abstract record Action;

public sealed record AddItemAction(string Title) : Action;
public sealed record ToggleItemAction(Guid Id) : Action;
public sealed record RemoveItemAction(Guid Id) : Action;
public sealed record SetFilterAction(string? Filter) : Action;
public sealed record LoadRequestAction : Action;
public sealed record LoadSuccessAction(IReadOnlyList<TodoItem> Items) : Action;
public sealed record LoadFailedAction(string Error) : Action;
public sealed record UndoAction : Action;
```

### Store with middleware support

```csharp
public sealed class Store<TState>
{
    private readonly object _lock = new();
    private TState _state;
    private readonly Func<TState, Action, TState> _reducer;

    public event Action<TState>? StateChanged;

    public Store(TState initialState, Func<TState, Action, TState> reducer)
    {
        _state = initialState;
        _reducer = reducer;
    }

    public TState GetState()
    {
        lock (_lock) return _state;
    }

    public void Dispatch(Action action)
    {
        TState newState;
        lock (_lock)
        {
            newState = _reducer(_state, action);
            _state = newState;
        }
        StateChanged?.Invoke(newState);
    }
}

// Registration
builder.Services.AddSingleton(new Store<AppState>(
    new AppState(Array.Empty<TodoItem>(), null, false, null),
    Reducer
));
```

### Reducer

```csharp
public static AppState Reducer(AppState state, Action action) => action switch
{
    AddItemAction a => state with
    {
        Items = state.Items.Append(
            new TodoItem(Guid.NewGuid(), a.Title, false, DateTime.UtcNow)
        ).ToList()
    },

    ToggleItemAction a => state with
    {
        Items = state.Items.Select(i =>
            i.Id == a.Id ? i with { IsComplete = !i.IsComplete } : i
        ).ToList()
    },

    RemoveItemAction a => state with
    {
        Items = state.Items.Where(i => i.Id != a.Id).ToList()
    },

    SetFilterAction a => state with { Filter = a.Filter },

    LoadRequestAction => state with { IsLoading = true, Error = null },

    LoadSuccessAction a => state with
    {
        Items = a.Items,
        IsLoading = false,
        Error = null
    },

    LoadFailedAction a => state with
    {
        IsLoading = false,
        Error = a.Error
    },

    _ => state
};
```

### ViewModel

```csharp
public partial class TodoViewModel : ObservableObject
{
    private readonly Store<AppState> _store;

    [ObservableProperty]
    private IReadOnlyList<TodoItem> _items = Array.Empty<TodoItem>();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _error;

    public TodoViewModel(Store<AppState> store)
    {
        _store = store;
        _store.StateChanged += OnStateChanged;
        OnStateChanged(_store.GetState());
    }

    private void OnStateChanged(AppState state)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var source = state.Filter is null
                ? state.Items
                : state.Items.Where(i => !i.IsComplete).ToList();

            Items = source;
            IsLoading = state.IsLoading;
            Error = state.Error;
        });
    }

    [RelayCommand]
    private void AddItem(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;
        _store.Dispatch(new AddItemAction(title));
    }

    [RelayCommand]
    private void ToggleItem(Guid id) =>
        _store.Dispatch(new ToggleItemAction(id));

    [RelayCommand]
    private void RemoveItem(Guid id) =>
        _store.Dispatch(new RemoveItemAction(id));

    [RelayCommand]
    private void SetFilter(string? filter) =>
        _store.Dispatch(new SetFilterAction(filter));

    [RelayCommand]
    private async Task LoadAsync()
    {
        _store.Dispatch(new LoadRequestAction());
        try
        {
            var items = await Task.FromResult(Array.Empty<TodoItem>());
            _store.Dispatch(new LoadSuccessAction(items));
        }
        catch (Exception ex)
        {
            _store.Dispatch(new LoadFailedAction(ex.Message));
        }
    }
}
```

### Key points

- Every user action is a typed `Action` record, enabling tracing and replay
- The reducer is a pure function — no side effects, easy to unit test
- async flows dispatch multiple actions (request → success/failure)
- The ViewModel never mutates state directly — it only dispatches actions

---

## Example 3: Cross-ViewModel Communication with IMessenger

A workspace switcher where changing the workspace in a sidebar updates a detail panel.

### Message

```csharp
public sealed record WorkspaceSelectedMessage(Workspace Workspace);
```

### SidebarViewModel (sender)

```csharp
public partial class SidebarViewModel : ObservableObject
{
    private readonly IMessenger _messenger;

    public SidebarViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }

    [RelayCommand]
    private void SelectWorkspace(Workspace workspace)
    {
        _messenger.Send(new WorkspaceSelectedMessage(workspace));
    }
}
```

### DetailPanelViewModel (receiver)

```csharp
public partial class DetailPanelViewModel : ObservableObject, IRecipient<WorkspaceSelectedMessage>
{
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private string _workspaceName = "";

    [ObservableProperty]
    private int _itemCount;

    public DetailPanelViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        messenger.Register(this);
    }

    public void Receive(WorkspaceSelectedMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            WorkspaceName = message.Workspace.Name;
            ItemCount = message.Workspace.Items.Count;
        });
    }
}
```

### StatusBarViewModel (second receiver)

```csharp
public partial class StatusBarViewModel : ObservableObject, IRecipient<WorkspaceSelectedMessage>
{
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private string _status = "No workspace selected";

    public StatusBarViewModel(IMessenger messenger)
    {
        _messenger = messenger;
        messenger.Register(this);
    }

    public void Receive(WorkspaceSelectedMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Status = $"Workspace: {message.Workspace.Name}";
        });
    }
}
```

### Registration

```csharp
builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
builder.Services.AddTransient<SidebarViewModel>();
builder.Services.AddTransient<DetailPanelViewModel>();
builder.Services.AddTransient<StatusBarViewModel>();
```

### Key points

- No shared state object — the message itself carries the data
- Multiple decoupled receivers react to the same message
- Sender has no knowledge of who receives the message
- For transient VMs (dialogs), add `IDisposable` and call `_messenger.Unregister(this)`

---

## Example 4: Hybrid — Singleton Store with Reactive Notifications

Combines the simplicity of a singleton with the traceability of action objects using `System.Reactive`.

### Store

```csharp
public sealed class SettingsStore
{
    private readonly Subject<SettingsAction> _actions = new();
    private Settings _current = Settings.Default;

    public IObservable<SettingsAction> Actions => _actions;
    public Settings Current => _current;

    public void Apply(SettingsAction action)
    {
        _current = action switch
        {
            SetThemeAction a => _current with { Theme = a.Theme },
            SetLanguageAction a => _current with { Language = a.Language },
            _ => _current
        };
        _actions.OnNext(action);
    }
}

public abstract record SettingsAction;
public sealed record SetThemeAction(string Theme) : SettingsAction;
public sealed record SetLanguageAction(string Language) : SettingsAction;

public sealed record Settings(string Theme, string Language)
{
    public static readonly Settings Default = new("Light", "en-US");
}
```

### ViewModel

```csharp
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly SettingsStore _store;
    private IDisposable? _subscription;

    [ObservableProperty]
    private string _currentTheme = "";

    [ObservableProperty]
    private string _currentLanguage = "";

    public SettingsViewModel(SettingsStore store)
    {
        _store = store;
        _subscription = store.Actions
            .Subscribe(action =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    var s = _store.Current;
                    CurrentTheme = s.Theme;
                    CurrentLanguage = s.Language;
                });
            });

        var initial = store.Current;
        CurrentTheme = initial.Theme;
        CurrentLanguage = initial.Language;
    }

    [RelayCommand]
    private void SwitchTheme(string theme) =>
        _store.Apply(new SetThemeAction(theme));

    public void Dispose() => _subscription?.Dispose();
}
```

### Key points

- `System.Reactive` gives you LINQ-style composition over state changes
- Actions are traceable objects (can log, serialize, replay)
- No generic `Store<TState>` ceremony — the store is typed to the domain
- The `IDisposable` subscription replaces event unsubscription
