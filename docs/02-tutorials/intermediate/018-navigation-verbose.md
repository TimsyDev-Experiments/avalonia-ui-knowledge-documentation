---
tier: intermediate
topic: navigation
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 018-navigation.md
---

# 018V — Navigation Patterns: An In-Depth Companion

**Why this exists:** The original tutorial covers the ViewModel-first navigation pattern with `ContentControl`, view locator, and back-stack. This companion explains *why each piece exists*, *what happens inside the navigation service when NavigateTo is called*, *how the view locator resolves types*, *when to use convention-based vs. dictionary-based locators*, and *how navigation interacts with DI lifetimes*.

**Cross-reference:** Original tutorial at [018-navigation.md](018-navigation.md).

---

## 1. Why ContentControl + DataTemplate for navigation

```xml
<ContentControl Content="{Binding CurrentView}">
  <ContentControl.ContentTemplate>
    <DataTemplate x:DataType="vm:ViewModelBase">
      <views:ViewLocator />
    </DataTemplate>
  </ContentControl.ContentTemplate>
</ContentControl>
```

**Why ContentControl instead of Frame (WPF) or NavigationPage (Xamarin):** Avalonia does not have a built-in `Frame` control with navigation journal. The `ContentControl` approach is simpler and more flexible: `Content` is set to a ViewModel, the `ContentTemplate` runs the `ViewLocator` `IDataTemplate` which returns the View for that ViewModel, and the View is rendered.

**What happens when `CurrentView` changes:**

1. The ViewModel sets `CurrentView = new SettingsViewModel()`.
2. `PropertyChanged` fires for `CurrentView`. The `ContentControl` binding re-evaluates.
3. `ContentControl` sets its `Content` property to the new `SettingsViewModel`.
4. `ContentControl` calls `ContentTemplate.Match(Content)` — the `ViewLocator.Match` method.
5. `Match` returns `true` because the content is a `ViewModelBase`.
6. `ContentControl` calls `ContentTemplate.Build(Content)` — the `ViewLocator.Build` method.
7. `Build` creates the `SettingsView`, sets its `DataContext` to `SettingsViewModel`, and returns it.
8. The `SettingsView` is added as a child of the `ContentControl`'s visual tree.
9. The old view (HomeView) is removed from the visual tree and becomes eligible for garbage collection.

**Performance:** Every navigation creates a new View instance (transient). The old view is disconnected and collected. There is no view caching in this pattern. For most desktop apps this is fine — view construction is fast. If you need caching (e.g., tab restore), extend the navigation service to reuse views by key.

---

## 2. NavigationService — what NavigateTo does internally

```csharp
public class NavigationService : INavigationService
{
    private readonly Stack<ViewModelBase> _history = new();
    private readonly IServiceProvider _services;

    public void NavigateTo<T>() where T : ViewModelBase
    {
        var vm = _services.GetRequiredService<T>();
        NavigateTo(vm);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        if (CurrentView is not null)
            _history.Push(CurrentView);

        CurrentView = viewModel;
    }
}
```

**The history stack:**

When `NavigateTo<SettingsViewModel>()` is called:

1. `NavigationService.GetRequiredService<SettingsViewModel>()` — DI creates a new `SettingsViewModel` with all its dependencies injected.
2. The current `CurrentView` (e.g., `HomeViewModel`) is pushed onto the `_history` stack.
3. `CurrentView` is set to the new `SettingsViewModel`.
4. The `CurrentView` property setter fires `PropertyChanged`, which the `ShellViewModel` subscribed to.
5. `ShellViewModel`'s handler raises `PropertyChanged(nameof(CurrentView))`, which the `ContentControl` binding picks up.

**What GoBack does:**

1. Pops the top item from `_history` stack.
2. Sets `CurrentView` to that item.
3. The old ViewModel (e.g., `SettingsViewModel`) has no more references and is garbage collected (assuming no other references).

**Edge case — navigating back to the beginning:** When the stack has one item and you call `GoBack`, `_history.Count` is 1, so the pop succeeds and `CurrentView` becomes the root item. Calling `GoBack` again sees `_history.Count == 0` and does nothing — `CurrentView` stays at the root.

**The CurrentViewChanged event in the original:**

The original tutorial uses `CurrentViewChanged` as an event on `INavigationService`. The companion implementation above uses a simpler property-change approach. Both work. The event approach allows the `ShellViewModel` to react without needing `INotifyPropertyChanged` on the service.

---

## 3. View locator — dictionary-based vs. convention-based

### Dictionary-based (explicit mapping)

```csharp
private static readonly Dictionary<Type, Type> _mappings = new()
{
    { typeof(HomeViewModel), typeof(HomeView) },
    { typeof(SettingsViewModel), typeof(SettingsView) },
    { typeof(AboutViewModel), typeof(AboutView) },
};
```

**Advantages:**

- Explicit — you can see every ViewModel-to-View mapping in one place.
- Non-standard names are supported (e.g., `LoginViewModel` → `SignInView`).
- Compile-time safety: if you rename a View or ViewModel, the mapping dictionary entry becomes stale and fails at startup (not silently at runtime).

**Disadvantages:**

- You must register every pair manually. For large apps with 50+ views, this is maintenance overhead.
- It is easy to forget to register a new view, causing a runtime exception.

### Convention-based (name matching)

```csharp
var viewName = viewModelType.FullName!
    .Replace("ViewModels", "Views")
    .Replace("ViewModel", "View");

var viewType = Type.GetType(viewName);
```

**Advantages:**

- Zero registration for new views — if you follow the naming convention, it just works.
- Adding a new ViewModel+View pair requires no changes to the locator.

**Disadvantages:**

- The naming convention must be strictly followed. A typo in the namespace or class name produces a runtime error.
- Non-standard mappings require special-casing (e.g., `LoginViewModel` → `AuthenticationView`).
- `Type.GetType(viewName)` requires the assembly-qualified name or the type must be in the executing assembly. For types in other assemblies, use `Assembly.GetType()`.

### Combined approach

```csharp
public Control? Build(object? param)
{
    if (param is null) return null;

    var viewModelType = param.GetType();

    // Check explicit mappings first
    if (_mappings.TryGetValue(viewModelType, out var viewType))
    {
        var view = (Control)Activator.CreateInstance(viewType)!;
        view.DataContext = param;
        return view;
    }

    // Fall back to convention
    var conventionName = viewModelType.FullName!
        .Replace("ViewModels", "Views")
        .Replace("ViewModel", "View");

    viewType = Type.GetType(conventionName);
    if (viewType is null)
        throw new InvalidOperationException($"No view found for {viewModelType}");

    var conventionView = (Control)Activator.CreateInstance(viewType)!;
    conventionView.DataContext = param;
    return conventionView;
}
```

---

## 4. DI registration — transient views, singleton services

```csharp
services.AddSingleton<INavigationService, NavigationService>();
services.AddTransient<HomeViewModel>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<SettingsView>();
services.AddTransient<HomeView>();
```

**Why ViewModels are transient:** Each navigation should create a fresh ViewModel. If `SettingsViewModel` is singleton, the same instance is reused — its state persists across navigation. When the user navigates to Settings, changes the language, navigates back, and returns to Settings, they see their previous changes. For most screens, transient (new instance per navigation) is preferred.

**Why the navigation service is singleton:** The navigation service owns the history stack and the current ViewModel reference. If it were transient, each new instance would have an empty history stack. It must be a singleton (or scoped to the application lifetime).

**Why views are transient:** The `ViewLocator` creates a new view instance each time it builds. Views hold references to their visual children and are heavyweight. Making them transient ensures memory is reclaimed when the user navigates away.

**ShellViewModel lifetime:** The `ShellViewModel` is typically singleton (it lives for the duration of the app). It owns the navigation service reference and the `CurrentView` property that the shell window binds to.

---

## 5. Navigation with parameters

```csharp
public void NavigateTo<T>(Action<T> configure) where T : ViewModelBase
{
    var vm = _services.GetRequiredService<T>();
    configure(vm);
    NavigateTo(vm);
}

// Usage
_navigation.NavigateTo<DetailViewModel>(vm =>
{
    vm.ItemId = selectedItem.Id;
});
```

**Why a configure action instead of constructor parameters:** Constructor parameters would require the navigation service to know the parameter types. An `Action<T>` lets the caller configure the ViewModel after construction but before navigation.

**Tradeoff:** The ViewModel is temporarily in a partially-configured state between construction and the configure callback. Do not put navigation-triggering logic in the ViewModel constructor — use `OnNavigatedTo` instead (if you implement a `INavigationAware` interface).

**Alternative — query-string-like navigation:**

```csharp
public void NavigateTo(string route, Dictionary<string, object> parameters)
{
    // Parse route, resolve ViewModel type, set properties
}
```

This is more flexible (supports URI-based deep linking) but requires a routing table and type conversion.

---

## 6. INavigationAware — lifecycle hooks

```csharp
public interface INavigationAware
{
    void OnNavigatedTo(NavigationContext context);
    void OnNavigatedFrom(NavigationContext context);
}
```

Implement on ViewModels that need to know when they are entered or exited:

```csharp
public partial class DetailViewModel : ViewModelBase, INavigationAware
{
    public void OnNavigatedTo(NavigationContext context)
    {
        // Load data based on context.Parameters["Id"]
    }

    public void OnNavigatedFrom(NavigationContext context)
    {
        // Save state, dispose resources
    }
}
```

The navigation service calls these hooks in `NavigateTo`:

```csharp
public void NavigateTo(ViewModelBase viewModel)
{
    if (CurrentView is INavigationAware from)
        from.OnNavigatedFrom(new NavigationContext());

    if (CurrentView is not null)
        _history.Push(CurrentView);

    CurrentView = viewModel;

    if (CurrentView is INavigationAware to)
        to.OnNavigatedTo(new NavigationContext());
}
```

---

## 7. Common mistakes

**Mistake 1: ViewModel does not implement INotifyPropertyChanged for CurrentView.**

The `ShellViewModel.CurrentView` property must notify the `ContentControl` binding when it changes. If the navigation service changes `CurrentView` without raising `PropertyChanged`, the UI does not update.

Fix: Use `[ObservableProperty]` on the shell's `CurrentView` or manually raise `OnPropertyChanged`.

**Mistake 2: ViewLocator.Build returns null.**

If the mapping is missing or the convention fails, `Build` returns null. The `ContentControl` shows nothing. Always include error handling (throw with a clear message).

**Mistake 3: Multiple ContentControls in the same view.**

If you have two `ContentControl` bindings to the same `CurrentView`, the ViewLocator builds the view twice — two instances of the same View, each with its own state. Use a single `ContentControl` or wrap in a `ViewLocator` that caches per ViewModel instance.

**Mistake 4: Navigation service holds strong references to old ViewModels.**

The `_history` stack holds strong references to every ViewModel the user navigated away from. If the user navigates through 50 screens, all 50 ViewModels are alive. For long-lived apps, limit the stack size:

```csharp
private readonly Stack<ViewModelBase> _history = new();
private const int MaxHistory = 20;

public void NavigateTo(ViewModelBase viewModel)
{
    if (_history.Count >= MaxHistory)
    {
        // Remove oldest, dispose if IDisposable
        var oldest = _history.ToArray()[^1];
        _history.Pop();
    }
    // ...
}
```

---

## Key Takeaways

- Use `ContentControl` with a `DataTemplate` containing a `ViewLocator` for ViewModel-first navigation.
- The `NavigationService` owns a `Stack<ViewModelBase>` for back navigation. It resolves ViewModels from DI.
- The `ViewLocator` maps ViewModel types to View types — either by explicit dictionary or naming convention.
- Register ViewModels and Views as transient; register the navigation service and shell ViewModel as singleton.
- Use `Action<T>` configure callbacks to pass parameters to the next ViewModel.
- Implement `INavigationAware` for lifecycle hooks (load data on navigate, save on leave).
- Limit history stack size to prevent memory growth in long-lived navigation sessions.

---

## See Also

- [018 — Navigation (original)](018-navigation.md)
- [015 — Item Lists](015-item-lists.md)
- [016 — Window & Dialog Management](016-window-dialog-management.md) (dialog service uses similar mapping pattern)
- [011 — Compiled Bindings](011-compiled-bindings.md) (used in all navigation view XAML)
- [018E — Navigation Patterns (examples)](018-navigation-examples.md)
- [Avalonia Docs: View Locator](https://docs.avaloniaui.net/docs/concepts/view-locator)
