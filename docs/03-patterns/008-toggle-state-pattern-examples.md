---
tier: intermediate
topic: architecture
estimated: 15-20 min
researched: 2026-06-18
avalonia-version: 12.0.4
example-of: 008-toggle-state-pattern.md
---

# 008X — Toggle & State Pattern: Real-World Examples

## Example 1: Image Editor Tool Palette with Mutual Exclusion

A photo editor toolbar where exactly one tool is active at a time. Each tool is a toggle, and switching tools deactivates the previous one.

### Setup

```csharp
// Register toggles with mutual exclusion
var toggles = new ToggleService();
toggles.MakeMutuallyExclusive("tool:select", "tool:brush", "tool:eraser",
    "tool:fill", "tool:eyedropper", "tool:zoom");
toggles.Enable("tool:select"); // default tool
```

### ViewModel

```csharp
public sealed partial class ToolPaletteViewModel : ObservableObject
{
    private readonly IToggleService _toggles;

    [ObservableProperty]
    private bool _selectToolActive;

    [ObservableProperty]
    private bool _brushToolActive;

    [ObservableProperty]
    private bool _eraserToolActive;

    [ObservableProperty]
    private bool _fillToolActive;

    [ObservableProperty]
    private bool _eyedropperToolActive;

    [ObservableProperty]
    private bool _zoomToolActive;

    [ObservableProperty]
    private string _activeToolName = "Select";

    public ToolPaletteViewModel(IToggleService toggles)
    {
        _toggles = toggles;

        // Sync properties with toggle service
        SelectToolActive = toggles.IsEnabled("tool:select");
        BrushToolActive = toggles.IsEnabled("tool:brush");

        toggles.Observe("tool:select").Subscribe(v => SelectToolActive = v);
        toggles.Observe("tool:brush").Subscribe(v => BrushToolActive = v);
        toggles.Observe("tool:eraser").Subscribe(v => EraserToolActive = v);

        toggles.ToggleChanged += (key, _) =>
        {
            ActiveToolName = key switch
            {
                "tool:select" => "Select",
                "tool:brush" => "Brush",
                "tool:eraser" => "Eraser",
                "tool:fill" => "Fill",
                "tool:eyedropper" => "Eyedropper",
                "tool:zoom" => "Zoom",
                _ => ActiveToolName
            };
        };
    }

    [RelayCommand]
    private void ActivateTool(string tool)
    {
        _toggles.Enable($"tool:{tool}");
    }
}
```

### View

```xml
<ToolBar>
  <ToggleButton IsChecked="{Binding SelectToolActive}"
                Command="{Binding ActivateToolCommand}" CommandParameter="select"
                ToolTip.Tip="Select (V)">
    <Path Data="M0,0 L10,0 L10,10 Z" />
  </ToggleButton>
  <ToggleButton IsChecked="{Binding BrushToolActive}"
                Command="{Binding ActivateToolCommand}" CommandParameter="brush"
                ToolTip.Tip="Brush (B)">
    <Ellipse Width="8" Height="8" Fill="Black" />
  </ToggleButton>
  <ToggleButton IsChecked="{Binding EraserToolActive}"
                Command="{Binding ActivateToolCommand}" CommandParameter="eraser"
                ToolTip.Tip="Eraser (E)" />
  <ToggleButton IsChecked="{Binding FillToolActive}"
                Command="{Binding ActivateToolCommand}" CommandParameter="fill"
                ToolTip.Tip="Fill (G)" />
  <ToggleButton IsChecked="{Binding EyedropperToolActive}"
                Command="{Binding ActivateToolCommand}" CommandParameter="eyedropper"
                ToolTip.Tip="Eyedropper (I)" />
  <ToggleButton IsChecked="{Binding ZoomToolActive}"
                Command="{Binding ActivateToolCommand}" CommandParameter="zoom"
                ToolTip.Tip="Zoom (Z)" />
</ToolBar>
```

---

## Example 2: Settings Panel with Persistent Toggles

An application settings dialog where toggles survive application restart.

### Setup and Registration

```csharp
// Program.cs
var innerToggles = new ToggleService();

// Create persistent wrapper
var appData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyApp", "settings.json");

var persistentToggles = new PersistentToggleService(innerToggles, appData);
builder.Services.AddSingleton<IToggleService>(persistentToggles);

// In App.axaml.cs after initialization
var toggles = services.GetRequiredService<IToggleService>();
if (toggles is PersistentToggleService pts)
    await pts.LoadAsync();

// On app exit
if (toggles is PersistentToggleService pts2)
    await pts2.SaveAsync();
```

### ViewModel

```csharp
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IToggleService _toggles;

    [ObservableProperty]
    private bool _darkMode;

    [ObservableProperty]
    private bool _compactLayout;

    [ObservableProperty]
    private bool _showGrid;

    [ObservableProperty]
    private bool _autoSave;

    [ObservableProperty]
    private bool _notificationsEnabled;

    public SettingsViewModel(IToggleService toggles)
    {
        _toggles = toggles;

        // Initialize from saved state
        DarkMode = toggles.IsEnabled("settings:darkmode");
        CompactLayout = toggles.IsEnabled("settings:compact");
        ShowGrid = toggles.IsEnabled("settings:showgrid");
        AutoSave = toggles.IsEnabled("settings:autosave");
        NotificationsEnabled = toggles.IsEnabled("settings:notifications");
    }

    partial void OnDarkModeChanged(bool value)
    {
        _toggles.SetState("settings:darkmode", value);
        ApplyTheme(value ? ThemeVariant.Dark : ThemeVariant.Light);
    }

    partial void OnCompactLayoutChanged(bool value)
    {
        _toggles.SetState("settings:compact", value);
    }

    partial void OnShowGridChanged(bool value)
    {
        _toggles.SetState("settings:showgrid", value);
    }

    partial void OnAutoSaveChanged(bool value)
    {
        _toggles.SetState("settings:autosave", value);
    }

    partial void OnNotificationsEnabledChanged(bool value)
    {
        _toggles.SetState("settings:notifications", value);
    }

    [RelayCommand]
    private async Task ResetDefaultsAsync()
    {
        DarkMode = false;
        CompactLayout = false;
        ShowGrid = true;
        AutoSave = true;
        NotificationsEnabled = true;

        if (_toggles is PersistentToggleService pts)
            await pts.SaveAsync();
    }
}
```

### View

```xml
<StackPanel Spacing="8" Margin="16">
  <TextBlock Text="Appearance" FontWeight="SemiBold" FontSize="16" />
  <ToggleSwitch IsOn="{Binding DarkMode}" Content="Dark Mode"
                OnContent="Dark" OffContent="Light" />
  <ToggleSwitch IsOn="{Binding CompactLayout}" Content="Compact Layout" />

  <TextBlock Text="Editor" FontWeight="SemiBold" FontSize="16" Margin="0,16,0,0" />
  <CheckBox IsChecked="{Binding ShowGrid}" Content="Show Grid" />
  <CheckBox IsChecked="{Binding AutoSave}" Content="Auto-Save" />

  <TextBlock Text="Notifications" FontWeight="SemiBold" FontSize="16" Margin="0,16,0,0" />
  <CheckBox IsChecked="{Binding NotificationsEnabled}" Content="Enable Notifications" />

  <Button Content="Reset Defaults" Command="{Binding ResetDefaultsCommand}"
          Margin="0,16,0,0" />
</StackPanel>
```

---

## Example 3: Feature Flags with Reactive UI

A feature-flag system where toggling a developer option enables/disables entire sections of the UI reactively.

### Feature Flag Service

```csharp
public sealed class FeatureFlagService
{
    private readonly IToggleService _toggles;

    public FeatureFlagService(IToggleService toggles) => _toggles = toggles;

    public bool IsEnabled(string flag) => _toggles.IsEnabled($"feature:{flag}");
    public void Enable(string flag) => _toggles.Enable($"feature:{flag}");
    public void Disable(string flag) => _toggles.Disable($"feature:{flag}");
    public IObservable<bool> Observe(string flag) => _toggles.Observe($"feature:{flag}");
}
```

### ViewModel

```csharp
public sealed partial class DevToolsViewModel : ObservableObject
{
    private readonly FeatureFlagService _features;

    [ObservableProperty]
    private bool _showPerformanceMonitor;

    [ObservableProperty]
    private bool _showDebugConsole;

    [ObservableProperty]
    private bool _showNodeInspector;

    [ObservableProperty]
    private bool _enableHotReload;

    [ObservableProperty]
    private bool _logNetworkTraffic;

    // Derived visibility — controlled by master toggle
    [ObservableProperty]
    private bool _devToolsVisible;

    public DevToolsViewModel(IToggleService toggles)
    {
        _features = new FeatureFlagService(toggles);

        // Master toggle
        toggles.Observe("feature:devtools")
            .Subscribe(v => DevToolsVisible = v);

        // Individual features
        toggles.Observe("feature:perfmon")
            .Subscribe(v => ShowPerformanceMonitor = v && DevToolsVisible);
        toggles.Observe("feature:debugconsole")
            .Subscribe(v => ShowDebugConsole = v && DevToolsVisible);
        toggles.Observe("feature:nodeinspector")
            .Subscribe(v => ShowNodeInspector = v && DevToolsVisible);
        toggles.Observe("feature:hotreload")
            .Subscribe(v => EnableHotReload = v);
        toggles.Observe("feature:networklog")
            .Subscribe(v => LogNetworkTraffic = v);
    }

    [RelayCommand]
    private void ToggleDevTools() => _features.Enable("devtools");
}
```

### Conditional UI Binding

```xml
<!-- Only visible when DevTools master toggle is enabled -->
<DockPanel IsVisible="{Binding DevToolsVisible}">
  <TextBlock Text="Developer Tools" FontWeight="Bold" DockPanel.Dock="Top" />

  <CheckBox IsChecked="{Binding ShowPerformanceMonitor}" Content="Performance Monitor"
            IsVisible="{Binding DevToolsVisible}" />
  <CheckBox IsChecked="{Binding ShowDebugConsole}" Content="Debug Console"
            IsVisible="{Binding DevToolsVisible}" />
  <CheckBox IsChecked="{Binding ShowNodeInspector}" Content="Node Inspector"
            IsVisible="{Binding DevToolsVisible}" />
  <CheckBox IsChecked="{Binding EnableHotReload}" Content="Hot Reload" />
  <CheckBox IsChecked="{Binding LogNetworkTraffic}" Content="Log Network Traffic" />
</DockPanel>
```

---

## Example 4: Responsive Layout Modes with Reactive Combined State

An application that switches between List, Grid, and Detail view modes, and a separate Compact/Dense mode toggle, with a status bar that reacts to both.

### ViewModel

```csharp
public sealed partial class LayoutViewModel : ObservableObject
{
    private readonly IToggleService _toggles;

    [ObservableProperty]
    private string _layoutMode = "list";

    [ObservableProperty]
    private bool _isCompact;

    [ObservableProperty]
    private string _statusMessage = "List view · Normal density";

    public LayoutViewModel(IToggleService toggles)
    {
        _toggles = toggles;

        // Reactively combine layout mode + compact state
        toggles.Observe("view:list")
            .CombineLatest(
                toggles.Observe("view:grid"),
                toggles.Observe("view:detail"),
                toggles.Observe("layout:compact"),
                (list, grid, detail, compact) => new { list, grid, detail, compact })
            .Subscribe(state =>
            {
                var mode = state.list ? "List" : state.grid ? "Grid" : "Detail";
                var density = state.compact ? "Compact" : "Normal";
                StatusMessage = $"{mode} view · {density} density";
            });

        // Initialize
        toggles.Enable("view:list");
        toggles.Enable("layout:compact"); // default density
    }

    [RelayCommand]
    private void SwitchToList()
    {
        _toggles.Enable("view:list");
        LayoutMode = "list";
    }

    [RelayCommand]
    private void SwitchToGrid()
    {
        _toggles.Enable("view:grid");
        LayoutMode = "grid";
    }

    [RelayCommand]
    private void SwitchToDetail()
    {
        _toggles.Enable("view:detail");
        LayoutMode = "detail";
    }

    [RelayCommand]
    private void ToggleCompact()
    {
        _toggles.Toggle("layout:compact");
    }
}
```

### View

```xml
<DockPanel>
  <!-- Toolbar -->
  <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Spacing="4" Margin="8">
    <ToggleButton Content="List" Command="{Binding SwitchToListCommand}"
                  IsChecked="{Binding LayoutMode, Converter={StaticResource EqualsParam}, ConverterParameter=list}" />
    <ToggleButton Content="Grid" Command="{Binding SwitchToGridCommand}"
                  IsChecked="{Binding LayoutMode, Converter={StaticResource EqualsParam}, ConverterParameter=grid}" />
    <ToggleButton Content="Detail" Command="{Binding SwitchToDetailCommand}"
                  IsChecked="{Binding LayoutMode, Converter={StaticResource EqualsParam}, ConverterParameter=detail}" />
    <Separator Width="1" Height="24" Margin="4,0" />
    <ToggleButton Content="Compact" Command="{Binding ToggleCompactCommand}"
                  IsChecked="{Binding IsCompact}" />
  </StackPanel>

  <!-- Content area (shown conditionally based on layout mode) -->
  <ContentControl Content="{Binding LayoutMode}">
    <ContentControl.DataTemplate>
      <DataTemplate>
        <TextBlock Text="Content area" HorizontalAlignment="Center"
                   VerticalAlignment="Center" FontSize="24" />
      </DataTemplate>
    </ContentControl.DataTemplate>
  </ContentControl>

  <!-- Status bar -->
  <Border DockPanel.Dock="Bottom" Background="{StaticResource SystemAccentColor}" Padding="8">
    <TextBlock Text="{Binding StatusMessage}" />
  </Border>
</DockPanel>
```
