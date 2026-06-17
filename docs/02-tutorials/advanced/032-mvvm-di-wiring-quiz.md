---
tier: advanced
topic: di
estimated: 3-5 min
researched: 2026-06-15
avalonia-version: 12.0.4
example-of: 032-mvvm-di-wiring.md
---

# Quiz — MVVM Dependency Injection with Microsoft.Extensions.DependencyInjection

```quiz
Q: Where should you configure service registrations (AddSingleton, AddTransient) in an Avalonia DI setup?
A. In the App.axaml.cs constructor || The constructor runs before the service provider is built — registration must happen during host construction.
B. In the ConfigureServices method passed to Host.CreateDefaultBuilder().ConfigureServices() (correct) || The ConfigureServices callback receives IServiceCollection where all services are registered before the host is built.
C. In each ViewModel's constructor using a service locator pattern || Service locator is an anti-pattern — use constructor injection with proper DI registration.
D. In the MainWindow constructor by calling AddSingleton on a static container || Registration must occur before Build() — the MainWindow constructor is too late.
Explanation: ConfigureServices(HostBuilderContext, IServiceCollection) is the standard location where services are registered before the host builds the provider.
```

```quiz
Q: How do you resolve the MainViewModel from the DI container in App.axaml.cs?
A. Program.AppHost.Services.GetService<MainViewModel>() with a null check || GetService returns null if not registered — GetRequiredService throws a clear exception when missing, which is safer.
B. Program.AppHost.Services.GetRequiredService<MainViewModel>() (correct) || GetRequiredService<T> resolves the registered MainViewModel and throws if the registration is missing, making configuration errors obvious.
C. new MainViewModel(Program.AppHost.Services) || ViewModels should receive their dependencies via constructor injection, not the entire service provider.
D. Program.AppHost.GetService<MainViewModel>() || AppHost returns an IHost — services are accessed via the Services property, not directly on the host.
Explanation: Program.AppHost.Services.GetRequiredService<MainViewModel>() resolves the singleton MainViewModel after the host is built in Program.Main.
```

```quiz
Q: Which lifetime should you use when registering a ViewModel that should create a new instance for every consumer?
A. AddSingleton<SettingsViewModel>() || Singleton creates one instance shared across all consumers — every resolution returns the same object.
B. AddTransient<SettingsViewModel>() (correct) || Transient creates a new instance every time the service is resolved from the container.
C. AddScoped<SettingsViewModel>() || Scoped is tied to a scope (e.g., per-request in ASP.NET) and is not commonly used in desktop Avalonia DI.
D. AddInstance<SettingsViewModel>() || There is no AddInstance method — use AddSingleton with an existing instance or AddTransient for new instances each time.
Explanation: AddTransient creates a fresh instance per resolution, appropriate for ViewModels that should not share state.
```

```quiz
Q: What is the correct way to register the IMessenger for cross-ViewModel communication in the DI container?
A. services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default) (correct) || WeakReferenceMessenger.Default is the singleton messenger instance — registering it as IMessenger allows any ViewModel to inject IMessenger.
B. services.AddTransient<IMessenger>(_ => new WeakReferenceMessenger()) || Creating new messenger instances per resolution means ViewModels would not share a message bus and cannot communicate.
C. services.AddSingleton<WeakReferenceMessenger>() || This registers the concrete type, not the interface — ViewModels injecting IMessenger will not receive it without the interface registration.
D. IMessenger is automatically registered by Avalonia and does not need explicit DI registration || Avalonia does not auto-register IMessenger — it must be explicitly added to the service collection.
Explanation: Registering WeakReferenceMessenger.Default as a singleton IMessenger ensures all ViewModels share the same message bus.
```

```quiz
Q: What is a critical timing requirement when using Program.AppHost.Services?
A. The service provider must only be called from background threads || The service provider is thread-safe but the requirement is about availability timing, not threading.
B. Do not call GetRequiredService before Program.AppHost is built (after Host.CreateDefaultBuilder().Build()) (correct) || The host and its service provider are only available after Build() completes — calling before throws an invalid operation exception.
C. The service provider must be disposed after every resolution || The host's service provider lives for the application lifetime — it is disposed when the host shuts down, not per resolution.
D. Services can only be resolved inside the ConfigureServices method || ConfigureServices is for registration, not resolution — resolution happens at runtime after Build().
Explanation: Program.AppHost is null until Host.CreateDefaultBuilder(args).ConfigureServices(...).Build() assigns it — resolve services only after that point.
```
