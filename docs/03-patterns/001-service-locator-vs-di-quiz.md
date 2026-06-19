---
title: Quiz
topic: 001-service-locator-vs-di
type: quiz
---

# Quiz: Service Locator vs DI

```quiz
Q: Why does constructor injection make unit testing easier than service locator?
A. The container automatically provides mock objects || Mocks must be registered with the container, which adds setup overhead
B. Dependencies are passed explicitly, so no global state needs to be configured before each test (correct) || Constructor injection passes all dependencies as parameters — there is no hidden global state to set up or tear down
C. Constructor injection prevents async methods from being tested || Constructor injection has no effect on async testing
D. Service locator is inherently non-testable || Service locator tests can work but require careful global state management

Explanation: Constructor injection makes dependencies visible in the constructor signature. Unit tests can pass mocks directly without configuring any global registry. Service locator tests require setting up and tearing down shared mutable state before and after each test.
```

```quiz
Q: Which of the following is NOT a valid drawback of the service locator pattern?
A. Hidden dependencies are not visible in the constructor signature || The constructor reveals nothing about which services are used internally
B. Service locator makes it impossible to register transient services || Service locators can support transient registration, but the distinction is not visible at the call site
C. Tests must manage global state that can leak between test cases || Shared mutable state in the locator must be reset between tests
D. The compiler cannot find call sites that depend on a removed service interface || Because `Locator.Get<T>()` is always valid C# regardless of whether `T` is registered

Explanation: A service locator can support transient lifestyles by registering factory functions. The problem is that the consumer has no way to distinguish between singleton and transient at the call site — but transient registration itself is possible.
```

```quiz
Q: In which scenario is a locator-style approach considered acceptable in Avalonia?
A. Resolving database connections at runtime || Database connections benefit from container-managed lifetimes and disposal tracking
B. Mapping ViewModel types to View types in a ViewLocator (correct) || ViewLocator is a narrow, deterministic type-to-type mapping that Avalonia's template system requires — it involves no service lifecycle management
C. Accessing configuration settings throughout the application || Configuration should be injected as `IOptions<T>` or a typed settings object
D. Creating feature-specific ViewModels on demand || Use an injected factory with `IServiceProvider` instead of a static locator

Explanation: A ViewLocator that maps ViewModel types to View types is the canonical exception. It is scoped to a single concern (type resolution), is deterministic, and works within Avalonia's framework-created-object constraint where constructor injection is not available.
```

```quiz
Q: What is the recommended strategy for migrating a legacy codebase that uses service locator everywhere?
A. Rewrite all classes in a single sprint || Big-bang rewrites are high-risk and rarely succeed in large codebases
B. Introduce an intermediary `IServiceResolver` interface, switch its backing implementation, then eliminate it class by class (correct) || This phased approach keeps the application working at every stage and spreads risk across multiple iterations
C. Add a second static locator alongside the first || Multiple static locators compound the problem without solving it
D. Remove the service locator and use `Activator.CreateInstance` instead || That replaces one anti-pattern with another and loses DI benefits

Explanation: The recommended phased migration introduces `IServiceResolver` as a seam, swaps the backing from the static locator to the DI container, and then gradually eliminates the resolver by injecting the actual service interfaces directly.
```

```quiz
Q: How does an `IServiceProvider`-backed factory differ from a static service locator?
A. The factory is injected through the constructor and has no global state (correct) || `IServiceProvider` is a framework abstraction injected into the factory — the factory is registered with the container and receives its dependencies normally
B. There is no difference — both are service locators || A factory injected via constructor is DI-compatible and testable; a static locator is not
C. The factory is slower than a static locator || Performance is not a meaningful differentiator; both resolve services in microseconds
D. Factories cannot be tested with mocks || Factories backed by `IServiceProvider` can be tested by building a `ServiceCollection` with mock registrations

Explanation: An `IServiceProvider`-backed factory is itself injected through the DI container. It has no static state. It can be tested by configuring a `ServiceCollection` with mock services. The key difference is that the dependency on the service provider is explicit rather than ambient.
```
