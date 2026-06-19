---
title: Quiz
topic: 008-toggle-state-pattern
type: quiz
---

# Quiz: Toggle & State Pattern

```quiz
Q: What is the primary problem that the IToggleService solves?
A. It provides a centralized API for managing, observing, and persisting binary/multi-state toggles across the application (correct) || Manual bool properties with [ObservableProperty] don't scale to grouped/exclusive toggles, cross-ViewModel observation, or persistence.
B. It replaces all bool properties in ViewModels with a single enum
C. It converts all synchronous toggle operations to async
D. It automatically generates toggle UI from data annotations

Explanation: The core problem is that scattering bool properties across ViewModels leads to sync bugs, cannot express mutual exclusion, lacks reactive observation, and makes persistence tedious. IToggleService centralizes all toggle state with a uniform API.
```

```quiz
Q: What happens when you call Enable() on a toggle that belongs to a mutual exclusion group?
A. All toggles in the group are disabled, then the requested toggle is enabled (correct) || Mutual exclusion guarantees exactly one toggle in the group is active at a time. Enabling one automatically disables all others in the same group.
B. An exception is thrown because you must use Toggle() instead
C. Only the requested toggle is enabled; other toggles remain unchanged
D. The group is dissolved and all toggles become independent

Explanation: When MakeMutuallyExclusive is configured, calling Enable() on one key disables all other keys in the same group before enabling the requested one. This ensures exactly one active toggle per group.
```

```quiz
Q: What design pattern does PersistentToggleService use to add persistence to IToggleService?
A. Factory Pattern — it creates new instances of IToggleService with persistence built in
B. Decorator Pattern — it wraps an existing IToggleService and adds save/load behavior (correct) || PersistentToggleService implements IToggleService, delegates all operations to an inner IToggleService, and adds persistence as a cross-cutting concern.
C. Adapter Pattern — it adapts IToggleService to a file-based storage interface
D. Singleton Pattern — it ensures only one persistent toggle service exists

Explanation: The PersistentToggleService wraps any IToggleService and adds save/load functionality without modifying the underlying implementation. This is the Decorator Pattern — persistence is composed onto the service.
```

```quiz
Q: How does the Observe(string key) method enable reactive UI patterns?
A. It returns a bool that is automatically bound to XAML
B. It returns IObservable<bool> which can be used with System.Reactive operators like CombineLatest, Merge, and Where (correct) || Observe returns an IObservable<bool> that emits the current value and all future changes, enabling reactive LINQ chains.
C. It directly updates the UI thread via an internal event loop
D. It writes to a file that the UI polls every frame

Explanation: Observe returns IObservable<bool>, allowing subscribers to use reactive operators (CombineLatest, Merge, Select, Where, Subscribe). This enables powerful patterns like combining multiple toggle states into a single reactive expression.
```

```quiz
Q: In the ViewModel integration pattern, how are local [ObservableProperty] properties kept in sync with the IToggleService?
A. By using the partial void On<PropertyName>Changed method to push changes to the service, and subscribing to Observe() to pull changes from the service (correct) || The partial void OnChanged handler pushes local changes to the service, while an Observe subscription pulls external changes (e.g., from keyboard shortcuts) back into the local property.
B. By using one-way binding from the service to the property only
C. By using a custom ValueConverter that reads the service directly
D. By calling SyncAll() in a loop

Explanation: Bidirectional sync is achieved by: (a) partial void OnChanged fires when the local property changes and pushes to the service, and (b) Observe().Subscribe() updates the local property when the service changes from another source.
```

```quiz
Q: What is the purpose of the Snapshot() method on IToggleService?
A. It creates a visual screenshot of the toggle button
B. It returns a dictionary of all toggle keys and their current values for serialization (correct) || Snapshot() returns IReadOnlyDictionary<string, bool> with every registered toggle's current state. This is used by PersistentToggleService to serialize the entire toggle state to JSON.
C. It captures the current call stack for debugging
D. It resets all toggles to their default values

Explanation: Snapshot() collects all toggle keys and their boolean values into a dictionary, which the persistence layer serializes to JSON for saving and later restoration.
```
