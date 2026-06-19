---
tier: intermediate
topic: architecture
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 001-service-locator-vs-di.md
---

# 001V — Service Locator vs DI: An In-Depth Companion

You should already have read: [001 — Service Locator vs DI](001-service-locator-vs-di.md) for the quick-start version. This file goes deeper on every section.

---

## Prerequisites

- [001 — Service Locator vs DI](001-service-locator-vs-di.md) (the core companion page)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- Familiarity with `Microsoft.Extensions.DependencyInjection`

---

## 1. Why the Service Locator Pattern Persists

Despite being labelled an anti-pattern, the service locator remains common in Avalonia codebases. Understanding *why* developers reach for it helps you recognise when its use hides a deeper design issue.

### 1.1 Framework Convenience

Avalonia's own `Application.Current` is a service locator. Developers familiar with WPF's `Application.Current` and `FindResource` naturally extend that pattern to their own services:

```csharp
var dialog = (IDialogService)Application.Current.FindResource("DialogService");
```

The framework does not discourage this — it provides no built-in container integration. Developers must bring their own DI setup. When they skip that step, the locator fills the gap.

### 1.2 Perceived Simplicity

A static locator requires zero setup in the composition root:

```csharp
// No DI config needed — just register and go
Locator.Register<IDialogService>(new DialogService());
```

Compare to DI where you must configure a `ServiceCollection`, choose a lifetime, and ensure every consumer receives its dependencies through constructors. The locator's "just get it" ergonomics feel faster in small projects.

### 1.3 Framework-Created Objects

Avalonia creates certain objects itself — windows, user controls, data templates. These objects do not go through the DI container, so developers inside them cannot use constructor injection. The locator becomes the escape hatch:

```csharp
public partial class MyUserControl : UserControl
{
    public MyUserControl()
    {
        InitializeComponent();
        // Cannot inject — framework calls this constructor
        DataContext = Locator.Get<MyViewModel>();
    }
}
```

---

## 2. Detailed Drawbacks of Service Locator

### 2.1 Hidden Dependencies Break Static Analysis

With constructor injection, a simple grep reveals every dependency:

```bash
# Find all constructor parameters
rg "public \w+ViewModel\(" src/
```

With a locator, dependencies are scattered across methods, properties, and event handlers. No tool can discover them reliably:

```csharp
public class OrderViewModel
{
    public async Task PlaceOrderAsync()
    {
        // Which services does this depend on?
        var billing = Locator.Get<IBillingService>();
        var inventory = Locator.Get<IInventoryService>();
        var email = Locator.Get<IEmailService>();
        // ...
    }

    public void CancelOrder()
    {
        var billing = Locator.Get<IBillingService>();
        var refund = Locator.Get<IRefundService>();
        // Different set of dependencies!
    }
}
```

Code reviews miss these. Refactoring one service interface breaks call sites the compiler cannot find.

### 2.2 Test Isolation Requires Global State Management

Every unit test that exercises code using a locator must set up the global registry *before* the test runs and *tear it down* after:

```csharp
public class OrderViewModelTests : IDisposable
{
    public OrderViewModelTests()
    {
        // Must remember to register every service the SUT needs
        Locator.Reset();
        Locator.Register<IBillingService>(new MockBillingService());
        Locator.Register<IInventoryService>(new MockInventoryService());
        Locator.Register<IEmailService>(new MockEmailService());
    }

    [Fact]
    public void PlaceOrder_CallsBillingService()
    {
        var vm = new OrderViewModel();
        await vm.PlaceOrderAsync();
        // ...
    }

    public void Dispose()
    {
        Locator.Reset(); // Must clean up or tests leak state
    }
}
```

If a test forgets to register a service, the locator throws at runtime — not at compile time. If a test forgets to reset, shared state corrupts subsequent tests. Parallel test execution becomes impossible because the global state races between threads.

Compare to constructor injection:

```csharp
[Fact]
public void PlaceOrder_CallsBillingService()
{
    var billing = new Mock<IBillingService>();
    var inventory = new Mock<IInventoryService>();
    var email = new Mock<IEmailService>();

    // All deps explicit — no global state
    var vm = new OrderViewModel(billing.Object, inventory.Object, email.Object);
    await vm.PlaceOrderAsync();
}
```

### 2.3 Lifecycle Ambiguity

A service locator typically stores references in a `Dictionary<Type, object>`. This gives every registration singleton semantics — the first registration is the only instance forever:

```csharp
Locator.Register<IDataService>(new DataService());
// Every consumer gets the same instance
var a = Locator.Get<IDataService>();
var b = Locator.Get<IDataService>();
ReferenceEquals(a, b); // true
```

There is no way to express "create a new instance every time" (transient) or "one instance per scope" (scoped) at the call site. If someone wants transient behaviour, they must register a factory explicitly:

```csharp
public static class Locator
{
    private static readonly Dictionary<Type, Func<object>> _factories = new();

    public static void RegisterFactory<T>(Func<T> factory) =>
        _factories[typeof(T)] = () => factory()!;

    public static T Get<T>() where T : notnull
    {
        if (_factories.TryGetValue(typeof(T), out var factory))
            return (T)factory();
        return (T)_services[typeof(T)];
    }
}
```

But now the locator has two registration APIs (`Register` and `RegisterFactory`), and callers cannot tell which is which at the call site.

### 2.4 Ambient Coupling and Surprising Behaviour

Because any module can call `Locator.Get<T>()` at any time, the locator creates implicit coupling between unrelated parts of the system:

```csharp
// In a ViewModel constructor — harmless registration?
Locator.Register<IReportingService>(new ReportingService());
```

Another developer, debugging a crash in an unrelated feature, discovers that `IReportingService` is unexpectedly available because the reporting module initialised first. Removing the reporting module breaks the other feature silently.

---

## 3. Deep Dive: Why DI Is Preferable

### 3.1 Explicit Dependency Graphs

Constructor injection makes the dependency graph visible at a glance:

```csharp
public class CheckoutViewModel
{
    public CheckoutViewModel(
        ICartService cart,
        IBillingService billing,
        IInventoryService inventory,
        IEmailService email,
        INavigationService navigation)
    {
        // Every dependency is visible in the signature
    }
}
```

Any DI container can verify the entire graph at startup:

```csharp
var provider = services.BuildServiceProvider();
// Validates all registrations — crashes fast if something is missing
provider.GetRequiredService<CheckoutViewModel>();
```

### 3.2 Container-Managed Lifetimes

DI containers provide three standard lifetimes with precise semantics:

| Lifetime | Behaviour | Typical Use |
|----------|-----------|-------------|
| `Singleton` | One instance per container | Shared state, cache, configuration |
| `Scoped` | One instance per scope (e.g., per request) | Unit of work, DbContext |
| `Transient` | New instance every resolution | Lightweight stateless services, ViewModels |

These are declared at registration time, not at the call site, and the container guarantees correctness:

```csharp
services.AddSingleton<IConfigService, ConfigService>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddTransient<ICartService, CartService>();
```

### 3.3 Disposal Tracking

DI containers track `IDisposable` and `IAsyncDisposable` instances. When the container is disposed, it cleans up all managed instances in reverse order. Service locators typically leak disposables unless manually tracked.

```csharp
public class ScopedService : IDisposable
{
    public void Dispose() => Console.WriteLine("Disposed");
}

var services = new ServiceCollection();
services.AddScoped<IScopedService, ScopedService>();
using var scope = services.BuildServiceProvider().CreateScope();
var svc = scope.ServiceProvider.GetRequiredService<IScopedService>();
// Disposed automatically when scope is disposed
```

### 3.4 Decorator and Interception Support

DI containers (via libraries like Scrutor) support decorating registered services without changing the consumer:

```csharp
services.AddSingleton<IDataService, DataService>();
services.Decorate<IDataService, CachingDataService>();
services.Decorate<IDataService, LoggingDataService>();

// Consumer sees a chain: Logging -> Caching -> DataService
public class ReportViewModel
{
    public ReportViewModel(IDataService data) { }
    // data is the decorated chain — consumer is unaware
}
```

Achieving the same with a service locator requires manual composition of the decorator chain.

---

## 4. Deeper Look at Acceptable Locator Scenarios

### 4.1 View Locator (ViewModel-to-View Resolution)

Avalonia's `DataTemplate` system resolves views from ViewModel types. This is a *mapping* concern, not a *service resolution* concern. The community's `ViewLocator` class (included in project templates) uses a simple dictionary:

```csharp
public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type> _map = new()
    {
        { typeof(HomeViewModel), typeof(HomeView) },
        { typeof(DashboardViewModel), typeof(DashboardView) },
    };

    public Control Build(object? data)
    {
        var type = data?.GetType();
        if (type is not null && _map.TryGetValue(type, out var viewType))
            return (Control)Activator.CreateInstance(viewType)!;
        return new TextBlock { Text = "View not found" };
    }

    public bool Match(object? data) =>
        data is ViewModelBase;
}
```

This is acceptable because:

- The scope is narrow (one type mapping, not arbitrary service resolution)
- The mapping is deterministic and testable
- There is no lifecycle management (views are transient by nature)
- Replacing it with a different strategy does not affect the rest of the application

### 4.2 Factory Pattern with IServiceProvider

When you need dynamic type selection at runtime, inject `IServiceProvider` rather than a static locator:

```csharp
public class ReportFactory
{
    private readonly IServiceProvider _provider;

    public ReportFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IReportViewModel CreateReport(string reportType)
    {
        return reportType switch
        {
            "sales" => _provider.GetRequiredService<SalesReportViewModel>(),
            "inventory" => _provider.GetRequiredService<InventoryReportViewModel>(),
            "audit" => _provider.GetRequiredService<AuditReportViewModel>(),
            _ => throw new ArgumentException($"Unknown report: {reportType}")
        };
    }
}
```

This is DI-compatible — the factory itself is injected, the container resolves it, and the underlying `IServiceProvider` is a framework-provided abstraction, not a custom static registry.

### 4.3 Legacy Code Migration

When migrating a legacy codebase that uses service locator everywhere, adopt a phased strategy:

**Phase 1 — Wrap the locator behind an interface:**

```csharp
public interface IServiceResolver
{
    T Resolve<T>() where T : notnull;
}

public class LocatorResolver : IServiceResolver
{
    public T Resolve<T>() where T : notnull =>
        Locator.Get<T>();
}
```

Now every class that uses the locator depends on `IServiceResolver` (injected via constructor), not on the static `Locator` class.

**Phase 2 — Replace implementation with DI-backed resolver:**

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

**Phase 3 — Eliminate the resolver, inject dependencies directly:**

One by one, replace `IServiceResolver` parameters with the actual service interfaces until the resolver is removed entirely.

---

## 5. Advanced: IServiceProvider Factory Patterns

### 5.1 Named Services via Factory

When you need multiple implementations of the same interface:

```csharp
public interface IExportService
{
    string Format { get; }
    Task ExportAsync(IEnumerable<Record> records, Stream target);
}

public class CsvExportService : IExportService { /* ... */ }
public class JsonExportService : IExportService { /* ... */ }
public class XmlExportService : IExportService { /* ... */ }

public class ExportServiceFactory
{
    private readonly IServiceProvider _provider;

    public ExportServiceFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IExportService GetExporter(string format)
    {
        var exporters = _provider.GetServices<IExportService>();
        return exporters.First(e =>
            e.Format.Equals(format, StringComparison.OrdinalIgnoreCase));
    }
}
```

Registration:

```csharp
services.AddTransient<IExportService, CsvExportService>();
services.AddTransient<IExportService, JsonExportService>();
services.AddTransient<IExportService, XmlExportService>();
services.AddTransient<ExportServiceFactory>();
```

### 5.2 Lazy Service Resolution with Lazy<T>

When a dependency is expensive to create and rarely used:

```csharp
public class AnalyticsViewModel
{
    private readonly Lazy<IHeavyReportingService> _heavy;

    public AnalyticsViewModel(Lazy<IHeavyReportingService> heavy)
    {
        _heavy = heavy;
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        // Heavy service is created only when this command executes
        var report = await _heavy.Value.GenerateAsync();
    }
}
```

Requires registration of `Lazy<T>` support (available via `Microsoft.Extensions.DependencyInjection.Abstractions` or a helper):

```csharp
services.AddTransient(typeof(Lazy<>), typeof(LazyService<>));

public class LazyService<T> : Lazy<T>
{
    public LazyService(IServiceProvider provider)
        : base(() => provider.GetRequiredService<T>())
    {
    }
}
```

---

## 6. Key Takeaways (Expanded)

- **Constructor injection is the default.** It makes dependencies explicit, enables compile-time verification, simplifies testing, and gives the container control over lifetimes and disposal.
- **Service locator hides coupling.** It makes code harder to analyse, test, and refactor. No tool can discover which services a method depends on.
- **Global state breaks test isolation.** Tests that use a locator must manage shared mutable state, which prevents parallelisation and creates order-dependent failures.
- **Acceptable locator use is narrow.** View locators map types deterministically. Factory patterns should use injected `IServiceProvider`. Legacy migrations should adopt a phased replacement strategy.
- **DI containers provide lifecycle management.** Singleton, scoped, and transient lifetimes are declared at registration and enforced by the container, with automatic disposal tracking.
- **All ViewModels in this documentation use constructor injection** through `Microsoft.Extensions.DependencyInjection`. Every example is testable without global setup.

---

## See Also

- [001 — Service Locator vs DI](001-service-locator-vs-di.md) (core file)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Pattern: Modular App with Plugin-Style Views](002-plugin-architecture.md)
- [Avalonia Docs: MVVM Architecture](https://docs.avaloniaui.net/docs/concepts/mvvm)
