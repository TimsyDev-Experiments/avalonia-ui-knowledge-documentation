---
tier: intermediate
topic: architecture
estimated: 10 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Pattern: Service Locator vs Dependency Injection

**What you'll learn:** The difference between service locator and dependency injection, why DI is preferred, and when each approach makes sense in an Avalonia application.

**Prerequisites:** [032 -- MVVM with Dependency Injection](/docs/02-tutorials/advanced/032-mvvm-di-wiring.md)

---

## Problem

A ViewModel or service needs access to other services (dialogs, file pickers, data access, navigation). Two common resolution strategies exist:

- **Service locator**: a global registry that any class can query at any time
- **Dependency injection**: the container pushes dependencies into the consumer via constructor parameters

Which should you use, and why?

---

## Solution: Dependency Injection

Constructor injection is the default and recommended approach.

```csharp
public class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialog;
    private readonly IDataService _data;

    // Container resolves these automatically
    public MainViewModel(IDialogService dialog, IDataService data)
    {
        _dialog = dialog;
        _data = data;
    }
}
```

Registration wire-up:

```csharp
services.AddSingleton<IDialogService, DialogService>();
services.AddSingleton<IDataService, DataService>();
services.AddTransient<MainViewModel>();
```

### Advantages

- **Explicit**: all dependencies visible in the constructor signature
- **Testable**: pass mocks in unit tests without touching global state
- **Lifecycle-aware**: container manages singleton/transient/scoped lifetimes
- **No hidden coupling**: nothing magic happens at runtime

### Test example

```csharp
[Fact]
public void MainViewModel_LoadData_UpdatesItems()
{
    var mockData = new Mock<IDataService>();
    mockData.Setup(d => d.FetchAsync())
        .ReturnsAsync(new[] { "Item A" });

    var vm = new MainViewModel(
        Mock.Of<IDialogService>(), mockData.Object);

    vm.LoadDataCommand.Execute(null);

    Assert.Contains("Item A", vm.Items);
}
```

---

## Alternative: Service Locator (anti-pattern)

A service locator exposes a static or ambient registry:

```csharp
public static class Locator
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T instance) =>
        _services[typeof(T)] = instance;

    public static T Get<T>() =>
        (T)_services[typeof(T)];
}
```

Used like this:

```csharp
public class MainViewModel : ObservableObject
{
    public async Task SaveAsync()
    {
        var dialog = Locator.Get<IDialogService>();
        var confirmed = await dialog.ConfirmAsync("Save?");
        // ...
    }
}
```

### Problems

- **Hidden dependencies**: the constructor does not reveal what services are needed
- **Untestable in isolation**: tests must set up global state before each test
- **Lifecycle opaque**: no distinction between transient and singleton at the call site
- **Ambient coupling**: any code can pull any service from anywhere

---

## When the Locator Is Acceptable

There are narrow cases where a controlled locator is pragmatic:

| Scenario | Why | Mitigation |
|----------|-----|------------|
| View locator (ViewModel -> View) | Views are created by the framework, not the container | Keep locator scoped to a single concern (type mapping) |
| Factory pattern | Dynamic type selection at runtime | Inject `IServiceProvider` instead of a static locator |
| Legacy code migration | Gradual DI adoption | Use locator as a bridge, remove it after full migration |

A disciplined factory-based approach using `IServiceProvider`:

```csharp
public class ViewModelFactory
{
    private readonly IServiceProvider _provider;

    public ViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Create<T>() where T : notnull =>
        _provider.GetRequiredService<T>();
}
```

---

## Key Takeaways

- Constructor injection is the default — explicit, testable, lifecycle-safe
- Service locator is an anti-pattern that hides coupling and breaks test isolation
- Acceptable locator use cases are narrow: view resolution, factory patterns, legacy migration
- Inject `IServiceProvider` for dynamic resolution instead of a static locator
- All ViewModels in this documentation use constructor injection through `Microsoft.Extensions.DependencyInjection`

---

## See Also

- [032 -- MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Pattern: Modular App with Plugin-Style Views](002-plugin-architecture.md)
- [Avalonia Docs: MVVM Architecture](https://docs.avaloniaui.net/docs/concepts/mvvm)
