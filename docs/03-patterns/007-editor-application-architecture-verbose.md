---
tier: reference
topic: architecture
estimated: 20-25 min
researched: 2026-06-18
avalonia-version: 12.0.4
companion-to: 007-editor-application-architecture.md
---

# 007V — Editor Application Architecture: An In-Depth Companion

You should already have read: [007 — Editor Application Architecture](007-editor-application-architecture.md) for the quick-start version. This file goes deeper on every section.

---

## 1. Why Standard MVVM Breaks at Editor Scale

A content-creation tool has fundamentally different state-sharing requirements than a line-of-business app. In a typical CRUD app, each page owns its own ViewModel and data context. In an editor:

- **Ten panels observe the same selection.** When the user clicks a node in the hierarchy tree, the inspector, property grid, canvas, status bar, and console all need to react. If each panel fetches selection from its own ViewModel property, you get stale reads and synchronization bugs.
- **One action triggers multi-step domain changes.** Deleting a node also removes its connections, updates the document's dirty flag, adjusts the scene graph's bounding box, and may invalidate a cached render. A ViewModel-bound `DeleteCommand` that only removes an item from an `ObservableCollection` cannot express these side effects.
- **The inspector is not a form — it is a UI factory.** The properties panel renders completely different controls depending on whether a `LightNode`, `CameraNode`, or `MeshNode` is selected. Hard-coding that dispatch in XAML with `DataTemplate` selectors works for three types but fails at thirty.
- **Undo/redo must span documents, panels, and services.** A single user action ("paint stroke", "move handle", "composite command") touches multiple domain objects. Property-value snapshots or `INotifyPropertyChanged` rollback are insufficient — you need first-class command objects that encapsulate inverse operations.

**Core insight:** The ViewModel layer in an editor is thin — it only exists to bridge services to XAML bindings. All real state lives in the service layer.

---

## 2. Service Layer Deep Dive

### Singleton Registration Rules

Every service must be registered as singleton because:
- Two instances of `SelectionService` would mean two sources of truth
- `UndoService` owns a stack that must be global
- `DocumentManager` tracks every open document

```csharp
// Program.cs — full editor service registration
builder.Services.AddSingleton<SelectionService>();
builder.Services.AddSingleton<UndoService>();
builder.Services.AddSingleton<DocumentManager>();
builder.Services.AddSingleton<WorkspaceService>();
builder.Services.AddSingleton<PluginService>();
builder.Services.AddSingleton<ProjectService>();
builder.Services.AddSingleton<IEditorProviderRegistry, EditorProviderRegistry>();
builder.Services.AddSingleton<IDockingLayoutService, DockingLayoutService>();
```

### SelectionService as the Architecture Hub

The `SelectionService` is the single most important service in an editor. Every panel depends on it directly or indirectly.

**Design decisions:**
- Expose both an event (`SelectionChanged`) and a message-bus message (`SelectionChangedMessage`). The event is for strongly-typed observers (ViewModels that need a direct reference), the message is for decoupled observers (plugins, logging, telemetry).
- Selection is always a list, not a single object. Even single-select editors use a list internally to simplify the API.
- The service is stateless with respect to *what* is selected — it only tracks *which* objects. The interpretation of a selection is the responsibility of each panel.

```csharp
public sealed class SelectionService
{
    private readonly IMessageBus _bus;
    private readonly List<object> _selected = new();
    private readonly HashSet<object> _selectedSet = new(); // O(1) contains check

    public IReadOnlyList<object> SelectedObjects => _selected;
    public int Count => _selected.Count;
    public bool IsSelected(object target) => _selectedSet.Contains(target);

    public event Action<SelectionChangedArgs>? SelectionChanged;

    public SelectionService(IMessageBus bus) => _bus = bus;

    public void Select(object target)
    {
        _selected.Clear();
        _selectedSet.Clear();
        _selected.Add(target);
        _selectedSet.Add(target);
        NotifyChanged();
    }

    public void SelectRange(IEnumerable<object> targets)
    {
        _selected.Clear();
        _selectedSet.Clear();
        foreach (var t in targets)
        {
            _selected.Add(t);
            _selectedSet.Add(t);
        }
        NotifyChanged();
    }

    public void ToggleSelection(object target)
    {
        if (_selectedSet.Remove(target))
            _selected.Remove(target);
        else
        {
            _selected.Add(target);
            _selectedSet.Add(target);
        }
        NotifyChanged();
    }

    public void Clear()
    {
        _selected.Clear();
        _selectedSet.Clear();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        var args = new SelectionChangedArgs(_selected);
        SelectionChanged?.Invoke(args);
        _bus.Publish(new SelectionChangedMessage(_selected));
    }
}
```

### WorkspaceService — Panel Layout State

The `WorkspaceService` owns the layout model: which panels are open, where they are docked, their sizes, and whether they are pinned or auto-hidden. It serializes to and deserializes from JSON so the editor restores its layout on restart.

```csharp
public sealed class WorkspaceService
{
    private readonly List<ToolWindowDescriptor> _toolWindows = new();
    public IReadOnlyList<ToolWindowDescriptor> ToolWindows => _toolWindows;

    public event Action? LayoutChanged;

    public void RegisterToolWindow(ToolWindowDescriptor descriptor)
    {
        _toolWindows.Add(descriptor);
        LayoutChanged?.Invoke();
    }

    public void ToggleToolWindow(string viewModelTypeName)
    {
        var existing = _toolWindows.FirstOrDefault(t => t.ViewModelTypeName == viewModelTypeName);
        if (existing is not null)
        {
            existing.IsVisible = !existing.IsVisible;
            LayoutChanged?.Invoke();
        }
    }

    public string SerializeLayout()
    {
        return JsonSerializer.Serialize(_toolWindows.Select(t => new
        {
            t.ViewModelTypeName, t.DefaultDock, t.IsVisible, t.IsPinned
        }));
    }

    public void DeserializeLayout(string json)
    {
        // Restore saved positions, sizes, visibility
    }
}

public sealed class ToolWindowDescriptor
{
    public string ViewModelTypeName { get; init; }
    public string Title { get; init; }
    public string DefaultDock { get; init; }
    public bool IsVisible { get; set; } = true;
    public bool IsPinned { get; set; } = true;
}
```

---

## 3. Document-View Architecture Deep Dive

### Why Documents Are Not ViewModels

In MVVM, the ViewModel wraps the Model and exposes properties for binding. In an editor, this coupling is harmful:

- A document has multiple views (outline tree, canvas, text source, property panel). If the document *is* a ViewModel, every view shares the same data context — you cannot have independent representations.
- Documents manage their own persistence. A ViewModel should not know about file paths, serialization formats, or save dialogs.
- Documents are long-lived and can outlive the panels that display them. When the user closes the outline tree and reopens it later, the same document instance should be bound.

```csharp
// IDocument — pure domain contract
public interface IDocument
{
    string Name { get; }
    string FilePath { get; }
    bool IsDirty { get; }
    Task<bool> SaveAsync(string? path = null);
    Task<bool> LoadAsync(string path);
    event Action? DirtyStateChanged;
    event Action? DocumentChanged;
}

// Concrete document — no UI concerns
public sealed class SceneDocument : IDocument
{
    public string Name { get; private set; } = "Untitled";
    public string FilePath { get; private set; } = "";
    public bool IsDirty { get; private set; }

    public SceneGraph SceneGraph { get; } = new();
    public AssetLibrary Assets { get; } = new();

    public event Action? DirtyStateChanged;
    public event Action? DocumentChanged;

    public async Task<bool> LoadAsync(string path)
    {
        FilePath = path;
        Name = Path.GetFileNameWithoutExtension(path);
        await using var stream = File.OpenRead(path);
        // Deserialize scene graph, assets, metadata
        IsDirty = false;
        DirtyStateChanged?.Invoke();
        DocumentChanged?.Invoke();
        return true;
    }

    public async Task<bool> SaveAsync(string? path = null)
    {
        path ??= FilePath;
        await using var stream = File.Create(path);
        // Serialize scene graph
        IsDirty = false;
        DirtyStateChanged?.Invoke();
        return true;
    }

    public void MarkDirty()
    {
        IsDirty = true;
        DirtyStateChanged?.Invoke();
    }
}
```

### DocumentManager as the Document Registry

The `DocumentManager` owns the list of open documents, tracks the active document, and orchestrates open/save/close workflows.

```csharp
public sealed class DocumentManager
{
    private readonly IMessageBus _bus;
    private readonly IDocumentFactory _factory;
    private readonly List<IDocument> _documents = new();

    public IReadOnlyList<IDocument> OpenDocuments => _documents;
    public IDocument? ActiveDocument { get; private set; }

    public event Action<IDocument>? DocumentOpened;
    public event Action<IDocument>? DocumentClosed;
    public event Action<IDocument>? ActiveDocumentChanged;

    public DocumentManager(IMessageBus bus, IDocumentFactory factory)
    {
        _bus = bus;
        _factory = factory;
    }

    public async Task<IDocument> OpenAsync(string path)
    {
        var doc = _factory.CreateForPath(path);
        await doc.LoadAsync(path);
        _documents.Add(doc);
        ActiveDocument = doc;
        DocumentOpened?.Invoke(doc);
        _bus.Publish(new DocumentOpenedMessage(doc));
        return doc;
    }

    public async Task<bool> SaveAsync(IDocument doc)
    {
        var success = await doc.SaveAsync();
        if (success)
            _bus.Publish(new DocumentSavedMessage(doc));
        return success;
    }

    public async Task<bool> SaveAllAsync()
    {
        foreach (var doc in _documents)
            await doc.SaveAsync();
        return true;
    }

    public async Task<bool> CloseAsync(IDocument doc)
    {
        if (doc.IsDirty)
        {
            // Prompt save dialog — delegate to a dialog service
            var result = await _dialogService.ConfirmSaveAsync(doc.Name);
            if (result == SaveResult.Cancel) return false;
            if (result == SaveResult.Save) await SaveAsync(doc);
        }
        _documents.Remove(doc);
        if (ActiveDocument == doc)
            ActiveDocument = _documents.LastOrDefault();
        DocumentClosed?.Invoke(doc);
        _bus.Publish(new DocumentClosedMessage(doc));
        return true;
    }

    public void Activate(IDocument doc)
    {
        if (ActiveDocument != doc)
        {
            ActiveDocument = doc;
            ActiveDocumentChanged?.Invoke(doc);
            _bus.Publish(new DocumentActivatedMessage(doc));
        }
    }
}
```

### The Shell ViewModel

The shell window's ViewModel owns the top-level chrome and composes tool windows dynamically.

```csharp
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly WorkspaceService _workspace;
    private readonly DocumentManager _docManager;
    private readonly IViewLocator _viewLocator;

    [ObservableProperty]
    private string _title = "Editor — Untitled";

    [ObservableProperty]
    private IDocument? _activeDocument;

    public ObservableCollection<ToolWindowViewModel> ToolWindows { get; } = new();

    public ShellViewModel(
        WorkspaceService workspace,
        DocumentManager docManager,
        IViewLocator viewLocator)
    {
        _workspace = workspace;
        _docManager = docManager;
        _viewLocator = viewLocator;

        docManager.DocumentOpened += OnDocumentOpened;
        docManager.ActiveDocumentChanged += OnActiveDocumentChanged;
        workspace.LayoutChanged += RebuildToolWindows;

        RebuildToolWindows();
    }

    private void OnDocumentOpened(IDocument doc)
    {
        Title = $"Editor — {doc.Name}";
    }

    private void OnActiveDocumentChanged(IDocument? doc)
    {
        ActiveDocument = doc;
        Title = doc is not null ? $"Editor — {doc.Name}" : "Editor — Untitled";
    }

    private void RebuildToolWindows()
    {
        ToolWindows.Clear();
        foreach (var descriptor in _workspace.ToolWindows.Where(t => t.IsVisible))
        {
            var vm = _viewLocator.CreateViewModel(descriptor.ViewModelTypeName);
            if (vm is ToolWindowViewModel twvm)
                ToolWindows.Add(twvm);
        }
    }
}
```

---

## 4. Command Pattern & UndoService — Advanced Topics

### Transactional Command Execution

The naive `UndoService` executes the command immediately inside `Record()`. In practice, commands may fail halfway through. A robust implementation wraps execution in a try/catch and supports rollback:

```csharp
public sealed class TransactionalUndoService
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private const int MaxHistory = 500;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public event Action? HistoryChanged;
    public event Action<string>? CommandFailed;

    public bool Record(IUndoableCommand command)
    {
        try
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            TrimHistory();
            HistoryChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            CommandFailed?.Invoke($"Command '{command.Name}' failed: {ex.Message}");
            // Attempt rollback if command exposes a rollback method
            if (command is IRecoverableCommand recoverable)
                recoverable.Rollback();
            return false;
        }
    }

    public bool Undo()
    {
        if (_undoStack.TryPop(out var cmd))
        {
            try
            {
                cmd.Undo();
                _redoStack.Push(cmd);
                HistoryChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                CommandFailed?.Invoke($"Undo failed for '{cmd.Name}': {ex.Message}");
                return false;
            }
        }
        return false;
    }

    public bool Redo()
    {
        if (_redoStack.TryPop(out var cmd))
        {
            try
            {
                cmd.Redo();
                _undoStack.Push(cmd);
                HistoryChanged?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                CommandFailed?.Invoke($"Redo failed for '{cmd.Name}': {ex.Message}");
                return false;
            }
        }
        return false;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    private void TrimHistory()
    {
        if (_undoStack.Count > MaxHistory)
        {
            var trimmed = _undoStack.Reverse().TakeLast(MaxHistory).Reverse().ToList();
            _undoStack.Clear();
            foreach (var c in trimmed) _undoStack.Push(c);
        }
    }
}

// Interface for commands that can recover from partial execution
public interface IRecoverableCommand
{
    void Rollback();
}
```

### CompositeCommand with Macro Recording

For complex operations (paste, duplicate, batch transform), use `CompositeCommand` to group multiple atomic commands into one undo step. An advanced variant supports live macro recording:

```csharp
public sealed class MacroRecorder
{
    private readonly UndoService _undoService;
    private List<IUndoableCommand>? _recording;

    public MacroRecorder(UndoService undoService) => _undoService = undoService;

    public bool IsRecording => _recording is not null;

    public void StartRecording(string macroName)
    {
        _recording = new List<IUndoableCommand>();
    }

    public void Capture(IUndoableCommand command)
    {
        _recording?.Add(command);
    }

    public void StopRecording()
    {
        if (_recording is null) return;
        if (_recording.Count > 0)
        {
            var composite = new CompositeCommand("Macro", _recording);
            _undoService.Record(composite);
        }
        _recording = null;
    }

    public void CancelRecording()
    {
        _recording = null;
    }
}
```

### PropertyBagCommand — Generic Undoable Property Changes

For simple property edits (rename a node, change a float value), writing a dedicated command class for every property is tedious. A generic `PropertyBagCommand` covers 80% of cases:

```csharp
public sealed class PropertyBagCommand : IUndoableCommand
{
    public string Name { get; }

    private readonly object _target;
    private readonly Dictionary<string, object?> _oldValues = new();
    private readonly Dictionary<string, object?> _newValues = new();

    public PropertyBagCommand(string name, object target,
        Dictionary<string, object?> newValues,
        Dictionary<string, object?> oldValues)
    {
        Name = name;
        _target = target;
        _newValues = newValues;
        _oldValues = oldValues;
    }

    public void Execute()
    {
        ApplyValues(_newValues);
    }

    public void Undo()
    {
        ApplyValues(_oldValues);
    }

    public void Redo() => Execute();

    private void ApplyValues(Dictionary<string, object?> values)
    {
        foreach (var (propName, value) in values)
        {
            var prop = _target.GetType().GetProperty(propName);
            prop?.SetValue(_target, value);
        }
    }
}

// Usage
var oldValues = new Dictionary<string, object?> { ["Position"] = oldPos, ["Rotation"] = oldRot };
var newValues = new Dictionary<string, object?> { ["Position"] = newPos, ["Rotation"] = newRot };
var cmd = new PropertyBagCommand("Transform", node, newValues, oldValues);
_undoService.Record(cmd);
```

---

## 5. Dynamic Inspector — Strategy Pattern In Depth

### Editor Provider Registry

Rather than injecting `IEnumerable<IEditorProvider>` directly into each inspector ViewModel, use a registry that orders providers by priority and caches results:

```csharp
public interface IEditorProviderRegistry
{
    IEditorProvider? GetProvider(Type type);
    void Register(IEditorProvider provider, int priority = 0);
}

public sealed class EditorProviderRegistry : IEditorProviderRegistry
{
    private readonly List<(IEditorProvider Provider, int Priority)> _providers = new();
    private readonly Dictionary<Type, IEditorProvider> _cache = new();

    public void Register(IEditorProvider provider, int priority = 0)
    {
        _providers.Add((provider, priority));
        _cache.Clear(); // invalidate cache
    }

    public IEditorProvider? GetProvider(Type type)
    {
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        foreach (var (provider, _) in _providers.OrderByDescending(p => p.Priority))
        {
            if (provider.CanHandle(type))
            {
                _cache[type] = provider;
                return provider;
            }
        }

        return null;
    }
}
```

### Composite Providers for Complex Types

A composite provider chains multiple sub-providers to build a multi-field editor:

```csharp
public sealed class CompositeEditorProvider : IEditorProvider
{
    private readonly IEditorProviderRegistry _registry;

    public CompositeEditorProvider(IEditorProviderRegistry registry) => _registry = registry;

    public bool CanHandle(Type type) => true; // fallback: decompose into properties

    public Control CreateEditor(object target)
    {
        var panel = new StackPanel { Spacing = 4 };
        foreach (var prop in target.GetType().GetProperties(
            BindingFlags.Public | BindingFlags.Instance))
        {
            var subProvider = _registry.GetProvider(prop.PropertyType);
            if (subProvider is not null)
            {
                var label = new TextBlock
                {
                    Text = prop.Name,
                    FontWeight = FontWeight.SemiBold,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var editor = subProvider.CreateEditor(prop.GetValue(target)!);
                panel.Children.Add(label);
                panel.Children.Add(editor);
            }
        }
        return new ScrollViewer { Content = panel };
    }
}
```

### InspectorViewModel — Reacting to Selection Changes

The inspector ViewModel orchestrates providers and manages the lifetime of created editors:

```csharp
public sealed partial class InspectorViewModel : ObservableObject
{
    private readonly SelectionService _selection;
    private readonly IEditorProviderRegistry _providers;

    [ObservableProperty]
    private Control? _currentEditor;

    [ObservableProperty]
    private string _header = "No selection";

    [ObservableProperty]
    private bool _hasSelection;

    private IDisposable? _previousSubscription;

    public InspectorViewModel(SelectionService selection, IEditorProviderRegistry providers)
    {
        _selection = selection;
        _providers = providers;
        _selection.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(SelectionChangedArgs args)
    {
        _previousSubscription?.Dispose();
        _previousSubscription = null;

        if (args.Selection.Count == 0)
        {
            CurrentEditor = null;
            Header = "No selection";
            HasSelection = false;
            return;
        }

        var target = args.Selection[0];
        Header = $"{target.GetType().Name}";
        HasSelection = true;

        var provider = _providers.GetProvider(target.GetType());
        if (provider is not null)
        {
            CurrentEditor = provider.CreateEditor(target);

            // If the target is observable, subscribe to changes
            if (target is INotifyPropertyChanged inpc)
            {
                PropertyChangedEventHandler handler = (_, e) =>
                {
                    // Rebuild editor for the changed property
                    CurrentEditor = provider.CreateEditor(target);
                };
                inpc.PropertyChanged += handler;
                _previousSubscription = Disposable.Create(() => inpc.PropertyChanged -= handler);
            }
        }
        else
        {
            CurrentEditor = new TextBlock
            {
                Text = $"No editor for {target.GetType().Name}",
                Foreground = Brushes.Gray,
            };
        }
    }
}
```

---

## 6. Message Bus — Decoupling at Scale

### Message Design Guidelines

- **Immutable records.** Every message should be a `sealed record`. Immutability guarantees thread safety.
- **Fine-grained types.** Do not use a single `GenericMessage<object>` — typed messages enable the bus to use `System.Reactive` `.OfType<T>()` filtering.
- **No payload references to UI objects.** Messages carry domain objects or IDs, never `Control` references. A `SelectionChangedMessage` carries domain `INode` references, not the `ListBoxItem` that was clicked.

```csharp
// Comprehensive message catalog for an editor
public sealed record SelectionChangedMessage(IReadOnlyList<object> Selection);
public sealed record SelectionClearedMessage;
public sealed record DocumentOpenedMessage(IDocument Document);
public sealed record DocumentClosedMessage(IDocument Document);
public sealed record DocumentSavedMessage(IDocument Document);
public sealed record DocumentActivatedMessage(IDocument Document);
public sealed record UndoRedoPerformedMessage(string ActionName);
public sealed record ProjectLoadedMessage(string ProjectPath);
public sealed record ToolChangedMessage(string ToolName);
public sealed record WorkspaceModifiedMessage(string PropertyName);
public sealed record NodeAddedMessage(INode Parent, INode Child, int Index);
public sealed record NodeRemovedMessage(INode Parent, INode Child, int Index);
public sealed record NodeMovedMessage(INode Node, INode OldParent, int OldIndex, INode NewParent, int NewIndex);
public sealed record AssetImportedMessage(string AssetPath, string Type);
public sealed record ProgressMessage(string Operation, double Progress);
```

### Message Bus Implementation

A lightweight in-process message bus that doesn't require external packages:

```csharp
public interface IMessageBus
{
    void Publish<T>(T message) where T : class;
    IDisposable Subscribe<T>(Action<T> handler) where T : class;
}

public sealed class MessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, List<WeakReference<object>>> _handlers = new();
    private readonly object _lock = new();

    public void Publish<T>(T message) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var weakRefs))
        {
            List<Action<T>> live = new();
            lock (_lock)
            {
                var dead = new List<WeakReference<object>>();
                foreach (var wr in weakRefs)
                {
                    if (wr.TryGetTarget(out var raw) && raw is Action<T> handler)
                        live.Add(handler);
                    else
                        dead.Add(wr);
                }
                foreach (var d in dead) weakRefs.Remove(d);
            }
            foreach (var handler in live)
                handler(message);
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : class
    {
        var wr = new WeakReference<object>(handler);
        var handlers = _handlers.GetOrAdd(typeof(T), _ => new List<WeakReference<object>>());
        lock (_lock) handlers.Add(wr);
        return Disposable.Create(() =>
        {
            lock (_lock) handlers.Remove(wr);
        });
    }
}
```

---

## 7. Tool Window Architecture — ViewModel + View per Panel

### ToolWindowAttribute and Discovery

The `ToolWindowAttribute` decorates ViewModel classes so the `WorkspaceService` can discover and instantiate tool windows automatically:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ToolWindowAttribute : Attribute
{
    public string Title { get; }
    public string DefaultDock { get; }
    public Type ViewType { get; }

    public ToolWindowAttribute(string title, string defaultDock = "Right", Type? viewType = null)
    {
        Title = title;
        DefaultDock = defaultDock;
        ViewType = viewType ?? typeof(object);
    }
}

// Concrete tool window ViewModel
[ToolWindow("Hierarchy", "Left")]
public sealed partial class HierarchyViewModel : ObservableObject
{
    private readonly SelectionService _selection;
    private readonly DocumentManager _docManager;
    private readonly IMessageBus _bus;

    [ObservableProperty]
    private IReadOnlyList<INode> _nodes = Array.Empty<INode>();

    [ObservableProperty]
    private INode? _selectedNode;

    public HierarchyViewModel(
        SelectionService selection,
        DocumentManager docManager,
        IMessageBus bus)
    {
        _selection = selection;
        _docManager = docManager;
        _bus = bus;

        _docManager.DocumentOpened += OnDocumentOpened;
    }

    private void OnDocumentOpened(IDocument doc)
    {
        if (doc is SceneDocument scene)
            Nodes = scene.SceneGraph.RootNodes;
    }

    [RelayCommand]
    private void SelectNode(INode node)
    {
        SelectedNode = node;
        _selection.Select(node);
    }

    [RelayCommand]
    private void DeleteNode(INode node)
    {
        // Create undoable command and record it
        var cmd = new DeleteNodeCommand(node, GetActiveSceneGraph());
        // Resolve UndoService via service locator or DI
    }
}

// Corresponding view
public partial class HierarchyView : UserControl
{
    public HierarchyView()
    {
        InitializeComponent();
    }
}
```

### ToolWindowViewModel Base Class

All tool window ViewModels share a common base for lifecycle, visibility, and docking metadata:

```csharp
public abstract class ToolWindowViewModel : ObservableObject
{
    public string Title { get; protected set; } = "Tool Window";
    public string DefaultDock { get; protected set; } = "Right";

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isPinned = true;

    protected ToolWindowViewModel() { }

    protected ToolWindowViewModel(string title, string defaultDock)
    {
        Title = title;
        DefaultDock = defaultDock;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual void OnClosed() { }
}
```

---

## 8. Plugin / Extensibility System — Deep Dive

### Discovery Strategies

There are three approaches to plugin discovery in Avalonia:

| Approach | Pros | Cons | Best for |
|---|---|---|---|
| **MEF** (`System.ComponentModel.Composition`) | Runtime discovery, no compile-time reference | Reflection overhead, no AOT | Desktop-only, third-party plugins loaded from disk |
| **Keyed DI** (`Microsoft.Extensions.DependencyInjection`) | AOT-safe, compile-time verified | Plugin must be referenced at build time | First-party plugins, internal modules |
| **Source Generators** | Fastest, AOT-safe | Complex to implement | Large teams, closed-ecosystem plugins |

### MEF-Based Plugin Service

```csharp
public sealed class PluginService
{
    private readonly CompositionContainer _container;
    private readonly IMessageBus _bus;

    [ImportMany]
    private IEnumerable<IMenuContributor> MenuContributors = Array.Empty<IMenuContributor>();

    [ImportMany]
    private IEnumerable<IToolWindowContributor> ToolWindowContributors = Array.Empty<IToolWindowContributor>();

    [ImportMany]
    private IEnumerable<IEditorProvider> EditorProviders = Array.Empty<IEditorProvider>();

    public PluginService(IMessageBus bus)
    {
        _bus = bus;
        var catalog = new DirectoryCatalog("./plugins");
        _container = new CompositionContainer(catalog);
        _container.ComposeParts(this);
    }

    public void Initialize(IEditorProviderRegistry editorRegistry, WorkspaceService workspace)
    {
        foreach (var contributor in MenuContributors)
            contributor.BuildMenu(new MenuBuilder(_bus));

        foreach (var contributor in ToolWindowContributors)
        {
            var descriptor = new ToolWindowDescriptor
            {
                Title = contributor.Title,
                ViewModelTypeName = contributor.GetType().FullName!,
                DefaultDock = "Right",
                IsVisible = contributor.IsVisibleByDefault
            };
            workspace.RegisterToolWindow(descriptor);
        }

        foreach (var provider in EditorProviders)
            editorRegistry.Register(provider);
    }
}

// Plugin contract interfaces
public interface IMenuContributor
{
    void BuildMenu(IMenuBuilder builder);
}

public interface IToolWindowContributor
{
    string Title { get; }
    bool IsVisibleByDefault { get; }
    Control CreateView();
    object CreateViewModel();
}

// Example plugin
[Export(typeof(IMenuContributor))]
[Export(typeof(IToolWindowContributor))]
public sealed class DebugPlugin : IMenuContributor, IToolWindowContributor
{
    public string Title => "Debug Console";
    public bool IsVisibleByDefault => false;

    public void BuildMenu(IMenuBuilder builder)
    {
        builder.Add("Tools/Debug Console", () =>
        {
            // Toggle debug console visibility
        });
    }

    public Control CreateView() => new DebugConsoleView();
    public object CreateViewModel() => new DebugConsoleViewModel();
}
```

### IMenuBuilder Interface

```csharp
public interface IMenuBuilder
{
    void Add(string path, Action action);
    void AddSeparator(string path);
    void AddToggle(string path, bool isChecked, Action<bool> onToggle);
    void AddSubmenu(string path, Action<IMenuBuilder> buildSubmenu);
}

public sealed class MenuBuilder : IMenuBuilder
{
    private readonly IMessageBus _bus;
    private readonly List<MenuItemDescriptor> _items = new();

    public MenuBuilder(IMessageBus bus) => _bus = bus;

    public void Add(string path, Action action)
    {
        _items.Add(new MenuItemDescriptor { Path = path, Action = action, IsSeparator = false });
    }

    public void AddSeparator(string path)
    {
        _items.Add(new MenuItemDescriptor { Path = path, IsSeparator = true });
    }

    public void AddToggle(string path, bool isChecked, Action<bool> onToggle)
    {
        _items.Add(new MenuItemDescriptor { Path = path, IsToggle = true, IsChecked = isChecked, ToggleAction = onToggle });
    }

    public void AddSubmenu(string path, Action<IMenuBuilder> buildSubmenu)
    {
        var subBuilder = new MenuBuilder(_bus);
        buildSubmenu(subBuilder);
        _items.Add(new MenuItemDescriptor { Path = path, IsSubmenu = true, SubItems = subBuilder._items });
    }

    public IReadOnlyList<MenuItemDescriptor> Build() => _items;
}
```

---

## 9. Concrete Flow: User Deletes a Node — Full Trace

Stepping through the full lifecycle with code traces:

### Step 1: User clicks Delete in Hierarchy panel

```csharp
// HierarchyViewModel
[RelayCommand]
private void DeleteNode(INode node)
{
    var graph = GetActiveSceneGraph();
    var cmd = new DeleteNodeCommand(node, graph);
    _undoService.Record(cmd);
}
```

### Step 2: UndoService.Record() executes and pushes

```csharp
// Inside DeleteNodeCommand.Execute()
public void Execute()
{
    _graph.RemoveNode(_node);
    // RemoveNode publishes NodeRemovedMessage via IMessageBus
}
```

### Step 3: Cascade via message bus

```csharp
// SceneGraph.RemoveNode
public void RemoveNode(INode node)
{
    var index = _nodes.IndexOf(node);
    _nodes.Remove(node);
    _bus.Publish(new NodeRemovedMessage(_parent, node, index));
    MarkDirty();
}

// CanvasViewModel — subscriber
_bus.Subscribe<NodeRemovedMessage>(msg =>
{
    if (msg.Node == _selectedNode)
        _selection.Clear();
    InvalidateRender();
});

// InspectorViewModel — subscriber
_bus.Subscribe<NodeRemovedMessage>(msg =>
{
    if (msg.Node == _selection.SelectedObjects.FirstOrDefault())
        _selection.Clear();
});

// OutlineViewModel — subscriber (handles tree update)
_bus.Subscribe<NodeRemovedMessage>(msg =>
{
    var parentVm = FindViewModel(msg.Parent);
    parentVm?.Children.Remove(msg.Node);
});
```

### Step 4: UI updates automatically via bindings

The shell title, status bar text, and undo/redo labels all update because their ViewModels observed the `SelectionService`, `DocumentManager`, or `UndoService` events:

```csharp
// UndoService fires HistoryChanged
HistoryChanged?.Invoke();

// ShellViewModel subscribes to HistoryChanged
_undoService.HistoryChanged += () =>
{
    UndoLabel = $"Undo {_undoService.UndoName}";
    RedoLabel = $"Redo {_undoService.RedoName}";
    CanUndo = _undoService.CanUndo;
    CanRedo = _undoService.CanRedo;
};
```

---

## 10. Performance Considerations

### Command Stack Memory

With `MaxHistory = 200`, each command can hold references to domain objects. For large commands (duplicate of 1000 nodes), consider:

- Storing only IDs or weak references in the command, not full object graphs
- Implementing `IUndoableCommand.Dispose()` to release references when the command is evicted from the stack
- Using the `TrimHistory()` strategy that removes the oldest half, not just the oldest one

### Selection Notification Throttling

When the user drags across 500 objects in the canvas, `SelectRange` fires once at the end of the drag operation, not per-object. Implement a debounce:

```csharp
public void SelectRangeBulk(IEnumerable<object> targets)
{
    _selected.Clear();
    _selectedSet.Clear();
    foreach (var t in targets)
    {
        _selected.Add(t);
        _selectedSet.Add(t);
    }
    // Debounce — batch the notification
    _debouncer.Debounce(50, NotifyChanged);
}
```

### Inspector Editor Pooling

Creating new `Control` instances on every selection change causes allocation pressure. Pool editors for commonly selected types:

```csharp
public sealed class PooledEditorProvider : IEditorProvider
{
    private readonly IEditorProvider _inner;
    private readonly Dictionary<object, Control> _pool = new();

    public PooledEditorProvider(IEditorProvider inner) => _inner = inner;

    public bool CanHandle(Type type) => _inner.CanHandle(type);

    public Control CreateEditor(object target)
    {
        if (_pool.TryGetValue(target, out var existing))
            return existing;
        var editor = _inner.CreateEditor(target);
        _pool[target] = editor;
        return editor;
    }

    public void Release(object target) => _pool.Remove(target);
}
```

---

## Summary Comparison: Core vs. In-Depth

| Concept | Core Coverage | Verbose Additions |
|---|---|---|
| Service Layer | Singleton registration, basic `SelectionService` | `SelectionService` with `HashSet`, `WorkspaceService` with layout serialization |
| Document-View | Basic `IDocument` + `DocumentManager` | `DocumentManager` with close flow, save prompts, `IDocumentFactory`, `ShellViewModel` |
| Command Pattern | `IUndoableCommand`, `UndoService`, `CompositeCommand` | Transactional execution, `MacroRecorder`, `PropertyBagCommand`, `IRecoverableCommand` |
| Dynamic Inspector | `IEditorProvider`, basic providers, `InspectorViewModel` | `EditorProviderRegistry` with caching/priority, composite provider, pooled editors |
| Message Bus | Message records, `StatusBarViewModel` subscription | Full `IMessageBus` implementation with weak references, message design guidelines, comprehensive catalog |
| Tool Windows | `ToolWindowAttribute`, `HierarchyViewModel` | `ToolWindowViewModel` base class, full lifecycle, dock metadata |
| Plugin System | MEF contracts, `PluginService` | MEF vs keyed DI vs source generators comparison, `IMenuBuilder`, full plugin example |
| Concrete Flow | 10-step delete flow | Code-level trace with actual method calls and message bus subscribers |
| Performance | — | Command memory, notification throttling, editor pooling |
