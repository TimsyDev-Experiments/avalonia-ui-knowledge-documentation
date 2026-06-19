---
tier: advanced
topic: architecture
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 002-plugin-architecture.md
---

# 002X — Modular App with Plugin Views: Real-World Examples

You should already have read: [002 — Modular App with Plugin-Style Views](002-plugin-architecture.md) for the core concepts. This file provides complete, worked examples.

---

## Example 1: Building a Three-Plugin App from Scratch

**What you'll learn:** How to structure an Avalonia solution with a host project and three plugin projects, each with its own ViewModel, view, and DI registrations.

### Solution Structure

```
Solution/
├── src/
│   ├── DemoApp/                     # Host application
│   │   ├── App.axaml
│   │   ├── App.axaml.cs
│   │   ├── Program.cs
│   │   ├── ShellView.axaml
│   │   ├── ShellView.axaml.cs
│   │   ├── ShellViewModel.cs
│   │   └── PluginLoader.cs
│   ├── DemoApp.Plugins.Dashboard/
│   │   ├── DashboardPlugin.cs
│   │   ├── DashboardViewModel.cs
│   │   └── DashboardView.axaml
│   ├── DemoApp.Plugins.Reports/
│   │   ├── ReportPlugin.cs
│   │   ├── ReportsViewModel.cs
│   │   └── ReportsView.axaml
│   └── DemoApp.Plugins.Settings/
│       ├── SettingsPlugin.cs
│       ├── SettingsViewModel.cs
│       └── SettingsView.axaml
└── DemoApp.Shared/
    ├── IAppPlugin.cs
    └── ViewModelBase.cs
```

### Shared Contract (DemoApp.Shared)

```csharp
public interface IAppPlugin
{
    string Name { get; }
    void RegisterServices(IServiceCollection services);
}
```

### Plugin 1: Dashboard

```csharp
namespace DemoApp.Plugins.Dashboard;

public class DashboardPlugin : IAppPlugin
{
    public string Name => "Dashboard";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<DashboardView>();
    }
}

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Dashboard";
}
```

```xml
<!-- DashboardView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="DemoApp.Plugins.Dashboard.DashboardView">
  <StackPanel Spacing="8" Margin="16">
    <TextBlock Text="{Binding WelcomeMessage}"
               FontSize="24" FontWeight="Bold" />
    <TextBlock Text="Dashboard content goes here." />
  </StackPanel>
</UserControl>
```

### Plugin 2: Reports

```csharp
namespace DemoApp.Plugins.Reports;

public class ReportPlugin : IAppPlugin
{
    public string Name => "Reports";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<ReportsView>();
    }
}

public partial class ReportsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<string> _reports = new()
    {
        "Monthly Summary",
        "Quarterly Analytics",
        "Annual Report"
    };
}
```

### Plugin 3: Settings

```csharp
namespace DemoApp.Plugins.Settings;

public class SettingsPlugin : IAppPlugin
{
    public string Name => "Settings";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsView>();
    }
}

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _notificationsEnabled = true;

    [ObservableProperty]
    private string _theme = "Light";
}
```

### Host: Plugin Loader

```csharp
public class PluginLoader
{
    public static List<IAppPlugin> LoadPlugins(IServiceCollection services)
    {
        var pluginTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(IAppPlugin).IsAssignableFrom(t))
            .ToList();

        var plugins = new List<IAppPlugin>();

        foreach (var type in pluginTypes)
        {
            var plugin = (IAppPlugin)Activator.CreateInstance(type)!;
            plugin.RegisterServices(services);
            plugins.Add(plugin);
        }

        return plugins;
    }
}
```

### Host: Shell ViewModel

```csharp
public partial class ShellViewModel : ObservableObject
{
    private readonly Dictionary<string, Type> _routes;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _title = "Modular App";

    public ShellViewModel(IEnumerable<IAppPlugin> plugins)
    {
        _routes = new Dictionary<string, Type>
        {
            { "dashboard", typeof(DashboardViewModel) },
            { "reports", typeof(ReportsViewModel) },
            { "settings", typeof(SettingsViewModel) },
        };
    }

    [RelayCommand]
    private void Navigate(string route)
    {
        if (_routes.TryGetValue(route, out var vmType))
        {
            CurrentView = AppHost.Services.GetRequiredService(vmType);
        }
    }
}
```

### Host: ShellView

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="DemoApp.ShellView"
        Title="{Binding Title}">
  <DockPanel>
    <StackPanel DockPanel.Dock="Left" Spacing="4" Margin="8" MinWidth="160">
      <Button Content="Dashboard"
              Command="{Binding NavigateCommand}"
              CommandParameter="dashboard" />
      <Button Content="Reports"
              Command="{Binding NavigateCommand}"
              CommandParameter="reports" />
      <Button Content="Settings"
              Command="{Binding NavigateCommand}"
              CommandParameter="settings" />
    </StackPanel>

    <ContentControl Content="{Binding CurrentView}"
                    Margin="16" />
  </DockPanel>
</Window>
```

---

## Example 2: Plugin with Scrutor Convention Registration

**What you'll learn:** How to use Scrutor to automatically register all services, ViewModels, and views in a plugin assembly by convention, eliminating the need for per-type registration in each plugin.

### Modified Plugin Interface (Minimal)

```csharp
public interface IAppPlugin
{
    string Name { get; }
    Assembly Assembly { get; }
}
```

### Plugin Implementation

```csharp
public class DashboardPlugin : IAppPlugin
{
    public string Name => "Dashboard";
    public Assembly Assembly => GetType().Assembly;
}
```

### Convention Registration in Host

```csharp
public static void RegisterPluginByConvention(
    IServiceCollection services, IAppPlugin plugin)
{
    services.Scan(scan => scan
        .FromAssemblies(plugin.Assembly)
        .AddClasses(classes => classes.Where(t =>
            t.Name.EndsWith("ViewModel")))
            .AsSelf()
            .WithTransientLifetime()
        .AddClasses(classes => classes.Where(t =>
            t.Name.EndsWith("View")))
            .AsSelf()
            .WithTransientLifetime()
        .AddClasses(classes => classes.Where(t =>
            t.Name.EndsWith("Service") || t.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());
}
```

### Benefits

- Plugins become thinner — they only declare their assembly and name
- Registration rules are centralised and consistent across all plugins
- Adding a new ViewModel or service in a plugin requires zero registration changes
- Naming conventions enforce team consistency

---

## Example 3: External Plugin DLL Loading

**What you'll learn:** How to load plugins from separate `.dll` files at runtime, enabling true plug-and-play where plugins can be added or removed by deploying files.

### Plugin Project Configuration

Each plugin project must be a separate `.csproj` that targets the same runtime:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputPath>..\DemoApp\bin\Debug\net9.0\plugins</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\DemoApp.Shared\DemoApp.Shared.csproj" />
    <ProjectReference Include="..\..\..\shared\DemoApp.Plugins.Base\DemoApp.Plugins.Base.csproj" />
  </ItemGroup>
</Project>
```

### Host: External Plugin Loader

```csharp
public static class ExternalPluginLoader
{
    public static List<IAppPlugin> LoadExternalPlugins(
        IServiceCollection services, string directory)
    {
        var plugins = new List<IAppPlugin>();

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            return plugins;
        }

        foreach (var dll in Directory.GetFiles(directory, "*.Plugin.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => !t.IsAbstract
                             && typeof(IAppPlugin).IsAssignableFrom(t));

                foreach (var type in pluginTypes)
                {
                    var plugin = (IAppPlugin)Activator.CreateInstance(type)!;
                    plugin.RegisterServices(services);
                    plugins.Add(plugin);
                    Console.WriteLine($"Loaded plugin: {plugin.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load {dll}: {ex.Message}");
            }
        }

        return plugins;
    }
}
```

### Host: Program.cs Integration

```csharp
public static void Main(string[] args)
{
    var services = new ServiceCollection();

    // 1. Load built-in plugins (from same process)
    var builtInPlugins = PluginLoader.LoadPlugins(services);

    // 2. Load external plugins (from plugins/ directory)
    var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");
    var externalPlugins = ExternalPluginLoader.LoadExternalPlugins(services, pluginDir);

    // 3. Combine all plugins for navigation
    services.AddSingleton<IReadOnlyList<IAppPlugin>>(
        builtInPlugins.Concat(externalPlugins).ToList());

    // 4. Build provider and run
    var provider = services.BuildServiceProvider();
    AppHost.Initialize(provider);

    AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .StartWithClassicDesktopLifetime(args);
}
```

### Testing External Plugin Loading

```csharp
[Fact]
public void LoadExternalPlugins_LoadsPluginDll()
{
    // Create a temp directory with a test plugin DLL
    var tempDir = Path.Combine(Path.GetTempPath(), "PluginTest_" + Guid.NewGuid());
    Directory.CreateDirectory(tempDir);

    try
    {
        // Copy a pre-built test plugin DLL
        File.Copy("TestPlugins/TestPlugin.Plugin.dll",
            Path.Combine(tempDir, "TestPlugin.Plugin.dll"));

        var services = new ServiceCollection();
        var plugins = ExternalPluginLoader.LoadExternalPlugins(services, tempDir);

        Assert.Single(plugins);
        Assert.Equal("TestPlugin", plugins[0].Name);
    }
    finally
    {
        Directory.Delete(tempDir, recursive: true);
    }
}
```

---

## Example 4: Plugin with Navigation and Parameters

**What you'll learn:** How to pass parameters when navigating to a plugin view, enabling scenarios like opening a specific report or editing a specific item.

### Extend the Navigation Contract

```csharp
public interface IPluginNavigation
{
    string Route { get; }
    Type ViewModelType { get; }
    Task InitializeAsync(object? parameter);
}
```

### Plugin: Report Viewer with Navigation

```csharp
public class ReportViewerPlugin : IAppPlugin
{
    public string Name => "Report Viewer";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<ReportViewerViewModel>();
        services.AddTransient<ReportViewerView>();
    }
}

public partial class ReportViewerViewModel :
    ObservableObject, IPluginNavigation
{
    public string Route => "report-viewer";
    public Type ViewModelType => GetType();

    [ObservableProperty]
    private string _reportTitle = "";

    [ObservableProperty]
    private bool _isLoading;

    public async Task InitializeAsync(object? parameter)
    {
        var reportId = parameter?.ToString();
        if (string.IsNullOrEmpty(reportId))
            return;

        IsLoading = true;
        ReportTitle = $"Loading report {reportId}...";
        await Task.Delay(300);
        ReportTitle = $"Report {reportId}";
        IsLoading = false;
    }
}
```

### Shell: Parameterised Navigation

```csharp
public partial class ShellViewModel : ObservableObject
{
    private readonly Dictionary<string, IPluginNavigation> _navigations;

    [ObservableProperty]
    private object? _currentView;

    public ShellViewModel(IEnumerable<IPluginNavigation> navigations)
    {
        _navigations = navigations.ToDictionary(n => n.Route);
    }

    [RelayCommand]
    private async Task NavigateToAsync(string route)
    {
        if (!_navigations.TryGetValue(route, out var nav))
            return;

        var vm = AppHost.Services.GetRequiredService(nav.ViewModelType);
        if (vm is IPluginNavigation navVm)
            await navVm.InitializeAsync(null);

        CurrentView = vm;
    }

    [RelayCommand]
    private async Task OpenReportAsync(string reportId)
    {
        if (!_navigations.TryGetValue("report-viewer", out var nav))
            return;

        var vm = AppHost.Services.GetRequiredService(nav.ViewModelType);
        if (vm is IPluginNavigation navVm)
            await navVm.InitializeAsync(reportId);

        CurrentView = vm;
    }
}
```

### View Binding

```xml
<Button Content="Open Report #42"
        Command="{Binding OpenReportCommand}"
        CommandParameter="42" />
```

---

## Key Takeaways

- **Three-plugin app structure** demonstrates the core pattern: shared contract, independent plugin projects, host-based discovery and navigation.
- **Scrutor convention registration** eliminates per-type boilerplate. Plugins only declare their assembly; naming conventions drive registration.
- **External DLL loading** enables true plug-and-play. Plugins live in a `plugins/` directory and are discovered at startup.
- **Parameterised navigation** extends the pattern for real-world scenarios like opening specific reports or editing items.
- All examples are testable by calling `RegisterServices` and verifying the container resolves the expected types.

---

## See Also

- [002 — Modular App with Plugin-Style Views](002-plugin-architecture.md)
- [002V — Modular App with Plugin-Style Views: In-Depth Companion](002-plugin-architecture-verbose.md)
- [Pattern: Service Locator vs DI](001-service-locator-vs-di.md)
- [032 — MVVM with Dependency Injection](../02-tutorials/advanced/032-mvvm-di-wiring.md)
