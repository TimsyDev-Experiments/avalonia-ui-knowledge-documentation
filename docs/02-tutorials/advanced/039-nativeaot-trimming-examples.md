---
tier: advanced
topic: build
estimated: 15-20 min
researched: 2026-06-14
avalonia-version: 12.0.4
example-of: 039-nativeaot-trimming.md
---

# 039E — NativeAOT and Trimming: Real-World Examples

**What this is:** Two complete scenarios that apply NativeAOT-safe patterns — trimmer annotations, compiled bindings, `ILLink.Descriptors.xml`, converter preservation, and dynamic type resolution — to real Avalonia applications.

**Prerequisites:** [039 — NativeAOT and Trimming](039-nativeaot-trimming.md), [039V — Verbose Companion](039-nativeaot-trimming-verbose.md)

---

## Example 1: Plugin System with Trimmer-Safe Type Loading

### Goal

Load external plugin assemblies at runtime, instantiate plugin types, and display their UIs in a tabbed interface — all while keeping the NativeAOT trimmer from removing the plugin types. Use `TrimmerRootAssembly` to preserve plugin assemblies and `[DynamicDependency]` to preserve types loaded by name.

### Trimmer Configuration

```xml
<!-- ILLink.Descriptors.xml -->
<linker>
  <assembly fullname="DemoApp.Plugins.ImageFilter" />
  <assembly fullname="DemoApp.Plugins.TextTools" />
  <assembly fullname="DemoApp">
    <type fullname="DemoApp.Plugins.IPlugin" preserve="all" />
    <type fullname="DemoApp.ViewModels.PluginHostViewModel" preserve="all" />
  </assembly>
</linker>
```

```xml
<!-- .csproj additions -->
<ItemGroup>
  <TrimmerRootDescriptor Include="ILLink.Descriptors.xml" />
  <!-- Also root plugin assemblies directly -->
  <TrimmerRootAssembly Include="DemoApp.Plugins.ImageFilter" />
  <TrimmerRootAssembly Include="DemoApp.Plugins.TextTools" />
</ItemGroup>
```

### Plugin Interface and Base

```csharp
using Avalonia.Controls;

namespace DemoApp.Plugins;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    Control CreateView();
}
```

### Plugin Implementations (referenced by project reference, not by string)

```csharp
// DemoApp.Plugins.ImageFilter/FilterPlugin.cs
using Avalonia.Controls;
using Avalonia.Media;
using DemoApp.Plugins;

namespace DemoApp.Plugins.ImageFilter;

[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FilterPlugin))]
public class FilterPlugin : IPlugin
{
    public string Name => "Image Filter";
    public string Description => "Apply filters to loaded images.";

    public Control CreateView()
    {
        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Filter Settings" },
                new Slider { Minimum = 0, Maximum = 100, Value = 50 }
            }
        };
    }
}
```

### PluginHostViewModel — Safe Dynamic Loading

```csharp
using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DemoApp.ViewModels;

public partial class PluginTab : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    public required IPlugin Plugin { get; init; }
}

public partial class PluginHostViewModel : ObservableObject
{
    private static readonly HashSet<string> KnownPluginAssemblies = new()
    {
        "DemoApp.Plugins.ImageFilter",
        "DemoApp.Plugins.TextTools"
    };

    public ObservableCollection<PluginTab> Tabs { get; } = new();

    [ObservableProperty]
    private PluginTab? _activeTab;

    [ObservableProperty]
    private string? _loadError;

    [RelayCommand]
    private void LoadPlugins()
    {
        LoadError = null;
        Tabs.Clear();

        foreach (var asmName in KnownPluginAssemblies)
        {
            try
            {
                var assembly = Assembly.Load(asmName);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract);

                foreach (var type in pluginTypes)
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin)
                    {
                        Tabs.Add(new PluginTab
                        {
                            Name = plugin.Name,
                            Description = plugin.Description,
                            Plugin = plugin
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LoadError = $"Failed to load {asmName}: {ex.Message}";
            }
        }
    }

    public Control? CreatePluginView(PluginTab tab)
    {
        return tab.Plugin.CreateView();
    }
}
```

### Plugin View (XAML)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             x:Class="DemoApp.Views.PluginHostView"
             x:DataType="vm:PluginHostViewModel">
  <Grid RowDefinitions="Auto,*,Auto" Margin="8" Spacing="8">
    <StackPanel Grid.Row="0" Orientation="Horizontal" Spacing="8">
      <Button Content="Load Plugins"
              Command="{Binding LoadPluginsCommand}" />
    </StackPanel>

    <TabControl Grid.Row="1"
                ItemsSource="{Binding Tabs}"
                SelectedItem="{Binding ActiveTab}"
                x:DataType="vm:PluginHostViewModel">
      <TabControl.ItemTemplate>
        <DataTemplate x:DataType="vm:PluginTab">
          <TextBlock Text="{Binding Name}" />
        </DataTemplate>
      </TabControl.ItemTemplate>
      <TabControl.ContentTemplate>
        <DataTemplate x:DataType="vm:PluginTab">
          <!-- ContentControl that resolves the plugin view -->
          <ContentControl Content="{Binding Plugin}"
                          ContentTemplate="{Binding $parent.DataContext.PluginTemplate}" />
        </DataTemplate>
      </TabControl.ContentTemplate>
    </TabControl>

    <TextBlock Grid.Row="2"
               Text="{Binding LoadError}"
               Foreground="{DynamicResource ErrorBrush}"
               IsVisible="{Binding LoadError, Converter={StaticResource IsNotNullConverter}}" />
  </Grid>
</UserControl>
```

### How It Works

1. **`TrimmerRootAssembly` preserves plugin assemblies** — The linker sees `DemoApp.Plugins.ImageFilter` and `DemoApp.Plugins.TextTools` as roots. All public types in those assemblies are preserved even though the trimmer does not see explicit `new` calls for them.

2. **`ILLink.Descriptors.xml` preserves the interface** — The `IPlugin` interface and `PluginHostViewModel` are marked for full preservation, ensuring `Activator.CreateInstance` and `GetTypes` work at runtime.

3. **Assembly.Load by known name** — The plugin assemblies are referenced by project reference (compile-time), not loaded from external DLLs at runtime. `Assembly.Load("DemoApp.Plugins.ImageFilter")` succeeds because the assembly is embedded in the native binary. The know set `KnownPluginAssemblies` maps to compile-time dependencies.

4. **`[DynamicDependency]` as defense-in-depth** — `FilterPlugin` is annotated with `[DynamicDependency]`. If the `TrimmerRootAssembly` entry were missing, this attribute would still preserve the type.

5. **View per plugin** — `IPlugin.CreateView()` returns an `Avalonia.Controls.Control`. The host displays it inside a `TabControl`. No XAML reflection is involved — each plugin provides its own UI programmatically.

### Design Decisions and Trade-offs

- **Compile-time plugin references vs runtime DLL loading** — True runtime plugin loading (`Assembly.LoadFile` from disk) is incompatible with NativeAOT because the native binary cannot load arbitrary IL assemblies at runtime. All "plugins" must be compile-time dependencies.
- **`KnownPluginAssemblies` set vs auto-discovery** — A hardcoded set is the AOT-safe approach. Auto-discovery (scanning a directory for DLLs) works only in a JIT context.
- **`IPlugin` as interface vs abstract class** — Interface allows trim-safe `is` checks. Abstract classes require `[DynamicDependency]` on the subclass constructors.

---

## Example 2: AOT-Safe IValueConverter with Conditional Formatting

### Goal

Create a custom `IValueConverter` that formats log entries with color and style based on log level — and ensure the converter survives trimming, is referenced through compiled bindings, and preserves all `DynamicallyAccessedMembers` that the binding engine needs.

### Converter Implementation

```csharp
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DemoApp.Converters;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public class LogLevelToBrushConverter : IValueConverter
{
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#4EC9B0"));
    private static readonly IBrush WarnBrush = new SolidColorBrush(Color.Parse("#CE9178"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#F44747"));
    private static readonly IBrush DebugBrush = new SolidColorBrush(Color.Parse("#569CD6"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string level)
            return Brushes.Gray;

        return level.ToUpperInvariant() switch
        {
            "INFO" => InfoBrush,
            "WARN" or "WARNING" => WarnBrush,
            "ERROR" or "FATAL" => ErrorBrush,
            "DEBUG" or "TRACE" => DebugBrush,
            _ => Brushes.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
```

### Markup Extension for AOT-Safe Resource Access

```csharp
using Avalonia.Markup.Xaml;

namespace DemoApp.Markup;

public class AotResourceExtension : MarkupExtension
{
    public string? Key { get; set; }

    public override object ProvideValue()
    {
        if (Key is null)
            throw new InvalidOperationException("Key must be set.");

        // StaticResource lookup at XAML compile time — trim-safe
        return new StaticResourceExtension(Key).ProvideValue();
    }
}
```

### Converter Registration (Static Resource)

```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:converters="using:DemoApp.Converters"
             xmlns:markup="using:DemoApp.Markup">
  <Application.Resources>
    <converters:LogLevelToBrushConverter x:Key="LogLevelToBrush" />
    <SolidColorBrush x:Key="AppBackground"
                     Color="#1E1E2E" />
  </Application.Resources>
</Application>
```

### View Using the Converter (Compiled Bindings)

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:DemoApp.ViewModels"
             xmlns:markup="using:DemoApp.Markup"
             x:Class="DemoApp.Views.LogEntryView"
             x:DataType="vm:LogEntry">
  <Grid ColumnDefinitions="Auto,*,Auto" Margin="4" Spacing="8">
    <!-- Level badge with trim-safe converter -->
    <Border Grid.Column="0" CornerRadius="3" Padding="6,2"
            Background="{Binding Level,
                Converter={StaticResource LogLevelToBrush}}">
      <TextBlock Text="{Binding Level}" FontSize="11" />
    </Border>

    <!-- Message with compiled binding -->
    <TextBlock Grid.Column="1"
               Text="{Binding Message}" />

    <!-- AOT-safe resource via custom markup extension -->
    <TextBlock Grid.Column="2"
               Foreground="{markup:AotResource Key=AppBackground}"
               Text="{Binding Timestamp, StringFormat='{0:g}'}" />
  </Grid>
</UserControl>
```

### Headless Test (Verifying the Converter Survives Trimming)

```csharp
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using DemoApp.Converters;
using FluentAssertions;

namespace DemoApp.Tests;

public class LogLevelToBrushConverterTests
{
    private readonly LogLevelToBrushConverter _converter = new();

    [AvaloniaTheory]
    [InlineData("INFO", "#4EC9B0")]
    [InlineData("WARN", "#CE9178")]
    [InlineData("ERROR", "#F44747")]
    [InlineData("DEBUG", "#569CD6")]
    [InlineData("warning", "#CE9178")]
    [InlineData("FATAL", "#F44747")]
    [InlineData("UNKNOWN", "#808080")]
    [InlineData(null, "#808080")]
    public void Convert_ReturnsExpectedColor(string? input, string expectedHex)
    {
        var result = _converter.Convert(input, typeof(IBrush), null, null);
        result.Should().BeOfType<SolidColorBrush>();

        var brush = (SolidColorBrush)result!;
        brush.Color.ToString().Should().Be(expectedHex);
    }
}
```

### How It Works

1. **`[DynamicallyAccessedMembers]` on converter** — The attribute tells the trimmer to preserve all public methods on `LogLevelToBrushConverter`. The binding engine calls `Convert` via reflection; without this attribute, the trimmer could remove `Convert` if no static call is visible.

2. **Converter as static resource** — The converter is declared in `App.axaml` as a `<converters:LogLevelToBrushConverter>` resource. The XAML compiler emits a direct type reference (not an `Activator.CreateInstance` string), so the trimmer sees the constructor call.

3. **Compiled bindings with `Converter` parameter** — `{Binding Level, Converter={StaticResource LogLevelToBrush}}` is compiled binding. The XAML compiler generates code that retrieves the `LogLevelToBrushConverter` instance and calls `Convert` directly — no reflection at runtime.

4. **Custom `AotResourceExtension`** — The markup extension wraps `StaticResourceExtension`, which is resolved at XAML compile time. This avoids `DynamicResource` (which uses runtime key lookup that can fail under trim). The extension is simple enough that the trimmer preserves it.

5. **Test verifies trim safety** — The headless test creates the converter directly and calls `Convert` in various cases. If the trimmer removed the converter, the test would fail with `TypeLoadException` or `MissingMethodException` at the `new LogLevelToBrushConverter()` line.

### Design Decisions and Trade-offs

- **`StaticResource` vs `DynamicResource`** — `StaticResource` is resolved at XAML load time and is trim-safe. `DynamicResource` requires runtime key lookup and may fail under trim unless the key is preserved. Always prefer `StaticResource` for AOT builds.
- **`[DynamicallyAccessedMembers]` scope** — The example applies it to the whole converter class. A more targeted approach applies it only to `Convert` and `ConvertBack` methods, but the class-level attribute is simpler and the trimmer cost is negligible.
- **No `IValueConverter` base class** — `LogLevelToBrushConverter` implements `IValueConverter` directly. Some projects use a `ConverterBase` helper that provides null-safe `Convert`/`ConvertBack`. That would also need `[DynamicallyAccessedMembers]`.

---

## Comparison: What the Two Examples Demonstrate

| Aspect | Example 1 — Plugin System | Example 2 — AOT-Safe Converter |
|--------|---------------------------|---------------------------------|
| Trimmer preservation | `TrimmerRootAssembly` + `ILLink.Descriptors.xml` | `[DynamicallyAccessedMembers]` + static resource |
| Dynamic type loading | `Assembly.Load` by name | `Activator.CreateInstance` avoided (static resource) |
| Compiled bindings | Implicit (`x:DataType` on `TabControl`) | Explicit (`Converter={StaticResource ...}`) |
| Converter usage | Not needed | `IValueConverter` with `DynamicallyAccessedMembers` |
| Markup extension | None | Custom `AotResourceExtension` |
| Plugin architecture | Compile-time only (NativeAOT constraint) | Not applicable |
| View creation | `IPlugin.CreateView()` returns `Control` | Pure XAML with static resources |
| Test strategy | Integration test loading assemblies | Unit test on converter directly |

## See Also

- [039 — NativeAOT and Trimming](039-nativeaot-trimming.md) — the original tutorial
- [039V — NativeAOT and Trimming (verbose companion)](039-nativeaot-trimming-verbose.md)
- [038 — Headless Testing](038-headless-testing.md) — testing AOT-safe patterns
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md) — NativeAOT compatibility per platform
- [Avalonia Docs: NativeAOT](https://docs.avaloniaui.net/docs/concepts/native-aot)
- [Microsoft Docs: NativeAOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
