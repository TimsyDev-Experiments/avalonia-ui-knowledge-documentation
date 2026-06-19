---
tier: advanced
topic: tooling
estimated: 25 min
researched: 2026-06-18
avalonia-version: 12.0.4
---

# 088 — XAML Previewer & Hot Reload — Verbose

**Prerequisites:** [088-core](088-xaml-previewer-hot-reload.md)

---

## 1. How the Previewer works internally

The XAML Previewer compiles your `.axaml` file in a separate process using the Avalonia XAML compiler (`Avalonia.Build.Tasks`). It sends the compiled visual tree to a preview host process that renders it with Skia. The host has no access to your C# code-behind, so only static XAML is rendered unless design-time data context is provided.

This is why the previewer shows your control tree and styling but does not execute commands, animations, or event handlers at design time.

---

## 2. Design-time attributes deep dive

### `d:DataContext` with `DesignInstance`

```xml
<Window xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:vm="clr-namespace:MyApp.ViewModels"
        d:DataContext="{d:DesignInstance vm:MainViewModel}">
```

| Attribute | Behavior |
|---|---|
| `IsDesignTimeCreatable=True` | Calls the ViewModel constructor — requires parameterless constructor or design-time factory |
| `CreateList=True` | If the type is enumerable, creates a sample list with 5–10 items |
| `Type` (design-time alias) | Use `vm:MainViewModel` as shorthand |

### `d:DesignInstance` with sample data

```xml
<d:DesignInstance xmlns:models="clr-namespace:MyApp.Models"
                  Type="models:Product"
                  CreateList="True"
                  IsDesignTimeCreatable="True" />
```

### `d:DataContext` with static resource

```xml
<Window.Resources>
  <models:SampleData x:Key="DesignData" />
</Window.Resources>

<d:DataContext>
  <Binding Source="{StaticResource DesignData}" />
</d:DataContext>
```

---

## 3. Hot Reload — architecture

When the app debugs with `AttachDeveloperTools()`, Avalonia starts a `FileSystemWatcher` on the project's output directory. On file write:

1. `FileSystemWatcher` fires for the `.axaml` file.
2. The XAML compiler re-parses just that file.
3. The new visual tree is diffed against the current one.
4. Only changed properties are applied — existing control instances are reused.
5. Bindings are re-resolved if anything in the binding path changed.

This is why Hot Reload is fast even for complex layouts: it never recreates the entire window, it patches in-place.

---

## 4. Hot Reload with merged dictionaries

When you edit a resource dictionary file (`Styles.axaml`, `Colors.axaml`, etc.), Hot Reload re-evaluates all resources from that dictionary but does NOT force a full re-template. Controls that reference changed resources via `DynamicResource` update automatically. Controls using `StaticResource` must be re-loaded (close and re-open the window).

---

## 5. Previewer limitations by platform

| Platform | Previewer Support |
|---|---|
| Windows (Visual Studio) | Full — split pane, design-time data, zoom, orientation |
| macOS (Visual Studio for Mac) | Limited — legacy previewer, design-time data works |
| Linux (Visual Studio Code) | MCP-based only — `attach-to-file` tool |
| Rider (any OS) | Full — AvaloniaRider plugin required |
| .NET Interactive (Jupyter) | Not supported |

---

## 6. Custom previewer host for CI

You can script the previewer for screenshot-based regression tests:

```bash
# Install the tool
dotnet tool install -g avdt

# Attach to a XAML file and capture preview
avdt mcp attach-to-file --file "src/MyApp/Views/MainWindow.axaml"
avdt mcp screenshot --output "preview.png"
avdt mcp detach
```

---

## 7. Known issues

| Issue | Workaround |
|---|---|
| Previewer fails on files with `x:CompileBindings` | Add `d:DataContext` with the exact ViewModel type |
| Previewer crash with third-party controls | Ensure NuGet packages are restored before opening preview |
| Hot Reload stops working after many edits | Restart the app — the delta cache can hit limits |
| Previewer shows blank for files with code-behind logic | Add design-time data; the previewer cannot run C# |
