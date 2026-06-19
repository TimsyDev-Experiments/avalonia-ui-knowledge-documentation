---
title: Quiz
topic: 002-plugin-architecture
type: quiz
---

# Quiz: Modular App with Plugin-Style Views

```quiz
Q: What is the primary purpose of separating `RegisterServices` and `RegisterViews` in the IAppPlugin interface?
A. To satisfy Scrutor's scanning requirements || Scrutor does not require separate registration methods
B. To keep services in the DI container and views in a view-locator or template registry, maintaining separate resolution mechanisms (correct) || Services and views have different resolution strategies — services go through the DI container while views are resolved through templates or view locators
C. To make the plugin interface harder to implement || The separation is for clarity and extensibility, not complexity
D. To ensure plugins cannot access views without going through the container || The separation is about organising registrations, not access control

Explanation: Keeping `RegisterServices` and `RegisterViews` separate acknowledges that services and views use different resolution mechanisms. Services are resolved through the DI container with lifecycle management, while views are typically resolved through Avalonia's DataTemplate system or a ViewLocator.
```

```quiz
Q: Which assembly scanning approach is most appropriate for a plugin system that needs to support hot-reloading without locking DLL files?
A. `Assembly.LoadFrom` with a DLL path || LoadFrom locks the DLL file and does not support unloading
B. `Assembly.Load(byte[])` || Loading from bytes avoids locking but can cause type identity issues
C. `AssemblyLoadContext` (ALC) with a custom load context (correct) || ALC provides isolated load contexts that support unloading individual plugins without locking the source files
D. `AppDomain.CreateDomain` || .NET Core and later do not support creating additional AppDomains for isolation

Explanation: `AssemblyLoadContext` is the recommended approach for .NET Core and later. It provides per-plugin isolation, supports unloading (when not using unmanaged dependencies), and does not lock the source DLL files on disk.
```

```quiz
Q: What is the benefit of using Scrutor for convention-based registration in a plugin system?
A. It eliminates the need for the IAppPlugin interface entirely || Scrutor handles registration conventions but the plugin contract is still needed
B. Plugins no longer need to call `AddTransient<SomeViewModel>()` for every type — the convention handles it automatically (correct) || Scrutor scans the assembly and registers types by naming convention (e.g., all types ending with "ViewModel" are registered as transient)
C. Scrutor automatically generates ViewModel code || Scrutor only handles DI registration, not code generation
D. Scrutor replaces the DI container || Scrutor extends `ServiceCollection`, not replaces it

Explanation: Convention-based registration with Scrutor means plugins are thinner — they only declare their assembly and the naming convention handles registration automatically. Adding a new ViewModel or service to a plugin requires zero registration changes.
```

```quiz
Q: In the plugin architecture, how should the shell ViewModel resolve the active plugin ViewModel for navigation?
A. Using a static service locator with a dictionary of routes || A static locator introduces hidden dependencies and global state
B. By calling `Activator.CreateInstance` for each route || Activator.CreateInstance bypasses the DI container and prevents dependency injection into the ViewModel
C. By resolving the ViewModel type from the DI container using `IServiceProvider.GetRequiredService` (correct) || Resolving from the container ensures the ViewModel receives its dependencies through constructor injection and the container manages its lifetime
D. By requiring each plugin to register its ViewModel in a separate navigation registry || The DI container itself serves as the service registry — no additional registry is needed

Explanation: The shell ViewModel uses the DI container to resolve the active plugin's ViewModel. `IServiceProvider.GetRequiredService(nav.ViewModelType)` creates a fresh instance (for transient registrations) with all dependencies automatically injected. This keeps the shell decoupled from plugin internals.
```

```quiz
Q: When testing a plugin in isolation, what is the minimum setup required to verify its ViewModel registration works?
A. Creating a mock IAppPlugin and calling its methods || The test needs to test the real plugin, not a mock
B. Instantiating the plugin, calling `RegisterServices` on a `ServiceCollection`, building the provider, and resolving the ViewModel (correct) || This verifies the plugin registers its ViewModel and all its dependencies resolve correctly
C. Running the full application and clicking through the UI || Full integration testing is unnecessary for verifying registration
D. Only calling `Activator.CreateInstance` on the ViewModel type || This bypasses DI and does not verify the ViewModel's dependencies are registered

Explanation: Testing a plugin in isolation involves instantiating the plugin class, calling `RegisterServices` on a new `ServiceCollection`, building the `ServiceProvider`, and resolving the ViewModel. This verifies that the plugin's registrations are complete and that the ViewModel and all its transitive dependencies can be resolved.
```
