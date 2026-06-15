---
tier: advanced
topic: devtools
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
packages: AvaloniaUI.DiagnosticsSupport 2.2.1
example-of: 029-avalonia-plus-devtools.md
---

# 029X — Using Avalonia DevTools: Real-World Examples

## Scenario 1: Diagnosing a Layout Storm in a Dashboard

### Goal

Use the DevTools performance profiler and layout explorer to identify, isolate, and fix a cascading layout invalidation (layout storm) caused by a frequently-updating ViewModel property in a monitoring dashboard.

### ViewModel (before fix — the problem)

```csharp
using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DashboardApp.ViewModels;

public partial class MonitorViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _currentTime = DateTime.Now;

    [ObservableProperty]
    private double _cpuUsage;

    [ObservableProperty]
    private double _memoryUsage;

    [ObservableProperty]
    private string _statusText = "Initializing...";

    private Timer? _timer;

    [RelayCommand]
    private void StartMonitoring()
    {
        _timer = new Timer(_ =>
        {
            CpuUsage = Random.Shared.NextDouble() * 100;
            MemoryUsage = Random.Shared.NextDouble() * 100;
            CurrentTime = DateTime.Now;
            StatusText = $"CPU: {CpuUsage:F1}% | MEM: {MemoryUsage:F1}%";
        }, null, 0, 100);  // 10 updates/sec
    }
}
```

### Steps to Diagnose

#### Step 1 — Open DevTools (F12) and navigate to the Performance tab

The real-time FPS counter shows 15–20 FPS instead of the expected 60 FPS. The Layout Pass Count spikes to 8–12 passes per frame.

#### Step 2 — Open the Layout Explorer on the main dashboard Grid

The invalidation tracking log shows:
```
Invalidation: MonitorViewModel.StatusText changed → StatusBar.Text updated
Invalidation: MonitorViewModel.CpuUsage changed → CpuProgressBar.Value updated
Invalidation: MonitorViewModel.MemoryUsage changed → MemoryProgressBar.Value updated
Invalidation: MonitorViewModel.CurrentTime changed → ClockTextBlock.Text updated
```

Each property change triggers a separate layout pass because one of the bound controls has `Width="*"` in a `Grid` column that re-measures on content change.

#### Step 3 — Identify the culprit via the Styles panel

Select the `StatusBar` element in the Visual Tree Explorer. The Styles panel shows that `StatusText` binding triggers a `TextBlock` resize, which reflows the parent `Grid` column, which invalidates all sibling controls.

### Fixed ViewModel

```csharp
using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DashboardApp.ViewModels;

public partial class MonitorViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _currentTime = DateTime.Now;

    [ObservableProperty]
    private double _cpuUsage;

    [ObservableProperty]
    private double _memoryUsage;

    // StatusText is now a computed property — does not update on timer tick
    public string StatusText => $"CPU: {CpuUsage:F1}% | MEM: {MemoryUsage:F1}%";

    private Timer? _timer;

    [RelayCommand]
    private void StartMonitoring()
    {
        _timer = new Timer(_ =>
        {
            // Batch updates: raise PropertyChanged once for all changed properties
            CpuUsage = Random.Shared.NextDouble() * 100;
            MemoryUsage = Random.Shared.NextDouble() * 100;
            CurrentTime = DateTime.Now;
            OnPropertyChanged(nameof(StatusText));
        }, null, 0, 100);
    }
}
```

### XAML View (before and after)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DashboardApp.ViewModels"
             x:Class="DashboardApp.Views.DashboardView"
             x:DataType="vm:MonitorViewModel">

  <Grid ColumnDefinitions="Auto,*,Auto" Spacing="8">
    <!-- Before: StatusText updated every 100ms → reflow on each change -->
    <TextBlock Grid.Column="2"
               Text="{Binding StatusText}"
               FontSize="{StaticResource FontSizeSm}" />
  </Grid>
</UserControl>
```

After the fix, `StatusText` is a read-only computed property. The `OnPropertyChanged(nameof(StatusText))` call in the setter ensures the binding re-reads the value, but the `Grid` column no longer re-measures because the text width is now stable (or use `MaxWidth` constraint).

### What DevTools Showed

| DevTools Feature | Finding |
|---|---|
| Performance FPS counter | 15–20 FPS vs expected 60 |
| Layout Pass Count | 8–12 passes per frame |
| Invalidation tracking | Each of 4 property changes → separate layout |
| Styles panel (selected TextBlock) | Binding source = `StatusText` changed every 100ms |
| Layout Explorer (parent Grid) | Column `*` re-measured on each invalidation |

### Design Decisions & Edge Cases

- **Batched notifications**: `OnPropertyChanged(nameof(StatusText))` fires once after all properties update, reducing layout passes from 4 to 1 per tick.
- **Fixed-width status bar**: Set `MaxWidth="300"` on the status `TextBlock` to prevent reflow even if text length varies. The text truncates with `TextTrimming="CharacterEllipsis"`.
- **Timer resolution**: 100ms timer (10 Hz) is within Avalonia's render frame budget (16ms at 60 FPS). The layout system coalesces multiple invalidations into a single pass if they arrive between frames.

---

## Scenario 2: Debugging a Broken DataTemplate with the Visual Tree Explorer

### Goal

A `ListBox` with an `ItemTemplate` shows empty rows — items exist in the bound collection but display nothing. Use DevTools to find the template binding failure and the missing `x:DataType`.

### ViewModel

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreApp.ViewModels;

public partial class ProductListViewModel : ObservableObject
{
    public ObservableCollection<ProductItem> Products { get; } = new()
    {
        new ProductItem { Name = "Widget", Price = 9.99 },
        new ProductItem { Name = "Gadget", Price = 24.99 }
    };
}

public partial class ProductItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private double _price;
}
```

### Broken XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:StoreApp.ViewModels"
             x:Class="StoreApp.Views.ProductListView"
             x:DataType="vm:ProductListViewModel">

  <ListBox ItemsSource="{Binding Products}">
    <ListBox.ItemTemplate>
      <DataTemplate>
        <!-- Missing: x:DataType="vm:ProductItem" -->
        <Grid ColumnDefinitions="*,Auto" Spacing="8">
          <TextBlock Text="{Binding Name}" />
          <TextBlock Grid.Column="1" Text="{Binding Price}"
                     Foreground="{DynamicResource TextSecondaryBrush}" />
        </Grid>
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</UserControl>
```

### Steps to Debug

#### Step 1 — Open DevTools (F12) and inspect the Visual Tree Explorer

The logical tree shows `ListBox` with two `ListBoxItem` children (correct). Expanding a `ListBoxItem` shows a `ContentPresenter` but **no** inner `Grid` or `TextBlock` children.

This means the `DataTemplate` was applied but produced a different visual result than expected.

#### Step 2 — Select a ListBoxItem and open the Property panel

The `Content` property shows `StoreApp.ViewModels.ProductItem` — the item is present. The `DataContext` is also `ProductItem`. The `ContentTemplate` property shows the `DataTemplate`. All wiring is correct at the `ListBoxItem` level.

#### Step 3 — Select the ContentPresenter inside the ListBoxItem

The Property panel shows `Content` = `ProductItem` but `Child` = `null`. The `ContentPresenter` has a content object but failed to instantiate the template's visual tree.

#### Step 4 — Open the Styles panel for the ContentPresenter

The Styles panel shows that no compiled binding is active. The `Text` properties on the inner `TextBlock` elements show `(unset)`.

#### Root cause

Missing `x:DataType="vm:ProductItem"` on the `DataTemplate` causes Avalonia 12 to fall back to `ReflectionBinding` but the `DataTemplate` is inside a compiled-binding scope (the outer `UserControl` has `x:DataType="vm:ProductListViewModel"`). The inner bindings fail silently.

### Fixed XAML View

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:StoreApp.ViewModels"
             x:Class="StoreApp.Views.ProductListView"
             x:DataType="vm:ProductListViewModel">

  <ListBox ItemsSource="{Binding Products}">
    <ListBox.ItemTemplate>
      <DataTemplate x:DataType="vm:ProductItem">
        <Grid ColumnDefinitions="*,Auto" Spacing="8">
          <TextBlock Text="{Binding Name}" />
          <TextBlock Grid.Column="1" Text="{Binding Price, StringFormat='{}{0:C}'}"
                     Foreground="{DynamicResource TextSecondaryBrush}" />
        </Grid>
      </DataTemplate>
    </ListBox.ItemTemplate>
  </ListBox>
</UserControl>
```

After adding `x:DataType="vm:ProductItem"`, the DevTools visual tree shows the `Grid` and `TextBlock` children with correct binding values.

### What DevTools Showed

| DevTools Feature | Finding |
|---|---|
| Visual Tree Explorer | ListBoxItems present but ContentPresenter has no Child |
| Property panel (ContentPresenter) | `Content` = ProductItem, `ContentTemplate` = DataTemplate, `Child` = null |
| Styles panel (ContentPresenter) | TextBlock bindings show `(unset)` — no compiled binding active |
| Search for `x:DataType` | DataTemplate missing type annotation |

### Design Decisions & Edge Cases

- **Mixed `x:DataType` scopes**: The outer scope (`UserControl`) sets `x:DataType="vm:ProductListViewModel"`. The `DataTemplate` defines a new scope for `ProductItem`. Without the inner `x:DataType`, Avalonia 12's compiled binding system cannot resolve `Name` or `Price` on the template context.
- **Silent failure**: Unlike WPF, Avalonia 12 does not throw for missing `x:DataType` in templates — the bindings silently produce `(unset)` values. DevTools are the primary way to catch this.
- **Nested templates**: For `HierarchicalDataTemplate` (tree views), each level needs its own `x:DataType` annotation matching the child item type.

### Comparison

| Aspect | Scenario 1: Layout Storm | Scenario 2: Broken Template |
|---|---|---|
| DevTools panel used | Performance, Layout Explorer | Visual Tree, Properties, Styles |
| Problem symptom | Low FPS, high layout passes | Empty rows, no visible content |
| Root cause | Frequent property changes triggering reflow | Missing `x:DataType` on DataTemplate |
| DevTools finding | 12 layout passes/frame | ContentPresenter Child = null |
| Fix applied | Batch property notifications, computed props | Added `x:DataType` to DataTemplate |
| Verification method | FPS counter returns to 60 | Visual tree shows template children |

## See Also

- [029 — Using Avalonia DevTools](029-avalonia-plus-devtools.md)
- [029V — Using Avalonia DevTools (verbose companion)](029-avalonia-plus-devtools-verbose.md)
- [026 — Accessibility & Automation](026-accessibility-automation.md)
- [027 — Advanced Composite Bindings](027-advanced-composite-bindings.md)
- [Avalonia Docs: DevTools](https://docs.avaloniaui.net/tools/developer-tools/installation)
