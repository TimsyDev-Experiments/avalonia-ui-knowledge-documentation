---
tier: advanced
topic: architecture
estimated: 15 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# Pattern: Modular App with Plugin-Style Views

**What you'll learn:** How to structure an Avalonia application so that features are self-contained plugins that can be added, removed, or replaced independently, with their own views, ViewModels, and DI registrations.

**Prerequisites:** [032 -- MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)

---

## Problem

As an Avalonia application grows, a monolithic structure becomes hard to maintain. Different teams own different features. Features need to be added or removed without touching shared code. A plugin architecture solves this by modelling each feature as an independent module that registers itself with the host.

---

## Solution

Define a plugin contract, implement features against it, and let the composition root discover and register plugins automatically.

### Plugin interface

```csharp
public interface IAppPlugin
{
    string Name { get; }
    void RegisterServices(IServiceCollection services);
    void RegisterViews(IServiceCollection services);
}
```

### Navigation integration

```csharp
public interface IPluginNavigation
{
    string Route { get; }
    Type ViewModelType { get; }
    Type ViewType { get; }
}
```

---

## Implementation

### Step 1: Create a plugin

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace DemoApp.Plugins.Dashboard;

public class DashboardPlugin : IAppPlugin
{
    public string Name => "Dashboard";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardView>();
    }

    public void RegisterViews(IServiceCollection services)
    {
        // Views registered alongside services for clarity
    }
}
```

### Step 2: Discover plugins at startup

```csharp
public static void ConfigureServices(HostBuilderContext context,
    IServiceCollection services)
{
    // Convention-based discovery: all types implementing IAppPlugin
    var pluginTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => !t.IsAbstract
                 && typeof(IAppPlugin).IsAssignableFrom(t));

    foreach (var type in pluginTypes)
    {
        var plugin = (IAppPlugin)Activator.CreateInstance(type)!;
        plugin.RegisterServices(services);
        plugins.Add(plugin);
    }
}
```

### Step 3: Plugin-aware navigation

```csharp
public partial class ShellViewModel : ObservableObject
{
    private readonly List<IPluginNavigation> _plugins;

    [ObservableProperty]
    private object? _currentView;

    public ShellViewModel(IEnumerable<IPluginNavigation> plugins)
    {
        _plugins = plugins.ToList();
    }

    [RelayCommand]
    private void NavigateTo(string route)
    {
        var nav = _plugins.FirstOrDefault(p => p.Route == route);
        if (nav is null) return;

        var vm = Program.AppHost.Services
            .GetRequiredService(nav.ViewModelType);
        CurrentView = vm;
    }
}
```

### Step 4: Plugin host view

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="DemoApp.Views.ShellWindow"
        Title="Modular App">
  <DockPanel>
    <StackPanel DockPanel.Dock="Left" Spacing="4" Margin="8">
      <Button Content="Dashboard"
              Command="{Binding NavigateToCommand}"
              CommandParameter="dashboard" />
      <Button Content="Reports"
              Command="{Binding NavigateToCommand}"
              CommandParameter="reports" />
      <Button Content="Settings"
              Command="{Binding NavigateToCommand}"
              CommandParameter="settings" />
    </StackPanel>

    <ContentControl Content="{Binding CurrentView}"
                    Margin="16" />
  </DockPanel>
</Window>
```

---

## Variant: Convention-Based Registration

Instead of each plugin calling `RegisterServices`, scan assemblies by convention:

```csharp
services.Scan(scan => scan
    .FromAssemblies(pluginAssemblies)
    .AddClasses(classes => classes.AssignableTo<IViewModel>())
        .AsImplementedInterfaces()
        .WithTransientLifetime()
    .AddClasses(classes => classes.AssignableTo<IView>())
        .AsSelf()
        .WithTransientLifetime());
```

Requires `Scrutor` NuGet package:

```bash
dotnet add package Scrutor
```

---

## Variant: Plugin Assembly Loading

Load plugins from external `.dll` files at runtime:

```csharp
public static IEnumerable<Assembly> LoadPluginAssemblies(string directory)
{
    if (!Directory.Exists(directory)) yield break;

    foreach (var dll in Directory.GetFiles(directory, "*.Plugin.dll"))
    {
        var assembly = Assembly.LoadFrom(dll);
        yield return assembly;
    }
}
```

Used in the composition root:

```csharp
var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");
var pluginAssemblies = LoadPluginAssemblies(pluginDir).ToArray();

foreach (var assembly in pluginAssemblies)
{
    var plugins = assembly.GetTypes()
        .Where(t => !t.IsAbstract
                 && typeof(IAppPlugin).IsAssignableFrom(t));
    foreach (var type in plugins)
    {
        var plugin = (IAppPlugin)Activator.CreateInstance(type)!;
        plugin.RegisterServices(services);
    }
}
```

---

## Testing a Plugin in Isolation

```csharp
[Fact]
public void DashboardPlugin_RegistersViewModel()
{
    var services = new ServiceCollection();
    var plugin = new DashboardPlugin();

    plugin.RegisterServices(services);

    var provider = services.BuildServiceProvider();
    var vm = provider.GetService<DashboardViewModel>();
    vm.Should().NotBeNull();
}
```

---

## Key Takeaways

- Define `IAppPlugin` as the contract for self-registering feature modules
- Discover plugins at startup by scanning assemblies (convention or explicit)
- Each plugin owns its DI registrations (services + views + ViewModels)
- Use a `ContentControl` bound to `CurrentView` as the plugin host
- Load external assemblies from a `plugins/` directory for true plug-and-play
- Test plugins in isolation by instantiating their `RegisterServices` method

---

## See Also

- [Pattern: Service Locator vs DI](001-service-locator-vs-di.md)
- [032 -- MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
- [Avalonia Docs: ContentControl](https://docs.avaloniaui.net/controls/contentcontrol)
