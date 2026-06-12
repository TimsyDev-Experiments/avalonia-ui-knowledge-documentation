---
tier: advanced
topic: di
estimated: 30 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 032 -- MVVM Dependency Injection with Microsoft.Extensions.DependencyInjection

**What you'll learn:** How to wire CommunityToolkit.Mvvm ViewModels into your Avalonia application using the Microsoft.Extensions.DependencyInjection container.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md), [007 -- Observable Object and Property](../basics/007-observable-object-property.md)

---

## 1. Install packages

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
```

## 2. Create a HostBuilder in Program.cs

```csharp
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DemoApp;

class Program
{
    public static IHost AppHost { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        AppHost = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServices)
            .Build();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static void ConfigureServices(HostBuilderContext context,
        IServiceCollection services)
    {
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddTransient<SettingsViewModel>();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

## 3. Resolve the main ViewModel in App.axaml.cs

```csharp
using DemoApp.ViewModels;

namespace DemoApp;

public partial class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Program.AppHost.Services
                    .GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

## 4. Create a service interface and implementation

```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string message);
}

public class DialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string message)
    {
        // In production, resolve a Window reference
        await Task.Delay(100);
        return true;
    }
}
```

## 5. Inject services into ViewModels

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialog;

    [ObservableProperty]
    private string _result = string.Empty;

    public MainViewModel(IDialogService dialog)
    {
        _dialog = dialog;
    }

    [RelayCommand]
    private async Task ShowConfirmAsync()
    {
        var confirmed = await _dialog.ConfirmAsync("Are you sure?");
        Result = confirmed ? "Confirmed" : "Cancelled";
    }
}
```

## 6. Register IMessenger for cross-ViewModel communication

```csharp
using CommunityToolkit.Mvvm.Messaging;

private static void ConfigureServices(HostBuilderContext context,
    IServiceCollection services)
{
    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    services.AddSingleton<MainViewModel>();
    services.AddTransient<SettingsViewModel>();
}
```

Inject the messenger into any ViewModel:

```csharp
public partial class SettingsViewModel : ObservableObject,
    IRecipient<ThemeChangedMessage>
{
    public SettingsViewModel(IMessenger messenger)
    {
        messenger.RegisterAll(this);
    }

    public void Receive(ThemeChangedMessage message)
    {
        // Handle theme change
    }
}
```

## Key takeaways

- `Microsoft.Extensions.DependencyInjection` integrates directly with Avalonia via `Program.cs`
- Register services with `AddSingleton` (one instance) or `AddTransient` (new per request)
- Resolve ViewModels from `App.Services.GetRequiredService<T>()`
- Constructor injection works naturally with CommunityToolkit.Mvvm ViewModels
- Register `IMessenger` in the container for decoupled ViewModel communication

> **Warning:** Do not call `Program.AppHost.Services.GetRequiredService` before `Program.AppHost` is built. The host is available after `Host.CreateDefaultBuilder().Build()`.
