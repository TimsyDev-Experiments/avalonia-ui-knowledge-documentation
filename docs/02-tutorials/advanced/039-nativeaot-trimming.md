---
tier: advanced
topic: build
estimated: 30 min
researched: 2026-06-12
avalonia-version: 12.0.4
---

# 039 -- NativeAOT and Trimming

**What you'll learn:** How to publish an Avalonia application with NativeAOT for fast startup, low memory, and single-file deployment, plus trimming configuration to avoid reflection-related crashes.

**Prerequisites:** [001 -- Project Setup](../basics/001-project-setup.md)

---

## 1. NativeAOT overview

NativeAOT compiles .NET IL to a native binary with no JIT and no runtime IL interpreter. Benefits for Avalonia:

- Startup: 10-50ms vs 200-500ms (normal)
- Binary size: 15-40 MB (single file)
- Memory: No JIT overhead, no IL metadata at runtime

## 2. Enable NativeAOT in the project file

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IlcTrimMetadata>true</IlcTrimMetadata>
  <IlcGenerateCompleteTypeMetadata>false</IlcGenerateCompleteTypeMetadata>
  <EventSourceSupport>false</EventSourceSupport>
</PropertyGroup>
```

Also target `win-x64`, `linux-x64`, or `osx-x64`:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 3. XAML compiler requirements

Enable the Avalonia XAML compiler for AOT:

```xml
<PropertyGroup>
  <AvaloniaNameGeneratorIsEnabled>true</AvaloniaNameGeneratorIsEnabled>
</PropertyGroup>
```

This generates `x:Name` field access without reflection. Without it, `this.FindControl<T>("name")` uses reflection and crashes under trim.

## 4. Compiled bindings requirement

NativeAOT + trimming removes reflection support. You must use compiled bindings:

```xml
<!-- Good -- compiled binding -->
<TextBlock Text="{Binding Greeting}"
           x:DataType="viewModels:MainViewModel" />

<!-- Bad -- reflection binding, crashes under NativeAOT -->
<TextBlock Text="{Binding Greeting, Mode=ReflectionBinding}" />
```

Enable compiled bindings globally (default in v12):

```xml
<XmlnsDefinitionAttribute xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:DemoApp.ViewModels"
    x:CompileBindings="True" />
```

## 5. Trimming-safe patterns

**Avoid:**

```csharp
// Reflection-driven view location
var viewType = Type.GetType("DemoApp.Views." + viewModel.GetType().Name);
```

**Use:**

```csharp
// Explicit mapping or DI registration
services.AddTransient<MainView>();
services.AddTransient<SettingsView>();
```

**Avoid dynamic resource loading by string:**

```csharp
// Crashes under trim -- string-based resource lookup
Application.Current!.TryFindResource("MyResource");
```

**Use static resource references in XAML:**

```xml
<StaticResource ResourceKey="MyResource" />
```

## 6. Runtime configuration for trimming

Create `rd.xml` (runtime directives):

```xml
<Directives xmlns="urn:standards:metadata:runtime-directives">
  <Application>
    <Assembly Name="Avalonia.Themes.Fluent" Dynamic="Required All" />
    <Assembly Name="DemoApp" Dynamic="Required All" />
    <Type Name="DemoApp.Views.MainWindow" Dynamic="Required All" />
  </Application>
</Directives>
```

Include in `.csproj`:

```xml
<ItemGroup>
  <RdXmlFile Include="rd.xml" />
</ItemGroup>
```

## 7. Publish command examples

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained
# Output: bin/Release/net10.0/win-x64/publish/DemoApp.exe (~18 MB)

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
# Output: bin/Release/net10.0/linux-x64/publish/DemoApp (~22 MB)

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained
# Output: bin/Release/net10.0/osx-x64/publish/DemoApp (~24 MB)

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained
```

## 8. Known trimming issues with Avalonia

| Issue | Symptom | Fix |
|-------|---------|-----|
| Missing `Styles` | Blank window | Add `[DynamicDependency]` or preserve assembly |
| `FindControl` returns null | `x:Name` bindings broken | Enable `AvaloniaNameGenerator` |
| `IValueConverter` not found | Converter fails silently | Keep converter type with `[DynamicallyAccessedMembers]` |
| Data template not applied | Content shows type name | Use `x:DataType` with compiled bindings |
| Fluent theme missing | No styles | Preserve `Avalonia.Themes.Fluent` assembly |

## 9. Size optimization

```xml
<PropertyGroup>
  <!-- Remove globalization data (saves ~2 MB) -->
  <InvariantGlobalization>true</InvariantGlobalization>
  <!-- Remove ICU data (~30 MB savings) -->
  <InvariantTimezone>false</InvariantTimezone>
  <!-- Strip debug metadata -->
  <DebugType>none</DebugType>
  <!-- Optimize for size -->
  <OptimizationPreference>Size</OptimizationPreference>
</PropertyGroup>
```

> **Note:** `InvariantGlobalization` breaks culture-aware formatting. Only use if you control all locale rendering.

## Key takeaways

- Set `PublishAot=true` in `.csproj` for NativeAOT publishing
- Enable `AvaloniaNameGenerator` for trim-safe `x:Name` access
- Use compiled bindings (`x:DataType`) -- reflection bindings crash under trim
- Dynamic resource loading by string key fails under trim; use `StaticResource`
- `rd.xml` preserves assemblies and types needed at runtime
- Test AOT publish early in development -- fixing trim issues late is costly

## See Also

- [039E — NativeAOT and Trimming (examples)](039-nativeaot-trimming-examples.md)
- [039V — NativeAOT and Trimming (verbose companion)](039-nativeaot-trimming-verbose.md)
- [038 — Headless Testing](038-headless-testing.md)
- [042 — Multi-Targeting: Desktop, Browser, Mobile](042-multi-targeting-desktop-browser-mobile.md)
- [Avalonia Docs: NativeAOT](https://docs.avaloniaui.net/docs/concepts/native-aot)
