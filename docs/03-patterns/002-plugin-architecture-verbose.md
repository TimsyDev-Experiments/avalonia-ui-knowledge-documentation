---
tier: advanced
topic: architecture
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 002-plugin-architecture.md
---

# 002V — Modular App with Plugin-Style Views: An In-Depth Companion

You should already have read: [002 — Modular App with Plugin-Style Views](002-plugin-architecture.md) for the quick-start version. This file goes deeper on every section.

---

## Prerequisites

- [002 — Modular App with Plugin-Style Views](002-plugin-architecture.md)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- Familiarity with `Microsoft.Extensions.DependencyInjection` and `Scrutor`

---

## 1. Why Plugin Architecture Matters

A plugin architecture enforces a contract between the host and feature modules. Each plugin owns its dependencies, views, and ViewModels. The host only knows about the plugin contract — not the internals of any plugin.

### Benefits at Scale

| Concern | Monolithic App | Plugin-Based App |
|---------|---------------|------------------|
| Team ownership | Shared codebase, merge conflicts | Each team owns their plugin project |
| Feature toggling | Conditional compilation or if/else | Remove plugin DLL, done |
| Onboarding | Must understand entire app | Understand one plugin contract |
| Testing | Mock everything | Isolated plugin registration tests |
| Deployment | One binary per platform | Plugin DLLs deployable independently |

---

## 2. Detailed Plugin Interface Design

### 2.1 Why `RegisterServices` and `RegisterViews` Are Separate

The core page defines `IAppPlugin` with two registration methods. Keeping them separate serves two purposes:

1. **Explicit separation of concerns**: services belong in the DI container, views belong in a view-locator or template registry. Combining them would conflate two different resolution mechanisms.

2. **Future extensibility**: if you add runtime plugin hot-reload later, services may need different lifecycle handling than views.

### 2.2 Adding Lifecycle Hooks

Real-world plugins often need startup and shutdown callbacks:

```csharp
public interface IAppPlugin
{
    string Name { get; }
    Version Version { get; }
    void RegisterServices(IServiceCollection services);
    void RegisterViews(IServiceCollection services);
    Task OnPluginLoadedAsync(IServiceProvider provider);
    Task OnPluginUnloadedAsync(IServiceProvider provider);
}
```

The host calls `OnPluginLoadedAsync` after building the container:

```csharp
public static async Task LoadPluginsAsync(IServiceCollection services)
{
    var plugins = DiscoverPlugins();
    foreach (var plugin in plugins)
    {
        plugin.RegisterServices(services);
    }

    var provider = services.BuildServiceProvider();

    foreach (var plugin in plugins)
    {
        await plugin.OnPluginLoadedAsync(provider);
    }
}
```

### 2.3 Plugin Metadata

For richer plugin discovery, add metadata attributes:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PluginMetadataAttribute : Attribute
{
    public string DisplayName { get; }
    public string Description { get; }
    public string Author { get; }

    public PluginMetadataAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

[PluginMetadata("Invoice Generator", Description = "Generates PDF invoices")]
public class InvoicePlugin : IAppPlugin
{
    public string Name => "Invoice";
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<IInvoiceService, InvoiceService>();
        services.AddTransient<InvoiceViewModel>();
        services.AddTransient<InvoiceView>();
    }
    public void RegisterViews(IServiceCollection services) { }
}
```

Discovery with metadata:

```csharp
public static List<(IAppPlugin Plugin, PluginMetadataAttribute Metadata)> DiscoverPlugins()
{
    return AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => !t.IsAbstract && typeof(IAppPlugin).IsAssignableFrom(t))
        .Select(t => (
            Plugin: (IAppPlugin)Activator.CreateInstance(t)!,
            Metadata: t.GetCustomAttribute<PluginMetadataAttribute>()))
        .ToList();
}
```

---

## 3. Assembly Scanning in Depth

### 3.1 Convention-Based Discovery with Scrutor

The core page shows `ServiceCollection.Scan()` using Scrutor. This enables convention-driven registration without each plugin explicitly registering every type:

```csharp
services.Scan(scan => scan
    .FromAssemblies(pluginAssemblies)
    .AddClasses(classes => classes.AssignableTo<IViewModel>())
        .AsImplementedInterfaces()
        .WithTransientLifetime()
    .AddClasses(classes => classes.AssignableTo<IView>())
        .AsSelf()
        .WithTransientLifetime()
    .AddClasses(classes => classes.AssignableTo<IService>())
        .AsImplementedInterfaces()
        .WithSingletonLifetime());
```

### 3.2 Custom Convention Builder

For more control, define a custom convention class:

```csharp
public class PluginConvention : IRegistrationConvention
{
    public void Process(IServiceCollection services, ITypeSource typeSource)
    {
        var types = typeSource.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            if (type.Name.EndsWith("ViewModel"))
            {
                services.AddTransient(type);
            }
            else if (type.Name.EndsWith("View"))
            {
                services.AddTransient(type);
            }
            else if (type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRepository<>)))
            {
                services.AddSingleton(type);
            }
        }
    }
}
```

Usage:

```csharp
services.Scan(scan => scan
    .FromAssemblies(pluginAssemblies)
    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
    .AddConvention<PluginConvention>());
```

### 3.3 Assembly Loading Strategies

When loading plugins from external DLLs, different loading contexts affect behaviour:

| Method | Behaviour | When to Use |
|--------|-----------|-------------|
| `Assembly.LoadFrom` | Loads into the default context. Locks the DLL file. | Simple plugin loading, no unloading needed |
| `Assembly.Load(byte[]) | Loads without locking. Can create type identity issues. | Needs to delete/replace the DLL at runtime |
| `AssemblyLoadContext` (ALC) | Isolated context. Supports unloading. | Hot-reload, per-plugin isolation |

**AssemblyLoadContext example for plugin isolation:**

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
            return LoadFromAssemblyPath(assemblyPath);
        return null!;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var filePath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (filePath is not null)
            return LoadUnmanagedDllFromPath(filePath);
        return IntPtr.Zero;
    }
}
```

Usage:

```csharp
public static IAppPlugin LoadPlugin(string dllPath)
{
    var context = new PluginLoadContext(dllPath);
    var assembly = context.LoadFromAssemblyName(
        new AssemblyName(Path.GetFileNameWithoutExtension(dllPath)));

    var pluginType = assembly.GetTypes()
        .First(t => !t.IsAbstract && typeof(IAppPlugin).IsAssignableFrom(t));

    return (IAppPlugin)Activator.CreateInstance(pluginType)!;
}
```

---

## 4. Advanced Navigation Integration

### 4.1 Region-Based Navigation

Instead of a single `ContentControl`, use named regions that plugins register for:

```csharp
public interface IPluginNavigation
{
    string Route { get; }
    string Region { get; }
    Type ViewModelType { get; }
}

public class ShellViewModel
{
    private readonly Dictionary<string, List<IPluginNavigation>> _regions;

    public ShellViewModel(IEnumerable<IPluginNavigation> plugins)
    {
        _regions = plugins
            .GroupBy(p => p.Region)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void NavigateTo(string region, string route)
    {
        if (!_regions.TryGetValue(region, out var regionPlugins))
            return;

        var nav = regionPlugins.FirstOrDefault(p => p.Route == route);
        if (nav is null) return;

        // Resolve and set the view for the specific region
    }
}
```

### 4.2 Navigation with Parameters

```csharp
public interface IPluginNavigation
{
    string Route { get; }
    Type ViewModelType { get; }
    Task NavigateToAsync(INavigationContext context);
}

public class NavigationContext
{
    public required string Route { get; init; }
    public Dictionary<string, object> Parameters { get; } = new();
}

public class DashboardNavigation : IPluginNavigation
{
    public string Route => "dashboard";
    public Type ViewModelType => typeof(DashboardViewModel);

    public async Task NavigateToAsync(INavigationContext context)
    {
        var vm = AppHost.Services.GetRequiredService<DashboardViewModel>();

        if (context.Parameters.TryGetValue("period", out var period))
            vm.SelectedPeriod = period.ToString();

        await vm.InitializeAsync();
        // View resolution happens separately
    }
}
```

---

## 5. Plugin Testing in Depth

### 5.1 Verifying All Registrations

```csharp
[Fact]
public void DashboardPlugin_AllRegistrationsResolve()
{
    var services = new ServiceCollection();
    var plugin = new DashboardPlugin();

    plugin.RegisterServices(services);

    var provider = services.BuildServiceProvider();

    // Verify every registered service resolves
    using var scope = provider.CreateScope();
    var sp = scope.ServiceProvider;

    Assert.NotNull(sp.GetService<DashboardViewModel>());
    Assert.NotNull(sp.GetService<DashboardView>());
    Assert.NotNull(sp.GetService<IDashboardService>());
}
```

### 5.2 Lifetime Verification

```csharp
[Fact]
public void DashboardPlugin_ViewModelsAreTransient()
{
    var services = new ServiceCollection();
    var plugin = new DashboardPlugin();

    plugin.RegisterServices(services);

    var provider = services.BuildServiceProvider();

    var first = provider.GetRequiredService<DashboardViewModel>();
    var second = provider.GetRequiredService<DashboardViewModel>();

    Assert.NotSame(first, second);
}
```

### 5.3 Plugin Dependency Test

```csharp
[Fact]
public void ReportPlugin_DependsOnExportService()
{
    var services = new ServiceCollection();
    var plugin = new ReportPlugin();

    plugin.RegisterServices(services);

    // ReportViewModel requires IExportService — if plugin does not register it, this throws
    // This verifies the plugin either registers the dependency or the host provides it
    var provider = services.BuildServiceProvider();

    // Host-provided service should be registered by the composition root
    // For isolated test, we register a mock
    services.AddSingleton(Mock.Of<IExportService>());
    provider = services.BuildServiceProvider();

    var vm = provider.GetRequiredService<ReportViewModel>();
    Assert.NotNull(vm);
}
```

---

## 6. Key Takeaways (Expanded)

- **Define a clear plugin contract** with `IAppPlugin`. Keep `RegisterServices` separate from `RegisterViews` for clarity and extensibility.
- **Discover plugins at startup** by scanning assemblies. Convention-based scanning (via Scrutor) reduces boilerplate in each plugin.
- **Each plugin owns its DI registrations.** The plugin knows what services, ViewModels, and views it needs. The host only knows about `IAppPlugin`.
- **Use a `ContentControl` bound to `CurrentView`** as the plugin host. The shell ViewModel resolves and assigns the active ViewModel.
- **Load external assemblies from a `plugins/` directory** for true plug-and-play. Use `AssemblyLoadContext` for isolation when hot-reload is needed.
- **Test plugins in isolation** by calling `RegisterServices` directly. Verify both correct registration and correct lifetimes.
- **Add lifecycle hooks** (`OnPluginLoadedAsync`, `OnPluginUnloadedAsync`) for real-world plugins that need startup initialisation or graceful shutdown.

---

## See Also

- [002 — Modular App with Plugin-Style Views](002-plugin-architecture.md) (core file)
- [Pattern: Service Locator vs DI](001-service-locator-vs-di.md)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Scrutor GitHub](https://github.com/khellang/Scrutor)
- [Avalonia Docs: ContentControl](https://docs.avaloniaui.net/controls/contentcontrol)
