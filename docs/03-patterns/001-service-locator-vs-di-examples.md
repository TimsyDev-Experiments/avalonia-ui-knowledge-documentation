---
tier: intermediate
topic: architecture
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 001-service-locator-vs-di.md
---

# 001X — Service Locator vs DI: Real-World Examples

You should already have read: [001 — Service Locator vs DI](001-service-locator-vs-di.md) for the core concepts. This file provides complete, worked examples that show the pattern in practice.

---

## Example 1: Refactoring from Service Locator to Constructor Injection

**What you'll learn:** How to identify hidden dependencies in a legacy ViewModel, extract them through constructor injection, and verify the result with a unit test.

### Before: Service Locator

The `OrderViewModel` pulls every service it needs from a static `Locator` class. The constructor is empty — all dependencies are hidden inside methods.

```csharp
public class OrderViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();

    [ObservableProperty]
    private string _status = "";

    public async Task LoadOrdersAsync()
    {
        var data = Locator.Get<IOrderDataService>();
        var auth = Locator.Get<IAuthService>();

        Status = "Loading...";
        var orders = await data.FetchOrdersAsync(auth.CurrentUserId);
        Orders = new ObservableCollection<Order>(orders);
        Status = $"Loaded {orders.Count} orders";
    }

    public async Task ExportOrderAsync(int orderId)
    {
        var data = Locator.Get<IOrderDataService>();
        var export = Locator.Get<IExportService>();

        var order = await data.GetOrderAsync(orderId);
        await export.ExportToCsvAsync(order, $"order_{orderId}.csv");
    }
}
```

Problems:

- To write a test, you must set up `Locator` with three mock services before each test
- `ExportOrderAsync` reveals it needs `IExportService`, but the constructor signature does not
- If someone removes the export feature, the compiler does not flag the still-present `Locator.Get<IExportService>()`

### After: Constructor Injection

```csharp
public class OrderViewModel : ObservableObject
{
    private readonly IOrderDataService _data;
    private readonly IAuthService _auth;
    private readonly IExportService _export;

    [ObservableProperty]
    private ObservableCollection<Order> _orders = new();

    [ObservableProperty]
    private string _status = "";

    public OrderViewModel(
        IOrderDataService data,
        IAuthService auth,
        IExportService export)
    {
        _data = data;
        _auth = auth;
        _export = export;
    }

    public async Task LoadOrdersAsync()
    {
        Status = "Loading...";
        var orders = await _data.FetchOrdersAsync(_auth.CurrentUserId);
        Orders = new ObservableCollection<Order>(orders);
        Status = $"Loaded {orders.Count} orders";
    }

    public async Task ExportOrderAsync(int orderId)
    {
        var order = await _data.GetOrderAsync(orderId);
        await _export.ExportToCsvAsync(order, $"order_{orderId}.csv");
    }
}
```

### Unit Test (After)

```csharp
[Fact]
public async Task LoadOrdersAsync_PopulatesOrders()
{
    var mockData = new Mock<IOrderDataService>();
    mockData.Setup(d => d.FetchOrdersAsync(It.IsAny<int>()))
        .ReturnsAsync(new List<Order>
        {
            new() { Id = 1, Product = "Widget" }
        });

    var mockAuth = new Mock<IAuthService>();
    mockAuth.Setup(a => a.CurrentUserId).Returns(42);

    var vm = new OrderViewModel(mockData.Object, mockAuth.Object, Mock.Of<IExportService>());

    await vm.LoadOrdersAsync();

    Assert.Single(vm.Orders);
    Assert.Equal("Loaded 1 orders", vm.Status);
}
```

No global setup, no cleanup, no shared state.

---

## Example 2: ViewModelFactory with IServiceProvider

**What you'll learn:** How to create ViewModels dynamically at runtime using injected `IServiceProvider` instead of a static locator, and how to register the factory in the DI container.

### Scenario

A dashboard application shows different widget types. Each widget has its own ViewModel. The user adds widgets at runtime by selecting from a menu.

### Factory Implementation

```csharp
public interface IWidgetViewModel
{
    string DisplayName { get; }
    Task RefreshAsync();
}

public class WeatherWidgetViewModel : ObservableObject, IWidgetViewModel
{
    public string DisplayName => "Weather";
    // ...
}

public class StockWidgetViewModel : ObservableObject, IWidgetViewModel
{
    public string DisplayName => "Stock Ticker";
    // ...
}

public class CalendarWidgetViewModel : ObservableObject, IWidgetViewModel
{
    public string DisplayName => "Calendar";
    // ...
}
```

### The Factory (DI-Safe)

```csharp
public class WidgetFactory
{
    private readonly IServiceProvider _provider;

    public WidgetFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IWidgetViewModel Create(string widgetType)
    {
        return widgetType.ToLowerInvariant() switch
        {
            "weather" => _provider.GetRequiredService<WeatherWidgetViewModel>(),
            "stock" => _provider.GetRequiredService<StockWidgetViewModel>(),
            "calendar" => _provider.GetRequiredService<CalendarWidgetViewModel>(),
            _ => throw new ArgumentException($"Unknown widget: {widgetType}")
        };
    }
}
```

### Registration

```csharp
services.AddTransient<WeatherWidgetViewModel>();
services.AddTransient<StockWidgetViewModel>();
services.AddTransient<CalendarWidgetViewModel>();
services.AddTransient<WidgetFactory>();
```

### Usage in the Shell ViewModel

```csharp
public partial class DashboardViewModel : ObservableObject
{
    private readonly WidgetFactory _factory;

    [ObservableProperty]
    private ObservableCollection<IWidgetViewModel> _widgets = new();

    public DashboardViewModel(WidgetFactory factory)
    {
        _factory = factory;
    }

    [RelayCommand]
    private void AddWidget(string widgetType)
    {
        var widget = _factory.Create(widgetType);
        Widgets.Add(widget);
    }
}
```

### Why This Beats a Static Locator

- `WidgetFactory` is injected — no global state
- The factory explicitly declares which types it can create via the switch expression
- Each widget ViewModel still receives its own dependencies through constructor injection
- The factory itself is testable with a real `ServiceProvider` or a mock

---

## Example 3: View Locator (Acceptable Locator Use)

**What you'll learn:** The one place where a simple locator is the idiomatic Avalonia approach — mapping ViewModels to Views — and how to implement it cleanly.

### Scenario

Avalonia's `DataTemplate` system needs to resolve a `Control` (view) from a ViewModel type. The framework calls into this at runtime. Since views are framework-created, constructor injection is not available here.

### ViewLocator Implementation

```csharp
public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type> _mappings = new()
    {
        { typeof(HomeViewModel), typeof(HomeView) },
        { typeof(DashboardViewModel), typeof(DashboardView) },
        { typeof(SettingsViewModel), typeof(SettingsView) },
        { typeof(ReportsViewModel), typeof(ReportsView) },
    };

    public Control Build(object? data)
    {
        if (data is null) return new TextBlock { Text = "No data" };

        var viewModelType = data.GetType();
        if (_mappings.TryGetValue(viewModelType, out var viewType))
        {
            var control = (Control)Activator.CreateInstance(viewType)!;
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = $"View not found: {viewModelType.Name}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
```

### Wiring in App.axaml.cs

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow
        {
            DataContext = AppHost.Services.GetRequiredService<ShellViewModel>()
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

### Why This Is Acceptable

- The locator is scoped to a single concern: type-to-type mapping
- It is deterministic — the same ViewModel type always resolves to the same view
- No lifecycle management is needed (views are transient by design)
- The locator is injected into Avalonia's template system, not into application code
- It is trivially testable and replaceable

### Test

```csharp
[Fact]
public void ViewLocator_ResolvesHomeViewModel()
{
    var locator = new ViewLocator();
    var vm = new HomeViewModel(Mock.Of<INavigationService>());

    var result = locator.Build(vm);

    Assert.IsType<HomeView>(result);
    Assert.Same(vm, result.DataContext);
}
```

---

## Example 4: Legacy Migration — Phased Replacement

**What you'll learn:** How to incrementally migrate a large codebase from service locator to DI without a big-bang rewrite, using an intermediary abstraction.

### Phase 1: Introduce IServiceResolver

```csharp
// New abstraction — all existing Locator.Get calls move behind this
public interface IServiceResolver
{
    T Resolve<T>() where T : notnull;
}

// Backed by the old static locator — no behaviour change yet
public class LegacyResolver : IServiceResolver
{
    public T Resolve<T>() where T : notnull => Locator.Get<T>();
}
```

Every ViewModel that uses `Locator.Get<T>()` is refactored to receive `IServiceResolver` via constructor injection. This is mechanical and safe — the resolver behaves identically to the locator.

```csharp
// Before
public class InvoiceViewModel
{
    public async Task GenerateInvoiceAsync(int orderId)
    {
        var printer = Locator.Get<IInvoicePrinter>();
        var data = Locator.Get<IOrderDataService>();
        // ...
    }
}

// After
public class InvoiceViewModel
{
    private readonly IServiceResolver _resolver;

    public InvoiceViewModel(IServiceResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task GenerateInvoiceAsync(int orderId)
    {
        var printer = _resolver.Resolve<IInvoicePrinter>();
        var data = _resolver.Resolve<IOrderDataService>();
        // ...
    }
}
```

Registration in the composition root:

```csharp
services.AddSingleton<IServiceResolver, LegacyResolver>();
```

### Phase 2: Switch to DI-Backed Resolver

Once all ViewModels use `IServiceResolver`, replace the backing implementation:

```csharp
public class DiServiceResolver : IServiceResolver
{
    private readonly IServiceProvider _provider;

    public DiServiceResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Resolve<T>() where T : notnull =>
        _provider.GetRequiredService<T>();
}
```

Swap the registration — no other code changes:

```csharp
services.AddSingleton<IServiceResolver, DiServiceResolver>();
```

### Phase 3: Eliminate the Resolver

One ViewModel at a time, replace `IServiceResolver` with the actual service interfaces:

```csharp
public class InvoiceViewModel
{
    private readonly IInvoicePrinter _printer;
    private readonly IOrderDataService _data;

    public InvoiceViewModel(IInvoicePrinter printer, IOrderDataService data)
    {
        _printer = printer;
        _data = data;
    }

    public async Task GenerateInvoiceAsync(int orderId)
    {
        await _printer.PrintAsync(orderId);
        // ...
    }
}
```

Remove `IServiceResolver` when no classes reference it.

### Migration Summary

| Phase | Change | Risk | Effort |
|-------|--------|------|--------|
| 1 | Extract `IServiceResolver` interface | Low — mechanical refactor | 1-2 days for large codebase |
| 2 | Swap implementation to DI | Low — single-line change | 1 hour |
| 3 | Eliminate resolver class by class | Medium — requires understanding each ViewModel's true deps | 1-2 weeks |

---

## Key Takeaways

- **Refactoring from locator to DI** is mechanical: identify hidden `Locator.Get<T>()` calls, add constructor parameters, update callers.
- **Factory patterns should use injected `IServiceProvider`**, not a static locator. This keeps factories testable and DI-compatible.
- **View locators are the exception** — they are narrow, deterministic mapping tables that Avalonia's template system requires.
- **Legacy migration** should be phased: introduce an abstraction, swap the implementation, then eliminate the abstraction class by class.
- All examples above are testable without global state setup or teardown.

---

## See Also

- [001 — Service Locator vs DI](001-service-locator-vs-di.md)
- [001V — Service Locator vs DI: In-Depth Companion](001-service-locator-vs-di-verbose.md)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
