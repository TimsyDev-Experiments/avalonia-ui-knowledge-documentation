---
tier: reference
topic: architecture
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 007-editor-application-architecture.md
---

# 007X — Editor Application Architecture: Real-World Examples

## Example 1: Node Graph Editor with Full Undo/Redo and Selection

This example builds a minimal node graph editor where users can add, delete, and connect nodes, with full undo/redo support and a selection-driven inspector.

### Domain Model

```csharp
public sealed class NodeGraph
{
    public List<Node> Nodes { get; } = new();
    public List<Connection> Connections { get; } = new();

    public void AddNode(Node node) => Nodes.Add(node);
    public void RemoveNode(Node node) => Nodes.Remove(node);
    public void AddConnection(Connection c) => Connections.Add(c);
    public void RemoveConnection(Connection c) => Connections.Remove(c);
}

public sealed class Node
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Node";
    public double X { get; set; }
    public double Y { get; set; }
    public List<InputPort> Inputs { get; } = new();
    public List<OutputPort> Outputs { get; } = new();
}

public sealed record Connection(Node Source, OutputPort Output, Node Target, InputPort Input);
public sealed record InputPort(string Name);
public sealed record OutputPort(string Name);
```

### Commands

```csharp
public sealed class AddNodeCommand : IUndoableCommand
{
    public string Name => "Add Node";

    private readonly NodeGraph _graph;
    private readonly Node _node;

    public AddNodeCommand(NodeGraph graph, Node node)
    {
        _graph = graph;
        _node = node;
    }

    public void Execute() => _graph.AddNode(_node);
    public void Undo() => _graph.RemoveNode(_node);
    public void Redo() => Execute();
}

public sealed class DeleteNodeCommand : IUndoableCommand
{
    public string Name => "Delete Node";

    private readonly NodeGraph _graph;
    private readonly Node _node;
    private readonly List<Connection> _connections;

    public DeleteNodeCommand(NodeGraph graph, Node node)
    {
        _graph = graph;
        _node = node;
        _connections = graph.Connections
            .Where(c => c.Source == node || c.Target == node)
            .ToList();
    }

    public void Execute()
    {
        foreach (var c in _connections) _graph.RemoveConnection(c);
        _graph.RemoveNode(_node);
    }

    public void Undo()
    {
        _graph.AddNode(_node);
        foreach (var c in _connections) _graph.AddConnection(c);
    }

    public void Redo() => Execute();
}

public sealed class ConnectNodesCommand : IUndoableCommand
{
    public string Name => "Connect Nodes";

    private readonly NodeGraph _graph;
    private readonly Connection _connection;

    public ConnectNodesCommand(NodeGraph graph, Connection connection)
    {
        _graph = graph;
        _connection = connection;
    }

    public void Execute() => _graph.AddConnection(_connection);
    public void Undo() => _graph.RemoveConnection(_connection);
    public void Redo() => Execute();
}
```

### SelectionService and UndoService

```csharp
public sealed class GraphEditorViewModel
{
    private readonly SelectionService _selection;
    private readonly UndoService _undo;
    private readonly NodeGraph _graph = new();

    public GraphEditorViewModel(SelectionService selection, UndoService undo)
    {
        _selection = selection;
        _undo = undo;
    }

    [RelayCommand]
    private void AddNode()
    {
        var node = new Node { Name = $"Node {_graph.Nodes.Count + 1}", X = 100, Y = 100 };
        _undo.Record(new AddNodeCommand(_graph, node));
        _selection.Select(node);
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        foreach (var obj in _selection.SelectedObjects)
        {
            if (obj is Node node)
                _undo.Record(new DeleteNodeCommand(_graph, node));
        }
        _selection.Clear();
    }

    [RelayCommand]
    private void Undo() => _undo.Undo();

    [RelayCommand]
    private void Redo() => _undo.Redo();
}
```

---

## Example 2: Dynamic Inspector for a 3D Scene Editor

This example demonstrates how different object types render different editors in the property panel.

### Domain Objects

```csharp
public interface ISceneNode
{
    string Name { get; set; }
    Guid Id { get; }
}

public sealed class LightNode : ISceneNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Light";
    public string LightType { get; set; } = "Point"; // Point, Directional, Spot
    public float Intensity { get; set; } = 1.0f;
    public Color Color { get; set; } = Colors.White;
}

public sealed class CameraNode : ISceneNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Camera";
    public float FieldOfView { get; set; } = 60f;
    public float NearClip { get; set; } = 0.1f;
    public float FarClip { get; set; } = 1000f;
}

public sealed class MeshNode : ISceneNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; } = "Mesh";
    public string MeshPath { get; set; } = "";
    public bool CastShadows { get; set; } = true;
    public bool ReceiveShadows { get; set; } = true;
}
```

### Editor Providers

```csharp
public sealed class LightEditorProvider : IEditorProvider
{
    public bool CanHandle(Type type) => type == typeof(LightNode);

    public Control CreateEditor(object target)
    {
        var light = (LightNode)target;
        var panel = new StackPanel { Spacing = 8 };

        var nameBox = new TextBox { Text = light.Name };
        nameBox.TextChanged += (_, _) => light.Name = nameBox.Text;
        panel.Children.Add(new TextBlock { Text = "Name" });
        panel.Children.Add(nameBox);

        var typeBox = new ComboBox
        {
            Items = new[] { "Point", "Directional", "Spot" },
            SelectedItem = light.LightType
        };
        typeBox.SelectionChanged += (_, _) => light.LightType = (string)typeBox.SelectedItem!;
        panel.Children.Add(new TextBlock { Text = "Type" });
        panel.Children.Add(typeBox);

        var intensitySlider = new Slider
        {
            Minimum = 0, Maximum = 10,
            Value = light.Intensity
        };
        intensitySlider.ValueChanged += (_, e) => light.Intensity = (float)e.NewValue;
        panel.Children.Add(new TextBlock { Text = "Intensity" });
        panel.Children.Add(intensitySlider);

        return panel;
    }
}

public sealed class CameraEditorProvider : IEditorProvider
{
    public bool CanHandle(Type type) => type == typeof(CameraNode);

    public Control CreateEditor(object target)
    {
        var camera = (CameraNode)target;
        var panel = new StackPanel { Spacing = 8 };

        panel.Children.Add(new TextBlock { Text = "Field of View" });
        var fov = new NumericUpDown
        {
            Value = (decimal)camera.FieldOfView,
            Minimum = 1, Maximum = 179
        };
        fov.ValueChanged += (_, e) => camera.FieldOfView = (float)e.NewValue;
        panel.Children.Add(fov);

        panel.Children.Add(new TextBlock { Text = "Near Clip" });
        var near = new NumericUpDown
        {
            Value = (decimal)camera.NearClip,
            Minimum = 0.01m, Maximum = 100,
            Increment = 0.1m
        };
        near.ValueChanged += (_, e) => camera.NearClip = (float)e.NewValue;
        panel.Children.Add(near);

        panel.Children.Add(new TextBlock { Text = "Far Clip" });
        var far = new NumericUpDown
        {
            Value = (decimal)camera.FarClip,
            Minimum = 1, Maximum = 10000
        };
        far.ValueChanged += (_, e) => camera.FarClip = (float)e.NewValue;
        panel.Children.Add(far);

        return panel;
    }
}

public sealed class MeshEditorProvider : IEditorProvider
{
    public bool CanHandle(Type type) => type == typeof(MeshNode);

    public Control CreateEditor(object target)
    {
        var mesh = (MeshNode)target;
        var panel = new StackPanel { Spacing = 8 };

        var pathBox = new TextBox { Text = mesh.MeshPath, Watermark = "Path to mesh file..." };
        pathBox.TextChanged += (_, _) => mesh.MeshPath = pathBox.Text;
        panel.Children.Add(new TextBlock { Text = "Mesh Path" });
        panel.Children.Add(pathBox);

        var castBox = new CheckBox { Content = "Cast Shadows", IsChecked = mesh.CastShadows };
        castBox.IsCheckedChanged += (_, e) => mesh.CastShadows = e.GetValueOrDefault();
        panel.Children.Add(castBox);

        var receiveBox = new CheckBox { Content = "Receive Shadows", IsChecked = mesh.ReceiveShadows };
        receiveBox.IsCheckedChanged += (_, e) => mesh.ReceiveShadows = e.GetValueOrDefault();
        panel.Children.Add(receiveBox);

        return panel;
    }
}
```

---

## Example 3: Plugin System with MEF — Custom Export Inspector

This example shows how a third-party plugin contributes a custom inspector for a type from an external assembly.

### Plugin Contract (shared library)

```csharp
// Contracts.dll — shared interface assembly
public interface IEditorProvider
{
    bool CanHandle(Type type);
    Control CreateEditor(object target);
}

public interface IMenuContributor
{
    void BuildMenu(IMenuBuilder builder);
}
```

### Plugin Implementation

```csharp
// MyCustomPlugin.dll
[Export(typeof(IEditorProvider))]
public sealed class CustomMaterialEditor : IEditorProvider
{
    public bool CanHandle(Type type) =>
        type.FullName == "MyGameEngine.Material";

    public Control CreateEditor(object target)
    {
        var panel = new StackPanel { Spacing = 8 };

        var albedo = target.GetType().GetProperty("AlbedoColor")?.GetValue(target);
        var metallic = target.GetType().GetProperty("Metallic")?.GetValue(target);
        var roughness = target.GetType().GetProperty("Roughness")?.GetValue(target);

        panel.Children.Add(new TextBlock
        {
            Text = "Material Properties",
            FontSize = 16,
            FontWeight = FontWeight.Bold
        });

        var colorEditor = new ColorPicker { Color = albedo is Color c ? c : Colors.White };
        colorEditor.ColorChanged += (_, e) =>
            target.GetType().GetProperty("AlbedoColor")?.SetValue(target, e.NewColor);
        panel.Children.Add(new TextBlock { Text = "Albedo" });
        panel.Children.Add(colorEditor);

        var metallicSlider = new Slider
        {
            Minimum = 0, Maximum = 1,
            Value = metallic is float m ? m : 0
        };
        metallicSlider.ValueChanged += (_, e) =>
            target.GetType().GetProperty("Metallic")?.SetValue(target, (float)e.NewValue);
        panel.Children.Add(new TextBlock { Text = "Metallic" });
        panel.Children.Add(metallicSlider);

        var roughSlider = new Slider
        {
            Minimum = 0, Maximum = 1,
            Value = roughness is float r ? r : 0.5f
        };
        roughSlider.ValueChanged += (_, e) =>
            target.GetType().GetProperty("Roughness")?.SetValue(target, (float)e.NewValue);
        panel.Children.Add(new TextBlock { Text = "Roughness" });
        panel.Children.Add(roughSlider);

        return panel;
    }
}

[Export(typeof(IMenuContributor))]
public sealed class PluginMenu : IMenuContributor
{
    public void BuildMenu(IMenuBuilder builder)
    {
        builder.Add("Plugins/Material Editor", () =>
        {
            var window = new Window
            {
                Title = "Material Editor",
                Width = 400,
                Height = 600,
                Content = new TextBlock
                {
                    Text = "Material Editor Window",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            window.Show();
        });
    }
}
```

### Host Application Discovery

```csharp
// In editor startup
public sealed class PluginLoader
{
    public IEnumerable<IEditorProvider> LoadEditorProviders()
    {
        var catalog = new DirectoryCatalog("./Plugins");
        var container = new CompositionContainer(catalog);
        return container.GetExportedValues<IEditorProvider>();
    }
}

// Registration in App.axaml.cs
var pluginProviders = new PluginLoader().LoadEditorProviders();
foreach (var provider in pluginProviders)
    editorProviderRegistry.Register(provider, priority: 10);
```

---

## Example 4: Message Bus — Cross-Panel Synchronization

This example shows four panels reacting to the same user action: selecting a node in the hierarchy panel.

### Panel ViewModels

```csharp
// Messages
public sealed record NodeSelectedMessage(INode Node);
public sealed record NodeDeselectedMessage;

// Hierarchy Panel
[ToolWindow("Hierarchy", "Left")]
public sealed partial class HierarchyPanelViewModel : ObservableObject
{
    private readonly IMessageBus _bus;
    private readonly SelectionService _selection;

    [ObservableProperty]
    private ObservableCollection<INode> _nodes = new();

    public HierarchyPanelViewModel(IMessageBus bus, SelectionService selection)
    {
        _bus = bus;
        _selection = selection;
    }

    [RelayCommand]
    private void SelectNode(INode node)
    {
        _selection.Select(node);
        _bus.Publish(new NodeSelectedMessage(node));
    }
}

// Properties Panel — rebuilds UI on selection
[ToolWindow("Properties", "Right")]
public sealed partial class PropertiesPanelViewModel : ObservableObject
{
    private readonly IMessageBus _bus;
    private readonly IEditorProviderRegistry _editors;

    [ObservableProperty]
    private Control? _editorControl;

    [ObservableProperty]
    private string _header = "No selection";

    public PropertiesPanelViewModel(IMessageBus bus, IEditorProviderRegistry editors)
    {
        _bus = bus;
        _editors = editors;
        _bus.Subscribe<NodeSelectedMessage>(OnNodeSelected);
        _bus.Subscribe<NodeDeselectedMessage>(_ => ClearEditor());
    }

    private void OnNodeSelected(NodeSelectedMessage msg)
    {
        Header = msg.Node.Name;
        var provider = _editors.GetProvider(msg.Node.GetType());
        EditorControl = provider?.CreateEditor(msg.Node);
    }

    private void ClearEditor()
    {
        EditorControl = null;
        Header = "No selection";
    }
}

// Status Bar — shows selection info
[ToolWindow("Status Bar", "Bottom")]
public sealed partial class StatusBarPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    public StatusBarPanelViewModel(IMessageBus bus)
    {
        bus.Subscribe<NodeSelectedMessage>(msg =>
            StatusText = $"Selected: {msg.Node.Name} ({msg.Node.GetType().Name})");
        bus.Subscribe<NodeDeselectedMessage>(_ =>
            StatusText = "No selection");
    }
}

// Canvas — updates gizmo target
[ToolWindow("Canvas", "Center")]
public sealed partial class CanvasPanelViewModel : ObservableObject
{
    [ObservableProperty]
    private INode? _selectedNode;

    [ObservableProperty]
    private bool _showGizmo;

    public CanvasPanelViewModel(IMessageBus bus)
    {
        bus.Subscribe<NodeSelectedMessage>(msg =>
        {
            SelectedNode = msg.Node;
            ShowGizmo = true;
        });
        bus.Subscribe<NodeDeselectedMessage>(_ =>
        {
            SelectedNode = null;
            ShowGizmo = false;
        });
    }
}
```

### XAML Shell Composition

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        x:DataType="vm:ShellViewModel">
  <DockPanel>
    <!-- Toolbar -->
    <Menu DockPanel.Dock="Top">
      <MenuItem Header="File" />
      <MenuItem Header="Edit" />
      <MenuItem Header="View" />
    </Menu>

    <!-- Status Bar -->
    <ContentControl DockPanel.Dock="Bottom"
                    Content="{Binding StatusBarPanel}" />

    <!-- Left: Hierarchy -->
    <ContentControl DockPanel.Dock="Left" Width="250"
                    Content="{Binding HierarchyPanel}" />

    <!-- Right: Properties -->
    <ContentControl DockPanel.Dock="Right" Width="300"
                    Content="{Binding PropertiesPanel}" />

    <!-- Center: Canvas -->
    <ContentControl Content="{Binding CanvasPanel}" />
  </DockPanel>
</Window>
```
