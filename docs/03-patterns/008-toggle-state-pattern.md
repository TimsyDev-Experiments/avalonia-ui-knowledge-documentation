---
tier: intermediate
topic: architecture
estimated: 10 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# Pattern: Toggle & State Pattern

**What you'll learn:** A reusable pattern for managing binary and multi-state toggles in ViewModels, with support for exclusive groups, persistence, and command integration.

**Prerequisites:** [007 — ObservableObject & ObservableProperty](../02-tutorials/basics/007-observable-object-property.md), [008 — RelayCommand](../02-tutorials/basics/008-relay-command.md)

---

## Problem

An application needs to track toggle states (filters, options, feature flags, view modes) with mutual exclusion, persistence, and reactive UI updates. Manual bool properties with `[ObservableProperty]` work for one-off cases but don't scale to grouped or exclusive toggles.

---

## Solution: ToggleService

```csharp
public interface IToggleService
{
    bool IsEnabled(string key);
    void Enable(string key);
    void Disable(string key);
    void Toggle(string key);
    IObservable<bool> Observe(string key);
}

public class ToggleService : IToggleService
{
    private readonly Dictionary<string, SourceCache<bool>> _toggles = new();
    private readonly HashSet<string> _mutualExclusion = new();

    public bool IsEnabled(string key) =>
        _toggles.GetValueOrDefault(key)?.Value ?? false;

    public void Enable(string key)
    {
        if (_mutualExclusion.Contains(key))
            foreach (var k in _mutualExclusion)
                SetState(k, false);
        SetState(key, true);
    }

    public void Disable(string key) => SetState(key, false);
    public void Toggle(string key) => SetState(key, !IsEnabled(key));

    public IObservable<bool> Observe(string key)
        => _toggles.GetOrAdd(key, _ => new SourceCache<bool>(false));

    private void SetState(string key, bool value) =>
        _toggles.GetOrAdd(key, _ => new SourceCache<bool>(value)).Set(value);

    public void MakeMutuallyExclusive(params string[] keys)
    {
        foreach (var k in keys) _mutualExclusion.Add(k);
    }

    private class SourceCache<T>(T initial)
    {
        public T Value { get; private set; } = initial;
        public event Action<T>? Changed;
        public void Set(T value) { Value = value; Changed?.Invoke(value); }
    }
}
```

---

## Usage in ViewModel

```csharp
public partial class SettingsViewModel(IToggleService toggles) : ObservableObject
{
    [ObservableProperty] private bool _darkMode;
    [ObservableProperty] private bool _compactLayout;
    [ObservableProperty] private string _selectedView = "list";

    partial void OnDarkModeChanged(bool value) => toggles.Enable("darkmode");
    partial void OnCompactLayoutChanged(bool value) => toggles.Enable("compact");

    [RelayCommand]
    private void SwitchView(string view)
    {
        SelectedView = view;
        toggles.Enable($"view:{view}");
    }
}
```

---

## UI binding

```xml
<ToggleSwitch IsOn="{Binding DarkMode}" Content="Dark Mode" />
<RadioButton Content="List View" IsChecked="{Binding SelectedView,
              Converter={StaticResource EnumToBool}, ConverterParameter=list}" />
```

---

## Persistence layer

```csharp
public class PersistentToggleService(IToggleService inner, string filePath) : IToggleService
{
    public void Save()
    {
        var json = JsonSerializer.Serialize(collectedStates);
        File.WriteAllText(filePath, json);
    }

    public void Load()
    {
        if (File.Exists(filePath))
        {
            var states = JsonSerializer.Deserialize<Dictionary<string, bool>>(
                File.ReadAllText(filePath));
            if (states != null)
                foreach (var (k, v) in states)
                    if (v) inner.Enable(k); else inner.Disable(k);
        }
    }
}
```

Call `Save()` on app close, `Load()` on startup.
