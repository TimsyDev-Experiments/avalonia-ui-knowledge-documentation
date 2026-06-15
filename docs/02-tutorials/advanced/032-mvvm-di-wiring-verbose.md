---
tier: advanced
topic: di
estimated: 20-25 min
researched: 2026-06-14
avalonia-version: 12.0.4
companion-to: 032-mvvm-di-wiring.md
---

# 032V — MVVM Dependency Injection with Microsoft.Extensions.DependencyInjection: An In-Depth Companion

This companion explains the DI container setup, lifetime management, service resolution order, and how each component interacts. Read it alongside [032 — MVVM Dependency Injection](032-mvvm-di-wiring.md).

---

## 1. Why Dependency Injection in a Desktop App

### The problem DI solves

Without DI, ViewModels create their own services:

```csharp
public class MainViewModel
{
    private readonly IDialogService _dialog = new DialogService();
}
```

This hard-codes the dependency. Testing `MainViewModel` requires `DialogService` to work, which might show real dialogs, access the filesystem, or make network calls. With DI, the service is injected:

```csharp
public class MainViewModel(IDialogService dialog) { ... }
```

Now you can inject a `MockDialogService` in tests. The ViewModel does not know or care which implementation it receives.

### Why Microsoft.Extensions.DependencyInjection specifically

`Microsoft.Extensions.DependencyInjection` (M.E.DI) is the standard DI container for .NET. It is:
- **Official**: Built by Microsoft, maintained with the .NET platform.
- **Extensible**: Over 50 third-party containers (Autofac, StructureMap, DryIoc) can replace it via the `IServiceProviderFactory` interface.
- **Aware** of .NET Generic Host — `Host.CreateDefaultBuilder()` adds logging, configuration, and environment management automatically.
- **Fast**: M.E.DI is one of the fastest DI containers in synthetic benchmarks (sub-100ns resolve for singletons).

### Why not the `Ioc.Default` from CommunityToolkit.Mvvm?

`Ioc.Default` is a simple service locator — a single static `IServiceProvider` with no scoping, no lifecycle management, and no configuration system. It is fine for small apps, but M.E.DI adds:
- Named/keyed services (multiple implementations of the same interface).
- Scoped lifetimes (per-window, per-page).
- Integration with `IHostedService` for background tasks.
- Configuration binding from JSON, environment variables, etc.

---

## 2. Installing the Packages — What Each Adds

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
```

| Package | Purpose |
|---|---|
| `Microsoft.Extensions.DependencyInjection` | The DI container itself: `IServiceCollection`, `ServiceProvider`, `AddSingleton`, `AddTransient`, etc. |
| `Microsoft.Extensions.Hosting` | `Host.CreateDefaultBuilder()`, which configures logging, app configuration, environment, and DI in one call. Includes `DependencyInjection` transitively. |

`Host.CreateDefaultBuilder(args)` internally:
1. Sets the `ContentRoot` to the app's directory.
2. Loads `appsettings.json` and `appsettings.{Environment}.json`.
3. Adds environment variable and command-line configuration providers.
4. Adds console and debug logging.
5. Configures the DI container.

If you do not need hosting features (logging, configuration), you can use just `new ServiceCollection()` and build manually. The tutorial uses the Host builder for forward-compatibility.

---

## 3. Creating a HostBuilder — What Each Call Does

```csharp
AppHost = Host.CreateDefaultBuilder(args)
    .ConfigureServices(ConfigureServices)
    .Build();
```

### `ConfigureServices` delegate

```csharp
private static void ConfigureServices(HostBuilderContext context,
    IServiceCollection services)
{
    services.AddSingleton<MainViewModel>();
    services.AddSingleton<IDialogService, DialogService>();
    services.AddTransient<SettingsViewModel>();
}
```

`HostBuilderContext context` provides access to:
- `context.Configuration` — the merged `IConfiguration` from JSON, env vars, CLI args.
- `context.HostingEnvironment` — environment name (Development, Production).
- `context.Properties` — a dictionary for passing custom state.

You can use `context.Configuration` to register services conditionally:

```csharp
if (context.HostingEnvironment.IsDevelopment())
    services.AddSingleton<IDialogService, MockDialogService>();
else
    services.AddSingleton<IDialogService, DialogService>();
```

### Building the host

`.Build()` creates the `IHost` instance and compiles the service provider. This is the most expensive operation in the DI setup (it validates all registrations and generates resolve factories). It should happen once at startup, not per-window or per-request.

---

## 4. Resolving the Main ViewModel — Why It Happens in App.axaml.cs

```csharp
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
```

### Why in `OnFrameworkInitializationCompleted` and not `Main`

`OnFrameworkInitializationCompleted` runs after the Avalonia framework has initialized styles, themes, and the rendering subsystem. If you create windows or access services before this point, the Avalonia infrastructure may not be ready.

### Why resolve in code-behind and not inject into MainWindow constructor

The tutorial resolves `MainViewModel` in `App.axaml.cs` and assigns it as `MainWindow.DataContext`. An alternative is to inject `MainViewModel` into `MainWindow`'s constructor:

```csharp
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
```

Then resolve from the container:

```csharp
desktop.MainWindow = Program.AppHost.Services.GetRequiredService<MainWindow>();
```

The tutorial's approach (resolving in App.axaml.cs) is simpler and avoids coupling `MainWindow` to the DI container. The constructor-injection approach is cleaner for apps with many dependencies.

### The null-forgiving operator

`Program.AppHost.Services.GetRequiredService<MainViewModel>()` — `GetRequiredService` throws if the type is not registered. Use `GetService<T>()` (returns null) if the service is optional and you have a fallback.

---

## 5. Service Lifetimes — Singleton vs. Transient vs. Scoped

### Singleton

```csharp
services.AddSingleton<MainViewModel>();
services.AddSingleton<IDialogService, DialogService>();
```

- One instance for the entire application lifetime.
- Created on first resolve (or on app start if configured).
- Never disposed until the `ServiceProvider` is disposed (on app shutdown).
- Use for: Application-wide state, dialog services, settings services, loggers.

### Transient

```csharp
services.AddTransient<SettingsViewModel>();
```

- A new instance every time the service is requested.
- Created on each resolution.
- Disposed when the resolving scope is disposed (for `IDisposable` transients).
- Use for: Per-dialog ViewModels, temporary data services, stateless utilities.

### Scoped (not shown in the tutorial)

```csharp
services.AddScoped<ISessionService, SessionService>();
```

- One instance per scope (typically per-window or per-operation).
- Created on first resolve within the scope.
- Disposed when the scope ends.
- Use for: Per-window state, request-scoped data in multi-window apps.

A scope is created with `IServiceScopeFactory`:

```csharp
using (var scope = Program.AppHost.Services.CreateScope())
{
    var vm = scope.ServiceProvider.GetRequiredService<SettingsViewModel>();
}
```

### Choosing the right lifetime

| Pattern | Singleton | Transient | Scoped |
|---|---|---|---|
| ViewModel for main window | Yes | No | No |
| ViewModel for dialog | No | Yes | No |
| ViewModel for each tab in a multi-tab window | No | No | Yes |
| Service with state (DialogService) | Yes | No | No |
| Service without state | Yes | Yes | Yes |

**Warning**: If a singleton ViewModel holds a reference to a transient service, the transient service cannot be garbage collected for the app's lifetime. This is a captive dependency leak.

---

## 6. Service Interface and Implementation — Why Separate

```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string message);
}

public class DialogService : IDialogService
{
    public async Task<bool> ConfirmAsync(string message)
    {
        await Task.Delay(100);
        return true;
    }
}
```

### Why the interface

- **Testability**: `MockDialogService` can implement `IDialogService` and return canned results.
- **Decoupling**: The ViewModel depends on the *abstraction* (`IDialogService`), not the *concretion* (`DialogService`). Changing the implementation (e.g., from `Task.Delay` to a real `Window` dialog) requires no changes to ViewModels that use it.
- **Registration**: `services.AddSingleton<IDialogService, DialogService>()` maps the interface to the concrete class. Any code requesting `IDialogService` gets `DialogService`.

### Why not inject concrete classes

You can inject concrete classes without interfaces:

```csharp
services.AddSingleton<DialogService>();
```

This works but makes testing harder — you cannot mock a concrete class without a mocking framework like Moq or NSubstitute.

---

## 7. Constructor Injection with CommunityToolkit.Mvvm

```csharp
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

### How `[RelayCommand]` generates the command

The source generator creates `ShowConfirmAsyncCommand` — a `AsyncRelayCommand` property — from the `ShowConfirmAsync` method. The generated code wraps the method in an `IAsyncRelayCommand` that:
- Invokes `ShowConfirmAsync` when executed.
- Tracks `IsRunning` (can bind to a "Loading..." indicator).
- Handles `CanExecute` if the method has a `CanExecute` counterpart.

### Why partial class is required

CommunityToolkit.Mvvm's source generators emit code into a partial class declaration. Without `partial`, the generated code has no class to extend.

### `[ObservableProperty]` generated code

```csharp
private string _result = string.Empty;
```

Becomes the equivalent of:

```csharp
public string Result
{
    get => _result;
    set => SetProperty(ref _result, value);
}
```

The `SetProperty` method raises `PropertyChanged` automatically, which notifies bindings subscribed to `Result`.

---

## 8. Registering IMessenger for Cross-ViewModel Communication

```csharp
services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
```

### Why register IMessenger in DI

Without DI registration, each ViewModel that uses messaging would either:
- Access `WeakReferenceMessenger.Default` directly (static singleton, hard to test).
- Create its own `WeakReferenceMessenger` instance (messages would not be shared).

Registering as a singleton ensures all ViewModels share the same messenger instance. Messages sent by `MainViewModel` are received by `SettingsViewModel` and vice versa.

### WeakReferenceMessenger vs. StrongReferenceMessenger

| Messenger | Subscriber reference | When subscriber is GC'd |
|---|---|---|
| `WeakReferenceMessenger` | Weak reference (default) | Automatically, if no other references exist |
| `StrongReferenceMessenger` | Strong reference | Only when explicitly unregistered |

`WeakReferenceMessenger` is the default and recommended for most apps. It prevents memory leaks when ViewModels are created and destroyed (e.g., dialog ViewModels that are never explicitly unregistered).

### The IRecipient pattern

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

`IRecipient<TMessage>` defines a single `Receive(TMessage)` method. `messenger.RegisterAll(this)` uses reflection to find all `IRecipient<T>` implementations on the object and registers them all at once. This is cleaner than calling `messenger.Register<T>(this, handler)` for each message type.

### The message class

```csharp
public class ThemeChangedMessage
{
    public string ThemeName { get; }

    public ThemeChangedMessage(string themeName)
    {
        ThemeName = themeName;
    }
}
```

Messages are simple data objects. They can be record types, structs, or classes. The messenger delivers them by reference, not by copy.

---

## Key Takeaways — Why Each Pattern Exists

- **Host.CreateDefaultBuilder**: Provides a DI container with logging, configuration, and environment support in one call.
- **Singleton lifetimes**: Share state across the app without static globals.
- **Transient lifetimes**: Ensure each consumer gets a fresh instance, avoiding state leakage.
- **Interface abstraction**: Enables testing as a first-class concern.
- **Constructor injection**: Makes dependencies explicit, not hidden behind service locator calls.
- **IMessenger singleton**: Decouples ViewModels without direct references or event subscription lifecycle issues.
- **Resolution in OnFrameworkInitializationCompleted**: Guarantees the Avalonia framework is ready before ViewModels are created.

---

## See Also

- [032 — MVVM Dependency Injection (original)](032-mvvm-di-wiring.md)
- [001 — Project Setup](../basics/001-project-setup.md)
- [007 — Observable Object and Property](../basics/007-observable-object-property.md)
- [033 — Localization i18n](033-localization-i18n.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: Dependency Injection](https://docs.avaloniaui.net/docs/guides/implementation-guides/dependency-injection)
- [CommunityToolkit.Mvvm: Messenger docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/messenger)
- [032X — MVVM Dependency Injection (examples)](032-mvvm-di-wiring-examples.md)
