---
title: Quiz
topic: 004-state-management
type: quiz
---

# Quiz: State Management

```quiz
Q: In the Flux store pattern, why are `record` types recommended for state?
A. Records are automatically serialized to JSON || While true, this is not the primary reason
B. Records provide value equality and the `with` expression for ergonomic immutable updates (correct) || Records give you value equality for change-detection and the `with` expression to derive new state from existing state, which is the core mechanism of the Flux reducer pattern.
C. Records use less memory than classes || Records are reference types and have similar memory characteristics
D. Records implement INotifyPropertyChanged automatically || Records do not implement INotifyPropertyChanged

Explanation: Records provide value equality (EqualityComparer<T>.Default can detect no-op changes) and the `with` expression for creating modified copies. These two features make them the natural choice for immutable Flux-style state, where every dispatch produces a new state snapshot via a pure reducer function.
```

```quiz
Q: A ViewModel subscribes to a singleton event service. What is the most important precaution?
A. Always use async void event handlers || async void is for async event handlers but does not address subscription management
B. Unsubscribe from the event when the ViewModel is disposed to prevent leaks (correct) || Singleton event services hold a strong reference to subscribers. If the ViewModel is disposed without unsubscribing, the singleton keeps the ViewModel alive, causing a memory leak.
C. Always dispatch to a background thread || UI-bound properties must be updated on the UI thread, not background
D. Register the event service as AddScoped || The event service is a singleton by design; Scoped would create per-scope instances that cannot communicate across ViewModels

Explanation: Singleton event services hold a reference to each delegate registered on their events. If a ViewModel is disposed (or goes out of scope) without unregistering, the delegate reference prevents garbage collection of the ViewModel. Always implement IDisposable and subtract the handler in Dispose().
```

```quiz
Q: Which scenario best fits the IMessenger approach over a singleton event service?
A. Two ViewModels in the same window need to share a boolean flag || A singleton is simpler for 2-3 ViewModels
B. Multiple decoupled components in different parts of the visual tree need to react to the same event without a shared state object (correct) || IMessenger decouples sender from receiver completely — neither side needs a reference to shared state. Messages are fire-and-forget, ideal for loosely coupled components.
C. The application needs undo/redo functionality || Flux/store pattern is far better suited for undo/redo
D. The state changes must be logged and traceable for debugging || IMessenger has no built-in traceability; the Flux pattern is better

Explanation: IMessenger's key advantage is decoupling — the sender does not know who (if anyone) receives the message, and receivers do not share a common state object. This makes it ideal for cross-cutting notifications (workspace switch, user logout, theme change) where the sending and receiving components are in unrelated parts of the view tree.
```

```quiz
Q: In the Flux/store pattern, how do you handle an asynchronous operation like loading data from a database?
A. Make the Dispatch method async and await the database inside the reducer || Reducers must be pure synchronous functions with no side effects
B. Dispatch one action to set a loading state, perform the async work, then dispatch another action with the result (correct) || The pattern is: dispatch a "request" action (sets IsLoading=true), await the async work, then dispatch a "success" or "failure" action. The reducer handles each action independently, keeping the reducer pure.
C. Store the Task in the state record and await it from the ViewModel || Storing Task or Promise in state breaks immutability and serializability
D. Bypass the store entirely and set ViewModel properties directly || This defeats the purpose of having a single source of truth

Explanation: Reducers must be pure functions — no side effects, no async, no I/O. Async operations are modeled as multi-step action sequences: a synchronous action to set loading state, the async operation itself (in the ViewModel or a service), and a synchronous action to commit the result. This keeps the reducer testable and the state changes traceable.
```

```quiz
Q: What is a key limitation of the IMessenger approach compared to the Flux/store pattern?
A. IMessenger requires more boilerplate code || Flux typically requires more boilerplate (action records, reducer arms)
B. IMessenger provides no built-in mechanism to log or trace state changes (correct) || Messages are fire-and-forget; there is no central log of what messages were sent or how state changed. In the Flux pattern, every state change originates from a Dispatch call with a typed Action record that can be logged, serialized, or replayed.
C. IMessenger cannot be used with dependency injection || IMessenger is commonly registered as a singleton in DI
D. IMessenger only works with WeakReferenceMessenger || StrongReferenceMessenger is also available

Explanation: IMessenger is a pub/sub bus — it delivers messages but does not record them. There is no built-in "message history" or "state timeline." The Flux pattern provides complete traceability because every state change goes through Dispatch with a serializable Action object. This makes debugging, logging, and time-travel development much easier in the Flux pattern.
```
