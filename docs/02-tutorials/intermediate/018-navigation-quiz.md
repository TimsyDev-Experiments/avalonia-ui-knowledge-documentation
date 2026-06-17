---
tier: intermediate
topic: navigation
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 018-navigation.md
---

# Quiz — Navigation

```quiz
Q: What role does a ViewLocator serve in an Avalonia navigation setup?
A. It registers all ViewModels and Views in the DI container || Incorrect — DI registration is handled in Program.cs, not by the ViewLocator.
B. It maps a ViewModel type to its corresponding View type and creates the View instance (correct) || Correct — the ViewLocator's Build method resolves the View type from the ViewModel and sets the DataContext.
C. It manages the back-stack history of visited ViewModels || Incorrect — history management belongs to INavigationService, not the ViewLocator.
D. It provides compiled bindings for ContentControl content || Incorrect — compiled bindings are declared in XAML with x:DataType, not provided by the ViewLocator.
Explanation: A ViewLocator implements IDataTemplate and resolves a View type from a ViewModel type at runtime so ContentControl can render the correct view.
```

```quiz
Q: Which control acts as the primary navigation surface for swapping views in a ViewModel-driven navigation pattern?
A. Frame || Incorrect — Frame is a WPF concept; Avalonia does not have a Frame control.
B. TabControl || Incorrect — TabControl shows all tabs simultaneously and is not designed for programmatic navigation push/pop.
C. ContentControl (correct) || Correct — ContentControl displays a single piece of content and swaps views when its bound CurrentView property changes.
D. ItemsRepeater || Incorrect — ItemsRepeater renders a collection of items, not a single active view.
Explanation: ContentControl is the idiomatic navigation host in Avalonia; binding its Content property to the current ViewModel drives view swapping.
```

```quiz
Q: In the convention-based ViewLocator, the ViewModel type `MyApp.ViewModels.DashboardViewModel` is resolved to which view type?
A. MyApp.Views.DashboardView (correct) || Correct — the convention replaces "ViewModels" with "Views" and strips "ViewModel" to "View".
B. MyApp.Views.DashboardViewModelView || Incorrect — the convention replaces "ViewModel" with "View", not appends "View".
C. MyApp.Views.Dashboard || Incorrect — the convention strips "ViewModel" but also replaces the "ViewModels" segment with "Views".
D. MyApp.Pages.DashboardPage || Incorrect — the convention replaces "ViewModels" with "Views", not "Pages", and replaces "ViewModel" with "View", not "Page".
Explanation: The convention replaces the namespace segment `ViewModels` → `Views` and the suffix `ViewModel` → `View`.
```

```quiz
Q: Identify the bug in this navigation service snippet:
    public void NavigateTo<T>() where T : ViewModelBase
    {
        var vm = new T();
        if (CurrentView is not null)
            _history.Push(CurrentView);
        CurrentView = vm;
    }
A. The history stack is not checked for null before Push || Incorrect — _history.Push is safe; the bug is elsewhere.
B. The ViewModel is created with `new T()` instead of resolving from the DI container (correct) || Correct — using `new T()` bypasses DI and skips constructor-injected dependencies; the service should call `_services.GetRequiredService<T>()`.
C. CurrentView should be set before pushing to history || Incorrect — the order (push then set) is correct for back-stack semantics.
D. The method should be async || Incorrect — navigation can be synchronous; async is not required here.
Explanation: ViewModels with constructor-injected dependencies must be resolved through the DI container; `new T()` will fail if the ViewModel has non-trivial constructor parameters.
```

```quiz
Q: How does `NavigateTo<T>(Action<T> configure)` enable passing parameters to the target ViewModel?
A. It serializes the configure delegate and passes it as a query string || Incorrect — there are no query strings in Avalonia navigation.
B. It resolves the ViewModel from DI, invokes the configure callback on it, then navigates (correct) || Correct — the pattern resolves `T` from the container, lets the caller set properties via the callback, and then pushes the configured instance onto the navigation stack.
C. It stores the configure action and invokes it when the view is loaded || Incorrect — the configure action is invoked immediately, not deferred to view loading.
D. It creates the ViewModel with `new T()` and passes the configure delegate to its constructor || Incorrect — it resolves from DI and the callback is invoked after construction, not passed to the constructor.
Explanation: The overload resolves the ViewModel from `IServiceProvider`, lets the caller configure it with a lambda, then navigates — keeping parameter passing explicit and type-safe.
```
