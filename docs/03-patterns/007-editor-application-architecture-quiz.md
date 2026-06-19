---
title: Quiz
topic: 007-editor-application-architecture
type: quiz
---

# Quiz: Editor Application Architecture

```quiz
Q: Why does standard MVVM break down in complex editor applications?
A. MVVM is not supported in Avalonia
B. One ViewModel cannot own the state of 10+ panels, and selection changes affect nearly every panel simultaneously (correct) || In an editor, multiple panels observe the same selection, undo/redo spans domain objects, and the inspector is a dynamic UI factory — standard MVVM with one ViewModel per page cannot handle these cross-cutting concerns.
C. MVVM does not support dependency injection
D. MVVM requires WPF-specific APIs not available in Avalonia

Explanation: MVVM works well for single-page applications but breaks down in editors because shared state (selection, workspace) must be observed by many panels simultaneously, undo/redo requires the Command Pattern across domain objects, and the inspector must dynamically generate UI per type.
```

```quiz
Q: What is the primary role of the SelectionService in an editor architecture?
A. To save and load documents from disk
B. To provide edit/undo/redo functionality
C. To own the selection state and notify all panels when it changes (correct) || SelectionService is the central hub — every panel (inspector, canvas, hierarchy, status bar, property grid) observes it and reacts when the selection changes.
D. To manage plugin discovery and loading

Explanation: SelectionService is described as "the single most important service in an editor" and "the hub everything spins around." It holds the list of selected objects and fires events/messages that every panel observes.
```

```quiz
Q: Which pattern is used to implement undo/redo in the editor architecture, and why?
A. Memento Pattern — because it saves object state snapshots
B. Command Pattern — because each user action is a self-reversing object that knows how to undo itself (correct) || The Command Pattern (IUndoableCommand) encapsulates each action's execute and undo logic in a single object, making undo/redo transactional and composable via CompositeCommand.
C. Observer Pattern — because it notifies all panels of changes
D. Strategy Pattern — because it allows swapping undo algorithms at runtime

Explanation: Pure MVVM cannot implement undo/redo because property setters are not transactional. The Command Pattern makes each action a self-contained object with Execute(), Undo(), and Redo() methods. CompositeCommand groups multiple commands into one undo step.
```

```quiz
Q: How does the Dynamic Inspector / Property Panel determine what UI to display?
A. It uses a hard-coded switch statement on the selected object's type name
B. It uses the Strategy Pattern via IEditorProvider — each type or category gets its own editor provider that returns the appropriate Control (correct) || The InspectorViewModel iterates through registered IEditorProvider instances, asking each CanHandle(type). The first matching provider creates the editor Control. Plugins can contribute their own providers.
C. It binds directly to every possible property in XAML and shows/hides sections
D. It compiles a new UserControl at runtime for each selected type

Explanation: The inspector uses the Strategy Pattern (and implicitly Chain-of-Responsibility). Each IEditorProvider checks if it can handle a given type and returns the appropriate Control. This makes the inspector extensible by plugins and avoids a central switch statement.
```

```quiz
Q: What mechanism decouples panels from each other and from services in the editor architecture?
A. Direct method calls between ViewModels
B. A shared static class with global state
C. A message bus (IMessageBus) that publishes typed, immutable messages (correct) || The message bus publishes messages like SelectionChangedMessage, DocumentOpenedMessage, etc. Panels subscribe to relevant messages without holding direct references to each other.
D. XAML data binding with ElementName

Explanation: The message bus is explicitly described as the solution for decoupled cross-panel communication. Each panel subscribes to the message types it cares about and reacts without knowing which other component published the message.
```

```quiz
Q: In the Document-View architecture, why is the document NOT a ViewModel?
A. Documents manage their own persistence, have multiple views, and outlive the panels that display them (correct) || A document is a domain object that handles load/save/validation/mutation. It has multiple views (outline tree, canvas, property panel) and lives independently of any single panel.
B. ViewModels cannot implement IDocument
C. Documents are always singletons and cannot be bound to views
D. The Document-View pattern is deprecated in Avalonia 12

Explanation: Documents are domain objects, not ViewModels. They manage file persistence, serialization, and domain logic. Multiple views can observe the same document, and the document outlives any particular panel or view.
```
