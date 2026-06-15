---
tier: reference
topic: architecture
estimated: 15 min
researched: 2026-06-14
avalonia-version: 12.0.4
---

# Pattern 007 — Editor Application Architecture

## Problem

Building a content-creation tool (level editor, audio workstation, visual novel tool, node graph editor, IDE) that needs multiple tool windows, a dynamic inspector, undo/redo, selection-driven layout, and plugin extensibility. Standard MVVM breaks down at this scale because:

- One ViewModel cannot own the state of 10+ panels
- Selection changes affect nearly every panel simultaneously
- The inspector renders different UI depending on the selected object's type
- Undo/redo must operate on domain objects, not ViewModel properties
- Plugins need to contribute panels, inspectors, and menu items

---

## Architecture Overview

```
┌──────────────────────────────────────────────────┐
│                  Shell (Chrome)                   │
│  ┌──────┐ ┌────────┐ ┌────────────┐ ┌─────────┐ │
│  │ Menu │ │Toolbar │ │Status Bar  │ │Docking  │ │
│  │      │ │        │ │            │ │Host     │ │
│  └──────┘ └────────┘ └────────────┘ └─────────┘ │
├──────────────────────────────────────────────────┤
│  ┌──────────┐ ┌─────────────┐ ┌───────────────┐ │
│  │ Panel A  │ │  Main View  │ │  Panel B      │ │
│  │ (Tree)   │ │  (Canvas/   │ │  (Properties) │ │
│  │          │ │   Preview)  │ │               │ │
│  └──────────┘ └─────────────┘ └───────────────┘ │
│  ┌──────────┐ ┌─────────────┐ ┌───────────────┐ │
│  │ Panel C  │ │  Console    │ │  Panel D       │ │
│  │(Browser) │ │  (Log)      │ │  (Timeline)   │ │
│  └──────────┘ └─────────────┘ └───────────────┘ │
├──────────────────────────────────────────────────┤
│                Service Layer                      │
│  Selection │ Undo/Redo │ Document  │ Command     │
│  Service   │ History   │ Manager   │ Router      │
├──────────────────────────────────────────────────┤
│               Domain / Document Model             │
│  Project        │  Scene Graph  │  Assets        │
│  (files + meta) │  (nodes)      │  (importers)   │
└──────────────────────────────────────────────────┘
```

---

## 1. Service Layer — Singleton Services Own All Shared State

Every service is registered as singleton in DI. Services own mutable state and expose events or an `IMessageBus` for cross-panel notification.

```csharp
// Program.cs
builder.Services.AddSingleton<SelectionService>();
builder.Services.AddSingleton<UndoService>();
builder.Services.AddSingleton<DocumentManager>();
builder.Services.AddSingleton<WorkspaceService>();
builder.Services.AddSingleton<PluginService>();
builder.Services.AddSingleton<ProjectService>();
```

### SelectionService — the hub everything spins around

```csharp
public sealed class SelectionService
{
    public IReadOnlyList<object> SelectedObjects => _selected;
    private List<object> _selected = new();

    // Events for strong-typed observers (ViewModels)
    public event Action<SelectionChangedArgs>? SelectionChanged;

    // Or use IMessageBus for decoupled observers
    private readonly IMessageBus _bus;

    public SelectionService(IMessageBus bus) => _bus = bus;

    public void Select(object target)
    {
        _selected = new List<object> { target };
        SelectionChanged?.Invoke(new SelectionChangedArgs(_selected));
        _bus.Publish(new SelectionChangedMessage(target));
    }

    public void SelectRange(IEnumerable<object> targets)
    {
        _selected = targets.ToList();
        SelectionChanged?.Invoke(new SelectionChangedArgs(_selected));
        _bus.Publish(new SelectionChangedMessage(_selected));
    }

    public void Clear()
    {
        _selected.Clear();
        SelectionChanged?.Invoke(new SelectionChangedArgs(_selected));
    }
}

public sealed record SelectionChangedArgs(IReadOnlyList<object> Selection);
```

Every panel observes `SelectionService`:

| Panel | Reacts to selection |
|---|---|
| Inspector / Properties | Rebuilds editor UI for selected type |
| Main View / Canvas | Updates gizmos, transform handles |
| Hierarchy / Tree | Highlights selected rows |
| Console | Filters messages by selected context |
| Status Bar | Shows name, type, or count |
| Property Grid | Displays fields of selected object |

---

## 2. Document-View Architecture

The document is the domain model. Views are transient windows onto it.

```csharp
public interface IDocument
{
    string Name { get; }
    string FilePath { get; }
    bool IsDirty { get; }
    Task<bool> SaveAsync(string? path = null);
}

public sealed class DocumentManager
{
    private readonly IMessageBus _bus;
    public IReadOnlyList<IDocument> OpenDocuments => _documents;
    public IDocument? ActiveDocument { get; private set; }
    private readonly List<IDocument> _documents = new();

    public DocumentManager(IMessageBus bus) => _bus = bus;

    public async Task<IDocument> OpenAsync(string path)
    {
        var doc = CreateDocumentForPath(path);
        _documents.Add(doc);
        ActiveDocument = doc;
        _bus.Publish(new DocumentOpenedMessage(doc));
        return doc;
    }

    public async Task<bool> SaveAsync(IDocument doc)
    {
        var success = await doc.SaveAsync();
        if (success) _bus.Publish(new DocumentSavedMessage(doc));
        return success;
    }

    public async Task<bool> SaveAllAsync()
    {
        foreach (var doc in _documents)
            await doc.SaveAsync();
        return true;
    }

    private IDocument CreateDocumentForPath(string path) { /* factory */ }
}
```

Documents are not ViewModels — they are domain objects that know how to load, save, validate, and mutate themselves.

---

## 3. Undo/Redo via the Command Pattern

Pure MVVM cannot implement undo/redo on object graphs because property setters are not transactional. Use the **Command Pattern**: each user action is an `IUndoableCommand` that knows how to reverse itself.

```csharp
public interface IUndoableCommand
{
    string Name { get; }        // "Move", "Delete", "Set Property"
    void Execute();
    void Undo();
    void Redo();
}

public sealed class DeleteNodeCommand : IUndoableCommand
{
    public string Name => "Delete Node";

    private readonly Node _node;
    private readonly NodeGraph _graph;
    private readonly IReadOnlyList<Connection> _connections;

    public DeleteNodeCommand(Node node, NodeGraph graph)
    {
        _node = node;
        _graph = graph;
        _connections = _graph.Connections
            .Where(c => c.Source == node || c.Target == node)
            .ToList();
    }

    public void Execute()
    {
        _graph.RemoveNode(_node);
    }

    public void Undo()
    {
        _graph.AddNode(_node);
        foreach (var conn in _connections)
            _graph.AddConnection(conn);
    }

    public void Redo() => Execute();
}
```

### UndoService

```csharp
public sealed class UndoService
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private const int MaxHistory = 200;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string UndoName => _undoStack.TryPeek(out var c) ? c.Name : "";
    public string RedoName => _redoStack.TryPeek(out var c) ? c.Name : "";

    public event Action? HistoryChanged;

    public void Record(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
        TrimHistory();
        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (_undoStack.TryPop(out var cmd))
        {
            cmd.Undo();
            _redoStack.Push(cmd);
            HistoryChanged?.Invoke();
        }
    }

    public void Redo()
    {
        if (_redoStack.TryPop(out var cmd))
        {
            cmd.Redo();
            _undoStack.Push(cmd);
            HistoryChanged?.Invoke();
        }
    }

    private void TrimHistory()
    {
        if (_undoStack.Count > MaxHistory)
            _undoStack = new Stack<IUndoableCommand>(
                _undoStack.Reverse().TakeLast(MaxHistory).Reverse());
    }
}
```

### Batch grouping

Group fine-grained commands into a single undo step:

```csharp
public sealed class CompositeCommand : IUndoableCommand
{
    private readonly List<IUndoableCommand> _commands;
    public string Name { get; }

    public CompositeCommand(string name, IEnumerable<IUndoableCommand> commands)
    {
        Name = name;
        _commands = commands.ToList();
    }

    public void Execute() => _commands.ForEach(c => c.Execute());
    public void Undo()    => _commands.AsEnumerable().Reverse().ToList()
                                   .ForEach(c => c.Undo());
    public void Redo()    => Execute();
}
```

---

## 4. Dynamic Inspector / Property Panel

The inspector is not a fixed ViewModel — it generates UI based on the selected object's type.

```csharp
public interface IEditorProvider
{
    bool CanHandle(Type type);
    Control CreateEditor(object target);
}

// Built-in providers
public sealed class StringEditorProvider : IEditorProvider
{
    public bool CanHandle(Type type) => type == typeof(string);

    public Control CreateEditor(object target)
    {
        var textBox = new TextBox { Text = (string)target };
        return textBox;
    }
}

public sealed class NumericEditorProvider : IEditorProvider
{
    public bool CanHandle(Type type) =>
        type == typeof(int) || type == typeof(float) || type == typeof(double);

    public Control CreateEditor(object target)
    {
        var spinner = new NumericUpDown { Value = Convert.ToDecimal(target) };
        return spinner;
    }
}

// Inspector ViewModel
public sealed class InspectorViewModel
{
    private readonly SelectionService _selection;
    private readonly IEnumerable<IEditorProvider> _providers;

    [ObservableProperty]
    private Control? _currentEditor;

    [ObservableProperty]
    private string _header = "No selection";

    public InspectorViewModel(
        SelectionService selection,
        IEnumerable<IEditorProvider> providers)
    {
        _selection = selection;
        _providers = providers;
        _selection.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(SelectionChangedArgs args)
    {
        if (args.Selection.Count == 0)
        {
            CurrentEditor = null;
            Header = "No selection";
            return;
        }

        var target = args.Selection[0];
        Header = $"{target.GetType().Name}";

        foreach (var provider in _providers)
        {
            if (provider.CanHandle(target.GetType()))
            {
                CurrentEditor = provider.CreateEditor(target);
                return;
            }
        }

        CurrentEditor = new TextBlock
        {
            Text = $"No editor for {target.GetType().Name}",
            Foreground = Brushes.Gray,
        };
    }
}
```

This is the **Strategy Pattern** — each type or category of type gets its own editor control. The chain-of-responsibility allows plugin providers to insert editors for their types.

---

## 5. Message Bus for Cross-Panel Communication

```csharp
// Messages — simple records
public sealed record SelectionChangedMessage(IReadOnlyList<object> Selection);
public sealed record DocumentOpenedMessage(IDocument Document);
public sealed record DocumentSavedMessage(IDocument Document);
public sealed record UndoRedoPerformedMessage(string ActionName);
public sealed record ProjectLoadedMessage(string Path);
public sealed record ToolChangedMessage(string ToolName);
public sealed record WorkspaceModifiedMessage(string PropertyName);

// Subscribe in any panel ViewModel
public sealed class StatusBarViewModel
{
    public StatusBarViewModel(IMessageBus bus)
    {
        bus.Subscribe<SelectionChangedMessage>(this, OnSelectionChanged);
        bus.Subscribe<DocumentSavedMessage>(this, OnDocumentSaved);
    }

    private void OnSelectionChanged(SelectionChangedMessage msg)
    {
        StatusText = msg.Selection.Count switch
        {
            0 => "No selection",
            1 => msg.Selection[0].GetType().Name,
            _ => $"{msg.Selection.Count} objects selected",
        };
    }

    private void OnDocumentSaved(DocumentSavedMessage msg)
    {
        StatusText = $"Saved: {msg.Document.Name}";
    }

    [ObservableProperty]
    private string _statusText = "Ready";
}
```

---

## 6. Panel / Tool Window Architecture

Each tool window is a ViewModel + View pair. They do not own shared state — they observe services.

```csharp
public sealed class ToolWindowAttribute : Attribute
{
    public string Title { get; }
    public string DefaultDock { get; }   // "Left", "Right", "Bottom", "Center"
    public ToolWindowAttribute(string title, string defaultDock = "Right")
    {
        Title = title;
        DefaultDock = defaultDock;
    }
}

[ToolWindow("Hierarchy", "Left")]
public sealed partial class HierarchyViewModel : ObservableObject
{
    private readonly SelectionService _selection;

    [ObservableProperty]
    private IReadOnlyList<INode> _nodes = Array.Empty<INode>();

    public HierarchyViewModel(SelectionService selection)
    {
        _selection = selection;
    }

    [RelayCommand]
    private void SelectNode(INode node)
    {
        _selection.Select(node);
    }

    [RelayCommand]
    private void DeleteNode(INode node)
    {
        // 'undoService' would be injected similarly
    }
}
```

View resolution via `DataTemplate` or view locator:

```xml
<DataTemplate DataType="x:Type="viewModels:HierarchyViewModel"">
  <views:HierarchyView />
</DataTemplate>
```

---

## 7. Plugin / Extensibility System

Use MEF or keyed DI to let plugins contribute panels, editors, and menu items.

```csharp
// Contract for menu contributions
public interface IMenuContributor
{
    void BuildMenu(IMenuBuilder builder);
}

// Contract for tool window contributions
public interface IToolWindowContributor
{
    string Title { get; }
    Control CreateView();
}

// Plugin discovery at startup
[Export(typeof(IMenuContributor))]
public sealed class MyPluginMenu : IMenuContributor
{
    public void BuildMenu(IMenuBuilder builder)
    {
        builder.Add("Plugins/My Tool", () =>
        {
            var window = new Window
            {
                Title = "My Plugin Tool",
                Content = new MyPluginView(),
            };
            window.Show();
        });
    }
}

// Plugin service
public sealed class PluginService
{
    private readonly IEnumerable<IMenuContributor> _menuContributors;
    private readonly IEnumerable<IToolWindowContributor> _toolWindows;

    public PluginService(
        IEnumerable<IMenuContributor> menuContributors,
        IEnumerable<IToolWindowContributor> toolWindows)
    {
        _menuContributors = menuContributors;
        _toolWindows = toolWindows;
    }

    public void Initialize(IMenuBuilder menuBuilder)
    {
        foreach (var contributor in _menuContributors)
            contributor.BuildMenu(menuBuilder);
    }
}
```

---

## 8. Concrete Flow: User Deletes a Node

1. **Hierarchy Panel** — `DeleteNodeCommand` executed via `UndoService.Record()`
2. **UndoService** — calls `DeleteNodeCommand.Execute()`, pushes to undo stack, fires `HistoryChanged`
3. **Document** — removes node, marks itself dirty
4. **DocumentManager** — publishes `DocumentModifiedMessage`
5. **Title Bar** — asterisk appears (`MyProject*`)
6. **Main View / Canvas** — re-renders without the deleted node
7. **SelectionService** — selection clears or moves to sibling
8. **Inspector** — clears or shows sibling's properties
9. **Status Bar** — shows "Deleted Node" / "No selection"
10. **Edit > Undo** label updates to "Undo Delete Node"

**Ctrl+Z:**

1. **Edit > Undo** command calls `UndoService.Undo()`
2. UndoService pops command, calls `Undo()`
3. Document re-inserts node and connections
4. Same cascade of events in reverse
5. Console may log "Undo: Delete Node"

---

## Summary of Architecture Decisions

| Concern | Pattern | Why |
|---|---|---|
| Shared state (selection, workspace) | Singleton service + events | Every panel reads the same instance |
| Undo/redo | Command Pattern | Each action is a self-reversing object |
| Inspector UI | Strategy / Chain-of-Responsibility | Dynamic UI per type, extensible by plugins |
| Cross-panel communication | Message Bus | Decoupled, no direct references |
| Documents | Document-View | Domain objects, not ViewModels |
| Tool windows | ViewModel + View per panel | Familiar MVVM for chrome, not state |
| Extension | MEF / keyed DI | Third parties contribute panels, editors, menus |
| Docking layout | Serialized layout model | Persist panel positions, tabs, and splits |

## Key takeaways

- MVVM works for chrome (menus, panels, status bar) but fails for shared state — use services
- Selection is the most important state in any editor — everything observes it
- Undo/redo requires the Command Pattern, not property snapshots or Flux stores
- The Inspector is a dynamic UI factory, not a static ViewModel binding
- A message bus decouples panels from each other and from services
- Document-View separates domain logic from presentation
- Plugin systems need a discovery mechanism (MEF, keyed DI) and published contracts
- Do not fight MVVM — use it where it fits, replace it where it does not

---

## See Also

- [Pattern 001 — Service Locator vs DI](001-service-locator-vs-di.md)
- [Pattern 002 — Plugin Architecture](002-plugin-architecture.md)
- [Pattern 003 — Async Initialization](003-async-initialization.md)
- [Pattern 004 — State Management](004-state-management.md)
- [026 — Accessibility & Automation](../02-tutorials/advanced/026-accessibility-automation.md)
- [041 — Menus, Context Menus, and Tray](../02-tutorials/intermediate/041-menus-context-menus-tray.md)
- [022 — Attached Properties & Behaviors](../02-tutorials/advanced/022-attached-properties-behaviors.md)
