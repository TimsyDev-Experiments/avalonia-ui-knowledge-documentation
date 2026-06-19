---
tier: intermediate
topic: architecture
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 008-toggle-state-pattern.md
---

# 008V — Toggle & State Pattern: An In-Depth Companion

You should already have read: [008 — Toggle & State Pattern](008-toggle-state-pattern.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Why Dedicated Toggle Management?

The naive approach — scattering `bool` properties across ViewModels — breaks down when:

- **A single boolean is observed by multiple ViewModels.** Two panels both need to react to "dark mode enabled." If each panel owns its own `_darkMode` field, they can fall out of sync.
- **Toggles belong to mutually exclusive groups.** A view-mode toggle (List, Grid, Detail) should allow exactly one active state. Enforcing this with manual `if`/`else` chains in every command handler is error-prone.
- **Toggle state must survive application restart.** Persisting ten scattered `bool` properties means ten separate `Settings` entries or a fragile serialization scheme.
- **A toggle's value should be observable without polling.** The UI needs to reactively update when a toggle changes, even if the change originated from a different code path (keyboard shortcut, menu item, API call).

The `IToggleService` centralizes all toggle state in one service, providing a uniform API for read, write, toggle, observe, mutual exclusion, and persistence.

---

## 2. IToggleService — Complete Interface

```csharp
public interface IToggleService
{
    bool IsEnabled(string key);
    void Enable(string key);
    void Disable(string key);
    void Toggle(string key);
    IObservable<bool> Observe(string key);

    // Advanced operations
    void SetState(string key, bool value);
    IReadOnlyDictionary<string, bool> Snapshot();
    void Clear(string key);
    event Action<string, bool>? ToggleChanged;
}
```

Each method is designed for a specific use case:

| Method | Use Case |
|---|---|
| `IsEnabled` | Quick synchronous read (e.g., in a command's `CanExecute`) |
| `Enable` / `Disable` | Explicit set from a checkbox or toggle button |
| `Toggle` | Invert current state from a keyboard shortcut or momentary button |
| `Observe` | Reactive LINQ chain with `System.Reactive` (`CombineLatest`, `Merge`, `WhenAnyValue`) |
| `Snapshot` | Serialize all toggles for persistence |
| `Clear` | Reset a toggle to its default (false) |

---

## 3. Full ToggleService Implementation

```csharp
public sealed class ToggleService : IToggleService
{
    private readonly ConcurrentDictionary<string, ToggleState> _toggles = new();
    private readonly HashSet<string> _mutualExclusionGroup = new();
    private readonly object _groupLock = new();

    public bool IsEnabled(string key) =>
        _toggles.TryGetValue(key, out var state) && state.Value;

    public void Enable(string key)
    {
        lock (_groupLock)
        {
            if (_mutualExclusionGroup.Contains(key))
            {
                // Disable all toggles in the same group, then enable this one
                foreach (var k in _toggles.Keys)
                {
                    if (k != key && _mutualExclusionGroup.Contains(k))
                        SetStateInternal(k, false);
                }
            }
            SetStateInternal(key, true);
        }
    }

    public void Disable(string key) => SetStateInternal(key, false);

    public void Toggle(string key) => SetStateInternal(key, !IsEnabled(key));

    public void SetState(string key, bool value) => SetStateInternal(key, value);

    public IObservable<bool> Observe(string key)
    {
        var state = _toggles.GetOrAdd(key, _ => new ToggleState());
        return state.Observable;
    }

    public IReadOnlyDictionary<string, bool> Snapshot()
    {
        return _toggles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
    }

    public void Clear(string key)
    {
        if (_toggles.TryRemove(key, out var state))
        {
            state.Set(false);
            ToggleChanged?.Invoke(key, false);
        }
    }

    public event Action<string, bool>? ToggleChanged;

    public void MakeMutuallyExclusive(IEnumerable<string> keys)
    {
        lock (_groupLock)
        {
            foreach (var k in keys)
                _mutualExclusionGroup.Add(k);
        }
    }

    public ToggleService WithMutualExclusion(params string[] keys)
    {
        MakeMutuallyExclusive(keys);
        return this;
    }

    private void SetStateInternal(string key, bool value)
    {
        var state = _toggles.GetOrAdd(key, _ => new ToggleState());
        state.Set(value);
        ToggleChanged?.Invoke(key, value);
    }

    private sealed class ToggleState
    {
        private bool _value;
        private readonly Subject<bool> _subject = new();

        public bool Value => _value;

        public IObservable<bool> Observable => _subject.AsObservable();

        public void Set(bool value)
        {
            if (_value != value)
            {
                _value = value;
                _subject.OnNext(value);
            }
        }
    }
}
```

### Reactive Extensions Integration

The `Observe` method returns `IObservable<bool>`, enabling powerful reactive chains:

```csharp
// Reactively combine multiple toggles
var isDarkMode = toggleService.Observe("darkmode");
var isCompact = toggleService.Observe("compact");

isDarkMode.CombineLatest(isCompact, (dark, compact) => new { dark, compact })
    .Subscribe(state =>
    {
        ApplyTheme(state.dark ? Theme.Dark : Theme.Light);
        ApplyDensity(state.compact ? Density.Compact : Density.Normal);
    });

// Filter for specific transitions
toggleService.Observe("darkmode")
    .Where(enabled => enabled)
    .Subscribe(_ => StatusBar?.ShowMessage("Dark mode enabled"));
```

---

## 4. Mutual Exclusion Groups — Advanced Scenarios

### Group Membership and Nested Groups

A toggle can belong to only one mutual-exclusion group. Groups are identified implicitly by the set of keys passed to `MakeMutuallyExclusive`. When a toggle in a group is enabled, all other toggles in that group are automatically disabled.

```csharp
// View mode group — exactly one active
toggleService.MakeMutuallyExclusive("view:list", "view:grid", "view:detail");

// Render mode group — exactly one active
toggleService.MakeMutuallyExclusive("render:wireframe", "render:shaded", "render:raytraced");

// Each group is independent — enabling "view:list" does not affect "render:wireframe"
toggleService.Enable("view:list");
toggleService.Enable("render:wireframe");
```

### Enabling Without Group Enforcement

Sometimes you want to set a toggle without triggering mutual exclusion (e.g., restoring saved state). Provide a bypass:

```csharp
public void EnableWithoutExclusion(string key)
{
    SetStateInternal(key, true);
}

// Used during persistence restore
public void RestoreState(IReadOnlyDictionary<string, bool> snapshot)
{
    foreach (var (key, value) in snapshot)
        EnableWithoutExclusion(key);
}
```

---

## 5. PersistentToggleService — Decorator Pattern Deep Dive

The `PersistentToggleService` wraps any `IToggleService` and adds automatic save/load. This uses the **Decorator Pattern** — persistence is a cross-cutting concern applied via composition, not inheritance.

```csharp
public sealed class PersistentToggleService : IToggleService
{
    private readonly IToggleService _inner;
    private readonly string _filePath;
    private readonly ILogger? _logger;
    private bool _loaded;

    public PersistentToggleService(IToggleService inner, string filePath, ILogger? logger = null)
    {
        _inner = inner;
        _filePath = filePath;
        _logger = logger;

        // Subscribe to changes and auto-save with debounce
        _inner.ToggleChanged += OnToggleChanged;
    }

    // Delegate all operations to inner
    public bool IsEnabled(string key) => _inner.IsEnabled(key);
    public void Enable(string key) => _inner.Enable(key);
    public void Disable(string key) => _inner.Disable(key);
    public void Toggle(string key) => _inner.Toggle(key);
    public void SetState(string key, bool value) => _inner.SetState(key, value);
    public IObservable<bool> Observe(string key) => _inner.Observe(key);
    public void Clear(string key) => _inner.Clear(key);
    public IReadOnlyDictionary<string, bool> Snapshot() => _inner.Snapshot();
    public event Action<string, bool>? ToggleChanged
    {
        add => _inner.ToggleChanged += value;
        remove => _inner.ToggleChanged -= value;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        try
        {
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var snapshot = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (snapshot is not null)
                {
                    foreach (var (key, value) in snapshot)
                    {
                        if (value) _inner.Enable(key);
                        else _inner.Disable(key);
                    }
                }
            }
            _loaded = true;
            _logger?.LogInformation("Toggle state loaded from {Path}", _filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load toggle state from {Path}", _filePath);
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var snapshot = _inner.Snapshot();
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(_filePath, json);
            _logger?.LogInformation("Toggle state saved to {Path}", _filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save toggle state to {Path}", _filePath);
        }
    }

    private int _saveDebounceToken;
    private async void OnToggleChanged(string key, bool value)
    {
        // Debounce: save at most once per 2 seconds
        var token = Interlocked.Increment(ref _saveDebounceToken);
        await Task.Delay(2000);
        if (token == _saveDebounceToken)
            await SaveAsync();
    }
}
```

### DI Registration for Persistent Toggle

```csharp
// Program.cs
var toggleService = new ToggleService()
    .WithMutualExclusion("view:list", "view:grid", "view:detail")
    .WithMutualExclusion("render:wireframe", "render:shaded", "render:raytraced");

var appDataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyApp", "toggles.json");

var persistentToggle = new PersistentToggleService(toggleService, appDataPath);

builder.Services.AddSingleton<IToggleService>(persistentToggle);

// Start app
var app = builder.Build();
var toggleSvc = app.Services.GetRequiredService<IToggleService>();
if (toggleSvc is PersistentToggleService pts)
    await pts.LoadAsync();
```

---

## 6. ViewModel Integration Patterns

### Direct [ObservableProperty] Sync

The simplest approach: mirror a toggle's state in a ViewModel property and sync via `partial void OnChanged`:

```csharp
public sealed partial class ToolbarViewModel : ObservableObject
{
    private readonly IToggleService _toggles;
    private readonly Stack<IDisposable> _subscriptions = new();

    [ObservableProperty]
    private bool _isSnapEnabled;

    [ObservableProperty]
    private bool _isGridEnabled;

    [ObservableProperty]
    private bool _isDarkMode;

    [ObservableProperty]
    private string _selectedViewMode = "list";

    public ToolbarViewModel(IToggleService toggles)
    {
        _toggles = toggles;

        // Initialize from service
        IsSnapEnabled = toggles.IsEnabled("snap");
        IsGridEnabled = toggles.IsEnabled("grid");
        IsDarkMode = toggles.IsEnabled("darkmode");

        // Subscribe to external changes (e.g., from keyboard shortcuts)
        _subscriptions.Push(toggles.Observe("snap").Subscribe(v => IsSnapEnabled = v));
        _subscriptions.Push(toggles.Observe("grid").Subscribe(v => IsGridEnabled = v));
        _subscriptions.Push(toggles.Observe("darkmode").Subscribe(v => IsDarkMode = v));
    }

    partial void OnIsSnapEnabledChanged(bool value) => _toggles.Enable("snap");
    partial void OnIsGridEnabledChanged(bool value) => _toggles.Enable("grid");
    partial void OnIsDarkModeChanged(bool value) => _toggles.Enable("darkmode");

    [RelayCommand]
    private void SwitchViewMode(string mode)
    {
        SelectedViewMode = mode;
        _toggles.Enable($"view:{mode}");
    }
}
```

### Reactive Bindings with WhenAnyValue

Using CommunityToolkit.Mvvm's `WhenAnyValue` for more complex reactive chains:

```csharp
public sealed partial class StatusBarViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    public StatusBarViewModel(IToggleService toggles)
    {
        toggles.Observe("darkmode")
            .Select(dark => dark ? "Dark mode" : "Light mode")
            .Subscribe(text => StatusText = text);

        // Combine multiple toggles
        toggles.Observe("snap")
            .CombineLatest(toggles.Observe("grid"))
            .Subscribe(tuple =>
            {
                var (snap, grid) = tuple;
                // Update overlay indicators
            });
    }
}
```

---

## 7. UI Binding Patterns

### CheckBox / ToggleSwitch

```xml
<!-- Direct binding to ViewModel property, which syncs with service -->
<ToggleSwitch IsOn="{Binding IsDarkMode}" Content="Dark Mode"
              OffContent="Light" OnContent="Dark" />

<CheckBox IsChecked="{Binding IsSnapEnabled}" Content="Snap to Grid" />
```

### RadioButton with Group Name

```xml
<!-- Radio buttons for mutually exclusive view modes -->
<StackPanel>
  <TextBlock Text="View Mode" FontWeight="SemiBold" />
  <RadioButton GroupName="ViewMode" Content="List"
               IsChecked="{Binding SelectedViewMode, Converter={StaticResource EnumMatchConverter}, ConverterParameter=list}" />
  <RadioButton GroupName="ViewMode" Content="Grid"
               IsChecked="{Binding SelectedViewMode, Converter={StaticResource EnumMatchConverter}, ConverterParameter=grid}" />
  <RadioButton GroupName="ViewMode" Content="Detail"
               IsChecked="{Binding SelectedViewMode, Converter={StaticResource EnumMatchConverter}, ConverterParameter=detail}" />
</StackPanel>
```

### ToggleButton with Dynamic Icon

```xml
<!-- Toolbar toggle button with reactive icon -->
<ToggleButton IsChecked="{Binding IsSnapEnabled}"
              ToolTip.Tip="Toggle Snap">
  <Path Data="{Binding IsSnapEnabled, Converter={StaticResource BoolToSnapIcon}}" />
</ToggleButton>
```

### MenuItem with CheckMark

```xml
<MenuItem Header="Snap to Grid"
          IsChecked="{Binding IsSnapEnabled}"
          IsCheckable="True" />

<Separator />

<!-- Radio menu items for view mode -->
<MenuItem Header="View Mode">
  <MenuItem Header="List" IsCheckable="True"
            IsChecked="{Binding SelectedViewMode, Converter={StaticResource EnumMatchConverter}, ConverterParameter=list}"
            Command="{Binding SwitchViewModeCommand}" CommandParameter="list" />
  <MenuItem Header="Grid" IsCheckable="True"
            IsChecked="{Binding SelectedViewMode, Converter={StaticResource EnumMatchConverter}, ConverterParameter=grid}"
            Command="{Binding SwitchViewModeCommand}" CommandParameter="grid" />
  <MenuItem Header="Detail" IsCheckable="True"
            IsChecked="{Binding SelectedViewMode, Converter={StaticResource EnumMatchConverter}, ConverterParameter=detail}"
            Command="{Binding SwitchViewModeCommand}" CommandParameter="detail" />
</MenuItem>
```

---

## 8. Testing ToggleService

```csharp
[TestClass]
public sealed class ToggleServiceTests
{
    [TestMethod]
    public void Enable_SetsStateToTrue()
    {
        var service = new ToggleService();
        service.Enable("test");
        Assert.IsTrue(service.IsEnabled("test"));
    }

    [TestMethod]
    public void Disable_SetsStateToFalse()
    {
        var service = new ToggleService();
        service.Enable("test");
        service.Disable("test");
        Assert.IsFalse(service.IsEnabled("test"));
    }

    [TestMethod]
    public void Toggle_InvertsState()
    {
        var service = new ToggleService();
        Assert.IsFalse(service.IsEnabled("test"));
        service.Toggle("test");
        Assert.IsTrue(service.IsEnabled("test"));
        service.Toggle("test");
        Assert.IsFalse(service.IsEnabled("test"));
    }

    [TestMethod]
    public void MutualExclusion_DisablesOthers()
    {
        var service = new ToggleService();
        service.MakeMutuallyExclusive("a", "b", "c");
        service.Enable("a");
        service.Enable("b");
        Assert.IsTrue(service.IsEnabled("b"));
        Assert.IsFalse(service.IsEnabled("a"));
    }

    [TestMethod]
    public void Observe_EmitsChanges()
    {
        var service = new ToggleService();
        var results = new List<bool>();
        using var sub = service.Observe("test").Subscribe(v => results.Add(v));
        service.Enable("test");
        service.Disable("test");
        CollectionAssert.AreEqual(new[] { false, true, false }, results);
    }

    [TestMethod]
    public void Snapshot_ReturnsAllKeys()
    {
        var service = new ToggleService();
        service.Enable("a");
        service.Enable("b");
        service.Disable("c");
        var snapshot = service.Snapshot();
        Assert.AreEqual(3, snapshot.Count);
        Assert.IsTrue(snapshot["a"]);
        Assert.IsTrue(snapshot["b"]);
        Assert.IsFalse(snapshot["c"]);
    }
}
```

---

## 9. Performance and Thread Safety

- `ConcurrentDictionary` ensures thread-safe reads and writes
- `Subject<bool>` from `System.Reactive` is thread-safe for observers
- Mutex exclusion uses a `lock` to prevent race conditions during group enforcement
- The `ToggleState.Set()` method guards against no-op updates, preventing unnecessary UI re-renders
- `PersistentToggleService` debounces file writes to prevent I/O storms from rapid toggling (e.g., slider drag)

---

## Summary: Core vs. Verbose

| Concept | Core | Verbose |
|---|---|---|
| Interface | Basic `IToggleService` | Full interface with `SetState`, `Snapshot`, `Clear`, `ToggleChanged` event |
| Implementation | Simple `Dictionary` + `SourceCache` | `ConcurrentDictionary` + `Subject<bool>` with reactive integration |
| Mutual Exclusion | `MakeMutuallyExclusive` with `HashSet` | Group membership, bypass for restore, nested groups |
| Persistence | Basic `Save`/`Load` | Debounced auto-save, `ILogger`, async I/O, DI registration |
| ViewModel | One simple ViewModel | Multiple patterns: direct sync, reactive subscriptions, `WhenAnyValue` |
| UI Binding | One XAML snippet | CheckBox, ToggleSwitch, RadioButton, ToggleButton, MenuItem examples |
| Testing | — | 5 unit tests with `[TestClass]` |
| Thread Safety | — | `ConcurrentDictionary`, locks, no-op guards |
